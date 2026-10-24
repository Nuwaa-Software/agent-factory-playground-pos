using System.Net;
using System.Net.Http.Json;
using MiniPos.Api.Features.Common;
using MiniPos.Api.Features.Users.CreateUser;
using MiniPos.Domain.Users;

namespace MiniPos.Tests.Integration;

public class UserTests(PostgresFixture db) : ApiTestBase(db)
{
    // ── Create (POST /api/users) ──────────────────────────────────────

    [Fact]
    public async Task Admin_creates_a_user_returns_201()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var created = await client.PostAsJsonAsync("/api/users",
            new CreateUserCommand("Ahmed Ali", "أحمد علي", UserRole.Admin));

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var user = (await created.Content.ReadFromJsonAsync<UserResponse>())!;
        user.Id.Should().NotBeEmpty();
        user.NameEn.Should().Be("Ahmed Ali");
        user.NameAr.Should().Be("أحمد علي");
        user.Role.Should().Be(UserRole.Admin);
        user.IsActive.Should().BeTrue();
        user.CreatedBy.Should().NotBeNullOrEmpty();
        user.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task Create_with_invalid_role_returns_400()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Send a JSON object with an invalid role string — deserialization fails first, then validation.
        var body = new { nameEn = "Test", nameAr = "اختبار", role = "Superuser" };
        var response = await client.PostAsJsonAsync("/api/users", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_with_empty_nameEn_returns_400()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var body = new { nameEn = "", nameAr = "اختبار", role = UserRole.Cashier };
        var response = await client.PostAsJsonAsync("/api/users", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_with_null_nameEn_returns_400()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var body = new { nameEn = (string?)null, nameAr = "اختبار", role = UserRole.Cashier };
        var response = await client.PostAsJsonAsync("/api/users", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_with_X_User_Name_sets_CreatedBy()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);
        client.DefaultRequestHeaders.Add("X-User-Name", "test-user");

        var created = await client.PostAsJsonAsync("/api/users",
            new CreateUserCommand("Test User", "مستخدم اختبار", UserRole.Cashier));

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var user = (await created.Content.ReadFromJsonAsync<UserResponse>())!;
        user.CreatedBy.Should().Be("test-user");
    }

    [Fact]
    public async Task Create_without_X_User_Name_falls_back_to_role()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var created = await client.PostAsJsonAsync("/api/users",
            new CreateUserCommand("Test User", "مستخدم اختبار", UserRole.Cashier));

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var user = (await created.Content.ReadFromJsonAsync<UserResponse>())!;
        user.CreatedBy.Should().Be("Admin");
    }

    // ── List (GET /api/users) ─────────────────────────────────────────

    [Fact]
    public async Task List_empty_company_returns_200_empty_list()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var list = await client.GetFromJsonAsync<List<UserResponse>>("/api/users");

        list.Should().NotBeNull();
        list!.Should().BeEmpty();
    }

    [Fact]
    public async Task List_populated_company_returns_users_with_role_and_is_active()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        await client.PostAsJsonAsync("/api/users",
            new CreateUserCommand("User One", "مستخدم واحد", UserRole.Cashier));
        await client.PostAsJsonAsync("/api/users",
            new CreateUserCommand("User Two", "مستخدم اثنين", UserRole.Viewer));

        var list = await client.GetFromJsonAsync<List<UserResponse>>("/api/users");

        list.Should().NotBeNull();
        var users = list!;
        users.Should().HaveCount(2);
        foreach (var u in users)
        {
            u.Role.Should().BeOneOf(UserRole.Admin, UserRole.Cashier, UserRole.Viewer);
            u.IsActive.Should().BeTrue();
        }
    }

    // ── Update (PATCH /api/users/{id}) ────────────────────────────────

    [Fact]
    public async Task Patch_role_changes_and_persists()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var created = await client.PostAsJsonAsync("/api/users",
            new CreateUserCommand("Original", "أصلي", UserRole.Cashier));
        var user = (await created.Content.ReadFromJsonAsync<UserResponse>())!;

        var update = new { role = UserRole.Viewer };
        var response = await client.PatchAsJsonAsync($"/api/users/{user.Id}", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await response.Content.ReadFromJsonAsync<UserResponse>())!;
        updated.Role.Should().Be(UserRole.Viewer);
        updated.ModifiedBy.Should().NotBeNull();
        updated.ModifiedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Patch_deactivate_sets_is_active_false()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var created = await client.PostAsJsonAsync("/api/users",
            new CreateUserCommand("Deactivated", "معطل", UserRole.Cashier));
        var user = (await created.Content.ReadFromJsonAsync<UserResponse>())!;

        var update = new { isActive = false };
        var response = await client.PatchAsJsonAsync($"/api/users/{user.Id}", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await response.Content.ReadFromJsonAsync<UserResponse>())!;
        updated.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Deactivated_user_still_appears_in_list()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var created = await client.PostAsJsonAsync("/api/users",
            new CreateUserCommand("Deactivated User", "مستخدم معطل", UserRole.Cashier));
        var user = (await created.Content.ReadFromJsonAsync<UserResponse>())!;

        await client.PatchAsJsonAsync($"/api/users/{user.Id}", new { isActive = false });

        var list = await client.GetFromJsonAsync<List<UserResponse>>("/api/users");
        list!.Should().Contain(u => u.Id == user.Id && u.IsActive == false);
    }

    [Fact]
    public async Task Patch_reactivate_sets_is_active_true()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var created = await client.PostAsJsonAsync("/api/users",
            new CreateUserCommand("Reactivate", "تفعيل", UserRole.Cashier));
        var user = (await created.Content.ReadFromJsonAsync<UserResponse>())!;

        await client.PatchAsJsonAsync($"/api/users/{user.Id}", new { isActive = false });
        var update = await client.PatchAsJsonAsync($"/api/users/{user.Id}", new { isActive = true });

        update.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await update.Content.ReadFromJsonAsync<UserResponse>())!;
        updated.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Patch_unknown_id_returns_404()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var response = await client.PatchAsJsonAsync($"/api/users/{Guid.NewGuid()}", new { role = UserRole.Viewer });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Auth ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Cashier_cannot_create_users()
    {
        var company = await Factory.CreateCompanyAsync();
        var response = await Factory.ClientFor(company, "Cashier").PostAsJsonAsync("/api/users",
            new CreateUserCommand("Hacker", "هاكر", UserRole.Cashier));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Viewer_cannot_create_users()
    {
        var company = await Factory.CreateCompanyAsync();
        var response = await Factory.ClientFor(company, "Viewer").PostAsJsonAsync("/api/users",
            new CreateUserCommand("Hacker", "هاكر", UserRole.Cashier));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Cashier_cannot_list_users()
    {
        var company = await Factory.CreateCompanyAsync();
        var response = await Factory.ClientFor(company, "Cashier").GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Viewer_cannot_list_users()
    {
        var company = await Factory.CreateCompanyAsync();
        var response = await Factory.ClientFor(company, "Viewer").GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Cashier_cannot_update_users()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var created = await client.PostAsJsonAsync("/api/users",
            new CreateUserCommand("Target", "هدف", UserRole.Cashier));
        var user = (await created.Content.ReadFromJsonAsync<UserResponse>())!;

        var response = await Factory.ClientFor(company, "Cashier")
            .PatchAsJsonAsync($"/api/users/{user.Id}", new { role = UserRole.Viewer });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Viewer_cannot_update_users()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var created = await client.PostAsJsonAsync("/api/users",
            new CreateUserCommand("Target", "هدف", UserRole.Cashier));
        var user = (await created.Content.ReadFromJsonAsync<UserResponse>())!;

        var response = await Factory.ClientFor(company, "Viewer")
            .PatchAsJsonAsync($"/api/users/{user.Id}", new { role = UserRole.Viewer });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Anonymous_get_users_returns_401()
    {
        var response = await Factory.CreateClient().GetAsync(new Uri("/api/users", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Tenant isolation ──────────────────────────────────────────────

    [Fact]
    public async Task Company_A_users_not_visible_to_company_B()
    {
        var companyA = await Factory.CreateCompanyAsync("A", "أ");
        var companyB = await Factory.CreateCompanyAsync("B", "ب");

        var clientA = Factory.ClientFor(companyA);
        await clientA.PostAsJsonAsync("/api/users",
            new CreateUserCommand("Ahmed", "أحمد", UserRole.Admin));

        var seenByB = await Factory.ClientFor(companyB).GetFromJsonAsync<List<UserResponse>>("/api/users");
        seenByB.Should().BeEmpty();
    }

    [Fact]
    public async Task Company_B_patch_on_company_A_user_returns_404()
    {
        var companyA = await Factory.CreateCompanyAsync("A", "أ");
        var companyB = await Factory.CreateCompanyAsync("B", "ب");

        var clientA = Factory.ClientFor(companyA);
        var created = await clientA.PostAsJsonAsync("/api/users",
            new CreateUserCommand("Target", "هدف", UserRole.Cashier));
        var user = (await created.Content.ReadFromJsonAsync<UserResponse>())!;

        var response = await Factory.ClientFor(companyB)
            .PatchAsJsonAsync($"/api/users/{user.Id}", new { role = UserRole.Viewer });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

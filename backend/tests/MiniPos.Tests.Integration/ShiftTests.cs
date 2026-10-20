using System.Net;
using System.Net.Http.Json;
using MiniPos.Api.Features.Shifts;
using MiniPos.Api.Auth;

namespace MiniPos.Tests.Integration;

public class ShiftTests(PostgresFixture db) : ApiTestBase(db)
{
    [Fact]
    public async Task Open_shift_returns_201_with_details()
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync());
        
        var response = await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "John Doe", startingCashHalalas = 10000 });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var shift = (await response.Content.ReadFromJsonAsync<ShiftResponse>())!;
        
        shift.CashierName.Should().Be("John Doe");
        shift.StartingCashHalalas.Should().Be(10000);
        shift.OpenedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        shift.ClosedAt.Should().BeNull();
        shift.ActualCashHalalas.Should().BeNull();
        shift.ExpectedCashHalalas.Should().BeNull();
        shift.DiscrepancyHalalas.Should().BeNull();
    }

    [Fact]
    public async Task Open_shift_rejects_negative_starting_cash()
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync());
        
        var response = await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "John Doe", startingCashHalalas = -100 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Open_shift_rejects_empty_cashier_name()
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync());
        
        var response = await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "", startingCashHalalas = 10000 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Open_shift_rejects_long_cashier_name()
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync());
        
        var response = await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = new string('A', 101), startingCashHalalas = 10000 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Multiple_open_shifts_allowed()
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync());
        
        var response1 = await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "John Doe", startingCashHalalas = 10000 });
        
        var response2 = await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "Jane Smith", startingCashHalalas = 5000 });

        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        response2.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Close_shift_sets_closed_at_and_actual_cash()
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync());
        
        // Open a shift
        var openResponse = await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "John Doe", startingCashHalalas = 10000 });
        
        var shift = (await openResponse.Content.ReadFromJsonAsync<ShiftResponse>())!;
        
        // Close the shift
        var closeResponse = await client.PatchAsJsonAsync($"/api/pos/shifts/{shift.Id}/close",
            new { actualCashHalalas = 12000 });

        closeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var closedShift = (await closeResponse.Content.ReadFromJsonAsync<ShiftResponse>())!;
        
        closedShift.ClosedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        closedShift.ActualCashHalalas.Should().Be(12000);
        closedShift.ExpectedCashHalalas.Should().BeNull();
        closedShift.DiscrepancyHalalas.Should().BeNull();
    }

    [Fact]
    public async Task Close_already_closed_shift_returns_409()
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync());
        
        // Open a shift
        var openResponse = await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "John Doe", startingCashHalalas = 10000 });
        
        var shift = (await openResponse.Content.ReadFromJsonAsync<ShiftResponse>())!;
        
        // Close the shift
        await client.PatchAsJsonAsync($"/api/pos/shifts/{shift.Id}/close",
            new { actualCashHalalas = 12000 });
        
        // Try to close again
        var closeResponse = await client.PatchAsJsonAsync($"/api/pos/shifts/{shift.Id}/close",
            new { actualCashHalalas = 13000 });

        closeResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Close_non_existent_shift_returns_404()
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync());
        
        var response = await client.PatchAsJsonAsync("/api/pos/shifts/00000000-0000-0000-0000-000000000000/close",
            new { actualCashHalalas = 12000 });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_shift_returns_details()
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync());
        
        // Open a shift
        var openResponse = await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "John Doe", startingCashHalalas = 10000 });
        
        var shift = (await openResponse.Content.ReadFromJsonAsync<ShiftResponse>())!;
        
        // Get the shift
        var getResponse = await client.GetAsync($"/api/pos/shifts/{shift.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrievedShift = (await getResponse.Content.ReadFromJsonAsync<ShiftResponse>())!;
        
        retrievedShift.Id.Should().Be(shift.Id);
        retrievedShift.CashierName.Should().Be("John Doe");
        retrievedShift.StartingCashHalalas.Should().Be(10000);
        retrievedShift.OpenedAt.Should().BeCloseTo(shift.OpenedAt, TimeSpan.FromSeconds(5));
        retrievedShift.ClosedAt.Should().BeNull();
    }

    [Fact]
    public async Task Get_non_existent_shift_returns_404()
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync());
        
        var response = await client.GetAsync("/api/pos/shifts/00000000-0000-0000-0000-000000000000");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_shifts_returns_company_shifts_only()
    {
        var company1 = await Factory.CreateCompanyAsync();
        var company2 = await Factory.CreateCompanyAsync();
        
        var client1 = Factory.ClientFor(company1);
        var client2 = Factory.ClientFor(company2);
        
        // Open shifts for both companies
        await client1.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "John Doe", startingCashHalalas = 10000 });
        
        await client2.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "Jane Smith", startingCashHalalas = 5000 });
        
        // List shifts for company 1
        var response1 = await client1.GetAsync("/api/pos/shifts");
        var shifts1 = (await response1.Content.ReadFromJsonAsync<List<ShiftResponse>>())!;
        
        // Should only have 1 shift (the one from company 1)
        shifts1.Count.Should().Be(1);
        shifts1[0].CashierName.Should().Be("John Doe");
    }

    [Fact]
    public async Task List_shifts_filters_by_status_open()
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync());
        
        // Open two shifts
        await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "John Doe", startingCashHalalas = 10000 });
        
        var openResponse = await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "Jane Smith", startingCashHalalas = 5000 });
        
        var shift = (await openResponse.Content.ReadFromJsonAsync<ShiftResponse>())!;
        
        // Close one shift
        await client.PatchAsJsonAsync($"/api/pos/shifts/{shift.Id}/close",
            new { actualCashHalalas = 12000 });
        
        // List only open shifts
        var response = await client.GetAsync("/api/pos/shifts?status=open");
        var shifts = (await response.Content.ReadFromJsonAsync<List<ShiftResponse>>())!;
        
        // Should only have 1 open shift
        shifts.Count.Should().Be(1);
        shifts[0].CashierName.Should().Be("John Doe");
        shifts[0].ClosedAt.Should().BeNull();
    }

    [Fact]
    public async Task List_shifts_filters_by_status_closed()
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync());
        
        // Open two shifts
        await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "John Doe", startingCashHalalas = 10000 });
        
        var openResponse = await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "Jane Smith", startingCashHalalas = 5000 });
        
        var shift = (await openResponse.Content.ReadFromJsonAsync<ShiftResponse>())!;
        
        // Close one shift
        await client.PatchAsJsonAsync($"/api/pos/shifts/{shift.Id}/close",
            new { actualCashHalalas = 12000 });
        
        // List only closed shifts
        var response = await client.GetAsync("/api/pos/shifts?status=closed");
        var shifts = (await response.Content.ReadFromJsonAsync<List<ShiftResponse>>())!;
        
        // Should only have 1 closed shift
        shifts.Count.Should().Be(1);
        shifts[0].CashierName.Should().Be("Jane Smith");
        shifts[0].ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task List_shifts_ordered_newest_first()
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync());
        
        // Open two shifts with a small delay
        await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "John Doe", startingCashHalalas = 10000 });
        
        await Task.Delay(10); // Small delay to ensure different timestamps
        
        var openResponse = await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "Jane Smith", startingCashHalalas = 5000 });
        
        // List shifts
        var response = await client.GetAsync("/api/pos/shifts");
        var shifts = (await response.Content.ReadFromJsonAsync<List<ShiftResponse>>())!;
        
        // Should be ordered newest first
        shifts.Count.Should().Be(2);
        shifts[0].CashierName.Should().Be("Jane Smith"); // Newer shift comes first
        shifts[1].CashierName.Should().Be("John Doe");
    }

    [Fact]
    public async Task Viewer_cannot_access_shift_endpoints()
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync(), Roles.Viewer);
        
        // Try to open a shift
        var response = await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "John Doe", startingCashHalalas = 10000 });
        
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Cross_tenant_shift_returns_404()
    {
        var company1 = await Factory.CreateCompanyAsync();
        var company2 = await Factory.CreateCompanyAsync();
        
        var client1 = Factory.ClientFor(company1);
        var client2 = Factory.ClientFor(company2);
        
        // Open a shift for company 1
        var openResponse = await client1.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "John Doe", startingCashHalalas = 10000 });
        
        var shift = (await openResponse.Content.ReadFromJsonAsync<ShiftResponse>())!;
        
        // Try to access the shift from company 2
        var response = await client2.GetAsync($"/api/pos/shifts/{shift.Id}");
        
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
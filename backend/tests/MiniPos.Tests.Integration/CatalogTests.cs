using System.Net;
using System.Net.Http.Json;
using MiniPos.Api.Features.Catalog.CreateProduct;

namespace MiniPos.Tests.Integration;

public class CatalogTests(PostgresFixture db) : ApiTestBase(db)
{
    [Fact]
    public async Task Admin_creates_and_lists_products()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var created = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 5000, 10));

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var list = await client.GetFromJsonAsync<List<ProductResponse>>("/api/inventory/products");
        list!.Select(p => p.Sku).Should().Equal("SHIRT-01");
    }

    [Fact]
    public async Task Companies_never_see_each_others_products()
    {
        var a = await Factory.CreateCompanyAsync("A", "أ");
        var b = await Factory.CreateCompanyAsync("B", "ب");
        await Factory.ClientFor(a).PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Hat", "قبعة", "HAT-01", 2000, 5));

        var seenByB = await Factory.ClientFor(b).GetFromJsonAsync<List<ProductResponse>>("/api/inventory/products");

        seenByB.Should().BeEmpty();
    }

    [Fact]
    public async Task Viewer_cannot_create_products()
    {
        var company = await Factory.CreateCompanyAsync();
        var response = await Factory.ClientFor(company, "Viewer").PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-02", 5000, 10));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Anonymous_requests_are_rejected()
    {
        var response = await Factory.CreateClient().GetAsync(new Uri("/api/inventory/products", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Duplicate_sku_is_a_conflict()
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync());
        var command = new CreateProductCommand("Shirt", "قميص", "SHIRT-03", 5000, 10);
        await client.PostAsJsonAsync("/api/inventory/products", command);
        (await client.PostAsJsonAsync("/api/inventory/products", command)).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}

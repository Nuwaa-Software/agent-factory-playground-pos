using System.Net;
using System.Net.Http.Json;
using MiniPos.Api.Features.Catalog.CreateProduct;
using MiniPos.Api.Features.Sales.CreateSale;
using MiniPos.Api.Features.Sales.GetSale;
using MiniPos.Domain.Sales;
using System.Text.Json;

namespace MiniPos.Tests.Integration;

public class VoidSaleReasonTests(PostgresFixture db) : ApiTestBase(db)
{
    private async Task<(HttpClient Client, ProductResponse Product)> ShopWithProductAsync(long price = 1000, int stock = 10)
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync());
        var response = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", $"SKU-{Guid.NewGuid():N}"[..12].ToUpperInvariant(), price, stock));
        return (client, (await response.Content.ReadFromJsonAsync<ProductResponse>())!);
    }

    [Fact]
    public async Task Void_with_valid_reason_persists_and_reads_back()
    {
        var (client, product) = await ShopWithProductAsync();
        var created = await (await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new CreateSaleLine(product.Id, 1)]))).Content.ReadFromJsonAsync<SaleResponse>();

        var response = await client.PatchAsJsonAsync($"/api/pos/sales/{created!.Id}/void",
            new { reason = "Customer returned item" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var voidedSale = await response.Content.ReadFromJsonAsync<SaleResponse>();
        voidedSale!.VoidReason.Should().Be("Customer returned item");

        // Verify GET also returns the reason
        var getResponse = await client.GetFromJsonAsync<SaleResponse>($"/api/pos/sales/{created.Id}");
        getResponse!.VoidReason.Should().Be("Customer returned item");
    }

    [Fact]
    public async Task Void_with_null_body_still_works()
    {
        var (client, product) = await ShopWithProductAsync();
        var created = await (await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new CreateSaleLine(product.Id, 1)]))).Content.ReadFromJsonAsync<SaleResponse>();

        var response = await client.PatchAsync($"/api/pos/sales/{created!.Id}/void", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var voidedSale = await response.Content.ReadFromJsonAsync<SaleResponse>();
        voidedSale!.VoidReason.Should().BeNull();
    }

    [Fact]
    public async Task Void_with_reason_null_in_body_sets_void_reason_to_null()
    {
        var (client, product) = await ShopWithProductAsync();
        var created = await (await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new CreateSaleLine(product.Id, 1)]))).Content.ReadFromJsonAsync<SaleResponse>();

        var response = await client.PatchAsJsonAsync($"/api/pos/sales/{created!.Id}/void",
            new { reason = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var voidedSale = await response.Content.ReadFromJsonAsync<SaleResponse>();
        voidedSale!.VoidReason.Should().BeNull();
    }

    [Fact]
    public async Task Void_with_reason_omitted_sets_void_reason_to_null()
    {
        var (client, product) = await ShopWithProductAsync();
        var created = await (await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new CreateSaleLine(product.Id, 1)]))).Content.ReadFromJsonAsync<SaleResponse>();

        // Send an empty JSON object — reason is omitted
        var response = await client.PatchAsJsonAsync($"/api/pos/sales/{created!.Id}/void",
            new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var voidedSale = await response.Content.ReadFromJsonAsync<SaleResponse>();
        voidedSale!.VoidReason.Should().BeNull();
    }

    [Fact]
    public async Task Void_with_reason_over_500_chars_returns_400()
    {
        var (client, product) = await ShopWithProductAsync();
        var created = await (await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new CreateSaleLine(product.Id, 1)]))).Content.ReadFromJsonAsync<SaleResponse>();

        var longReason = new string('a', 501);
        var response = await client.PatchAsJsonAsync($"/api/pos/sales/{created!.Id}/void",
            new { reason = longReason });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Void_with_reason_exactly_500_chars_succeeds()
    {
        var (client, product) = await ShopWithProductAsync();
        var created = await (await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new CreateSaleLine(product.Id, 1)]))).Content.ReadFromJsonAsync<SaleResponse>();

        var exactReason = new string('a', 500);
        var response = await client.PatchAsJsonAsync($"/api/pos/sales/{created!.Id}/void",
            new { reason = exactReason });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var voidedSale = await response.Content.ReadFromJsonAsync<SaleResponse>();
        voidedSale!.VoidReason.Should().Be(exactReason);
    }

    [Fact]
    public async Task Void_with_control_character_returns_400()
    {
        var (client, product) = await ShopWithProductAsync();
        var created = await (await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new CreateSaleLine(product.Id, 1)]))).Content.ReadFromJsonAsync<SaleResponse>();

        var reasonWithControl = "Hello\u0000World";
        var response = await client.PatchAsJsonAsync($"/api/pos/sales/{created!.Id}/void",
            new { reason = reasonWithControl });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Void_with_bidi_control_character_returns_400()
    {
        var (client, product) = await ShopWithProductAsync();
        var created = await (await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new CreateSaleLine(product.Id, 1)]))).Content.ReadFromJsonAsync<SaleResponse>();

        var reasonWithBidi = "Hello\u202EWorld";
        var response = await client.PatchAsJsonAsync($"/api/pos/sales/{created!.Id}/void",
            new { reason = reasonWithBidi });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Idempotent_void_keeps_first_reason()
    {
        var (client, product) = await ShopWithProductAsync();
        var created = await (await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new CreateSaleLine(product.Id, 1)]))).Content.ReadFromJsonAsync<SaleResponse>();

        // First void with reason A
        var response1 = await client.PatchAsJsonAsync($"/api/pos/sales/{created!.Id}/void",
            new { reason = "Reason A" });
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        var voidedSale1 = await response1.Content.ReadFromJsonAsync<SaleResponse>();
        voidedSale1!.VoidReason.Should().Be("Reason A");

        // Second void with reason B — should keep first reason
        var response2 = await client.PatchAsJsonAsync($"/api/pos/sales/{created.Id}/void",
            new { reason = "Reason B" });
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        var voidedSale2 = await response2.Content.ReadFromJsonAsync<SaleResponse>();
        voidedSale2!.VoidReason.Should().Be("Reason A");

        // Verify via GET as well
        var getResponse = await client.GetFromJsonAsync<SaleResponse>($"/api/pos/sales/{created.Id}");
        getResponse!.VoidReason.Should().Be("Reason A");
    }
}

using System.Net;
using System.Net.Http.Json;
using MiniPos.Api.Features.Catalog.CreateProduct;
using MiniPos.Api.Features.Sales.CreateSale;
using MiniPos.Api.Features.Sales.DiscountLimits;
using MiniPos.Api.Features.Sales.GetReceipt;
using MiniPos.Api.Features.Sales.GetSale;
using MiniPos.Domain.Sales;

namespace MiniPos.Tests.Integration;

public class DiscountIntegrationTests(PostgresFixture db) : ApiTestBase(db)
{
    private async Task<(HttpClient Client, ProductResponse Product)> ShopWithProductAsync(long price = 1000, int stock = 10)
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync());
        var response = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", $"SKU-{Guid.NewGuid():N}"[..12].ToUpperInvariant(), price, stock));
        return (client, (await response.Content.ReadFromJsonAsync<ProductResponse>())!);
    }

    // ── Authorization: non-admin gets 403 when discount is present ──────────

    [Fact]
    public async Task Cashier_with_discount_gets_403()
    {
        var (client, product) = await ShopWithProductAsync();
        var cashierClient = Factory.ClientFor(await Factory.CreateCompanyAsync(), "Cashier");

        var response = await cashierClient.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand(
                [new(product.Id, 1, new LineDiscountRequest(DiscountType.Percent, 10, "damaged"))]));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Viewer_with_discount_gets_403()
    {
        var (client, product) = await ShopWithProductAsync();
        var viewerClient = Factory.ClientFor(await Factory.CreateCompanyAsync(), "Viewer");

        var response = await viewerClient.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand(
                [new(product.Id, 1, new LineDiscountRequest(DiscountType.Percent, 10, "damaged"))]));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_with_discount_succeeds()
    {
        var (client, product) = await ShopWithProductAsync();

        var response = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand(
                [new(product.Id, 1, new LineDiscountRequest(DiscountType.Percent, 10, "damaged"))]));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ── Line discount: percentage ────────────────────────────────────────────

    [Fact]
    public async Task Line_percent_discount_applied_correctly()
    {
        var (client, product) = await ShopWithProductAsync(price: 1000);

        var response = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand(
                [new(product.Id, 2, new LineDiscountRequest(DiscountType.Percent, 10, "damaged"))]));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var sale = (await response.Content.ReadFromJsonAsync<SaleResponse>())!;
        sale.Lines.Should().ContainSingle();
        sale.Lines[0].DiscountHalalas.Should().Be(200); // 10% of 2000
        sale.Lines[0].DiscountReason.Should().Be("damaged");
        sale.NetHalalas.Should().Be(1800);
        sale.VatHalalas.Should().Be(270); // 15% of 1800
        sale.TotalHalalas.Should().Be(2070);
    }

    [Fact]
    public async Task Line_fixed_discount_applied_correctly()
    {
        var (client, product) = await ShopWithProductAsync(price: 1000);

        var response = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand(
                [new(product.Id, 2, new LineDiscountRequest(DiscountType.Fixed, 300, "promo"))]));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var sale = (await response.Content.ReadFromJsonAsync<SaleResponse>())!;
        sale.Lines[0].DiscountHalalas.Should().Be(300);
        sale.NetHalalas.Should().Be(1700);
    }

    // ── Sale-level discount ─────────────────────────────────────────────────

    [Fact]
    public async Task Sale_percent_discount_applied_correctly()
    {
        var (client, product) = await ShopWithProductAsync(price: 1000);

        var response = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand(
                [new(product.Id, 2)],
                SaleDiscount: new SaleDiscountRequest(DiscountType.Percent, 5, "loyal")));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var sale = (await response.Content.ReadFromJsonAsync<SaleResponse>())!;
        sale.SaleDiscountHalalas.Should().Be(100); // 5% of 2000
        sale.SaleDiscountReason.Should().Be("loyal");
        sale.NetHalalas.Should().Be(1900);
        sale.VatHalalas.Should().Be(285); // 15% of 1900
        sale.TotalHalalas.Should().Be(2185);
    }

    [Fact]
    public async Task Sale_fixed_discount_applied_correctly()
    {
        var (client, product) = await ShopWithProductAsync(price: 1000);

        var response = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand(
                [new(product.Id, 2)],
                SaleDiscount: new SaleDiscountRequest(DiscountType.Fixed, 500, "promo")));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var sale = (await response.Content.ReadFromJsonAsync<SaleResponse>())!;
        sale.SaleDiscountHalalas.Should().Be(500);
        sale.NetHalalas.Should().Be(1500);
    }

    // ── Stacked discounts ───────────────────────────────────────────────────

    [Fact]
    public async Task Line_and_sale_discounts_stacked()
    {
        var (client, product) = await ShopWithProductAsync(price: 1000);

        var response = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand(
                [new(product.Id, 2, new LineDiscountRequest(DiscountType.Percent, 10, "damaged"))],
                SaleDiscount: new SaleDiscountRequest(DiscountType.Percent, 5, "loyal")));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var sale = (await response.Content.ReadFromJsonAsync<SaleResponse>())!;
        // Line discount: 10% of 2000 = 200 → line net = 1800
        sale.Lines[0].DiscountHalalas.Should().Be(200);
        // Sale discount: 5% of 1800 = 90
        sale.SaleDiscountHalalas.Should().Be(90);
        // Net: 1800 - 90 = 1710
        sale.NetHalalas.Should().Be(1710);
        sale.VatHalalas.Should().Be(257); // 15% of 1710 = 256.5 → 257 (half-up)
        sale.TotalHalalas.Should().Be(1967);
    }

    // ── Floor at zero ───────────────────────────────────────────────────────

    [Fact]
    public async Task Line_discount_clamped_to_zero_net()
    {
        var (client, product) = await ShopWithProductAsync(price: 100);

        var response = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand(
                [new(product.Id, 1, new LineDiscountRequest(DiscountType.Percent, 100, "free"))]));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var sale = (await response.Content.ReadFromJsonAsync<SaleResponse>())!;
        sale.Lines[0].LineNetHalalas.Should().Be(0);
        sale.NetHalalas.Should().Be(0);
        sale.VatHalalas.Should().Be(0);
        sale.TotalHalalas.Should().Be(0);
    }

    [Fact]
    public async Task Sale_discount_clamped_to_zero_net()
    {
        var (client, product) = await ShopWithProductAsync(price: 100);

        var response = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand(
                [new(product.Id, 1)],
                SaleDiscount: new SaleDiscountRequest(DiscountType.Fixed, 9999, "free")));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var sale = (await response.Content.ReadFromJsonAsync<SaleResponse>())!;
        // Line net = 100, sale discount clamped to 100 → net = 0
        sale.SaleDiscountHalalas.Should().Be(100);
        sale.NetHalalas.Should().Be(0);
    }

    [Fact]
    public async Task Stacked_discounts_net_never_negative()
    {
        var (client, product) = await ShopWithProductAsync(price: 500);

        // Line discount 100% → line net = 0; sale discount should clamp to 0
        var response = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand(
                [new(product.Id, 1, new LineDiscountRequest(DiscountType.Percent, 100, "free"))],
                SaleDiscount: new SaleDiscountRequest(DiscountType.Fixed, 500, "extra")));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var sale = (await response.Content.ReadFromJsonAsync<SaleResponse>())!;
        sale.NetHalalas.Should().Be(0);
    }

    // ── Limits: config round-trip ───────────────────────────────────────────

    [Fact]
    public async Task Discount_limits_get_returns_null_when_not_configured()
    {
        var companyId = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(companyId);

        var response = await client.GetAsync("/api/pos/discount-limits");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var limits = (await response.Content.ReadFromJsonAsync<DiscountLimitsResponse>())!;
        limits.MaxPercent.Should().BeNull();
        limits.MaxFixedHalalas.Should().BeNull();
    }

    [Fact]
    public async Task Discount_limits_put_round_trips()
    {
        var companyId = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(companyId);

        // PUT both limits
        var putResponse = await client.PutAsJsonAsync("/api/pos/discount-limits",
            new { MaxPercent = 50, MaxFixedHalalas = 1000 });
        putResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var response = (await putResponse.Content.ReadFromJsonAsync<DiscountLimitsResponse>())!;
        response.MaxPercent.Should().Be(50);
        response.MaxFixedHalalas.Should().Be(1000);

        // GET to verify persistence
        var getResponse = await client.GetAsync("/api/pos/discount-limits");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var limits = (await getResponse.Content.ReadFromJsonAsync<DiscountLimitsResponse>())!;
        limits.MaxPercent.Should().Be(50);
        limits.MaxFixedHalalas.Should().Be(1000);
    }

    [Fact]
    public async Task Discount_limits_put_clears_limit()
    {
        var companyId = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(companyId);

        // Set a limit
        await client.PutAsJsonAsync("/api/pos/discount-limits",
            new { MaxPercent = 50, MaxFixedHalalas = (long?)null });
        var getResponse = await client.GetAsync("/api/pos/discount-limits");
        var limits = (await getResponse.Content.ReadFromJsonAsync<DiscountLimitsResponse>())!;
        limits.MaxPercent.Should().Be(50);

        // Clear it
        await client.PutAsJsonAsync("/api/pos/discount-limits",
            new { MaxPercent = (int?)null, MaxFixedHalalas = (long?)null });
        getResponse = await client.GetAsync("/api/pos/discount-limits");
        limits = (await getResponse.Content.ReadFromJsonAsync<DiscountLimitsResponse>())!;
        limits.MaxPercent.Should().BeNull();
    }

    [Fact]
    public async Task Discount_limits_non_admin_gets_403()
    {
        var companyId = await Factory.CreateCompanyAsync();
        var cashierClient = Factory.ClientFor(companyId, "Cashier");

        var response = await cashierClient.GetAsync("/api/pos/discount-limits");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Limits: enforcement ─────────────────────────────────────────────────

    [Fact]
    public async Task Exceeding_both_limits_returns_400()
    {
        var companyId = await Factory.CreateCompanyAsync();
        await Factory.SetCompanyConfigAsync(companyId, "Pos:MaxDiscountPercent", "50");
        await Factory.SetCompanyConfigAsync(companyId, "Pos:MaxDiscountFixedHalalas", "500");

        var client = Factory.ClientFor(companyId);
        var response = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", $"SKU-{Guid.NewGuid():N}"[..12].ToUpperInvariant(), 1000, 10));
        var product = (await response.Content.ReadFromJsonAsync<ProductResponse>())!;

        // 60% of 1000 = 600 halalas → exceeds max % (500) AND max fixed (500)
        var saleResponse = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand(
                [new(product.Id, 1, new LineDiscountRequest(DiscountType.Percent, 60, "too much"))]));

        saleResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Exceeding_one_limit_but_within_other_is_valid()
    {
        var companyId = await Factory.CreateCompanyAsync();
        await Factory.SetCompanyConfigAsync(companyId, "Pos:MaxDiscountPercent", "50");
        await Factory.SetCompanyConfigAsync(companyId, "Pos:MaxDiscountFixedHalalas", "700");

        var client = Factory.ClientFor(companyId);
        var response = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", $"SKU-{Guid.NewGuid():N}"[..12].ToUpperInvariant(), 1000, 10));
        var product = (await response.Content.ReadFromJsonAsync<ProductResponse>())!;

        // 60% of 1000 = 600 halalas → exceeds max % (500) but within max fixed (700)
        var saleResponse = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand(
                [new(product.Id, 1, new LineDiscountRequest(DiscountType.Percent, 60, "within fixed"))]));

        saleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var sale = (await saleResponse.Content.ReadFromJsonAsync<SaleResponse>())!;
        sale.Lines[0].DiscountHalalas.Should().Be(600); // Not clamped — within fixed limit
    }

    [Fact]
    public async Task No_limits_allows_full_discount()
    {
        var companyId = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(companyId);
        var response = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", $"SKU-{Guid.NewGuid():N}"[..12].ToUpperInvariant(), 1000, 10));
        var product = (await response.Content.ReadFromJsonAsync<ProductResponse>())!;

        var saleResponse = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand(
                [new(product.Id, 1, new LineDiscountRequest(DiscountType.Percent, 100, "free"))]));

        saleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var sale = (await saleResponse.Content.ReadFromJsonAsync<SaleResponse>())!;
        sale.Lines[0].DiscountHalalas.Should().Be(1000); // Full amount allowed
        sale.NetHalalas.Should().Be(0);
    }

    [Fact]
    public async Task Max_percent_limit_clamps_discount()
    {
        var companyId = await Factory.CreateCompanyAsync();
        await Factory.SetCompanyConfigAsync(companyId, "Pos:MaxDiscountPercent", "50");

        var client = Factory.ClientFor(companyId);
        var response = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", $"SKU-{Guid.NewGuid():N}"[..12].ToUpperInvariant(), 1000, 10));
        var product = (await response.Content.ReadFromJsonAsync<ProductResponse>())!;

        // Request 60% → clamped to 50%
        var saleResponse = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand(
                [new(product.Id, 1, new LineDiscountRequest(DiscountType.Percent, 60, "over limit"))]));

        saleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var sale = (await saleResponse.Content.ReadFromJsonAsync<SaleResponse>())!;
        sale.Lines[0].DiscountHalalas.Should().Be(500); // Clamped to 50%
    }

    [Fact]
    public async Task Max_fixed_limit_clamps_discount()
    {
        var companyId = await Factory.CreateCompanyAsync();
        await Factory.SetCompanyConfigAsync(companyId, "Pos:MaxDiscountFixedHalalas", "300");

        var client = Factory.ClientFor(companyId);
        var response = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", $"SKU-{Guid.NewGuid():N}"[..12].ToUpperInvariant(), 1000, 10));
        var product = (await response.Content.ReadFromJsonAsync<ProductResponse>())!;

        // Request 500 fixed → clamped to 300
        var saleResponse = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand(
                [new(product.Id, 1, new LineDiscountRequest(DiscountType.Fixed, 500, "over limit"))]));

        saleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var sale = (await saleResponse.Content.ReadFromJsonAsync<SaleResponse>())!;
        sale.Lines[0].DiscountHalalas.Should().Be(300); // Clamped to max fixed
    }

    // ── Reason mandatory ────────────────────────────────────────────────────

    [Fact]
    public async Task Empty_discount_reason_returns_400()
    {
        var (client, product) = await ShopWithProductAsync();

        var response = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand(
                [new(product.Id, 1, new LineDiscountRequest(DiscountType.Percent, 10, ""))]));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Reason stored and returned ──────────────────────────────────────────

    [Fact]
    public async Task Discount_reason_stored_and_returned_in_get_sale()
    {
        var (client, product) = await ShopWithProductAsync();

        var createResponse = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand(
                [new(product.Id, 1, new LineDiscountRequest(DiscountType.Percent, 10, "damaged goods"))],
                SaleDiscount: new SaleDiscountRequest(DiscountType.Fixed, 50, "loyalty")));

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var sale = (await createResponse.Content.ReadFromJsonAsync<SaleResponse>())!;

        // GetSale should return the same data
        var getResponse = await client.GetAsync($"/api/pos/sales/{sale.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrieved = (await getResponse.Content.ReadFromJsonAsync<SaleResponse>())!;

        retrieved.Lines[0].DiscountReason.Should().Be("damaged goods");
        retrieved.SaleDiscountReason.Should().Be("loyalty");
    }

    // ── Receipt shows discounts ─────────────────────────────────────────────

    [Fact]
    public async Task Receipt_shows_line_discount()
    {
        var (client, product) = await ShopWithProductAsync(price: 1000);

        var createResponse = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand(
                [new(product.Id, 2, new LineDiscountRequest(DiscountType.Percent, 10, "damaged"))]));

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var sale = (await createResponse.Content.ReadFromJsonAsync<SaleResponse>())!;

        var receiptResponse = await client.GetAsync($"/api/pos/sales/{sale.Id}/receipt");
        receiptResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var receipt = await receiptResponse.Content.ReadAsStringAsync();

        receipt.Should().Contain("Discount (damaged): -2.00");
    }

    [Fact]
    public async Task Receipt_shows_sale_discount()
    {
        var (client, product) = await ShopWithProductAsync(price: 1000);

        var createResponse = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand(
                [new(product.Id, 2)],
                SaleDiscount: new SaleDiscountRequest(DiscountType.Percent, 5, "loyal")));

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var sale = (await createResponse.Content.ReadFromJsonAsync<SaleResponse>())!;

        var receiptResponse = await client.GetAsync($"/api/pos/sales/{sale.Id}/receipt");
        receiptResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var receipt = await receiptResponse.Content.ReadAsStringAsync();

        receipt.Should().Contain("Sale Discount (loyal): -1.00");
    }

    [Fact]
    public async Task Receipt_shows_vat_on_discounted_amount()
    {
        var (client, product) = await ShopWithProductAsync(price: 1000);

        var createResponse = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand(
                [new(product.Id, 2, new LineDiscountRequest(DiscountType.Percent, 10, "damaged"))],
                SaleDiscount: new SaleDiscountRequest(DiscountType.Fixed, 100, "promo")));

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var sale = (await createResponse.Content.ReadFromJsonAsync<SaleResponse>())!;

        // Net = 2000 - 200 (line) - 100 (sale) = 1700
        // VAT = 15% of 1700 = 255
        var receiptResponse = await client.GetAsync($"/api/pos/sales/{sale.Id}/receipt");
        var receipt = await receiptResponse.Content.ReadAsStringAsync();

        receipt.Should().Contain("VAT 15%: 2.55");
    }

    // ── Backward compatibility ──────────────────────────────────────────────

    [Fact]
    public async Task Sale_without_discount_unchanged()
    {
        var (client, product) = await ShopWithProductAsync(price: 1000);

        var response = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new(product.Id, 2)]));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var sale = (await response.Content.ReadFromJsonAsync<SaleResponse>())!;
        sale.NetHalalas.Should().Be(2000);
        sale.VatHalalas.Should().Be(300);
        sale.TotalHalalas.Should().Be(2300);
        sale.SaleDiscountHalalas.Should().Be(0);
        sale.SaleDiscountReason.Should().BeNull();
        sale.Lines[0].DiscountHalalas.Should().Be(0);
        sale.Lines[0].DiscountReason.Should().BeNull();
    }

    // ── Percent validation: must be ≤ 100 ───────────────────────────────────

    [Fact]
    public async Task Percent_discount_over_100_returns_400()
    {
        var (client, product) = await ShopWithProductAsync();

        var response = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand(
                [new(product.Id, 1, new LineDiscountRequest(DiscountType.Percent, 150, "too much"))]));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Multiple line discounts ─────────────────────────────────────────────

    [Fact]
    public async Task Multiple_lines_with_different_discounts()
    {
        var companyId = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(companyId);

        // Create two products in the same company
        var responseA = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", $"SKU-{Guid.NewGuid():N}"[..12].ToUpperInvariant(), 1000, 10));
        var productA = (await responseA.Content.ReadFromJsonAsync<ProductResponse>())!;

        var responseB = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Pants", "بنطال", $"SKU-{Guid.NewGuid():N}"[..12].ToUpperInvariant(), 500, 10));
        var productB = (await responseB.Content.ReadFromJsonAsync<ProductResponse>())!;

        var saleResponse = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([
                new(productA.Id, 2, new LineDiscountRequest(DiscountType.Percent, 10, "damaged")),
                new(productB.Id, 3, new LineDiscountRequest(DiscountType.Fixed, 150, "promo"))
            ]));

        saleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var sale = (await saleResponse.Content.ReadFromJsonAsync<SaleResponse>())!;

        // Product A: 2 * 1000 = 2000, 10% discount = 200 → net = 1800
        sale.Lines[0].DiscountHalalas.Should().Be(200);
        sale.Lines[0].LineNetHalalas.Should().Be(1800);

        // Product B: 3 * 500 = 1500, fixed discount = 150 → net = 1350
        sale.Lines[1].DiscountHalalas.Should().Be(150);
        sale.Lines[1].LineNetHalalas.Should().Be(1350);

        // Total: 1800 + 1350 = 3150, VAT = 473 (15% of 3150 = 472.5 → 473)
        sale.NetHalalas.Should().Be(3150);
        sale.VatHalalas.Should().Be(473);
        sale.TotalHalalas.Should().Be(3623);
    }
}

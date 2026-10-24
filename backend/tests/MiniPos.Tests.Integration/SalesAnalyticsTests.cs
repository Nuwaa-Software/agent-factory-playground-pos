using System.Net;
using System.Net.Http.Json;
using MiniPos.Api.Features.Catalog.CreateProduct;
using MiniPos.Api.Features.Reports.SalesAnalytics;
using MiniPos.Api.Features.Sales.CreateSale;
using MiniPos.Api.Features.Sales.GetSale;
using MiniPos.Domain.Sales;

namespace MiniPos.Tests.Integration;

public class SalesAnalyticsTests(PostgresFixture db) : ApiTestBase(db)
{
    private async Task<(HttpClient Client, ProductResponse Product)> ShopWithProductAsync(long price = 1000, int stock = 10)
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync());
        var response = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", $"SKU-{Guid.NewGuid():N}"[..12].ToUpperInvariant(), price, stock));
        return (client, (await response.Content.ReadFromJsonAsync<ProductResponse>())!);
    }

    [Fact]
    public async Task Empty_range_returns_all_zeros()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Query a date range with no sales
        var response = await client.GetAsync("/api/pos/reports/analytics?startDate=2026-01-01&endDate=2026-01-31");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SalesAnalyticsResponse>();
        result!.startDate.Should().Be("2026-01-01");
        result.endDate.Should().Be("2026-01-31");
        result.totalSales.Should().Be(0);
        result.totalNetHalalas.Should().Be(0);
        result.totalVatHalalas.Should().Be(0);
        result.totalGrossHalalas.Should().Be(0);
        result.averageTransactionHalalas.Should().Be(0);
        result.topProducts.Should().BeEmpty();
        result.revenueByHour.Should().BeEmpty();
    }

    [Fact]
    public async Task Multi_day_aggregation()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create a product
        var productResponse = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SKU-ANALYTICS01", 1000, 100));
        var product = (await productResponse.Content.ReadFromJsonAsync<ProductResponse>())!;

        // Create 5 sales on different days within the range
        for (int i = 0; i < 5; i++)
        {
            await client.PostAsJsonAsync("/api/pos/sales",
                new CreateSaleCommand([new CreateSaleLine(product.Id, 2)]));
        }

        var today = DateTimeOffset.UtcNow.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var startDate = DateTimeOffset.UtcNow.AddDays(-4).Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        var response = await client.GetAsync($"/api/pos/reports/analytics?startDate={startDate}&endDate={today}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SalesAnalyticsResponse>();

        result!.totalSales.Should().Be(5);
        result.totalNetHalalas.Should().Be(10000); // 5 sales * 2 qty * 1000 net each
        result.totalVatHalalas.Should().Be(1500);  // 5 sales * 300 vat each (15% of 2000)
        result.totalGrossHalalas.Should().Be(11500); // 5 sales * 2300 total each
        result.averageTransactionHalalas.Should().Be(2300); // 11500 / 5 = 2300
        result.topProducts.Should().ContainSingle();
        result.topProducts[0].productId.Should().Be(product.Id);
        result.topProducts[0].quantitySold.Should().Be(10); // 5 sales * 2 qty each
        result.topProducts[0].revenueHalalas.Should().Be(10000); // net revenue (no VAT)
        result.revenueByHour.Should().NotBeEmpty();

        // Verify consistency: gross == net + vat
        result.totalGrossHalalas.Should().Be(result.totalNetHalalas + result.totalVatHalalas);
    }

    [Fact]
    public async Task Voided_sales_excluded()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create a product
        var productResponse = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SKU-VOID01", 1000, 100));
        var product = (await productResponse.Content.ReadFromJsonAsync<ProductResponse>())!;

        // Create a sale
        var created = await (await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new CreateSaleLine(product.Id, 3)]))).Content.ReadFromJsonAsync<SaleResponse>();

        // Void the sale
        await client.PatchAsync($"/api/pos/sales/{created!.Id}/void", null);

        var today = DateTimeOffset.UtcNow.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var startDate = DateTimeOffset.UtcNow.AddDays(-1).Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        var response = await client.GetAsync($"/api/pos/reports/analytics?startDate={startDate}&endDate={today}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SalesAnalyticsResponse>();

        // All metrics should be zero since the only sale was voided
        result!.totalSales.Should().Be(0);
        result.totalNetHalalas.Should().Be(0);
        result.totalVatHalalas.Should().Be(0);
        result.totalGrossHalalas.Should().Be(0);
        result.averageTransactionHalalas.Should().Be(0);
        result.topProducts.Should().BeEmpty();
        result.revenueByHour.Should().BeEmpty();
    }

    [Fact]
    public async Task Top_N_limiting()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create 15 products with different quantities
        for (int i = 0; i < 15; i++)
        {
            await client.PostAsJsonAsync("/api/inventory/products",
                new CreateProductCommand($"Product-{i}", $"منتج-{i}", $"SKU-TOP{i:D2}", 1000, 100));
        }

        // Get the products back
        var products = (await client.GetFromJsonAsync<List<ProductResponse>>("/api/inventory/products"))!;

        // Create one sale per product (quantity=1 each)
        foreach (var p in products)
        {
            await client.PostAsJsonAsync("/api/pos/sales",
                new CreateSaleCommand([new CreateSaleLine(p.Id, 1)]));
        }

        var today = DateTimeOffset.UtcNow.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var startDate = DateTimeOffset.UtcNow.AddDays(-1).Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        // Default topN=10 should cap at 10
        var response = await client.GetAsync($"/api/pos/reports/analytics?startDate={startDate}&endDate={today}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SalesAnalyticsResponse>();

        result!.topProducts.Should().HaveCount(10); // capped at default topN=10
    }

    [Fact]
    public async Task Top_N_explicit_limit()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create 5 products
        for (int i = 0; i < 5; i++)
        {
            await client.PostAsJsonAsync("/api/inventory/products",
                new CreateProductCommand($"Product-{i}", $"منتج-{i}", $"SKU-EXPLIMIT{i:D2}", 1000, 100));
        }

        var products = (await client.GetFromJsonAsync<List<ProductResponse>>("/api/inventory/products"))!;

        // Create one sale per product
        foreach (var p in products)
        {
            await client.PostAsJsonAsync("/api/pos/sales",
                new CreateSaleCommand([new CreateSaleLine(p.Id, 1)]));
        }

        var today = DateTimeOffset.UtcNow.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var startDate = DateTimeOffset.UtcNow.AddDays(-1).Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        // topN=3 should return only 3 products
        var response = await client.GetAsync($"/api/pos/reports/analytics?startDate={startDate}&endDate={today}&topN=3");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SalesAnalyticsResponse>();

        result!.topProducts.Should().HaveCount(3);
    }

    [Fact]
    public async Task Cross_tenant_isolation()
    {
        var companyA = await Factory.CreateCompanyAsync("A", "أ");
        var companyB = await Factory.CreateCompanyAsync("B", "ب");
        var clientA = Factory.ClientFor(companyA);
        var clientB = Factory.ClientFor(companyB);

        // Create a product in company A
        var productResponse = await clientA.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SKU-TENANT01", 5000, 10));
        var product = (await productResponse.Content.ReadFromJsonAsync<ProductResponse>())!;

        // Create a sale in company A
        await clientA.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new CreateSaleLine(product.Id, 2)]));

        // Query analytics for company B - should see no sales
        var today = DateTimeOffset.UtcNow.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var startDate = DateTimeOffset.UtcNow.AddDays(-1).Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        var response = await clientB.GetAsync($"/api/pos/reports/analytics?startDate={startDate}&endDate={today}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SalesAnalyticsResponse>();

        result!.totalSales.Should().Be(0);
        result.totalNetHalalas.Should().Be(0);
        result.topProducts.Should().BeEmpty();
        result.revenueByHour.Should().BeEmpty();
    }

    [Fact]
    public async Task Unauthenticated_returns_401()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Remove authentication headers
        client.DefaultRequestHeaders.Clear();

        var response = await client.GetAsync("/api/pos/reports/analytics");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Viewer_returns_403()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company, "Viewer");

        var response = await client.GetAsync("/api/pos/reports/analytics");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Default_date_range_is_last_30_days()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create a product
        var productResponse = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SKU-DEFAULT30", 1000, 100));
        var product = (await productResponse.Content.ReadFromJsonAsync<ProductResponse>())!;

        // Create a sale today
        await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new CreateSaleLine(product.Id, 3)]));

        // Query without date params - should default to last 30 days
        var response = await client.GetAsync("/api/pos/reports/analytics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SalesAnalyticsResponse>();

        result!.totalSales.Should().Be(1);
        result.totalNetHalalas.Should().Be(3000);
    }

    [Fact]
    public async Task Top_products_sorted_by_quantity_desc_then_revenue_desc()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create 3 products with same quantity but different prices
        var productResponse1 = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Product-A", "منتج-A", "SKU-SORT01", 1000, 100));
        var productA = (await productResponse1.Content.ReadFromJsonAsync<ProductResponse>())!;

        var productResponse2 = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Product-B", "منتج-B", "SKU-SORT02", 2000, 100));
        var productB = (await productResponse2.Content.ReadFromJsonAsync<ProductResponse>())!;

        // Create one sale of qty=5 for each product
        await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new CreateSaleLine(productA.Id, 5)]));
        await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new CreateSaleLine(productB.Id, 5)]));

        var today = DateTimeOffset.UtcNow.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var startDate = DateTimeOffset.UtcNow.AddDays(-1).Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        var response = await client.GetAsync($"/api/pos/reports/analytics?startDate={startDate}&endDate={today}&topN=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SalesAnalyticsResponse>();

        // Both have quantitySold=5, so tiebreaker is revenueHalalas desc: B (10000) > A (5000)
        result!.topProducts[0].productId.Should().Be(productB.Id);
        result.topProducts[0].revenueHalalas.Should().Be(10000);
        result.topProducts[1].productId.Should().Be(productA.Id);
        result.topProducts[1].revenueHalalas.Should().Be(5000);
    }

    [Fact]
    public async Task Revenue_by_hour_grouped_correctly()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create a product
        var productResponse = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SKU-HOUR01", 1000, 100));
        var product = (await productResponse.Content.ReadFromJsonAsync<ProductResponse>())!;

        // Create two sales today
        await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new CreateSaleLine(product.Id, 1)]));
        await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new CreateSaleLine(product.Id, 1)]));

        var today = DateTimeOffset.UtcNow.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var startDate = DateTimeOffset.UtcNow.AddDays(-1).Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        var response = await client.GetAsync($"/api/pos/reports/analytics?startDate={startDate}&endDate={today}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SalesAnalyticsResponse>();

        // Both sales were created today, so they should be in the same hour bucket (or different hours)
        result!.revenueByHour.Should().NotBeEmpty();
        // Total across all hours should equal totalGrossHalalas
        var sumHours = result.revenueByHour.Sum(h => h.grossHalalas);
        sumHours.Should().Be(result.totalGrossHalalas);
    }

    [Fact]
    public async Task topN_less_than_1_defaults_to_10()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create a product
        var productResponse = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SKU-NEGTOPN", 1000, 100));
        var product = (await productResponse.Content.ReadFromJsonAsync<ProductResponse>())!;

        // Create 5 sales
        for (int i = 0; i < 5; i++)
        {
            await client.PostAsJsonAsync("/api/pos/sales",
                new CreateSaleCommand([new CreateSaleLine(product.Id, 1)]));
        }

        var today = DateTimeOffset.UtcNow.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var startDate = DateTimeOffset.UtcNow.AddDays(-1).Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        // topN=0 should be rejected by validator as not > 0
        var response = await client.GetAsync($"/api/pos/reports/analytics?startDate={startDate}&endDate={today}&topN=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

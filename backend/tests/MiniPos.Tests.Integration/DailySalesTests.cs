using System.Net;
using System.Net.Http.Json;
using MiniPos.Api.Features.Catalog.CreateProduct;
using MiniPos.Api.Features.Reports.DailySales;
using MiniPos.Api.Features.Sales.CreateSale;
using MiniPos.Api.Auth;

namespace MiniPos.Tests.Integration;
 
public class DailySalesTests(PostgresFixture db) : ApiTestBase(db)
{
    [Fact]
    public async Task Empty_day_returns_all_zeros()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);
        
        var response = await client.GetAsync("/api/pos/reports/daily-sales?date=2026-10-18");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DailySalesResponse>();
        result!.date.Should().Be("2026-10-18");
        result.saleCount.Should().Be(0);
        result.netTotal.Should().Be(0);
        result.vatTotal.Should().Be(0);
        result.grossTotal.Should().Be(0);
    }

    [Fact]
    public async Task Omitted_date_param_defaults_to_today()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);
        
        var response = await client.GetAsync("/api/pos/reports/daily-sales");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DailySalesResponse>();
        // Should match today's date (UTC)
        result!.date.Should().Be(DateTimeOffset.UtcNow.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        result.saleCount.Should().Be(0);
        result.netTotal.Should().Be(0);
        result.vatTotal.Should().Be(0);
        result.grossTotal.Should().Be(0);
    }

    [Fact]
    public async Task Multi_sale_aggregation()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);
        
        // Create a product
        var productResponse = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SKU-1234567890AB", 1000, 10));
        var product = (await productResponse.Content.ReadFromJsonAsync<ProductResponse>())!;
        
        // Create 5 sales on today's date
        for (int i = 0; i < 5; i++)
        {
            await client.PostAsJsonAsync("/api/pos/sales",
                new CreateSaleCommand([new CreateSaleLine(product.Id, 1)]));
        }
        
        var response = await client.GetAsync("/api/pos/reports/daily-sales");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DailySalesResponse>();
        result!.saleCount.Should().Be(5);
        result.netTotal.Should().Be(5000); // 5 sales * 1000 net each
        result.vatTotal.Should().Be(750);  // 5 sales * 150 vat each (15% of 1000)
        result.grossTotal.Should().Be(5750); // 5 sales * 1150 total each
        result.date.Should().Be(DateTimeOffset.UtcNow.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        
        // Verify the consistency check: grossTotal == netTotal + vatTotal
        result.grossTotal.Should().Be(result.netTotal + result.vatTotal);
    }

    [Fact]
    public async Task Cross_tenant_isolation()
    {
        var companyA = await Factory.CreateCompanyAsync();
        var companyB = await Factory.CreateCompanyAsync();
        var clientA = Factory.ClientFor(companyA);
        var clientB = Factory.ClientFor(companyB);
        
        // Create a product in Company A
        var productResponse = await clientA.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SKU-1234567890AB", 1000, 10));
        var product = (await productResponse.Content.ReadFromJsonAsync<ProductResponse>())!;
        
        // Create a sale in Company A
        await clientA.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new CreateSaleLine(product.Id, 1)]));
        
        // Query report for Company B - should see no sales
        var response = await clientB.GetAsync("/api/pos/reports/daily-sales?date=2026-10-18");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DailySalesResponse>();
        result!.saleCount.Should().Be(0);
        result.netTotal.Should().Be(0);
        result.vatTotal.Should().Be(0);
        result.grossTotal.Should().Be(0);
    }

    [Fact]
    public async Task Invalid_date_format_returns_400()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);
        
        // Test with invalid calendar date
        var response1 = await client.GetAsync("/api/pos/reports/daily-sales?date=2026-13-01");
        response1.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        // Test with invalid format (abc)
        var response2 = await client.GetAsync("/api/pos/reports/daily-sales?date=abc");
        response2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unauthenticated_request_returns_401()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);
        
        // Remove authentication headers
        client.DefaultRequestHeaders.Clear();
        
        var response = await client.GetAsync("/api/pos/reports/daily-sales");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Viewer_role_returns_403()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company, Roles.Viewer);
        
        var response = await client.GetAsync("/api/pos/reports/daily-sales");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
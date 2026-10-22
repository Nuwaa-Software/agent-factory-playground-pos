using System.Net;
using System.Net.Http.Json;
using MiniPos.Api.Features.Catalog.CreateProduct;
using MiniPos.Api.Features.Sales.CreateSale;
using MiniPos.Api.Features.Sales.ListSales;
using MiniPos.Api.Features.Sales.GetSale;
using System.Text.Json;

namespace MiniPos.Tests.Integration;

public class SalesListTests(PostgresFixture db) : ApiTestBase(db)
{
    [Fact]
    public async Task Empty_list_returns_200_with_totalCount_zero()
    {
        // Arrange
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);
        
        // Act
        var response = await client.GetAsync("/api/pos/sales");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        
        json.RootElement.GetProperty("totalCount").GetInt32().Should().Be(0);
        json.RootElement.GetProperty("sales").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Pagination_page_1_and_2_with_25_sales()
    {
        // Arrange
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);
        
        // Create a product first with sufficient stock
        var productResponse = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 1000, 50));
        productResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = (await productResponse.Content.ReadFromJsonAsync<Api.Features.Common.ProductResponse>())!;
        
        // Create 25 sales one by one to avoid concurrency issues
        for (int i = 0; i < 25; i++)
        {
            var saleResponse = await client.PostAsJsonAsync("/api/pos/sales",
                new CreateSaleCommand([new CreateSaleLine(product.Id, 1)]));
            saleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        }
        
        // Act - Get first page
        var response1 = await client.GetAsync("/api/pos/sales?page=1&pageSize=20");
        
        // Assert first page
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        var content1 = await response1.Content.ReadAsStringAsync();
        var json1 = JsonDocument.Parse(content1);
        
        json1.RootElement.GetProperty("totalCount").GetInt32().Should().Be(25);
        json1.RootElement.GetProperty("totalPages").GetInt32().Should().Be(2);
        json1.RootElement.GetProperty("page").GetInt32().Should().Be(1);
        json1.RootElement.GetProperty("pageSize").GetInt32().Should().Be(20);
        json1.RootElement.GetProperty("sales").GetArrayLength().Should().Be(20);
        
        // Act - Get second page
        var response2 = await client.GetAsync("/api/pos/sales?page=2&pageSize=20");
        
        // Assert second page
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        var content2 = await response2.Content.ReadAsStringAsync();
        var json2 = JsonDocument.Parse(content2);
        
        json2.RootElement.GetProperty("totalCount").GetInt32().Should().Be(25);
        json2.RootElement.GetProperty("totalPages").GetInt32().Should().Be(2);
        json2.RootElement.GetProperty("page").GetInt32().Should().Be(2);
        json2.RootElement.GetProperty("pageSize").GetInt32().Should().Be(20);
        json2.RootElement.GetProperty("sales").GetArrayLength().Should().Be(5);
    }

    [Fact]
    public async Task PageSize_capped_at_100()
    {
        // Arrange
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);
        
        // Create a product first
        var productResponse = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 1000, 10));
        productResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = (await productResponse.Content.ReadFromJsonAsync<Api.Features.Common.ProductResponse>())!;
        
        // Create 5 sales
        for (int i = 0; i < 5; i++)
        {
            var saleResponse = await client.PostAsJsonAsync("/api/pos/sales",
                new CreateSaleCommand([new CreateSaleLine(product.Id, 1)]));
            saleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        }
        
        // Act - Request with pageSize=200 (should be capped to 100)
        var response = await client.GetAsync("/api/pos/sales?page=1&pageSize=200");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        
        json.RootElement.GetProperty("totalCount").GetInt32().Should().Be(5);
        json.RootElement.GetProperty("totalPages").GetInt32().Should().Be(1);
        json.RootElement.GetProperty("page").GetInt32().Should().Be(1);
        json.RootElement.GetProperty("pageSize").GetInt32().Should().Be(100);
        json.RootElement.GetProperty("sales").GetArrayLength().Should().Be(5);
    }

    [Fact]
    public async Task Returns_401_without_auth()
    {
        // Arrange
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);
        
        // Remove authentication headers
        client.DefaultRequestHeaders.Clear();
        
        // Act
        var response = await client.GetAsync("/api/pos/sales");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Returns_403_for_Viewer()
    {
        // Arrange
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company, "Viewer");
        
        // Act
        var response = await client.GetAsync("/api/pos/sales");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Cross_tenant_isolation()
    {
        // Arrange
        var companyA = await Factory.CreateCompanyAsync("A", "أ");
        var companyB = await Factory.CreateCompanyAsync("B", "ب");
        var clientA = Factory.ClientFor(companyA);
        var clientB = Factory.ClientFor(companyB);
        
        // Create a product in company A
        var productResponse = await clientA.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 1000, 10));
        productResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = (await productResponse.Content.ReadFromJsonAsync<Api.Features.Common.ProductResponse>())!;
        
        // Create sales in company A
        for (int i = 0; i < 5; i++)
        {
            var saleResponse = await clientA.PostAsJsonAsync("/api/pos/sales",
                new CreateSaleCommand([new CreateSaleLine(product.Id, 1)]));
            saleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        }
        
        // Act - Query from company B
        var response = await clientB.GetAsync("/api/pos/sales");
        
        // Assert - Should return empty list (totalCount = 0)
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        
        json.RootElement.GetProperty("totalCount").GetInt32().Should().Be(0);
        json.RootElement.GetProperty("sales").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Default_30_day_range_returns_only_recent_sales()
    {
        // Arrange
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);
        
        // Create a product first
        var productResponse = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 1000, 10));
        productResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = (await productResponse.Content.ReadFromJsonAsync<Api.Features.Common.ProductResponse>())!;
        
        // Create a recent sale
        var recentSaleResponse = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new CreateSaleLine(product.Id, 1)]));
        recentSaleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        
        // Act - Get sales with default 30-day range (should return recent sale)
        var response = await client.GetAsync("/api/pos/sales");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        
        json.RootElement.GetProperty("totalCount").GetInt32().Should().Be(1);
        json.RootElement.GetProperty("sales").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task Explicit_from_to_filtering_returns_only_sales_in_range()
    {
        // Arrange
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);
        
        // Create a product first
        var productResponse = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 1000, 10));
        productResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = (await productResponse.Content.ReadFromJsonAsync<Api.Features.Common.ProductResponse>())!;
        
        // Create a few sales
        for (int i = 0; i < 3; i++)
        {
            var saleResponse = await client.PostAsJsonAsync("/api/pos/sales",
                new CreateSaleCommand([new CreateSaleLine(product.Id, 1)]));
            saleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        }
        
        // Act - Query with a narrow date range that should match all sales
        var from = DateTimeOffset.UtcNow.AddDays(-35);
        var to = DateTimeOffset.UtcNow.AddDays(-25);
        var response = await client.GetAsync($"/api/pos/sales?from={from:yyyy-MM-ddTHH:mm:ssZ}&to={to:yyyy-MM-ddTHH:mm:ssZ}");
        
        // Assert - Should return 0 sales since we're looking in the past
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        
        json.RootElement.GetProperty("totalCount").GetInt32().Should().Be(0);
        json.RootElement.GetProperty("sales").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Boundary_inclusion_works_correctly()
    {
        // Arrange
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);
        
        // Create a product first
        var productResponse = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 1000, 10));
        productResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = (await productResponse.Content.ReadFromJsonAsync<Api.Features.Common.ProductResponse>())!;
        
        // Create a few sales
        for (int i = 0; i < 2; i++)
        {
            var saleResponse = await client.PostAsJsonAsync("/api/pos/sales",
                new CreateSaleCommand([new CreateSaleLine(product.Id, 1)]));
            saleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        }
        
        // Act - Query with a broad date range that should match all sales
        var from = DateTimeOffset.UtcNow.AddDays(-35);
        var to = DateTimeOffset.UtcNow.AddDays(1);
        var response = await client.GetAsync($"/api/pos/sales?from={from:yyyy-MM-ddTHH:mm:ssZ}&to={to:yyyy-MM-ddTHH:mm:ssZ}");
        
        // Assert - Should return all sales
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        
        json.RootElement.GetProperty("totalCount").GetInt32().Should().Be(2);
        json.RootElement.GetProperty("sales").GetArrayLength().Should().Be(2);
    }
}
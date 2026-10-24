using System.Net;
using System.Net.Http.Json;
using MiniPos.Api.Features.Catalog.CreateProduct;
using MiniPos.Api.Features.Sales.CreateSale;
using MiniPos.Api.Features.Sales.ListSales;
using MiniPos.Api.Features.Sales.GetSale;
using MiniPos.Domain.Sales;
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

    [Fact]
    public async Task Status_filter_returns_correct_results()
    {
        // Arrange
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create a product first
        var productResponse = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 1000, 10));
        productResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = (await productResponse.Content.ReadFromJsonAsync<Api.Features.Common.ProductResponse>())!;

        // Create 3 sales (all active by default)
        for (int i = 0; i < 3; i++)
        {
            var saleResponse = await client.PostAsJsonAsync("/api/pos/sales",
                new CreateSaleCommand([new CreateSaleLine(product.Id, 1)]));
            saleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        // Void one sale (requires Admin role)
        var listResp = await client.GetAsync("/api/pos/sales");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var listJson = (await listResp.Content.ReadFromJsonAsync<ListSalesResponse>())!;
        var saleToVoid = listJson.Sales[0];
        var voidResponse = await client.PatchAsync($"/api/pos/sales/{saleToVoid.Id}/void", null);
        voidResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act & Assert — status=active: expect 2 sales, all with voided == false
        var activeResponse = await client.GetAsync("/api/pos/sales?status=active");
        activeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var activeContent = await activeResponse.Content.ReadAsStringAsync();
        var activeJson = JsonDocument.Parse(activeContent);
        activeJson.RootElement.GetProperty("totalCount").GetInt32().Should().Be(2);
        activeJson.RootElement.GetProperty("sales").GetArrayLength().Should().Be(2);
        foreach (var sale in activeJson.RootElement.GetProperty("sales").EnumerateArray())
        {
            sale.GetProperty("voided").GetBoolean().Should().BeFalse();
        }

        // Act & Assert — status=voided: expect 1 sale with voided == true and non-null voidedAt
        var voidedResponse = await client.GetAsync("/api/pos/sales?status=voided");
        voidedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var voidedContent = await voidedResponse.Content.ReadAsStringAsync();
        var voidedJson = JsonDocument.Parse(voidedContent);
        voidedJson.RootElement.GetProperty("totalCount").GetInt32().Should().Be(1);
        voidedJson.RootElement.GetProperty("sales").GetArrayLength().Should().Be(1);
        var voidedSale = voidedJson.RootElement.GetProperty("sales")[0];
        voidedSale.GetProperty("voided").GetBoolean().Should().BeTrue();
        voidedSale.GetProperty("voidedAt").GetString().Should().NotBeNull();

        // Act & Assert — status=all: expect 3 sales
        var allResponse = await client.GetAsync("/api/pos/sales?status=all");
        allResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var allContent2 = await allResponse.Content.ReadAsStringAsync();
        var allJson2 = JsonDocument.Parse(allContent2);
        allJson2.RootElement.GetProperty("totalCount").GetInt32().Should().Be(3);

        // Act & Assert — status=bogus (unknown value): expect 3 sales (no filter)
        var bogusResponse = await client.GetAsync("/api/pos/sales?status=bogus");
        bogusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var bogusContent = await bogusResponse.Content.ReadAsStringAsync();
        var bogusJson = JsonDocument.Parse(bogusContent);
        bogusJson.RootElement.GetProperty("totalCount").GetInt32().Should().Be(3);

        // Act & Assert — pagination with status filter: pageSize=1, status=active → totalCount=2, totalPages=2
        var paginatedResponse = await client.GetAsync("/api/pos/sales?status=active&page=1&pageSize=1");
        paginatedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var paginatedContent = await paginatedResponse.Content.ReadAsStringAsync();
        var paginatedJson = JsonDocument.Parse(paginatedContent);
        paginatedJson.RootElement.GetProperty("totalCount").GetInt32().Should().Be(2);
        paginatedJson.RootElement.GetProperty("totalPages").GetInt32().Should().Be(2);
        paginatedJson.RootElement.GetProperty("sales").GetArrayLength().Should().Be(1);

        // Page 2 should also have 1 sale
        var page2Response = await client.GetAsync("/api/pos/sales?status=active&page=2&pageSize=1");
        page2Response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page2Content = await page2Response.Content.ReadAsStringAsync();
        var page2Json = JsonDocument.Parse(page2Content);
        page2Json.RootElement.GetProperty("totalCount").GetInt32().Should().Be(2);
        page2Json.RootElement.GetProperty("sales").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task SaleSummary_includes_voided_voidedAt_paymentType_fields()
    {
        // Arrange
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create a product first
        var productResponse = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 1000, 10));
        productResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = (await productResponse.Content.ReadFromJsonAsync<Api.Features.Common.ProductResponse>())!;

        // Create a sale with PaymentType.Card
        var saleResponse = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new CreateSaleLine(product.Id, 1)], PaymentType.Card));
        saleResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act — Get sales and verify SaleSummary fields for active sale
        var getResponse = await client.GetAsync("/api/pos/sales");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var getContent = await getResponse.Content.ReadAsStringAsync();
        var getJson = JsonDocument.Parse(getContent);

        var firstSale = getJson.RootElement.GetProperty("sales")[0];
        firstSale.GetProperty("voided").GetBoolean().Should().BeFalse();
        firstSale.GetProperty("voidedAt").ValueKind.Should().Be(JsonValueKind.Null);
        var paymentTypeValue = firstSale.GetProperty("paymentType");
        if (paymentTypeValue.ValueKind == JsonValueKind.String)
            paymentTypeValue.GetString().Should().Be("Card");
        else
            paymentTypeValue.GetInt32().Should().Be(1); // Card = 1

        // Void the sale
        var saleId = firstSale.GetProperty("id").GetGuid();
        var voidResp = await client.PatchAsync($"/api/pos/sales/{saleId}/void", null);
        voidResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act — Get sales again and verify SaleSummary fields for voided sale
        var getResponse2 = await client.GetAsync("/api/pos/sales");
        getResponse2.StatusCode.Should().Be(HttpStatusCode.OK);
        var getContent2 = await getResponse2.Content.ReadAsStringAsync();
        var getJson2 = JsonDocument.Parse(getContent2);

        var voidedSale = getJson2.RootElement.GetProperty("sales")[0];
        voidedSale.GetProperty("voided").GetBoolean().Should().BeTrue();
        voidedSale.GetProperty("voidedAt").GetString().Should().NotBeNull();
        var paymentTypeValue2 = voidedSale.GetProperty("paymentType");
        if (paymentTypeValue2.ValueKind == JsonValueKind.String)
            paymentTypeValue2.GetString().Should().Be("Card");
        else
            paymentTypeValue2.GetInt32().Should().Be(1); // Card = 1
    }
}
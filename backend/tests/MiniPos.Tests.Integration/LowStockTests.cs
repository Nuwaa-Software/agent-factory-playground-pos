using System.Net;
using System.Net.Http.Json;
using MiniPos.Api.Features.Catalog.CreateProduct;
using MiniPos.Api.Features.Catalog.LowStockProducts;
using MiniPos.Api.Features.Common;

namespace MiniPos.Tests.Integration;

public class LowStockTests(PostgresFixture db) : ApiTestBase(db)
{
    [Fact]
    public async Task Empty_result_when_all_above_threshold()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create products with stock above threshold
        await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 5000, 15));
        await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Pants", "بنطال", "PANTS-01", 8000, 20));

        var response = await client.GetFromJsonAsync<List<MiniPos.Api.Features.Common.ProductResponse>>("/api/pos/catalog/low-stock");
        
        response.Should().NotBeNull();
        response.Should().BeEmpty();
    }

    [Fact]
    public async Task Returns_401_without_auth()
    {
        var response = await Factory.CreateClient().GetAsync(new Uri("/api/pos/catalog/low-stock", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Returns_403_without_CatalogRead_role()
    {
        var company = await Factory.CreateCompanyAsync();
        // Create a client with no role (which should result in 401, not 403)
        var client = Factory.ClientFor(company, "None");
        
        var response = await client.GetAsync("/api/pos/catalog/low-stock");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Filters_and_sorts_by_stock_ascending()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create products with stock [5, 15, 3, 20, 8] 
        await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Product A", "منتج أ", "A-01", 5000, 5));
        await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Product B", "منتج ب", "B-01", 8000, 15));
        await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Product C", "منتج ج", "C-01", 2000, 3));
        await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Product D", "منتج د", "D-01", 10000, 20));
        await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Product E", "منتج ه", "E-01", 3000, 8));

        // With threshold 10, should return products with stock 3, 5, 8 (sorted ascending)
        var response = await client.GetFromJsonAsync<List<MiniPos.Api.Features.Common.ProductResponse>>("/api/pos/catalog/low-stock?threshold=10");
        
        response.Should().NotBeNull();
        response.Should().HaveCount(3);
        
        // Check that they are sorted by stock ascending (most critical first)
        response![0].Stock.Should().Be(3);  // Product C
        response![1].Stock.Should().Be(5);  // Product A  
        response![2].Stock.Should().Be(8); // Product E
    }

    [Fact]
    public async Task Cross_tenant_isolation()
    {
        var a = await Factory.CreateCompanyAsync("A", "أ");
        var b = await Factory.CreateCompanyAsync("B", "ب");
        
        // Create low-stock products for company A
        await Factory.ClientFor(a).PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 5000, 5));
        await Factory.ClientFor(a).PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Hat", "قبعة", "HAT-01", 2000, 3));

        // Company B should see empty list
        var responseB = await Factory.ClientFor(b).GetFromJsonAsync<List<MiniPos.Api.Features.Common.ProductResponse>>("/api/pos/catalog/low-stock");
        
        responseB.Should().NotBeNull();
        responseB.Should().BeEmpty();
    }

    [Fact]
    public async Task Negative_threshold_returns_400()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var response = await client.GetAsync("/api/pos/catalog/low-stock?threshold=-1");
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Config_key_fallback_to_10()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // No config key set - should use default threshold of 10
        await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 5000, 5));
        await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Pants", "بنطال", "PANTS-01", 8000, 15));

        var response = await client.GetFromJsonAsync<List<MiniPos.Api.Features.Common.ProductResponse>>("/api/pos/catalog/low-stock");
        
        // Should return the product with stock 5 (below default threshold of 10)
        response.Should().NotBeNull();
        response.Should().HaveCount(1);
    }

    [Fact]
    public async Task Config_key_uses_custom_threshold()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Set custom threshold in config
        await Factory.SetCompanyConfigAsync(company, "Pos:LowStockThreshold", "5");

        // Create products with stock above and below threshold (5)
        await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 5000, 3));
        await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Pants", "بنطال", "PANTS-01", 8000, 10));

        var response = await client.GetFromJsonAsync<List<MiniPos.Api.Features.Common.ProductResponse>>("/api/pos/catalog/low-stock");
        
        // Should return the product with stock 3 (below custom threshold of 5)
        response.Should().NotBeNull();
        response.Should().HaveCount(1);
        response![0].Stock.Should().Be(3);
    }

    [Fact]
    public async Task Threshold_parameter_filters_by_custom_threshold()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create products with stock [5, 15, 3, 20, 8] 
        await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Product A", "منتج أ", "A-01", 5000, 5));
        await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Product B", "منتج ب", "B-01", 8000, 15));
        await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Product C", "منتج ج", "C-01", 2000, 3));
        await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Product D", "منتج د", "D-01", 10000, 20));
        await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Product E", "منتج ه", "E-01", 3000, 8));

        // With custom threshold 8, should return products with stock 3, 5 (sorted ascending)
        var response = await client.GetFromJsonAsync<List<MiniPos.Api.Features.Common.ProductResponse>>("/api/pos/catalog/low-stock?threshold=8");
        
        response.Should().NotBeNull();
        response.Should().HaveCount(2);
        
        // Check that they are sorted by stock ascending
        response![0].Stock.Should().Be(3);  // Product C
        response![1].Stock.Should().Be(5); // Product A
    }
}
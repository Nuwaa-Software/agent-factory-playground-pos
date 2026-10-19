using System.Net;
using System.Net.Http.Json;
using MiniPos.Api.Features.Catalog.CreateProduct;

namespace MiniPos.Tests.Integration;

public class CatalogUpdateTests(PostgresFixture db) : ApiTestBase(db)
{
    [Fact]
    public async Task Admin_can_update_product_price()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create a product
        var created = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 5000, 10));

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var productResponse = await created.Content.ReadFromJsonAsync<ProductResponse>();
        var productId = productResponse!.Id;

        // Update only the price
        var updateResponse = await client.PatchAsJsonAsync($"/api/inventory/products/{productId}",
            new { PriceHalalas = 6000 });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify the updated value is returned in the response
        var updatedProduct = await updateResponse.Content.ReadFromJsonAsync<ProductResponse>();
        updatedProduct!.PriceHalalas.Should().Be(6000);
    }

    [Fact]
    public async Task Admin_can_update_product_stock()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create a product
        var created = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 5000, 10));

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var productResponse = await created.Content.ReadFromJsonAsync<ProductResponse>();
        var productId = productResponse!.Id;

        // Update only the stock
        var updateResponse = await client.PatchAsJsonAsync($"/api/inventory/products/{productId}",
            new { Stock = 20 });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify the updated value is returned in the response
        var updatedProduct = await updateResponse.Content.ReadFromJsonAsync<ProductResponse>();
        updatedProduct!.Stock.Should().Be(20);
    }

    [Fact]
    public async Task Admin_can_update_both_price_and_stock()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create a product
        var created = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 5000, 10));

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var productResponse = await created.Content.ReadFromJsonAsync<ProductResponse>();
        var productId = productResponse!.Id;

        // Update both price and stock
        var updateResponse = await client.PatchAsJsonAsync($"/api/inventory/products/{productId}",
            new { PriceHalalas = 7000, Stock = 15 });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify the updated values are returned in the response
        var updatedProduct = await updateResponse.Content.ReadFromJsonAsync<ProductResponse>();
        updatedProduct!.PriceHalalas.Should().Be(7000);
        updatedProduct.Stock.Should().Be(15);
    }

    [Fact]
    public async Task Admin_cannot_update_product_from_other_company()
    {
        var companyA = await Factory.CreateCompanyAsync("Company A", "أ");
        var companyB = await Factory.CreateCompanyAsync("Company B", "ب");
        var clientA = Factory.ClientFor(companyA);
        var clientB = Factory.ClientFor(companyB);

        // Create a product in company A
        var created = await clientA.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 5000, 10));

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var productResponse = await created.Content.ReadFromJsonAsync<ProductResponse>();
        var productId = productResponse!.Id;

        // Try to update the product from company B (should get 404)
        var updateResponse = await clientB.PatchAsJsonAsync($"/api/inventory/products/{productId}",
            new { PriceHalalas = 6000 });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Cashier_cannot_update_products()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company, "Cashier");

        // Try to update a product (should get 403)
        var updateResponse = await client.PatchAsJsonAsync("/api/inventory/products/123e4567-e89b-12d3-a456-426614174000",
            new { PriceHalalas = 6000 });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Viewer_cannot_update_products()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company, "Viewer");

        // Try to update a product (should get 403)
        var updateResponse = await client.PatchAsJsonAsync("/api/inventory/products/123e4567-e89b-12d3-a456-426614174000",
            new { PriceHalalas = 6000 });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Updating_nonexistent_product_returns_404()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Try to update a non-existent product (should get 404)
        var updateResponse = await client.PatchAsJsonAsync("/api/inventory/products/123e4567-e89b-12d3-a456-426614174000",
            new { PriceHalalas = 6000 });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Updating_with_negative_price_returns_400()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create a product first
        var created = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 5000, 10));

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var productResponse = await created.Content.ReadFromJsonAsync<ProductResponse>();
        var productId = productResponse!.Id;

        // Try to update with negative price (should get 400)
        var updateResponse = await client.PatchAsJsonAsync($"/api/inventory/products/{productId}",
            new { PriceHalalas = -100 });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Updating_with_negative_stock_returns_400()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create a product first
        var created = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 5000, 10));

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var productResponse = await created.Content.ReadFromJsonAsync<ProductResponse>();
        var productId = productResponse!.Id;

        // Try to update with negative stock (should get 400)
        var updateResponse = await client.PatchAsJsonAsync($"/api/inventory/products/{productId}",
            new { Stock = -5 });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
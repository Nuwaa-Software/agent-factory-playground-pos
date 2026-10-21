using System.Net;
using System.Net.Http.Json;
using MiniPos.Api.Features.Catalog.CreateProduct;
using MiniPos.Api.Features.Sales.CreateSale;
using MiniPos.Api.Features.Sales.GetSale;
using System.Text.Json;

namespace MiniPos.Tests.Integration;

public class SalesConcurrencyTests(PostgresFixture db) : ApiTestBase(db)
{
    [Fact]
    public async Task Concurrent_create_sale_returns_201_and_409_never_500()
    {
        // Create a company and product with sufficient stock to ensure only sale number collision occurs
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);
        
        var response = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", $"SKU-{Guid.NewGuid():N}"[..12].ToUpperInvariant(), 1000, 10));
        var product = (await response.Content.ReadFromJsonAsync<ProductResponse>())!;

        // Fire two sale requests in parallel for the same company and product
        var task1 = client.PostAsJsonAsync("/api/pos/sales", new CreateSaleCommand([new CreateSaleLine(product.Id, 1)]));
        var task2 = client.PostAsJsonAsync("/api/pos/sales", new CreateSaleCommand([new CreateSaleLine(product.Id, 1)]));
        
        var responses = await Task.WhenAll(task1, task2);
        
        // Count the results
        var createdCount = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);
        var errorCount = responses.Count(r => (int)r.StatusCode >= 400 && (int)r.StatusCode < 500 && r.StatusCode != HttpStatusCode.Conflict && r.StatusCode != HttpStatusCode.Created);
        
        // Exactly one should succeed (201) and one should fail (409)
        // No request should return 500 (which would indicate an unhandled exception)
        createdCount.Should().Be(1);
        conflictCount.Should().Be(1);
        errorCount.Should().Be(0); // Should not have any 500 errors or other unexpected errors
        
        // Verify that the successful sale has number 1
        var createdResponse = responses.FirstOrDefault(r => r.StatusCode == HttpStatusCode.Created);
        if (createdResponse != null)
        {
            var sale = (await createdResponse.Content.ReadFromJsonAsync<SaleResponse>())!;
            sale.Number.Should().Be(1);
        }
    }
}
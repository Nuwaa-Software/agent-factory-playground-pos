using System.Net;
using System.Net.Http.Json;
using MiniPos.Api.Features.Catalog.CreateProduct;
using MiniPos.Api.Features.Sales.CreateSale;
using MiniPos.Api.Features.Sales.GetSale;

namespace MiniPos.Tests.Integration;

public class SalesTests(PostgresFixture db) : ApiTestBase(db)
{
    private async Task<(HttpClient Client, ProductResponse Product)> ShopWithProductAsync(long price = 1000, int stock = 10)
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync());
        var response = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", $"SKU-{Guid.NewGuid():N}"[..12].ToUpperInvariant(), price, stock));
        return (client, (await response.Content.ReadFromJsonAsync<ProductResponse>())!);
    }

    [Fact]
    public async Task Sale_computes_net_vat_and_total_and_reduces_stock()
    {
        var (client, product) = await ShopWithProductAsync(price: 1000, stock: 10);

        var response = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new CreateSaleLine(product.Id, 2)]));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var sale = (await response.Content.ReadFromJsonAsync<SaleResponse>())!;
        sale.NetHalalas.Should().Be(2000);
        sale.VatHalalas.Should().Be(300);
        sale.TotalHalalas.Should().Be(2300);
        sale.Number.Should().Be(1);
        var products = await client.GetFromJsonAsync<List<ProductResponse>>("/api/inventory/products");
        products!.Single().Stock.Should().Be(8);
    }

    [Fact]
    public async Task Selling_more_than_stock_is_a_conflict()
    {
        var (client, product) = await ShopWithProductAsync(stock: 1);
        var response = await client.PostAsJsonAsync("/api/pos/sales", new CreateSaleCommand([new CreateSaleLine(product.Id, 2)]));
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Cashier_can_read_own_sale()
    {
        var (client, product) = await ShopWithProductAsync();
        var created = await (await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new CreateSaleLine(product.Id, 1)]))).Content.ReadFromJsonAsync<SaleResponse>();

        var sale = await client.GetFromJsonAsync<SaleResponse>($"/api/pos/sales/{created!.Id}");

        sale!.Lines.Should().ContainSingle().Which.Quantity.Should().Be(1);
    }

    [Fact]
    public async Task Empty_sale_is_rejected()
    {
        var (client, _) = await ShopWithProductAsync();
        var response = await client.PostAsJsonAsync("/api/pos/sales", new CreateSaleCommand([]));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Cross_tenant_sale_read_returns_404()
    {
        // Create two companies
        var companyA = await Factory.CreateCompanyAsync("A", "أ");
        var companyB = await Factory.CreateCompanyAsync("B", "ب");
        
        // Create a product in company A
        var clientA = Factory.ClientFor(companyA);
        var productResponse = await clientA.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 5000, 10));
        productResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = (await productResponse.Content.ReadFromJsonAsync<ProductResponse>())!;
        
        // Create a sale in company A
        var saleResponse = await clientA.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new CreateSaleLine(product.Id, 1)]));
        saleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdSale = (await saleResponse.Content.ReadFromJsonAsync<SaleResponse>())!;
        
        // Company B tries to read the sale from company A - should return 404
        var responseB = await Factory.ClientFor(companyB).GetAsync($"/api/pos/sales/{createdSale.Id}");
        responseB.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        // Company A should be able to read its own sale
        var responseA = await clientA.GetAsync($"/api/pos/sales/{createdSale.Id}");
        responseA.IsSuccessStatusCode.Should().BeTrue();
    }
}

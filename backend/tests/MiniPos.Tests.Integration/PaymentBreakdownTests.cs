using System.Net;
using System.Net.Http.Json;
using MiniPos.Api.Features.Catalog.CreateProduct;
using MiniPos.Api.Features.Customers.CreateCustomer;
using MiniPos.Api.Features.Sales.CreateSale;
using MiniPos.Api.Features.Sales.GetSale;
using MiniPos.Api.Features.Sales.ListSales;
using MiniPos.Domain.Sales;

namespace MiniPos.Tests.Integration;

public class PaymentBreakdownTests(PostgresFixture db) : ApiTestBase(db)
{
    private async Task<(HttpClient Client, Guid ProductId)> ShopWithProductAsync(long price = 1000, int stock = 10)
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);
        var response = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", $"SKU-{Guid.NewGuid():N}"[..12].ToUpperInvariant(), price, stock));
        var product = (await response.Content.ReadFromJsonAsync<Api.Features.Common.ProductResponse>())!;
        return (client, product.Id);
    }

    // ── Mixed payment with valid breakdown → 201 ──────────────────────────

    [Fact]
    public async Task Mixed_payment_with_valid_breakdown_returns_201()
    {
        var (client, productId) = await ShopWithProductAsync(price: 1000, stock: 10);

        var command = new CreateSaleCommand(
            Lines: [new(productId, 2)],
            PaymentType: PaymentType.Mixed,
            PaymentBreakdown: [
                new(PaymentType.Cash, 1150),
                new(PaymentType.Card, 1150)
            ]);

        var response = await client.PostAsJsonAsync("/api/pos/sales", command);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, $"Response body: {body}");

        var sale = (await response.Content.ReadFromJsonAsync<SaleResponse>())!;
        sale.PaymentBreakdown.Should().HaveCount(2);
        (sale.PaymentBreakdown[0].AmountHalalas + sale.PaymentBreakdown[1].AmountHalalas).Should().Be(sale.TotalHalalas);
    }

    // ── Mixed payment without breakdown → 400 ─────────────────────────────

    [Fact]
    public async Task Mixed_payment_without_breakdown_returns_400()
    {
        var (client, productId) = await ShopWithProductAsync();

        var command = new CreateSaleCommand(
            Lines: [new(productId, 1)],
            PaymentType: PaymentType.Mixed);

        var response = await client.PostAsJsonAsync("/api/pos/sales", command);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Mixed payment with breakdown sum mismatch → 400 ───────────────────

    [Fact]
    public async Task Mixed_payment_with_breakdown_sum_mismatch_returns_400()
    {
        var (client, productId) = await ShopWithProductAsync(price: 1000, stock: 10);

        var command = new CreateSaleCommand(
            Lines: [new(productId, 2)],
            PaymentType: PaymentType.Mixed,
            PaymentBreakdown: [
                new(PaymentType.Cash, 1000),
                new(PaymentType.Card, 500)
            ]);

        var response = await client.PostAsJsonAsync("/api/pos/sales", command);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var detail = await response.Content.ReadAsStringAsync();
        detail.Should().Contain("sum");
    }

    // ── Non-mixed without breakdown → 201 with default single entry ───────

    [Fact]
    public async Task Cash_without_breakdown_defaults_to_single_entry()
    {
        var (client, productId) = await ShopWithProductAsync();

        var command = new CreateSaleCommand(
            Lines: [new(productId, 1)],
            PaymentType: PaymentType.Cash);

        var response = await client.PostAsJsonAsync("/api/pos/sales", command);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var sale = (await response.Content.ReadFromJsonAsync<SaleResponse>())!;
        sale.PaymentBreakdown.Should().HaveCount(1);
        sale.PaymentBreakdown[0].PaymentType.Should().Be(PaymentType.Cash);
        sale.PaymentBreakdown[0].AmountHalalas.Should().Be(sale.TotalHalalas);
    }

    // ── Non-mixed with valid breakdown → 201 ──────────────────────────────

    [Fact]
    public async Task Cash_with_valid_breakdown_returns_201()
    {
        var (client, productId) = await ShopWithProductAsync(price: 1000, stock: 10);

        var command = new CreateSaleCommand(
            Lines: [new(productId, 2)],
            PaymentType: PaymentType.Cash,
            PaymentBreakdown: [
                new(PaymentType.Cash, 1500),
                new(PaymentType.Card, 800)
            ]);

        var response = await client.PostAsJsonAsync("/api/pos/sales", command);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var sale = (await response.Content.ReadFromJsonAsync<SaleResponse>())!;
        sale.PaymentBreakdown.Should().HaveCount(2);
        sale.PaymentBreakdown.Sum(e => e.AmountHalalas).Should().Be(sale.TotalHalalas);
    }

    // ── Non-mixed with invalid breakdown → 400 ────────────────────────────

    [Fact]
    public async Task Cash_with_invalid_breakdown_returns_400()
    {
        var (client, productId) = await ShopWithProductAsync(price: 1000, stock: 10);

        var command = new CreateSaleCommand(
            Lines: [new(productId, 2)],
            PaymentType: PaymentType.Cash,
            PaymentBreakdown: [
                new(PaymentType.Cash, 1000),
                new(PaymentType.Card, 500)
            ]);

        var response = await client.PostAsJsonAsync("/api/pos/sales", command);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Breakdown entry with type "Mixed" is accepted ─────────────────────

    [Fact]
    public async Task Breakdown_entry_with_type_Mixed_is_accepted()
    {
        var (client, productId) = await ShopWithProductAsync(price: 1000, stock: 10);

        var command = new CreateSaleCommand(
            Lines: [new(productId, 2)],
            PaymentType: PaymentType.Cash,
            PaymentBreakdown: [
                new(PaymentType.Mixed, 1500),
                new(PaymentType.Cash, 800)
            ]);

        var response = await client.PostAsJsonAsync("/api/pos/sales", command);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ── Zero-amount entry is accepted ─────────────────────────────────────

    [Fact]
    public async Task Zero_amount_entry_is_accepted()
    {
        var (client, productId) = await ShopWithProductAsync(price: 1000, stock: 10);

        var command = new CreateSaleCommand(
            Lines: [new(productId, 2)],
            PaymentType: PaymentType.Cash,
            PaymentBreakdown: [
                new(PaymentType.Cash, 1500),
                new(PaymentType.Card, 800),
                new(PaymentType.Mobile, 0)
            ]);

        var response = await client.PostAsJsonAsync("/api/pos/sales", command);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ── Negative-amount entry → 400 ───────────────────────────────────────

    [Fact]
    public async Task Negative_amount_entry_returns_400()
    {
        var (client, productId) = await ShopWithProductAsync(price: 1000, stock: 10);

        // Use anonymous object with negative value — validator should catch it
        var response = await client.PostAsJsonAsync("/api/pos/sales", new
        {
            lines = new[] { new { productId, quantity = 2 } },
            paymentType = "Cash",
            paymentBreakdown = new[]
            {
                new { paymentType = "Cash", amountHalalas = 1500 },
                new { paymentType = "Card", amountHalalas = -800 }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GET /api/pos/sales/{id} includes breakdown ────────────────────────

    [Fact]
    public async Task Get_sale_response_includes_breakdown()
    {
        var (client, productId) = await ShopWithProductAsync(price: 1000, stock: 10);

        var saleResponse = await client.PostAsJsonAsync("/api/pos/sales", new CreateSaleCommand(
            Lines: [new(productId, 2)],
            PaymentType: PaymentType.Mixed,
            PaymentBreakdown: [
                new(PaymentType.Cash, 1150),
                new(PaymentType.Card, 1150)
            ]));

        saleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdSale = (await saleResponse.Content.ReadFromJsonAsync<SaleResponse>())!;

        // Get the sale back
        var getResponse = await client.GetFromJsonAsync<SaleResponse>($"/api/pos/sales/{createdSale.Id}");
        getResponse.Should().NotBeNull();
        getResponse!.PaymentBreakdown.Should().HaveCount(2);
        (getResponse.PaymentBreakdown[0].AmountHalalas + getResponse.PaymentBreakdown[1].AmountHalalas).Should().Be(getResponse.TotalHalalas);
    }

    // ── SaleSummary in list includes breakdown ────────────────────────────

    [Fact]
    public async Task SaleSummary_includes_breakdown()
    {
        var (client, productId) = await ShopWithProductAsync(price: 1000, stock: 10);

        // Create a mixed sale
        var saleResponse = await client.PostAsJsonAsync("/api/pos/sales", new CreateSaleCommand(
            Lines: [new(productId, 2)],
            PaymentType: PaymentType.Mixed,
            PaymentBreakdown: [
                new(PaymentType.Cash, 1150),
                new(PaymentType.Card, 1150)
            ]));

        saleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdSale = (await saleResponse.Content.ReadFromJsonAsync<SaleResponse>())!;

        // List sales
        var listResponse = await client.GetFromJsonAsync<ListSalesResponse>("/api/pos/sales");
        listResponse.Should().NotBeNull();
        var summary = listResponse!.Sales.Single(s => s.Id == createdSale.Id);
        summary.PaymentBreakdown.Should().HaveCount(2);
    }

    // ── Credit sale still increments credit balance ───────────────────────

    [Fact]
    public async Task Credit_sale_still_increments_credit_balance()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create a customer
        var customerCreated = await client.PostAsJsonAsync("/api/customers",
            new CreateCustomerCommand("Ahmed", "0501234567"));
        var customer = (await customerCreated.Content.ReadFromJsonAsync<Api.Features.Common.CustomerResponse>())!;

        // Create a product
        var productResponse = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 5000, 10));
        var product = (await productResponse.Content.ReadFromJsonAsync<Api.Features.Common.ProductResponse>())!;

        // Create a credit sale
        var saleResponse = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new(product.Id, 2)], PaymentType.Credit, customer.Id));

        saleResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify balance increased
        var getCustomer = await client.GetFromJsonAsync<Api.Features.Common.CustomerResponse>($"/api/customers/{customer.Id}");
        getCustomer!.CreditBalanceHalalas.Should().BeGreaterThan(0);
    }

    // ── Cross-tenant isolation: existing tenant tests continue to pass ────

    [Fact]
    public async Task Cross_tenant_isolation_with_breakdown()
    {
        var companyA = await Factory.CreateCompanyAsync("A", "أ");
        var companyB = await Factory.CreateCompanyAsync("B", "ب");

        // Company A creates a product and a mixed sale
        var clientA = Factory.ClientFor(companyA);
        var productResponse = await clientA.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 5000, 10));
        var product = (await productResponse.Content.ReadFromJsonAsync<Api.Features.Common.ProductResponse>())!;

        await clientA.PostAsJsonAsync("/api/pos/sales", new CreateSaleCommand(
            Lines: [new(product.Id, 1)],
            PaymentType: PaymentType.Mixed,
            PaymentBreakdown: [
                new(PaymentType.Cash, 575),
                new(PaymentType.Card, 575)
            ]));

        // Company B tries to read the sale — should get 404
        var clientB = Factory.ClientFor(companyB);
        var responseB = await clientB.GetAsync($"/api/pos/sales");
        responseB.StatusCode.Should().Be(HttpStatusCode.OK);
        var listB = (await responseB.Content.ReadFromJsonAsync<ListSalesResponse>())!;
        listB.TotalCount.Should().Be(0);
    }

    // ── Transfer and Mobile payment types work ────────────────────────────

    [Fact]
    public async Task Transfer_payment_type_works()
    {
        var (client, productId) = await ShopWithProductAsync();

        var command = new CreateSaleCommand(
            Lines: [new(productId, 1)],
            PaymentType: PaymentType.Transfer);

        var response = await client.PostAsJsonAsync("/api/pos/sales", command);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Mobile_payment_type_works()
    {
        var (client, productId) = await ShopWithProductAsync();

        var command = new CreateSaleCommand(
            Lines: [new(productId, 1)],
            PaymentType: PaymentType.Mobile);

        var response = await client.PostAsJsonAsync("/api/pos/sales", command);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}

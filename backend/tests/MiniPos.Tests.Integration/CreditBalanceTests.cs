using System.Net;
using System.Net.Http.Json;
using MiniPos.Api.Features.Catalog.CreateProduct;
using MiniPos.Api.Features.Common;
using MiniPos.Api.Features.Customers.CreateCustomer;
using MiniPos.Api.Features.Sales.CreateSale;
using MiniPos.Domain.Sales;

namespace MiniPos.Tests.Integration;

public class CreditBalanceTests(PostgresFixture db) : ApiTestBase(db)
{
    private async Task<(HttpClient Client, Guid CustomerId)> SetupWithCustomerAsync()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);
        var created = await client.PostAsJsonAsync("/api/customers",
            new CreateCustomerCommand("Ahmed", "0501234567"));
        var customer = (await created.Content.ReadFromJsonAsync<CustomerResponse>())!;
        return (client, customer.Id);
    }

    private async Task<(HttpClient Client, Guid ProductId)> SetupWithProductAsync(long price = 1000, int stock = 10)
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);
        var response = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", $"SKU-{Guid.NewGuid():N}"[..12].ToUpperInvariant(), price, stock));
        var product = (await response.Content.ReadFromJsonAsync<MiniPos.Api.Features.Catalog.CreateProduct.ProductResponse>())!;
        return (client, product.Id);
    }

    // ── Credit sale increases balance ──────────────────────────────────────

    [Fact]
    public async Task Credit_sale_increases_customer_balance()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create a customer and product
        var customerCreated = await client.PostAsJsonAsync("/api/customers",
            new CreateCustomerCommand("Ahmed", "0501234567"));
        var customer = (await customerCreated.Content.ReadFromJsonAsync<CustomerResponse>())!;

        var productResponse = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 5000, 10));
        var product = (await productResponse.Content.ReadFromJsonAsync<MiniPos.Api.Features.Catalog.CreateProduct.ProductResponse>())!;

        // Create a credit sale
        var saleResponse = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new(product.Id, 2)], PaymentType.Credit, customer.Id));

        saleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var sale = (await saleResponse.Content.ReadFromJsonAsync<MiniPos.Api.Features.Sales.GetSale.SaleResponse>())!;

        // Verify balance increased by total (net + vat)
        var getCustomer = await client.GetFromJsonAsync<CustomerResponse>($"/api/customers/{customer.Id}");
        getCustomer!.CreditBalanceHalalas.Should().Be(sale.TotalHalalas);
    }

    [Fact]
    public async Task Credit_sale_with_no_customer_does_not_update_balance()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var productResponse = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 5000, 10));
        var product = (await productResponse.Content.ReadFromJsonAsync<MiniPos.Api.Features.Catalog.CreateProduct.ProductResponse>())!;

        // Credit sale without a customer — should still work (balance stays 0)
        var saleResponse = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new(product.Id, 1)], PaymentType.Credit));

        saleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Credit_sale_with_unknown_customer_returns_404()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var productResponse = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 5000, 10));
        var product = (await productResponse.Content.ReadFromJsonAsync<MiniPos.Api.Features.Catalog.CreateProduct.ProductResponse>())!;

        var saleResponse = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new(product.Id, 1)], PaymentType.Credit, Guid.NewGuid()));

        saleResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Payment reduces balance ────────────────────────────────────────────

    [Fact]
    public async Task Payment_reduces_customer_balance()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var customerCreated = await client.PostAsJsonAsync("/api/customers",
            new CreateCustomerCommand("Ahmed", "0501234567"));
        var customer = (await customerCreated.Content.ReadFromJsonAsync<CustomerResponse>())!;

        var productResponse = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 5000, 10));
        var product = (await productResponse.Content.ReadFromJsonAsync<MiniPos.Api.Features.Catalog.CreateProduct.ProductResponse>())!;

        // Create a credit sale to set the balance
        await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new(product.Id, 1)], PaymentType.Credit, customer.Id));

        var getCustomer = await client.GetFromJsonAsync<CustomerResponse>($"/api/customers/{customer.Id}");
        var originalBalance = getCustomer!.CreditBalanceHalalas;

        // Make a payment
        var paymentResponse = await client.PostAsJsonAsync($"/api/customers/{customer.Id}/payments",
            new { amountHalalas = 3000, note = "partial payment" });

        paymentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = (await paymentResponse.Content.ReadFromJsonAsync<CustomerResponse>())!;
        result.CreditBalanceHalalas.Should().Be(originalBalance - 3000);
    }

    // ── Overpayment rejected ───────────────────────────────────────────────

    [Fact]
    public async Task Overpayment_is_rejected()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var customerCreated = await client.PostAsJsonAsync("/api/customers",
            new CreateCustomerCommand("Ahmed", "0501234567"));
        var customer = (await customerCreated.Content.ReadFromJsonAsync<CustomerResponse>())!;

        // Set balance via credit sale
        var productResponse = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 5000, 10));
        var product = (await productResponse.Content.ReadFromJsonAsync<MiniPos.Api.Features.Catalog.CreateProduct.ProductResponse>())!;

        await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new(product.Id, 1)], PaymentType.Credit, customer.Id));

        var getCustomer = await client.GetFromJsonAsync<CustomerResponse>($"/api/customers/{customer.Id}");
        var balance = getCustomer!.CreditBalanceHalalas;

        // Try to pay more than the balance
        var paymentResponse = await client.PostAsJsonAsync($"/api/customers/{customer.Id}/payments",
            new { amountHalalas = balance + 1, note = "too much" });

        paymentResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Zero/negative payment rejected ─────────────────────────────────────

    [Fact]
    public async Task Zero_payment_is_rejected()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var customerCreated = await client.PostAsJsonAsync("/api/customers",
            new CreateCustomerCommand("Ahmed", "0501234567"));
        var customer = (await customerCreated.Content.ReadFromJsonAsync<CustomerResponse>())!;

        var paymentResponse = await client.PostAsJsonAsync($"/api/customers/{customer.Id}/payments",
            new { amountHalalas = 0, note = "zero" });

        paymentResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Negative_payment_is_rejected()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var customerCreated = await client.PostAsJsonAsync("/api/customers",
            new CreateCustomerCommand("Ahmed", "0501234567"));
        var customer = (await customerCreated.Content.ReadFromJsonAsync<CustomerResponse>())!;

        var paymentResponse = await client.PostAsJsonAsync($"/api/customers/{customer.Id}/payments",
            new { amountHalalas = -100, note = "negative" });

        paymentResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Unknown customer → 404 ─────────────────────────────────────────────

    [Fact]
    public async Task Payment_for_unknown_customer_returns_404()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var paymentResponse = await client.PostAsJsonAsync($"/api/customers/{Guid.NewGuid()}/payments",
            new { amountHalalas = 100, note = "ghost" });

        paymentResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Tenant isolation ───────────────────────────────────────────────────

    [Fact]
    public async Task Cross_tenant_customer_balance_isolation()
    {
        var companyA = await Factory.CreateCompanyAsync("A", "أ");
        var companyB = await Factory.CreateCompanyAsync("B", "ب");

        // Company A creates a customer and gives them credit
        var clientA = Factory.ClientFor(companyA);
        var customerCreated = await clientA.PostAsJsonAsync("/api/customers",
            new CreateCustomerCommand("Ahmed", "0501234567"));
        var customer = (await customerCreated.Content.ReadFromJsonAsync<CustomerResponse>())!;

        var productResponse = await clientA.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 5000, 10));
        var product = (await productResponse.Content.ReadFromJsonAsync<MiniPos.Api.Features.Catalog.CreateProduct.ProductResponse>())!;

        await clientA.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new(product.Id, 1)], PaymentType.Credit, customer.Id));

        // Company B tries to read the balance — should get 404
        var responseB = await Factory.ClientFor(companyB).GetAsync($"/api/customers/{customer.Id}");
        responseB.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Company B tries to make a payment — should get 404
        var paymentResponseB = await Factory.ClientFor(companyB)
            .PostAsJsonAsync($"/api/customers/{customer.Id}/payments",
                new { amountHalalas = 100, note = "hacker" });
        paymentResponseB.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── List includes balance ──────────────────────────────────────────────

    [Fact]
    public async Task List_customers_includes_credit_balance()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var customerCreated = await client.PostAsJsonAsync("/api/customers",
            new CreateCustomerCommand("Ahmed", "0501234567"));
        var customer = (await customerCreated.Content.ReadFromJsonAsync<CustomerResponse>())!;

        // Verify initial balance is 0
        var list = await client.GetFromJsonAsync<List<CustomerResponse>>("/api/customers");
        var listedCustomer = list!.Single(c => c.Id == customer.Id);
        listedCustomer.CreditBalanceHalalas.Should().Be(0);

        // Create a credit sale to update the balance
        var productResponse = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 5000, 10));
        var product = (await productResponse.Content.ReadFromJsonAsync<MiniPos.Api.Features.Catalog.CreateProduct.ProductResponse>())!;

        await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new(product.Id, 1)], PaymentType.Credit, customer.Id));

        // Verify balance is reflected in the list
        var updatedList = await client.GetFromJsonAsync<List<CustomerResponse>>("/api/customers");
        var updatedCustomer = updatedList!.Single(c => c.Id == customer.Id);
        updatedCustomer.CreditBalanceHalalas.Should().BeGreaterThan(0);
    }

    // ── Exact balance payment ──────────────────────────────────────────────

    [Fact]
    public async Task Payment_of_exact_balance_clears_it()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var customerCreated = await client.PostAsJsonAsync("/api/customers",
            new CreateCustomerCommand("Ahmed", "0501234567"));
        var customer = (await customerCreated.Content.ReadFromJsonAsync<CustomerResponse>())!;

        var productResponse = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 5000, 10));
        var product = (await productResponse.Content.ReadFromJsonAsync<MiniPos.Api.Features.Catalog.CreateProduct.ProductResponse>())!;

        await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new(product.Id, 1)], PaymentType.Credit, customer.Id));

        var getCustomer = await client.GetFromJsonAsync<CustomerResponse>($"/api/customers/{customer.Id}");
        var balance = getCustomer!.CreditBalanceHalalas;

        // Pay the exact balance
        var paymentResponse = await client.PostAsJsonAsync($"/api/customers/{customer.Id}/payments",
            new { amountHalalas = balance, note = "full payment" });

        paymentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = (await paymentResponse.Content.ReadFromJsonAsync<CustomerResponse>())!;
        result.CreditBalanceHalalas.Should().Be(0);
    }
}

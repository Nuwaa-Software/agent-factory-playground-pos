using System.Net;
using System.Net.Http.Json;
using MiniPos.Api.Features.Catalog.CreateProduct;
using MiniPos.Api.Features.Common;
using MiniPos.Api.Features.Customers.CreateCustomer;
using MiniPos.Api.Features.Sales.CreateSale;
using MiniPos.Api.Features.Sales.GetSale;
using MiniPos.Api.Features.Sales.ListSales;
using MiniPos.Domain.Sales;

namespace MiniPos.Tests.Integration;

public class CustomerTests(PostgresFixture db) : ApiTestBase(db)
{
    [Fact]
    public async Task Admin_creates_and_lists_customers()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var created = await client.PostAsJsonAsync("/api/customers",
            new CreateCustomerCommand("Ahmed", "0501234567"));

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var customer = (await created.Content.ReadFromJsonAsync<CustomerResponse>())!;
        customer.Id.Should().NotBeEmpty();
        customer.Name.Should().Be("Ahmed");
        customer.Phone.Should().Be("0501234567");

        var list = await client.GetFromJsonAsync<List<CustomerResponse>>("/api/customers");
        list!.Select(c => c.Id).Should().Contain(customer.Id);
    }

    [Fact]
    public async Task Admin_can_update_customer_name_and_phone()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var created = await client.PostAsJsonAsync("/api/customers",
            new CreateCustomerCommand("Original", "0501111111"));
        var customer = (await created.Content.ReadFromJsonAsync<CustomerResponse>())!;

        var update = new { Name = "Updated", Phone = "0509999999" };
        var response = await client.PatchAsJsonAsync($"/api/customers/{customer.Id}", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await response.Content.ReadFromJsonAsync<CustomerResponse>())!;
        updated.Name.Should().Be("Updated");
        updated.Phone.Should().Be("0509999999");
    }

    [Fact]
    public async Task Admin_can_update_only_name()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var created = await client.PostAsJsonAsync("/api/customers",
            new CreateCustomerCommand("Original", "0501111111"));
        var customer = (await created.Content.ReadFromJsonAsync<CustomerResponse>())!;

        var update = new { Name = "Updated" };
        var response = await client.PatchAsJsonAsync($"/api/customers/{customer.Id}", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await response.Content.ReadFromJsonAsync<CustomerResponse>())!;
        updated.Name.Should().Be("Updated");
        updated.Phone.Should().Be("0501111111");
    }

    [Fact]
    public async Task Update_unknown_customer_returns_404()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var update = new { Name = "Ghost" };
        var response = await client.PatchAsJsonAsync($"/api/customers/{Guid.NewGuid()}", update);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Companies_never_see_each_others_customers()
    {
        var a = await Factory.CreateCompanyAsync("A", "أ");
        var b = await Factory.CreateCompanyAsync("B", "ب");

        await Factory.ClientFor(a).PostAsJsonAsync("/api/customers",
            new CreateCustomerCommand("Ahmed", "0501234567"));

        var seenByB = await Factory.ClientFor(b).GetFromJsonAsync<List<CustomerResponse>>("/api/customers");

        seenByB.Should().BeEmpty();
    }

    [Fact]
    public async Task Viewer_cannot_create_customers()
    {
        var company = await Factory.CreateCompanyAsync();
        var response = await Factory.ClientFor(company, "Viewer").PostAsJsonAsync("/api/customers",
            new CreateCustomerCommand("Ahmed", "0501234567"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Viewer_cannot_update_customers()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var created = await client.PostAsJsonAsync("/api/customers",
            new CreateCustomerCommand("Ahmed", "0501234567"));
        var customer = (await created.Content.ReadFromJsonAsync<CustomerResponse>())!;

        var response = await Factory.ClientFor(company, "Viewer")
            .PatchAsJsonAsync($"/api/customers/{customer.Id}", new { Name = "Hacker" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Anonymous_requests_are_rejected()
    {
        var response = await Factory.CreateClient().GetAsync(new Uri("/api/customers", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_sale_with_customer_links_the_sale()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create a product and customer
        var productResponse = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 5000, 10));
        var product = (await productResponse.Content.ReadFromJsonAsync<MiniPos.Api.Features.Catalog.CreateProduct.ProductResponse>())!;

        var customerCreated = await client.PostAsJsonAsync("/api/customers",
            new CreateCustomerCommand("Ahmed", "0501234567"));
        var customer = (await customerCreated.Content.ReadFromJsonAsync<CustomerResponse>())!;

        // Create a sale with the customer
        var saleResponse = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new(product.Id, 1)], CustomerId: customer.Id));

        saleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var sale = (await saleResponse.Content.ReadFromJsonAsync<SaleResponse>())!;
        sale.CustomerId.Should().Be(customer.Id);
    }

    [Fact]
    public async Task List_sales_filters_by_customer_id()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create two customers
        var customer1Created = await client.PostAsJsonAsync("/api/customers",
            new CreateCustomerCommand("Ahmed", "0501234567"));
        var customer1 = (await customer1Created.Content.ReadFromJsonAsync<CustomerResponse>())!;

        var customer2Created = await client.PostAsJsonAsync("/api/customers",
            new CreateCustomerCommand("Sara", "0509876543"));
        var customer2 = (await customer2Created.Content.ReadFromJsonAsync<CustomerResponse>())!;

        // Create a product
        var productResponse = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 5000, 10));
        var product = (await productResponse.Content.ReadFromJsonAsync<MiniPos.Api.Features.Catalog.CreateProduct.ProductResponse>())!;

        // Create sales for both customers
        await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new(product.Id, 1)], CustomerId: customer1.Id));
        await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new(product.Id, 2)], CustomerId: customer2.Id));

        // Filter by customer1 - should return only 1 sale
        var salesForCustomer1 = await client.GetFromJsonAsync<ListSalesResponse>(
            $"/api/pos/sales?customerId={customer1.Id}");

        salesForCustomer1!.Sales.Should().ContainSingle().Which.CustomerId.Should().Be(customer1.Id);
    }

    [Fact]
    public async Task Cross_tenant_customer_sales_isolation()
    {
        var companyA = await Factory.CreateCompanyAsync("A", "أ");
        var companyB = await Factory.CreateCompanyAsync("B", "ب");

        // Create a customer in company A
        var clientA = Factory.ClientFor(companyA);
        var customerCreated = await clientA.PostAsJsonAsync("/api/customers",
            new CreateCustomerCommand("Ahmed", "0501234567"));
        var customer = (await customerCreated.Content.ReadFromJsonAsync<CustomerResponse>())!;

        // Create a product in company A
        var productResponse = await clientA.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 5000, 10));
        var product = (await productResponse.Content.ReadFromJsonAsync<MiniPos.Api.Features.Catalog.CreateProduct.ProductResponse>())!;

        // Create a sale in company A with the customer
        await clientA.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new(product.Id, 1)], CustomerId: customer.Id));

        // Company B tries to list customers - should be empty
        var customersB = await Factory.ClientFor(companyB).GetFromJsonAsync<List<CustomerResponse>>("/api/customers");
        customersB.Should().BeEmpty();

        // Company B tries to filter sales by a customer from company A - should return nothing
        var salesB = await Factory.ClientFor(companyB).GetFromJsonAsync<ListSalesResponse>(
            $"/api/pos/sales?customerId={customer.Id}");
        salesB!.Sales.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_customer_with_null_phone_works()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var created = await client.PostAsJsonAsync("/api/customers",
            new CreateCustomerCommand("Walk-in"));

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var customer = (await created.Content.ReadFromJsonAsync<CustomerResponse>())!;
        customer.Phone.Should().BeNull();
    }
}

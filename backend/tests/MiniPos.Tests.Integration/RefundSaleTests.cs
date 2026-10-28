using System.Net;
using System.Net.Http.Json;
using CreateProduct = MiniPos.Api.Features.Catalog.CreateProduct;
using Common = MiniPos.Api.Features.Common;
using CustomersCreateCustomer = MiniPos.Api.Features.Customers.CreateCustomer;
using SalesCreateSale = MiniPos.Api.Features.Sales.CreateSale;
using MiniPos.Api.Features.Sales.GetSale;
using MiniPos.Api.Features.Sales.RefundSale;
using MiniPos.Domain.Sales;

namespace MiniPos.Tests.Integration;

public class RefundSaleTests(PostgresFixture db) : ApiTestBase(db)
{
    private async Task<(HttpClient Client, Guid CompanyId, Guid ProductId)> SetupWithProductAsync(long price = 5000, int stock = 20)
    {
        var companyId = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(companyId);
        var productResp = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProduct.CreateProductCommand("Shirt", "قميص", $"SKU-{Guid.NewGuid():N}"[..12].ToUpperInvariant(), price, stock));
        var product = (await productResp.Content.ReadFromJsonAsync<Common.ProductResponse>())!;
        return (client, companyId, product.Id);
    }

    // ── AC: Full refund (no lineItems) ────────────────────────────────────────

    [Fact]
    public async Task Full_refund_returns_201_and_restores_all_stock()
    {
        var (client, companyId, productId) = await SetupWithProductAsync(price: 5000, stock: 20);

        var saleResp = await client.PostAsJsonAsync("/api/pos/sales",
            new SalesCreateSale.CreateSaleCommand([new(productId, 5)]));
        saleResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var sale = (await saleResp.Content.ReadFromJsonAsync<SaleResponse>())!;

        var refundResp = await client.PostAsJsonAsync($"/api/pos/sales/{sale.Id}/refund", new { });
        refundResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var refund = (await refundResp.Content.ReadFromJsonAsync<RefundSaleResponse>())!;

        refund.TotalHalalas.Should().Be(28750);

        var products = await client.GetFromJsonAsync<List<Common.ProductResponse>>("/api/inventory/products");
        products!.Single(p => p.Id == productId).Stock.Should().Be(20);
    }

    [Fact]
    public async Task Full_refund_returns_201_with_refund_details()
    {
        var (client, companyId, productId) = await SetupWithProductAsync(price: 5000, stock: 20);

        var saleResp = await client.PostAsJsonAsync("/api/pos/sales",
            new SalesCreateSale.CreateSaleCommand([new(productId, 3)]));
        var sale = (await saleResp.Content.ReadFromJsonAsync<SaleResponse>())!;

        var refundResp = await client.PostAsJsonAsync($"/api/pos/sales/{sale.Id}/refund", new { });
        refundResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var refund = (await refundResp.Content.ReadFromJsonAsync<RefundSaleResponse>())!;

        refund.SaleId.Should().Be(sale.Id);
        refund.TotalHalalas.Should().Be(17250);
        refund.Lines.Should().HaveCount(1);
        refund.Lines[0].RefundedQuantity.Should().Be(3);
    }

    // ── AC: Partial refund ────────────────────────────────────────────────────

    [Fact]
    public async Task Partial_refund_restores_only_specified_quantity()
    {
        var (client, companyId, productId) = await SetupWithProductAsync(price: 5000, stock: 20);

        var saleResp = await client.PostAsJsonAsync("/api/pos/sales",
            new SalesCreateSale.CreateSaleCommand([new(productId, 5)]));
        var sale = (await saleResp.Content.ReadFromJsonAsync<SaleResponse>())!;

        var refundResp = await client.PostAsJsonAsync($"/api/pos/sales/{sale.Id}/refund",
            new { lineItems = new[] { new { saleLineId = sale.Lines[0].Id, quantity = 2 } } });

        refundResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var refund = (await refundResp.Content.ReadFromJsonAsync<RefundSaleResponse>())!;

        refund.TotalHalalas.Should().Be(11500);
        refund.Lines[0].RefundedQuantity.Should().Be(2);

        var products = await client.GetFromJsonAsync<List<Common.ProductResponse>>("/api/inventory/products");
        products!.Single(p => p.Id == productId).Stock.Should().Be(17);
    }

    [Fact]
    public async Task Partial_refund_multiple_lines()
    {
        var (client, companyId, productId) = await SetupWithProductAsync(price: 5000, stock: 20);

        var productResp = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProduct.CreateProductCommand("Pants", "بنطال", "PANTS-01", 3000, 15));
        var productB = (await productResp.Content.ReadFromJsonAsync<Common.ProductResponse>())!;

        var saleResp = await client.PostAsJsonAsync("/api/pos/sales",
            new SalesCreateSale.CreateSaleCommand([new(productId, 3), new(productB.Id, 2)]));
        var sale = (await saleResp.Content.ReadFromJsonAsync<SaleResponse>())!;

        var refundResp = await client.PostAsJsonAsync($"/api/pos/sales/{sale.Id}/refund",
            new { lineItems = new[]
                {
                    new { saleLineId = sale.Lines[0].Id, quantity = 1 },
                    new { saleLineId = sale.Lines[1].Id, quantity = 1 }
                }
            });

        refundResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var refund = (await refundResp.Content.ReadFromJsonAsync<RefundSaleResponse>())!;

        refund.TotalHalalas.Should().Be(9200);

        var products = await client.GetFromJsonAsync<List<Common.ProductResponse>>("/api/inventory/products");
        products!.Single(p => p.Id == productId).Stock.Should().Be(18);
        products!.Single(p => p.Id == productB.Id).Stock.Should().Be(14);
    }

    // ── AC: VAT proration ─────────────────────────────────────────────────────

    [Fact]
    public async Task Partial_refund_vat_is_prorated_by_net_proportion()
    {
        var (client, companyId, productId) = await SetupWithProductAsync(price: 5000, stock: 20);

        var saleResp = await client.PostAsJsonAsync("/api/pos/sales",
            new SalesCreateSale.CreateSaleCommand([new(productId, 5)]));
        var sale = (await saleResp.Content.ReadFromJsonAsync<SaleResponse>())!;

        var refundResp = await client.PostAsJsonAsync($"/api/pos/sales/{sale.Id}/refund",
            new { lineItems = new[] { new { saleLineId = sale.Lines[0].Id, quantity = 2 } } });

        refundResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var refund = (await refundResp.Content.ReadFromJsonAsync<RefundSaleResponse>())!;

        refund.TotalHalalas.Should().Be(11500);
    }

    [Fact]
    public async Task VAT_proration_with_known_sale_net_10000_vat_1500_refund_2_of_5()
    {
        var (client, companyId, productId) = await SetupWithProductAsync(price: 2000, stock: 20);

        var saleResp = await client.PostAsJsonAsync("/api/pos/sales",
            new SalesCreateSale.CreateSaleCommand([new(productId, 5)]));
        var sale = (await saleResp.Content.ReadFromJsonAsync<SaleResponse>())!;

        sale.NetHalalas.Should().Be(10000);
        sale.VatHalalas.Should().Be(1500);

        var refundResp = await client.PostAsJsonAsync($"/api/pos/sales/{sale.Id}/refund",
            new { lineItems = new[] { new { saleLineId = sale.Lines[0].Id, quantity = 2 } } });

        refundResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var refund = (await refundResp.Content.ReadFromJsonAsync<RefundSaleResponse>())!;

        refund.TotalHalalas.Should().Be(4600);
    }

    // ── AC: Discount-aware refund ─────────────────────────────────────────────

    [Fact]
    public async Task Refund_respects_line_level_discount()
    {
        var (client, companyId, productId) = await SetupWithProductAsync(price: 10000, stock: 20);

        var saleResp = await client.PostAsJsonAsync("/api/pos/sales",
            new SalesCreateSale.CreateSaleCommand([new(productId, 1, new SalesCreateSale.LineDiscountRequest(DiscountType.Percent, 10, "promo"))]));
        var sale = (await saleResp.Content.ReadFromJsonAsync<SaleResponse>())!;

        sale.Lines[0].LineNetHalalas.Should().Be(9000);

        var refundResp = await client.PostAsJsonAsync($"/api/pos/sales/{sale.Id}/refund", new { });
        refundResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var refund = (await refundResp.Content.ReadFromJsonAsync<RefundSaleResponse>())!;

        refund.TotalHalalas.Should().Be(10350);
    }

    [Fact]
    public async Task Refund_respects_sale_level_discount()
    {
        var (client, companyId, productId) = await SetupWithProductAsync(price: 10000, stock: 20);

        var saleResp = await client.PostAsJsonAsync("/api/pos/sales",
            new SalesCreateSale.CreateSaleCommand(
                [new(productId, 2)],
                SaleDiscount: new SalesCreateSale.SaleDiscountRequest(DiscountType.Percent, 10, "sale")));
        var sale = (await saleResp.Content.ReadFromJsonAsync<SaleResponse>())!;

        sale.NetHalalas.Should().Be(18000);
        sale.SaleDiscountHalalas.Should().Be(2000);

        var refundResp = await client.PostAsJsonAsync($"/api/pos/sales/{sale.Id}/refund", new { });
        refundResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var refund = (await refundResp.Content.ReadFromJsonAsync<RefundSaleResponse>())!;

        refund.TotalHalalas.Should().Be(20700);
    }

    // ── AC: Authorization ─────────────────────────────────────────────────────

    [Fact]
    public async Task CompanyAdmin_can_refund()
    {
        var (client, companyId, productId) = await SetupWithProductAsync();

        var saleResp = await client.PostAsJsonAsync("/api/pos/sales",
            new SalesCreateSale.CreateSaleCommand([new(productId, 1)]));
        var sale = (await saleResp.Content.ReadFromJsonAsync<SaleResponse>())!;

        var adminClient = Factory.ClientFor(companyId, "Admin");
        var refundResp = await adminClient.PostAsJsonAsync($"/api/pos/sales/{sale.Id}/refund", new { });
        refundResp.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Cashier_gets_403_on_refund()
    {
        var (client, companyId, productId) = await SetupWithProductAsync();

        var saleResp = await client.PostAsJsonAsync("/api/pos/sales",
            new SalesCreateSale.CreateSaleCommand([new(productId, 1)]));
        var sale = (await saleResp.Content.ReadFromJsonAsync<SaleResponse>())!;

        var cashierClient = Factory.ClientFor(companyId, "Cashier");
        var refundResp = await cashierClient.PostAsJsonAsync($"/api/pos/sales/{sale.Id}/refund", new { });
        refundResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Viewer_gets_403_on_refund()
    {
        var (client, companyId, productId) = await SetupWithProductAsync();

        var saleResp = await client.PostAsJsonAsync("/api/pos/sales",
            new SalesCreateSale.CreateSaleCommand([new(productId, 1)]));
        var sale = (await saleResp.Content.ReadFromJsonAsync<SaleResponse>())!;

        var viewerClient = Factory.ClientFor(companyId, "Viewer");
        var refundResp = await viewerClient.PostAsJsonAsync($"/api/pos/sales/{sale.Id}/refund", new { });
        refundResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── AC: Credit sale refund ────────────────────────────────────────────────

    [Fact]
    public async Task Full_credit_refund_reduces_balance_to_pre_sale_value()
    {
        var companyId = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(companyId);

        var customerResp = await client.PostAsJsonAsync("/api/customers",
            new CustomersCreateCustomer.CreateCustomerCommand("Ahmed", "0501234567"));
        var customer = (await customerResp.Content.ReadFromJsonAsync<Common.CustomerResponse>())!;

        var productResp = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProduct.CreateProductCommand("Shirt", "قميص", "SHIRT-CR", 5000, 20));
        var product = (await productResp.Content.ReadFromJsonAsync<Common.ProductResponse>())!;

        var getCustomer = await client.GetFromJsonAsync<Common.CustomerResponse>($"/api/customers/{customer.Id}");
        var initialBalance = getCustomer!.CreditBalanceHalalas;

        var saleResp = await client.PostAsJsonAsync("/api/pos/sales",
            new SalesCreateSale.CreateSaleCommand([new(product.Id, 2)], PaymentType.Credit, customer.Id));
        var sale = (await saleResp.Content.ReadFromJsonAsync<SaleResponse>())!;

        getCustomer = await client.GetFromJsonAsync<Common.CustomerResponse>($"/api/customers/{customer.Id}");
        getCustomer!.CreditBalanceHalalas.Should().Be(initialBalance + sale.TotalHalalas);

        var refundResp = await client.PostAsJsonAsync($"/api/pos/sales/{sale.Id}/refund", new { });
        refundResp.StatusCode.Should().Be(HttpStatusCode.Created);

        getCustomer = await client.GetFromJsonAsync<Common.CustomerResponse>($"/api/customers/{customer.Id}");
        getCustomer!.CreditBalanceHalalas.Should().Be(initialBalance);
    }

    [Fact]
    public async Task Partial_credit_refund_reduces_balance_proportionally()
    {
        var companyId = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(companyId);

        var customerResp = await client.PostAsJsonAsync("/api/customers",
            new CustomersCreateCustomer.CreateCustomerCommand("Ahmed", "0501234567"));
        var customer = (await customerResp.Content.ReadFromJsonAsync<Common.CustomerResponse>())!;

        var productResp = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProduct.CreateProductCommand("Shirt", "قميص", "SHIRT-CR2", 5000, 20));
        var product = (await productResp.Content.ReadFromJsonAsync<Common.ProductResponse>())!;

        var getCustomer = await client.GetFromJsonAsync<Common.CustomerResponse>($"/api/customers/{customer.Id}");
        var initialBalance = getCustomer!.CreditBalanceHalalas;

        var saleResp = await client.PostAsJsonAsync("/api/pos/sales",
            new SalesCreateSale.CreateSaleCommand([new(product.Id, 5)], PaymentType.Credit, customer.Id));
        var sale = (await saleResp.Content.ReadFromJsonAsync<SaleResponse>())!;

        getCustomer = await client.GetFromJsonAsync<Common.CustomerResponse>($"/api/customers/{customer.Id}");
        var balanceAfterSale = getCustomer!.CreditBalanceHalalas;

        var refundResp = await client.PostAsJsonAsync($"/api/pos/sales/{sale.Id}/refund",
            new { lineItems = new[] { new { saleLineId = sale.Lines[0].Id, quantity = 2 } } });
        refundResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var refund = (await refundResp.Content.ReadFromJsonAsync<RefundSaleResponse>())!;

        getCustomer = await client.GetFromJsonAsync<Common.CustomerResponse>($"/api/customers/{customer.Id}");
        getCustomer!.CreditBalanceHalalas.Should().Be(balanceAfterSale - refund.TotalHalalas);
    }

    // ── AC: Voided sale → 409 ────────────────────────────────────────────────

    [Fact]
    public async Task Refunding_voided_sale_returns_409()
    {
        var (client, companyId, productId) = await SetupWithProductAsync();

        var saleResp = await client.PostAsJsonAsync("/api/pos/sales",
            new SalesCreateSale.CreateSaleCommand([new(productId, 1)]));
        var sale = (await saleResp.Content.ReadFromJsonAsync<SaleResponse>())!;

        var voidResp = await client.PatchAsync($"/api/pos/sales/{sale.Id}/void", null);
        voidResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var refundResp = await client.PostAsJsonAsync($"/api/pos/sales/{sale.Id}/refund", new { });
        refundResp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── AC: Already fully refunded → 409 ──────────────────────────────────────

    [Fact]
    public async Task Refunding_already_fully_refunded_sale_returns_409()
    {
        var (client, companyId, productId) = await SetupWithProductAsync();

        var saleResp = await client.PostAsJsonAsync("/api/pos/sales",
            new SalesCreateSale.CreateSaleCommand([new(productId, 1)]));
        var sale = (await saleResp.Content.ReadFromJsonAsync<SaleResponse>())!;

        var refundResp1 = await client.PostAsJsonAsync($"/api/pos/sales/{sale.Id}/refund", new { });
        refundResp1.StatusCode.Should().Be(HttpStatusCode.Created);

        var refundResp2 = await client.PostAsJsonAsync($"/api/pos/sales/{sale.Id}/refund", new { });
        refundResp2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── AC: Over quantity → 400 ───────────────────────────────────────────────

    [Fact]
    public async Task Refunding_more_than_sold_returns_400()
    {
        var (client, companyId, productId) = await SetupWithProductAsync();

        var saleResp = await client.PostAsJsonAsync("/api/pos/sales",
            new SalesCreateSale.CreateSaleCommand([new(productId, 3)]));
        var sale = (await saleResp.Content.ReadFromJsonAsync<SaleResponse>())!;

        var refundResp = await client.PostAsJsonAsync($"/api/pos/sales/{sale.Id}/refund",
            new { lineItems = new[] { new { saleLineId = sale.Lines[0].Id, quantity = 5 } } });
        refundResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Partial_refund_then_over_quantity_returns_400()
    {
        var (client, companyId, productId) = await SetupWithProductAsync();

        var saleResp = await client.PostAsJsonAsync("/api/pos/sales",
            new SalesCreateSale.CreateSaleCommand([new(productId, 5)]));
        var sale = (await saleResp.Content.ReadFromJsonAsync<SaleResponse>())!;

        var refundResp1 = await client.PostAsJsonAsync($"/api/pos/sales/{sale.Id}/refund",
            new { lineItems = new[] { new { saleLineId = sale.Lines[0].Id, quantity = 2 } } });
        refundResp1.StatusCode.Should().Be(HttpStatusCode.Created);

        var refundResp2 = await client.PostAsJsonAsync($"/api/pos/sales/{sale.Id}/refund",
            new { lineItems = new[] { new { saleLineId = sale.Lines[0].Id, quantity = 4 } } });
        refundResp2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── AC: Refund entity persisted with all fields ───────────────────────────

    [Fact]
    public async Task Refund_entity_is_persisted_with_all_fields()
    {
        var (client, companyId, productId) = await SetupWithProductAsync();

        var saleResp = await client.PostAsJsonAsync("/api/pos/sales",
            new SalesCreateSale.CreateSaleCommand([new(productId, 1)]));
        var sale = (await saleResp.Content.ReadFromJsonAsync<SaleResponse>())!;

        var refundResp = await client.PostAsJsonAsync($"/api/pos/sales/{sale.Id}/refund",
            new { reason = "damaged goods" });
        refundResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var refund = (await refundResp.Content.ReadFromJsonAsync<RefundSaleResponse>())!;

        refund.Id.Should().NotBeEmpty();
        refund.SaleId.Should().Be(sale.Id);
        refund.TotalHalalas.Should().BeGreaterThan(0);
        refund.CreatedAt.Should().NotBe(default);
    }

    // ── AC: RefundedBy population ─────────────────────────────────────────────

    [Fact]
    public async Task RefundedBy_is_set_from_X_User_Name_header()
    {
        var companyId = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(companyId);

        var productResp = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProduct.CreateProductCommand("Shirt", "قميص", "SHIRT-RB", 5000, 20));
        var product = (await productResp.Content.ReadFromJsonAsync<Common.ProductResponse>())!;

        var saleResp = await client.PostAsJsonAsync("/api/pos/sales",
            new SalesCreateSale.CreateSaleCommand([new(product.Id, 1)]));
        var sale = (await saleResp.Content.ReadFromJsonAsync<SaleResponse>())!;

        var adminClient = Factory.ClientFor(companyId, "Admin");
        adminClient.DefaultRequestHeaders.Add("X-User-Name", "test_user_123");
        var refundResp = await adminClient.PostAsJsonAsync($"/api/pos/sales/{sale.Id}/refund", new { });
        refundResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var getSaleResp = await adminClient.GetAsync($"/api/pos/sales/{sale.Id}");
        getSaleResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var getSale = (await getSaleResp.Content.ReadFromJsonAsync<SaleResponse>())!;
        getSale!.Refunds.Should().ContainSingle();
        getSale.Refunds[0].RefundedBy.Should().Be("test_user_123");
    }

    [Fact]
    public async Task RefundedBy_falls_back_to_role_when_no_X_User_Name()
    {
        var companyId = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(companyId, "Admin");

        var productResp = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProduct.CreateProductCommand("Shirt", "قميص", "SHIRT-RB2", 5000, 20));
        var product = (await productResp.Content.ReadFromJsonAsync<Common.ProductResponse>())!;

        var saleResp = await client.PostAsJsonAsync("/api/pos/sales",
            new SalesCreateSale.CreateSaleCommand([new(product.Id, 1)]));
        var sale = (await saleResp.Content.ReadFromJsonAsync<SaleResponse>())!;

        var refundResp = await client.PostAsJsonAsync($"/api/pos/sales/{sale.Id}/refund", new { });
        refundResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var getSaleResp = await client.GetAsync($"/api/pos/sales/{sale.Id}");
        var getSale = (await getSaleResp.Content.ReadFromJsonAsync<SaleResponse>())!;
        getSale!.Refunds.Should().ContainSingle();
        getSale.Refunds[0].RefundedBy.Should().Be("Admin");
    }

    // ── AC: GET sale includes refunds array ───────────────────────────────────

    [Fact]
    public async Task GET_sale_includes_refunds_array()
    {
        var (client, companyId, productId) = await SetupWithProductAsync();

        var saleResp = await client.PostAsJsonAsync("/api/pos/sales",
            new SalesCreateSale.CreateSaleCommand([new(productId, 1)]));
        var sale = (await saleResp.Content.ReadFromJsonAsync<SaleResponse>())!;

        var getSaleResp = await client.GetAsync($"/api/pos/sales/{sale.Id}");
        var getSale = (await getSaleResp.Content.ReadFromJsonAsync<SaleResponse>())!;
        getSale!.Refunds.Should().BeEmpty();

        var refundResp = await client.PostAsJsonAsync($"/api/pos/sales/{sale.Id}/refund", new { });
        refundResp.StatusCode.Should().Be(HttpStatusCode.Created);

        getSaleResp = await client.GetAsync($"/api/pos/sales/{sale.Id}");
        getSale = (await getSaleResp.Content.ReadFromJsonAsync<SaleResponse>())!;
        getSale!.Refunds.Should().HaveCount(1);
        getSale.Refunds[0].TotalHalalas.Should().Be(sale.TotalHalalas);
    }

    // ── AC: Stock restored correctly ──────────────────────────────────────────

    [Fact]
    public async Task Stock_is_restored_correctly()
    {
        var (client, companyId, productId) = await SetupWithProductAsync(price: 5000, stock: 20);

        var saleResp = await client.PostAsJsonAsync("/api/pos/sales",
            new SalesCreateSale.CreateSaleCommand([new(productId, 3)]));
        var sale = (await saleResp.Content.ReadFromJsonAsync<SaleResponse>())!;

        var products = await client.GetFromJsonAsync<List<Common.ProductResponse>>("/api/inventory/products");
        products!.Single(p => p.Id == productId).Stock.Should().Be(17);

        var refundResp = await client.PostAsJsonAsync($"/api/pos/sales/{sale.Id}/refund", new { });
        refundResp.StatusCode.Should().Be(HttpStatusCode.Created);

        products = await client.GetFromJsonAsync<List<Common.ProductResponse>>("/api/inventory/products");
        products!.Single(p => p.Id == productId).Stock.Should().Be(20);
    }

    // ── AC: Cross-tenant isolation ────────────────────────────────────────────

    [Fact]
    public async Task Cross_tenant_refund_returns_404()
    {
        var companyA = await Factory.CreateCompanyAsync("A", "أ");
        var companyB = await Factory.CreateCompanyAsync("B", "ب");

        var clientA = Factory.ClientFor(companyA);
        var productResp = await clientA.PostAsJsonAsync("/api/inventory/products",
            new CreateProduct.CreateProductCommand("Shirt", "قميص", "SHIRT-CT", 5000, 10));
        var product = (await productResp.Content.ReadFromJsonAsync<Common.ProductResponse>())!;

        var saleResp = await clientA.PostAsJsonAsync("/api/pos/sales",
            new SalesCreateSale.CreateSaleCommand([new(product.Id, 1)]));
        var sale = (await saleResp.Content.ReadFromJsonAsync<SaleResponse>())!;

        var clientB = Factory.ClientFor(companyB);
        var refundResp = await clientB.PostAsJsonAsync($"/api/pos/sales/{sale.Id}/refund", new { });
        refundResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── AC: Refund with reason field ──────────────────────────────────────────

    [Fact]
    public async Task Refund_with_reason_is_persisted()
    {
        var (client, companyId, productId) = await SetupWithProductAsync();

        var saleResp = await client.PostAsJsonAsync("/api/pos/sales",
            new SalesCreateSale.CreateSaleCommand([new(productId, 1)]));
        var sale = (await saleResp.Content.ReadFromJsonAsync<SaleResponse>())!;

        var refundResp = await client.PostAsJsonAsync($"/api/pos/sales/{sale.Id}/refund",
            new { reason = "customer not satisfied" });
        refundResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var getSaleResp = await client.GetAsync($"/api/pos/sales/{sale.Id}");
        var getSale = (await getSaleResp.Content.ReadFromJsonAsync<SaleResponse>())!;
        getSale!.Refunds.Should().ContainSingle(r => r.Reason == "customer not satisfied");
    }

    // ── AC: Refund with no reason (null) is allowed ───────────────────────────

    [Fact]
    public async Task Refund_with_no_reason_is_allowed()
    {
        var (client, companyId, productId) = await SetupWithProductAsync();

        var saleResp = await client.PostAsJsonAsync("/api/pos/sales",
            new SalesCreateSale.CreateSaleCommand([new(productId, 1)]));
        var sale = (await saleResp.Content.ReadFromJsonAsync<SaleResponse>())!;

        var refundResp = await client.PostAsJsonAsync($"/api/pos/sales/{sale.Id}/refund", new { });
        refundResp.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}

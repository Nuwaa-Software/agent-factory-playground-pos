using System.Net;
using System.Net.Http.Json;
using MiniPos.Api.Features.Audit.ListAuditLog;
using MiniPos.Api.Features.Catalog.CreateProduct;
using MiniPos.Api.Features.Sales.CreateSale;
using MiniPos.Api.Features.Sales.GetSale;
using MiniPos.Domain.Sales;

namespace MiniPos.Tests.Integration;

public class AuditLogTests(PostgresFixture db) : ApiTestBase(db)
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<(HttpClient Client, Guid CompanyId, Guid ProductId)> SetupWithProductAsync(long price = 5000, int stock = 20)
    {
        var companyId = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(companyId);
        var productResp = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", $"SKU-{Guid.NewGuid():N}"[..12].ToUpperInvariant(), price, stock));
        var product = (await productResp.Content.ReadFromJsonAsync<MiniPos.Api.Features.Common.ProductResponse>())!;
        return (client, companyId, product.Id);
    }

    private async Task<(HttpClient Client, Guid CompanyId, Guid ProductId, Guid SaleId)> SetupWithSaleAsync(long price = 5000, int stock = 20)
    {
        var (client, companyId, productId) = await SetupWithProductAsync(price, stock);
        var saleResp = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new(productId, 5)]));
        saleResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var sale = (await saleResp.Content.ReadFromJsonAsync<SaleResponse>())!;
        return (client, companyId, productId, sale.Id);
    }

    // ── AC: AuditLog entity exists with required fields ───────────────────────

    [Fact]
    public async Task List_audit_log_returns_200_with_paginated_list()
    {
        var (client, companyId, productId, saleId) = await SetupWithSaleAsync();

        // Trigger a refund to create an audit entry
        var refundResp = await client.PostAsJsonAsync($"/api/pos/sales/{saleId}/refund", new { });
        refundResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await client.GetAsync("/api/audit-log");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ListAuditLogResponse>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
        result.PageSize.Should().Be(50);
        result.TotalCount.Should().BeGreaterThan(0);
        result.Items.Should().NotBeEmpty();
    }

    // ── AC: Price change audit ────────────────────────────────────────────────

    [Fact]
    public async Task Changing_product_price_creates_audit_entry()
    {
        var (client, companyId, productId) = await SetupWithProductAsync(price: 5000, stock: 20);

        // Update price
        var updateResp = await client.PatchAsJsonAsync($"/api/inventory/products/{productId}",
            new { PriceHalalas = 6000 });
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify audit entry
        var auditResp = await client.GetAsync("/api/audit-log?entityName=Product&action=price_changed");
        auditResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await auditResp.Content.ReadFromJsonAsync<ListAuditLogResponse>();
        result!.Items.Should().ContainSingle(i => i.EntityId == productId && i.Action == "price_changed");

        var entry = result.Items.First(i => i.EntityId == productId && i.Action == "price_changed");
        entry.EntityName.Should().Be("Product");
        entry.Details.Should().Contain("\"oldPriceHalalas\":5000");
        entry.Details.Should().Contain("\"newPriceHalalas\":6000");
    }

    [Fact]
    public async Task Changing_product_price_with_user_name_header_uses_that_actor()
    {
        var companyId = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(companyId);
        client.DefaultRequestHeaders.Add("X-User-Name", "test-user");

        var productResp = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Pants", "بنطال", "PANTS-ACT", 3000, 15));
        var product = (await productResp.Content.ReadFromJsonAsync<MiniPos.Api.Features.Common.ProductResponse>())!;

        await client.PatchAsJsonAsync($"/api/inventory/products/{product.Id}",
            new { PriceHalalas = 4000 });

        var auditResp = await client.GetAsync("/api/audit-log?entityName=Product&action=price_changed");
        var result = await auditResp.Content.ReadFromJsonAsync<ListAuditLogResponse>();
        result!.Items.Should().ContainSingle(i => i.ActorName == "test-user");
    }

    [Fact]
    public async Task Changing_product_price_without_user_name_falls_back_to_role()
    {
        var companyId = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(companyId, "Admin");
        // No X-User-Name header — should fall back to role

        var productResp = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shoes", "أحذية", "SHOES-ACT", 7000, 10));
        var product = (await productResp.Content.ReadFromJsonAsync<MiniPos.Api.Features.Common.ProductResponse>())!;

        await client.PatchAsJsonAsync($"/api/inventory/products/{product.Id}",
            new { PriceHalalas = 8000 });

        var auditResp = await client.GetAsync("/api/audit-log?entityName=Product&action=price_changed");
        var result = await auditResp.Content.ReadFromJsonAsync<ListAuditLogResponse>();
        result!.Items.Should().ContainSingle(i => i.ActorName == "Admin");
    }

    // ── AC: Stock change audit ────────────────────────────────────────────────

    [Fact]
    public async Task Changing_product_stock_creates_audit_entry()
    {
        var (client, companyId, productId) = await SetupWithProductAsync(price: 5000, stock: 20);

        // Update stock
        var updateResp = await client.PatchAsJsonAsync($"/api/inventory/products/{productId}",
            new { Stock = 30 });
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify audit entry
        var auditResp = await client.GetAsync("/api/audit-log?entityName=Product&action=stock_adjusted");
        auditResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await auditResp.Content.ReadFromJsonAsync<ListAuditLogResponse>();
        result!.Items.Should().ContainSingle(i => i.EntityId == productId && i.Action == "stock_adjusted");

        var entry = result.Items.First(i => i.EntityId == productId && i.Action == "stock_adjusted");
        entry.Details.Should().Contain("\"oldStockQuantity\":20");
        entry.Details.Should().Contain("\"newStockQuantity\":30");
    }

    // ── AC: Sale void audit ───────────────────────────────────────────────────

    [Fact]
    public async Task Voiding_sale_creates_audit_entry()
    {
        var (client, companyId, productId, saleId) = await SetupWithSaleAsync();

        var voidResp = await client.PatchAsJsonAsync($"/api/pos/sales/{saleId}/void",
            new { reason = "Customer returned item" });
        voidResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify audit entry
        var auditResp = await client.GetAsync("/api/audit-log?entityName=Sale&action=sale_voided");
        auditResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await auditResp.Content.ReadFromJsonAsync<ListAuditLogResponse>();
        result!.Items.Should().ContainSingle(i => i.EntityId == saleId && i.Action == "sale_voided");

        var entry = result.Items.First(i => i.EntityId == saleId && i.Action == "sale_voided");
        entry.Details.Should().Contain("Customer returned item");
    }

    [Fact]
    public async Task Idempotent_void_does_not_create_duplicate_audit_entry()
    {
        var (client, companyId, productId, saleId) = await SetupWithSaleAsync();

        // First void
        await client.PatchAsJsonAsync($"/api/pos/sales/{saleId}/void", new { reason = "First" });

        // Second void (idempotent)
        var resp2 = await client.PatchAsJsonAsync($"/api/pos/sales/{saleId}/void", new { reason = "Second" });
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify only one audit entry
        var auditResp = await client.GetAsync("/api/audit-log?entityName=Sale&action=sale_voided");
        var result = await auditResp.Content.ReadFromJsonAsync<ListAuditLogResponse>();
        result!.Items.Count(i => i.EntityId == saleId).Should().Be(1);
    }

    // ── AC: Sale refund audit ─────────────────────────────────────────────────

    [Fact]
    public async Task Refunding_sale_creates_audit_entry()
    {
        var (client, companyId, productId, saleId) = await SetupWithSaleAsync();

        var refundResp = await client.PostAsJsonAsync($"/api/pos/sales/{saleId}/refund", new { });
        refundResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify audit entry
        var auditResp = await client.GetAsync("/api/audit-log?entityName=Sale&action=sale_refunded");
        auditResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await auditResp.Content.ReadFromJsonAsync<ListAuditLogResponse>();
        result!.Items.Should().ContainSingle(i => i.EntityId == saleId && i.Action == "sale_refunded");

        var entry = result.Items.First(i => i.EntityId == saleId && i.Action == "sale_refunded");
        entry.Details.Should().Contain("\"totalHalalas\"");
    }

    // ── AC: Filtering by entityName ────────────────────────────────────────────

    [Fact]
    public async Task Filter_by_entityName_returns_only_matching_entries()
    {
        var (client, companyId, productId, saleId) = await SetupWithSaleAsync();

        // Create a price change audit entry
        await client.PatchAsJsonAsync($"/api/inventory/products/{productId}",
            new { PriceHalalas = 7000 });

        // Create a void audit entry
        await client.PatchAsJsonAsync($"/api/pos/sales/{saleId}/void", new { reason = "test" });

        // Filter by Product
        var productAuditResp = await client.GetAsync("/api/audit-log?entityName=Product");
        var productResult = await productAuditResp.Content.ReadFromJsonAsync<ListAuditLogResponse>();
        productResult!.Items.Should().AllSatisfy(i => i.EntityName.Should().Be("Product"));

        // Filter by Sale
        var saleAuditResp = await client.GetAsync("/api/audit-log?entityName=Sale");
        var saleResult = await saleAuditResp.Content.ReadFromJsonAsync<ListAuditLogResponse>();
        saleResult!.Items.Should().AllSatisfy(i => i.EntityName.Should().Be("Sale"));
    }

    // ── AC: Filtering by action ───────────────────────────────────────────────

    [Fact]
    public async Task Filter_by_action_returns_only_matching_entries()
    {
        var (client, companyId, productId, saleId) = await SetupWithSaleAsync();

        await client.PatchAsJsonAsync($"/api/inventory/products/{productId}",
            new { PriceHalalas = 7000 });
        await client.PatchAsJsonAsync($"/api/pos/sales/{saleId}/void", new { reason = "test" });

        var priceAuditResp = await client.GetAsync("/api/audit-log?action=price_changed");
        var priceResult = await priceAuditResp.Content.ReadFromJsonAsync<ListAuditLogResponse>();
        priceResult!.Items.Should().AllSatisfy(i => i.Action.Should().Be("price_changed"));
    }

    // ── AC: Filtering by date range ───────────────────────────────────────────

    [Fact]
    public async Task Filter_by_date_range_returns_only_matching_entries()
    {
        var (client, companyId, productId) = await SetupWithProductAsync();

        // Create an audit entry
        await client.PatchAsJsonAsync($"/api/inventory/products/{productId}",
            new { PriceHalalas = 7000 });

        // Filter with a wide range that includes the entry
        var now = DateTimeOffset.UtcNow;
        var past = now.AddDays(-1);
        var future = now.AddDays(1);

        var dateAuditResp = await client.GetAsync($"/api/audit-log?startDate={past:yyyy-MM-dd}&endDate={future:yyyy-MM-dd}");
        var dateResult = await dateAuditResp.Content.ReadFromJsonAsync<ListAuditLogResponse>();
        dateResult!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Filter_by_date_range_excludes_entries_outside_range()
    {
        var (client, companyId, productId) = await SetupWithProductAsync();

        // Create an audit entry
        await client.PatchAsJsonAsync($"/api/inventory/products/{productId}",
            new { PriceHalalas = 7000 });

        // Filter with a range that does NOT include the entry (far future)
        var farFuture = DateTimeOffset.UtcNow.AddDays(365);
        var evenFurther = farFuture.AddDays(1);

        var dateAuditResp = await client.GetAsync($"/api/audit-log?startDate={farFuture:yyyy-MM-dd}&endDate={evenFurther:yyyy-MM-dd}");
        var dateResult = await dateAuditResp.Content.ReadFromJsonAsync<ListAuditLogResponse>();
        dateResult!.Items.Should().BeEmpty();
    }

    // ── AC: Pagination ────────────────────────────────────────────────────────

    [Fact]
    public async Task PageSize_clamped_to_max_200()
    {
        var (client, companyId, productId, saleId) = await SetupWithSaleAsync();

        var auditResp = await client.GetAsync("/api/audit-log?pageSize=999");
        var result = await auditResp.Content.ReadFromJsonAsync<ListAuditLogResponse>();
        result!.PageSize.Should().Be(200);
    }

    [Fact]
    public async Task Page_2_returns_different_items()
    {
        var (client, companyId, productId, saleId) = await SetupWithSaleAsync();

        // Create enough entries to span multiple pages
        for (int i = 0; i < 5; i++)
        {
            var productResp = await client.PostAsJsonAsync("/api/inventory/products",
                new CreateProductCommand($"Item-{i}", $"عنصر-{i}", $"SKU-PAGE-{i}", 1000, 10));
            var product = (await productResp.Content.ReadFromJsonAsync<MiniPos.Api.Features.Common.ProductResponse>())!;
            await client.PatchAsJsonAsync($"/api/inventory/products/{product.Id}",
                new { PriceHalalas = 2000 });
        }

        var page1Resp = await client.GetAsync("/api/audit-log?page=1&pageSize=3");
        var page1Result = await page1Resp.Content.ReadFromJsonAsync<ListAuditLogResponse>();
        page1Result!.Items.Should().HaveCount(3);
        page1Result.TotalCount.Should().BeGreaterThan(3);

        var page2Resp = await client.GetAsync("/api/audit-log?page=2&pageSize=3");
        var page2Result = await page2Resp.Content.ReadFromJsonAsync<ListAuditLogResponse>();
        page2Result!.Items.Should().HaveCountLessThanOrEqualTo(3);
        // Items on page 2 should be different from page 1
        page2Result.Items.Should().NotBeEquivalentTo(page1Result.Items);
    }

    [Fact]
    public async Task TotalCount_and_TotalPages_are_correct()
    {
        var (client, companyId, productId, saleId) = await SetupWithSaleAsync();

        // Trigger a refund to create 1 audit entry
        var refundResp = await client.PostAsJsonAsync($"/api/pos/sales/{saleId}/refund", new { });
        refundResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // Create 4 more entries
        for (int i = 0; i < 4; i++)
        {
            var productResp = await client.PostAsJsonAsync("/api/inventory/products",
                new CreateProductCommand($"Item-TC-{i}", $"عناصر-{i}", $"SKU-TC-{i}", 1000, 10));
            var product = (await productResp.Content.ReadFromJsonAsync<MiniPos.Api.Features.Common.ProductResponse>())!;
            await client.PatchAsJsonAsync($"/api/inventory/products/{product.Id}",
                new { PriceHalalas = 2000 });
        }

        var auditResp = await client.GetAsync("/api/audit-log?pageSize=5");
        var result = await auditResp.Content.ReadFromJsonAsync<ListAuditLogResponse>();
        result!.TotalCount.Should().Be(5); // 1 sale refund + 4 price changes
        result.TotalPages.Should().Be(1);

        var page2Resp = await client.GetAsync("/api/audit-log?pageSize=3");
        var page2Result = await page2Resp.Content.ReadFromJsonAsync<ListAuditLogResponse>();
        page2Result!.TotalCount.Should().Be(5);
        page2Result.TotalPages.Should().Be(2);
    }

    // ── AC: Auth — 401 unauthenticated, 403 Viewer ───────────────────────────

    [Fact]
    public async Task Unauthenticated_request_gets_401()
    {
        var client = Factory.CreateClient();
        var response = await client.GetAsync("/api/audit-log");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Viewer_gets_403_on_audit_log()
    {
        var companyId = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(companyId, "Viewer");
        var response = await client.GetAsync("/api/audit-log");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Cashier_gets_403_on_audit_log()
    {
        var companyId = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(companyId, "Cashier");
        var response = await client.GetAsync("/api/audit-log");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── AC: Tenant isolation ───────────────────────────────────────────────────

    [Fact]
    public async Task Company_A_audit_log_does_not_include_company_B_entries()
    {
        var companyA = await Factory.CreateCompanyAsync("Alpha Store", "ألفا");
        var companyB = await Factory.CreateCompanyAsync("Beta Store", "بيتا");

        var clientA = Factory.ClientFor(companyA);
        var clientB = Factory.ClientFor(companyB);

        // Create a product and trigger an audit entry in company A
        var productRespA = await clientA.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Alpha Product", "منتج ألفا", "ALPHA-01", 5000, 10));
        var productA = (await productRespA.Content.ReadFromJsonAsync<MiniPos.Api.Features.Common.ProductResponse>())!;
        await clientA.PatchAsJsonAsync($"/api/inventory/products/{productA.Id}",
            new { PriceHalalas = 6000 });

        // Create a product and trigger an audit entry in company B
        var productRespB = await clientB.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Beta Product", "منتج بيتا", "BETA-01", 3000, 5));
        var productB = (await productRespB.Content.ReadFromJsonAsync<MiniPos.Api.Features.Common.ProductResponse>())!;
        await clientB.PatchAsJsonAsync($"/api/inventory/products/{productB.Id}",
            new { PriceHalalas = 4000 });

        // Company A's audit log should NOT contain company B's entries
        var aAuditResp = await clientA.GetAsync("/api/audit-log");
        var aResult = await aAuditResp.Content.ReadFromJsonAsync<ListAuditLogResponse>();
        aResult!.Items.Should().NotContain(i => i.EntityId == productB.Id);

        // Company B's audit log should NOT contain company A's entries
        var bAuditResp = await clientB.GetAsync("/api/audit-log");
        var bResult = await bAuditResp.Content.ReadFromJsonAsync<ListAuditLogResponse>();
        bResult!.Items.Should().NotContain(i => i.EntityId == productA.Id);
    }

    // ── AC: Filter by entityId ────────────────────────────────────────────────

    [Fact]
    public async Task Filter_by_entityId_returns_only_matching_entries()
    {
        var (client, companyId, productId) = await SetupWithProductAsync();

        await client.PatchAsJsonAsync($"/api/inventory/products/{productId}",
            new { PriceHalalas = 7000 });

        var auditResp = await client.GetAsync($"/api/audit-log?entityId={productId}");
        var result = await auditResp.Content.ReadFromJsonAsync<ListAuditLogResponse>();
        result!.Items.Should().AllSatisfy(i => i.EntityId.Should().Be(productId));
    }
}

using System.Net;
using System.Net.Http.Json;
using MiniPos.Api.Features.Catalog.CreateProduct;
using MiniPos.Api.Features.Company.GetSettings;
using MiniPos.Api.Features.Company.UpdateSettings;
using MiniPos.Api.Features.Sales.CreateSale;
using MiniPos.Api.Features.Sales.GetSale;
using MiniPos.Domain.Sales;

namespace MiniPos.Tests.Integration;

public class CompanySettingsTests(PostgresFixture db) : ApiTestBase(db)
{
    // ---- GET /api/company/settings ----

    [Fact]
    public async Task Get_returns_200_with_defaults_when_no_config()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var response = await client.GetAsync("/api/company/settings");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var settings = await response.Content.ReadFromJsonAsync<CompanySettingsResponse>();
        settings.Should().NotBeNull();
        settings!.TaxRatePercent.Should().Be(15.0m);
        settings.ReceiptFooterText.Should().BeEmpty();
        settings.LowStockThreshold.Should().Be(10);
        settings.CompanyName.Should().BeEmpty();
        settings.VatRegistrationNumber.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_returns_401_without_auth()
    {
        var response = await Factory.CreateClient().GetAsync("/api/company/settings");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_returns_403_for_viewer()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company, "Viewer");

        var response = await client.GetAsync("/api/company/settings");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- PATCH /api/company/settings ----

    [Fact]
    public async Task Patch_partial_update_returns_full_object()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var response = await client.PatchAsJsonAsync("/api/company/settings",
            new UpdateSettingsCommand(TaxRatePercent: 12.5m));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var settings = await response.Content.ReadFromJsonAsync<CompanySettingsResponse>();
        settings.Should().NotBeNull();
        settings!.TaxRatePercent.Should().Be(12.5m);
        settings.ReceiptFooterText.Should().BeEmpty();
        settings.LowStockThreshold.Should().Be(10);
        settings.CompanyName.Should().BeEmpty();
        settings.VatRegistrationNumber.Should().BeEmpty();
    }

    [Fact]
    public async Task Patch_updates_all_fields()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var response = await client.PatchAsJsonAsync("/api/company/settings",
            new UpdateSettingsCommand(TaxRatePercent: 20.0m, ReceiptFooterText: "Thank you!", LowStockThreshold: 5, CompanyName: "Test Co", VatRegistrationNumber: "VAT-123"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var settings = await response.Content.ReadFromJsonAsync<CompanySettingsResponse>();
        settings!.TaxRatePercent.Should().Be(20.0m);
        settings.ReceiptFooterText.Should().Be("Thank you!");
        settings.LowStockThreshold.Should().Be(5);
        settings.CompanyName.Should().Be("Test Co");
        settings.VatRegistrationNumber.Should().Be("VAT-123");
    }

    [Fact]
    public async Task Patch_tax_rate_above_100_returns_400()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var response = await client.PatchAsJsonAsync("/api/company/settings",
            new UpdateSettingsCommand(TaxRatePercent: 101));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Patch_tax_rate_below_0_returns_400()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var response = await client.PatchAsJsonAsync("/api/company/settings",
            new UpdateSettingsCommand(TaxRatePercent: -1));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Patch_low_stock_threshold_below_0_returns_400()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var response = await client.PatchAsJsonAsync("/api/company/settings",
            new UpdateSettingsCommand(LowStockThreshold: -1));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Patch_low_stock_threshold_above_10000_returns_400()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var response = await client.PatchAsJsonAsync("/api/company/settings",
            new UpdateSettingsCommand(LowStockThreshold: 10001));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Patch_receipt_footer_over_500_chars_returns_400()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var longText = new string('A', 501);
        var response = await client.PatchAsJsonAsync("/api/company/settings",
            new UpdateSettingsCommand(ReceiptFooterText: longText));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Patch_receipt_footer_exactly_500_chars_succeeds()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var text = new string('A', 500);
        var response = await client.PatchAsJsonAsync("/api/company/settings",
            new UpdateSettingsCommand(ReceiptFooterText: text));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var settings = await response.Content.ReadFromJsonAsync<CompanySettingsResponse>();
        settings!.ReceiptFooterText.Should().HaveLength(500);
    }

    [Fact]
    public async Task Patch_requires_admin_returns_403_for_viewer()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company, "Viewer");

        var response = await client.PatchAsJsonAsync("/api/company/settings",
            new UpdateSettingsCommand(TaxRatePercent: 10));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- VAT uses configured rate ----

    [Fact]
    public async Task CreateSale_uses_configured_tax_rate()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Set tax rate to 10%
        await client.PatchAsJsonAsync("/api/company/settings",
            new UpdateSettingsCommand(TaxRatePercent: 10.0m));

        // Create a product
        var response = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", $"SKU-{Guid.NewGuid():N}"[..12].ToUpperInvariant(), 1000, 10));
        var product = (await response.Content.ReadFromJsonAsync<ProductResponse>())!;

        // Create a sale with quantity 10 => net = 10000 halalas
        var saleResponse = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new CreateSaleLine(product.Id, 10)]));
        saleResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var sale = (await saleResponse.Content.ReadFromJsonAsync<SaleResponse>())!;
        sale.NetHalalas.Should().Be(10000);
        // VAT at 10% of 10000 = 1000 halalas
        sale.VatHalalas.Should().Be(1000);
        sale.TotalHalalas.Should().Be(11000);
    }

    [Fact]
    public async Task CreateSale_uses_default_15_when_no_config()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // No config set — should use default 15%
        var response = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", $"SKU-{Guid.NewGuid():N}"[..12].ToUpperInvariant(), 1000, 10));
        var product = (await response.Content.ReadFromJsonAsync<ProductResponse>())!;

        var saleResponse = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new CreateSaleLine(product.Id, 2)]));
        saleResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var sale = (await saleResponse.Content.ReadFromJsonAsync<SaleResponse>())!;
        sale.NetHalalas.Should().Be(2000);
        sale.VatHalalas.Should().Be(300); // 15% of 2000
        sale.TotalHalalas.Should().Be(2300);
    }

    // ---- Receipt shows footer ----

    [Fact]
    public async Task Receipt_shows_footer_when_set()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        await client.PatchAsJsonAsync("/api/company/settings",
            new UpdateSettingsCommand(ReceiptFooterText: "Thank you for your purchase!"));

        var response = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", $"SKU-{Guid.NewGuid():N}"[..12].ToUpperInvariant(), 1000, 10));
        var product = (await response.Content.ReadFromJsonAsync<ProductResponse>())!;

        var saleResponse = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new CreateSaleLine(product.Id, 1)]));
        var sale = (await saleResponse.Content.ReadFromJsonAsync<SaleResponse>())!;

        var receiptResponse = await client.GetAsync($"/api/pos/sales/{sale.Id}/receipt");
        receiptResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var receipt = await receiptResponse.Content.ReadAsStringAsync();
        receipt.Should().Contain("Thank you for your purchase!");
    }

    [Fact]
    public async Task Receipt_omits_footer_when_empty()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // No footer set (default is empty)
        var response = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", $"SKU-{Guid.NewGuid():N}"[..12].ToUpperInvariant(), 1000, 10));
        var product = (await response.Content.ReadFromJsonAsync<ProductResponse>())!;

        var saleResponse = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new CreateSaleLine(product.Id, 1)]));
        var sale = (await saleResponse.Content.ReadFromJsonAsync<SaleResponse>())!;

        var receiptResponse = await client.GetAsync($"/api/pos/sales/{sale.Id}/receipt");
        receiptResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var receipt = await receiptResponse.Content.ReadAsStringAsync();
        // Should NOT contain the footer text
        receipt.Should().NotContain("Thank you");
    }

    [Fact]
    public async Task Receipt_shows_dynamic_vat_rate()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Set tax rate to 10%
        await client.PatchAsJsonAsync("/api/company/settings",
            new UpdateSettingsCommand(TaxRatePercent: 10.0m));

        var response = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", $"SKU-{Guid.NewGuid():N}"[..12].ToUpperInvariant(), 1000, 10));
        var product = (await response.Content.ReadFromJsonAsync<ProductResponse>())!;

        var saleResponse = await client.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new CreateSaleLine(product.Id, 2)]));
        var sale = (await saleResponse.Content.ReadFromJsonAsync<SaleResponse>())!;

        var receiptResponse = await client.GetAsync($"/api/pos/sales/{sale.Id}/receipt");
        var receipt = await receiptResponse.Content.ReadAsStringAsync();
        receipt.Should().Contain("VAT 10%:");
    }

    // ---- Low-stock uses configured threshold ----

    [Fact]
    public async Task LowStock_uses_configured_threshold()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Set low stock threshold to 5
        await client.PatchAsJsonAsync("/api/company/settings",
            new UpdateSettingsCommand(LowStockThreshold: 5));

        // Create products with stock [3, 10]
        await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 5000, 3));
        await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Pants", "بنطال", "PANTS-01", 8000, 10));

        var response = await client.GetFromJsonAsync<List<ProductResponse>>("/api/pos/catalog/low-stock");
        response.Should().NotBeNull();
        response.Should().HaveCount(1);
        response![0].Stock.Should().Be(3);
    }

    [Fact]
    public async Task LowStock_uses_default_threshold_when_no_config()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // No config set — should use default 10
        await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-01", 5000, 5));
        await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Pants", "بنطال", "PANTS-01", 8000, 15));

        var response = await client.GetFromJsonAsync<List<ProductResponse>>("/api/pos/catalog/low-stock");
        response.Should().NotBeNull();
        response.Should().HaveCount(1);
        response![0].Stock.Should().Be(5);
    }

    // ---- Cross-tenant isolation ----

    [Fact]
    public async Task Cross_tenant_isolation_settings()
    {
        var companyA = await Factory.CreateCompanyAsync("A", "أ");
        var companyB = await Factory.CreateCompanyAsync("B", "ب");

        var clientA = Factory.ClientFor(companyA);
        var clientB = Factory.ClientFor(companyB);

        // Company A sets tax rate to 10%
        await clientA.PatchAsJsonAsync("/api/company/settings",
            new UpdateSettingsCommand(TaxRatePercent: 10.0m));

        // Company B should still see default 15%
        var settingsB = await clientB.GetFromJsonAsync<CompanySettingsResponse>("/api/company/settings");
        settingsB!.TaxRatePercent.Should().Be(15.0m);

        // Create a product in each company
        var prodA = (await (await clientA.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-A", 1000, 10))).Content.ReadFromJsonAsync<ProductResponse>())!;
        var prodB = (await (await clientB.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Pants", "بنطال", "PANTS-B", 1000, 10))).Content.ReadFromJsonAsync<ProductResponse>())!;

        // Company A sale: VAT at 10% => net=1000, vat=100
        var saleA = (await (await clientA.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new CreateSaleLine(prodA.Id, 1)]))).Content.ReadFromJsonAsync<SaleResponse>())!;
        saleA.VatHalalas.Should().Be(100);

        // Company B sale: VAT at default 15% => net=1000, vat=150
        var saleB = (await (await clientB.PostAsJsonAsync("/api/pos/sales",
            new CreateSaleCommand([new CreateSaleLine(prodB.Id, 1)]))).Content.ReadFromJsonAsync<SaleResponse>())!;
        saleB.VatHalalas.Should().Be(150);
    }

    [Fact]
    public async Task Cross_tenant_isolation_low_stock()
    {
        var companyA = await Factory.CreateCompanyAsync("A", "أ");
        var companyB = await Factory.CreateCompanyAsync("B", "ب");

        var clientA = Factory.ClientFor(companyA);
        var clientB = Factory.ClientFor(companyB);

        // Company A sets low stock threshold to 3
        await clientA.PatchAsJsonAsync("/api/company/settings",
            new UpdateSettingsCommand(LowStockThreshold: 3));

        // Create a product with stock 5 in each company
        var prodA = (await (await clientA.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Shirt", "قميص", "SHIRT-A", 5000, 5))).Content.ReadFromJsonAsync<ProductResponse>())!;
        var prodB = (await (await clientB.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Pants", "بنطال", "PANTS-B", 8000, 5))).Content.ReadFromJsonAsync<ProductResponse>())!;

        // Company A: threshold=3, stock=5 => NOT low stock (5 >= 3)
        var lowStockA = await clientA.GetFromJsonAsync<List<ProductResponse>>("/api/pos/catalog/low-stock");
        lowStockA.Should().NotBeNull();
        lowStockA.Should().BeEmpty();

        // Company B: default threshold=10, stock=5 => IS low stock (5 < 10)
        var lowStockB = await clientB.GetFromJsonAsync<List<ProductResponse>>("/api/pos/catalog/low-stock");
        lowStockB.Should().NotBeNull();
        lowStockB.Should().HaveCount(1);
        lowStockB![0].Id.Should().Be(prodB.Id);
    }

    // ---- CompanyName and VatRegistrationNumber defaults ----

    [Fact]
    public async Task Get_returns_empty_defaults_for_company_name_and_vat_number()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var response = await client.GetAsync("/api/company/settings");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var settings = await response.Content.ReadFromJsonAsync<CompanySettingsResponse>();
        settings.Should().NotBeNull();
        settings!.CompanyName.Should().BeEmpty();
        settings.VatRegistrationNumber.Should().BeEmpty();
    }

    // ---- PATCH set companyName ----

    [Fact]
    public async Task Patch_set_company_name_returns_200_and_read_back()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var response = await client.PatchAsJsonAsync("/api/company/settings",
            new UpdateSettingsCommand(CompanyName: "My Company"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var settings = await response.Content.ReadFromJsonAsync<CompanySettingsResponse>();
        settings!.CompanyName.Should().Be("My Company");
    }

    [Fact]
    public async Task Patch_set_company_name_persists_and_returned_on_get()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        await client.PatchAsJsonAsync("/api/company/settings",
            new UpdateSettingsCommand(CompanyName: "Persisted Co"));

        var response = await client.GetAsync("/api/company/settings");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var settings = await response.Content.ReadFromJsonAsync<CompanySettingsResponse>();
        settings!.CompanyName.Should().Be("Persisted Co");
    }

    // ---- PATCH set vatRegistrationNumber ----

    [Fact]
    public async Task Patch_set_vat_registration_number_returns_200_and_read_back()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var response = await client.PatchAsJsonAsync("/api/company/settings",
            new UpdateSettingsCommand(VatRegistrationNumber: "310122393500003"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var settings = await response.Content.ReadFromJsonAsync<CompanySettingsResponse>();
        settings!.VatRegistrationNumber.Should().Be("310122393500003");
    }

    [Fact]
    public async Task Patch_set_vat_registration_number_persists_and_returned_on_get()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        await client.PatchAsJsonAsync("/api/company/settings",
            new UpdateSettingsCommand(VatRegistrationNumber: "VAT-12345"));

        var response = await client.GetAsync("/api/company/settings");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var settings = await response.Content.ReadFromJsonAsync<CompanySettingsResponse>();
        settings!.VatRegistrationNumber.Should().Be("VAT-12345");
    }

    // ---- Partial update leaves others unchanged ----

    [Fact]
    public async Task Patch_omitting_company_name_and_vat_number_leaves_them_unchanged()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Set both fields first
        await client.PatchAsJsonAsync("/api/company/settings",
            new UpdateSettingsCommand(CompanyName: "Old Name", VatRegistrationNumber: "OLD-VAT"));

        // Now PATCH only tax rate — omitting both new fields
        var response = await client.PatchAsJsonAsync("/api/company/settings",
            new UpdateSettingsCommand(TaxRatePercent: 20.0m));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var settings = await response.Content.ReadFromJsonAsync<CompanySettingsResponse>();
        settings!.CompanyName.Should().Be("Old Name");
        settings.VatRegistrationNumber.Should().Be("OLD-VAT");
    }

    // ---- Validation: > 500 chars ----

    [Fact]
    public async Task Patch_company_name_over_500_chars_returns_400()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var longName = new string('A', 501);
        var response = await client.PatchAsJsonAsync("/api/company/settings",
            new UpdateSettingsCommand(CompanyName: longName));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Patch_vat_registration_number_over_500_chars_returns_400()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var longVat = new string('A', 501);
        var response = await client.PatchAsJsonAsync("/api/company/settings",
            new UpdateSettingsCommand(VatRegistrationNumber: longVat));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---- Validation: exactly 500 chars ----

    [Fact]
    public async Task Patch_company_name_exactly_500_chars_succeeds()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var name = new string('A', 500);
        var response = await client.PatchAsJsonAsync("/api/company/settings",
            new UpdateSettingsCommand(CompanyName: name));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var settings = await response.Content.ReadFromJsonAsync<CompanySettingsResponse>();
        settings!.CompanyName.Should().HaveLength(500);
    }

    [Fact]
    public async Task Patch_vat_registration_number_exactly_500_chars_succeeds()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var vat = new string('A', 500);
        var response = await client.PatchAsJsonAsync("/api/company/settings",
            new UpdateSettingsCommand(VatRegistrationNumber: vat));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var settings = await response.Content.ReadFromJsonAsync<CompanySettingsResponse>();
        settings!.VatRegistrationNumber.Should().HaveLength(500);
    }

    // ---- Validation: control characters ----

    [Fact]
    public async Task Patch_company_name_with_control_char_returns_400()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var nameWithControl = "Valid\u0000Name";
        var response = await client.PatchAsJsonAsync("/api/company/settings",
            new UpdateSettingsCommand(CompanyName: nameWithControl));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Patch_vat_registration_number_with_control_char_returns_400()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var vatWithControl = "VAT\u0001-123";
        var response = await client.PatchAsJsonAsync("/api/company/settings",
            new UpdateSettingsCommand(VatRegistrationNumber: vatWithControl));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---- Cross-tenant isolation for company name and VAT number ----

    [Fact]
    public async Task Cross_tenant_isolation_company_name_and_vat_number()
    {
        var companyA = await Factory.CreateCompanyAsync("A", "أ");
        var companyB = await Factory.CreateCompanyAsync("B", "ب");

        var clientA = Factory.ClientFor(companyA);
        var clientB = Factory.ClientFor(companyB);

        // Company A sets both fields
        await clientA.PatchAsJsonAsync("/api/company/settings",
            new UpdateSettingsCommand(CompanyName: "Company A Name", VatRegistrationNumber: "VAT-A"));

        // Company B should still see empty defaults
        var settingsB = await clientB.GetFromJsonAsync<CompanySettingsResponse>("/api/company/settings");
        settingsB!.CompanyName.Should().BeEmpty();
        settingsB.VatRegistrationNumber.Should().BeEmpty();

        // Company A should see its own values
        var settingsA = await clientA.GetFromJsonAsync<CompanySettingsResponse>("/api/company/settings");
        settingsA!.CompanyName.Should().Be("Company A Name");
        settingsA.VatRegistrationNumber.Should().Be("VAT-A");
    }
}

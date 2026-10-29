using System.Net;
using System.Net.Http.Json;
using MiniPos.Api.Features.Shifts;
using MiniPos.Api.Auth;
using MiniPos.Domain.Sales;

namespace MiniPos.Tests.Integration;

public class ShiftTests(PostgresFixture db) : ApiTestBase(db)
{
    [Fact]
    public async Task Open_shift_returns_201_with_details()
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync());
        
        var response = await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "John Doe", startingCashHalalas = 10000 });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var shift = (await response.Content.ReadFromJsonAsync<ShiftResponse>())!;
        
        shift.CashierName.Should().Be("John Doe");
        shift.StartingCashHalalas.Should().Be(10000);
        shift.OpenedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        shift.ClosedAt.Should().BeNull();
        shift.ActualCashHalalas.Should().BeNull();
        shift.ExpectedCashHalalas.Should().BeNull();
        shift.DiscrepancyHalalas.Should().BeNull();
    }

    [Fact]
    public async Task Open_shift_rejects_negative_starting_cash()
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync());
        
        var response = await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "John Doe", startingCashHalalas = -100 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Open_shift_rejects_empty_cashier_name()
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync());
        
        var response = await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "", startingCashHalalas = 10000 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Open_shift_rejects_long_cashier_name()
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync());
        
        var response = await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = new string('A', 101), startingCashHalalas = 10000 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Multiple_open_shifts_allowed()
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync());
        
        var response1 = await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "John Doe", startingCashHalalas = 10000 });
        
        var response2 = await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "Jane Smith", startingCashHalalas = 5000 });

        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        response2.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Close_shift_sets_closed_at_and_actual_cash()
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync());
        
        // Open a shift
        var openResponse = await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "John Doe", startingCashHalalas = 10000 });
        
        var shift = (await openResponse.Content.ReadFromJsonAsync<ShiftResponse>())!;
        
        // Close the shift
        var closeResponse = await client.PatchAsJsonAsync($"/api/pos/shifts/{shift.Id}/close",
            new { actualCashHalalas = 12000 });

        closeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var closedShift = (await closeResponse.Content.ReadFromJsonAsync<ShiftResponse>())!;
        
        closedShift.ClosedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        closedShift.ActualCashHalalas.Should().Be(12000);
        // No sales → expected = starting cash; discrepancy = actual − expected
        closedShift.ExpectedCashHalalas.Should().Be(10000);
        closedShift.DiscrepancyHalalas.Should().Be(2000);
    }

    [Fact]
    public async Task Close_already_closed_shift_returns_409()
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync());
        
        // Open a shift
        var openResponse = await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "John Doe", startingCashHalalas = 10000 });
        
        var shift = (await openResponse.Content.ReadFromJsonAsync<ShiftResponse>())!;
        
        // Close the shift
        await client.PatchAsJsonAsync($"/api/pos/shifts/{shift.Id}/close",
            new { actualCashHalalas = 12000 });
        
        // Try to close again
        var closeResponse = await client.PatchAsJsonAsync($"/api/pos/shifts/{shift.Id}/close",
            new { actualCashHalalas = 13000 });

        closeResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Close_non_existent_shift_returns_404()
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync());
        
        var response = await client.PatchAsJsonAsync("/api/pos/shifts/00000000-0000-0000-0000-000000000000/close",
            new { actualCashHalalas = 12000 });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_shift_returns_details()
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync());
        
        // Open a shift
        var openResponse = await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "John Doe", startingCashHalalas = 10000 });
        
        var shift = (await openResponse.Content.ReadFromJsonAsync<ShiftResponse>())!;
        
        // Get the shift
        var getResponse = await client.GetAsync($"/api/pos/shifts/{shift.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrievedShift = (await getResponse.Content.ReadFromJsonAsync<ShiftResponse>())!;
        
        retrievedShift.Id.Should().Be(shift.Id);
        retrievedShift.CashierName.Should().Be("John Doe");
        retrievedShift.StartingCashHalalas.Should().Be(10000);
        retrievedShift.OpenedAt.Should().BeCloseTo(shift.OpenedAt, TimeSpan.FromSeconds(5));
        retrievedShift.ClosedAt.Should().BeNull();
    }

    [Fact]
    public async Task Get_non_existent_shift_returns_404()
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync());
        
        var response = await client.GetAsync("/api/pos/shifts/00000000-0000-0000-0000-000000000000");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_shifts_returns_company_shifts_only()
    {
        var company1 = await Factory.CreateCompanyAsync();
        var company2 = await Factory.CreateCompanyAsync();
        
        var client1 = Factory.ClientFor(company1);
        var client2 = Factory.ClientFor(company2);
        
        // Open shifts for both companies
        await client1.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "John Doe", startingCashHalalas = 10000 });
        
        await client2.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "Jane Smith", startingCashHalalas = 5000 });
        
        // List shifts for company 1
        var response1 = await client1.GetAsync("/api/pos/shifts");
        var shifts1 = (await response1.Content.ReadFromJsonAsync<List<ShiftResponse>>())!;
        
        // Should only have 1 shift (the one from company 1)
        shifts1.Count.Should().Be(1);
        shifts1[0].CashierName.Should().Be("John Doe");
    }

    [Fact]
    public async Task List_shifts_filters_by_status_open()
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync());
        
        // Open two shifts
        await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "John Doe", startingCashHalalas = 10000 });
        
        var openResponse = await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "Jane Smith", startingCashHalalas = 5000 });
        
        var shift = (await openResponse.Content.ReadFromJsonAsync<ShiftResponse>())!;
        
        // Close one shift
        await client.PatchAsJsonAsync($"/api/pos/shifts/{shift.Id}/close",
            new { actualCashHalalas = 12000 });
        
        // List only open shifts
        var response = await client.GetAsync("/api/pos/shifts?status=open");
        var shifts = (await response.Content.ReadFromJsonAsync<List<ShiftResponse>>())!;
        
        // Should only have 1 open shift
        shifts.Count.Should().Be(1);
        shifts[0].CashierName.Should().Be("John Doe");
        shifts[0].ClosedAt.Should().BeNull();
    }

    [Fact]
    public async Task List_shifts_filters_by_status_closed()
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync());
        
        // Open two shifts
        await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "John Doe", startingCashHalalas = 10000 });
        
        var openResponse = await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "Jane Smith", startingCashHalalas = 5000 });
        
        var shift = (await openResponse.Content.ReadFromJsonAsync<ShiftResponse>())!;
        
        // Close one shift
        await client.PatchAsJsonAsync($"/api/pos/shifts/{shift.Id}/close",
            new { actualCashHalalas = 12000 });
        
        // List only closed shifts
        var response = await client.GetAsync("/api/pos/shifts?status=closed");
        var shifts = (await response.Content.ReadFromJsonAsync<List<ShiftResponse>>())!;
        
        // Should only have 1 closed shift
        shifts.Count.Should().Be(1);
        shifts[0].CashierName.Should().Be("Jane Smith");
        shifts[0].ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task List_shifts_ordered_newest_first()
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync());
        
        // Open two shifts with a small delay
        await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "John Doe", startingCashHalalas = 10000 });
        
        await Task.Delay(10); // Small delay to ensure different timestamps
        
        var openResponse = await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "Jane Smith", startingCashHalalas = 5000 });
        
        // List shifts
        var response = await client.GetAsync("/api/pos/shifts");
        var shifts = (await response.Content.ReadFromJsonAsync<List<ShiftResponse>>())!;
        
        // Should be ordered newest first
        shifts.Count.Should().Be(2);
        shifts[0].CashierName.Should().Be("Jane Smith"); // Newer shift comes first
        shifts[1].CashierName.Should().Be("John Doe");
    }

    [Fact]
    public async Task Viewer_cannot_access_shift_endpoints()
    {
        var client = Factory.ClientFor(await Factory.CreateCompanyAsync(), Roles.Viewer);
        
        // Try to open a shift
        var response = await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "John Doe", startingCashHalalas = 10000 });
        
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Cross_tenant_shift_returns_404()
    {
        var company1 = await Factory.CreateCompanyAsync();
        var company2 = await Factory.CreateCompanyAsync();
        
        var client1 = Factory.ClientFor(company1);
        var client2 = Factory.ClientFor(company2);
        
        // Open a shift for company 1
        var openResponse = await client1.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "John Doe", startingCashHalalas = 10000 });
        
        var shift = (await openResponse.Content.ReadFromJsonAsync<ShiftResponse>())!;
        
        // Try to access the shift from company 2
        var response = await client2.GetAsync($"/api/pos/shifts/{shift.Id}");
        
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Reconciliation: no-sales case (already covered above) ───────────────

    [Fact]
    public async Task Cash_only_sales_reconcile_correctly()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create a product
        var productResponse = await client.PostAsJsonAsync("/api/inventory/products",
            new MiniPos.Api.Features.Catalog.CreateProduct.CreateProductCommand(
                "Rice", "أرز", $"SKU-{Guid.NewGuid():N}"[..12].ToUpperInvariant(), 1000, 10));
        var product = (await productResponse.Content.ReadFromJsonAsync<MiniPos.Api.Features.Common.ProductResponse>())!;

        // Open shift with starting cash 10000
        var openResponse = await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "John Doe", startingCashHalalas = 10000 });
        var shift = (await openResponse.Content.ReadFromJsonAsync<ShiftResponse>())!;

        // Create a cash sale: 1 × 1000 = 1000 net, VAT 15% = 150, total = 1150
        var saleResponse = await client.PostAsJsonAsync("/api/pos/sales",
            new MiniPos.Api.Features.Sales.CreateSale.CreateSaleCommand(
                [new(product.Id, 1)], PaymentType.Cash));
        saleResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Close with actual cash matching expected
        var closeResponse = await client.PatchAsJsonAsync($"/api/pos/shifts/{shift.Id}/close",
            new { actualCashHalalas = 10000 + 1150 });
        closeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var closedShift = (await closeResponse.Content.ReadFromJsonAsync<ShiftResponse>())!;

        closedShift.ExpectedCashHalalas.Should().Be(10000 + 1150); // starting + sale total
        closedShift.DiscrepancyHalalas.Should().Be(0);

        // Verify GET /api/pos/shifts/{id} also returns reconciliation fields
        var getResponse = await client.GetAsync($"/api/pos/shifts/{shift.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrievedShift = (await getResponse.Content.ReadFromJsonAsync<ShiftResponse>())!;
        retrievedShift.ExpectedCashHalalas.Should().Be(10000 + 1150);
        retrievedShift.DiscrepancyHalalas.Should().Be(0);

        // Verify GET /api/pos/shifts?status=closed includes reconciliation fields
        var listResponse = await client.GetAsync("/api/pos/shifts?status=closed");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var shifts = (await listResponse.Content.ReadFromJsonAsync<List<ShiftResponse>>())!;
        shifts.Count.Should().Be(1);
        shifts[0].ExpectedCashHalalas.Should().Be(10000 + 1150);
        shifts[0].DiscrepancyHalalas.Should().Be(0);
    }

    [Fact]
    public async Task Mixed_cash_card_credit_sales_only_cash_counts()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create a product
        var productResponse = await client.PostAsJsonAsync("/api/inventory/products",
            new MiniPos.Api.Features.Catalog.CreateProduct.CreateProductCommand(
                "Shirt", "قميص", $"SKU-{Guid.NewGuid():N}"[..12].ToUpperInvariant(), 1000, 10));
        var product = (await productResponse.Content.ReadFromJsonAsync<MiniPos.Api.Features.Common.ProductResponse>())!;

        // Open shift with starting cash 10000
        var openResponse = await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "John Doe", startingCashHalalas = 10000 });
        var shift = (await openResponse.Content.ReadFromJsonAsync<ShiftResponse>())!;

        // Create a cash sale: 1 × 1000 = 1000 net, VAT 15% = 150, total = 1150
        await client.PostAsJsonAsync("/api/pos/sales",
            new MiniPos.Api.Features.Sales.CreateSale.CreateSaleCommand(
                [new(product.Id, 1)], PaymentType.Cash));

        // Create a card sale: 1 × 1000 = 1000 net, VAT 15% = 150, total = 1150
        await client.PostAsJsonAsync("/api/pos/sales",
            new MiniPos.Api.Features.Sales.CreateSale.CreateSaleCommand(
                [new(product.Id, 1)], PaymentType.Card));

        // Create a credit sale: 1 × 1000 = 1000 net, VAT 15% = 150, total = 1150
        var customerResponse = await client.PostAsJsonAsync("/api/customers",
            new MiniPos.Api.Features.Customers.CreateCustomer.CreateCustomerCommand("Ahmed", "0501234567"));
        var customer = (await customerResponse.Content.ReadFromJsonAsync<MiniPos.Api.Features.Common.CustomerResponse>())!;

        await client.PostAsJsonAsync("/api/pos/sales",
            new MiniPos.Api.Features.Sales.CreateSale.CreateSaleCommand(
                [new(product.Id, 1)], PaymentType.Credit, customer.Id));

        // Close with actual cash matching expected (only cash sale counts)
        var closeResponse = await client.PatchAsJsonAsync($"/api/pos/shifts/{shift.Id}/close",
            new { actualCashHalalas = 10000 + 1150 });
        closeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var closedShift = (await closeResponse.Content.ReadFromJsonAsync<ShiftResponse>())!;

        // Only the cash sale's TotalHalalas (1150) counts, not card or credit
        closedShift.ExpectedCashHalalas.Should().Be(10000 + 1150);
        closedShift.DiscrepancyHalalas.Should().Be(0);
    }

    [Fact]
    public async Task Voided_cash_sale_subtracts_from_expected()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create a product
        var productResponse = await client.PostAsJsonAsync("/api/inventory/products",
            new MiniPos.Api.Features.Catalog.CreateProduct.CreateProductCommand(
                "Juice", "عصير", $"SKU-{Guid.NewGuid():N}"[..12].ToUpperInvariant(), 500, 10));
        var product = (await productResponse.Content.ReadFromJsonAsync<MiniPos.Api.Features.Common.ProductResponse>())!;

        // Open shift with starting cash 10000
        var openResponse = await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "John Doe", startingCashHalalas = 10000 });
        var shift = (await openResponse.Content.ReadFromJsonAsync<ShiftResponse>())!;

        // Create a cash sale: 1 × 500 = 500 net, VAT 15% = 75, total = 575
        var saleResponse = await client.PostAsJsonAsync("/api/pos/sales",
            new MiniPos.Api.Features.Sales.CreateSale.CreateSaleCommand(
                [new(product.Id, 1)], PaymentType.Cash));
        var sale = (await saleResponse.Content.ReadFromJsonAsync<MiniPos.Api.Features.Sales.GetSale.SaleResponse>())!;

        // Void the sale
        var voidResponse = await client.PatchAsJsonAsync($"/api/pos/sales/{sale.Id}/void",
            new { reason = "Customer changed mind" });
        voidResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Close with actual cash matching expected (starting only, voided sale subtracts itself)
        var closeResponse = await client.PatchAsJsonAsync($"/api/pos/shifts/{shift.Id}/close",
            new { actualCashHalalas = 10000 });
        closeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var closedShift = (await closeResponse.Content.ReadFromJsonAsync<ShiftResponse>())!;

        // Expected = starting + nonVoided(0) - voided(575) = 9425
        closedShift.ExpectedCashHalalas.Should().Be(10000 - 575);
        // Discrepancy = actual(10000) - expected(9425) = 575
        closedShift.DiscrepancyHalalas.Should().Be(575);
    }

    [Fact]
    public async Task Discounted_cash_sale_uses_stored_total_halalas()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create a product: price 1000
        var productResponse = await client.PostAsJsonAsync("/api/inventory/products",
            new MiniPos.Api.Features.Catalog.CreateProduct.CreateProductCommand(
                "Shirt", "قميص", $"SKU-{Guid.NewGuid():N}"[..12].ToUpperInvariant(), 1000, 10));
        var product = (await productResponse.Content.ReadFromJsonAsync<MiniPos.Api.Features.Common.ProductResponse>())!;

        // Open shift with starting cash 10000
        var openResponse = await client.PostAsJsonAsync("/api/pos/shifts/open",
            new { cashierName = "John Doe", startingCashHalalas = 10000 });
        var shift = (await openResponse.Content.ReadFromJsonAsync<ShiftResponse>())!;

        // Create a discounted cash sale: 1 × 1000, line discount 10% → net 900, VAT 15% = 135, total = 1035
        var saleResponse = await client.PostAsJsonAsync("/api/pos/sales",
            new MiniPos.Api.Features.Sales.CreateSale.CreateSaleCommand(
                [new(product.Id, 1, new MiniPos.Api.Features.Sales.CreateSale.LineDiscountRequest(
                    DiscountType.Percent, 10, "promo"))],
                PaymentType.Cash));
        saleResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify the stored TotalHalalas is 1035 (not 1150)
        var sale = (await saleResponse.Content.ReadFromJsonAsync<MiniPos.Api.Features.Sales.GetSale.SaleResponse>())!;
        sale.TotalHalalas.Should().Be(1035);

        // Close with actual cash matching expected
        var closeResponse = await client.PatchAsJsonAsync($"/api/pos/shifts/{shift.Id}/close",
            new { actualCashHalalas = 10000 + 1035 });
        closeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var closedShift = (await closeResponse.Content.ReadFromJsonAsync<ShiftResponse>())!;

        // Expected = starting + stored TotalHalalas(1035) — no double-counting of discount
        closedShift.ExpectedCashHalalas.Should().Be(10000 + 1035);
        closedShift.DiscrepancyHalalas.Should().Be(0);
    }
}
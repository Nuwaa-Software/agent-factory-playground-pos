using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Features.Sales.GetSale;
using MiniPos.Domain.Catalog;
using MiniPos.Domain.Common;
using MiniPos.Domain.Companies;
using MiniPos.Domain.Sales;
using MiniPos.Infrastructure.Persistence;
using MiniPos.Infrastructure.Tenancy;

namespace MiniPos.Api.Features.Sales.CreateSale;

public sealed partial class CreateSaleHandler(AppDbContext db, ITenantContext tenant, TimeProvider clock,
    ILogger<CreateSaleHandler> logger) : IRequestHandler<CreateSaleCommand, Result<SaleResponse>>
{
    private const string ConfigMaxDiscountPercent = "Pos:MaxDiscountPercent";
    private const string ConfigMaxDiscountFixedHalalas = "Pos:MaxDiscountFixedHalalas";

    public async Task<Result<SaleResponse>> Handle(CreateSaleCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Load discount limits from config (null = no cap)
        var maxPercent = await GetConfigInt(ConfigMaxDiscountPercent, cancellationToken);
        var maxFixedHalalas = await GetConfigLong(ConfigMaxDiscountFixedHalalas, cancellationToken);

        var ids = request.Lines.Select(l => l.ProductId).ToList();
        var products = await db.Products.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);

        var sale = new Sale
        {
            CompanyId = tenant.CompanyId,
            CreatedAt = clock.GetUtcNow(),
            PaymentType = request.PaymentType ?? PaymentType.Cash,
            CustomerId = request.CustomerId,
        };

        foreach (var line in request.Lines)
        {
            if (!products.TryGetValue(line.ProductId, out var product))
            {
                return new Error("sale.product_not_found", $"Product {line.ProductId} not found.", ErrorKind.NotFound);
            }

            if (product.Stock < line.Quantity)
            {
                return new Error("sale.out_of_stock", $"Only {product.Stock} of {product.Sku} in stock.",
                    ErrorKind.Conflict);
            }

            product.Stock -= line.Quantity;

            long discountHalalas = 0;
            string? discountReason = null;

            if (line.Discount is not null)
            {
                var resolved = ResolveLineDiscount(product.PriceHalalas, line.Quantity, line.Discount.Value, line.Discount.Type, maxPercent, maxFixedHalalas);
                if (!resolved.IsSuccess)
                    return resolved.Error!;
                discountHalalas = resolved.Value;
                discountReason = line.Discount.Reason;
            }

            sale.Lines.Add(new SaleLine
            {
                ProductId = product.Id,
                ProductNameEn = product.NameEn,
                ProductNameAr = product.NameAr,
                Quantity = line.Quantity,
                UnitPriceHalalas = product.PriceHalalas,
                DiscountHalalas = discountHalalas,
                DiscountReason = discountReason,
            });
        }

        // Compute sale-level discount
        long saleDiscountHalalas = 0;
        string? saleDiscountReason = null;

        if (request.SaleDiscount is not null)
        {
            var lineNetSum = sale.Lines.Sum(l => l.LineNetHalalas);
            var resolved = ResolveSaleDiscount(lineNetSum, request.SaleDiscount.Value, request.SaleDiscount.Type, maxPercent, maxFixedHalalas);
            if (!resolved.IsSuccess)
                return resolved.Error!;
            saleDiscountHalalas = resolved.Value;
            saleDiscountReason = request.SaleDiscount.Reason;
        }

        // Compute totals: net floored at 0, then VAT on net, then total
        long netHalalas = Math.Max(0, sale.Lines.Sum(l => l.LineNetHalalas) - saleDiscountHalalas);
        sale.SaleDiscountHalalas = saleDiscountHalalas;
        sale.SaleDiscountReason = saleDiscountReason;

        sale.NetHalalas = netHalalas;
        var taxRate = await GetTaxRate(cancellationToken);
        sale.VatHalalas = Vat.Compute(sale.NetHalalas, taxRate);
        sale.TotalHalalas = sale.NetHalalas + sale.VatHalalas;

        // Update customer credit balance for credit sales
        if (sale.PaymentType == PaymentType.Credit && request.CustomerId is not null)
        {
            var customer = await db.Customers.SingleOrDefaultAsync(
                c => c.Id == request.CustomerId.Value, cancellationToken);
            if (customer is null)
            {
                return new Error("sale.customer_not_found", "Customer not found.", ErrorKind.NotFound);
            }
            customer.CreditBalanceHalalas += sale.TotalHalalas;
        }

        // Use a more robust approach to assign sale numbers that handles concurrency
        var maxNumber = await db.Sales.Where(s => s.CompanyId == tenant.CompanyId).MaxAsync(s => (int?)s.Number, cancellationToken);
        sale.Number = (maxNumber ?? 0) + 1;

        db.Sales.Add(sale);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex) when (ex.Entries.Any(e => e.Entity is Product))
        {
            return Result<SaleResponse>.Failure(
                new Error("sale.stock_conflict", "Stock changed while processing this sale. Please retry.", ErrorKind.Conflict));
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
        {
            // Handle the unique constraint violation for sale number
            db.ChangeTracker.Clear();
            return Result<SaleResponse>.Failure(
                new Error("sale.number_conflict", "Another sale was created at the same time. Please retry.", ErrorKind.Conflict));
        }
        LogSaleCreated(logger, sale.Number, sale.TotalHalalas);

        var company = await db.Companies.SingleAsync(c => c.Id == tenant.CompanyId, cancellationToken);
        return SaleResponse.From(sale, company);
    }

    private static Result<long> ResolveLineDiscount(
        long unitPriceHalalas, int quantity, long value, DiscountType type,
        int? maxPercent, long? maxFixedHalalas)
    {
        var originalAmount = unitPriceHalalas * quantity;
        var resolvedValue = type == DiscountType.Percent
            ? DiscountResolver.PercentToHalalas(originalAmount, (int)value)
            : value;
        return DiscountResolver.Resolve(originalAmount, resolvedValue, maxPercent, maxFixedHalalas);
    }

    private static Result<long> ResolveSaleDiscount(
        long lineNetSum, long value, DiscountType type,
        int? maxPercent, long? maxFixedHalalas)
    {
        var resolvedValue = type == DiscountType.Percent
            ? DiscountResolver.PercentToHalalas(lineNetSum, (int)value)
            : value;
        return DiscountResolver.Resolve(lineNetSum, resolvedValue, maxPercent, maxFixedHalalas);
    }

    private async Task<int?> GetConfigInt(string key, CancellationToken ct)
    {
        var config = await db.CompanyConfigs.Where(c => c.Key == key).FirstOrDefaultAsync(ct);
        return config != null && int.TryParse(config.Value, out var val) ? val : null;
    }

    private async Task<long?> GetConfigLong(string key, CancellationToken ct)
    {
        var config = await db.CompanyConfigs.Where(c => c.Key == key).FirstOrDefaultAsync(ct);
        return config != null && long.TryParse(config.Value, out var val) ? val : null;
    }

    private async Task<decimal> GetTaxRate(CancellationToken ct)
    {
        var config = await db.CompanyConfigs.FirstOrDefaultAsync(c => c.Key == CompanySettings.TaxRatePercentKey && c.CompanyId == tenant.CompanyId, ct);
        if (config != null && decimal.TryParse(config.Value, out var rate))
        {
            return rate;
        }
        return CompanySettings.TaxRatePercentDefault;
    }

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "Sale {Number} created, total {TotalHalalas} halalas")]
    private static partial void LogSaleCreated(ILogger logger, int number, long totalHalalas);
}

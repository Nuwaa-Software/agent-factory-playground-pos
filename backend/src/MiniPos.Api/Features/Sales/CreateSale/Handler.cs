using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Features.Sales.GetSale;
using MiniPos.Domain.Catalog;
using MiniPos.Domain.Common;
using MiniPos.Domain.Sales;
using MiniPos.Infrastructure.Persistence;
using MiniPos.Infrastructure.Tenancy;

namespace MiniPos.Api.Features.Sales.CreateSale;

public sealed partial class CreateSaleHandler(AppDbContext db, ITenantContext tenant, TimeProvider clock,
    ILogger<CreateSaleHandler> logger) : IRequestHandler<CreateSaleCommand, Result<SaleResponse>>
{
    public async Task<Result<SaleResponse>> Handle(CreateSaleCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ids = request.Lines.Select(l => l.ProductId).ToList();
        var products = await db.Products.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);

        var sale = new Sale { CompanyId = tenant.CompanyId, CreatedAt = clock.GetUtcNow() };
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
            sale.Lines.Add(new SaleLine
            {
                ProductId = product.Id,
                ProductNameEn = product.NameEn,
                ProductNameAr = product.NameAr,
                Quantity = line.Quantity,
                UnitPriceHalalas = product.PriceHalalas,
            });
        }

        sale.NetHalalas = sale.Lines.Sum(l => l.LineNetHalalas);
        sale.VatHalalas = Vat.Compute(sale.NetHalalas);
        sale.TotalHalalas = sale.NetHalalas + sale.VatHalalas;
        sale.Number = (await db.Sales.MaxAsync(s => (int?)s.Number, cancellationToken) ?? 0) + 1;

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
        LogSaleCreated(logger, sale.Number, sale.TotalHalalas);

        var company = await db.Companies.SingleAsync(c => c.Id == tenant.CompanyId, cancellationToken);
        return SaleResponse.From(sale, company);
    }

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "Sale {Number} created, total {TotalHalalas} halalas")]
    private static partial void LogSaleCreated(ILogger logger, int number, long totalHalalas);
}

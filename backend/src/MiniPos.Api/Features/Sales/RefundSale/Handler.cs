using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Features.Sales.GetSale;
using MiniPos.Domain.Common;
using MiniPos.Domain.Companies;
using MiniPos.Domain.Customers;
using MiniPos.Domain.Sales;
using MiniPos.Infrastructure.Persistence;
using MiniPos.Infrastructure.Tenancy;

namespace MiniPos.Api.Features.Sales.RefundSale;

public sealed class RefundSaleHandler(
    AppDbContext db,
    ITenantContext tenant,
    TimeProvider clock)
    : IRequestHandler<RefundSaleCommand, Result<RefundSaleResponse>>
{
    public async Task<Result<RefundSaleResponse>> Handle(RefundSaleCommand request, CancellationToken cancellationToken)
    {
        // Load the sale via the global tenant filter; cross-tenant rows are invisible → 404
        var sale = await db.Sales
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == request.SaleId, cancellationToken);

        if (sale is null)
        {
            return new Error("sale.not_found", "Sale not found.", ErrorKind.NotFound);
        }

        // Tenant isolation check
        if (sale.CompanyId != tenant.CompanyId)
        {
            return new Error("sale.not_found", "Sale not found.", ErrorKind.NotFound);
        }

        // Cannot refund a voided sale
        if (sale.Voided)
        {
            return new Error("sale.already_voided", "Cannot refund a voided sale.", ErrorKind.Conflict);
        }

        // Determine which lines to refund
        IReadOnlyList<RefundLine> refundLines;
        bool isFullRefund = request.LineItems is null || request.LineItems.Count == 0;

        if (isFullRefund)
        {
            // Full refund: all lines with quantity = line.Quantity
            refundLines = sale.Lines.Select(l => new RefundLine(l.Id, l.Quantity)).ToList();
        }
        else
        {
            var items = request.LineItems!;
            refundLines = items;
        }

        // Check for already fully refunded
        var anyUnrefunded = sale.Lines.Any(l => l.RefundedQuantity < l.Quantity);
        if (!anyUnrefunded)
        {
            return new Error("sale.already_fully_refunded", "This sale has already been fully refunded.", ErrorKind.Conflict);
        }

        // Validate quantities and compute refund amounts
        long refundNetHalalas = 0;
        long lineNetSum = sale.Lines.Sum(l => l.LineNetHalalas);

        foreach (var item in refundLines)
        {
            var line = sale.Lines.FirstOrDefault(l => l.Id == item.SaleLineId);
            if (line is null)
            {
                return new Error("sale.line_not_found", $"Sale line {item.SaleLineId} not found.", ErrorKind.NotFound);
            }

            var remainingQuantity = line.Quantity - line.RefundedQuantity;
            if (item.Quantity > remainingQuantity)
            {
                return new Error(
                    "sale.over_quantity",
                    $"Cannot refund {item.Quantity} of this line; only {remainingQuantity} remains unrefunded.",
                    ErrorKind.Validation);
            }

            // Per-line net: LineNetHalalas × qty / line.Quantity
            refundNetHalalas += line.LineNetHalalas * item.Quantity / line.Quantity;
        }

        // Prorate sale-level discount proportionally
        long saleDiscountRefunded = 0;
        if (sale.SaleDiscountHalalas > 0 && lineNetSum > 0)
        {
            saleDiscountRefunded = (long)Math.Round(
                sale.SaleDiscountHalalas * (decimal)refundNetHalalas / lineNetSum,
                MidpointRounding.AwayFromZero);
        }

        long adjustedRefundNet = refundNetHalalas - saleDiscountRefunded;

        // Prorate VAT by net proportion: refundVat = saleVat × (adjustedRefundNet / saleNet), rounded half-up
        long refundVatHalalas;
        if (sale.NetHalalas > 0)
        {
            refundVatHalalas = (long)Math.Round(
                sale.VatHalalas * (decimal)adjustedRefundNet / sale.NetHalalas,
                MidpointRounding.AwayFromZero);
        }
        else
        {
            refundVatHalalas = 0;
        }

        long refundTotalHalalas = adjustedRefundNet + refundVatHalalas;

        // Resolve RefundedBy from Claims.UserName → role fallback (set by endpoint)
        var refundedBy = string.IsNullOrEmpty(request.RefundedBy) ? "system" : request.RefundedBy;

        // Create the Refund record
        var refund = new Refund
        {
            SaleId = sale.Id,
            CompanyId = tenant.CompanyId,
            TotalHalalas = refundTotalHalalas,
            Reason = request.Reason,
            CreatedAt = clock.GetUtcNow(),
            RefundedBy = refundedBy,
        };

        // Restore stock and update line RefundedQuantity
        var productIds = refundLines.Select(item => sale.Lines.First(l => l.Id == item.SaleLineId).ProductId).Distinct().ToList();
        var products = await db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);

        foreach (var item in refundLines)
        {
            var line = sale.Lines.First(l => l.Id == item.SaleLineId);
            line.RefundedQuantity += item.Quantity;

            if (products.TryGetValue(line.ProductId, out var product))
            {
                product.Stock += item.Quantity;
            }
        }

        // If Credit payment type, reduce customer's CreditBalanceHalalas
        if (sale.PaymentType == PaymentType.Credit && sale.CustomerId is not null)
        {
            var customer = await db.Customers.SingleOrDefaultAsync(
                c => c.Id == sale.CustomerId.Value, cancellationToken);
            if (customer is not null)
            {
                customer.CreditBalanceHalalas -= refundTotalHalalas;
            }
        }

        db.Refunds.Add(refund);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex) when (ex.Entries.Any(e => e.Entity is MiniPos.Domain.Catalog.Product))
        {
            return Result<RefundSaleResponse>.Failure(
                new Error("sale.stock_conflict", "Stock changed while processing this refund. Please retry.", ErrorKind.Conflict));
        }

        // Build response
        var productResponses = sale.Lines.Select(line =>
        {
            var refundedQty = refundLines.FirstOrDefault(r => r.SaleLineId == line.Id)?.Quantity ?? 0;
            return new RefundLineResponse(
                line.Id,
                line.ProductId,
                line.ProductNameEn,
                refundedQty,
                products.TryGetValue(line.ProductId, out var p) ? p.Stock : 0);
        }).ToList();

        return new RefundSaleResponse(
            refund.Id,
            sale.Id,
            tenant.CompanyId,
            refundTotalHalalas,
            productResponses,
            clock.GetUtcNow());
    }
}

public sealed record RefundLineResponse(
    Guid SaleLineId,
    Guid ProductId,
    string ProductNameEn,
    int RefundedQuantity,
    long NewStockQuantity);

public sealed record RefundSaleResponse(
    Guid Id,
    Guid SaleId,
    Guid CompanyId,
    long TotalHalalas,
    IReadOnlyList<RefundLineResponse> Lines,
    DateTimeOffset CreatedAt);

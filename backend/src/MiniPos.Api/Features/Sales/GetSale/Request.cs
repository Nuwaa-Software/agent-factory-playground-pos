using MediatR;
using MiniPos.Domain.Common;
using MiniPos.Domain.Companies;
using MiniPos.Domain.Invoicing;
using MiniPos.Domain.Sales;

namespace MiniPos.Api.Features.Sales.GetSale;

public sealed record GetSaleQuery(Guid Id) : IRequest<Result<SaleResponse>>;

public sealed record SaleLineResponse(
    Guid ProductId,
    string NameEn,
    string NameAr,
    int Quantity,
    long UnitPriceHalalas,
    long LineNetHalalas,
    long DiscountHalalas,
    string? DiscountReason);

public sealed record PaymentBreakdownResponse(
    PaymentType PaymentType,
    long AmountHalalas);

public sealed record SaleResponse(
    Guid Id,
    int Number,
    DateTimeOffset CreatedAt,
    IReadOnlyList<SaleLineResponse> Lines,
    long NetHalalas,
    long VatHalalas,
    long TotalHalalas,
    string QrCode,
    bool Voided,
    DateTimeOffset? VoidedAt,
    PaymentType PaymentType,
    Guid? CustomerId,
    long SaleDiscountHalalas,
    string? SaleDiscountReason,
    IReadOnlyList<PaymentBreakdownResponse> PaymentBreakdown)
{
    /// <summary>The QR seller name is the company's Arabic name, as on the printed simplified invoice.</summary>
    public static SaleResponse From(Sale sale, MiniPos.Domain.Companies.Company company)
    {
        ArgumentNullException.ThrowIfNull(sale);
        ArgumentNullException.ThrowIfNull(company);
        return new SaleResponse(
            sale.Id, sale.Number, sale.CreatedAt,
            [.. sale.Lines.Select(l => new SaleLineResponse(
                l.ProductId, l.ProductNameEn, l.ProductNameAr, l.Quantity,
                l.UnitPriceHalalas, l.LineNetHalalas, l.DiscountHalalas, l.DiscountReason))],
            sale.NetHalalas, sale.VatHalalas, sale.TotalHalalas,
            InvoiceQr.Encode(company.NameAr, company.VatNumber, sale.CreatedAt, sale.TotalHalalas, sale.VatHalalas),
            sale.Voided, sale.VoidedAt, sale.PaymentType, sale.CustomerId,
            sale.SaleDiscountHalalas, sale.SaleDiscountReason,
            [.. sale.PaymentBreakdown.Select(e => new PaymentBreakdownResponse(
                e.PaymentType, e.AmountHalalas))]);
    }
}

using MediatR;
using MiniPos.Api.Features.Sales.GetSale;
using MiniPos.Domain.Common;
using MiniPos.Domain.Sales;

namespace MiniPos.Api.Features.Sales.CreateSale;

public sealed record CreateSaleCommand(
    IReadOnlyList<CreateSaleLine> Lines,
    PaymentType? PaymentType = null,
    Guid? CustomerId = null,
    SaleDiscountRequest? SaleDiscount = null,
    IReadOnlyList<PaymentBreakdownRequest>? PaymentBreakdown = null) : IRequest<Result<SaleResponse>>;

public sealed record CreateSaleLine(
    Guid ProductId,
    int Quantity,
    LineDiscountRequest? Discount = null);

public sealed record LineDiscountRequest(
    DiscountType Type,
    long Value,
    string Reason);

public sealed record SaleDiscountRequest(
    DiscountType Type,
    long Value,
    string Reason);

public sealed record PaymentBreakdownRequest(
    PaymentType PaymentType,
    long AmountHalalas);

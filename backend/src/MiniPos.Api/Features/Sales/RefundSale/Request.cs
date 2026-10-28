using MediatR;
using MiniPos.Api.Features.Sales.GetSale;
using MiniPos.Domain.Common;

namespace MiniPos.Api.Features.Sales.RefundSale;

public sealed record RefundSaleCommand(
    Guid SaleId,
    IReadOnlyList<RefundLine>? LineItems = null,
    string? Reason = null,
    string RefundedBy = "") : IRequest<Result<RefundSaleResponse>>;

public sealed record RefundLine(Guid SaleLineId, int Quantity);

using MediatR;
using MiniPos.Domain.Common;
using MiniPos.Domain.Sales;

namespace MiniPos.Api.Features.Sales.VoidSale;

public sealed record VoidSaleCommand(Guid Id, string? Reason) : IRequest<Result<MiniPos.Api.Features.Sales.GetSale.SaleResponse>>;

/// <summary>Request body for voiding a sale.</summary>
public sealed record VoidSaleRequest(string? Reason = null);
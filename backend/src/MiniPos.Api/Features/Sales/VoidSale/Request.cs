using MediatR;
using MiniPos.Domain.Common;
using MiniPos.Domain.Sales;

namespace MiniPos.Api.Features.Sales.VoidSale;

public sealed record VoidSaleCommand(Guid Id) : IRequest<Result<MiniPos.Api.Features.Sales.GetSale.SaleResponse>>;
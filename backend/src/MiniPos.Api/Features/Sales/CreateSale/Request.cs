using MediatR;
using MiniPos.Api.Features.Sales.GetSale;
using MiniPos.Domain.Common;

namespace MiniPos.Api.Features.Sales.CreateSale;

public sealed record CreateSaleCommand(IReadOnlyList<CreateSaleLine> Lines) : IRequest<Result<SaleResponse>>;

public sealed record CreateSaleLine(Guid ProductId, int Quantity);

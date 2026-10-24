using MediatR;
using MiniPos.Api.Features.Sales.GetSale;
using MiniPos.Domain.Common;
using MiniPos.Domain.Sales;

namespace MiniPos.Api.Features.Sales.CreateSale;

public sealed record CreateSaleCommand(IReadOnlyList<CreateSaleLine> Lines, PaymentType? PaymentType = null, Guid? CustomerId = null) : IRequest<Result<SaleResponse>>;

public sealed record CreateSaleLine(Guid ProductId, int Quantity);

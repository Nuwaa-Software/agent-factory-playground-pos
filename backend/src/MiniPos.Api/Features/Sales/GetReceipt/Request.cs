using MediatR;
using MiniPos.Domain.Common;

namespace MiniPos.Api.Features.Sales.GetReceipt;

public sealed record GetReceiptQuery(Guid Id) : IRequest<Result<string>>;
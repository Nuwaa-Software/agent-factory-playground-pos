using MediatR;
using MiniPos.Api.Features.Common;
using MiniPos.Domain.Common;

namespace MiniPos.Api.Features.Catalog.LowStockProducts;

public sealed record LowStockProductsQuery(int? Threshold) : IRequest<Result<IReadOnlyList<ProductResponse>>>;
using MediatR;
using MiniPos.Api.Features.Common;

namespace MiniPos.Api.Features.Catalog.ListProducts;

public sealed record ListProductsQuery(string? Q) : IRequest<IReadOnlyList<ProductResponse>>;

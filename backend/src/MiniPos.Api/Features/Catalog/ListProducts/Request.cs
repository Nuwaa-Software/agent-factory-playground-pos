using MediatR;
using MiniPos.Api.Features.Common;

namespace MiniPos.Api.Features.Catalog.ListProducts;

public sealed record ListProductsQuery : IRequest<IReadOnlyList<ProductResponse>>;

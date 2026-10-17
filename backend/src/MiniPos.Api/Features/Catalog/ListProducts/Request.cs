using MediatR;
using MiniPos.Api.Features.Catalog.CreateProduct;

namespace MiniPos.Api.Features.Catalog.ListProducts;

public sealed record ListProductsQuery : IRequest<IReadOnlyList<ProductResponse>>;

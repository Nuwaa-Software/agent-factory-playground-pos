using MediatR;
using MiniPos.Domain.Common;
using MiniPos.Api.Features.Common;

namespace MiniPos.Api.Features.Catalog.UpdateProduct;

public sealed record UpdateProductCommand(Guid Id, long? PriceHalalas, int? Stock, Guid? CategoryId, string? Actor) : IRequest<Result<ProductResponse>>;
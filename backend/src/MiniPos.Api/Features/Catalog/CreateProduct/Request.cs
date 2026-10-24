using MediatR;
using MiniPos.Domain.Common;
using MiniPos.Api.Features.Common;

namespace MiniPos.Api.Features.Catalog.CreateProduct;

public sealed record CreateProductCommand(string NameEn, string NameAr, string Sku, long PriceHalalas, int Stock, Guid? CategoryId = null)
    : IRequest<Result<ProductResponse>>;

public sealed record ProductResponse(Guid Id, string NameEn, string NameAr, string Sku, long PriceHalalas, int Stock, Guid? CategoryId = null);

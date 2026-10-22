using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Features.Common;
using MiniPos.Infrastructure.Persistence;

namespace MiniPos.Api.Features.Catalog.ListProducts;

public sealed class ListProductsHandler(AppDbContext db) : IRequestHandler<ListProductsQuery, IReadOnlyList<ProductResponse>>
{
    public async Task<IReadOnlyList<ProductResponse>> Handle(ListProductsQuery request, CancellationToken cancellationToken) =>
        await db.Products
            .OrderBy(p => p.Sku)
            .Select(p => new ProductResponse(p.Id, p.NameEn, p.NameAr, p.Sku, p.PriceHalalas, p.Stock, p.CategoryId))
            .ToListAsync(cancellationToken);
}

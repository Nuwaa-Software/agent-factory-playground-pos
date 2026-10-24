using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Features.Common;
using MiniPos.Domain.Catalog;
using MiniPos.Infrastructure.Persistence;

namespace MiniPos.Api.Features.Catalog.ListProducts;
 
public sealed class ListProductsHandler(AppDbContext db) : IRequestHandler<ListProductsQuery, IReadOnlyList<ProductResponse>>
{
    public async Task<IReadOnlyList<ProductResponse>> Handle(ListProductsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Product> query = db.Products;
        
        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            var searchTerm = request.Q.Trim();
            // Filter on Product entities first (before projection to DTO), so EF Core can translate the Where clause to SQL
            // Use EF.Functions.Like for case-insensitive search in PostgreSQL
            query = query.Where(p => 
                EF.Functions.ILike(p.NameEn, $"%{searchTerm}%") ||
                EF.Functions.ILike(p.NameAr, $"%{searchTerm}%"));
        }

        var result = await query
            .OrderBy(p => p.Sku)
            .Select(p => new ProductResponse(p.Id, p.NameEn, p.NameAr, p.Sku, p.PriceHalalas, p.Stock, p.CategoryId))
            .ToListAsync(cancellationToken);
            
        return result;
    }
}
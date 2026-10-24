using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Features.Common;
using MiniPos.Infrastructure.Persistence;

namespace MiniPos.Api.Features.Catalog.ListCategories;

public sealed class ListCategoriesHandler(AppDbContext db) : IRequestHandler<ListCategoriesQuery, IReadOnlyList<CategoryResponse>>
{
    public async Task<IReadOnlyList<CategoryResponse>> Handle(ListCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await db.Categories
            .OrderBy(c => c.NameEn)
            .Select(c => new CategoryResponse(c.Id, c.NameEn, c.NameAr, c.Description))
            .ToListAsync(cancellationToken);
    }
}
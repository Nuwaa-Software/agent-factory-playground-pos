using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Domain.Catalog;
using MiniPos.Api.Features.Common;
using MiniPos.Domain.Common;
using MiniPos.Infrastructure.Persistence;
using MiniPos.Infrastructure.Tenancy;

namespace MiniPos.Api.Features.Catalog.CreateCategory;

public sealed class CreateCategoryHandler(AppDbContext db, ITenantContext tenant) : IRequestHandler<CreateCategoryCommand, Result<CategoryResponse>>
{
    public async Task<Result<CategoryResponse>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var parentId = request.ParentId;

        if (parentId.HasValue)
        {
            var parentExists = await db.Categories.AnyAsync(c => c.Id == parentId.Value && c.CompanyId == tenant.CompanyId, cancellationToken);
            if (!parentExists)
                return Result<CategoryResponse>.Failure(new Error("ParentNotFound", "Parent category not found.", ErrorKind.NotFound));
        }

        var category = new Category
        {
            CompanyId = tenant.CompanyId,
            NameEn = request.NameEn,
            NameAr = request.NameAr,
            Description = request.Description,
            ParentId = parentId
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);

        return new CategoryResponse(category.Id, category.NameEn, category.NameAr, category.Description, category.ParentId);
    }
}
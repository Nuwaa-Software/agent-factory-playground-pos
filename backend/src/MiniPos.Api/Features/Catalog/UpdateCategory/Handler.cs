using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Features.Common;
using MiniPos.Domain.Common;
using MiniPos.Infrastructure.Persistence;

namespace MiniPos.Api.Features.Catalog.UpdateCategory;

public sealed class UpdateCategoryHandler(AppDbContext db) : IRequestHandler<UpdateCategoryCommand, Result<CategoryResponse>>
{
    public async Task<Result<CategoryResponse>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await db.Categories.SingleOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        
        if (category is null)
            return Result<CategoryResponse>.Failure(new Error("NotFound", "Category not found.", ErrorKind.NotFound));

        if (request.NameEn != null)
            category.NameEn = request.NameEn;
            
        if (request.NameAr != null)
            category.NameAr = request.NameAr;
            
        if (request.Description != null)
            category.Description = request.Description;

        // Handle ParentId: set, change, or clear-to-null via ClearParentId flag
        if (request.ClearParentId)
        {
            category.ParentId = null;
        }
        else if (request.ParentId.HasValue)
        {
            var parentId = request.ParentId.Value;

            // Self-parent check
            if (parentId == category.Id)
                return Result<CategoryResponse>.Failure(new Error("SelfParent", "A category cannot be its own parent.", ErrorKind.Validation));

            // Parent must exist and belong to the same company
            var parentExists = await db.Categories.AnyAsync(c => c.Id == parentId && c.CompanyId == category.CompanyId, cancellationToken);
            if (!parentExists)
                return Result<CategoryResponse>.Failure(new Error("ParentNotFound", "Parent category not found.", ErrorKind.NotFound));

            category.ParentId = parentId;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new CategoryResponse(category.Id, category.NameEn, category.NameAr, category.Description, category.ParentId);
    }
}
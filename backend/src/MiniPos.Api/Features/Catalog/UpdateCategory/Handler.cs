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

        await db.SaveChangesAsync(cancellationToken);

        return new CategoryResponse(category.Id, category.NameEn, category.NameAr, category.Description);
    }
}
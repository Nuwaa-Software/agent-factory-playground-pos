using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Domain.Common;
using MiniPos.Infrastructure.Persistence;

namespace MiniPos.Api.Features.Catalog.DeleteCategory;

public sealed class DeleteCategoryHandler(AppDbContext db) : IRequestHandler<DeleteCategoryCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await db.Categories.SingleOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category is null)
            return Result<Unit>.Failure(new Error("NotFound", "Category not found.", ErrorKind.NotFound));

        // Check for products referencing this category
        var productCount = await db.Products.CountAsync(p => p.CategoryId == category.Id, cancellationToken);
        if (productCount > 0)
            return Result<Unit>.Failure(new Error("HasProducts", $"Cannot delete category: it has {productCount} products. Reassign or delete them first.", ErrorKind.Conflict));

        // Check for child categories
        var childCount = await db.Categories.CountAsync(c => c.ParentId == category.Id, cancellationToken);
        if (childCount > 0)
            return Result<Unit>.Failure(new Error("HasChildren", $"Cannot delete category: it has {childCount} child categories.", ErrorKind.Conflict));

        db.Categories.Remove(category);
        await db.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Domain.Catalog;
using MiniPos.Domain.Common;
using MiniPos.Infrastructure.Persistence;
using MiniPos.Api.Features.Common;

namespace MiniPos.Api.Features.Catalog.UpdateProduct;

public sealed class UpdateProductHandler(AppDbContext db) : IRequestHandler<UpdateProductCommand, Result<ProductResponse>>
{
    public async Task<Result<ProductResponse>> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var product = await db.Products.SingleOrDefaultAsync(p => p.Id == command.Id, cancellationToken);
        
        if (product is null)
        {
            return Result<ProductResponse>.Failure(new Error("NotFound", "Product not found.", ErrorKind.NotFound));
        }

        if (command.PriceHalalas.HasValue)
        {
            product.PriceHalalas = command.PriceHalalas.Value;
        }

        if (command.Stock.HasValue)
        {
            product.Stock = command.Stock.Value;
        }

        if (command.CategoryId.HasValue)
        {
            product.CategoryId = command.CategoryId.Value;
        }

        await db.SaveChangesAsync(cancellationToken);
        
        return new ProductResponse(product.Id, product.NameEn, product.NameAr, product.Sku, product.PriceHalalas, product.Stock, product.CategoryId);
    }
}
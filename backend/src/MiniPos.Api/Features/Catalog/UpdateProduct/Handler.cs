using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Domain.Audit;
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

        var oldPrice = product.PriceHalalas;
        var oldStock = product.Stock;

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

        // Write audit entries for changed fields before saving
        if (command.PriceHalalas.HasValue && command.PriceHalalas.Value != oldPrice)
        {
            db.AuditLogs.Add(new AuditLog
            {
                CompanyId = product.CompanyId,
                Action = "price_changed",
                EntityName = "Product",
                EntityId = product.Id,
                ActorName = command.Actor ?? "system",
                Details = System.Text.Json.JsonSerializer.Serialize(new { oldPriceHalalas = oldPrice, newPriceHalalas = command.PriceHalalas.Value }),
                CreatedAt = DateTimeOffset.UtcNow,
            });
        }

        if (command.Stock.HasValue && command.Stock.Value != oldStock)
        {
            db.AuditLogs.Add(new AuditLog
            {
                CompanyId = product.CompanyId,
                Action = "stock_adjusted",
                EntityName = "Product",
                EntityId = product.Id,
                ActorName = command.Actor ?? "system",
                Details = System.Text.Json.JsonSerializer.Serialize(new { oldStockQuantity = oldStock, newStockQuantity = command.Stock.Value }),
                CreatedAt = DateTimeOffset.UtcNow,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        
        return new ProductResponse(product.Id, product.NameEn, product.NameAr, product.Sku, product.PriceHalalas, product.Stock, product.CategoryId);
    }
}
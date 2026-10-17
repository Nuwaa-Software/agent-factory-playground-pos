using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Domain.Catalog;
using MiniPos.Domain.Common;
using MiniPos.Infrastructure.Persistence;
using MiniPos.Infrastructure.Tenancy;

namespace MiniPos.Api.Features.Catalog.CreateProduct;

public sealed class CreateProductHandler(AppDbContext db, ITenantContext tenant)
    : IRequestHandler<CreateProductCommand, Result<ProductResponse>>
{
    public async Task<Result<ProductResponse>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await db.Products.AnyAsync(p => p.Sku == request.Sku, cancellationToken))
        {
            return new Error("product.sku_taken", $"SKU {request.Sku} already exists.", ErrorKind.Conflict);
        }

        var product = new Product
        {
            CompanyId = tenant.CompanyId,
            NameEn = request.NameEn,
            NameAr = request.NameAr,
            Sku = request.Sku,
            PriceHalalas = request.PriceHalalas,
            Stock = request.Stock,
        };
        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);
        return new ProductResponse(product.Id, product.NameEn, product.NameAr, product.Sku, product.PriceHalalas,
            product.Stock);
    }
}

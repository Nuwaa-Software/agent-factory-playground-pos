using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Features.Common;
using MiniPos.Domain.Common;
using MiniPos.Infrastructure.Persistence;

namespace MiniPos.Api.Features.Catalog.LowStockProducts;

public sealed class LowStockProductsHandler(AppDbContext db) : IRequestHandler<LowStockProductsQuery, Result<IReadOnlyList<ProductResponse>>>
{
    public async Task<Result<IReadOnlyList<ProductResponse>>> Handle(LowStockProductsQuery request, CancellationToken cancellationToken)
    {
        // Resolve threshold value
        int threshold = request.Threshold ?? await GetThresholdFromConfig(cancellationToken);

        // Query products with stock below threshold
        var products = await db.Products
            .Where(p => p.Stock < threshold)
            .OrderBy(p => p.Stock)
            .Select(p => new ProductResponse(
                p.Id,
                p.NameEn,
                p.NameAr,
                p.Sku,
                p.PriceHalalas,
                p.Stock))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ProductResponse>>.Success(products);
    }

    private async Task<int> GetThresholdFromConfig(CancellationToken cancellationToken)
    {
        var config = await db.CompanyConfigs
            .Where(c => c.Key == "Pos:LowStockThreshold")
            .FirstOrDefaultAsync(cancellationToken);

        if (config != null && int.TryParse(config.Value, out int threshold))
        {
            return threshold;
        }

        // Default to 10 if not configured or parsing fails
        return 10;
    }
}
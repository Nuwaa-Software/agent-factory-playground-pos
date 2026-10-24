using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Domain.Common;
using MiniPos.Infrastructure.Persistence;
using System.Globalization;

namespace MiniPos.Api.Features.Reports.SalesAnalytics;

public sealed partial class SalesAnalyticsHandler(AppDbContext db, TimeProvider clock)
    : IRequestHandler<SalesAnalyticsQuery, Result<SalesAnalyticsResponse>>
{
    public async Task<Result<SalesAnalyticsResponse>> Handle(SalesAnalyticsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Resolve date range
        var today = DateOnly.FromDateTime(clock.GetUtcNow().Date);
        DateOnly endDate;
        if (request.endDate != null)
        {
            if (!DateOnly.TryParseExact(request.endDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate))
            {
                return new Error("invalid_date", "Invalid endDate format.", ErrorKind.Validation);
            }
        }
        else
        {
            endDate = today;
        }

        DateOnly startDate;
        if (request.startDate != null)
        {
            if (!DateOnly.TryParseExact(request.startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate))
            {
                return new Error("invalid_date", "Invalid startDate format.", ErrorKind.Validation);
            }
        }
        else
        {
            // Default: 30 calendar days inclusive (today - 29 days)
            startDate = endDate.AddDays(-29);
        }

        var startDateTime = startDate.ToDateTime(TimeOnly.MinValue);
        var endDateTime = endDate.ToDateTime(TimeOnly.MaxValue).AddDays(1).AddTicks(-1);

        // Filter: non-voided sales in range (tenant isolation via global filter)
        var filteredSales = db.Sales.Where(s => !s.Voided && s.CreatedAt >= startDateTime && s.CreatedAt <= endDateTime);

        // Totals
        var totalSales = await filteredSales.CountAsync(cancellationToken);
        var totalNetHalalas = await filteredSales.SumAsync(s => s.NetHalalas, cancellationToken);
        var totalVatHalalas = await filteredSales.SumAsync(s => s.VatHalalas, cancellationToken);
        var totalGrossHalalas = await filteredSales.SumAsync(s => s.TotalHalalas, cancellationToken);

        // Average transaction value (integer division)
        var averageTransactionHalalas = totalSales == 0 ? 0 : totalGrossHalalas / totalSales;

        // topProducts: group sale lines by ProductId, sum quantity and net revenue
        int topN = request.topN ?? 10;
        if (topN < 1) topN = 10;
        if (topN > 50) topN = 50;

        var topProducts = await filteredSales
            .SelectMany(s => s.Lines)
            .GroupBy(sl => sl.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                QuantitySold = g.Sum(sl => sl.Quantity),
                RevenueHalalas = g.Sum(sl => sl.UnitPriceHalalas * sl.Quantity),
            })
            .OrderByDescending(x => x.QuantitySold)
            .ThenByDescending(x => x.RevenueHalalas)
            .Take(topN)
            .ToListAsync(cancellationToken);

        var topProductList = new List<TopProduct>(topProducts.Count);
        foreach (var p in topProducts)
        {
            // Fetch product name from the database
            var product = await db.Products.FindAsync([p.ProductId], cancellationToken);
            string nameEn = product?.NameEn ?? "Unknown";
            topProductList.Add(new TopProduct(p.ProductId, nameEn, p.QuantitySold, p.RevenueHalalas));
        }

        // revenueByHour: group by UTC hour of CreatedAt
        var revenueByHourData = await filteredSales
            .GroupBy(s => s.CreatedAt.UtcDateTime.Hour)
            .Select(g => new
            {
                Hour = g.Key,
                GrossHalalas = g.Sum(s => s.TotalHalalas),
            })
            .OrderBy(x => x.Hour)
            .ToListAsync(cancellationToken);

        var revenueByHourList = new List<RevenueByHour>(revenueByHourData.Count);
        foreach (var h in revenueByHourData)
        {
            revenueByHourList.Add(new RevenueByHour(h.Hour, h.GrossHalalas));
        }

        return new SalesAnalyticsResponse(
            startDate: startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            endDate: endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            totalSales: totalSales,
            totalNetHalalas: totalNetHalalas,
            totalVatHalalas: totalVatHalalas,
            totalGrossHalalas: totalGrossHalalas,
            averageTransactionHalalas: averageTransactionHalalas,
            topProducts: topProductList,
            revenueByHour: revenueByHourList);
    }
}

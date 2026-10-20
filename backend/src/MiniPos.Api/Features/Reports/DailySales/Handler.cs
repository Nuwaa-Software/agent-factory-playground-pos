using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Domain.Common;
using MiniPos.Infrastructure.Persistence;
using MiniPos.Infrastructure.Tenancy;
using System.Globalization;

namespace MiniPos.Api.Features.Reports.DailySales;

public sealed partial class DailySalesHandler(AppDbContext db, TimeProvider clock)
    : IRequestHandler<DailySalesQuery, Result<DailySalesResponse>>
{
    public async Task<Result<DailySalesResponse>> Handle(DailySalesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Resolve target date
        DateOnly targetDate;
        if (request.date == null)
        {
            targetDate = DateOnly.FromDateTime(clock.GetUtcNow().Date);
        }
        else
        {
            // Parse the date string - this should be validated by the validator before reaching here
            if (!DateOnly.TryParseExact(request.date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out targetDate))
            {
                // This shouldn't happen due to validation, but just in case
                return new Error("invalid_date", "Invalid date format.", ErrorKind.Validation);
            }
        }

        // Query sales for the specific date (tenant isolation is automatic via EF Core global filters)
        // Compare using date ranges to handle timezone issues properly
        var startDate = targetDate.ToDateTime(TimeOnly.MinValue);
        var endDate = targetDate.ToDateTime(TimeOnly.MaxValue).AddDays(1).AddTicks(-1); // End of the day
        
        var sales = db.Sales.Where(s => s.CreatedAt >= startDate && s.CreatedAt <= endDate);
        
        // Compute aggregates
        var saleCount = await sales.CountAsync(cancellationToken);
        var netTotal = await sales.SumAsync(s => s.NetHalalas, cancellationToken);
        var vatTotal = await sales.SumAsync(s => s.VatHalalas, cancellationToken);
        var grossTotal = await sales.SumAsync(s => s.TotalHalalas, cancellationToken);

        return new DailySalesResponse(
            date: targetDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            saleCount: saleCount,
            netTotal: netTotal,
            vatTotal: vatTotal,
            grossTotal: grossTotal
        );
    }
}
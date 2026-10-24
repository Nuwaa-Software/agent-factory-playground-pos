using MediatR;
using MiniPos.Domain.Common;

namespace MiniPos.Api.Features.Reports.SalesAnalytics;

public sealed record SalesAnalyticsQuery(string? startDate, string? endDate, int? topN)
    : IRequest<Result<SalesAnalyticsResponse>>;

public sealed record SalesAnalyticsResponse(
    string startDate,
    string endDate,
    int totalSales,
    long totalNetHalalas,
    long totalVatHalalas,
    long totalGrossHalalas,
    long averageTransactionHalalas,
    IReadOnlyList<TopProduct> topProducts,
    IReadOnlyList<RevenueByHour> revenueByHour);

public sealed record TopProduct(
    Guid productId,
    string nameEn,
    int quantitySold,
    long revenueHalalas);

public sealed record RevenueByHour(
    int hour,
    long grossHalalas);

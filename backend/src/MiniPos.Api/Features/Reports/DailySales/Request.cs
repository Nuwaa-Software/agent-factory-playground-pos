using MediatR;
using MiniPos.Domain.Common;

namespace MiniPos.Api.Features.Reports.DailySales;

public sealed record DailySalesQuery(string? date) : IRequest<Result<DailySalesResponse>>;

public sealed record DailySalesResponse(string date, int saleCount, long netTotal, long vatTotal, long grossTotal);
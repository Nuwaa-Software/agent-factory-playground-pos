using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MiniPos.Api.Auth;
using MiniPos.Api.Common;
using MiniPos.Api.Features.Reports.SalesAnalytics;
using MiniPos.Api.Validation;

namespace MiniPos.Api.Features.Reports.SalesAnalytics;

public static class Endpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/pos/reports/analytics", Handle)
            .AddEndpointFilter<ValidationFilter<SalesAnalyticsQuery>>()
            .RequireAuthorization(Policies.CompanyAdmin);
    }

    private static async Task<IResult> Handle(
        [AsParameters] SalesAnalyticsQuery query,
        ISender sender)
    {
        var result = await sender.Send(query);
        return ResultHttp.ToHttp(result, r => Results.Ok(r));
    }
}

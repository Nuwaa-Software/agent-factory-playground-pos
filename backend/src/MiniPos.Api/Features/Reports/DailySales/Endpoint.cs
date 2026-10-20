using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MiniPos.Api.Auth;
using MiniPos.Api.Common;
using MiniPos.Api.Features.Reports.DailySales;
using MiniPos.Api.Validation;

namespace MiniPos.Api.Features.Reports.DailySales;

public static class Endpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/pos/reports/daily-sales", Handle)
            .AddEndpointFilter<ValidationFilter<DailySalesQuery>>()
            .RequireAuthorization(Policies.PosCashier);
    }

    private static async Task<IResult> Handle(
        [AsParameters] DailySalesQuery query,
        ISender sender)
    {
        var result = await sender.Send(query);
        return ResultHttp.ToHttp(result, r => Results.Ok(r));
    }
}
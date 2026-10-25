using MediatR;
using MiniPos.Api.Auth;
using MiniPos.Api.Common;
using MiniPos.Api.Features.Sales.DiscountLimits;
using MiniPos.Api.Validation;

namespace MiniPos.Api.Features.Sales.DiscountLimits;

public static class Endpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        // GET /api/pos/discount-limits
        app.MapGet("/api/pos/discount-limits", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetDiscountLimitsQuery(), ct);
            return result.ToHttp(r => Results.Ok(r));
        })
        .RequireAuthorization(Policies.CompanyAdmin)
        .WithName("GetDiscountLimits");

        // PUT /api/pos/discount-limits
        app.MapPut("/api/pos/discount-limits", async (UpdateDiscountLimitsCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToHttp(r => Results.Ok(r));
        })
        .AddEndpointFilter<ValidationFilter<UpdateDiscountLimitsCommand>>()
        .RequireAuthorization(Policies.CompanyAdmin)
        .WithName("UpdateDiscountLimits");
    }
}

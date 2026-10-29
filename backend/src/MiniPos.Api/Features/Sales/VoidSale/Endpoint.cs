using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MiniPos.Api.Auth;
using MiniPos.Api.Common;
using MiniPos.Api.Features.Sales.VoidSale;
using MiniPos.Api.Validation;

namespace MiniPos.Api.Features.Sales.VoidSale;

public static class Endpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/pos/sales/{id:guid}/void", async (Guid id, [FromBody] VoidSaleRequest? body, ISender sender, ClaimsPrincipal user) =>
        {
            var actor = user.FindFirstValue(Claims.UserName) ?? user.FindFirstValue(ClaimTypes.Role);
            var result = await sender.Send(new VoidSaleCommand(id, body?.Reason, actor));
            return result.ToHttp(sale => Results.Ok(sale));
        })
        .AddEndpointFilter<ValidationFilter<VoidSaleRequest>>()
        .RequireAuthorization(Policies.CompanyAdmin);
    }
}
using System.Security.Claims;
using MediatR;
using MiniPos.Api.Auth;
using MiniPos.Api.Common;
using MiniPos.Api.Features.Sales.RefundSale;

namespace MiniPos.Api.Features.Sales.RefundSale;

public static class Endpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/pos/sales/{id:guid}/refund", async (
                Guid id,
                RefundSaleCommand command,
                ISender sender,
                CancellationToken ct,
                ClaimsPrincipal user) =>
            {
                var actor = user.FindFirstValue(Claims.UserName) ?? user.FindFirstValue(ClaimTypes.Role) ?? "system";
                var result = await sender.Send(new RefundSaleCommand(id, command.LineItems, command.Reason, actor), ct);
                return result.ToHttp(r => Results.Created($"/api/pos/refunds/{r.Id}", r));
            })
            .RequireAuthorization(Policies.CompanyAdmin);
    }
}

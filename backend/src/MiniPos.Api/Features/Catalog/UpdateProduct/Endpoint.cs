using System.Security.Claims;
using MediatR;
using MiniPos.Api.Auth;
using MiniPos.Api.Common;
using MiniPos.Api.Features.Catalog.UpdateProduct;
using MiniPos.Api.Validation;

namespace MiniPos.Api.Features.Catalog.UpdateProduct;

public static class Endpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPatch("/api/inventory/products/{id:guid}", async (Guid id, UpdateProductCommand command, ISender sender,
                CancellationToken ct, ClaimsPrincipal user) =>
        {
            var actor = user.FindFirstValue(Claims.UserName) ?? user.FindFirstValue(ClaimTypes.Role);
            var result = await sender.Send(command with { Id = id, Actor = actor }, ct);
            return result.ToHttp(p => Results.Ok(p));
        })
            .AddEndpointFilter<ValidationFilter<UpdateProductCommand>>()
            .RequireAuthorization(Policies.CompanyAdmin)
            .WithName("UpdateProduct");
}
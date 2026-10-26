using System.Security.Claims;
using MediatR;
using MiniPos.Api.Auth;
using MiniPos.Api.Common;
using MiniPos.Api.Features.Users.UpdateUser;

namespace MiniPos.Api.Features.Users.UpdateUser;

public static class Endpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPatch("/api/users/{id}", async (Guid id, UpdateUserCommand command, ISender sender, CancellationToken ct, ClaimsPrincipal user) =>
        {
            var actor = user.FindFirstValue(Claims.UserName) ?? user.FindFirstValue(ClaimTypes.Role);
            var result = await sender.Send(new UpdateUserQuery(id, command with { Actor = actor }), ct);
            return result.ToHttp(p => Results.Ok(p));
        })
        .RequireAuthorization(Policies.CompanyAdmin)
        .WithName("UpdateUser");
}

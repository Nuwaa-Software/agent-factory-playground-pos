using System.Security.Claims;
using MediatR;
using MiniPos.Api.Auth;
using MiniPos.Api.Common;
using MiniPos.Api.Features.Users.CreateUser;
using MiniPos.Api.Validation;

namespace MiniPos.Api.Features.Users.CreateUser;

public static class Endpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/users", async (CreateUserCommand command, ISender sender, CancellationToken ct, ClaimsPrincipal user) =>
        {
            var actor = user.FindFirstValue(Claims.UserName) ?? user.FindFirstValue(ClaimTypes.Role);
            var result = await sender.Send(command with { Actor = actor }, ct);
            return result.ToHttp(p => Results.Created($"/api/users/{p.Id}", p));
        })
        .AddEndpointFilter<ValidationFilter<CreateUserCommand>>()
        .RequireAuthorization(Policies.CompanyAdmin)
        .WithName("CreateUser");
}

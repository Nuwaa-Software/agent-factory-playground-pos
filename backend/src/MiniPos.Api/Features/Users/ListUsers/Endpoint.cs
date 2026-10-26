using MediatR;
using MiniPos.Api.Auth;

namespace MiniPos.Api.Features.Users.ListUsers;

public static class Endpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/users", async (ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListUsersQuery(), ct)))
            .RequireAuthorization(Policies.CompanyAdmin)
            .WithName("ListUsers");
}

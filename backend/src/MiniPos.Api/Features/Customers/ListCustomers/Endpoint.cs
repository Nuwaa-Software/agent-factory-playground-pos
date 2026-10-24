using MediatR;
using MiniPos.Api.Auth;

namespace MiniPos.Api.Features.Customers.ListCustomers;

public static class Endpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/customers", async (ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListCustomersQuery(), ct)))
            .RequireAuthorization(Policies.CatalogRead)
            .WithName("ListCustomers");
}

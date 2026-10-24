using MediatR;
using MiniPos.Api.Auth;

namespace MiniPos.Api.Features.Catalog.ListProducts;

public static class Endpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/inventory/products", async (ISender sender, string? q, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListProductsQuery(q), ct)))
            .RequireAuthorization(Policies.CatalogRead)
            .WithName("ListProducts");
}

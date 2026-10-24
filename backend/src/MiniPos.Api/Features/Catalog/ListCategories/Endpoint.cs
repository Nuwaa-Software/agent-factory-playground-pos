using MediatR;
using MiniPos.Api.Auth;
using MiniPos.Api.Common;

namespace MiniPos.Api.Features.Catalog.ListCategories;

public static class ListCategoriesEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/inventory/categories", async (ISender sender, CancellationToken ct) =>
            await sender.Send(new ListCategoriesQuery(), ct))
            .RequireAuthorization(Policies.CatalogRead)
            .WithName("ListCategories");
    }
}
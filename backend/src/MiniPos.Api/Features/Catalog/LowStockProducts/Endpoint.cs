using MediatR;
using MiniPos.Api.Auth;
using MiniPos.Api.Common;
using MiniPos.Api.Features.Catalog.LowStockProducts;
using MiniPos.Api.Validation;

namespace MiniPos.Api.Features.Catalog.LowStockProducts;

public static class Endpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/pos/catalog/low-stock", Handle)
            .AddEndpointFilter<ValidationFilter<LowStockProductsQuery>>()
            .RequireAuthorization(Policies.CatalogRead)
            .WithName("LowStockProducts");
    }

    private static async Task<IResult> Handle(
        [AsParameters] LowStockProductsQuery query,
        ISender sender)
    {
        var result = await sender.Send(query);
        return ResultHttp.ToHttp(result, r => Results.Ok(r));
    }
}
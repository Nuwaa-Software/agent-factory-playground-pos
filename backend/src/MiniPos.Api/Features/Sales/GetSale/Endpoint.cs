using MediatR;
using MiniPos.Api.Auth;
using MiniPos.Api.Common;

namespace MiniPos.Api.Features.Sales.GetSale;

public static class Endpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/pos/sales/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetSaleQuery(id), ct)).ToHttp(Results.Ok))
            .RequireAuthorization(Policies.PosCashier)
            .WithName("GetSale");
}

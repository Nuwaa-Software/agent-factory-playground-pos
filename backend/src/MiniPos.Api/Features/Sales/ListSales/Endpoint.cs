using MediatR;
using MiniPos.Api.Auth;

namespace MiniPos.Api.Features.Sales.ListSales;

public static class Endpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/pos/sales", async (ISender sender, int? page, int? pageSize, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListSalesQuery { Page = page ?? 1, PageSize = pageSize ?? 20 }, ct)))
            .RequireAuthorization(Policies.PosCashier)
            .WithName("ListSales");
}
using MediatR;
using MiniPos.Api.Auth;

namespace MiniPos.Api.Features.Sales.ListSales;

public static class Endpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/pos/sales", async (ISender sender, int? page, int? pageSize, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListSalesQuery { Page = page ?? 1, PageSize = pageSize ?? 20, From = from, To = to }, ct)))
            .RequireAuthorization(Policies.PosCashier)
            .WithName("ListSales");
}
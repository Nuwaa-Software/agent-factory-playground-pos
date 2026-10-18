using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MiniPos.Api.Common;
using MediatR;

namespace MiniPos.Api.Features.Sales.GetReceipt;

public static class Endpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/pos/sales/{id:guid}/receipt", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetReceiptQuery(id), ct);
                return result.ToHttp(s => Results.Text(s));
            })
            .RequireAuthorization("PosCashier")
            .WithName("GetReceipt");
    }
}
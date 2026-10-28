using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MiniPos.Api.Auth;
using MiniPos.Api.Common;
using MiniPos.Api.Features.Sales.VoidSale;
using MiniPos.Api.Validation;
using MediatR;

namespace MiniPos.Api.Features.Sales.VoidSale;

public static class Endpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/pos/sales/{id:guid}/void", async (Guid id, [FromBody] VoidSaleRequest? body, ISender sender) =>
        {
            var result = await sender.Send(new VoidSaleCommand(id, body?.Reason));
            return result.ToHttp(sale => Results.Ok(sale));
        })
        .AddEndpointFilter<ValidationFilter<VoidSaleRequest>>()
        .RequireAuthorization(Policies.CompanyAdmin);
    }
}
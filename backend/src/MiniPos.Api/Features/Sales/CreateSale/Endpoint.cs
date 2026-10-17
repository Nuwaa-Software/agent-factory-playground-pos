using MediatR;
using MiniPos.Api.Auth;
using MiniPos.Api.Common;
using MiniPos.Api.Validation;

namespace MiniPos.Api.Features.Sales.CreateSale;

public static class Endpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/pos/sales", async (CreateSaleCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToHttp(s => Results.Created($"/api/pos/sales/{s.Id}", s)))
            .AddEndpointFilter<ValidationFilter<CreateSaleCommand>>()
            .RequireAuthorization(Policies.PosCashier)
            .WithName("CreateSale");
}

using MediatR;
using MiniPos.Api.Auth;
using MiniPos.Api.Common;
using MiniPos.Api.Validation;

namespace MiniPos.Api.Features.Catalog.UpdateProduct;

public static class Endpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPatch("/api/inventory/products/{id:guid}", async (Guid id, UpdateProductCommand command, ISender sender,
                CancellationToken ct) =>
            (await sender.Send(command with { Id = id }, ct)).ToHttp(p => Results.Ok(p)))
            .AddEndpointFilter<ValidationFilter<UpdateProductCommand>>()
            .RequireAuthorization(Policies.CompanyAdmin)
            .WithName("UpdateProduct");
}
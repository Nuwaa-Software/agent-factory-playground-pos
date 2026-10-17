using MediatR;
using MiniPos.Api.Auth;
using MiniPos.Api.Common;
using MiniPos.Api.Validation;

namespace MiniPos.Api.Features.Catalog.CreateProduct;

public static class Endpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/inventory/products", async (CreateProductCommand command, ISender sender,
                CancellationToken ct) =>
            (await sender.Send(command, ct)).ToHttp(p => Results.Created($"/api/inventory/products/{p.Id}", p)))
            .AddEndpointFilter<ValidationFilter<CreateProductCommand>>()
            .RequireAuthorization(Policies.CompanyAdmin)
            .WithName("CreateProduct");
}

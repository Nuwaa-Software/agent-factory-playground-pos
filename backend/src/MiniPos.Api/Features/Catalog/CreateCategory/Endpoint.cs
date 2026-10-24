using MediatR;
using MiniPos.Api.Auth;
using MiniPos.Api.Common;
using MiniPos.Api.Validation;

namespace MiniPos.Api.Features.Catalog.CreateCategory;

public static class CreateCategoryEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/inventory/categories", async (CreateCategoryCommand command, ISender sender,
                CancellationToken ct) =>
            (await sender.Send(command, ct)).ToHttp(p => Results.Created($"/api/inventory/categories/{p.Id}", p)))
            .AddEndpointFilter<ValidationFilter<CreateCategoryCommand>>()
            .RequireAuthorization(Policies.CompanyAdmin)
            .WithName("CreateCategory");
    }
}
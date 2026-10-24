using MediatR;
using MiniPos.Api.Auth;
using MiniPos.Api.Common;
using MiniPos.Api.Validation;

namespace MiniPos.Api.Features.Catalog.UpdateCategory;

public static class UpdateCategoryEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/inventory/categories/{id:guid}", async (Guid id, UpdateCategoryCommand command, ISender sender,
                CancellationToken ct) =>
            (await sender.Send(command, ct)).ToHttp(p => Results.Ok(p)))
            .AddEndpointFilter<ValidationFilter<UpdateCategoryCommand>>()
            .RequireAuthorization(Policies.CompanyAdmin)
            .WithName("UpdateCategory");
    }
}
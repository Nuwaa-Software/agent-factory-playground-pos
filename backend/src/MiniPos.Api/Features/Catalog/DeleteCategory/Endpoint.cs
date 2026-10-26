using MediatR;
using MiniPos.Api.Auth;
using MiniPos.Api.Common;
using MiniPos.Api.Validation;

namespace MiniPos.Api.Features.Catalog.DeleteCategory;

public static class DeleteCategoryEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/inventory/categories/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteCategoryCommand(id), ct);

            if (!result.IsSuccess)
                return result.ToHttp(_ => Results.NoContent());

            return Results.NoContent();
        })
        .RequireAuthorization(Policies.CompanyAdmin)
        .WithName("DeleteCategory");
    }
}

using MediatR;
using MiniPos.Api.Auth;
using MiniPos.Api.Common;
using MiniPos.Api.Validation;

namespace MiniPos.Api.Features.Company.UpdateSettings;

public static class Endpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPatch("/api/company/settings", async (UpdateSettingsCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToHttp(s => Results.Ok(s)))
            .AddEndpointFilter<ValidationFilter<UpdateSettingsCommand>>()
            .RequireAuthorization(Policies.CompanyAdmin)
            .WithName("UpdateCompanySettings");
}

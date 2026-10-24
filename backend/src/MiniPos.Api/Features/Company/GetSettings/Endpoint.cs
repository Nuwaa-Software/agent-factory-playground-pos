using MediatR;
using MiniPos.Api.Auth;
using MiniPos.Api.Common;
using MiniPos.Api.Validation;

namespace MiniPos.Api.Features.Company.GetSettings;

public static class Endpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/company/settings", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetSettingsQuery(), ct)).ToHttp(s => Results.Ok(s)))
            .AddEndpointFilter<ValidationFilter<GetSettingsQuery>>()
            .RequireAuthorization(Policies.CompanyAdmin)
            .WithName("GetCompanySettings");
}

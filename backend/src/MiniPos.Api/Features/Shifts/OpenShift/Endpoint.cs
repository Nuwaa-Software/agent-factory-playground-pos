using MediatR;
using MiniPos.Api.Auth;
using MiniPos.Api.Common;
using MiniPos.Api.Features.Shifts.OpenShift;
using MiniPos.Api.Validation;

namespace MiniPos.Api.Features.Shifts;

public static class OpenShiftEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/pos/shifts/open", async (OpenShiftCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToHttp(s => Results.Created($"/api/pos/shifts/{s.Id}", s)))
            .AddEndpointFilter<ValidationFilter<OpenShiftCommand>>()
            .RequireAuthorization(Policies.PosCashier)
            .WithName("OpenShift");
}
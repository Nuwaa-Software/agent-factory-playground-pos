using MediatR;
using MiniPos.Api.Auth;
using MiniPos.Api.Common;
using MiniPos.Api.Features.Shifts.CloseShift;
using MiniPos.Api.Validation;

namespace MiniPos.Api.Features.Shifts;

public static class CloseShiftEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPatch("/api/pos/shifts/{id:guid}/close", async (Guid id, CloseShiftCommand command, ISender sender, CancellationToken ct) =>
                {
                    command = command with { Id = id };
                    var result = await sender.Send(command, ct);
                    return result.ToHttp(s => Results.Ok(s));
                })
            .AddEndpointFilter<ValidationFilter<CloseShiftCommand>>()
            .RequireAuthorization(Policies.PosCashier)
            .WithName("CloseShift");
}
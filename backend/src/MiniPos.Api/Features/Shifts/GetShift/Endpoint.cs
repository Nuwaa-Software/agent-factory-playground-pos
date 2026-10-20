using MediatR;
using MiniPos.Api.Auth;
using MiniPos.Api.Common;
using MiniPos.Api.Features.Shifts.GetShift;

namespace MiniPos.Api.Features.Shifts;

public static class GetShiftEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/pos/shifts/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetShiftQuery(id), ct)).ToHttp(Results.Ok))
            .RequireAuthorization(Policies.PosCashier)
            .WithName("GetShift");
}
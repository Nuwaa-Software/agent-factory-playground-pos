using MediatR;
using MiniPos.Api.Auth;
using MiniPos.Api.Common;
using MiniPos.Api.Features.Shifts.ListShifts;

namespace MiniPos.Api.Features.Shifts;

public static class ListShiftsEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/pos/shifts", async (string? status, ISender sender, CancellationToken ct) =>
                await sender.Send(new ListShiftsQuery(status), ct))
            .RequireAuthorization(Policies.PosCashier)
            .WithName("ListShifts");
}
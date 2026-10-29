using MediatR;
using MiniPos.Api.Auth;
using MiniPos.Api.Features.Audit.ListAuditLog;

namespace MiniPos.Api.Features.Audit.ListAuditLog;

public static class Endpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/audit-log", async (ISender sender, int? page, int? pageSize, string? entityName, Guid? entityId, DateTimeOffset? startDate, DateTimeOffset? endDate, string? action, CancellationToken ct) =>
            Results.Ok(await sender.Send(new ListAuditLogQuery
            {
                Page = page ?? 1,
                PageSize = pageSize ?? 50,
                EntityName = entityName,
                EntityId = entityId,
                StartDate = startDate,
                EndDate = endDate,
                Action = action,
            }, ct)))
        .RequireAuthorization(Policies.CompanyAdmin)
        .WithName("ListAuditLog");
}

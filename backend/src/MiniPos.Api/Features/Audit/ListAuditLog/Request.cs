using MediatR;
using MiniPos.Domain.Audit;

namespace MiniPos.Api.Features.Audit.ListAuditLog;

public sealed record ListAuditLogQuery : IRequest<ListAuditLogResponse>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public string? EntityName { get; init; }
    public Guid? EntityId { get; init; }
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? EndDate { get; init; }
    public string? Action { get; init; }
}

public sealed record ListAuditLogResponse(
    IReadOnlyList<AuditLogEntry> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record AuditLogEntry(
    Guid Id,
    Guid CompanyId,
    string Action,
    string EntityName,
    Guid EntityId,
    string ActorName,
    string Details,
    DateTimeOffset CreatedAt);

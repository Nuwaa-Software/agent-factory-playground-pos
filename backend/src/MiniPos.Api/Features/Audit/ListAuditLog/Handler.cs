using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Domain.Audit;
using MiniPos.Infrastructure.Persistence;

namespace MiniPos.Api.Features.Audit.ListAuditLog;

public sealed class ListAuditLogHandler(AppDbContext db) : IRequestHandler<ListAuditLogQuery, ListAuditLogResponse>
{
    public async Task<ListAuditLogResponse> Handle(ListAuditLogQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var query = db.AuditLogs.AsQueryable();

        if (request.EntityName is not null)
        {
            query = query.Where(a => a.EntityName == request.EntityName);
        }

        if (request.EntityId.HasValue)
        {
            query = query.Where(a => a.EntityId == request.EntityId.Value);
        }

        if (request.Action is not null)
        {
            query = query.Where(a => a.Action == request.Action);
        }

        if (request.StartDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt <= request.EndDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogEntry(
                a.Id,
                a.CompanyId,
                a.Action,
                a.EntityName,
                a.EntityId,
                a.ActorName,
                a.Details,
                a.CreatedAt))
            .ToListAsync(cancellationToken);

        return new ListAuditLogResponse(items, page, pageSize, totalCount, totalPages);
    }
}

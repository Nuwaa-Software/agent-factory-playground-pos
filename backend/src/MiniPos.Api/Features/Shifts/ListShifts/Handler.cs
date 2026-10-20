using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Features.Shifts;
using MiniPos.Infrastructure.Persistence;

namespace MiniPos.Api.Features.Shifts.ListShifts;

public sealed class ListShiftsHandler(AppDbContext db) : IRequestHandler<ListShiftsQuery, IReadOnlyList<ShiftResponse>>
{
    public async Task<IReadOnlyList<ShiftResponse>> Handle(ListShiftsQuery query, CancellationToken cancellationToken)
    {
        var shifts = db.Shifts.AsQueryable();

        if (query.Status is not null)
        {
            if (query.Status == "open")
                shifts = shifts.Where(s => s.ClosedAt == null);
            else if (query.Status == "closed")
                shifts = shifts.Where(s => s.ClosedAt != null);
        }

        var shiftList = await shifts
            .OrderByDescending(s => s.OpenedAt)
            .ToListAsync(cancellationToken);

        return shiftList.Select(s => new ShiftResponse(
            s.Id,
            s.CashierName,
            s.StartingCashHalalas,
            s.OpenedAt,
            s.ClosedAt,
            s.ActualCashHalalas,
            s.ExpectedCashHalalas,
            s.DiscrepancyHalalas)).ToList();
    }
}
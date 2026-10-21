using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Features.Shifts;
using MiniPos.Domain.Common;
using MiniPos.Domain.Shifts;
using MiniPos.Infrastructure.Persistence;

namespace MiniPos.Api.Features.Shifts.GetShift;

public sealed class GetShiftHandler(AppDbContext db) : IRequestHandler<GetShiftQuery, Result<ShiftResponse>>
{
    public async Task<Result<ShiftResponse>> Handle(GetShiftQuery query, CancellationToken cancellationToken)
    {
        // Use explicit query to respect tenant filter
        var shift = await db.Shifts.Where(s => s.Id == query.Id).FirstOrDefaultAsync(cancellationToken);
        
        if (shift is null)
            return Result<ShiftResponse>.Failure(new Error("NotFound", "Shift not found.", ErrorKind.NotFound));

        // Check if the shift belongs to current company (tenant isolation)
        if (shift.CompanyId != db.CurrentCompanyId)
            return Result<ShiftResponse>.Failure(new Error("NotFound", "Shift not found.", ErrorKind.NotFound));

        return Result<ShiftResponse>.Success(new ShiftResponse(
            shift.Id,
            shift.CashierName,
            shift.StartingCashHalalas,
            shift.OpenedAt,
            shift.ClosedAt,
            shift.ActualCashHalalas,
            shift.ExpectedCashHalalas,
            shift.DiscrepancyHalalas));
    }
}
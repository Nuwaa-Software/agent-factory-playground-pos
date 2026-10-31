using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Features.Shifts;
using MiniPos.Domain.Common;
using MiniPos.Domain.Sales;
using MiniPos.Domain.Shifts;
using MiniPos.Infrastructure.Persistence;

namespace MiniPos.Api.Features.Shifts.CloseShift;

public sealed class CloseShiftHandler(AppDbContext db, TimeProvider clock) : IRequestHandler<CloseShiftCommand, Result<ShiftResponse>>
{
    public async Task<Result<ShiftResponse>> Handle(CloseShiftCommand command, CancellationToken cancellationToken)
    {
        // Use explicit query to respect tenant filter
        var shift = await db.Shifts.Where(s => s.Id == command.Id).FirstOrDefaultAsync(cancellationToken);
        
        if (shift is null)
            return Result<ShiftResponse>.Failure(new Error("NotFound", "Shift not found.", ErrorKind.NotFound));

        // Check if the shift belongs to current company (tenant isolation)
        if (shift.CompanyId != db.CurrentCompanyId)
            return Result<ShiftResponse>.Failure(new Error("NotFound", "Shift not found.", ErrorKind.NotFound));

        if (shift.ClosedAt is not null)
            return Result<ShiftResponse>.Failure(new Error("Conflict", "Shift is already closed.", ErrorKind.Conflict));

        shift.ClosedAt = clock.GetUtcNow();
        shift.ActualCashHalalas = command.ActualCashHalalas;

        // Compute expected cash from non-voided cash sales in the shift window.
        // Voided sales are excluded (cash returned to customer, never in the drawer).
        // TotalHalalas already reflects discounts and VAT, so we use it directly.
        var salesInWindow = await db.Sales
            .Where(s => s.CreatedAt >= shift.OpenedAt && s.CreatedAt <= shift.ClosedAt.Value
                        && s.PaymentType == PaymentType.Cash)
            .ToListAsync(cancellationToken);

        var nonVoidedCashTotal = salesInWindow.Where(s => !s.Voided).Sum(s => (long?)s.TotalHalalas) ?? 0;

        shift.ExpectedCashHalalas = shift.StartingCashHalalas + nonVoidedCashTotal;
        shift.DiscrepancyHalalas = command.ActualCashHalalas - shift.ExpectedCashHalalas;

        await db.SaveChangesAsync(cancellationToken);

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
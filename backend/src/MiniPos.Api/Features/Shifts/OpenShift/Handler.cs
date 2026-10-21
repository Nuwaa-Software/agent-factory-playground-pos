using MediatR;
using MiniPos.Domain.Common;
using MiniPos.Domain.Shifts;
using MiniPos.Infrastructure.Persistence;

namespace MiniPos.Api.Features.Shifts.OpenShift;

public sealed class OpenShiftHandler(AppDbContext db, TimeProvider clock) : IRequestHandler<OpenShiftCommand, Result<ShiftResponse>>
{
    public async Task<Result<ShiftResponse>> Handle(OpenShiftCommand command, CancellationToken cancellationToken)
    {
        var shift = new Shift
        {
            CompanyId = db.CurrentCompanyId,
            CashierName = command.CashierName,
            StartingCashHalalas = command.StartingCashHalalas,
            OpenedAt = clock.GetUtcNow()
        };

        db.Shifts.Add(shift);
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
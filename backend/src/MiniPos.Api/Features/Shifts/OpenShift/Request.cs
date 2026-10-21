using MediatR;
using MiniPos.Api.Features.Shifts;
using MiniPos.Domain.Common;

namespace MiniPos.Api.Features.Shifts.OpenShift;

public record OpenShiftCommand(string CashierName, long StartingCashHalalas) : IRequest<Result<ShiftResponse>>;
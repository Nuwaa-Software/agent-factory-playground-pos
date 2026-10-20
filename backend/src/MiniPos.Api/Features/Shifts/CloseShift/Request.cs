using MediatR;
using MiniPos.Api.Features.Shifts;
using MiniPos.Domain.Common;

namespace MiniPos.Api.Features.Shifts.CloseShift;

public record CloseShiftCommand(Guid Id, long ActualCashHalalas) : IRequest<Result<ShiftResponse>>;
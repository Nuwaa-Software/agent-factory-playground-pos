using MediatR;
using MiniPos.Api.Features.Shifts;
using MiniPos.Domain.Common;

namespace MiniPos.Api.Features.Shifts.GetShift;

public record GetShiftQuery(Guid Id) : IRequest<Result<ShiftResponse>>;
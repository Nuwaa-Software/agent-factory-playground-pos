using MediatR;
using MiniPos.Api.Features.Shifts;

namespace MiniPos.Api.Features.Shifts.ListShifts;

public record ListShiftsQuery(string? Status) : IRequest<IReadOnlyList<ShiftResponse>>;
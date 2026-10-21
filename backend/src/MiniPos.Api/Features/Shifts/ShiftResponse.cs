namespace MiniPos.Api.Features.Shifts;

public record ShiftResponse(
    Guid Id,
    string CashierName,
    long StartingCashHalalas,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt,
    long? ActualCashHalalas,
    long? ExpectedCashHalalas,
    long? DiscrepancyHalalas
);
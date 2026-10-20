using MiniPos.Domain.Common;

namespace MiniPos.Domain.Shifts;

public sealed class Shift : ITenantScoped
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid CompanyId { get; init; }

    public required string CashierName { get; init; }

    public long StartingCashHalalas { get; init; }

    public DateTimeOffset OpenedAt { get; init; }

    public DateTimeOffset? ClosedAt { get; set; }

    public long? ActualCashHalalas { get; set; }

    public long? ExpectedCashHalalas { get; set; }

    public long? DiscrepancyHalalas { get; set; }
}
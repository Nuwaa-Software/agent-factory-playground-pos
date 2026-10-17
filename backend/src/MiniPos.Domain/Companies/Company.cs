namespace MiniPos.Domain.Companies;

/// <summary>A merchant (tenant). Not tenant-scoped itself: it IS the tenant.</summary>
public sealed class Company
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string NameEn { get; set; }

    public required string NameAr { get; set; }

    /// <summary>15-digit Saudi VAT registration number (starts and ends with 3).</summary>
    public required string VatNumber { get; set; }
}

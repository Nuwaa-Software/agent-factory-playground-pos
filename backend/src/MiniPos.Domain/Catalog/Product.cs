using MiniPos.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace MiniPos.Domain.Catalog;

public sealed class Product : ITenantScoped
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid CompanyId { get; init; }

    public required string NameEn { get; set; }

    public required string NameAr { get; set; }

    public required string Sku { get; set; }

    /// <summary>Net unit price (VAT excluded) in halalas (1 SAR = 100 halalas). Money is never a float.</summary>
    public long PriceHalalas { get; set; }

    [ConcurrencyCheck]
    public int Stock { get; set; }

    public Guid? CategoryId { get; set; }
}

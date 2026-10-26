using MiniPos.Domain.Common;

namespace MiniPos.Domain.Users;

public sealed class User : ITenantScoped
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid CompanyId { get; init; }

    public required string NameEn { get; set; }

    public required string NameAr { get; set; }

    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;

    public required string CreatedBy { get; set; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public string? ModifiedBy { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }
}

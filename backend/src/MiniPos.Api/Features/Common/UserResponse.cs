using MiniPos.Domain.Users;

namespace MiniPos.Api.Features.Common;

public sealed record UserResponse(
    Guid Id,
    string NameEn,
    string NameAr,
    UserRole Role,
    bool IsActive,
    string CreatedBy,
    DateTimeOffset CreatedAt,
    string? ModifiedBy,
    DateTimeOffset? ModifiedAt);

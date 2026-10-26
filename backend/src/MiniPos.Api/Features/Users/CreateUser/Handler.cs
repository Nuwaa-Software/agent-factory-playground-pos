using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Features.Common;
using MiniPos.Domain.Common;
using MiniPos.Domain.Users;
using MiniPos.Infrastructure.Persistence;
using MiniPos.Infrastructure.Tenancy;

namespace MiniPos.Api.Features.Users.CreateUser;

public sealed class CreateUserHandler(
    AppDbContext db,
    ITenantContext tenant,
    TimeProvider time)
    : IRequestHandler<CreateUserCommand, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = new User
        {
            CompanyId = tenant.CompanyId,
            NameEn = request.NameEn,
            NameAr = request.NameAr,
            Role = request.Role,
            IsActive = true,
            CreatedBy = request.Actor ?? "Admin",
            CreatedAt = time.GetUtcNow().UtcDateTime,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return new UserResponse(
            user.Id,
            user.NameEn,
            user.NameAr,
            user.Role,
            user.IsActive,
            user.CreatedBy,
            user.CreatedAt,
            user.ModifiedBy,
            user.ModifiedAt);
    }
}

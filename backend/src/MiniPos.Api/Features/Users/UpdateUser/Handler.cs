using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Features.Common;
using MiniPos.Domain.Common;
using MiniPos.Domain.Users;
using MiniPos.Infrastructure.Persistence;
using MiniPos.Infrastructure.Tenancy;

namespace MiniPos.Api.Features.Users.UpdateUser;

public sealed class UpdateUserHandler(
    AppDbContext db,
    TimeProvider time)
    : IRequestHandler<UpdateUserQuery, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> Handle(UpdateUserQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
        if (user is null)
        {
            return new Error("user.not_found", "User not found.", ErrorKind.NotFound);
        }

        var changed = false;

        if (request.Command.Role.HasValue && request.Command.Role.Value != user.Role)
        {
            user.Role = request.Command.Role.Value;
            changed = true;
        }

        if (request.Command.IsActive.HasValue && request.Command.IsActive.Value != user.IsActive)
        {
            user.IsActive = request.Command.IsActive.Value;
            changed = true;
        }

        if (changed)
        {
            user.ModifiedBy = request.Command.Actor ?? "Admin";
            user.ModifiedAt = time.GetUtcNow().UtcDateTime;
            await db.SaveChangesAsync(cancellationToken);
        }

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

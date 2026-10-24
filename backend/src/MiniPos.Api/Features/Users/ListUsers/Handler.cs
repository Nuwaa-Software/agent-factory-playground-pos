using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Features.Common;
using MiniPos.Infrastructure.Persistence;

namespace MiniPos.Api.Features.Users.ListUsers;

public sealed class ListUsersHandler(AppDbContext db)
    : IRequestHandler<ListUsersQuery, IReadOnlyList<UserResponse>>
{
    public async Task<IReadOnlyList<UserResponse>> Handle(ListUsersQuery request, CancellationToken cancellationToken) =>
        await db.Users
            .OrderBy(u => u.NameEn)
            .Select(u => new UserResponse(
                u.Id,
                u.NameEn,
                u.NameAr,
                u.Role,
                u.IsActive,
                u.CreatedBy,
                u.CreatedAt,
                u.ModifiedBy,
                u.ModifiedAt))
            .ToListAsync(cancellationToken);
}

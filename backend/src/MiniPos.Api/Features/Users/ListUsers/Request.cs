using MediatR;
using MiniPos.Api.Features.Common;

namespace MiniPos.Api.Features.Users.ListUsers;

public sealed record ListUsersQuery : IRequest<IReadOnlyList<UserResponse>>;

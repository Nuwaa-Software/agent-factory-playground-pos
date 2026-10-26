using MediatR;
using MiniPos.Api.Features.Common;
using MiniPos.Domain.Common;
using MiniPos.Domain.Users;

namespace MiniPos.Api.Features.Users.UpdateUser;

public sealed record UpdateUserCommand(MiniPos.Domain.Users.UserRole? Role = null, bool? IsActive = null, string? Actor = null);

public sealed record UpdateUserQuery(Guid Id, UpdateUserCommand Command) : IRequest<Result<UserResponse>>;

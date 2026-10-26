using MediatR;
using MiniPos.Api.Features.Common;
using MiniPos.Domain.Common;
using MiniPos.Domain.Users;

namespace MiniPos.Api.Features.Users.CreateUser;

public sealed record CreateUserCommand(string NameEn, string NameAr, UserRole Role, string? Actor = null)
    : IRequest<Result<UserResponse>>;

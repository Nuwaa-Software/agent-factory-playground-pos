using MediatR;
using MiniPos.Domain.Common;
using MiniPos.Api.Features.Common;

namespace MiniPos.Api.Features.Catalog.UpdateCategory;

public sealed record UpdateCategoryCommand(Guid Id, string? NameEn, string? NameAr, string? Description)
    : IRequest<Result<CategoryResponse>>;
using MediatR;
using MiniPos.Domain.Common;
using MiniPos.Api.Features.Common;

namespace MiniPos.Api.Features.Catalog.CreateCategory;

public sealed record CreateCategoryCommand(string NameEn, string NameAr, string? Description, Guid? ParentId = null)
    : IRequest<Result<CategoryResponse>>;
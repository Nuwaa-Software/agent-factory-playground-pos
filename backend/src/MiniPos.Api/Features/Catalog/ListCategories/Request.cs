using MediatR;
using MiniPos.Api.Features.Common;

namespace MiniPos.Api.Features.Catalog.ListCategories;

public sealed record ListCategoriesQuery() : IRequest<IReadOnlyList<CategoryResponse>>;
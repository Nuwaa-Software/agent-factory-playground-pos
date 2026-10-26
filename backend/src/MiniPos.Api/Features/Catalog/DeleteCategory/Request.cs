using MediatR;
using MiniPos.Domain.Common;

namespace MiniPos.Api.Features.Catalog.DeleteCategory;

public sealed record DeleteCategoryCommand(Guid Id) : IRequest<Result<Unit>>;

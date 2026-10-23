using MediatR;
using MiniPos.Api.Features.Common;
using MiniPos.Domain.Common;

namespace MiniPos.Api.Features.Customers.UpdateCustomer;

public sealed record UpdateCustomerCommand(string? Name = null, string? Phone = null);

public sealed record UpdateCustomerQuery(Guid Id, UpdateCustomerCommand Command) : IRequest<Result<CustomerResponse>>;

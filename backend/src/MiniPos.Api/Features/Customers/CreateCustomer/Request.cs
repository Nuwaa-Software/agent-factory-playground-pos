using MediatR;
using MiniPos.Api.Features.Common;
using MiniPos.Domain.Common;

namespace MiniPos.Api.Features.Customers.CreateCustomer;

public sealed record CreateCustomerCommand(string Name, string? Phone = null) : IRequest<Result<CustomerResponse>>;

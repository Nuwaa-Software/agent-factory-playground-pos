using MediatR;
using MiniPos.Api.Features.Common;
using MiniPos.Domain.Common;

namespace MiniPos.Api.Features.Customers.GetCustomer;

public sealed record GetCustomerQuery(Guid Id) : IRequest<Result<CustomerResponse>>;

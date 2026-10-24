using MediatR;
using MiniPos.Api.Features.Common;

namespace MiniPos.Api.Features.Customers.ListCustomers;

public sealed record ListCustomersQuery : IRequest<IReadOnlyList<CustomerResponse>>;

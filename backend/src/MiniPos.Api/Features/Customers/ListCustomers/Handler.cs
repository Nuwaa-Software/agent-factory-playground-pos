using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Features.Common;
using MiniPos.Infrastructure.Persistence;

namespace MiniPos.Api.Features.Customers.ListCustomers;

public sealed class ListCustomersHandler(AppDbContext db) : IRequestHandler<ListCustomersQuery, IReadOnlyList<CustomerResponse>>
{
    public async Task<IReadOnlyList<CustomerResponse>> Handle(ListCustomersQuery request, CancellationToken cancellationToken) =>
        await db.Customers
            .OrderBy(c => c.Name)
            .Select(c => new CustomerResponse(c.Id, c.Name, c.Phone, c.CreditBalanceHalalas))
            .ToListAsync(cancellationToken);
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Features.Common;
using MiniPos.Domain.Common;
using MiniPos.Infrastructure.Persistence;

namespace MiniPos.Api.Features.Customers.GetCustomer;

public sealed class GetCustomerHandler(AppDbContext db)
    : IRequestHandler<GetCustomerQuery, Result<CustomerResponse>>
{
    public async Task<Result<CustomerResponse>> Handle(GetCustomerQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var customer = await db.Customers.SingleOrDefaultAsync(
            c => c.Id == request.Id, cancellationToken);

        if (customer is null)
        {
            return new Error("customer.not_found", "Customer not found.", ErrorKind.NotFound);
        }

        return new CustomerResponse(customer.Id, customer.Name, customer.Phone, customer.CreditBalanceHalalas);
    }
}

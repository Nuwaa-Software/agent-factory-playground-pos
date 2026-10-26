using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Features.Common;
using MiniPos.Domain.Common;
using MiniPos.Domain.Customers;
using MiniPos.Infrastructure.Persistence;

namespace MiniPos.Api.Features.Customers.UpdateCustomer;

public sealed class UpdateCustomerHandler(AppDbContext db)
    : IRequestHandler<UpdateCustomerQuery, Result<CustomerResponse>>
{
    public async Task<Result<CustomerResponse>> Handle(UpdateCustomerQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var customer = await db.Customers.SingleOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (customer is null)
        {
            return new Error("customer.not_found", "Customer not found.", ErrorKind.NotFound);
        }

        customer.Name = request.Command.Name ?? customer.Name;
        customer.Phone = request.Command.Phone ?? customer.Phone;

        await db.SaveChangesAsync(cancellationToken);

        return new CustomerResponse(customer.Id, customer.Name, customer.Phone, customer.CreditBalanceHalalas);
    }
}

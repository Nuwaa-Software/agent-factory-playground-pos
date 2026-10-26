using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Features.Common;
using MiniPos.Domain.Common;
using MiniPos.Domain.Customers;
using MiniPos.Infrastructure.Persistence;
using MiniPos.Infrastructure.Tenancy;

namespace MiniPos.Api.Features.Customers.CreateCustomer;

public sealed class CreateCustomerHandler(AppDbContext db, ITenantContext tenant)
    : IRequestHandler<CreateCustomerCommand, Result<CustomerResponse>>
{
    public async Task<Result<CustomerResponse>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var customer = new Customer
        {
            CompanyId = tenant.CompanyId,
            Name = request.Name,
            Phone = request.Phone,
        };

        db.Customers.Add(customer);
        await db.SaveChangesAsync(cancellationToken);

        return new CustomerResponse(customer.Id, customer.Name, customer.Phone, customer.CreditBalanceHalalas);
    }
}

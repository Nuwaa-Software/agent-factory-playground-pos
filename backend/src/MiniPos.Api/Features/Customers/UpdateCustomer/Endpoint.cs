using MediatR;
using MiniPos.Api.Auth;
using MiniPos.Api.Common;
using MiniPos.Api.Validation;

namespace MiniPos.Api.Features.Customers.UpdateCustomer;

public static class Endpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPatch("/api/customers/{id}", async (Guid id, UpdateCustomerCommand command, ISender sender, CancellationToken ct) =>
            (await sender.Send(new UpdateCustomerQuery(id, command), ct)).ToHttp(p => Results.Ok(p)))
            .AddEndpointFilter<ValidationFilter<UpdateCustomerQuery>>()
            .RequireAuthorization(Policies.CompanyAdmin)
            .WithName("UpdateCustomer");
}

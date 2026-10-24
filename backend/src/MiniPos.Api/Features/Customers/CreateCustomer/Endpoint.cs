using MediatR;
using MiniPos.Api.Auth;
using MiniPos.Api.Common;
using MiniPos.Api.Validation;

namespace MiniPos.Api.Features.Customers.CreateCustomer;

public static class Endpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/customers", async (CreateCustomerCommand command, ISender sender, CancellationToken ct) =>
            (await sender.Send(command, ct)).ToHttp(p => Results.Created($"/api/customers/{p.Id}", p)))
            .AddEndpointFilter<ValidationFilter<CreateCustomerCommand>>()
            .RequireAuthorization(Policies.CompanyAdmin)
            .WithName("CreateCustomer");
}

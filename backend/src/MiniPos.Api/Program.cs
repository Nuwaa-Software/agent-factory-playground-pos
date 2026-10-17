using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using MiniPos.Api.Auth;
using MiniPos.Api.Features;
using MiniPos.Infrastructure;
using MiniPos.Infrastructure.Persistence;
using MiniPos.Infrastructure.Tenancy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is not set."));
builder.Services.AddMediatR(c => c.RegisterServicesFromAssemblyContaining<Program>());
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddAuthentication(HeaderAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>(HeaderAuthenticationHandler.SchemeName, null);
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.CompanyAdmin, p => p.RequireRole(Roles.Admin))
    .AddPolicy(Policies.PosCashier, p => p.RequireRole(Roles.Admin, Roles.Cashier))
    .AddPolicy(Policies.CatalogRead, p => p.RequireRole(Roles.Admin, Roles.Cashier, Roles.Viewer));

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:EnsureCreated"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
}

app.UseExceptionHandler();
app.UseAuthentication();

// Tenant resolution: the company comes from the authenticated user, never from the request body.
app.Use(async (context, next) =>
{
    if (Guid.TryParse(context.User.FindFirstValue(Claims.CompanyId), out var companyId))
    {
        context.RequestServices.GetRequiredService<TenantContext>().CompanyId = companyId;
    }

    await next(context);
});

app.UseAuthorization();
app.MapOpenApi();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapFeatures();

await app.RunAsync();

/// <summary>Entry point; public so the integration tests can use WebApplicationFactory&lt;Program&gt;.</summary>
public partial class Program;

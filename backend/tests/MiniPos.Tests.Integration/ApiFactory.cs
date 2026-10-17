using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MiniPos.Domain.Companies;
using MiniPos.Infrastructure.Persistence;

namespace MiniPos.Tests.Integration;

public sealed class ApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", connectionString);
        builder.UseSetting("Database:EnsureCreated", "true");
        builder.UseEnvironment("Testing");
    }

    /// <summary>A client acting as <paramref name="role"/> in <paramref name="companyId"/>.</summary>
    public HttpClient ClientFor(Guid companyId, string role = "Admin")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Company-Id", companyId.ToString());
        client.DefaultRequestHeaders.Add("X-Role", role);
        return client;
    }

    public async Task<Guid> CreateCompanyAsync(string nameEn = "Acme Store", string nameAr = "متجر أكمي")
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var company = new Company { NameEn = nameEn, NameAr = nameAr, VatNumber = "310122393500003" };
        db.Companies.Add(company);
        await db.SaveChangesAsync();
        return company.Id;
    }
}

/// <summary>Base class: one database + one API host per test class.</summary>
public abstract class ApiTestBase : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    protected ApiTestBase(PostgresFixture db) => Factory = new ApiFactory(db.ConnectionString);

    protected ApiFactory Factory { get; }

    public Task InitializeAsync()
    {
        _ = Factory.Server; // starts the host, which creates the database
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await Factory.DisposeAsync();
}

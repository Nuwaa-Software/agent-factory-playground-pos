using MiniPos.Domain.Common;
using MiniPos.Infrastructure.Persistence;

namespace MiniPos.Tests.Architecture;

public class MultiTenancyConventions
{
    [Fact]
    public void Tenant_entities_carry_company_id() =>
        typeof(AppDbContext).GetProperties()
            .Where(p => p.PropertyType.IsGenericType)
            .Select(p => p.PropertyType.GetGenericArguments()[0])
            .Where(t => t.Name is not "Company")
            .Should().OnlyContain(t => typeof(ITenantScoped).IsAssignableFrom(t));

    internal static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "global.json")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("repo root (global.json) not found");
    }
}

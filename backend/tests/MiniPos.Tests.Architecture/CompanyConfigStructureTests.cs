using Microsoft.EntityFrameworkCore;
using MiniPos.Domain.Companies;
using MiniPos.Domain.Common;
using MiniPos.Infrastructure.Persistence;

namespace MiniPos.Tests.Architecture;

public class CompanyConfigStructureTests
{
    [Fact]
    public void CompanyConfig_implements_ITenantScoped()
    {
        typeof(CompanyConfig).Should().Implement<ITenantScoped>();
    }

    [Fact]
    public void CompanyConfig_has_correct_properties()
    {
        var properties = typeof(CompanyConfig).GetProperties();
        
        properties.Should().Contain(p => p.Name == "Id" && p.PropertyType == typeof(Guid));
        properties.Should().Contain(p => p.Name == "CompanyId" && p.PropertyType == typeof(Guid));
        properties.Should().Contain(p => p.Name == "Key" && p.PropertyType == typeof(string));
        properties.Should().Contain(p => p.Name == "Value" && p.PropertyType == typeof(string));
    }

    [Fact]
    public void AppDbContext_exposes_CompanyConfigs_DbSet()
    {
        var dbSetProperty = typeof(AppDbContext).GetProperty("CompanyConfigs");
        dbSetProperty.Should().NotBeNull();
        dbSetProperty!.PropertyType.Should().Be<DbSet<CompanyConfig>>();
    }
}
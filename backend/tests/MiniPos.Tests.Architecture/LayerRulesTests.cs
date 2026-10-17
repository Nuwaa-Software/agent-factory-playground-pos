using MiniPos.Domain.Common;
using NetArchTest.Rules;

namespace MiniPos.Tests.Architecture;

public class LayerRulesTests
{
    private static readonly System.Reflection.Assembly Domain = typeof(ITenantScoped).Assembly;
    private static readonly System.Reflection.Assembly Infrastructure =
        typeof(MiniPos.Infrastructure.Persistence.AppDbContext).Assembly;

    [Fact]
    public void Domain_depends_on_nothing_else() =>
        Types.InAssembly(Domain).ShouldNot()
            .HaveDependencyOnAny("MiniPos.Infrastructure", "MiniPos.Api", "Microsoft.EntityFrameworkCore")
            .GetResult().IsSuccessful.Should().BeTrue();

    [Fact]
    public void Infrastructure_does_not_depend_on_the_api() =>
        Types.InAssembly(Infrastructure).ShouldNot().HaveDependencyOn("MiniPos.Api")
            .GetResult().IsSuccessful.Should().BeTrue();

    [Fact]
    public void Handlers_are_sealed()
    {
        var result = Types.InAssembly(typeof(Program).Assembly).That().HaveNameEndingWith("Handler")
            .And().AreClasses().Should().BeSealed().GetResult();
        result.FailingTypeNames.Should().BeNullOrEmpty();
    }
}

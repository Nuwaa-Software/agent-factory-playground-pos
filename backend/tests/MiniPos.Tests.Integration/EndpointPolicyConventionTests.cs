using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace MiniPos.Tests.Integration;

/// <summary>
/// Every /api endpoint must name an authorization POLICY. A bare RequireAuthorization() admits every role,
/// Viewer included (nuwaa-pos #1062 found an admin read that way).
/// </summary>
public class EndpointPolicyConventionTests(PostgresFixture db) : ApiTestBase(db)
{
    [Fact]
    public void Every_api_endpoint_names_a_policy()
    {
        var endpoints = Factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText?.StartsWith("/api/", StringComparison.Ordinal) == true)
            .ToList();

        endpoints.Should().NotBeEmpty();
        var withoutPolicy = endpoints
            .Where(e => !e.Metadata.GetOrderedMetadata<IAuthorizeData>().Any(a => !string.IsNullOrEmpty(a.Policy)))
            .Select(e => e.RoutePattern.RawText);
        withoutPolicy.Should().BeEmpty("every tenant-data endpoint must name an authorization policy");
    }
}

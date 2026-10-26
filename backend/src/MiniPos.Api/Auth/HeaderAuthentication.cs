using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace MiniPos.Api.Auth;

/// <summary>
/// Development/test authentication: the caller states its company and role in headers
/// (<c>X-Company-Id</c>, <c>X-Role</c>). Stands in for nuwaa-pos's cookie auth; the policies are the same idea.
/// </summary>
public sealed class HeaderAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Header";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Company-Id", out var company) ||
            !Guid.TryParse(company, out var companyId) ||
            !Request.Headers.TryGetValue("X-Role", out var role) ||
            !Roles.All.Contains(role.ToString()))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        Claim[] claims = [new(Claims.CompanyId, companyId.ToString()), new(ClaimTypes.Role, role.ToString())];

        if (Request.Headers.TryGetValue("X-User-Name", out var userName) && !string.IsNullOrEmpty(userName))
        {
            claims = [.. claims, new(Claims.UserName, userName!)];
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}

public static class Claims
{
    public const string CompanyId = "company_id";
    public const string UserName = "user_name";
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string Cashier = "Cashier";
    public const string Viewer = "Viewer";

    public static readonly string[] All = [Admin, Cashier, Viewer];
}

/// <summary>Every tenant-data endpoint names one of these — never a bare RequireAuthorization().</summary>
public static class Policies
{
    public const string CompanyAdmin = "CompanyAdmin";
    public const string PosCashier = "PosCashier";
    public const string CatalogRead = "CatalogRead";
}

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

public sealed class DemoAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, IHostEnvironment environment) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
    if (!environment.IsDevelopment()) return Task.FromResult(AuthenticateResult.Fail("Development authentication is disabled outside Development."));
        var userId = Request.Headers["X-Demo-User"].FirstOrDefault();
        var tenantId = Request.Headers["X-Demo-Tenant"].FirstOrDefault();
        var roles = Request.Headers["X-Demo-Roles"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(tenantId)) return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId), new("tenant_id", tenantId) };
        foreach (var role in (roles ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) claims.Add(new Claim(ClaimTypes.Role, role));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name)));
    }
}

public static class RequestIdentity
{
    public static string? UserId(this HttpContext context) => context.User.FindFirstValue(ClaimTypes.NameIdentifier);
    public static string? TenantId(this HttpContext context) => context.User.FindFirstValue("tenant_id");
    public static bool HasRole(this HttpContext context, string role) => context.User.IsInRole(role);
}

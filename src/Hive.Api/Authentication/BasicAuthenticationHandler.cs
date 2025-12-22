using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace Hive.Api.Authentication;

/// <summary>
/// Basic Authentication handler for admin access.
/// In production, replace with proper identity provider (OAuth2/OIDC).
/// </summary>
public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly AdminCredentials _adminCredentials;

    public BasicAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<AdminCredentials> adminCredentials)
        : base(options, logger, encoder)
    {
        _adminCredentials = adminCredentials.Value;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization header"));
        }

        try
        {
            var authHeader = Request.Headers.Authorization.ToString();
            if (!authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization scheme"));
            }

            var encodedCredentials = authHeader["Basic ".Length..].Trim();
            var decodedCredentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
            var credentials = decodedCredentials.Split(':', 2);

            if (credentials.Length != 2)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header format"));
            }

            var username = credentials[0];
            var password = credentials[1];

            if (!ValidateCredentials(username, password))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid username or password"));
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (FormatException)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Base64 encoding"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing authentication");
            return Task.FromResult(AuthenticateResult.Fail("Error processing authentication"));
        }
    }

    private bool ValidateCredentials(string username, string password)
    {
        // Constant-time comparison to prevent timing attacks
        var usernameMatch = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(username),
            Encoding.UTF8.GetBytes(_adminCredentials.Username));

        var passwordMatch = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(password),
            Encoding.UTF8.GetBytes(_adminCredentials.Password));

        return usernameMatch && passwordMatch;
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = "Basic realm=\"Hive API\", charset=\"UTF-8\"";
        return base.HandleChallengeAsync(properties);
    }
}

/// <summary>
/// Admin credentials configuration.
/// </summary>
public class AdminCredentials
{
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "admin123";
}

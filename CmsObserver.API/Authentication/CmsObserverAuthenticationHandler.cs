using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using CmsObserver.Users;
using CmsObserver.Users.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace CmsObserver.API.Authentication;

public sealed class CmsObserverAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IUserCredentialsStore usersStore)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authorizationHeaderValues))
            return AuthenticateResult.Fail("Missing Authorization header.");

        if (!AuthenticationHeaderValue.TryParse(authorizationHeaderValues, out var authenticationHeaderValue))
            return AuthenticateResult.Fail("Invalid Authorization header.");

        if (!"Basic".Equals(authenticationHeaderValue.Scheme, StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.Fail("Unsupported authentication scheme.");

        if (string.IsNullOrWhiteSpace(authenticationHeaderValue.Parameter))
            return AuthenticateResult.Fail("Missing Basic credentials.");

        if (!TryGetCredentials(authenticationHeaderValue.Parameter, out var username, out var password))
            return AuthenticateResult.Fail("Invalid Basic credentials format.");

        var user = await usersStore.GetByUsernameAsync(username, Context.RequestAborted);
        if (user is null || !IsValidCredentials(user, password)) return AuthenticateResult.Fail("Invalid username or password.");

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = "Basic realm=\"cms-observer-users\"";
        return base.HandleChallengeAsync(properties);
    }

    private static bool IsValidCredentials(CmsObserverUserCredentials user, string password)
    {
        if (!TryFromBase64(user.PasswordSaltBase64, out var salt)) return false;
        if (!TryFromBase64(user.PasswordHashBase64, out var expectedHash)) return false;

        var candidateHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            user.Iterations,
            HashAlgorithmName.SHA256,
            expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(candidateHash, expectedHash);
    }

    private static bool TryGetCredentials(string base64Credentials, out string username, out string password)
    {
        username = string.Empty;
        password = string.Empty;

        byte[] credentialBytes;
        try
        {
            credentialBytes = Convert.FromBase64String(base64Credentials);
        }
        catch (FormatException)
        {
            return false;
        }

        var rawCredentials = Encoding.UTF8.GetString(credentialBytes);
        var separatorIndex = rawCredentials.IndexOf(':');
        if (separatorIndex <= 0) return false;

        username = rawCredentials[..separatorIndex];
        password = rawCredentials[(separatorIndex + 1)..];
        return true;
    }

    private static bool TryFromBase64(string value, out byte[] bytes)
    {
        bytes = [];

        try
        {
            bytes = Convert.FromBase64String(value);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

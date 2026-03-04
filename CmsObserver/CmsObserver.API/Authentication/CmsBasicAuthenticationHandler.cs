using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace CmsObserver.API.Authentication;

public sealed class CmsBasicAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<CmsBasicAuthOptions> cmsAuthOptions)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private readonly CmsBasicAuthOptions _cmsAuthOptions = cmsAuthOptions.Value;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authorizationHeaderValues))
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization header."));

        if (!AuthenticationHeaderValue.TryParse(authorizationHeaderValues, out var authenticationHeaderValue))
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header."));

        if (!"Basic".Equals(authenticationHeaderValue.Scheme, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.Fail("Unsupported authentication scheme."));

        if (string.IsNullOrWhiteSpace(authenticationHeaderValue.Parameter))
            return Task.FromResult(AuthenticateResult.Fail("Missing Basic credentials."));

        if (!TryGetCredentials(authenticationHeaderValue.Parameter, out var username, out var password))
            return Task.FromResult(AuthenticateResult.Fail("Invalid Basic credentials format."));

        if (!IsValidCredentials(username, password))
            return Task.FromResult(AuthenticateResult.Fail("Invalid username or password."));

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, "CmsPublisher")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = "Basic realm=\"cms-events\"";
        return base.HandleChallengeAsync(properties);
    }

    private bool IsValidCredentials(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(_cmsAuthOptions.Username)) return false;
        if (string.IsNullOrWhiteSpace(_cmsAuthOptions.PasswordHashBase64)) return false;
        if (string.IsNullOrWhiteSpace(_cmsAuthOptions.PasswordSaltBase64)) return false;

        if (!username.Equals(_cmsAuthOptions.Username, StringComparison.Ordinal)) return false;

        if (!TryFromBase64(_cmsAuthOptions.PasswordSaltBase64, out var salt)) return false;
        if (!TryFromBase64(_cmsAuthOptions.PasswordHashBase64, out var expectedHash)) return false;

        var candidateHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            _cmsAuthOptions.Iterations,
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

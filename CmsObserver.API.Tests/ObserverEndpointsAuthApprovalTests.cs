using System.Net.Http.Headers;
using CmsObserver.API.Tests.Infrastructure;

namespace CmsObserver.API.Tests;

public sealed class ObserverEndpointsAuthApprovalTests(CmsObserverApiFactory factory) : IClassFixture<CmsObserverApiFactory>
{
    [Fact]
    public async Task ObserverEndpoints_AuthBoundary_IsExpected()
    {
        await factory.ResetStateAsync();

        using var client = factory.CreateApiClient();
        var cases = new[]
        {
            await ExecuteCaseAsync(client, "missing-auth", null),
            await ExecuteCaseAsync(client, "invalid-auth", CmsObserverApiFactory.CreateBasicAuthHeader(new TestCredentials("bad-user", "bad-password"))),
            await ExecuteCaseAsync(client, "cms-auth-on-observer-endpoint", CmsObserverApiFactory.CreateBasicAuthHeader(factory.CmsCredentials)),
            await ExecuteCaseAsync(client, "observer-user-auth", CmsObserverApiFactory.CreateBasicAuthHeader(factory.ObserverUserCredentials)),
            await ExecuteCaseAsync(client, "observer-admin-auth", CmsObserverApiFactory.CreateBasicAuthHeader(factory.ObserverAdminCredentials))
        };

        ApprovalJson.Verify(cases);
    }

    private static async Task<object> ExecuteCaseAsync(HttpClient client, string caseName, AuthenticationHeaderValue? authHeader)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/entities");
        if (authHeader is not null) request.Headers.Authorization = authHeader;

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        return new
        {
            Case = caseName,
            StatusCode = (int)response.StatusCode,
            Status = response.StatusCode.ToString(),
            WwwAuthenticate = response.Headers.TryGetValues("WWW-Authenticate", out var values)
                ? string.Join(";", values.OrderBy(x => x, StringComparer.Ordinal))
                : string.Empty,
            Body = body
        };
    }
}

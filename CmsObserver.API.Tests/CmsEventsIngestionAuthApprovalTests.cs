using System.Net.Http.Headers;
using System.Net.Http.Json;
using CmsObserver.API.Tests.Infrastructure;

namespace CmsObserver.API.Tests;

public sealed class CmsEventsIngestionAuthApprovalTests(CmsObserverApiFactory factory) : IClassFixture<CmsObserverApiFactory>
{
    [Fact]
    public async Task CmsEvents_AuthMatrix_IsExpected()
    {
        await factory.ResetStateAsync();

        using var client = factory.CreateApiClient();
        var cases = new[]
        {
            await ExecuteCaseAsync(client, "missing-auth", null),
            await ExecuteCaseAsync(client, "invalid-auth", CmsObserverApiFactory.CreateBasicAuthHeader(new TestCredentials("wrong-user", "wrong-password"))),
            await ExecuteCaseAsync(client, "valid-auth", CmsObserverApiFactory.CreateBasicAuthHeader(factory.CmsCredentials))
        };

        ApprovalJson.Verify(cases);
    }

    private static async Task<object> ExecuteCaseAsync(HttpClient client, string caseName, AuthenticationHeaderValue? authHeader)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/cms/events")
        {
            Content = JsonContent.Create(Array.Empty<object>())
        };

        if (authHeader is not null) request.Headers.Authorization = authHeader;

        using var response = await client.SendAsync(request);
        return new
        {
            Case = caseName,
            StatusCode = (int)response.StatusCode,
            Status = response.StatusCode.ToString(),
            WwwAuthenticate = response.Headers.TryGetValues("WWW-Authenticate", out var values)
                ? string.Join(";", values.OrderBy(x => x, StringComparer.Ordinal))
                : string.Empty
        };
    }
}

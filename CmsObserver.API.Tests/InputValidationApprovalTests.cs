using System.Net.Http.Headers;
using System.Text;
using CmsObserver.API.Tests.Infrastructure;

namespace CmsObserver.API.Tests;

public sealed class InputValidationApprovalTests(CmsObserverApiFactory factory) : IClassFixture<CmsObserverApiFactory>
{
    [Fact]
    public async Task CmsEvents_InvalidPayloads_AreRejected()
    {
        await factory.ResetStateAsync();
        using var client = factory.CreateApiClient();

        var cases = new[]
        {
            await SendAsync(client, "malformed-json", "[{\"type\":\"publish\",\"id\":\"x\"", "application/json"),
            await SendAsync(client, "invalid-event-type", "[{\"type\":\"publishX\",\"id\":\"x\",\"timestamp\":\"2024-01-01T00:00:00Z\"}]", "application/json"),
            await SendAsync(client, "non-json-content-type", "plain-text-body", "text/plain")
        };

        ApprovalJson.Verify(cases);
    }

    private async Task<object> SendAsync(HttpClient client, string caseName, string body, string contentType)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/cms/events")
        {
            Content = new StringContent(body, Encoding.UTF8, contentType)
        };
        request.Headers.Authorization = CmsObserverApiFactory.CreateBasicAuthHeader(factory.CmsCredentials);

        using var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        return new
        {
            Case = caseName,
            StatusCode = (int)response.StatusCode,
            Status = response.StatusCode.ToString(),
            BodySummary = SummarizeBody(responseBody)
        };
    }

    private static object SummarizeBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return new { HasBody = false, FirstLine = string.Empty };

        var firstLine = body.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
        return new { HasBody = true, FirstLine = firstLine };
    }
}

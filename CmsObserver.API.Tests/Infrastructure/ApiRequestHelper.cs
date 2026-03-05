using System.Text.Json;

namespace CmsObserver.API.Tests.Infrastructure;

public static class ApiRequestHelper
{
    public static async Task<ApiSnapshot> GetEntitiesSnapshotAsync(HttpClient client, TestCredentials credentials, bool includeUnpublished = false)
    {
        var url = includeUnpublished ? "/entities?includeUnpublished=true" : "/entities";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = CmsObserverApiFactory.CreateBasicAuthHeader(credentials);

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        using var jsonDocument = JsonDocument.Parse(body);
        return new ApiSnapshot(url, (int)response.StatusCode, response.StatusCode.ToString(), jsonDocument.RootElement.Clone());
    }
}

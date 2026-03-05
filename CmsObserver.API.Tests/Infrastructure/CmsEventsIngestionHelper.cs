using System.Net.Http.Json;

namespace CmsObserver.API.Tests.Infrastructure;

public static class CmsEventsIngestionHelper
{
    public static async Task<HttpResponseMessage> IngestAsync<TEvent>(HttpClient client, TestCredentials credentials, params TEvent[] cmsEvents)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/cms/events")
        {
            Content = JsonContent.Create(cmsEvents)
        };
        request.Headers.Authorization = CmsObserverApiFactory.CreateBasicAuthHeader(credentials);

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return response;
    }
}

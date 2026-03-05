using System.Net.Http.Json;
using System.Text.Json;
using CmsObserver.API.Tests.Infrastructure;

namespace CmsObserver.API.Tests;

public sealed class PublishFlowApprovalTests(CmsObserverApiFactory factory) : IClassFixture<CmsObserverApiFactory>
{
    [Fact]
    public async Task PublishFlow_PersistsAndListsEntity_ForObserverUser()
    {
        await factory.ResetStateAsync();
        using var client = factory.CreateApiClient();

        var cmsEvents = new[]
        {
            new
            {
                type = "publish",
                id = "entity-publish-1",
                payload = new { title = "First entity", category = "news" },
                version = 1,
                timestamp = DateTimeOffset.Parse("2024-01-01T00:00:00Z")
            }
        };

        using var ingestRequest = new HttpRequestMessage(HttpMethod.Post, "/cms/events")
        {
            Content = JsonContent.Create(cmsEvents)
        };
        ingestRequest.Headers.Authorization = CmsObserverApiFactory.CreateBasicAuthHeader(factory.CmsCredentials);

        using var ingestResponse = await client.SendAsync(ingestRequest);
        var entitiesResponse = await WaitForEntitiesAsync(client, factory.ObserverUserCredentials, "entity-publish-1");
        var entitiesBody = await entitiesResponse.Content.ReadAsStringAsync();

        ApprovalJson.Verify(new
        {
            IngestionStatusCode = (int)ingestResponse.StatusCode,
            IngestionStatus = ingestResponse.StatusCode.ToString(),
            EntitiesStatusCode = (int)entitiesResponse.StatusCode,
            EntitiesStatus = entitiesResponse.StatusCode.ToString(),
            Entities = JsonDocument.Parse(entitiesBody).RootElement
        });
    }

    private static async Task<HttpResponseMessage> WaitForEntitiesAsync(HttpClient client, TestCredentials credentials, string expectedId)
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            var response = await GetEntitiesAsync(client, credentials);
            var body = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode && body.Contains(expectedId, StringComparison.Ordinal)) return response;

            response.Dispose();
            await Task.Delay(100);
        }

        return await GetEntitiesAsync(client, credentials);
    }

    private static Task<HttpResponseMessage> GetEntitiesAsync(HttpClient client, TestCredentials credentials)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/entities");
        request.Headers.Authorization = CmsObserverApiFactory.CreateBasicAuthHeader(credentials);
        return client.SendAsync(request);
    }
}

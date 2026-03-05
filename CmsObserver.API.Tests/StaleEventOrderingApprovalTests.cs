using System.Net.Http.Json;
using System.Text.Json;
using CmsObserver.API.Tests.Infrastructure;

namespace CmsObserver.API.Tests;

public sealed class StaleEventOrderingApprovalTests(CmsObserverApiFactory factory) : IClassFixture<CmsObserverApiFactory>
{
    [Fact]
    public async Task OlderPublish_DoesNotOverwriteNewerState()
    {
        await factory.ResetStateAsync();
        using var client = factory.CreateApiClient();

        var entityId = "entity-ordering-1";

        await IngestAsync(client, factory.CmsCredentials, new
        {
            type = "publish",
            id = entityId,
            payload = new { title = "newer-state", priority = "high" },
            version = 2,
            timestamp = DateTimeOffset.Parse("2024-01-01T00:02:00Z")
        });

        await IngestAsync(client, factory.CmsCredentials, new
        {
            type = "publish",
            id = entityId,
            payload = new { title = "older-state", priority = "low" },
            version = 1,
            timestamp = DateTimeOffset.Parse("2024-01-01T00:01:00Z")
        });

        var entities = await WaitForExpectedStateAsync(client, factory.ObserverUserCredentials, body =>
        {
            using var json = JsonDocument.Parse(body);
            if (json.RootElement.GetArrayLength() != 1) return false;

            var entity = json.RootElement[0];
            return entity.GetProperty("version").GetInt32() == 2 &&
                   entity.GetProperty("payload").GetProperty("title").GetString() == "newer-state";
        });

        ApprovalJson.Verify(entities);
    }

    private static async Task IngestAsync(HttpClient client, TestCredentials credentials, object cmsEvent)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/cms/events")
        {
            Content = JsonContent.Create(new[] { cmsEvent })
        };
        request.Headers.Authorization = CmsObserverApiFactory.CreateBasicAuthHeader(credentials);

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<EntitiesSnapshot> WaitForExpectedStateAsync(HttpClient client, TestCredentials credentials, Func<string, bool> condition)
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            var snapshot = await GetEntitiesAsync(client, credentials);
            if (condition(snapshot.RawBody)) return snapshot;

            await Task.Delay(100);
        }

        return await GetEntitiesAsync(client, credentials);
    }

    private static async Task<EntitiesSnapshot> GetEntitiesAsync(HttpClient client, TestCredentials credentials)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/entities");
        request.Headers.Authorization = CmsObserverApiFactory.CreateBasicAuthHeader(credentials);

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        return new EntitiesSnapshot((int)response.StatusCode, response.StatusCode.ToString(), body);
    }

    private sealed record EntitiesSnapshot(int StatusCode, string Status, string RawBody);
}

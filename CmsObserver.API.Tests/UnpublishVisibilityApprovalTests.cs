using System.Net.Http.Json;
using System.Text.Json;
using CmsObserver.API.Tests.Infrastructure;

namespace CmsObserver.API.Tests;

public sealed class UnpublishVisibilityApprovalTests(CmsObserverApiFactory factory) : IClassFixture<CmsObserverApiFactory>
{
    [Fact]
    public async Task Unpublish_IsHiddenFromUser_ButVisibleToAdminWhenRequested()
    {
        await factory.ResetStateAsync();
        using var client = factory.CreateApiClient();

        var publishTimestamp = DateTimeOffset.Parse("2024-01-01T00:00:00Z");
        var unpublishTimestamp = DateTimeOffset.Parse("2024-01-01T00:01:00Z");
        var entityId = "entity-unpublish-1";

        await IngestAsync(client, factory.CmsCredentials, new
        {
            type = "publish",
            id = entityId,
            payload = new { title = "Entity before unpublish", category = "draft" },
            version = 1,
            timestamp = publishTimestamp
        });

        await IngestAsync(client, factory.CmsCredentials, new
        {
            type = "unpublish",
            id = entityId,
            payload = new { title = "Entity unpublished", category = "draft" },
            version = 2,
            timestamp = unpublishTimestamp
        });

        var userEntities = await WaitForEntitiesAsync(client, factory.ObserverUserCredentials, entities =>
            entities.GetArrayLength() == 0);

        var adminDefault = await WaitForEntitiesAsync(client, factory.ObserverAdminCredentials, entities =>
            entities.GetArrayLength() == 0);

        var adminIncludingUnpublished = await WaitForEntitiesAsync(client, factory.ObserverAdminCredentials, entities =>
            entities.EnumerateArray().Any(x => x.GetProperty("id").GetString() == entityId), includeUnpublished: true);

        ApprovalJson.Verify(new
        {
            UserEntities = userEntities,
            AdminEntitiesDefault = adminDefault,
            AdminEntitiesIncludingUnpublished = adminIncludingUnpublished
        });
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

    private static async Task<EntitiesSnapshot> WaitForEntitiesAsync(
        HttpClient client,
        TestCredentials credentials,
        Func<JsonElement, bool> condition,
        bool includeUnpublished = false)
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            var current = await GetEntitiesSnapshotAsync(client, credentials, includeUnpublished);
            if (condition(current.Body)) return current;

            await Task.Delay(100);
        }

        return await GetEntitiesSnapshotAsync(client, credentials, includeUnpublished);
    }

    private static async Task<EntitiesSnapshot> GetEntitiesSnapshotAsync(HttpClient client, TestCredentials credentials, bool includeUnpublished)
    {
        var url = includeUnpublished ? "/entities?includeUnpublished=true" : "/entities";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = CmsObserverApiFactory.CreateBasicAuthHeader(credentials);

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        return new EntitiesSnapshot(
            url,
            (int)response.StatusCode,
            response.StatusCode.ToString(),
            JsonDocument.Parse(body).RootElement,
            body);
    }

    private sealed record EntitiesSnapshot(string Url, int StatusCode, string Status, JsonElement Body, string RawBody);
}

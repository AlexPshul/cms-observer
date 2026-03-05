using System.Net.Http.Json;
using System.Text.Json;
using CmsObserver.API.Tests.Infrastructure;

namespace CmsObserver.API.Tests;

public sealed class DeleteFlowApprovalTests(CmsObserverApiFactory factory) : IClassFixture<CmsObserverApiFactory>
{
    [Fact]
    public async Task Delete_RemovesEntity_ForUserAndAdminViews()
    {
        await factory.ResetStateAsync();
        using var client = factory.CreateApiClient();

        var entityId = "entity-delete-1";

        await IngestAsync(client, factory.CmsCredentials, new
        {
            type = "publish",
            id = entityId,
            payload = new { title = "Delete target" },
            version = 1,
            timestamp = DateTimeOffset.Parse("2024-01-01T00:00:00Z")
        });

        await IngestAsync(client, factory.CmsCredentials, new
        {
            type = "delete",
            id = entityId,
            timestamp = DateTimeOffset.Parse("2024-01-01T00:01:00Z")
        });

        var userEntities = await WaitForEntitiesLengthAsync(client, factory.ObserverUserCredentials, 0);
        var adminEntities = await WaitForEntitiesLengthAsync(client, factory.ObserverAdminCredentials, 0, includeUnpublished: true);

        ApprovalJson.Verify(new
        {
            UserEntities = userEntities,
            AdminEntitiesIncludingUnpublished = adminEntities
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

    private static async Task<EntitiesSnapshot> WaitForEntitiesLengthAsync(HttpClient client, TestCredentials credentials, int expectedLength, bool includeUnpublished = false)
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            var snapshot = await GetEntitiesAsync(client, credentials, includeUnpublished);
            using var document = JsonDocument.Parse(snapshot.RawBody);
            if (document.RootElement.GetArrayLength() == expectedLength) return snapshot;

            await Task.Delay(100);
        }

        return await GetEntitiesAsync(client, credentials, includeUnpublished);
    }

    private static async Task<EntitiesSnapshot> GetEntitiesAsync(HttpClient client, TestCredentials credentials, bool includeUnpublished)
    {
        var url = includeUnpublished ? "/entities?includeUnpublished=true" : "/entities";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = CmsObserverApiFactory.CreateBasicAuthHeader(credentials);

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        return new EntitiesSnapshot(url, (int)response.StatusCode, response.StatusCode.ToString(), body);
    }

    private sealed record EntitiesSnapshot(string Url, int StatusCode, string Status, string RawBody);
}

using System.Net.Http.Json;
using System.Text.Json;
using CmsObserver.API.Tests.Infrastructure;

namespace CmsObserver.API.Tests;

public sealed class UnpublishWithoutPriorPublishApprovalTests(CmsObserverApiFactory factory) : IClassFixture<CmsObserverApiFactory>
{
    [Fact]
    public async Task UnpublishWithoutPriorPublish_PreservesLatestDataForAdmin()
    {
        await factory.ResetStateAsync();
        using var client = factory.CreateApiClient();

        var entityId = "entity-unpublish-only-1";

        await IngestAsync(client, factory.CmsCredentials, new
        {
            type = "unpublish",
            id = entityId,
            payload = new { title = "Latest unpublished", notes = "No prior publish" },
            version = 2,
            timestamp = DateTimeOffset.Parse("2024-01-01T00:05:00Z")
        });

        var userEntities = await WaitForStateAsync(client, factory.ObserverUserCredentials, "/entities", body =>
            MatchesArray(body, array => array.GetArrayLength() == 0));

        var adminEntities = await WaitForStateAsync(client, factory.ObserverAdminCredentials, "/entities?includeUnpublished=true", body =>
            MatchesArray(body, array =>
            {
                if (array.GetArrayLength() != 1) return false;

                var entity = array[0];
                return entity.GetProperty("id").GetString() == entityId &&
                       entity.GetProperty("version").GetInt32() == 2 &&
                       entity.GetProperty("isActive").GetBoolean() == false;
            }));

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

    private static async Task<EntitiesSnapshot> WaitForStateAsync(HttpClient client, TestCredentials credentials, string url, Func<string, bool> condition)
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            var snapshot = await GetSnapshotAsync(client, credentials, url);
            if (condition(snapshot.RawBody)) return snapshot;

            await Task.Delay(100);
        }

        return await GetSnapshotAsync(client, credentials, url);
    }

    private static async Task<EntitiesSnapshot> GetSnapshotAsync(HttpClient client, TestCredentials credentials, string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = CmsObserverApiFactory.CreateBasicAuthHeader(credentials);

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        return new EntitiesSnapshot(url, (int)response.StatusCode, response.StatusCode.ToString(), body);
    }

    private static bool MatchesArray(string body, Func<JsonElement, bool> predicate)
    {
        if (string.IsNullOrWhiteSpace(body)) return false;

        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.ValueKind == JsonValueKind.Array && predicate(doc.RootElement);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private sealed record EntitiesSnapshot(string Url, int StatusCode, string Status, string RawBody);
}

using System.Net.Http.Json;
using System.Text.Json;
using CmsObserver.API.Tests.Infrastructure;

namespace CmsObserver.API.Tests;

public sealed class BatchMixedEventsApprovalTests(CmsObserverApiFactory factory) : IClassFixture<CmsObserverApiFactory>
{
    [Fact]
    public async Task MixedBatch_ProducesExpectedFinalState()
    {
        await factory.ResetStateAsync();
        using var client = factory.CreateApiClient();

        var batch = new object[]
        {
            new
            {
                type = "publish",
                id = "batch-A",
                payload = new { title = "A active" },
                version = 1,
                timestamp = DateTimeOffset.Parse("2024-01-01T00:00:00Z")
            },
            new
            {
                type = "publish",
                id = "batch-B",
                payload = new { title = "B active" },
                version = 1,
                timestamp = DateTimeOffset.Parse("2024-01-01T00:00:00Z")
            },
            new
            {
                type = "unpublish",
                id = "batch-B",
                payload = new { title = "B inactive" },
                version = 2,
                timestamp = DateTimeOffset.Parse("2024-01-01T00:01:00Z")
            },
            new
            {
                type = "publish",
                id = "batch-C",
                payload = new { title = "C active" },
                version = 1,
                timestamp = DateTimeOffset.Parse("2024-01-01T00:00:00Z")
            },
            new
            {
                type = "delete",
                id = "batch-C",
                timestamp = DateTimeOffset.Parse("2024-01-01T00:01:00Z")
            }
        };

        using var ingestRequest = new HttpRequestMessage(HttpMethod.Post, "/cms/events")
        {
            Content = JsonContent.Create(batch)
        };
        ingestRequest.Headers.Authorization = CmsObserverApiFactory.CreateBasicAuthHeader(factory.CmsCredentials);
        using var ingestResponse = await client.SendAsync(ingestRequest);

        var userEntities = await WaitForStateAsync(client, factory.ObserverUserCredentials, "/entities", body =>
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetArrayLength() == 1 && doc.RootElement[0].GetProperty("id").GetString() == "batch-A";
        });

        var adminEntities = await WaitForStateAsync(client, factory.ObserverAdminCredentials, "/entities?includeUnpublished=true", body =>
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.GetArrayLength() != 2) return false;

            var ids = doc.RootElement.EnumerateArray().Select(x => x.GetProperty("id").GetString()).OrderBy(x => x).ToArray();
            return ids.SequenceEqual(["batch-A", "batch-B"]);
        });

        ApprovalJson.Verify(new
        {
            IngestStatusCode = (int)ingestResponse.StatusCode,
            IngestStatus = ingestResponse.StatusCode.ToString(),
            UserEntities = userEntities,
            AdminEntitiesIncludingUnpublished = adminEntities
        });
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

    private sealed record EntitiesSnapshot(string Url, int StatusCode, string Status, string RawBody);
}

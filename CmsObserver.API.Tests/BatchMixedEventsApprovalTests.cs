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

        using var ingestResponse = await CmsEventsIngestionHelper.IngestAsync(client, factory.CmsCredentials, batch);

        var userEntities = await PollingHelper.WaitForAsync(
            () => ApiRequestHelper.GetEntitiesSnapshotAsync(client, factory.ObserverUserCredentials),
            snapshot =>
            {
                using var doc = JsonDocument.Parse(snapshot.RawBody);
                return doc.RootElement.GetArrayLength() == 1 && doc.RootElement[0].GetProperty("id").GetString() == "batch-A";
            });

        var adminEntities = await PollingHelper.WaitForAsync(
            () => ApiRequestHelper.GetEntitiesSnapshotAsync(client, factory.ObserverAdminCredentials, true),
            snapshot =>
            {
                using var doc = JsonDocument.Parse(snapshot.RawBody);
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
}

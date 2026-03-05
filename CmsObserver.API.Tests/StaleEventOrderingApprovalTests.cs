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

        await CmsEventsIngestionHelper.IngestAsync(client, factory.CmsCredentials, new
        {
            type = "publish",
            id = entityId,
            payload = new { title = "newer-state", priority = "high" },
            version = 2,
            timestamp = DateTimeOffset.Parse("2024-01-01T00:02:00Z")
        });

        await CmsEventsIngestionHelper.IngestAsync(client, factory.CmsCredentials, new
        {
            type = "publish",
            id = entityId,
            payload = new { title = "older-state", priority = "low" },
            version = 1,
            timestamp = DateTimeOffset.Parse("2024-01-01T00:01:00Z")
        });

        var entities = await PollingHelper.WaitForAsync(
            () => ApiRequestHelper.GetEntitiesSnapshotAsync(client, factory.ObserverUserCredentials),
            snapshot =>
            {
                using var json = JsonDocument.Parse(snapshot.RawBody);
                if (json.RootElement.GetArrayLength() != 1) return false;

                var entity = json.RootElement[0];
                return entity.GetProperty("version").GetInt32() == 2 &&
                       entity.GetProperty("payload").GetProperty("title").GetString() == "newer-state";
            });

        ApprovalJson.Verify(entities);
    }
}

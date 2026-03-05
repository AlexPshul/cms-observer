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

        using var ingestResponse = await CmsEventsIngestionHelper.IngestAsync(client, factory.CmsCredentials, cmsEvents);
        var entitiesSnapshot = await PollingHelper.WaitForAsync(
            () => ApiRequestHelper.GetEntitiesSnapshotAsync(client, factory.ObserverUserCredentials),
            snapshot => snapshot.StatusCode is >= 200 and < 300 && snapshot.RawBody.Contains("entity-publish-1", StringComparison.Ordinal));

        ApprovalJson.Verify(new
        {
            IngestionStatusCode = (int)ingestResponse.StatusCode,
            IngestionStatus = ingestResponse.StatusCode.ToString(),
            EntitiesStatusCode = entitiesSnapshot.StatusCode,
            EntitiesStatus = entitiesSnapshot.Status,
            Entities = JsonDocument.Parse(entitiesSnapshot.RawBody).RootElement
        });
    }
}

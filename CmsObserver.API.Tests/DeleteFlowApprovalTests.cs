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

        await CmsEventsIngestionHelper.IngestAsync(client, factory.CmsCredentials, new
        {
            type = "publish",
            id = entityId,
            payload = new { title = "Delete target" },
            version = 1,
            timestamp = DateTimeOffset.Parse("2024-01-01T00:00:00Z")
        });

        await CmsEventsIngestionHelper.IngestAsync(client, factory.CmsCredentials, new
        {
            type = "delete",
            id = entityId,
            timestamp = DateTimeOffset.Parse("2024-01-01T00:01:00Z")
        });

        var userEntities = await PollingHelper.WaitForAsync(
            () => ApiRequestHelper.GetEntitiesSnapshotAsync(client, factory.ObserverUserCredentials),
            snapshot =>
            {
                using var document = JsonDocument.Parse(snapshot.RawBody);
                return document.RootElement.GetArrayLength() == 0;
            });
        var adminEntities = await PollingHelper.WaitForAsync(
            () => ApiRequestHelper.GetEntitiesSnapshotAsync(client, factory.ObserverAdminCredentials, true),
            snapshot =>
            {
                using var document = JsonDocument.Parse(snapshot.RawBody);
                return document.RootElement.GetArrayLength() == 0;
            });

        ApprovalJson.Verify(new
        {
            UserEntities = userEntities,
            AdminEntitiesIncludingUnpublished = adminEntities
        });
    }

}

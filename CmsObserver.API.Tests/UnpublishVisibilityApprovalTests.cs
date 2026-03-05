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

        await CmsEventsIngestionHelper.IngestAsync(client, factory.CmsCredentials, new
        {
            type = "publish",
            id = entityId,
            payload = new { title = "Entity before unpublish", category = "draft" },
            version = 1,
            timestamp = publishTimestamp
        });

        await CmsEventsIngestionHelper.IngestAsync(client, factory.CmsCredentials, new
        {
            type = "unpublish",
            id = entityId,
            payload = new { title = "Entity unpublished", category = "draft" },
            version = 2,
            timestamp = unpublishTimestamp
        });

        var userEntities = await PollingHelper.WaitForAsync(
            () => ApiRequestHelper.GetEntitiesSnapshotAsync(client, factory.ObserverUserCredentials),
            current => JsonDocument.Parse(current.RawBody).RootElement.GetArrayLength() == 0);

        var adminDefault = await PollingHelper.WaitForAsync(
            () => ApiRequestHelper.GetEntitiesSnapshotAsync(client, factory.ObserverAdminCredentials),
            current => JsonDocument.Parse(current.RawBody).RootElement.GetArrayLength() == 0);

        var adminIncludingUnpublished = await PollingHelper.WaitForAsync(
            () => ApiRequestHelper.GetEntitiesSnapshotAsync(client, factory.ObserverAdminCredentials, true),
            current => JsonDocument.Parse(current.RawBody).RootElement.EnumerateArray().Any(x => x.GetProperty("id").GetString() == entityId));

        ApprovalJson.Verify(new
        {
            UserEntities = userEntities,
            AdminEntitiesDefault = adminDefault,
            AdminEntitiesIncludingUnpublished = adminIncludingUnpublished
        });
    }

}

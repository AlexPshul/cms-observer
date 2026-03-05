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

        await CmsEventsIngestionHelper.IngestAsync(client, factory.CmsCredentials, new
        {
            type = "unpublish",
            id = entityId,
            payload = new { title = "Latest unpublished", notes = "No prior publish" },
            version = 2,
            timestamp = DateTimeOffset.Parse("2024-01-01T00:05:00Z")
        });

        var userEntities = await PollingHelper.WaitForAsync(
            () => ApiRequestHelper.GetEntitiesSnapshotAsync(client, factory.ObserverUserCredentials),
            snapshot => MatchesArray(snapshot.RawBody, array => array.GetArrayLength() == 0));

        var adminEntities = await PollingHelper.WaitForAsync(
            () => ApiRequestHelper.GetEntitiesSnapshotAsync(client, factory.ObserverAdminCredentials, true),
            snapshot => MatchesArray(snapshot.RawBody, array =>
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

}

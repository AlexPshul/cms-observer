using System.Net.Http.Json;
using System.Text.Json;
using CmsObserver.API.Tests.Infrastructure;

namespace CmsObserver.API.Tests;

public sealed class AdminDisableApprovalTests(CmsObserverApiFactory factory) : IClassFixture<CmsObserverApiFactory>
{
    [Fact]
    public async Task DisableEndpoint_RequiresAdmin_AndHidesEntityFromUser()
    {
        await factory.ResetStateAsync();
        using var client = factory.CreateApiClient();

        var entityId = "entity-disable-1";

        await CmsEventsIngestionHelper.IngestAsync(client, factory.CmsCredentials, new
        {
            type = "publish",
            id = entityId,
            payload = new { title = "Disable candidate" },
            version = 1,
            timestamp = DateTimeOffset.Parse("2024-01-01T00:00:00Z")
        });

        var nonAdminDisable = await DisableAsync(client, factory.ObserverUserCredentials, entityId);
        var adminDisable = await DisableAsync(client, factory.ObserverAdminCredentials, entityId);

        var userEntities = await PollingHelper.WaitForAsync(
            () => ApiRequestHelper.GetEntitiesSnapshotAsync(client, factory.ObserverUserCredentials),
            snapshot => MatchesArray(snapshot.RawBody, array => array.GetArrayLength() == 0));

        var adminEntities = await PollingHelper.WaitForAsync(
            () => ApiRequestHelper.GetEntitiesSnapshotAsync(client, factory.ObserverAdminCredentials, true),
            snapshot => MatchesArray(snapshot.RawBody, array => array.EnumerateArray().Any(x => x.GetProperty("id").GetString() == entityId)));

        ApprovalJson.Verify(new
        {
            NonAdminDisable = nonAdminDisable,
            AdminDisable = adminDisable,
            UserEntities = userEntities,
            AdminEntitiesIncludingUnpublished = adminEntities
        });
    }

    private static async Task<object> DisableAsync(HttpClient client, TestCredentials credentials, string entityId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/entities/{entityId}/disable");
        request.Headers.Authorization = CmsObserverApiFactory.CreateBasicAuthHeader(credentials);

        using var response = await client.SendAsync(request);

        return new
        {
            StatusCode = (int)response.StatusCode,
            Status = response.StatusCode.ToString()
        };
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

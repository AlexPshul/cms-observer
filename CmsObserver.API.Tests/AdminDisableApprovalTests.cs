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

        await IngestAsync(client, factory.CmsCredentials, new
        {
            type = "publish",
            id = entityId,
            payload = new { title = "Disable candidate" },
            version = 1,
            timestamp = DateTimeOffset.Parse("2024-01-01T00:00:00Z")
        });

        var nonAdminDisable = await DisableAsync(client, factory.ObserverUserCredentials, entityId);
        var adminDisable = await DisableAsync(client, factory.ObserverAdminCredentials, entityId);

        var userEntities = await WaitForEntitiesAsync(client, factory.ObserverUserCredentials, "/entities", body =>
            MatchesArray(body, array => array.GetArrayLength() == 0));

        var adminEntities = await WaitForEntitiesAsync(client, factory.ObserverAdminCredentials, "/entities?includeUnpublished=true", body =>
            MatchesArray(body, array => array.EnumerateArray().Any(x => x.GetProperty("id").GetString() == entityId)));

        ApprovalJson.Verify(new
        {
            NonAdminDisable = nonAdminDisable,
            AdminDisable = adminDisable,
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

    private static async Task<EntitiesSnapshot> WaitForEntitiesAsync(HttpClient client, TestCredentials credentials, string url, Func<string, bool> condition)
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            var snapshot = await GetEntitiesAsync(client, credentials, url);
            if (condition(snapshot.RawBody)) return snapshot;

            await Task.Delay(100);
        }

        return await GetEntitiesAsync(client, credentials, url);
    }

    private static async Task<EntitiesSnapshot> GetEntitiesAsync(HttpClient client, TestCredentials credentials, string url)
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

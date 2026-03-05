using CmsObserver.API.Dtos;
using CmsObserver.API.Authentication;
using CmsObserver.Managers;
using System.Security.Claims;
using System.Text.Json;

namespace CmsObserver.API;

public static class EntitiesEndpointExtensions
{
    public static WebApplication RegisterEntitiesEndpoints(this WebApplication app)
    {
        app.MapGet("/entities", async (ICmsEntitiesManager manager, ClaimsPrincipal user, CancellationToken cancellationToken, bool includeUnpublished = false) =>
        {
            var canSeeUnpublished = user.IsInRole(CmsAuthenticationConstants.AdminRole);
            var includeUnpublishedForRequest = canSeeUnpublished && includeUnpublished;

            var entities = await manager.ListAsync(includeUnpublishedForRequest, cancellationToken);
            var result = entities
                .Select(entity => new CmsEntityDto(
                    entity.Id,
                    entity.Version,
                    ParsePayload(entity.PayloadJson),
                    entity.TimestampUtc,
                    entity.IsActive))
                .ToArray();

            return Results.Ok(result);
        })
        .RequireAuthorization(CmsAuthenticationConstants.ObserverUserPolicy);

        app.MapPost("/admin/entities/{id}/disable", async (string id, ICmsEntitiesManager manager, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(id)) return Results.BadRequest();

            var disabled = await manager.DisableAsync(id, cancellationToken);
            if (!disabled) return Results.NotFound();

            return Results.NoContent();
        })
        .RequireAuthorization(CmsAuthenticationConstants.ObserverAdminPolicy);

        return app;
    }

    private static JsonElement ParsePayload(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson)) return JsonElement.Parse("{}");

        try
        {
            return JsonElement.Parse(payloadJson);
        }
        catch (JsonException)
        {
            return JsonElement.Parse(JsonSerializer.Serialize(payloadJson));
        }
    }
}


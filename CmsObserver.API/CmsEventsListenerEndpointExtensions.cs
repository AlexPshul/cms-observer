using CmsObserver.API.Dtos;
using CmsObserver.API.Authentication;
using CmsObserver.Managers;
using CmsObserver.Managers.Models;
using System.Security.Claims;

namespace CmsObserver.API;

public static class CmsEventsListenerEndpointExtensions
{
    public static WebApplication RegisterCmsEventsListener(this WebApplication app)
    {
        var cmsGroup = app
            .MapGroup("/cms")
            .RequireAuthorization(CmsAuthenticationConstants.CmsEventsIngestionPolicy);

        cmsGroup.MapPost("/events", (IReadOnlyCollection<CmsEventDto> events, ICmsEventProcessor processor, ClaimsPrincipal user, ILogger<WebApplication> logger) =>
            {
                var username = user.Identity?.Name ?? "unknown";
                logger.LogInformation("Batch received from username {Username}. Event count: {EventCount}", username, events?.Count ?? 0);

                if (events is null) return Results.BadRequest();

                var cmsEventModels = events
                    .Select(cmsEvent => new CmsEventModel(
                        MapType(cmsEvent.Type),
                        cmsEvent.Id,
                        cmsEvent.Timestamp,
                        cmsEvent.Payload,
                        cmsEvent.Version))
                    .ToArray();

                processor.Enqueue(cmsEventModels);
                return Results.Accepted();
            });

        return app;
    }

    private static CmsEventModelType MapType(CmsEventDtoType type) => type switch
    {
        CmsEventDtoType.Publish => CmsEventModelType.Publish,
        CmsEventDtoType.Unpublish => CmsEventModelType.Unpublish,
        CmsEventDtoType.Delete => CmsEventModelType.Delete,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported CMS event type")
    };
}

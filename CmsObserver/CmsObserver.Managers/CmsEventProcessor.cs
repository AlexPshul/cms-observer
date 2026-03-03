using System.Reactive.Linq;
using System.Reactive.Subjects;
using CmsObserver.Accessors;
using CmsObserver.Accessors.Entities;
using CmsObserver.Managers.Models;
using Microsoft.Extensions.Logging;

namespace CmsObserver.Managers;

public sealed class CmsEventProcessor : ICmsEventProcessor, IDisposable
{
    private readonly ISubject<IReadOnlyCollection<CmsEventModel>> _eventsStream;
    private readonly IDisposable _subscription;
    private readonly IEntitiesAccessor _entitiesAccessor;
    private readonly ILogger<CmsEventProcessor> _logger;

    public CmsEventProcessor(IEntitiesAccessor entitiesAccessor, ILogger<CmsEventProcessor> logger)
    {
        _entitiesAccessor = entitiesAccessor;
        _logger = logger;

        var subject = new Subject<IReadOnlyCollection<CmsEventModel>>();
        _eventsStream = Subject.Synchronize(subject);
        _subscription = _eventsStream
            .SelectMany(batch => batch)
            .Select(cmsEvent => Observable.FromAsync(ct => PersistAsync(cmsEvent, ct)))
            .Concat()
            .Subscribe(cmsEvent =>
                _logger.LogInformation("Received CMS event: Type={Type}, Id={Id}, Version={Version}, Timestamp={Timestamp}",
                    cmsEvent.Type,
                    cmsEvent.Id,
                    cmsEvent.Version?.ToString() ?? "n/a",
                    cmsEvent.Timestamp));
    }

    public void Enqueue(IReadOnlyCollection<CmsEventModel> events)
    {
        if (events is null) return;

        _eventsStream.OnNext(events);
    }

    public void Dispose()
    {
        _subscription.Dispose();
        _eventsStream.OnCompleted();
    }

    private async Task<CmsEventModel> PersistAsync(CmsEventModel cmsEvent, CancellationToken cancellationToken)
    {
        await (cmsEvent.Type switch
        {
            CmsEventModelType.Publish => _entitiesAccessor.UpsertAsync(MapEntity(cmsEvent, true), cancellationToken),
            CmsEventModelType.Unpublish => _entitiesAccessor.UpsertAsync(MapEntity(cmsEvent, false), cancellationToken),
            CmsEventModelType.Delete => _entitiesAccessor.DeleteAsync(cmsEvent.Id, cmsEvent.Timestamp, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(cmsEvent.Type), cmsEvent.Type, "Unsupported CMS event type")
        });

        return cmsEvent;
    }

    private static CmsEntity MapEntity(CmsEventModel cmsEvent, bool isActive)
        => new()
        {
            Id = cmsEvent.Id,
            Version = cmsEvent.Version ?? 0,
            PayloadJson = cmsEvent.Payload?.GetRawText() ?? "{}",
            TimestampUtc = cmsEvent.Timestamp,
            IsActive = isActive,
            IsDisabledByAdmin = false
        };
}

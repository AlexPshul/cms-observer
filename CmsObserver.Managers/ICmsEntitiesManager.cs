using CmsObserver.Managers.Models;

namespace CmsObserver.Managers;

public interface ICmsEntitiesManager
{
    Task<IReadOnlyCollection<CmsEntityModel>> ListAsync(bool includeUnpublished, CancellationToken cancellationToken = default);
    Task<bool> DisableAsync(string id, CancellationToken cancellationToken = default);
}

using CmsObserver.Accessors.Entities;

namespace CmsObserver.Accessors;

public interface IEntitiesAccessor
{
    Task UpsertAsync(CmsEntity entity, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CmsEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, DateTimeOffset eventTimestampUtc, CancellationToken cancellationToken = default);
}

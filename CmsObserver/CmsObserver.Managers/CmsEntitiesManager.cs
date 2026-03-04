using CmsObserver.Accessors;
using CmsObserver.Managers.Models;

namespace CmsObserver.Managers;

public sealed class CmsEntitiesManager : ICmsEntitiesManager
{
    private readonly IEntitiesAccessor _entitiesAccessor;

    public CmsEntitiesManager(IEntitiesAccessor entitiesAccessor)
    {
        _entitiesAccessor = entitiesAccessor;
    }

    public async Task<IReadOnlyCollection<CmsEntityModel>> ListAsync(bool includeUnpublished, CancellationToken cancellationToken = default)
    {
        var entities = includeUnpublished
            ? await _entitiesAccessor.GetAllAsync(cancellationToken)
            : await _entitiesAccessor.GetAllActiveAsync(cancellationToken);

        return entities
            .Select(entity => new CmsEntityModel
            {
                Id = entity.Id,
                Version = entity.Version,
                PayloadJson = entity.PayloadJson,
                TimestampUtc = entity.TimestampUtc,
                IsActive = entity.IsActive
            })
            .ToArray();
    }

    public Task<bool> DisableAsync(string id, CancellationToken cancellationToken = default) => _entitiesAccessor.DisableByAdminAsync(id, cancellationToken);
}

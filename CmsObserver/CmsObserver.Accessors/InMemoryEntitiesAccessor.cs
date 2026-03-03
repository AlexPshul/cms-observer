using System.Collections.Concurrent;
using CmsObserver.Accessors.Entities;

namespace CmsObserver.Accessors;

public sealed class InMemoryEntitiesAccessor : IEntitiesAccessor
{
    private readonly ConcurrentDictionary<string, CmsEntity> _entities = new();

    public Task UpsertAsync(CmsEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (string.IsNullOrWhiteSpace(entity.Id)) throw new ArgumentException("Entity id is required.", nameof(entity));

        _entities.AddOrUpdate(
            entity.Id,
            entity,
            (_, existing) => entity.TimestampUtc > existing.TimestampUtc ? entity : existing);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<CmsEntity>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<CmsEntity> entities = _entities.Values
            .Where(entity => entity.IsActive && !entity.IsDisabledByAdmin)
            .ToArray();
        return Task.FromResult(entities);
    }

    public Task<IReadOnlyCollection<CmsEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<CmsEntity> entities = _entities.Values.ToArray();
        return Task.FromResult(entities);
    }

    public Task<bool> DeleteAsync(string id, DateTimeOffset eventTimestampUtc, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Entity id is required.", nameof(id));

        if (!_entities.TryGetValue(id, out var existing)) return Task.FromResult(false);
        if (eventTimestampUtc <= existing.TimestampUtc) return Task.FromResult(false);
        return Task.FromResult(_entities.TryRemove(new KeyValuePair<string, CmsEntity>(id, existing)));
    }
}

using CmsObserver.Accessors.Entities;
using Microsoft.EntityFrameworkCore;

namespace CmsObserver.Accessors;

public sealed class PersistentEntitiesAccessor : IEntitiesAccessor
{
    private readonly IDbContextFactory<CmsEntitiesDbContext> _dbContextFactory;

    public PersistentEntitiesAccessor(IDbContextFactory<CmsEntitiesDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task UpsertAsync(CmsEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (string.IsNullOrWhiteSpace(entity.Id)) throw new ArgumentException("Entity id is required.", nameof(entity));

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await dbContext.Entities.SingleOrDefaultAsync(x => x.Id == entity.Id, cancellationToken);

        if (existing is null)
        {
            dbContext.Entities.Add(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        if (entity.TimestampUtc <= existing.TimestampUtc) return;

        dbContext.Entry(existing).CurrentValues.SetValues(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CmsEntity>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Entities
            .AsNoTracking()
            .Where(entity => entity.IsActive && !entity.IsDisabledByAdmin)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CmsEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Entities.AsNoTracking().ToArrayAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(string id, DateTimeOffset eventTimestampUtc, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Entity id is required.", nameof(id));

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await dbContext.Entities.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (existing is null) return false;
        if (eventTimestampUtc <= existing.TimestampUtc) return false;

        dbContext.Entities.Remove(existing);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

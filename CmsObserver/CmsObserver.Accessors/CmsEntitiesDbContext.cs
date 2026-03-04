using CmsObserver.Accessors.Entities;
using Microsoft.EntityFrameworkCore;

namespace CmsObserver.Accessors;

public sealed class CmsEntitiesDbContext : DbContext
{
    public CmsEntitiesDbContext(DbContextOptions<CmsEntitiesDbContext> options) : base(options) { }

    public DbSet<CmsEntity> Entities => Set<CmsEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CmsEntity>(entity =>
        {
            entity.ToTable("CmsEntities");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).IsRequired();
            entity.Property(x => x.PayloadJson).IsRequired();
            entity.Property(x => x.TimestampUtc).IsRequired();
        });
    }
}

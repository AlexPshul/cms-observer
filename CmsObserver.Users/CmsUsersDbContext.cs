using CmsObserver.Users.Entities;
using Microsoft.EntityFrameworkCore;

namespace CmsObserver.Users;

public sealed class CmsUsersDbContext(DbContextOptions<CmsUsersDbContext> options) : DbContext(options)
{
    public DbSet<CmsObserverUser> Users => Set<CmsObserverUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CmsObserverUser>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Username);
            entity.Property(x => x.Username).IsRequired();
            entity.Property(x => x.PasswordHashBase64).IsRequired();
            entity.Property(x => x.PasswordSaltBase64).IsRequired();
            entity.Property(x => x.Iterations).IsRequired();
            entity.Property(x => x.Role).IsRequired();
        });
    }
}

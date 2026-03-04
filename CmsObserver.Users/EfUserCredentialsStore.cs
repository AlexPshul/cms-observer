using CmsObserver.Users.Models;
using Microsoft.EntityFrameworkCore;

namespace CmsObserver.Users;

public sealed class EfUserCredentialsStore(IDbContextFactory<CmsUsersDbContext> dbContextFactory) : IUserCredentialsStore
{
    public async Task<CmsObserverUserCredentials?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username)) return null;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Username == username);

        if (user is null) return null;

        return new CmsObserverUserCredentials(
                 user.Username,
                 user.PasswordHashBase64,
                 user.PasswordSaltBase64,
                 user.Iterations,
                 user.Role);
    }
}

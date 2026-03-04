using CmsObserver.Users.Models;

namespace CmsObserver.Users;

public interface IUserCredentialsStore
{
    Task<CmsObserverUserCredentials?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
}

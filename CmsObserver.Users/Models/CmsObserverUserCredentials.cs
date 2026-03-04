namespace CmsObserver.Users.Models;

public sealed record CmsObserverUserCredentials(
    string Username,
    string PasswordHashBase64,
    string PasswordSaltBase64,
    int Iterations,
    string Role);

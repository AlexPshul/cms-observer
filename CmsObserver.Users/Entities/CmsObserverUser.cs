namespace CmsObserver.Users.Entities;

public sealed record CmsObserverUser
{
    public required string Username { get; init; }
    public required string PasswordHashBase64 { get; init; }
    public required string PasswordSaltBase64 { get; init; }
    public int Iterations { get; init; } = 120000;
    public required string Role { get; init; }
}

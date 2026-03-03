namespace CmsObserver.Managers.Models;

public sealed record CmsEntityModel
{
    public required string Id { get; init; }
    public required int Version { get; init; }
    public required string PayloadJson { get; init; }
    public required DateTimeOffset TimestampUtc { get; init; }
    public required bool IsActive { get; init; }
}

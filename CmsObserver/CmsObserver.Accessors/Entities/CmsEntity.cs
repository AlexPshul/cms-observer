namespace CmsObserver.Accessors.Entities;

public sealed record CmsEntity
{
    public required string Id { get; init; }
    public required int Version { get; init; }
    public required string PayloadJson { get; init; }
    public required DateTimeOffset TimestampUtc { get; init; }
    public bool IsActive { get; init; } = true;
    public bool IsDisabledByAdmin { get; init; }
}

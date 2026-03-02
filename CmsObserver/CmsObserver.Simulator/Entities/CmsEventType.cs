using System.Text.Json.Serialization;

namespace CmsObserver.Simulator.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum CmsEventType
{
    [JsonStringEnumMemberName("publish")]
    Published,
    [JsonStringEnumMemberName("unPublish")]
    Unpublished,
    [JsonStringEnumMemberName("delete")]
    Deleted
}

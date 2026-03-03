using System.Text.Json.Serialization;

namespace CmsObserver.API.Dtos;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CmsEventDtoType
{
    Publish,
    Unpublish,
    Delete
}

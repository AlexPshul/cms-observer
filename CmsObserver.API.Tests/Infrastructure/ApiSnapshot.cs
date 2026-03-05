using System.Text.Json;

namespace CmsObserver.API.Tests.Infrastructure;

public sealed record ApiSnapshot(string Url, int StatusCode, string Status, JsonElement Body)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string RawBody => Body.GetRawText();
}

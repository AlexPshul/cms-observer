using System.Text.Json;
using ApprovalTests;

namespace CmsObserver.API.Tests.Infrastructure;

public static class ApprovalJson
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public static void Verify(object value)
    {
        var json = JsonSerializer.Serialize(value, SerializerOptions);
        Approvals.VerifyJson(json);
    }
}

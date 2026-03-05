namespace CmsObserver.API.Tests.Infrastructure;

public sealed record ApiSnapshot(string Url, int StatusCode, string Status, string RawBody);

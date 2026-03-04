using System.ComponentModel.DataAnnotations;

namespace CmsObserver.API.Authentication;

public sealed class CmsBasicAuthOptions
{
    public const string SectionName = "CmsAuth";

    [Required]
    public string Username { get; init; } = string.Empty;

    [Required]
    public string PasswordHashBase64 { get; init; } = string.Empty;

    [Required]
    public string PasswordSaltBase64 { get; init; } = string.Empty;

    [Range(10000, int.MaxValue)]
    public int Iterations { get; init; } = 120000;
}

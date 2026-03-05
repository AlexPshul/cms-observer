using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using CmsObserver.Accessors;
using CmsObserver.API.Authentication;
using CmsObserver.Users;
using CmsObserver.Users.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CmsObserver.API.Tests.Infrastructure;

public sealed class CmsObserverApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const int PasswordIterations = 120000;
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "cms-observer-tests", Guid.NewGuid().ToString("N"));

    public TestCredentials CmsCredentials { get; } = new("cmshookuser01", "0f5811ec-0c66-42aa-a818-30f6aca3af57");
    public TestCredentials ObserverUserCredentials { get; } = new("observer.user", "user-password-1");
    public TestCredentials ObserverAdminCredentials { get; } = new("observer.admin", "admin-password-1");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_tempDirectory);

        var entitiesDbPath = Path.Combine(_tempDirectory, "cms-observer.entities.db");
        var usersDbPath = Path.Combine(_tempDirectory, "cms-observer.users.db");

        var cmsSalt = RandomNumberGenerator.GetBytes(16);
        var cmsHash = Rfc2898DeriveBytes.Pbkdf2(
            CmsCredentials.Password,
            cmsSalt,
            PasswordIterations,
            HashAlgorithmName.SHA256,
            32);

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            var overrides = new Dictionary<string, string?>
            {
                ["ConnectionStrings:CmsEntities"] = $"Data Source={entitiesDbPath}",
                ["ConnectionStrings:CmsUsers"] = $"Data Source={usersDbPath}",
                ["CmsAuth:Username"] = CmsCredentials.Username,
                ["CmsAuth:PasswordHashBase64"] = Convert.ToBase64String(cmsHash),
                ["CmsAuth:PasswordSaltBase64"] = Convert.ToBase64String(cmsSalt),
                ["CmsAuth:Iterations"] = PasswordIterations.ToString()
            };

            configurationBuilder.AddInMemoryCollection(overrides);
        });
    }

    public HttpClient CreateApiClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false
    });

    public static AuthenticationHeaderValue CreateBasicAuthHeader(TestCredentials credentials)
    {
        var raw = $"{credentials.Username}:{credentials.Password}";
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
        return new AuthenticationHeaderValue("Basic", encoded);
    }

    public async Task ResetStateAsync(CancellationToken cancellationToken = default)
    {
        await using (var entitiesDbContext = await Services
                         .GetRequiredService<IDbContextFactory<CmsEntitiesDbContext>>()
                         .CreateDbContextAsync(cancellationToken))
        {
            await entitiesDbContext.Database.EnsureCreatedAsync(cancellationToken);
            await entitiesDbContext.Entities.ExecuteDeleteAsync(cancellationToken);
        }

        await using (var usersDbContext = await Services
                         .GetRequiredService<IDbContextFactory<CmsUsersDbContext>>()
                         .CreateDbContextAsync(cancellationToken))
        {
            await usersDbContext.Database.EnsureCreatedAsync(cancellationToken);
            await usersDbContext.Users.ExecuteDeleteAsync(cancellationToken);

            usersDbContext.Users.AddRange(
                CreateUser(ObserverUserCredentials, "User"),
                CreateUser(ObserverAdminCredentials, "Admin"));

            await usersDbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task InitializeAsync()
    {
        _ = Services;
        await ResetStateAsync();
    }

    public new async Task DisposeAsync()
    {
        try
        {
            if (Directory.Exists(_tempDirectory)) Directory.Delete(_tempDirectory, true);
        }
        catch
        {
            // ignore cleanup failures in tests
        }

        await base.DisposeAsync();
    }

    private static CmsObserverUser CreateUser(TestCredentials credentials, string role)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            credentials.Password,
            salt,
            PasswordIterations,
            HashAlgorithmName.SHA256,
            32);

        return new CmsObserverUser
        {
            Username = credentials.Username,
            PasswordHashBase64 = Convert.ToBase64String(hash),
            PasswordSaltBase64 = Convert.ToBase64String(salt),
            Iterations = PasswordIterations,
            Role = role
        };
    }
}

public sealed record TestCredentials(string Username, string Password);

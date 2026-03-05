using CmsObserver.Accessors;
using CmsObserver.API;
using CmsObserver.API.Authentication;
using CmsObserver.Managers;
using CmsObserver.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("CmsEntities")
    ?? throw new InvalidOperationException("Connection string 'CmsEntities' is missing.");
var usersConnectionString = builder.Configuration.GetConnectionString("CmsUsers")
    ?? throw new InvalidOperationException("Connection string 'CmsUsers' is missing.");

builder.Services.AddOpenApi();
builder.Services.AddDbContextFactory<CmsEntitiesDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddDbContextFactory<CmsUsersDbContext>(options => options.UseSqlite(usersConnectionString));
builder.Services.AddSingleton<IEntitiesAccessor, PersistentEntitiesAccessor>();
builder.Services.AddSingleton<ICmsEventProcessor, CmsEventProcessor>();
builder.Services.AddSingleton<ICmsEntitiesManager, CmsEntitiesManager>();
builder.Services.AddSingleton<IUserCredentialsStore, EfUserCredentialsStore>();
builder.Services
    .AddOptions<CmsBasicAuthOptions>()
    .Bind(builder.Configuration.GetSection(CmsBasicAuthOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services
    .AddAuthentication(CmsAuthenticationConstants.Scheme)
    .AddScheme<AuthenticationSchemeOptions, CmsBasicAuthenticationHandler>(CmsAuthenticationConstants.Scheme, null)
    .AddScheme<AuthenticationSchemeOptions, CmsObserverAuthenticationHandler>(CmsAuthenticationConstants.UsersScheme, null);
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(CmsAuthenticationConstants.CmsEventsIngestionPolicy, policy =>
    {
        policy.AuthenticationSchemes.Add(CmsAuthenticationConstants.Scheme);
        policy.RequireAuthenticatedUser();
    });

    options.AddPolicy(CmsAuthenticationConstants.ObserverUserPolicy, policy =>
    {
        policy.AuthenticationSchemes.Add(CmsAuthenticationConstants.UsersScheme);
        policy.RequireAuthenticatedUser();
    });

    options.AddPolicy(CmsAuthenticationConstants.ObserverAdminPolicy, policy =>
    {
        policy.AuthenticationSchemes.Add(CmsAuthenticationConstants.UsersScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireRole(CmsAuthenticationConstants.AdminRole);
    });
});

var app = builder.Build();

await using (var dbContext = await app.Services
                   .GetRequiredService<IDbContextFactory<CmsEntitiesDbContext>>()
                   .CreateDbContextAsync())
{
    await dbContext.Database.EnsureCreatedAsync();
}

await using (var usersDbContext = await app.Services
                   .GetRequiredService<IDbContextFactory<CmsUsersDbContext>>()
                   .CreateDbContextAsync())
{
    await usersDbContext.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.RegisterCmsEventsListener();
app.RegisterEntitiesEndpoints();

app.Run();

public partial class Program;


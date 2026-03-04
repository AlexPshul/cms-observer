using CmsObserver.Accessors;
using CmsObserver.API;
using CmsObserver.API.Authentication;
using CmsObserver.Managers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("CmsEntities")
    ?? throw new InvalidOperationException("Connection string 'CmsEntities' is missing.");

builder.Services.AddOpenApi();
builder.Services.AddDbContextFactory<CmsEntitiesDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddSingleton<IEntitiesAccessor, PersistentEntitiesAccessor>();
builder.Services.AddSingleton<ICmsEventProcessor, CmsEventProcessor>();
builder.Services.AddSingleton<ICmsEntitiesManager, CmsEntitiesManager>();
builder.Services
    .AddOptions<CmsBasicAuthOptions>()
    .Bind(builder.Configuration.GetSection(CmsBasicAuthOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services
    .AddAuthentication(CmsAuthenticationConstants.Scheme)
    .AddScheme<AuthenticationSchemeOptions, CmsBasicAuthenticationHandler>(CmsAuthenticationConstants.Scheme, null);
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(CmsAuthenticationConstants.CmsEventsIngestionPolicy, policy =>
    {
        policy.AuthenticationSchemes.Add(CmsAuthenticationConstants.Scheme);
        policy.RequireAuthenticatedUser();
    });
});

var app = builder.Build();

await using (var dbContext = await app.Services
                   .GetRequiredService<IDbContextFactory<CmsEntitiesDbContext>>()
                   .CreateDbContextAsync())
{
    await dbContext.Database.EnsureCreatedAsync();
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

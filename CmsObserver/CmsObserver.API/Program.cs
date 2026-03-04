using CmsObserver.Accessors;
using CmsObserver.API;
using CmsObserver.Managers;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("CmsEntities")
    ?? throw new InvalidOperationException("Connection string 'CmsEntities' is missing.");

builder.Services.AddOpenApi();
builder.Services.AddDbContextFactory<CmsEntitiesDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddSingleton<IEntitiesAccessor, PersistentEntitiesAccessor>();
builder.Services.AddSingleton<ICmsEventProcessor, CmsEventProcessor>();
builder.Services.AddSingleton<ICmsEntitiesManager, CmsEntitiesManager>();

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

app.RegisterCmsEventsListener();
app.RegisterEntitiesEndpoints();

app.Run();

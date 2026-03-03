using CmsObserver.Accessors;
using CmsObserver.API;
using CmsObserver.Managers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<IEntitiesAccessor, InMemoryEntitiesAccessor>();
builder.Services.AddSingleton<ICmsEventProcessor, CmsEventProcessor>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.RegisterCmsEventsListener();

app.Run();

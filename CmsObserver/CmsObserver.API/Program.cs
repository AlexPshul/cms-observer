using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/cms/events", async (HttpRequest request) =>
{
    using var document = await JsonDocument.ParseAsync(request.Body);
    var events = document.RootElement.Clone();
    Console.WriteLine("Received: ");
    Console.WriteLine(document.RootElement.ToString());
    return Results.Ok(events);
});

app.Run();

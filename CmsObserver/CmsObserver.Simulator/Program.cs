using CmsObserver.Simulator.Entities;
using CmsObserver.Simulator.ScenarioInitializers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

static Uri PromptWebhookUrl()
{
    while (true)
    {
        Console.Write("Webhook URL: ");
        var input = Console.ReadLine()?.Trim();

        if (Uri.TryCreate(input, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return uri;
        }

        Console.WriteLine("Please enter a valid http/https URL.");
    }
}

static void PrintMenu(IReadOnlyList<ICmsEventsSimulator> simulators)
{
    Console.WriteLine();
    Console.WriteLine("Select scenario:");

    for (var index = 0; index < simulators.Count; index++)
    {
        Console.WriteLine($"{index + 1} - {simulators[index].MenuTitle}");
    }

    Console.WriteLine("0 - Exit");
    Console.Write("Option: ");
}

static ICmsEventsSimulator? ResolveSimulator(string? choice, IReadOnlyList<ICmsEventsSimulator> simulators)
{
    if (!int.TryParse(choice, out var option)) return null;
    if (option <= 0 || option > simulators.Count) return null;

    return simulators[option - 1];
}

static async Task SendBatchAsync(
    HttpClient httpClient,
    Uri webhookUrl,
    IReadOnlyList<CmsEvent> events,
    JsonSerializerOptions options)
{
    var request = new HttpRequestMessage(HttpMethod.Post, webhookUrl)
    {
        Content = new StringContent(JsonSerializer.Serialize(events, options), Encoding.UTF8, "application/json")
    };

    Console.WriteLine();
    Console.WriteLine($"Sending {events.Count} event(s) to {webhookUrl}...");

    try
    {
        using var response = await httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"Response: {(int)response.StatusCode} {response.ReasonPhrase}");
        if (!string.IsNullOrWhiteSpace(body))
        {
            Console.WriteLine("Response body:");
            Console.WriteLine(JsonSerializer.Serialize(JsonSerializer.Deserialize<object>(body)));
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Request failed: {ex.Message}");
    }
}

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true
};

var simulators = new ICmsEventsSimulator[]
{
    new AvengersAssembleSimulator(),
    new SpiderManErasSimulator(),
    new TchallaCornerCaseSimulator(),
    new GambitCornerCaseSimulator(),
    new KangDynastySimulator(),
    new DeadpoolRebootSimulator()
};

Console.WriteLine("CMS Observer Simulator");

var webhookUrl = PromptWebhookUrl();

using var httpClient = new HttpClient();

while (true)
{
    PrintMenu(simulators);
    var choice = Console.ReadLine()?.Trim();

    if (choice is "0")
    {
        Console.WriteLine("Simulation ended.");
        break;
    }

    var simulator = ResolveSimulator(choice, simulators);
    if (simulator is null)
    {
        Console.WriteLine("Invalid option. Please choose one of the menu options.");
        continue;
    }

    var events = simulator.Simulate();
    await SendBatchAsync(httpClient, webhookUrl, events, jsonOptions);
}

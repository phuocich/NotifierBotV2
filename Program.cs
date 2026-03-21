using NotifierBotV2;
using System.Text;
using System.Text.Json;

var basePath = Directory.GetCurrentDirectory();
var dataPath = Path.Combine(basePath, "Data", "data.json");
var statePath = Path.Combine(basePath, "Data", "state.json");

Console.WriteLine($"Data path: {dataPath}");
Console.WriteLine($"State path: {statePath}");

if (!File.Exists(dataPath))
{
    Console.WriteLine("data.json not found.");
    return;
}

// load items
var json = await File.ReadAllTextAsync(dataPath);
var items = JsonSerializer.Deserialize<List<Verse>>(
    json,
    new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    });

if (items == null || items.Count == 0)
{
    Console.WriteLine("No items found.");
    return;
}

// ensure state file exists
if (!File.Exists(statePath) || new FileInfo(statePath).Length == 0)
{
    var initState = new State();
    await File.WriteAllTextAsync(
        statePath,
        JsonSerializer.Serialize(initState, new JsonSerializerOptions { WriteIndented = true })
    );
}

// load state
var stateJson = await File.ReadAllTextAsync(statePath);
var state = JsonSerializer.Deserialize<State>(stateJson,
    new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    }) ?? new State();

Console.WriteLine($"LastItem: {state.LastVerse}");
Console.WriteLine($"LastDate: {state.LastDate}");

// next item
var nextItem = state.LastVerse + 1;
if (nextItem > items.Count)
    nextItem = 1;

var item = items.FirstOrDefault(x => x.Number == nextItem);
if (item == null)
{
    Console.WriteLine("Item not found.");
    return;
}

// build message
var sb = new StringBuilder();
sb.AppendLine($"📜 Pháp Số – Mục {item.Number}");
sb.AppendLine(item.Chapter);
sb.AppendLine();
sb.AppendLine(item.Label);
sb.AppendLine();
sb.AppendLine(item.Content.Trim());

if (!string.IsNullOrWhiteSpace(item.Reference))
{
    sb.AppendLine();
    sb.AppendLine($"📖 {item.Reference.Trim()}");
}

var message = sb.ToString();

var audioService = new AudioService();
var telegram = new TelegramService();

var audioStream = await audioService.GenerateAsync(item);

await telegram.SendMessage(message);
await telegram.SendAudio(
    audioStream,
    $"{item.Number}_Dhamma.mp3",
    $"📜 Pháp Số – Mục {item.Number}"
);

Console.WriteLine($"Sent item {nextItem}");

// update state
state.LastVerse = nextItem;
state.LastDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
await File.WriteAllTextAsync(
    statePath,
    JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true })
);

Console.WriteLine("State updated.");
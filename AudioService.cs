using Google.Apis.Auth.OAuth2;
using Google.Cloud.TextToSpeech.V1;
using System.Text;

namespace NotifierBotV2;

public class AudioService
{
    private const int MaxSsmlBytes = 5000;

    private readonly TextToSpeechClient _client;

    public AudioService()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "key.json");

        try
        {
            var credential = GoogleCredential
                .FromFile(path)
                .CreateScoped(TextToSpeechClient.DefaultScopes);

            _client = new TextToSpeechClientBuilder
            {
                Credential = credential
            }.Build();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AudioService] Init failed: {ex}");
            throw;
        }
    }

    public async Task<MemoryStream> GenerateAsync(Item item)
    {
        var script = BuildScript(item);
        var ssmlChunks = SplitSsml(script, item.Number);

        var voice = new VoiceSelectionParams
        {
            LanguageCode = "vi-VN",
            Name = "vi-VN-Wavenet-B"
        };
        var config = new AudioConfig
        {
            AudioEncoding = AudioEncoding.Mp3
        };

        var tasks = ssmlChunks.Select(ssml =>
            _client.SynthesizeSpeechAsync(
                new SynthesisInput { Ssml = ssml }, voice, config));

        var responses = await Task.WhenAll(tasks);
        var audioParts = responses.Select(r => r.AudioContent.ToByteArray()).ToList();

        var combined = audioParts.Count == 1
            ? audioParts[0]
            : audioParts.SelectMany(b => b).ToArray();

        var stream = new MemoryStream(combined);
        stream.Position = 0;
        return stream;
    }

    private List<string> SplitSsml(string rawScript, int number)
    {
        var prefix = $"Pháp Số {number}";
        var overhead = Encoding.UTF8.GetByteCount(
            $"<speak>{prefix}<break time=\"500ms\"/></speak>");
        var maxContentBytes = MaxSsmlBytes - overhead;

        var normalized = Normalize(rawScript);
        var sentences = System.Text.RegularExpressions.Regex
            .Split(normalized, @"(?<=\.)\s+")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        var chunks = new List<string>();
        var buf = new StringBuilder();

        foreach (var sentence in sentences)
        {
            var encoded = Escape(sentence);
            var sentenceBytes = Encoding.UTF8.GetByteCount(encoded);

            if (sentenceBytes > maxContentBytes)
            {
                if (buf.Length > 0)
                {
                    chunks.Add(WrapSsml(buf.ToString(), prefix));
                    buf.Clear();
                }
                chunks.AddRange(SplitLongText(encoded, maxContentBytes, prefix));
            }
            else
            {
                var candidate = buf.Length > 0
                    ? buf.ToString() + " " + encoded
                    : encoded;
                if (Encoding.UTF8.GetByteCount(candidate) > maxContentBytes && buf.Length > 0)
                {
                    chunks.Add(WrapSsml(buf.ToString(), prefix));
                    buf.Clear();
                    buf.Append(encoded);
                }
                else
                {
                    if (buf.Length > 0) buf.Append(' ');
                    buf.Append(encoded);
                }
            }
        }

        if (buf.Length > 0)
            chunks.Add(WrapSsml(buf.ToString(), prefix));

        return chunks;
    }

    private static List<string> SplitLongText(string text, int maxBytes, string prefix)
    {
        var result = new List<string>();
        var words = text.Split(' ');
        var buf = new StringBuilder();

        foreach (var word in words)
        {
            var candidate = buf.Length > 0
                ? buf.ToString() + " " + word
                : word;
            if (Encoding.UTF8.GetByteCount(candidate) > maxBytes && buf.Length > 0)
            {
                result.Add(WrapSsml(buf.ToString(), prefix));
                buf.Clear();
                buf.Append(word);
            }
            else
            {
                if (buf.Length > 0) buf.Append(' ');
                buf.Append(word);
            }
        }

        if (buf.Length > 0)
            result.Add(WrapSsml(buf.ToString(), prefix));

        return result;
    }

    private static string WrapSsml(string escapedText, string prefix)
    {
        return $"<speak>{prefix}<break time=\"500ms\"/>{escapedText}</speak>";
    }

    private static string BuildScript(Item item)
    {
        var label = System.Text.RegularExpressions.Regex
            .Replace(item.Label, @"^\[\d+\]\s*", string.Empty)
            .Trim();

        return $"{label}. {item.Content}";
    }

    private static string Normalize(string text)
    {
        return text
            .Replace("\n", ". ")
            .Replace("  ", " ")
            .Trim();
    }

    private static string Escape(string text)
    {
        return System.Security.SecurityElement.Escape(text) ?? string.Empty;
    }
}
using Google.Apis.Auth.OAuth2;
using Google.Cloud.TextToSpeech.V1;
using NotifierBotV2;

namespace NotifierBotV2;

public class AudioService
{
    private readonly TextToSpeechClient _client;

    public AudioService()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "key.json");

        var credential = GoogleCredential
            .FromFile(path)
            .CreateScoped(TextToSpeechClient.DefaultScopes);

        _client = new TextToSpeechClientBuilder
        {
            Credential = credential
        }.Build();
    }

    public async Task<MemoryStream> GenerateAsync(Verse item)
    {
        var script = BuildScript(item);

        var input = new SynthesisInput
        {
            Ssml = $@"<speak>
                        Pháp Số {item.Number}
                        <break time=""500ms""/>
                        {Escape(Normalize(script))}
                      </speak>"
        };

        var voice = new VoiceSelectionParams
        {
            LanguageCode = "vi-VN",
            Name = "vi-VN-Wavenet-B"
        };

        var config = new AudioConfig
        {
            AudioEncoding = AudioEncoding.Mp3
        };

        var response = await _client.SynthesizeSpeechAsync(input, voice, config);

        var stream = new MemoryStream(response.AudioContent.ToByteArray());
        stream.Position = 0;
        return stream;
    }

    private static string BuildScript(Verse item)
    {
        // Strip [N] prefix from label for cleaner audio
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
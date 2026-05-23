using System.Net.Http.Json;

namespace NotifierBotV2
{
    public class TelegramService
    {
        private const int MaxMessageLength = 4096;

        private readonly HttpClient _http = new();

        private readonly string _token;
        private readonly string _chatId;

        public TelegramService()
        {
            _token = Environment.GetEnvironmentVariable("TELEGRAM_TOKEN") ?? "";
            _chatId = Environment.GetEnvironmentVariable("TELEGRAM_CHAT_ID") ?? "";

            if (string.IsNullOrWhiteSpace(_token))
                throw new Exception("TELEGRAM_TOKEN not found");

            if (string.IsNullOrWhiteSpace(_chatId))
                throw new Exception("TELEGRAM_CHAT_ID not found");
        }

        public async Task SendMessage(string message)
        {
            var url = $"https://api.telegram.org/bot{_token}/sendMessage";

            if (message.Length <= MaxMessageLength)
            {
                var response = await _http.PostAsJsonAsync(url, new
                {
                    chat_id = _chatId,
                    text = message
                });
                await EnsureSuccess(response, "sendMessage");
                return;
            }

            // Split into chunks at newline boundaries
            foreach (var chunk in SplitMessage(message))
            {
                var response = await _http.PostAsJsonAsync(url, new
                {
                    chat_id = _chatId,
                    text = chunk
                });
                await EnsureSuccess(response, "sendMessage");
            }
        }

        public async Task SendAudio(Stream stream, string fileName, string caption)
        {
            var url = $"https://api.telegram.org/bot{_token}/sendAudio";

            using var form = new MultipartFormDataContent();

            form.Add(new StringContent(_chatId), "chat_id");
            form.Add(new StringContent(caption), "caption");

            var streamContent = new StreamContent(stream);
            form.Add(streamContent, "audio", fileName);

            var response = await _http.PostAsync(url, form);
            await EnsureSuccess(response, "sendAudio");
        }

        private static IEnumerable<string> SplitMessage(string message)
        {
            var lines = message.Split('\n');
            var current = new System.Text.StringBuilder();

            foreach (var line in lines)
            {
                if (current.Length + line.Length + 1 > MaxMessageLength && current.Length > 0)
                {
                    yield return current.ToString().TrimEnd();
                    current.Clear();
                }

                if (current.Length > 0)
                    current.Append('\n');
                current.Append(line);
            }

            if (current.Length > 0)
                yield return current.ToString().TrimEnd();
        }

        private static async Task EnsureSuccess(HttpResponseMessage response, string method)
        {
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new Exception($"Telegram {method} failed ({response.StatusCode}): {body}");
            }
        }
    }
}
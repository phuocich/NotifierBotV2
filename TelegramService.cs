using System.Net.Http.Json;

namespace NotifierBotV2
{
    public class TelegramService
    {
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

            await _http.PostAsJsonAsync(url, new
            {
                chat_id = _chatId,
                text = message
            });
        }

        public async Task SendAudio(Stream stream, string fileName, string caption)
        {
            var url = $"https://api.telegram.org/bot{_token}/sendAudio";

            using var form = new MultipartFormDataContent();

            form.Add(new StringContent(_chatId), "chat_id");
            form.Add(new StringContent(caption), "caption");

            var streamContent = new StreamContent(stream);
            form.Add(streamContent, "audio", fileName);

            await _http.PostAsync(url, form);
        }
    }
}
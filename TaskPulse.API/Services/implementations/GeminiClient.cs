using System.Net.Http.Json;
using System.Text.Json;

namespace TaskPulse.API.Services.implementations
{
    // Thin wrapper around Gemini's generateContent REST endpoint. Every AI
    // feature (project summary, risk detection, the help assistant) goes
    // through here so the HTTP/parsing details live in exactly one place.
    //
    // Gemini:ApiKey is a secret — read from IConfiguration, backed by
    // user-secrets in Development, never checked into appsettings.json.
    public class GeminiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiClient> _logger;

        public GeminiClient(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiClient> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_configuration["Gemini:ApiKey"]);

        // Single-shot: one prompt in, one reply out. Used by the AI
        // summary/risk-detection features, which don't need conversation state.
        public Task<string?> GenerateAsync(string prompt, CancellationToken ct = default)
        {
            var payload = new
            {
                contents = new[]
                {
                    new { role = "user", parts = new[] { new { text = prompt } } }
                },
                generationConfig = new { temperature = 0.4 }
            };

            return SendAsync(payload, ct);
        }

        // Multi-turn: a system instruction (kept out of the conversation
        // history proper, per Gemini's API) plus alternating user/model
        // turns — what the help assistant chat uses so follow-up questions
        // can refer back to earlier answers.
        public Task<string?> GenerateChatAsync(string systemInstruction, IEnumerable<(string Role, string Text)> turns, CancellationToken ct = default)
        {
            var payload = new
            {
                systemInstruction = new { parts = new[] { new { text = systemInstruction } } },
                contents = turns.Select(t => new { role = t.Role, parts = new[] { new { text = t.Text } } }).ToArray(),
                generationConfig = new { temperature = 0.4 }
            };

            return SendAsync(payload, ct);
        }

        // Returns null on any failure (not configured, network error, bad
        // response) — callers decide how to degrade, this just doesn't throw
        // into a request pipeline over what's ultimately a best-effort call.
        private async Task<string?> SendAsync(object payload, CancellationToken ct)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("Gemini:ApiKey is not configured — AI request skipped.");
                return null;
            }

            var model = _configuration["Gemini:Model"] ?? "gemini-2.5-flash";
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            try
            {
                using var response = await _httpClient.PostAsJsonAsync(url, payload, ct);
                var json = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Gemini request failed ({Status}): {Body}", response.StatusCode, json);
                    return null;
                }

                using var doc = JsonDocument.Parse(json);

                var candidates = doc.RootElement.GetProperty("candidates");
                if (candidates.GetArrayLength() == 0)
                {
                    _logger.LogWarning("Gemini returned no candidates. Body: {Body}", json);
                    return null;
                }

                var text = candidates[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gemini request threw.");
                return null;
            }
        }
    }
}

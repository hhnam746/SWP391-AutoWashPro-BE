using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace SWP391_AutoWashPro_BE.Service.AiService;

public class GoogleAiStudioService
{
    private readonly HttpClient _httpClient;
    private readonly GoogleAiStudioOptions _options = new();

    public GoogleAiStudioService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        configuration.GetSection(nameof(GoogleAiStudioOptions)).Bind(_options);

        _options.ApiKey ??= configuration["GOOGLE_AI_API_KEY"];
        _options.BaseUrl ??= configuration["GOOGLE_AI_BASE_URL"];
        _options.Model ??= configuration["GOOGLE_AI_MODEL"];
        _options.FallbackModel ??= configuration["GOOGLE_AI_FALLBACK_MODEL"];

        _options.BaseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl)
            ? "https://generativelanguage.googleapis.com/v1beta"
            : _options.BaseUrl;
        _options.Model = string.IsNullOrWhiteSpace(_options.Model) ? "gemini-3.5-flash" : _options.Model;
        _options.Temperature ??= 0.2m;
        _options.TimeoutSeconds ??= 180;
        _options.MaxRetries ??= 2;
        _options.RetryDelayMs ??= 1000;

        _httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(30, _options.TimeoutSeconds.Value));
    }

    public async Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new Exception("Google AI Studio API key is not configured.");
        }

        var attemptedModels = BuildModelCandidates();
        Exception? lastException = null;

        foreach (var model in attemptedModels)
        {
            try
            {
                return await GenerateWithRetryAsync(model, prompt, cancellationToken);
            }
            catch (Exception ex)
            {
                lastException = ex;
            }
        }

        throw lastException ?? new Exception("Google AI Studio request failed.");
    }

    private async Task<string> GenerateWithRetryAsync(string model, string prompt, CancellationToken cancellationToken)
    {
        var maxAttempts = Math.Max(1, (_options.MaxRetries ?? 0) + 1);
        var delayMs = Math.Max(250, _options.RetryDelayMs ?? 1000);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var request = CreateRequest(model, prompt);
            HttpResponseMessage? response = null;
            string? payload = null;

            try
            {
                response = await _httpClient.SendAsync(request, cancellationToken);
                payload = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                if (attempt == maxAttempts)
                {
                    throw new TimeoutException(
                        $"Google AI Studio request timed out after {_httpClient.Timeout.TotalSeconds:0} seconds for model '{model}'.",
                        ex);
                }

                await Task.Delay(delayMs * attempt, cancellationToken);
                continue;
            }

            if (response.IsSuccessStatusCode)
            {
                using var document = JsonDocument.Parse(payload!);
                var content = ExtractGeneratedText(document.RootElement);

                if (string.IsNullOrWhiteSpace(content))
                {
                    throw new Exception("Google AI Studio returned an empty response.");
                }

                return content.Trim();
            }

            var isTransient = response.StatusCode is HttpStatusCode.ServiceUnavailable or (HttpStatusCode)429;
            if (!isTransient || attempt == maxAttempts)
            {
                throw new Exception(
                    $"Google AI Studio request failed for model '{model}' with status {(int)response.StatusCode}: {payload}");
            }

            await Task.Delay(delayMs * attempt, cancellationToken);
        }

        throw new Exception($"Google AI Studio request failed for model '{model}' after retry attempts.");
    }

    private HttpRequestMessage CreateRequest(string model, string prompt)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"models/{model}:generateContent?key={Uri.EscapeDataString(_options.ApiKey!)}");
        request.Content = JsonContent.Create(new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new
                        {
                            text = prompt
                        }
                    }
                }
            },
            generationConfig = new
            {
                temperature = _options.Temperature
            }
        });

        return request;
    }

    private List<string> BuildModelCandidates()
    {
        var models = new List<string> { _options.Model! };

        if (!string.IsNullOrWhiteSpace(_options.FallbackModel) &&
            !string.Equals(_options.FallbackModel, _options.Model, StringComparison.OrdinalIgnoreCase))
        {
            models.Add(_options.FallbackModel);
        }

        return models;
    }

    private static string? ExtractGeneratedText(JsonElement rootElement)
    {
        if (!rootElement.TryGetProperty("candidates", out var candidates) ||
            candidates.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var candidate in candidates.EnumerateArray())
        {
            if (!candidate.TryGetProperty("content", out var content) ||
                !content.TryGetProperty("parts", out var parts) ||
                parts.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var textParts = parts.EnumerateArray()
                .Where(part => part.TryGetProperty("text", out _))
                .Select(part => part.GetProperty("text").GetString())
                .Where(text => !string.IsNullOrWhiteSpace(text));

            var combined = string.Concat(textParts);
            if (!string.IsNullOrWhiteSpace(combined))
            {
                return combined;
            }
        }

        return null;
    }
}

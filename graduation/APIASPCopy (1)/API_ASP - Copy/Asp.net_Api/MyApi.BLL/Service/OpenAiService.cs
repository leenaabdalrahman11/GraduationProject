using System;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MyApi.DAL.Models;

namespace MyApi.BLL.Service;

public class OpenAiService : IOpenAiService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public OpenAiService(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string?> TranscribeAudioAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return null;

        var apiKey = GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        using var content = new MultipartFormDataContent();

        await using var stream = file.OpenReadStream();
        using var fileContent = new StreamContent(stream);

        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(file.ContentType)
                ? "audio/webm"
                : file.ContentType);

        content.Add(fileContent, "file", file.FileName);
        content.Add(new StringContent("gpt-4o-mini-transcribe"), "model");
        content.Add(new StringContent("json"), "response_format");

        var response = await client.PostAsync(
            "https://api.openai.com/v1/audio/transcriptions",
            content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Transcription error: {error}");
            return null;
        }

        var responseBody = await response.Content.ReadAsStringAsync();

        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (root.TryGetProperty("text", out var textProp) &&
                textProp.ValueKind == JsonValueKind.String)
            {
                return textProp.GetString();
            }

            return responseBody;
        }
        catch
        {
            return responseBody;
        }
    }

    public async Task<CommandInterpretation?> InterpretCommandAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var apiKey = GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        var prompt = """
You are a voice shopping assistant router.

Read the user text and return JSON only.
No markdown.
No explanation.

Allowed actions:
- SearchProduct
- ViewCart
- TrackLatestOrder
- ViewOrders
- Repeat
- GoBack
- Exit
- Unknown

Rules:
- If the user asks to search/find/look for a product, set action = "SearchProduct".
- Put the product/category term inside "query".
- If the user only says "search for a product", query should be "".
- If the user asks about cart, set action = "ViewCart".
- If the user asks to track the latest order, set action = "TrackLatestOrder".
- If the user asks to view orders or my order, set action = "ViewOrders".
- If the user says repeat or relisten, set action = "Repeat".
- If the user says back or go back, set action = "GoBack".
- If the user says exit, quit, close, or goodbye, set action = "Exit".
- Otherwise set action = "Unknown".

Return exactly:
{"action":"...","query":"..."}

User text:
""" + text;

        var payload = new
        {
            model = "gpt-5.4",
            reasoning = new { effort = "minimal" },
            input = prompt
        };

        var json = JsonSerializer.Serialize(payload);
        using var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("https://api.openai.com/v1/responses", httpContent);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Interpretation error: {error}");
            return null;
        }

        var body = await response.Content.ReadAsStringAsync();
        var modelText = ExtractResponsesOutputText(body);

        if (string.IsNullOrWhiteSpace(modelText))
            return null;

        try
        {
            return JsonSerializer.Deserialize<CommandInterpretation>(
                modelText,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"JSON parse error: {ex.Message}");
            Console.WriteLine($"Model returned: {modelText}");
            return null;
        }
    }

    private string? GetApiKey()
    {
        return _config["OpenAI:ApiKey"]
               ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    }

    private static string? ExtractResponsesOutputText(string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        if (root.TryGetProperty("output_text", out var outputTextProp) &&
            outputTextProp.ValueKind == JsonValueKind.String)
        {
            return outputTextProp.GetString();
        }

        if (root.TryGetProperty("output", out var outputArray) &&
            outputArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in outputArray.EnumerateArray())
            {
                if (item.TryGetProperty("content", out var contentArray) &&
                    contentArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var contentItem in contentArray.EnumerateArray())
                    {
                        if (contentItem.TryGetProperty("text", out var textProp) &&
                            textProp.ValueKind == JsonValueKind.String)
                        {
                            return textProp.GetString();
                        }
                    }
                }
            }
        }

        return null;
    }
}
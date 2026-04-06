using System;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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
    
public async Task<string?> TranscribeAudioAsync(IFormFile file, string? language)
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
    content.Add(new StringContent(string.IsNullOrWhiteSpace(language) ? "ar" : language), "language");

    await using var stream = file.OpenReadStream();
    using var fileContent = new StreamContent(stream);

    fileContent.Headers.ContentType =
        new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(file.ContentType)
            ? "audio/webm"
            : file.ContentType);

    content.Add(fileContent, "file", file.FileName);
    content.Add(new StringContent("gpt-4o-transcribe"), "model");
    content.Add(new StringContent("json"), "response_format");

    var prompt = (language ?? "ar") == "en"
        ? "This audio is mostly English, sometimes mixed with Arabic names. Keep product names and brand names as spoken."
        : "This audio is mostly Arabic, sometimes mixed with English product names. Keep product names and brand names as spoken.";

    content.Add(new StringContent(prompt), "prompt");

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
public async Task<CommandInterpretation?> InterpretCommandAsync(string text, string? language)
{
    if (string.IsNullOrWhiteSpace(text))
        return null;

    var apiKey = GetApiKey();
    if (string.IsNullOrWhiteSpace(apiKey))
        return null;

    using var client = _httpClientFactory.CreateClient();
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", apiKey);

    var selectedLanguage = string.IsNullOrWhiteSpace(language) ? "ar" : language;

    var payload = new
    {
        model = "gpt-5.4",
        reasoning = new { effort = "minimal" },
        input = new object[]
        {
            new
            {
                role = "developer",
                content = new object[]
                {
                    new
                    {
                        type = "text",
                        text = """
You are a voice shopping assistant router.

Your job:
1. Correct obvious transcription mistakes without changing meaning.
2. Detect the intended action.
3. Extract the search term if present.
4. Respect the selected language.
5. Return structured JSON only.

Allowed actions:
- SearchProduct
- ViewCart
- TrackLatestOrder
- ViewOrders
- Repeat
- GoBack
- Exit
- GeneralConversation
- Unknown

Rules:
- If selected language is Arabic, prefer Arabic conversational understanding.
- If selected language is English, prefer English conversational understanding.
- If user asks to search/find/look for a product => SearchProduct
- If user asks about cart => ViewCart
- If user asks to track latest order => TrackLatestOrder
- If user asks to view orders => ViewOrders
- If user says repeat/relisten => Repeat
- If user says back/go back => GoBack
- If user says exit/quit/close/goodbye => Exit
- If user says greetings, thanks, or simple conversational phrases not related to store commands => GeneralConversation
- Otherwise => Unknown

Set confidence between 0 and 1.
If no product term exists, query should be empty string.
"""
                    }
                }
            },
            new
            {
                role = "user",
                content = new object[]
                {
                    new
                    {
                        type = "text",
                        text = $"Selected language: {selectedLanguage}. User text: {text}"
                    }
                }
            }
        },
        text = new
        {
            format = new
            {
                type = "json_schema",
                name = "voice_command_result",
                schema = new
                {
                    type = "object",
                    additionalProperties = false,
                    properties = new
                    {
                        action = new
                        {
                            type = "string",
                            @enum = new[]
                            {
                                "SearchProduct",
                                "ViewCart",
                                "TrackLatestOrder",
                                "ViewOrders",
                                "Repeat",
                                "GoBack",
                                "Exit",
                                "GeneralConversation",
                                "Unknown"
                            }
                        },
                        query = new { type = "string" },
                        correctedText = new { type = "string" },
                        confidence = new { type = "number" }
                    },
                    required = new[] { "action", "query", "correctedText", "confidence" }
                }
            }
        }
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
        using var doc = JsonDocument.Parse(modelText);
        var root = doc.RootElement;

        return new CommandInterpretation
        {
            Action = root.GetProperty("action").GetString(),
            Query = root.GetProperty("query").GetString(),
            CorrectedText = root.GetProperty("correctedText").GetString(),
            Confidence = root.GetProperty("confidence").GetDouble()
        };
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
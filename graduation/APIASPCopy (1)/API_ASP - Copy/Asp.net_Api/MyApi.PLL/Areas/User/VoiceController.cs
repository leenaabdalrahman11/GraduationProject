using Microsoft.AspNetCore.Mvc;
using MyApi.BLL.Service;
using MyApi.DAL.Models;

namespace MyApi.PLL.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VoiceController : ControllerBase
{
    private readonly IVoiceAssistantService _voiceAssistantService;
    private readonly IOpenAiService _openAiService;

    public VoiceController(
        IVoiceAssistantService voiceAssistantService,
        IOpenAiService openAiService)
    {
        _voiceAssistantService = voiceAssistantService;
        _openAiService = openAiService;
    }

    [HttpGet("start")]
    public IActionResult Start()
    {
        return Ok(new
        {
            message = "Hi, how can I help you? You can say search for a product, view cart items, track your latest order, or view my order.",
            options = new[]
            {
                "Search for a product",
                "View cart items",
                "Track your latest order",
                "View my order"
            },
            status = "ready"
        });
    }

[HttpPost("transcribe")]
public async Task<IActionResult> Transcribe(IFormFile file, [FromForm] string? language)
{
    if (file == null || file.Length == 0)
        return BadRequest("No audio file uploaded.");

    var text = await _openAiService.TranscribeAudioAsync(file, language);

    return Ok(new { text });
}
[HttpPost("command")]
public async Task<IActionResult> ExecuteCommand([FromBody] VoiceCommandRequest request)
{
    if (request == null || string.IsNullOrWhiteSpace(request.Text))
        return BadRequest(new { message = "Text is required" });

    var result = await _voiceAssistantService.ExecuteCommandAsync(request.Text, request.Language);
    return Ok(result);
}
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MyApi.BLL.Service;
using MyApi.DAL.Models;

namespace MyApi.PLL.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VoiceController : ControllerBase
    {
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
public async Task<IActionResult> Transcribe(
    [FromServices] IOpenAiService openAiService)
{
    var form = await Request.ReadFormAsync();
    var file = form.Files["file"];

    if (file is null || file.Length == 0)
    {
        return BadRequest(new { error = "No audio file uploaded." });
    }

    var result = await openAiService.TranscribeAudioAsync(file);

    if (string.IsNullOrWhiteSpace(result))
    {
        return Problem("Audio transcription failed.");
    }

    return Ok(new { text = result });
}

        [HttpPost("command")]
        public async Task<IActionResult> Command(
            [FromBody] VoiceCommandRequest request,
            [FromServices] IVoiceAssistantService voiceAssistantService)
        {
            var result = await voiceAssistantService.ExecuteCommandAsync(request.Text);
            return Ok(result);
        }
    }
}
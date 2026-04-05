using Microsoft.AspNetCore.Http;
using MyApi.DAL.Models;

namespace MyApi.BLL.Service;

public interface IOpenAiService
{
    Task<string?> TranscribeAudioAsync(IFormFile file, string? language);
    Task<CommandInterpretation?> InterpretCommandAsync(string text);
}
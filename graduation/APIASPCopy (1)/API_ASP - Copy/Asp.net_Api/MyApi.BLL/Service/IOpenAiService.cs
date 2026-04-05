using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using MyApi.DAL.Models;

namespace MyApi.BLL.Service
{
    public interface IOpenAiService
    {
        Task<string?> TranscribeAudioAsync(IFormFile file);
        Task<CommandInterpretation?> InterpretCommandAsync(string text);
    }
}
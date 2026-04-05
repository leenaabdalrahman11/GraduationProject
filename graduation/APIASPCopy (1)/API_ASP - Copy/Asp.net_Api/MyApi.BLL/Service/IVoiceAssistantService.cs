using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyApi.DAL.Models;

namespace MyApi.BLL.Service;
public interface IVoiceAssistantService
{
    Task<VoiceResponseDto> ExecuteCommandAsync(string text);
}
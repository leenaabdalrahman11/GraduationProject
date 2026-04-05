using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyApi.DAL.Models;
public class VoiceResponseDto
{
    public string Action { get; set; } = "";
    public string ReplyText { get; set; } = "";
    public string ScreenText { get; set; } = "";
    public object? Data { get; set; }
}
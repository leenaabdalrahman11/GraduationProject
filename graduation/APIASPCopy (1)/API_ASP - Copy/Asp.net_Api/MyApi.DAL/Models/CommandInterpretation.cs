namespace MyApi.DAL.Models;

public class CommandInterpretation
{
    public string? Action { get; set; }
    public string? Query { get; set; }
    public string? CorrectedText { get; set; }
    public double Confidence { get; set; }
    public bool NeedsConfirmation { get; set; }
}
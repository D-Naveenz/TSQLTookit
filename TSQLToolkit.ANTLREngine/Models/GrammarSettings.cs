namespace TSQLToolkit.ANTLREngine.Models;

public class GrammarSettings
{
    public string AntlrVersion { get; set; } = null!;
    public int JavaVersion { get; set; }
    public string? BaseDir { get; set; }
}
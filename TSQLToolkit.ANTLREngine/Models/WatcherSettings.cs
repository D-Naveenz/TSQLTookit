namespace TSQLToolkit.ANTLREngine.Models;

public class WatcherSettings
{
    public string WatchPath { get; set; } = null!;
    public string Filter { get; set; } = null!;
    public bool IsWatchSubdirectories { get; set; }
}
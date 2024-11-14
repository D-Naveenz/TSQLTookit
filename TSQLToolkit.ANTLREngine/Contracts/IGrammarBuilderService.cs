namespace TSQLToolkit.ANTLREngine.Contracts;

public interface IGrammarBuilderService
{
    string BaseDir { get; }
    string AntlrVersion { get; }
    string AntlrLocation { get; }
    string GrammarLocation { get; }

    void Initialize();

    void Cleanup();

    void Cleanup(string grammarPath);

    Task BuildAsync();

    Task BuildAsync(string grammarPath);
}
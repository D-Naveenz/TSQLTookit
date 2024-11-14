using Microsoft.Extensions.Options;
using System.Diagnostics;
using TSQLToolkit.ANTLREngine.Contracts;
using TSQLToolkit.ANTLREngine.Models;

namespace TSQLToolkit.ANTLREngine.Services;

public class GrammarBuilderService(ILogger<GrammarBuilderService> logger, IOptions<GrammarSettings> options, IHostApplicationLifetime hostApplicationLifetime)
    : IGrammarBuilderService
{
    private readonly GrammarSettings _grammarSettings = options.Value;
    private readonly string[] Folders = ["Lib", "Grammar"];

    public string BaseDir
    {
        get
        {
            return _grammarSettings.BaseDir ??
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ??
                throw new InvalidOperationException("Base directory cannot be determined.");
        }
    }

    public string AntlrVersion => _grammarSettings.AntlrVersion;
    public string AntlrLocation => Path.Combine(BaseDir, $"Lib/antlr-{AntlrVersion}-complete.jar");
    public string GrammarLocation => Path.Combine(BaseDir, "Grammar");

    public async void Initialize()
    {
        CreateDirectories();
        await CheckJavaVersion(_grammarSettings.JavaVersion);
        await CheckAntlrAvailableAsync();
    }

    public void Cleanup()
    {
        // Remove everything in the Grammar directory except .g4 files
        var directoryInfo = new DirectoryInfo(GrammarLocation);
        foreach (var file in directoryInfo.GetFiles().Where(file => file.Extension != ".g4"))
        {
            file.Delete();
        }

        foreach (var dir in directoryInfo.GetDirectories())
        {
            dir.Delete(true);
        }
    }

    public void Cleanup(string grammarPath)
    {
        var grammarName = Path.GetFileNameWithoutExtension(grammarPath);
        var outputDir = Path.Combine(GrammarLocation, grammarName);

        // Clean the output directory
        if (Directory.Exists(outputDir))
        {
            Directory.Delete(outputDir, true);
        }
    }

    public async Task BuildAsync()
    {
        // Cleanup the Grammar directory
        Cleanup();

        // Get the list of grammar files
        var grammarFiles = Directory.GetFiles(GrammarLocation, "*.g4");

        // Generate the parser files
        foreach (var grammarFile in grammarFiles)
        {
            var grammarName = Path.GetFileNameWithoutExtension(grammarFile);
            var outputDir = Path.Combine(GrammarLocation, grammarName);
            Directory.CreateDirectory(outputDir);

            logger.LogInformation("Generating parser for: {GrammarName}", Path.GetFileName(grammarFile));
            await RunAntlrToolAsync(grammarFile, outputDir);
        }
    }

    public async Task BuildAsync(string grammarPath)
    {
        var grammarName = Path.GetFileNameWithoutExtension(grammarPath);
        var outputDir = Path.Combine(GrammarLocation, grammarName);

        // Clean or create the output directory
        if (Directory.Exists(outputDir))
        {
            Directory.Delete(outputDir, true);
        }
        Directory.CreateDirectory(outputDir);

        logger.LogInformation("Generating parser for: {GrammarName}", Path.GetFileName(grammarPath));
        await RunAntlrToolAsync(grammarPath, outputDir);
    }

    private void CreateDirectories()
    {
        foreach (var folder in Folders)
        {
            var path = Path.Combine(BaseDir, folder);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }

    private async Task CheckJavaVersion(int requiredVersion)
    {
        try
        {
            // Run 'java -version' and capture the output
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = "-version",
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardError.ReadToEndAsync();
            process.WaitForExit();

            // Extract the version number
            var version = output.Split(' ')[2].Trim('"');
            int majorVersion = int.Parse(version.Split('.')[0]);

            if (majorVersion >= requiredVersion)
            {
                logger.LogInformation("Java version {Version} is supported.", version);
                return;
            }

            logger.LogError("Java version {Version} is not supported. Version {RequiredVersion} or higher is required.", version, requiredVersion);
            hostApplicationLifetime.StopApplication();
        }
        catch
        {
            logger.LogError("Java is not installed or cannot be found.");
            hostApplicationLifetime.StopApplication();
        }
    }

    private async Task CheckAntlrAvailableAsync()
    {
        if (!File.Exists(AntlrLocation))
        {
            logger.LogWarning("ANTLR library is missing. Downloading...");

            try
            {
                string url = $"https://www.antlr.org/download/antlr-{AntlrVersion}-complete.jar";
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(AntlrLocation, fileBytes);
                }

                logger.LogInformation("ANTLR library has been installed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to download ANTLR");
                hostApplicationLifetime.StopApplication();
            }
        }

        logger.LogInformation("ANTLR library is available.");
    }

    private async Task RunAntlrToolAsync(string grammarFile, string outputDir)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "java",
            Arguments = $"-jar {AntlrLocation} -Dlanguage=CSharp {grammarFile} -o {outputDir}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };
        process.Start();
        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();
        process.WaitForExit();

        if (!string.IsNullOrEmpty(error))
        {
            logger.LogError("ANTLR Error: {Error}", error);
        }
    }
}
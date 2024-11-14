using Microsoft.Extensions.Options;
using TSQLToolkit.ANTLREngine.Contracts;
using TSQLToolkit.ANTLREngine.Models;

namespace TSQLToolkit.ANTLREngine.Services
{
    public class GrammarWatchWorker(ILogger<GrammarWatchWorker> logger, IOptions<WatcherSettings> settings, IGrammarBuilderService grammarBuilderService)
        : BackgroundService
    {
        private readonly WatcherSettings _settings = settings.Value;
        private FileSystemWatcher? _watcher;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Initialize the grammar builder service
            grammarBuilderService.Initialize();

            // Create the file system watcher
            _watcher = new FileSystemWatcher
            {
                Path = Path.Combine(grammarBuilderService.BaseDir, _settings.WatchPath),
                Filter = _settings.Filter,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                EnableRaisingEvents = true,
                IncludeSubdirectories = _settings.IsWatchSubdirectories
            };

            // Event handlers
            _watcher.Changed += OnChangedAsync;
            _watcher.Created += OnChangedAsync;
            _watcher.Deleted += OnDeleted;

            while (!stoppingToken.IsCancellationRequested)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
                await Task.Delay(1000, stoppingToken);
            }
        }

        public override void Dispose()
        {
            if (_watcher is not null)
            {
                // Unsubscribe from events
                _watcher.Changed -= OnChangedAsync;
                _watcher.Created -= OnChangedAsync;
                _watcher.Deleted -= OnDeleted;

                // Dispose of the watcher
                _watcher.Dispose();
            }

            base.Dispose();
            GC.SuppressFinalize(this);
        }

        private async void OnChangedAsync(object sender, FileSystemEventArgs e)
        {
            logger.LogInformation("File: {fileName} {changeType}", e.Name, e.ChangeType);

            // Build the grammar
            await grammarBuilderService.BuildAsync(e.FullPath);
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            logger.LogInformation("File: {fileName} {changeType}", e.Name, e.ChangeType);

            // Cleanup the grammar
            grammarBuilderService.Cleanup(e.FullPath);
        }
    }
}
using TSQLToolkit.ANTLREngine.Contracts;
using TSQLToolkit.ANTLREngine.Models;
using TSQLToolkit.ANTLREngine.Services;

var builder = Host.CreateApplicationBuilder(args);

// Add services to the container.
builder.Services.Configure<GrammarSettings>(builder.Configuration.GetSection("GrammarSettings"));
builder.Services.Configure<WatcherSettings>(builder.Configuration.GetSection("WatcherSettings"));

builder.Services.AddHostedService<GrammarWatchWorker>();

builder.Services.AddTransient<IGrammarBuilderService, GrammarBuilderService>();

// Build the host.
var host = builder.Build();
host.Run();

using MediaService.Infrastructure.Extensions;
using MediaService.Workers;
using MediaService.Workers.Handlers;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// ============= LOGGING =============
builder.Services.AddSerilog(config => config
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// ============= INFRASTRUCTURE (DB, Storage, Image/Video processors) =============
builder.Services.AddInfrastructure(builder.Configuration);

// ============= WORKER HANDLERS =============
builder.Services.AddTransient<ImageProcessingHandler>();
builder.Services.AddTransient<MediaCleanupHandler>();

// ============= BACKGROUND SERVICES =============
builder.Services.AddHostedService<ImageProcessingWorker>();
builder.Services.AddHostedService<MediaCleanupWorker>();
builder.Services.AddHostedService<MediaUrlRefreshWorker>();

var host = builder.Build();
host.Run();

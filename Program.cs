using DotNetBungieAPI;
using DotNetBungieAPI.DefinitionProvider.Sqlite;
using DotNetBungieAPI.Extensions;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Models.Applications;
using DotNetBungieAPI.Models.Destiny;
using OpenTelemetry.Metrics;
using Quartz;
using Serilog;
using Serilog.Events;
using waterfall.Contexts;
using waterfall.Jobs;
using waterfall.Services;

namespace waterfall;

public static class Program
{
    private static IConfiguration? _configuration;

    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
#if DEBUG
            .MinimumLevel.Debug()
#endif
            .MinimumLevel.Override("Quartz", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog();

            _configuration = builder.Configuration;

            if (_configuration["Bungie:ApiKey"] == "api_key" ||
                _configuration["Bungie:ClientSecret"] == "client_secret" ||
                _configuration["Bungie:ManifestPath"] == "your_local_manifest_path")
            {
                Log.Fatal("Config not set up, aborting...");
                Environment.Exit(0);
            }

            EnsureDirectoryExists("Logs");
            EnsureDirectoryExists("Data");
            EnsureDirectoryExists(_configuration["Bungie:ManifestPath"]);

            Metrics.Initialize();
            DiscordWebhook.Initialize(_configuration.GetConnectionString("DiscordWebhook") ??
                                      throw new InvalidOperationException());

            builder.Services.AddOpenTelemetry()
                .WithMetrics(x => x.AddMeter("PGCRScraper")
                    .AddPrometheusExporter(y => y.ScrapeResponseCacheDurationMilliseconds = 0));

            builder.Services.AddDbContext<ActivityHistoryDb>(ServiceLifetime.Transient);
            builder.Services.AddDbContext<PlayerDb>(ServiceLifetime.Transient);

            builder.Services.UseBungieApiClient(bungieClient =>
                {
                    bungieClient.ClientConfiguration.ApiKey =
                        _configuration["Bungie:ApiKey"] ?? throw new InvalidOperationException();

                    bungieClient.ClientConfiguration.ApplicationScopes = ApplicationScopes.ReadUserData |
                                                                         ApplicationScopes.ReadBasicUserProfile;

                    bungieClient.ClientConfiguration.CacheDefinitions = true;
                    bungieClient.ClientConfiguration.ClientId = Convert.ToInt32(_configuration["Bungie:ClientId"]);
                    bungieClient.ClientConfiguration.ClientSecret = _configuration["Bungie:ClientSecret"] ??
                                                                    throw new InvalidOperationException();

                    bungieClient.ClientConfiguration.UsedLocales.Add(BungieLocales.EN);
                    bungieClient
                        .DefinitionProvider.UseSqliteDefinitionProvider(definitionProvider =>
                        {
                            definitionProvider.ManifestFolderPath = _configuration["Bungie:ManifestPath"] ??
                                                                    throw new InvalidOperationException();
                            definitionProvider.AutoUpdateManifestOnStartup = true;
                            definitionProvider.FetchLatestManifestOnInitialize = true;
                            definitionProvider.DeleteOldManifestDataAfterUpdates = false;
                        });
                    bungieClient.DotNetBungieApiHttpClient.ConfigureDefaultHttpClient(options =>
                        options.SetRateLimitSettings(190, TimeSpan.FromSeconds(10)));
                    bungieClient.DefinitionRepository.ConfigureDefaultRepository(x =>
                    {
                        var ignoreTypes = Enum.GetValues<DefinitionsEnum>()
                            .Where(y => y != DefinitionsEnum.DestinyActivityDefinition
                                        && y != DefinitionsEnum.DestinyActivityModeDefinition
                                        && y != DefinitionsEnum.DestinyActivityTypeDefinition
                                        && y != DefinitionsEnum.DestinyHistoricalStatsDefinition);

                        foreach (var defToIgnore in ignoreTypes)
                            x.IgnoreDefinitionType(defToIgnore);
                    });
                })
                .AddHostedService<BungieClientStartup>();

            builder.Services.Configure<QuartzOptions>(options => { options.SchedulerName = "QuartzTaskScheduler"; })
                .AddQuartz(q =>
                {
                    q.SchedulerId = "Core";
                    q.UseSimpleTypeLoader();
                    q.UseInMemoryStore();
                    q.UseDefaultThreadPool(tp => { tp.MaxConcurrency = 10; });

                    q.ScheduleJob<GetActivityHistory>(trigger => trigger
                        .WithIdentity("GetActivityHistoryTrigger")
                        .WithDailyTimeIntervalSchedule(
                            s => s.WithIntervalInHours(24)
                                .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(16, 50))
                                .InTimeZone(TimeZoneInfo.Utc)));

                    // q.ScheduleJob<GetPlayers>(trigger => trigger
                    //     .WithIdentity("GetPlayersTrigger")
                    // .WithDailyTimeIntervalSchedule(
                    //     s => s.WithIntervalInHours(24)
                    //         .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(17, 00))
                    //         .InTimeZone(TimeZoneInfo.Utc)));
                    // );

                    q.AddTrigger(t => t
                        .WithIdentity("GetActivityHistoryJob")
                        .ForJob(new JobKey("GetActivityHistoryTrigger"))
                        .StartAt(DateBuilder.EvenSecondDate(DateTimeOffset.UtcNow.AddSeconds(10))));

                    // q.AddTrigger(t => t
                    //     .WithIdentity("GetPlayersJob")
                    //     .ForJob(new JobKey("GetPlayersTrigger"))
                    //     .StartAt(DateBuilder.EvenSecondDate(DateTimeOffset.UtcNow.AddSeconds(10))));
                })
                .AddQuartzHostedService(options => { options.WaitForJobsToComplete = true; })
                .AddTransient<GetActivityHistory>()
                .AddTransient<GetAllPlayers>();

            var app = builder.Build();
            app.UseSerilogRequestLogging();
            app.UseOpenTelemetryPrometheusScrapingEndpoint();

            app.MapGet("/", () => "Hello World!");
            app.MapGet("/health", () => Results.Ok());

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void EnsureDirectoryExists(string? path)
    {
        if (Directory.Exists(path))
            return;
        if (path != null)
            Directory.CreateDirectory(path);
    }
}

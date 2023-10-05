using DotNetBungieAPI.Service.Abstractions;

namespace waterfall.Services;

public class BungieClientStartup(IBungieClient bungieClient,
    ILogger<BungieClientStartup> logger) : BackgroundService
{
    public static bool IsReady { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await bungieClient.DefinitionProvider.Initialize();
            await bungieClient.DefinitionProvider.ReadToRepository(bungieClient.Repository);
            IsReady = true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception in BungieClientStartupService");
        }
    }
}
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using DotNetBungieAPI.Extensions;
using DotNetBungieAPI.Service.Abstractions;
using Quartz;
using waterfall.DbContexts;
using waterfall.Services;

namespace waterfall.Jobs;

public class GetPlayers(ILogger<GetPlayers> logger,
    ActivityHistoryDb activityDb,
    PlayerDb playerDb,
    IBungieClient bungieClient) : IJob
{
    private const string JobName = "GetPlayers";

    [SuppressMessage("Reliability", "CA2016:Forward the 'CancellationToken' parameter to methods",
        Justification = "One PGCR failing to download causes them all to cancel.")]
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("Starting task {service}", JobName);

        if (!BungieClientStartup.IsReady)
        {
            logger.LogInformation("[{service}]: waiting for definitions...", JobName);

            while (!BungieClientStartup.IsReady)
                await Task.Delay(500);
        }

        try
        {
            var activityHistory = activityDb.Activities.ToList();

            var userDb = new ConcurrentBag<User>();
            lock (userDb)
            {
                foreach (var playerDbPlayer in playerDb.Players)
                    userDb.Add(playerDbPlayer);
            }

            var beforeCount = userDb.Count;

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = 16
            };

            await Parallel.ForEachAsync(activityHistory, options, async (activity, _) =>
            {
                // ReSharper disable once MethodSupportsCancellation
                var pgcr = await bungieClient.ApiAccess.Destiny2.GetPostGameCarnageReport(activity.InstanceId);

                var activityName =
                    pgcr.Response.ActivityDetails.ActivityReference.Select(x => x.DisplayProperties.Name);
                var activityTime = pgcr.Response.Period.ToString("g");

                logger.LogInformation("{service} processing {actName} from {actTime} ({actId})", JobName, activityName,
                    activityTime, activity.InstanceId);

                foreach (var pgcrEntry in pgcr.Response.Entries)
                {
                    if (userDb.Any(x => x.MembershipId == pgcrEntry.Player.DestinyUserInfo.MembershipId))
                        continue;

                    var player = new User
                    {
                        MembershipId = pgcrEntry.Player.DestinyUserInfo.MembershipId,
                        BungieName = pgcrEntry.Player.DestinyUserInfo.BungieGlobalDisplayName + "#" +
                                     pgcrEntry.Player.DestinyUserInfo.BungieGlobalDisplayNameCode
                    };

                    lock (userDb)
                    {
                        userDb.Add(player);
                    }

                    lock (Metrics.PgcrDownloaded)
                    {
                        Metrics.PgcrDownloaded.Add(1);
                    }
                }
            });

            var userList = userDb.ToList();

            var itemsToRemove = userList
                .GroupBy(x => x.MembershipId)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g.Skip(1));

            // needed because parallel threads can add duplicates
            foreach (var item in itemsToRemove)
                userList.Remove(item);

            playerDb.Players.UpdateRange(userList);
            await playerDb.SaveChangesAsync();

            logger.LogInformation("{service} added {count} players", JobName, userList.Count - beforeCount);
        }
        catch (Exception e)
        {
            if (!e.GetType().IsAssignableFrom(typeof(TaskCanceledException)))
                logger.LogError(e, "Exception in {service}", JobName);
        }

        logger.LogInformation("Finished task {service}", JobName);
    }
}
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using DotNetBungieAPI.Extensions;
using DotNetBungieAPI.Service.Abstractions;
using Quartz;
using waterfall.Contexts;
using waterfall.Contexts.Content;
using waterfall.Services;

namespace waterfall.Jobs;

public class GetPlayers(ILogger<GetPlayers> logger,
    ActivityHistoryDb activityDb,
    PlayerDb playerDb,
    IBungieClient bungieClient) : IJob
{
    private const string JobName = "GetPlayers";

    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("Starting task {service}", JobName);

        if (!BungieClientStartup.IsReady)
        {
            logger.LogInformation("[{service}]: waiting for definitions...", JobName);

            while (!BungieClientStartup.IsReady)
                await Task.Delay(500);
        }

        if (JobStatus.ActivityHistoryFetching)
        {
            logger.LogInformation("[{service}]: waiting for activity history...", JobName);

            while (JobStatus.ActivityHistoryFetching)
                await Task.Delay(500);
        }

        try
        {
            var activityHistory = activityDb.Activities
            // #if DEBUG
            //          .Take(500)
            // #endif
            .ToList();

            var userDb = new ConcurrentBag<Player>();
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
                var pgcr = await bungieClient.ApiAccess.Destiny2.GetPostGameCarnageReport(activity.InstanceId, CancellationToken.None);

                var activityName =
                    pgcr.Response.ActivityDetails.ActivityReference.Select(x => x.DisplayProperties.Name);
                var activityTime = pgcr.Response.Period.ToString("g");

                logger.LogInformation("[{service}] processing {actName} from {actTime} ({actId})", JobName, activityName,
                    activityTime, activity.InstanceId);

                foreach (var pgcrEntry in pgcr.Response.Entries)
                {
                    if (userDb.Any(x => x.MembershipId == pgcrEntry.Player.DestinyUserInfo.MembershipId))
                        continue;

                    var player = new Player
                    {
                        MembershipId = pgcrEntry.Player.DestinyUserInfo.MembershipId
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

            var toAddCount = userList.Count - beforeCount;
            logger.LogInformation("[{service}] {count} players to add", JobName, toAddCount);

            var playerDbPlayers = playerDb.Players.ToList();
            logger.LogInformation("[{service}] {count} players in db", JobName, playerDbPlayers.Count);

            await Task.Delay(1000);

            if (toAddCount == 0)
                return;

            var i = 0;
            foreach (var item in userList)
            {
                if (playerDbPlayers.Any(x => x.MembershipId == item.MembershipId))
                    continue;

                playerDb.Players.Add(item);
                i++;

                if (i == 250)
                {
                    await playerDb.SaveChangesAsync();
                    logger.LogInformation("[{service}] sent chunk of {count} to db", JobName, i);
                    i = 0;
                }
            }

            if (i > 0)
                await playerDb.SaveChangesAsync();

            logger.LogInformation("[{service}] added total of {count} players", JobName, toAddCount);
        }
        catch (Exception e)
        {
            if (!e.GetType().IsAssignableFrom(typeof(TaskCanceledException)))
                logger.LogError(e, "Exception in {service}", JobName);
        }

        logger.LogInformation("Finished task {service}", JobName);
    }
}

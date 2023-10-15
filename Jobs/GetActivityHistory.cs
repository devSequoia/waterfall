using System.Diagnostics;
using DotNetBungieAPI.Extensions;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Service.Abstractions;
using Quartz;
using waterfall.Contexts;
using waterfall.Constants;
using waterfall.Services;

namespace waterfall.Jobs;

public class GetActivityHistory(ILogger<GetActivityHistory> logger,
    ActivityHistoryDb activityDb,
    IBungieClient bungieClient) : IJob
{
    private const string JobName = "GetActivityHistory";

    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("Starting task {service}", JobName);
        JobStatus.ActivityHistoryFetching = true;

        if (!BungieClientStartup.IsReady)
        {
            logger.LogInformation("[{service}]: waiting for definitions...", JobName);

            while (!BungieClientStartup.IsReady)
                await Task.Delay(500);
        }

        var sw = Stopwatch.StartNew();

        foreach (var account in D2Accounts.GetAccountList())
        {
            var descriptor = account.Descriptor.ToString().PadLeft(4, '0');

            try
            {
                logger.LogInformation("[{service}]: fetching characters for #{id}...", JobName, descriptor);

                var characterRequest = await bungieClient.ApiAccess.Destiny2.GetHistoricalStatsForAccount(BungieMembershipType.TigerSteam, account.MembershipId);

                var characterId = characterRequest.Response.Characters.First().CharacterId;

                var currentPage = 0;

                while (true)
                {
                    var activityPage = await bungieClient.ApiAccess.Destiny2.GetActivityHistory(BungieMembershipType.TigerSteam,
                        account.MembershipId, characterId, 100, account.ModeType, currentPage);

                    if (activityPage.Response.Activities.Count == 0)
                    {
                        logger.LogInformation("[{service}]: finished activity fetching for {id}", JobName,
                            descriptor);
                        break;
                    }

                    var activityCount = 0;
                    foreach (var activity in activityPage.Response.Activities)
                    {
                        activityCount++;

                        if (activityDb.Activities.Any(x => x.InstanceId == activity.ActivityDetails.InstanceId && x.MembershipId == account.MembershipId))
                            continue;

                        var completed = activity.Values["completed"].BasicValue.DisplayValue == "Yes"
                                        && activity.Values["completionReason"].BasicValue.DisplayValue ==
                                        "Objective Completed";

                        var oldTime = activity.Period.ToUniversalTime();
                        var cleanTime = new DateTime(oldTime.Year, oldTime.Month, oldTime.Day, oldTime.Hour,
                            oldTime.Minute, oldTime.Second);

                        var newActivity = new Contexts.Content.Activity
                        {
                            MembershipId = account.MembershipId,
                            Time = cleanTime,
                            ActivityHash = activity.ActivityDetails.ActivityReference.Select(x => x.Hash),
                            InstanceId = activity.ActivityDetails.InstanceId,
                            IsCompleted = completed
                        };

                        await activityDb.Activities.AddAsync(newActivity);
                    }

                    logger.LogInformation("[{service}]: fetched {count} activities from page {page}", JobName,
                        activityCount, currentPage);
                    currentPage++;
                }

            }
            catch (Exception e)
            {
                if (!e.GetType().IsAssignableFrom(typeof(TaskCanceledException)))
                    logger.LogError(e, "Exception in {service}", JobName);
            }

            await activityDb.SaveChangesAsync();
        }

        sw.Stop();
        logger.LogInformation("[{service}]: finished in {time}", JobName, sw.Elapsed);
        JobStatus.ActivityHistoryFetching = false;
    }
}

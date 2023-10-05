using System.Diagnostics;
using DotNetBungieAPI.Extensions;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Models.Destiny.Definitions.ActivityModes;
using DotNetBungieAPI.Service.Abstractions;
using Quartz;
using waterfall.DbContexts;
using waterfall.Services;
using Activity = waterfall.DbContexts.Activity;

namespace waterfall.Jobs;

public class GetActivityHistory(ILogger<GetActivityHistory> logger,
    ActivityHistoryDb activityDb,
    IBungieClient bungieClient) : IJob
{
    private const string JobName = "GetActivityHistory";

    private const BungieMembershipType MembershipType = BungieMembershipType.TigerSteam;

    private readonly List<long> _membershipIds = new()
    {
        4611686018484346823, // platnootie
        4611686018474153927, // warokg
        4611686018471516071 // moons
    };

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
            var sw = Stopwatch.StartNew();

            foreach (var id in _membershipIds)
            {
                logger.LogInformation("[{service}]: fetching characters for {id}...", JobName, id);

                var characterRequest = await bungieClient.ApiAccess.Destiny2.GetHistoricalStatsForAccount(
                    MembershipType, id);

                var characterList = characterRequest.Response.Characters.Select(x => x.CharacterId).ToList();

                logger.LogInformation("[{service}]: found characters: {characters}", JobName,
                    string.Join(", ", characterList));

                foreach (var characterId in characterList)
                {
                    var currentPage = 0;

                    while (true)
                    {
                        var activityPage = await bungieClient.ApiAccess.Destiny2.GetActivityHistory(MembershipType,
                            id, characterId, 100, DestinyActivityModeType.Raid, currentPage);

                        if (activityPage.Response.Activities.Count == 0)
                        {
                            logger.LogInformation("[{service}]: finished activity fetching for {id}", JobName,
                                characterId);
                            break;
                        }

                        var activityCount = 0;
                        foreach (var activity in activityPage.Response.Activities)
                        {
                            activityCount++;

                            if (activityDb.Activities.Any(x => x.InstanceId == activity.ActivityDetails.InstanceId && x.MembershipId == id))
                                continue;

                            var completed = activity.Values["completed"].BasicValue.DisplayValue == "Yes"
                                            && activity.Values["completionReason"].BasicValue.DisplayValue ==
                                            "Objective Completed";

                            var oldTime = activity.Period.ToUniversalTime();
                            var cleanTime = new DateTime(oldTime.Year, oldTime.Month, oldTime.Day, oldTime.Hour,
                                oldTime.Minute, oldTime.Second);

                            var newActivity = new Activity
                            {
                                MembershipId = id,
                                ActivityHash = activity.ActivityDetails.ActivityReference.Select(x => x.Hash),
                                InstanceId = activity.ActivityDetails.InstanceId,
                                IsCompleted = completed,
                                Time = cleanTime
                            };

                            await activityDb.Activities.AddAsync(newActivity);
                        }

                        logger.LogInformation("[{service}]: fetched {count} activities from page {page}", JobName,
                            activityCount, currentPage);
                        currentPage++;
                    }
                }
            }

            await activityDb.SaveChangesAsync();

            sw.Stop();
            logger.LogInformation("[{service}]: finished in {time}", JobName, sw.Elapsed);
        }
        catch (Exception e)
        {
            if (!e.GetType().IsAssignableFrom(typeof(TaskCanceledException)))
                logger.LogError(e, "Exception in {service}", JobName);
        }

        logger.LogInformation("Finished task {service}", JobName);
    }
}
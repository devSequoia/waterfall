using System.Text;
using Discord;
using DotNetBungieAPI.Extensions;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Models.Destiny;
using DotNetBungieAPI.Models.Destiny.Definitions.InventoryItems;
using DotNetBungieAPI.Models.GroupsV2;
using DotNetBungieAPI.Service.Abstractions;
using waterfall.Constants;
using waterfall.Contexts;
using waterfall.Contexts.Content;
using waterfall.Services;

namespace waterfall.Jobs;

public class ProcessActivity(ILogger<GetActivityHistory> logger, IBungieClient bungieClient, PlayerDb playerDb)
{
    private const string JobName = "ProcessActivity";

    private const int KillsThreshold = 10;
    private const int DeathsThreshold = 10;

    public async Task ProcessActivityData(Activity activity, short botDescriptor)
    {
        // await DiscordWebhook.SendMessage($"New activity: {activity.InstanceId}");

        var pgcr = await bungieClient.ApiAccess.Destiny2.GetPostGameCarnageReport(activity.InstanceId,
            CancellationToken.None);

        var activityName =
            pgcr.Response.ActivityDetails.ActivityReference.Select(x => x.DisplayProperties.Name);
        var activityTime = pgcr.Response.Period.ToString("g");
        var activityType = pgcr.Response.ActivityDetails.Mode.ToString().ToLower();
        var activityUrl = $"https://{activityType}.report/pgcr/{activity.InstanceId}";

        logger.LogInformation("[{service}] processing {actName} from {actTime} ({actId})", JobName, activityName,
            activityTime, activity.InstanceId);

        var embedBuilder = new EmbedBuilder()
            .WithTitle("New PGCR found")
            .WithColor(Color.Blue)
            .WithUrl(activityUrl)
            .WithTimestamp(pgcr.Response.Period)
            .WithFooter("Waterfall");

        embedBuilder.AddField("Account", botDescriptor, true);
        embedBuilder.AddField("Activity", activityName, true);

        var offendingUsers = new List<string>();
        var usersToIgnore = D2Accounts.GetAccountList();

        foreach (var player in pgcr.Response.Entries)
        {
            if (usersToIgnore.Any(x => x.MembershipId == player.Player.DestinyUserInfo.MembershipId))
                continue;

            var playerFromDb =
                playerDb.Players.FirstOrDefault(x => x.MembershipId == player.Player.DestinyUserInfo.MembershipId);
            if (playerFromDb == null)
            {
                playerFromDb = new Player
                {
                    MembershipId = player.Player.DestinyUserInfo.MembershipId
                };
                playerDb.Add(playerFromDb);

                logger.LogInformation("[{service}] added new player {playerId}", JobName, playerFromDb.MembershipId);

                await playerDb.SaveChangesAsync();
            }

            var killsOver = player.Values["kills"].BasicValue.Value >= KillsThreshold;
            var deathsOver = player.Values["deaths"].BasicValue.Value >= DeathsThreshold;
            if (!killsOver && !deathsOver)
                continue;

            var nameTask =
                await bungieClient.ApiAccess.User.GetMembershipDataById(playerFromDb.MembershipId,
                    BungieMembershipType.All);
            var primaryMembership = nameTask.Response.GetDestinyPrimaryMembership();
            var bungieName = primaryMembership.BungieGlobalDisplayName + "#" +
                             primaryMembership.BungieGlobalDisplayNameCode?.ToString().PadLeft(4, '0');

            var userSb = new StringBuilder();
            userSb.Append($"[{bungieName}](https://b.moons.bio/{playerFromDb.MembershipId}) ");
            userSb.Append($"(**K**: {player.Values["kills"].BasicValue.Value}, ");
            userSb.Append($"**D**: {player.Values["deaths"].BasicValue.Value}, ");
            userSb.Append($"**F**: {player.Values["completed"].BasicValue.DisplayValue})\n");

            var clanTask = await bungieClient.ApiAccess.GroupV2.GetGroupsForMember(
                player.Player.DestinyUserInfo.MembershipType, player.Player.DestinyUserInfo.MembershipId,
                GroupsForMemberFilter.All, GroupType.Clan);
            var clan = clanTask.Response.Results.FirstOrDefault();
            if (clan != null)
                userSb.AppendLine(
                    $"> **C**: [{clan.Group.Name}](https://www.bungie.net/7/en/Clan/Profile/{clan.Group.GroupId}) [{clan.Group.ClanInfo.ClanCallSign}]");

            var mostUsedWeapon = player.ExtendedData.Weapons
                .OrderByDescending(x => x.Values["uniqueWeaponKills"].BasicValue.Value).FirstOrDefault();
            if (mostUsedWeapon != null)
            {
                var hash = mostUsedWeapon.ItemReference.Hash ?? 0;
                var weapon =
                    await bungieClient.ApiAccess.Destiny2.GetDestinyEntityDefinition<DestinyInventoryItemDefinition>(
                        DefinitionsEnum.DestinyInventoryItemDefinition, hash);
                userSb.AppendLine($"> **W**: {weapon.Response.DisplayProperties.Name}");
            }

            offendingUsers.Add(userSb.ToString());
        }

        if (offendingUsers.Count == 0)
            embedBuilder.Description = Format.Bold("No offending users found");
        else
            embedBuilder.Description = Format.Bold("Offending Users") + "\n" + string.Join("\n", offendingUsers);

        await DiscordWebhook.SendMessage("", embedBuilder.Build());
    }
}

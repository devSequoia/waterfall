using DotNetBungieAPI.Models.Destiny.Definitions.ActivityModes;

namespace waterfall.Objects;

public class D2Accounts
{
    public class Account
    {
        public long MembershipId { get; set; }
        public short Descriptor { get; set; }
        public DestinyActivityModeType ModeType { get; set; }
        public bool IsSubscriberBot { get; set; }
    }

    public static List<Account> GetAccountList()
    {
        return [
            new() {
                MembershipId = 4611686018526359012,
                Descriptor = 4108,
                ModeType = DestinyActivityModeType.Raid,
                IsSubscriberBot = false
            },
            new() {
                MembershipId = 4611686018526359041,
                Descriptor = 3036,
                ModeType = DestinyActivityModeType.Raid,
                IsSubscriberBot = false
            },
            new() {
                MembershipId = 4611686018526359175,
                Descriptor = 0422,
                ModeType = DestinyActivityModeType.Raid,
                IsSubscriberBot = false
            },
            new() {
                MembershipId = 4611686018526359234,
                Descriptor = 1883,
                ModeType = DestinyActivityModeType.Raid,
                IsSubscriberBot = false
            },
            new() {
                MembershipId = 4611686018526359277,
                Descriptor = 8880,
                ModeType = DestinyActivityModeType.Dungeon,
                IsSubscriberBot = false
            },
            new() {
                MembershipId = 4611686018527159700,
                Descriptor = 2845,
                ModeType = DestinyActivityModeType.Raid,
                IsSubscriberBot = false
            },
            new() {
                MembershipId = 4611686018527194251,
                Descriptor = 0753,
                ModeType = DestinyActivityModeType.Dungeon,
                IsSubscriberBot = false
            },
            new() {
                MembershipId = 4611686018527194296,
                Descriptor = 3587,
                ModeType = DestinyActivityModeType.Dungeon,
                IsSubscriberBot = false
            },
            new() {
                MembershipId = 4611686018527194350,
                Descriptor = 8653,
                ModeType = DestinyActivityModeType.Dungeon,
                IsSubscriberBot = false
            },
            new() {
                MembershipId = 4611686018527944111,
                Descriptor = 6113,
                ModeType = DestinyActivityModeType.Raid,
                IsSubscriberBot = false
            },
            new() {
                MembershipId = 4611686018530596538,
                Descriptor = 0387,
                ModeType = DestinyActivityModeType.Raid,
                IsSubscriberBot = false
            },
            new() {
                MembershipId = 4611686018528570054,
                Descriptor = 9119,
                ModeType = DestinyActivityModeType.Raid,
                IsSubscriberBot = true
            },
            new() {
                MembershipId = 4611686018528570519,
                Descriptor = 0668,
                ModeType = DestinyActivityModeType.Raid,
                IsSubscriberBot = true
            },
            new() {
                MembershipId = 4611686018528570779,
                Descriptor = 4104,
                ModeType = DestinyActivityModeType.Dungeon,
                IsSubscriberBot = true
            },
            new() {
                MembershipId = 4611686018528571004,
                Descriptor = 1446,
                ModeType = DestinyActivityModeType.Raid,
                IsSubscriberBot = true
            },
            new() {
                MembershipId = 4611686018530002386,
                Descriptor = 4524,
                ModeType = DestinyActivityModeType.Dungeon,
                IsSubscriberBot = true
            },
            new() {
                MembershipId = 4611686018530004593,
                Descriptor = 6393,
                ModeType = DestinyActivityModeType.Dungeon,
                IsSubscriberBot = true
            },
            new() {
                MembershipId = 4611686018530583525,
                Descriptor = 3246,
                ModeType = DestinyActivityModeType.Raid,
                IsSubscriberBot = true
            },
            new() {
                MembershipId = 4611686018530583534,
                Descriptor = 0199,
                ModeType = DestinyActivityModeType.Raid,
                IsSubscriberBot = true
            },
            new() {
                MembershipId = 4611686018530596518,
                Descriptor = 8537,
                ModeType = DestinyActivityModeType.Raid,
                IsSubscriberBot = true
            },
            new() {
                MembershipId = 4611686018530596612,
                Descriptor = 9484,
                ModeType = DestinyActivityModeType.Raid,
                IsSubscriberBot = true
            },
            new() {
                MembershipId = 4611686018530794041,
                Descriptor = 7761,
                ModeType = DestinyActivityModeType.Dungeon,
                IsSubscriberBot = true
            }
        ];
    }
}

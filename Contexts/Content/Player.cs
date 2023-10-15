namespace waterfall.Contexts.Content;

public partial class Player
{
    public long MembershipId { get; set; }
    public bool IsBanned { get; set; }
    public string? BanReason { get; set; }
    public TimeOnly? BanTime { get; set; }
}

namespace waterfall.Contexts.Content;

public class Activity
{
    public int Id { get; set; }
    public long MembershipId { get; set; }
    public DateTime Time { get; set; }
    public long ActivityHash { get; set; }
    public long InstanceId { get; set; }
    public bool IsCompleted { get; set; }
}


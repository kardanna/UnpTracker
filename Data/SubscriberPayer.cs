namespace UnpTracker.Data;

public class SubscriberPayer
{
    public int Id { get; set; }
    public int SubscriberId { get; set; }
    public int PayerId { get; set; }
    public Payer Payer { get; set; } = null!;
    public Subscriber Subscriber { get; set; } = null!;
}
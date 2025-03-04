namespace UnpTracker.Data;

public class Subscriber
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public ICollection<SubscriberPayer> SubscriberPayers { get; set; } = new List<SubscriberPayer>();
}
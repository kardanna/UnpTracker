namespace UnpTracker.Data;

public class Payer
{
    public int Id { get; set; }
    public string Unp { get; set; } = null!;
    public bool IsInLocalDb { get; set; }
    public bool IsInStateDb { get; set; }
    public LocalPayer? LocalPayer { get; set; }
    public ICollection<SubscriberPayer> SubscriberPayers { get; set; } = new List<SubscriberPayer>(); 
}
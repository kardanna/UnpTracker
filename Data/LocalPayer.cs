namespace UnpTracker.Data;

public class LocalPayer
{
    public int Id { get; set; }
    public int PayerId { get; set; }
    public Payer Payer { get; set; } = null!;
}
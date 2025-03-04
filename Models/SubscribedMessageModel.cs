namespace UnpTracker.Models;

public class SubscribedMessageModel
{
    public string Email { get; set; }
    public List<string> Unps { get; set; }
    public int TotalSubscriptions { get; set; }
}

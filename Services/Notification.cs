using System.Text;

namespace UnpTracker.Models;

public class Notification
{
    public string Unp { get; init; }
    public Notification(string unp)
    {
        Unp = unp;
    }
    private string localDbState = string.Empty;
    private string stateDbState = string.Empty;

    public void LocalDbStateChanged(bool newState)
    {
        localDbState = newState ? "есть в локальной базе" : "отсутствует в локальной базе";
    }
    public void StateDbStateChanged(bool newState)
    {
        stateDbState = newState ? "есть в государственной базе" : "отсутствует в государственной базе";
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append($"Теперь УНП {Unp} ");
        if (localDbState != string.Empty)
        {
            sb.Append(localDbState);
            if (stateDbState != string.Empty)
            {
                sb.Append(" и ");
            }
        }
        if (stateDbState != string.Empty)
        {
            sb.Append(stateDbState);
        }
        return sb.ToString();
    }

}
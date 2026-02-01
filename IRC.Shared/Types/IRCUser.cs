namespace IRC.Shared.Types;

public class IRCUser
{
    public string GUID { get; } = string.Empty;
    public string Nickname { get; set; }
    public string CurrentChannel { get; set; }
    public ISendable Socket { get; }

    public IRCUser(string guid, ISendable socket)
    {
        GUID = guid;
        Socket = socket;
        Nickname = $"Guest_{guid.Substring(0, 4)}";
        CurrentChannel = "default";
    }
}
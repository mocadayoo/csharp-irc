namespace IRC.Shared.Types;

public interface ISendable
{
    void Send(string message, byte opcode = Opcode.Text);
}
public enum IRCMessageTypes
{
    HEARTBEAT,
    PONG,
    Chat,
    Notify,
    Command,
    CommandResponse
}
public class IRCResponseJson
{
    public IRCMessageTypes Type { get; set; }
    public string? Message { get; set; } = string.Empty;
    public string? Channel { get; set; } = "default";
    public string? Special  { get; set; } = string.Empty;
}
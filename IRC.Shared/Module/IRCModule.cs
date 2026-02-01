using System.Text.Json;
using IRC.Shared.Types;

namespace IRC.Shared.Modules;

// TODO: ratelimitなど追加
public class IRCModule
{
    public static string JsonToMessage(IRCResponseJson json)
    {
        return JsonSerializer.Serialize(json);
    }

    public static IRCResponseJson? MessageToJson(string message)
    {
        try
        {
            return JsonSerializer.Deserialize<IRCResponseJson>(message, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static bool IRCHeartBeat(ISendable manager, IRCResponseJson json)
    {
        if (json.Type == IRCMessageTypes.HEARTBEAT)
        {
            IRCResponseJson respJson = new IRCResponseJson
            {
                Type = IRCMessageTypes.PONG,
                Message = null,
                Channel = "",
                Special = null
            };
            string msg = JsonSerializer.Serialize(respJson);
            manager.Send(msg);
            return true;
        }
        return false;
    }
}
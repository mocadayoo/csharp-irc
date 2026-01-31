using System.Net.WebSockets;
using System.Text;
using IRC.Shared.Types;
using IRC.Shared.Modules;
using System.ComponentModel;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.InputEncoding = System.Text.Encoding.UTF8;

Uri ServerUri = new("ws://localhost:8080");
ClientWebSocket Client = new ClientWebSocket();

async Task SendAsync(ClientWebSocket client, string message)
{
    byte[] sendBuf = Encoding.UTF8.GetBytes(message);
    await client.SendAsync(new ArraySegment<byte>(sendBuf), WebSocketMessageType.Text, true, CancellationToken.None);
}

async Task ReceiveLoop(ClientWebSocket client)
{
    byte[] buffer = new byte[1024 * 4];
    try
    {
        _ = HeartBeat(Client, TimeSpan.FromSeconds(5));
        while (client.State == WebSocketState.Open)
        {
            var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
            else
            {
                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var json = IRCModule.MessageToJson(message);

                if (json != null && json.Type == IRCMessageTypes.Chat) {
                    Console.WriteLine(json.Message);
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"recive error: {ex.Message}");
    }
}

async Task HeartBeat(ClientWebSocket ws, TimeSpan timeSpan)
{
    while (true)
    {
        var json = IRCModule.JsonToMessage(new IRCResponseJson
        {
            Type = IRCMessageTypes.HEARTBEAT,
            Message = "",
            Channel = "",
            Special = ""
        });
        await SendAsync(ws, json);
        await Task.Delay(timeSpan);
    }
}

try
{
    async Task Main()
    {
        await Client.ConnectAsync(ServerUri, CancellationToken.None);

        _ = ReceiveLoop(Client);

        while (true)
        {
            Console.Write("メッセージを入力: ");
            string input = Console.ReadLine() ?? "";
            
            var jsonMessage = IRCModule.JsonToMessage(new IRCResponseJson
            {
                Type = IRCMessageTypes.Chat,
                Message = input,
                Channel = null,
                Special = null
            });
            await SendAsync(Client, jsonMessage);
        }
    }

    await Main();
}
catch
{
    return;
}
using System.Net.WebSockets;
using System.Text;
using IRC.Shared.Types;
using IRC.Shared.Modules;

string currentInput = "";
Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.InputEncoding = System.Text.Encoding.UTF8;

Uri ServerUri = new("ws://localhost:8080");
ClientWebSocket Client = new ClientWebSocket();

async Task SendAsync(ClientWebSocket client, string message)
{
    byte[] sendBuf = Encoding.UTF8.GetBytes(message);
    await client.SendAsync(new ArraySegment<byte>(sendBuf), WebSocketMessageType.Text, true, CancellationToken.None);
}

void WriteConsole(string message)
{
    int currentLineCursor = Console.CursorLeft;
    int currentTop = Console.CursorTop;

    Console.SetCursorPosition(0, currentTop);
    Console.Write(new string(' ', Console.WindowWidth - 1));
    Console.SetCursorPosition(0, currentTop);

    Console.WriteLine(message);
    Console.Write("メッセージを入力: "+ currentInput);

    Console.SetCursorPosition(currentLineCursor, Console.CursorTop);
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

                if (json != null && (json.Type == IRCMessageTypes.Chat || json.Type == IRCMessageTypes.Notify))
                {
                    WriteConsole(json.Message);
                }

                if (json.Type == IRCMessageTypes.CommandResponse)
                {
                    WriteConsole(json.Message);
                    WriteConsole(json.Special);
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

string BuildSendJson(string input)
{
    if (input[0] == '/')
    {
        string[] commandArgs = input.Substring(1).Split(' ');
        string command = commandArgs[0].ToLower();

        string jsonMessage = "";

        switch (command)
        {
            case "switch":
                string channelName = commandArgs[0];

                jsonMessage = IRCModule.JsonToMessage(new IRCResponseJson
                {
                    Type = IRCMessageTypes.Command,
                    Message = "switch",
                    Channel = channelName,
                    Special = null
                });
                break;
            case "latency":
                break;
        }

        return jsonMessage;
    }
    return IRCModule.JsonToMessage(new IRCResponseJson
    {
        Type = IRCMessageTypes.Chat,
        Message = input,
        Channel = null,
        Special = null
    });
}

try
{
    await Client.ConnectAsync(ServerUri, CancellationToken.None);
    _ = ReceiveLoop(Client);

    Console.Write("メッセージを入力: ");

    while (Client.State == WebSocketState.Open)
    {
        if (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter)
            {
                if (!string.IsNullOrEmpty(currentInput))
                {
                    string toSend = currentInput;

                    Console.WriteLine();
                    currentInput = "";
                    Console.Write("メッセージを入力: ");

                    var jsonMessage = BuildSendJson(toSend);
                    await SendAsync(Client, jsonMessage);
                }
            }
            else if (key.Key == ConsoleKey.Backspace)
            {
                if (currentInput.Length > 0)
                {
                    currentInput = currentInput.Remove(currentInput.Length - 1);
                    Console.Write("\b \b");
                }
            }
            else if (!char.IsControl(key.KeyChar))
            {
                currentInput += key.KeyChar;
                Console.Write(key.KeyChar);
            }
        }
        await Task.Delay(10);
    }
} catch
{
    return;
}
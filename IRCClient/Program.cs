using System.Net.WebSockets;
using System.Text;

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
                Console.WriteLine($"[server]: {message}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"recive error: {ex.Message}");
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
            if (string.IsNullOrEmpty(input)) break;

            await SendAsync(Client, input);
        }
    }

    await Main();
}
catch
{
    return;
}
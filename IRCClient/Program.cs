using System.Collections.Specialized;
using System.Data.SqlTypes;
using System.Linq.Expressions;
using System.Net.WebSockets;
using System.Text;

Uri ServerUri = new("ws://localhost:8080");
ClientWebSocket Client = new ClientWebSocket();

async Task SendAsync(ClientWebSocket client, string message)
{
    byte[] sendBuf = Encoding.UTF8.GetBytes(message);
    await client.SendAsync(new ArraySegment<byte>(sendBuf), WebSocketMessageType.Text, true, CancellationToken.None);
}

try
{
    async Task Main()
    {
        await Client.ConnectAsync(ServerUri, CancellationToken.None);
        
        await SendAsync(Client, (string)"こんにちは");
    }

    await Main();
}
catch
{
    return;
}
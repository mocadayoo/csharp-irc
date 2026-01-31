using System.Net.WebSockets;

try
{
    Uri ServerUri = new("ws://localhost:8080");
    ClientWebSocket Client = new ClientWebSocket();
    async Task Main()
    {
        await Client.ConnectAsync(ServerUri, CancellationToken.None);
        var buffer = new byte[1024];
    }

    await Main();
}
catch
{
    return;
}
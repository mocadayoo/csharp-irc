namespace IRCServer.Utils;

public static class ClientManager
{
    private static readonly List<SocketManager> _clients = new();

    public static void Add(SocketManager client)
    {
        lock (_clients)
        {
            _clients.Add(client);
        }
    }

    public static void Remove(SocketManager client)
    {
        lock (_clients)
        {
            _clients.Remove(client);
        }
    }

    public static void Broadcast(string message)
    {
        foreach (var client in _clients)
        {
            _ = Task.Run(() =>
            {
                try
                {
                    client.Send(message);
                }
                catch
                {
                    return;
                }
            });
        }
    }

    public static int ClientCount()
    {
        lock (_clients)
        {
            return _clients.Count;
        }
    }
}
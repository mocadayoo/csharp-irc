namespace IRCServer.Utils;

// TODO: クライアントごとにnameを付けたりして idによる管理とか join leaveのnoticeを行う
public static class ClientManager
{
    private static readonly Dictionary<string, SocketManager> _clients = [];

    public static void Add(SocketManager client)
    {
        lock (_clients)
        {
            _clients[client.GUID] = client;
        }
    }

    public static void Remove(SocketManager client)
    {
        lock (_clients)
        {
            _clients.Remove(client.GUID);
        }
    }

    public static void Broadcast(string message, SocketManager sender)
    {
        List<SocketManager> currentClients;
        lock (_clients)
        {
            currentClients = _clients.Values.ToList();
        }

        currentClients.Remove(sender);
        foreach (var client in currentClients)
        {
            _ = Task.Run(() =>
            {
                if (client.currentChannel == sender.currentChannel)
                {
                    try
                    {
                        client.Send(message);
                    }
                    catch
                    {
                        return;
                    }
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
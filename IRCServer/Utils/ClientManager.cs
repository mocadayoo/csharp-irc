using IRC.Shared.Types;
using IRC.Shared.Modules;

namespace IRCServer.Utils;

public static class ClientManager
{
    private static readonly Dictionary<string, IRCUser> _clients = [];

    public static IRCUser Add(ISendable socket)
    {
        if (string.IsNullOrEmpty(socket.GUID))
        {
            socket.GUID = Guid.NewGuid().ToString();
        }

        var newUser = new IRCUser(socket.GUID, socket);

        lock (_clients)
        {
            _clients[newUser.GUID] = newUser;
        }
        return newUser;
    }

    public static void Remove(ISendable client)
    {
        lock (_clients)
        {
            _clients.Remove(client.GUID);
        }
    }

    public static IRCUser? GetUser(string guid)
    {
        lock (_clients)
        {
            return _clients.GetValueOrDefault(guid);
        }
    }

    public static void Broadcast(string message, IRCUser sender)
    {
        IRCUser[] allUsers;
        lock (_clients)
        {
            allUsers = _clients.Values.ToArray();
        }

        var sendClients = allUsers.Where(u =>
            u.GUID != sender.GUID &&
            u.CurrentChannel == sender.CurrentChannel
        ); // filter 自分以外 && 自分のチャンネルと同じチャンネルのユーザー

        foreach (var client in sendClients)
        {
            _ = Task.Run(() =>
            {
                try
                {
                    client.Socket.Send(message);
                }
                catch { } // 無視
            });
        }
    }

    public static int ClientCount()
    {
        lock (_clients) return _clients.Count;
    }
}
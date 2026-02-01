using System.Net;
using System.Net.Sockets;
using IRCServer.Utils;
using IRC.Shared.Types;
using IRC.Shared.Modules;
Console.WriteLine("[info] startup..");

TcpListener wsApp = new(IPAddress.Parse("127.0.0.1"), 8080); // localの8080ポートで作成

wsApp.Start();
while (true)
{
    TcpClient client = wsApp.AcceptTcpClient();
    Console.WriteLine("[info] client connected");

    NetworkStream stream = client.GetStream();

    // true なおかつ二個目がstringの場合
    if (SocketUtils.HandShake.IsWant(stream) is (true, string header))
    {
        SocketUtils.HandShake.DoHandShake(stream, header);
        Console.WriteLine("[info] HandShaked!");

        _ = Task.Run(() =>
        {
            SocketManager sockManager = new(stream, 30 * 1000);
            IRCUser user = ClientManager.Add(sockManager);
            ClientManager.Broadcast(IRCModule.JsonToMessage(new IRCResponseJson
            {
                Type = IRCMessageTypes.Notify,
                Message = $"[{user.CurrentChannel}] --> {user.Nickname} has joiend",
                Channel = user.CurrentChannel,
                Special = null
            }), user);

            sockManager.OnMessage = (manager, message, opcode, payload) =>
            {
                var userData = ClientManager.GetUser(manager.GUID);
                if (userData == null)
                {
                    Console.WriteLine($"{manager.GUID} user are not in ClientManager List");
                    return;
                }
                if (string.IsNullOrEmpty(message)) return;

                IRCResponseJson? json = IRCModule.MessageToJson(message);
                if (json == null) return;

                if (IRCModule.IRCHeartBeat(manager, json)) return;

                if (json.Type == IRCMessageTypes.Chat && json.Message != null && json.Message != "")
                {
                    ClientManager.Broadcast(IRCModule.JsonToMessage(new IRCResponseJson
                    {
                        Type = IRCMessageTypes.Chat,
                        Message = $"[{userData.CurrentChannel}] <{userData.Nickname}> {json.Message}",
                        Channel = userData.CurrentChannel,
                        Special = null
                    }), userData);
                }

                if (json.Type == IRCMessageTypes.Command)
                {
                    HandleCommand(json, userData);
                }
            };
            sockManager.OnClose = (manager) =>
            {
                ClientManager.Broadcast(IRCModule.JsonToMessage(new IRCResponseJson
                {
                    Type = IRCMessageTypes.Notify,
                    Message = $"[{user.CurrentChannel}] <-- {user.Nickname} has left",
                    Channel = user.CurrentChannel,
                    Special = null
                }), user);
                Console.WriteLine("[info] client closed connection");
                ClientManager.Remove(manager);
            };
            sockManager.Listen();
        });
    }
}

static void HandleCommand(IRCResponseJson json, IRCUser user)
{
    switch (json.Special)
    {
        case "switch":
            {
                if (json.Channel == null || json.Channel == "") break;
                user.CurrentChannel = json.Channel;

                var Response = IRCModule.JsonToMessage(new IRCResponseJson
                {
                    Type = IRCMessageTypes.CommandResponse,
                    Message = "switch",
                    Channel = user.CurrentChannel,
                    Special = "success",
                });
                user.Socket.Send(Response);
                break;
            }
        case "nick":
            {
                if (json.Message == "") break;

                var Response = "";
                if (json.Message.Length >= 32)
                {
                    Response = IRCModule.JsonToMessage(new IRCResponseJson
                    {
                        Type = IRCMessageTypes.CommandResponse,
                        Message = "nick",
                        Channel = user.CurrentChannel,
                        Special = "error",
                    });
                }
                else
                {
                    user.Nickname = json.Message ?? user.Nickname;
                    Response = IRCModule.JsonToMessage(new IRCResponseJson
                    {
                        Type = IRCMessageTypes.CommandResponse,
                        Message = "nick",
                        Channel = user.CurrentChannel,
                        Special = "success",
                    });
                }
                user.Socket.Send(Response);
                break;
            }
    }
}
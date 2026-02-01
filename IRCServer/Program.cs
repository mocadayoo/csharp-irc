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
            sockManager.OnMessage = (manager, message, opcode, payload) =>
            {
                if (string.IsNullOrEmpty(message)) return;

                IRCResponseJson? json = IRCModule.MessageToJson(message);
                if (json == null) return;

                if (IRCModule.IRCHeartBeat(manager, json)) return;

                if (json.Type == IRCMessageTypes.Chat && json.Message != null && json.Message != "")
                {
                    Console.WriteLine(json.Message);
                    ClientManager.Broadcast(IRCModule.JsonToMessage(new IRCResponseJson
                    {
                        Type = IRCMessageTypes.Chat,
                        Message = json.Message,
                        Channel = null,
                        Special = null
                    }), sockManager);
                }

                if (json.Type == IRCMessageTypes.Command)
                {
                    HandleCommand(json, sockManager);
                }
            };
            sockManager.OnClose = (manager) =>
            {
                Console.WriteLine("[info] client closed connection");
            };
            sockManager.Listen();
        });
    }
}

static void HandleCommand(IRCResponseJson json, SocketManager manager)
{
    switch (json.Message)
    {
        case "switch":
            if (json.Channel == null || json.Channel == "") break;
            manager.currentChannel = json.Channel;

            var Response = IRCModule.JsonToMessage(new IRCResponseJson{
                Type = IRCMessageTypes.CommandResponse,
                Message = $"now your channel in {json.Channel}",
                Channel = null,
                Special = "success",
            });
            manager.Send(Response);
            break;
    }
}
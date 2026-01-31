using System.Net;
using System.Net.Sockets;
using IRCServer.Utils;
using IRC.Shared.Types;
using IRC.Shared.Modules;
Console.WriteLine("[info] startup..");

TcpListener wsApp = new(IPAddress.Parse("127.0.0.1"), 8080); // localの8080ポートで作成

// TODO: IRCなので後はbroadcastを追加する必要がある。
// TODO: 上の追加が終わったらcommandなど特殊系の追加をしたい。
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

                if (json.Type == IRCMessageTypes.Chat)
                {
                    Console.WriteLine(json.Message);
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
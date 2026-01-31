using System.Net;
using System.Net.Sockets;
using IRCServer.Utils;

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

        _ = Task.Run(() => new SocketManager(stream));
    }
}
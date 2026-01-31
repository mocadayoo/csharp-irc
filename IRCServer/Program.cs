using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

void Main()
{
    Console.WriteLine("startup..");

    TcpListener wsApp = new TcpListener(IPAddress.Parse("127.0.0.1"), 8080); // localの8080ポートで作成

    wsApp.Start();
    while (true)
    {
        TcpClient client = wsApp.AcceptTcpClient();
        Console.WriteLine("client connected");

        Task.Run(() => HandlerClient(client));
    }
}

void HandlerClient(TcpClient client)
{
    NetworkStream stream = client.GetStream();

    byte[] bytes = new byte[1024];
    int count = stream.Read(bytes, 0, bytes.Length);
    if (count == 0) return;
    string s = Encoding.UTF8.GetString(bytes);

    if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))
    {
        Console.WriteLine("HandShake recive");
        // clientがランダムなキーをSec-WebSocket-Keyとしてheaderに入れてくる
        // それをGUID (RFC6455) で決められている "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" とくっつける
        // それをsha-1でhashそしてbase64してからSec-WebSocket-Acceptに乗せて返す
        string cswk = Regex.Match(s, "Sec-WebSocket-Key:(.*)").Groups[1].Value.Trim();
        cswk += "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        // sha1でハッシュ
        byte[] cswkHash = SHA1.HashData(Encoding.UTF8.GetBytes(cswk));
        string cswkHashBase64 = Convert.ToBase64String(cswkHash);

        byte[] response = Encoding.UTF8.GetBytes(
            "HTTP/1.1 101 Switching Protocols\r\n" +
            "Upgrade: websocket\r\n" +
            "Connection: Upgrade\r\n" +
            "Sec-WebSocket-Accept: " + cswkHashBase64 + "\r\n\r\n"
        );

        stream.Write(response, 0, response.Length);
    }
}

Main();
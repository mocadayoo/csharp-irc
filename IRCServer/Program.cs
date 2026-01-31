using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

void Main()
{
    Console.WriteLine("[info] startup..");

    TcpListener wsApp = new TcpListener(IPAddress.Parse("127.0.0.1"), 8080); // localの8080ポートで作成

    wsApp.Start();
    while (true)
    {
        TcpClient client = wsApp.AcceptTcpClient();
        Console.WriteLine("[info] client connected");

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
        Console.WriteLine("[info] HandShake recive");
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

    /*
        bufferなので出す順番を間違えると崩壊する (当たり前)
        長さによって 2^n のbyteにbufを広げないといけない
        その長さはpayloadのlengthがheaderにあるので読む
    */
    while (true)
    {
        // [0]にはFINフラグ、種別が入っている
        // [1] にはmaskのflagとdataのlength
        byte[] head = new byte[2];
        stream.ReadExactly(head);

        bool isMask = (head[1] & 0x80) != 0; // 一番左の1bitを読む
        long payloadLength = head[1] & 0x7F; // mask flag以外の右側の7bitを読む

        byte[] lenBuf;
        switch (payloadLength)
        {
            // length <= 125 まではそのまま使える
            // それ以外のケースの場合2byte先,8byte先を読む
            case 126:
                lenBuf = new byte[2];
                stream.ReadExactly(lenBuf);
                Array.Reverse(lenBuf);
                payloadLength = BitConverter.ToInt16(lenBuf, 0);
                break;
            case 127:
                lenBuf = new byte[8];
                stream.ReadExactly(lenBuf);
                Array.Reverse(lenBuf);
                payloadLength = BitConverter.ToInt64(lenBuf, 0);
                break;
        }

        byte[] masks = new byte[4];
        if (isMask) stream.ReadExactly(masks);

        byte[] payload = new byte[payloadLength];
        stream.ReadExactly(payload);

        if (isMask)
        {
            for (int i = 0; i < payload.Length; i++)
            {
                payload[i] = (byte)(payload[i] ^ masks[i % 4]);
            }
        }

        string message = Encoding.UTF8.GetString(payload);
        Console.WriteLine($"[info] recive: {message}");
    }
}

Main();
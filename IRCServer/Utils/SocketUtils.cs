using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Net.Sockets;
using IRCServer.Types;

namespace IRCServer.Utils;

public static class SocketUtils
{
    // heartbeatなどのデフォルトで提供するアクションの実装
    public static bool HeartBeat(SocketManager manager, byte opcode)
    {
        if (opcode == Opcode.Ping)
        {
            manager.Send("", Opcode.Pong);
            return true;
        }
        return false;
    }

    public static bool HeartBeatWithCustomString(SocketManager manager, string? message, string trigger, string reply)
    {
        if (message == trigger)
        {
            manager.Send(reply);
            return true;
        }
        return false;
    }
    public static class HandShake
    {
        public static (bool, string? header) IsWant(NetworkStream stream)
        {
            byte[] bytes = new byte[1024];
            int count = stream.Read(bytes, 0, bytes.Length);
            if (count == 0) return (false, null);
            string header = Encoding.UTF8.GetString(bytes);

            if (Regex.IsMatch(header, "^GET", RegexOptions.IgnoreCase))
            {
                return (true, header);
            }
            return (false, null);
        }

        public static void DoHandShake(NetworkStream stream, string header)
        {
            byte[] response = CreateResponse(header);
            stream.Write(response, 0, response.Length);
        }

        private static byte[] CreateResponse(string header)
        {
            // clientがランダムなキーをSec-WebSocket-Keyとしてheaderに入れてくる
            // それをGUID (RFC6455) で決められている "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" とくっつける
            // それをsha-1でhashそしてbase64してからSec-WebSocket-Acceptに乗せて返す
            string wsKey = Regex.Match(header, "Sec-WebSocket-Key:(.*)").Groups[1].Value.Trim();
            string acceptKey = CreateAcceptKey(wsKey);

            byte[] response = Encoding.UTF8.GetBytes(
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Accept: " + acceptKey + "\r\n\r\n"
            );

            return response;
        }

        private static string CreateAcceptKey(string wsKey)
        {
            string acceptKey = wsKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            byte[] hash = SHA1.HashData(Encoding.UTF8.GetBytes(acceptKey));
            return Convert.ToBase64String(hash);
        }
    }
}
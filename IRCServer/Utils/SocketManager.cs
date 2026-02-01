using System.Text;
using System.Net.Sockets;
using IRCServer.Types;
using IRC.Shared.Types;
using Opcode = IRCServer.Types.Opcode;

namespace IRCServer.Utils;

public class SocketManager : ISendable
{
    private readonly NetworkStream _stream;
    public string currentChannel = "default";
    public Action<SocketManager, string?, byte, byte[]>? OnMessage { get; set; }
    public Action<SocketManager>? OnClose { get; set; }

    public SocketManager(NetworkStream stream, int timeOut = 30 * 1000)
    {
        _stream = stream;
        _stream.ReadTimeout = timeOut;
    }

    public void Listen()
    {
        ClientManager.Add(this);
        ReceiveLoop();
    }

    public void Send(string message, byte opcode = Opcode.Text)
    {
        if (message == null) return;
        byte headOneByte = (byte)(0x80 | (opcode & 0x0F)); // 右4bitだけに変換
        byte[] payload = Encoding.UTF8.GetBytes(message);
        byte[] header;

        // headerの組み立て 125以下 126 127指定
        if (payload.Length <= 125)
        {
            header = new byte[2];
            header[0] = headOneByte; // Opcode text frame
            header[1] = (byte)payload.Length;
        }
        else if (payload.Length <= 65535)
        {
            header = new byte[4];
            header[0] = headOneByte;
            header[1] = 126; // 126指定　2byte
            byte[] lenBytes = BitConverter.GetBytes((ushort)payload.Length);
            Array.Reverse(lenBytes);
            Array.Copy(lenBytes, 0, header, 2, 2);
        }
        else
        {
            header = new byte[10];
            header[0] = headOneByte;
            header[1] = 127; // 127指定 6byte
            byte[] lenBytes = BitConverter.GetBytes((long)payload.Length);
            Array.Reverse(lenBytes);
            Array.Copy(lenBytes, 0, header, 2, 8);
        }

        try
        {
            lock (_stream)
            {
                _stream.Write(header, 0, header.Length);
                _stream.Write(payload, 0, payload.Length);
            }
        } catch
        {
            this.Close();
        }
    }

    private void ReceiveLoop()
    {
        /*
            bufferなので出す順番を間違えると崩壊する (当たり前)
            長さによって 2^n のbyteにbufを広げないといけない
            その長さはpayloadのlengthがheaderにあるので読む
        */
        try
        {
            while (true)
            {
                // [0]にはFINフラグ、種別が入っている
                // [1] にはmaskのflagとdataのlength
                byte[] head = new byte[2];
                _stream.ReadExactly(head);

                byte opcode = (byte)(head[0] & 0x0F);
                // もしcloseの場合は閉じる
                if (opcode == Opcode.Close) {
                    this.Close();
                    break;
                }

                bool isMask = (head[1] & 0x80) != 0; // 一番左の1bitを読む
                long payloadLength = head[1] & 0x7F; // mask flag以外の右側の7bitを読む

                byte[] lenBuf;
                // length <= 125 まではそのまま使える
                // それ以外のケースの場合2byte先,8byte先を読む
                if (payloadLength == 126)
                {
                    lenBuf = new byte[2];
                    _stream.ReadExactly(lenBuf);
                    Array.Reverse(lenBuf);
                    payloadLength = BitConverter.ToUInt16(lenBuf, 0);
                }
                else if (payloadLength >= 127)
                {
                    lenBuf = new byte[8];
                    _stream.ReadExactly(lenBuf);
                    Array.Reverse(lenBuf);
                    payloadLength = BitConverter.ToInt64(lenBuf, 0);
                }

                byte[] masks = new byte[4];
                if (isMask) _stream.ReadExactly(masks);

                byte[] payload = new byte[payloadLength];
                _stream.ReadExactly(payload);

                if (isMask)
                {
                    for (int i = 0; i < payload.Length; i++)
                    {
                        payload[i] = (byte)(payload[i] ^ masks[i % 4]);
                    }
                }

                string message = Encoding.UTF8.GetString(payload).Trim('\0');
                OnMessage?.Invoke(this, message, opcode, payload);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"[info] Client disconnected {e.Message}");
        }
        finally
        {
            this.Close();
        }
    }

    public void Close()
    {
        OnClose?.Invoke(this);
        _stream.Close();
        ClientManager.Remove(this);
    }
}
using System.Text;
using System.Net.Sockets;

namespace IRCServer.Utils;

public class SocketManager
{
    private readonly NetworkStream _stream;

    public SocketManager(NetworkStream stream)
    {
        _stream = stream;

        ReceiveLoop();
    }

    private void ReceiveLoop()
    {
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
            _stream.ReadExactly(head);

            bool isMask = (head[1] & 0x80) != 0; // 一番左の1bitを読む
            long payloadLength = head[1] & 0x7F; // mask flag以外の右側の7bitを読む

            byte[] lenBuf;
            switch (payloadLength)
            {
                // length <= 125 まではそのまま使える
                // それ以外のケースの場合2byte先,8byte先を読む
                case 126:
                    lenBuf = new byte[2];
                    _stream.ReadExactly(lenBuf);
                    Array.Reverse(lenBuf);
                    payloadLength = BitConverter.ToUInt16(lenBuf, 0);
                    break;
                case 127:
                    lenBuf = new byte[8];
                    _stream.ReadExactly(lenBuf);
                    Array.Reverse(lenBuf);
                    payloadLength = BitConverter.ToInt64(lenBuf, 0);
                    break;
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

            string message = Encoding.UTF8.GetString(payload);
        }
    }
}
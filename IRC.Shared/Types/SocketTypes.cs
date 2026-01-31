namespace IRC.Shared.Types;

/// <summary>
/// WebSocketのOpcodeを名前で扱いやすく定義
/// </summary>
public static class Opcode
{
    public const byte Continuation = 0x00;
    public const byte Text = 0x01;
    public const byte Binary = 0x02;
    public const byte Close = 0x08;
    public const byte Ping = 0x09;
    public const byte Pong = 0x0A;
}
using Tcp.Abstractions;

namespace RandomChat.Models;

public readonly struct ServerMessage : ITcpMessage
{
    public ServerMessage(MessageType type, string? data = null) {
        Type = type;
        Data = data;
    }
    
    public MessageType Type { get; }
    
    public string? Data { get; }
    
    public enum MessageType
    {
        WaitingForPartner,
        PartnerConnected,
        PartnerDisconnected,
        PartnerTyping,
        PartnerMessage
    }
}
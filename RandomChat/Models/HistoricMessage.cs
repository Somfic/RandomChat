namespace RandomChat.Models;

public readonly struct HistoricMessage
{
    public HistoricMessage(bool isSender, Message message)
    {
        IsSender = isSender;
        Message = message;
    }
    
    public HistoricMessage(bool isSender, DateTime timestamp, string content)
    {
        IsSender = isSender;
        Message = new Message(timestamp, content);
    }
    
    public bool IsSender { get; }
    
    public Message Message { get; }
}
namespace RandomChat.Models;

public readonly struct Message
{
    public Message(DateTime timestamp, string content)
    {
        Timestamp = timestamp;
        Content = content;
    }
    
    public DateTime Timestamp { get; }

    public string Content { get; }
}
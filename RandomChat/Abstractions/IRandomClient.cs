using RandomChat.Models;

namespace RandomChat.Abstractions;

public interface IRandomClient
{
    /// <summary>
    /// A delegate that is called when a partner connects to the client.
    /// </summary>
    public delegate Task PartnerConnected();
    
    /// <summary>
    /// A delegate that is called when a partner disconnects from the client.
    /// </summary>
    public delegate Task PartnerDisconnected();
    
    /// <summary>
    /// A delegate that is called when a partner sends a message to the client.
    /// </summary>
    public delegate Task PartnerMessage(Message message);
    
    /// <summary>
    /// A delegate that is called when a partner starts typing.
    /// </summary>
    public delegate Task PartnerStartedTyping();
    
    /// <summary>
    /// A delegate that is called when a partner stops typing.
    /// </summary>
    public delegate Task PartnerStoppedTyping();
    
    /// <summary>
    /// A delegate that is called when the client sends a message to the partner.
    /// </summary>
    public delegate Task OutgoingMessage(Message message);
    
    /// <summary>
    /// A delegate that is called when the client is waiting for a new partner.
    /// </summary>
    public delegate Task WaitingForPartner();
    
    /// <summary>
    /// Whether or not the client has a partner.
    /// </summary>
    bool HasPartner { get; }
    
    /// <summary>
    /// Whether or not the partner is typing.
    /// </summary>
    bool PartnerIsTyping { get; }
    
    /// <summary>
    /// A collection of all messages sent and received by the client.
    /// </summary>
    IReadOnlyCollection<HistoricMessage> History { get; }

    int? LastPort { get; }

    /// <summary>
    /// Connects to a server.
    /// </summary>
    /// <param name="port">The port to connect to.</param>
    Task ConnectAsync(int port);
    
    /// <summary>
    /// Disconnects from the server.
    /// </summary>
    Task DisconnectAsync();
    
    /// <summary>
    /// Registers a callback for when a partner connects to the client.
    /// </summary>
    /// <param name="callback">The callback to register.</param>
    void OnPartnerConnected(PartnerConnected callback);
    
    /// <summary>
    /// Registers a callback for when a partner disconnects from the client.
    /// </summary>
    /// <param name="callback">The callback to register.</param>
    void OnPartnerDisconnected(PartnerDisconnected callback);
    
    /// <summary>
    /// Registers a callback for when a partner starts typing.
    /// </summary>
    /// <param name="callback">The callback to register.</param>
    void OnPartnerStartedTyping(PartnerStartedTyping callback);
    
    /// <summary>
    /// Registers a callback for when a partner stops typing.
    /// </summary>
    /// <param name="callback">The callback to register.</param>
    void OnPartnerStoppedTyping(PartnerStoppedTyping callback);
    
    /// <summary>
    /// Registers a callback for when a partner sends a message to the client.
    /// </summary>
    /// <param name="callback">The callback to register.</param>
    void OnPartnerMessage(PartnerMessage callback);
    
    /// <summary>
    /// Registers a callback for when the client sends a message to the partner.
    /// </summary>
    /// <param name="callback">The callback to register.</param>
    void OnOutgoingMessage(OutgoingMessage callback);
    
    /// <summary>
    /// Registers a callback for when the client is waiting for a new partner.
    /// </summary>
    /// <param name="callback">The callback to register.</param>
    void OnWaitingForPartner(WaitingForPartner callback);
    
    /// <summary>
    /// Sends a message to the partner.
    /// </summary>
    /// <param name="message">The message to send.</param>
    Task SendAsync(string message);
    
    /// <summary>
    /// Marks the client as typing.
    /// </summary>
    Task MarkAsTypingAsync();
}
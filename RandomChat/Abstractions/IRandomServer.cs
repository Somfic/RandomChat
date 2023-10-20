namespace RandomChat.Abstractions;

public interface IRandomServer
{
    /// <summary>
    /// A collection of all clients that are waiting for a partner.
    /// </summary>
    IReadOnlyCollection<Guid> WaitingClients { get; }
    
    /// <summary>
    /// A dictionary of all clients and their partners.
    /// </summary>
    IReadOnlyDictionary<Guid, Guid> Rooms { get; }
    
    /// <summary>
    /// The port the server is listening on.
    /// </summary>
    int Port { get; }
    
    /// <summary>
    /// Starts the server.
    /// </summary>
    Task StartAsync();
    
    /// <summary>
    /// Stops the server.
    /// </summary>
    Task StopAsync();
}
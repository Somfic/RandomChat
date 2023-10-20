namespace Tcp.Abstractions;

public interface IServer
{
    /// <summary>
    /// A delegate that is called when a client connects to the server.
    /// </summary>
    public delegate Task ClientConnectedToServerDelegate(Guid client);
    
    /// <summary>
    /// A delegate that is called when a client requests data from the server.
    /// </summary>
    public delegate Task ClientDisconnectedFromServerDelegate(Guid client);
    
    /// <summary>
    /// A delegate that is called when a client disconnects from the server.
    /// </summary>
    public delegate Task ClientRequestedToServerDelegate(Guid client, string data);
    
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
    
    /// <summary>
    /// Registers a callback for when a client connects to the server.
    /// </summary>
    /// <param name="callback">The callback to register.</param>
    void OnClientConnected(ClientConnectedToServerDelegate callback);
    
    /// <summary>
    /// Registers a callback for when a client disconnects from the server.
    /// </summary>
    /// <param name="callback">The callback to register.</param>
    void OnClientRequested(ClientRequestedToServerDelegate callback);
    
    /// <summary>
    /// Registers a callback for when a client requests data from the server.
    /// </summary>
    /// <param name="callback">The callback to register.</param>
    void OnClientDisconnected(ClientDisconnectedFromServerDelegate callback);
    
    /// <summary>
    /// Sends data to a client.
    /// </summary>
    /// <param name="client">The client to send data to.</param>
    /// <param name="data">The data to send.</param>
    /// <typeparam name="T">The type of data to send.</typeparam>
    Task SendAsync<T>(Guid client, T data) where T : ITcpMessage;
    
    /// <summary>
    /// Sends data to all clients.
    /// </summary>
    /// <param name="data">The data to send.</param>
    /// <typeparam name="T">The type of data to send.</typeparam>
    Task SendAsync<T>(T data) where T : ITcpMessage;
    
    /// <summary>
    /// Checks if a client is connected to the server.
    /// </summary>
    /// <param name="client">The client to check.</param>
    bool IsConnected(Guid client);
}
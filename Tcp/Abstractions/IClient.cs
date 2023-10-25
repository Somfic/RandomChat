namespace Tcp.Abstractions;

public interface IClient
{
    /// <summary>
    /// A delegate that is called when the client connects to the server.
    /// </summary>
    public delegate Task ServerConnectedDelegate();
    
    /// <summary>
    /// A delegate that is called when the server responds to the client.
    /// </summary>
    public delegate Task ServerRespondedDelegate(string data);
    
    /// <summary>
    /// A delegate that is called when the client disconnects from the server.
    /// </summary>
    public delegate Task ServerDisconnectedDelegate();
    
    /// <summary>
    /// The port the client is connected to.
    /// </summary>
    int Port { get; }

    int? LastPort { get; }

    /// <summary>
    /// Connects to a server.
    /// </summary>
    /// <param name="port">An optional port to connect to.</param>
    Task ConnectAsync(int? port = null);
    
    /// <summary>
    /// Registers a callback for when the client connects to the server.
    /// </summary>
    /// <param name="callback">The callback to register.</param>
    void OnServerConnected(ServerConnectedDelegate callback);
    
    /// <summary>
    /// Registers a callback for when the server responds to the client.
    /// </summary>
    /// <param name="callback">The callback to register.</param>
    void OnServerResponded(ServerRespondedDelegate callback);
    
    /// <summary>
    /// Registers a callback for when the client disconnects from the server.
    /// </summary>
    /// <param name="callback">The callback to register.</param>
    void OnServerDisconnected(ServerDisconnectedDelegate callback);
    
    /// <summary>
    /// Sends data to the server.
    /// </summary>
    /// <param name="data">The data to send.</param>
    /// <typeparam name="T">The type of data to send.</typeparam>
    Task SendAsync<T>(T data) where T : ITcpMessage;
    
    /// <summary>
    /// Disconnects from the server.
    /// </summary>
    Task DisconnectAsync();
}
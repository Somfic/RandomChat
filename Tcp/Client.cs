using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Tcp;

public class Client
{
    private static readonly Encoding Encoding = Encoding.UTF8;
    
    private readonly ILogger<Client> _log;
    private readonly TcpClient _client = new();
    
    private readonly List<ServerConnectedDelegate> _connectedHandlers = new();
    private readonly List<ServerRespondedDelegate> _respondedHandlers = new();
    private readonly List<ServerDisconnectedDelegate> _disconnectedHandlers = new();
    
    private int _port;
    
    public Client(ILogger<Client> log, IConfiguration config)
    {
        _log = log;
        _port = config.GetValue("Tcp:Port", 51555);
    }

    public async Task ConnectAsync(int? port = null)
    {
        _port = port ?? _port;
        
        _log.LogDebug("Connecting to port {Port} ... ", _port);
        
        await _client.ConnectAsync(IPAddress.Loopback, _port);
        
        _log.LogInformation("Connected to port {Port}", _port);

        Task.Run(async () => await HandleServer());
    }
    
    public void OnServerConnected(ServerConnectedDelegate callback) =>
        _connectedHandlers.Add(callback);
    
    public void OnServerResponded(ServerRespondedDelegate callback) =>
        _respondedHandlers.Add(callback);
    
    public void OnServerDisconnected(ServerDisconnectedDelegate callback) =>
        _disconnectedHandlers.Add(callback);

    private async Task HandleServer()
    {
        foreach (var handler in _connectedHandlers)
        {
            try
            {
                await handler();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Unhandled exception in {Delegate} on connected", handler.Method.Name);
            }
        }

        var stream = _client.GetStream();
        var buffer = new byte[1024];

        while (_client.Connected)
        {
            var read = await stream.ReadAsync(buffer);

            if (read == 0)
                break;

            var data = Encoding.GetString(buffer, 0, read);

            _log.LogTrace("Server: {Data}", data);

            foreach (var handler in _respondedHandlers)
            {
                try
                {
                    await handler(data);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Unhandled exception in {Delegate} on data received", handler.Method.Name);
                }
            }
        }

        foreach (var handler in _disconnectedHandlers)
        {
            try
            {
                await handler();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Unhandled exception in {Delegate} on disconnected", handler.Method.Name);
            }
        }

        _log.LogInformation("Disconnected from server");
    }
    
    public delegate Task ServerConnectedDelegate();
    public delegate Task ServerRespondedDelegate(string data);
    public delegate Task ServerDisconnectedDelegate();

    public async Task SendAsync(string data)
    {
        _log.LogTrace("Client: {Data}", data);
        
        var clientStream = _client.GetStream();
        var buffer = Encoding.GetBytes(data);
        await clientStream.WriteAsync(buffer);
    }
}
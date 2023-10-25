using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Tcp.Abstractions;

namespace Tcp;

public class Client : IClient
{
    private static readonly Encoding Encoding = Encoding.UTF8;
    
    private readonly ILogger<Client> _log;
    private readonly TcpClient _client = new();
    
    private readonly List<IClient.ServerConnectedDelegate> _connectedHandlers = new();
    private readonly List<IClient.ServerRespondedDelegate> _respondedHandlers = new();
    private readonly List<IClient.ServerDisconnectedDelegate> _disconnectedHandlers = new();
    
    private int _port;
    
    public Client(ILogger<Client> log, IConfiguration config)
    {
        _log = log;
        _port = config.GetValue("Tcp:Port", 51555);
    }

    public int Port => _client.Client.LocalEndPoint is IPEndPoint endpoint ? endpoint.Port : 0;
    public int? LastPort => GetLastUsedPort();

    public async Task ConnectAsync(int? port = null)
    {
        _port = port ?? _port;
        
        _log.LogDebug("Connecting to port {Port} ... ", _port);

        SetLastUsedPort(_port);
        
        await _client.ConnectAsync(IPAddress.Loopback, _port);

        while (!_client.Connected)
            await Task.Delay(100);
        
        _log.LogInformation("Connected to port {Port}", _port);

        Task.Run(async () => await HandleServer());
    }

    public void OnServerConnected(IClient.ServerConnectedDelegate callback) =>
        _connectedHandlers.Add(callback);
    
    public void OnServerResponded(IClient.ServerRespondedDelegate callback) =>
        _respondedHandlers.Add(callback);
    
    public void OnServerDisconnected(IClient.ServerDisconnectedDelegate callback) =>
        _disconnectedHandlers.Add(callback);

    public async Task SendAsync<T>(T data) where T : ITcpMessage
    {
        var clientStream = _client.GetStream();
        var json = JsonConvert.SerializeObject(data);
        
        _log.LogTrace("< Server: {Data}", json);
        
        var buffer = Encoding.GetBytes(json);
        await clientStream.WriteAsync(buffer);
        await clientStream.FlushAsync();
    }

    public async Task DisconnectAsync()
    {
        _log.LogDebug("Disconnecting from server ...");
        
        await _client.GetStream().FlushAsync();
        _client.Close();
        
        _log.LogInformation("Disconnected from server");
    }
    
    private async Task HandleServer()
    {
        foreach (var handler in _connectedHandlers)
        {
            try { await handler(); }
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

            _log.LogTrace("< Server: {Data}", data);

            foreach (var handler in _respondedHandlers)
            {
                try { await handler(data); }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Unhandled exception in {Delegate} on data received", handler.Method.Name);
                }
            }
        }

        foreach (var handler in _disconnectedHandlers)
        {
            try { await handler(); }
            catch (Exception ex)
            {
                _log.LogError(ex, "Unhandled exception in {Delegate} on disconnected", handler.Method.Name);
            }
        }

        _log.LogInformation("Disconnected from server");
    }
    
    private int? GetLastUsedPort()
    {
        if (!File.Exists("cache"))
            return null;
        
        var port = File.ReadAllText("cache");
        
        if (int.TryParse(port, out var result))
            return result;
        
        return null;
    }
    
    private void SetLastUsedPort(int port)
    {
        File.WriteAllText("cache", port.ToString());
    }
}
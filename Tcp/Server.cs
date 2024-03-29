﻿using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Tcp.Abstractions;

namespace Tcp;

public class Server : IServer
{
    private static readonly Encoding Encoding = Encoding.UTF8;

    private readonly ILogger<Server> _log;
    
    private readonly TcpListener _listener;
    private readonly Dictionary<Guid, TcpClient> _clients = new();
    
    private readonly List<IServer.ClientConnectedToServerDelegate> _connectedHandlers = new();
    private readonly List<IServer.ClientRequestedToServerDelegate> _requestedHandlers = new();
    private readonly List<IServer.ClientDisconnectedFromServerDelegate> _disconnectedHandlers = new();
    
    public Server(ILogger<Server> log, IConfiguration config)
    {
        _log = log;
        _listener = new TcpListener(IPAddress.Any, config.GetValue("Tcp:Port", 0));
    }
    
    public int Port => _listener.LocalEndpoint is IPEndPoint endpoint ? endpoint.Port : 0;

    public async Task StartAsync()
    {
        _log.LogDebug("Starting server on port {Port} ... ", Port);

        try { _listener.Start(); }
        catch (SocketException ex)
        {
            _log.LogError(ex, "Failed to start server on port {Port}", Port);
            throw;
        }

        _log.LogInformation("Server started on port {Port}", Port);

        Task.Run(async () =>
        {
            try
            {
                while (_listener.Server.IsBound)
                {
                    _log.LogDebug("Waiting for client ... ");
                    var client = await _listener.AcceptTcpClientAsync();
                    HandleClient(client);
                }
            } catch (Exception ex) {
                _log.LogError(ex, "Unhandled exception in server loop");
            }

            _log.LogInformation("Server stopped");
        });
    }

    public async Task StopAsync()
    {
        _log.LogDebug("Stopping server ... ");
        _listener.Stop();

        while (_listener.Server.IsBound)
            await Task.Delay(1);
    }
    
    public void OnClientConnected(IServer.ClientConnectedToServerDelegate callback) =>
        _connectedHandlers.Add(callback);
    
    public void OnClientRequested(IServer.ClientRequestedToServerDelegate callback) =>
        _requestedHandlers.Add(callback);
    
    public void OnClientDisconnected(IServer.ClientDisconnectedFromServerDelegate callback) =>
        _disconnectedHandlers.Add(callback);
    
    public async Task SendAsync<T>(Guid client, T data) where T : ITcpMessage
    {
        if (!_clients.ContainsKey(client))
        {
            _log.LogWarning("Client {Guid} not found. Connected clients: {Clients}", client, _clients.Keys);
            throw new ArgumentException("Client not found", nameof(client));
        }

        var clientStream = _clients[client].GetStream();
        var json = JsonConvert.SerializeObject(data);
        
        _log.LogTrace("> {Guid}: {Data}", client, json);
        
        var buffer = Encoding.GetBytes(json);
        await clientStream.WriteAsync(buffer);
        await clientStream.FlushAsync();
    }

    public async Task SendAsync<T>(T data) where T : ITcpMessage
    {
        foreach (var client in _clients)
            await SendAsync(client.Key, data);
    }

    public bool IsConnected(Guid client)
    {
        if (!_clients.ContainsKey(client))
            return false;
        
        var tcpClient = _clients[client];
        return tcpClient.Connected;
    }
    
    private void HandleClient(TcpClient client)
    {
        var guid = Guid.NewGuid();
        _clients.Add(guid, client);

        _log.LogDebug("{Guid} connected", guid);
        
        // Start on a new thread so that multiple clients can be handled at the same time
        Task.Run(async () =>
        {
          
                foreach (var handler in _connectedHandlers)
                {
                    try
                    {
                        await handler(guid);
                    }
                    catch (Exception ex)
                    {
                        _log.LogWarning(ex, "{Guid}: Unhandled exception in {Delegate} on connect", guid,
                            handler.Method.Name);
                    }
                }

                try
                {

                    var stream = client.GetStream();
                    var buffer = new byte[4096];

                    while (client.Connected)
                    {
                        var read = await stream.ReadAsync(buffer);

                        if (read == 0)
                            break;

                        var data = Encoding.GetString(buffer, 0, read);

                        _log.LogTrace("< {Guid}: {Data}", guid, data);

                        foreach (var handler in _requestedHandlers)
                        {
                            try
                            {
                                await handler(guid, data);
                            }
                            catch (Exception ex)
                            {
                                _log.LogWarning(ex, "{Guid}: Unhandled exception in {Delegate} on data received", guid,
                                    handler.Method.Name);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Unhandled exception in client handler");
                }

                _log.LogDebug("Client {Guid} disconnected", guid);

                foreach (var handler in _disconnectedHandlers)
                {
                    try
                    {
                        await handler(guid);
                    }
                    catch (Exception ex)
                    {
                        _log.LogWarning(ex, "{Guid}: Unhandled exception in {Delegate} on disconnect", guid,
                            handler.Method.Name);
                    }
                }

                _clients.Remove(guid);
        });
    }
}
using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.Extensions.Logging;
namespace RandomChat;

public readonly struct ServerMessage
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
        PartnerMessage
    }
}

public class RandomServer
{
    private readonly ILogger<RandomServer> _log;
    private readonly Tcp.Server _server;
    
    private readonly Queue<Guid> _waitingClients = new();
    private readonly Dictionary<Guid, Guid> _rooms = new();

    public RandomServer(ILogger<RandomServer> log, Tcp.Server server)
    {
        _log = log;
        _server = server;
    }
    
    public IReadOnlyCollection<Guid> WaitingClients => _waitingClients.ToImmutableArray();
    public IReadOnlyDictionary<Guid, Guid> Rooms => _rooms.ToImmutableDictionary();
    
    public int Port => _server.Port;

    public async Task StartAsync()
    {
        _server.OnClientConnected(OnClientConnected);
        _server.OnClientDisconnected(OnClientDisconnected);
        _server.OnClientRequested(OnClientMessage);
        
        await _server.StartAsync();
    }

    private async Task OnClientConnected(Guid client)
    {
        await EnqueueClient(client);
    }

    private async Task OnClientMessage(Guid client, string json)
    {
        var data = JsonSerializer.Deserialize<ServerMessage>(json);
        
        if (data.Type != ServerMessage.MessageType.PartnerMessage)
            return;
        
        if (data.Data == null)
        {
            _log.LogWarning("Partner message is null");
            return;
        }
        
        if (!_rooms.ContainsKey(client))
        {
            _log.LogWarning("Client {Client} is not in a room", client);
            return;
        }
        
        
        if (_rooms.TryGetValue(client, out var partner))
            await _server.SendAsync(partner, new ServerMessage(ServerMessage.MessageType.PartnerMessage, data.Data));
    }

    private async Task OnClientDisconnected(Guid client)
    {
        // Remove the disconnected client from the room
        _rooms.Remove(client);
        
        // Check if the partner is still connected
        if (_rooms.TryGetValue(client, out var partner) && !_server.IsConnected(partner))
        {
            // Let the partner know that the other client disconnected
            await _server.SendAsync(partner, new ServerMessage(ServerMessage.MessageType.PartnerDisconnected));
            _rooms.Remove(partner);
            
            // Re-enqueue the partner
            await EnqueueClient(partner);
        }
    }
    
    private async Task EnqueueClient(Guid client)
    {
        _waitingClients.Enqueue(client);
        await _server.SendAsync(client, new ServerMessage(ServerMessage.MessageType.WaitingForPartner));
        
        
        await TryMatchMaking();
    }
    
    private async Task TryMatchMaking()
    {
        if (_waitingClients.Count < 2)
            return;
        
        var client1 = _waitingClients.Dequeue();
        var client2 = _waitingClients.Dequeue();
        
        _log.LogInformation("Matched {Client1} with {Client2}", client1, client2);
        
        _rooms.Add(client1, client2);
        _rooms.Add(client2, client1);
        
        await _server.SendAsync(client1, new ServerMessage(ServerMessage.MessageType.PartnerConnected));
        await _server.SendAsync(client2, new ServerMessage(ServerMessage.MessageType.PartnerConnected));
    }

    public async Task StopAsync()
    {
        await _server.StopAsync();
    }
}

public class RandomClient
{
    private readonly ILogger<RandomClient> _log;
    private readonly Tcp.Client _client;
    
    private readonly List<PartnerConnected> _partnerConnectedHandlers = new();
    private readonly List<PartnerDisconnected> _partnerDisconnectedHandlers = new();
    private readonly List<PartnerMessage> _partnerMessageHandlers = new();
    private readonly List<WaitingForPartner> _waitingForPartnerHandlers = new();
    
    public bool HasPartner { get; private set; }

    public RandomClient(ILogger<RandomClient> log, Tcp.Client client)
    {
        _log = log;
        _client = client;
    }
    
    public async Task ConnectAsync(int port)
    {
        _client.OnServerConnected(OnServerConnected);
        _client.OnServerResponded(OnServerResponded);
        _client.OnServerDisconnected(OnServerDisconnected);
        
        await _client.ConnectAsync(port);
    }
    
    public async Task SendAsync(string message)
    {
        if (!HasPartner)
        {
            _log.LogWarning("Cannot send message to partner because there is no partner");
            return;
        }
        
        await _client.SendAsync(new ServerMessage(ServerMessage.MessageType.PartnerMessage, message));
    }
    
    public void OnPartnerConnected(PartnerConnected callback) =>
        _partnerConnectedHandlers.Add(callback);
    
    public void OnPartnerDisconnected(PartnerDisconnected callback) =>
        _partnerDisconnectedHandlers.Add(callback);
    
    public void OnPartnerMessage(PartnerMessage callback) =>
        _partnerMessageHandlers.Add(callback);
    
    public void OnWaitingForPartner(WaitingForPartner callback) =>
        _waitingForPartnerHandlers.Add(callback);

    private Task OnServerConnected()
    {
        _log.LogInformation("Connected to server on port {Port}", _client.Port);
        return Task.CompletedTask;
    }
    
    private async Task OnServerResponded(string json)
    {
        var data = JsonSerializer.Deserialize<ServerMessage>(json);

        switch (data.Type)
        {
            case ServerMessage.MessageType.PartnerConnected:
                _log.LogInformation("Partner connected");
                HasPartner = true;
                foreach (var handler in _partnerConnectedHandlers)
                {
                    try { await handler(); }
                    catch (Exception ex) { _log.LogError(ex, "Unhandled exception in {Delegate} on partner connected", handler.Method.Name); }
                }
                break;
            
            case ServerMessage.MessageType.PartnerDisconnected:
                _log.LogInformation("Partner disconnected");
                HasPartner = false;
                foreach (var handler in _partnerDisconnectedHandlers)
                {
                    try { await handler(); }
                    catch (Exception ex) { _log.LogError(ex, "Unhandled exception in {Delegate} on partner disconnected", handler.Method.Name); }
                }
                break;
            
            case ServerMessage.MessageType.PartnerMessage:
                if (data.Data == null)
                {
                    _log.LogWarning("Partner message is null");
                    break;
                }
                
                _log.LogInformation("Partner message: {Message}", data.Data);
                foreach (var handler in _partnerMessageHandlers)
                {
                    try { await handler(data.Data); }
                    catch (Exception ex) { _log.LogError(ex, "Unhandled exception in {Delegate} on partner message", handler.Method.Name); }
                }
                break;
            
            case ServerMessage.MessageType.WaitingForPartner:
                _log.LogInformation("Waiting for partner");
                HasPartner = false;
                foreach (var handler in _waitingForPartnerHandlers)
                {
                    try { await handler(); }
                    catch (Exception ex) { _log.LogError(ex, "Unhandled exception in {Delegate} on waiting for partner", handler.Method.Name); }
                }
                break;
        }
    }
    
    
    
    private Task OnServerDisconnected()
    {
        throw new NotImplementedException();
    }
    
    public delegate Task PartnerConnected();
    public delegate Task PartnerDisconnected();
    public delegate Task PartnerMessage(string message);
    public delegate Task WaitingForPartner();
}
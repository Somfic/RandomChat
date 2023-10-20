using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RandomChat.Abstractions;
using RandomChat.Models;
using Tcp.Abstractions;

namespace RandomChat;

public class RandomServer : IRandomServer
{
    private readonly ILogger<RandomServer> _log;
    private readonly IServer _server;
    
    private readonly List<Guid> _waitingClients = new();
    private readonly Dictionary<Guid, Guid> _rooms = new();

    public RandomServer(ILogger<RandomServer> log, IServer server)
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
    
    public async Task StopAsync()
    {
        await _server.StopAsync();
    }

    private async Task OnClientConnected(Guid client)
    {
        await EnqueueClient(client);
    }

    private async Task OnClientMessage(Guid client, string json)
    {
        var data = JsonConvert.DeserializeObject<ServerMessage>(json);

        switch (data.Type)
        {
            case ServerMessage.MessageType.PartnerMessage:
            {
                if (string.IsNullOrEmpty(data.Data))
                    break;

                if (!_rooms.ContainsKey(client))
                {
                    _log.LogWarning("Client {Client} is not in a room", client);
                    break;
                }
                
                if (_rooms.TryGetValue(client, out var partner))
                    await _server.SendAsync(partner,
                        new ServerMessage(ServerMessage.MessageType.PartnerMessage, data.Data));

                break;
            }

            case ServerMessage.MessageType.PartnerTyping:
            {
                if (_rooms.TryGetValue(client, out var partner))
                    await _server.SendAsync(partner,
                        new ServerMessage(ServerMessage.MessageType.PartnerTyping));
                break;
            }
        }
    }

    private async Task OnClientDisconnected(Guid client)
    {
        // Remove client from queue
        _waitingClients.Remove(client);
        
        // Check if the partner is still connected
        if (_rooms.TryGetValue(client, out var partner))
        {
            // Let the partner know that the other client disconnected
            await _server.SendAsync(partner, new ServerMessage(ServerMessage.MessageType.PartnerDisconnected));
            _rooms.Remove(partner);
            
            // Re-enqueue the partner
            await EnqueueClient(partner);
        }
        
        // Remove room
        _rooms.Remove(client);
    }
    
    private async Task EnqueueClient(Guid client)
    {
        _waitingClients.Add(client);
        await _server.SendAsync(client, new ServerMessage(ServerMessage.MessageType.WaitingForPartner));
        
        
        await TryMatchMaking();
    }
    
    private async Task TryMatchMaking()
    {
        await Task.Delay(500);
        
        _log.LogTrace("Trying to match clients");
        
        // Try to peek two clients from the queue
        if (_waitingClients.Count < 2)
        {
            _log.LogTrace("Not enough clients to match, skipping");
            return;
        }
            
        
        var client1 = _waitingClients[0];
        var client2 = _waitingClients[1];
        
        // Remove the clients from the queue
        _waitingClients.Remove(client1);
        _waitingClients.Remove(client2);
        
        await MatchClients(client1, client2);
    }

    private async Task MatchClients(Guid client1, Guid client2)
    {
        _log.LogInformation("Matched {Client1} with {Client2}", client1, client2);
        
        _rooms.Add(client1, client2);
        _rooms.Add(client2, client1);
        
        await _server.SendAsync(client1, new ServerMessage(ServerMessage.MessageType.PartnerConnected));
        await _server.SendAsync(client2, new ServerMessage(ServerMessage.MessageType.PartnerConnected));
    }
}
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace RandomChat;

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
        var data = JsonConvert.DeserializeObject<ServerMessage>(json);
        
        _log.LogDebug("Server responded with {Type}. {Json}", data.Type.ToString(), json);

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
        return Task.CompletedTask;
    }
    
    public delegate Task PartnerConnected();
    public delegate Task PartnerDisconnected();
    public delegate Task PartnerMessage(string message);
    public delegate Task WaitingForPartner();
}
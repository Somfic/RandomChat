using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RandomChat.Abstractions;
using RandomChat.Models;
using Tcp.Abstractions;

namespace RandomChat;

public class RandomClient : IRandomClient
{
    private readonly ILogger<RandomClient> _log;
    private readonly IClient _client;
    
    private readonly List<IRandomClient.PartnerConnected> _partnerConnectedHandlers = new();
    private readonly List<IRandomClient.PartnerDisconnected> _partnerDisconnectedHandlers = new();
    private readonly List<IRandomClient.PartnerMessage> _partnerMessageHandlers = new();
    private readonly List<IRandomClient.PartnerStartedTyping> _partnerStartedTypingHandlers = new();
    private readonly List<IRandomClient.PartnerStoppedTyping> _partnerStoppedTypingHandlers = new();
    private readonly List<IRandomClient.OutgoingMessage> _outgoingMessageHandlers = new();
    private readonly List<IRandomClient.WaitingForPartner> _waitingForPartnerHandlers = new();

    private readonly Stopwatch _lastTypingBroadcast = Stopwatch.StartNew();
    private readonly Timer _typingCooldown;
    
    private readonly List<HistoricMessage> _history = new ();
    
    public bool HasPartner { get; private set; }
    
    public bool PartnerIsTyping { get; private set; }

    public int? LastPort => _client.LastPort;
    
    public IReadOnlyCollection<HistoricMessage> History => _history.ToImmutableArray();

    public RandomClient(ILogger<RandomClient> log, IClient client)
    {
        _log = log;
        _client = client;
        _typingCooldown = new Timer(async _ => await PartnerStoppedTypingHandler(), null, Timeout.Infinite, Timeout.Infinite);
    }
    
    public async Task ConnectAsync(int port)
    {
        _client.OnServerConnected(OnServerConnected);
        _client.OnServerResponded(OnServerResponded);
        _client.OnServerDisconnected(OnServerDisconnected);
        
        await _client.ConnectAsync(port);
    }
    
    public async Task DisconnectAsync()
    {
        await _client.DisconnectAsync();
    }
    
    public void OnPartnerConnected(IRandomClient.PartnerConnected callback) =>
        _partnerConnectedHandlers.Add(callback);
    
    public void OnPartnerDisconnected(IRandomClient.PartnerDisconnected callback) =>
        _partnerDisconnectedHandlers.Add(callback);
    
    public void OnPartnerStartedTyping(IRandomClient.PartnerStartedTyping callback) =>
        _partnerStartedTypingHandlers.Add(callback);
    
    public void OnPartnerStoppedTyping(IRandomClient.PartnerStoppedTyping callback) =>
        _partnerStoppedTypingHandlers.Add(callback);
    
    public void OnPartnerMessage(IRandomClient.PartnerMessage callback) =>
        _partnerMessageHandlers.Add(callback);
    
    public void OnOutgoingMessage(IRandomClient.OutgoingMessage callback) =>
        _outgoingMessageHandlers.Add(callback);
    
    public void OnWaitingForPartner(IRandomClient.WaitingForPartner callback) =>
        _waitingForPartnerHandlers.Add(callback);
    
    public async Task SendAsync(string message)
    {
        if (!HasPartner)
        {
            _log.LogWarning("Cannot send message to partner because there is no partner");
            return;
        }
        
        await PartnerStoppedTypingHandler();

        _history.Add(new HistoricMessage(true, DateTime.Now, message));
        
        foreach (var handler in _outgoingMessageHandlers)
        {
            try { await handler(new Message(DateTime.Now, message)); }
            catch (Exception ex) { _log.LogError(ex, "Unhandled exception in {Delegate} on outgoing message", handler.Method.Name); }
        }
        
        await _client.SendAsync(new ServerMessage(ServerMessage.MessageType.PartnerMessage, message));
    }
    
    public async Task MarkAsTypingAsync()
    {
        if (!HasPartner)
            return;

        if (_lastTypingBroadcast.ElapsedMilliseconds < 1000)
            return;
        
        await _client.SendAsync(new ServerMessage(ServerMessage.MessageType.PartnerTyping));
        _lastTypingBroadcast.Restart();
    }
    
    private async Task PartnerStoppedTypingHandler()
    {
        if (!PartnerIsTyping)
            return;
        
        PartnerIsTyping = false;
                
        foreach (var handler in _partnerStoppedTypingHandlers)
        {
            try { await handler(); }
            catch (Exception ex) { _log.LogError(ex, "Unhandled exception in {Delegate} on partner stopped typing", handler.Method.Name); }
        }
    }
    
    private async Task OnServerDisconnected()
    {
        await DisconnectAsync();
    }
    
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
                _history.Clear();
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

            case ServerMessage.MessageType.PartnerTyping:
            {
                _typingCooldown.Change(5000, Timeout.Infinite);

                if (PartnerIsTyping)
                    break;

                PartnerIsTyping = true;
                
                foreach (var handler in _partnerStartedTypingHandlers)
                {
                    try { await handler(); }
                    catch (Exception ex) { _log.LogError(ex, "Unhandled exception in {Delegate} on partner started typing", handler.Method.Name); }
                }
                break;
            }
            
            case ServerMessage.MessageType.PartnerMessage:
                await PartnerStoppedTypingHandler();
                
                if (data.Data == null)
                {
                    _log.LogWarning("Partner message is null");
                    break;
                }
                
                _log.LogInformation("Partner message: {Message}", data.Data);
                
                _history.Add(new HistoricMessage(false, DateTime.Now, data.Data));
                
                foreach (var handler in _partnerMessageHandlers)
                {
                    try { await handler(new Message(DateTime.Now, data.Data)); }
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
}
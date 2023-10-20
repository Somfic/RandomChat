using Castle.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;

namespace Server.Tests;

public class Tests
{
    [Test]
    public async Task ClientConnects()
    {
        var (server, client) = BuildServerClient();
        
        var serverSawClient = new TaskCompletionSource<bool>();
        var clientSawServer = new TaskCompletionSource<bool>();
        
        server.OnClientConnected(guid =>
        {
            serverSawClient.SetResult(true);
            return Task.CompletedTask;
        });
        
        client.OnServerConnected(() =>
        {
            clientSawServer.SetResult(true);
            return Task.CompletedTask;
        });
        
        
        await server.StartAsync();
        await client.ConnectAsync(server.Port);
        
        await Task.WhenAll(serverSawClient.Task, clientSawServer.Task);

        await server.StopAsync();
        
        Assert.Pass();
    }
    
    [Test]
    public async Task ServerReceivesMessage()
    {
        var (server, client) = BuildServerClient();

        var data = GenerateRandomString();
        
        var serverReceivedMessage = new TaskCompletionSource<bool>();
        
        server.OnClientRequested((guid, message) =>
        {
            if (message == data)
                serverReceivedMessage.SetResult(true);
            
            return Task.CompletedTask;
        });
        
        await server.StartAsync();
        await client.ConnectAsync(server.Port);
        
        await client.SendAsync(new TestMessage(data));
        
        await server.StopAsync();
        
        Assert.Pass();
    }
    
    [Test]
    public async Task ClientReceivesMessage()
    {
        var (server, client) = BuildServerClient();
        
        var data = GenerateRandomString();
        
        var clientReceivedMessage = new TaskCompletionSource<bool>();
        
        client.OnServerResponded(json =>
        {
            var incomingData = JsonConvert.DeserializeObject<TestMessage>(json);
            clientReceivedMessage.SetResult(incomingData.Data == data);
            return Task.CompletedTask;
        });
        
        await server.StartAsync();
        await client.ConnectAsync(server.Port);
        
        await client.SendAsync(new TestMessage(data));
        
        await Task.WhenAny(clientReceivedMessage.Task, Task.Delay(1000));
        
        Assert.That(clientReceivedMessage.Task.Result, Is.EqualTo(true));
        
        await server.StopAsync();
    }

    [Test]
    public async Task ClientsHaveUniqueGuids()
    {
        var (server, clients) = BuildServerClients(10);
        
        var guids = new List<Guid>();
        
        server.OnClientConnected(guid =>
        {
            guids.Add(guid);
            return Task.CompletedTask;
        });
        
        await server.StartAsync();
        
        foreach (var client in clients)
            await client.ConnectAsync(server.Port);
        
        await Task.Delay(1000);
        
        await server.StopAsync();
        
        Assert.That(guids, Is.Unique);
        Assert.That(guids, Has.Count.EqualTo(10));
        Assert.That(guids, Has.No.Member(Guid.Empty));
    }

    private static (Tcp.Server server, Tcp.Client client) BuildServerClient()
    {
        var services = BuildServices();
        
        return (services.GetRequiredService<Tcp.Server>(),
            services.GetRequiredService<Tcp.Client>());
    }

    private static string GenerateRandomString(int bytes = short.MaxValue) => 
        Convert.ToBase64String(Enumerable.Range(0, bytes).Select(_ => (byte)new Random().Next(0, 255)).ToArray());
    
    private static (Tcp.Server server, Tcp.Client[] clients) BuildServerClients(int amountOfClients)
    {
        var services = BuildServices();
        
        return (services.GetRequiredService<Tcp.Server>(),
            Enumerable.Range(0, amountOfClients).Select(_ => services.GetRequiredService<Tcp.Client>()).ToArray());
    }
        
    private static IServiceProvider BuildServices() => Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            services.AddSingleton<Tcp.Server>();
            services.AddTransient<Tcp.Client>();
        })
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Trace);
            logging.AddConsole();
        })
        .Build()
        .Services;

    private struct TestMessage
    {
        public TestMessage(string data)
        {
            Data = data;
        }
        
        public string Data { get; }
    }
}
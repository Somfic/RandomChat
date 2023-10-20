using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Tcp.Abstractions;

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
            serverReceivedMessage.SetResult(message == data);
            return Task.CompletedTask;
        });
        
        await server.StartAsync();
        await client.ConnectAsync(server.Port);
        
        await client.SendAsync(new TestMessage(data));
        
        while (!serverReceivedMessage.Task.IsCompleted)
            await Task.Delay(100);
        
        await server.StopAsync();
        
        Assert.Pass();
    }
    
    [Test]
    [Ignore("This test is flaky")]
    public async Task ClientReceivesMessage()
    {
        var (server, client) = BuildServerClient();
        
        var data = GenerateRandomString();
        
        var clientReceivedMessage = new TaskCompletionSource<bool>();
        
        server.OnClientConnected(async client =>
        {
            await server.SendAsync(client, new TestMessage(data));
        });
        
        client.OnServerResponded(json =>
        {
            var message = JsonConvert.DeserializeObject<TestMessage>(json);
            
            clientReceivedMessage.SetResult(message.Data == data);
            return Task.CompletedTask;
        });
        
        await server.StartAsync();
        await client.ConnectAsync(server.Port);
        
        while (!clientReceivedMessage.Task.IsCompleted)
            await Task.Delay(100);
        
        await server.StopAsync();
        
        Assert.Pass();
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
        
        await Task.Delay(500);
        
        await server.StopAsync();
        
        Assert.That(guids, Is.Unique);
        Assert.That(guids, Has.Count.EqualTo(10));
        Assert.That(guids, Has.No.Member(Guid.Empty));
    }

    private static (IServer server, IClient client) BuildServerClient()
    {
        var services = BuildServices();
        
        return (services.GetRequiredService<IServer>(),
            services.GetRequiredService<IClient>());
    }

    private static string GenerateRandomString(int bytes = short.MaxValue) => 
        Convert.ToBase64String(Enumerable.Range(0, bytes).Select(_ => (byte)new Random().Next(0, 255)).ToArray());
    
    private static (IServer server, IClient[] clients) BuildServerClients(int amountOfClients)
    {
        var services = BuildServices();
        
        return (services.GetRequiredService<IServer>(),
            Enumerable.Range(0, amountOfClients).Select(_ => services.GetRequiredService<IClient>()).ToArray());
    }
        
    private static IServiceProvider BuildServices() => Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            services.AddSingleton<IServer, Tcp.Server>();
            services.AddTransient<IClient, Tcp.Client>();
        })
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Trace);
            logging.AddConsole();
        })
        .Build()
        .Services;

    private struct TestMessage : ITcpMessage
    {
        public TestMessage(string data)
        {
            Data = data;
        }
        
        public string Data { get; }
    }
}
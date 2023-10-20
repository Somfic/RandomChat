using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RandomChat.Abstractions;

namespace RandomChat.Tests;

public class Tests
{
    [Test]
    public async Task ServerStarts()
    {
        var (server, _) = BuildServerClients(1);
        
        await server.StartAsync();
        await server.StopAsync();
        
        Assert.Pass();
    }

    [Test]
    public async Task NewClientGetsPutInQueue()
    {
        var (server, client) = BuildServerClient();
        
        var clientPutInQueue = new TaskCompletionSource<bool>();
        
        client.OnWaitingForPartner(() =>
        {
            clientPutInQueue.SetResult(true);
            return Task.CompletedTask;
        });
        
        
        await server.StartAsync();
        await client.ConnectAsync(server.Port);
        
        await clientPutInQueue.Task;
        
        Assert.That(server.WaitingClients, Has.Count.EqualTo(1));
        
        await server.StopAsync();
        
        Assert.Pass();
    }

    [Test]
    public async Task ClientsGetMatched()
    {
        var (server, clients) = BuildServerClients(2);
        
        var client1 = clients[0];
        var client2 = clients[1];

        var client1To2Message = "abc";
        var client2To1Message = "cba";
        
        var client1ReceivedMessage = new TaskCompletionSource<string>();
        var client2ReceivedMessage = new TaskCompletionSource<string>();
        
        client1.OnPartnerConnected(async () => await client1.SendAsync(client1To2Message));
        client2.OnPartnerConnected(async () => await client2.SendAsync(client2To1Message));
        
        client1.OnPartnerMessage(async message => client1ReceivedMessage.SetResult(message.Content));
        client2.OnPartnerMessage(async message => client2ReceivedMessage.SetResult(message.Content));
        
        await server.StartAsync();
        
        await client1.ConnectAsync(server.Port);
        await client2.ConnectAsync(server.Port);

        await client1ReceivedMessage.Task;
        await client2ReceivedMessage.Task;
        
        Assert.That(client1ReceivedMessage.Task.Result, Is.EqualTo(client2To1Message));
        Assert.That(client2ReceivedMessage.Task.Result, Is.EqualTo(client1To2Message));
        
        await server.StopAsync();
    }
    
    private static (IRandomServer server, IRandomClient client) BuildServerClient()
    {
        var services = BuildServices();
        
        return (services.GetRequiredService<IRandomServer>(),
            services.GetRequiredService<IRandomClient>());
    }
    
    private static (IRandomServer server, IRandomClient[] clients) BuildServerClients(int amountOfClients)
    {
        var services = BuildServices();
        
        return (services.GetRequiredService<IRandomServer>(),
            Enumerable.Range(0, amountOfClients).Select(_ => services.GetRequiredService<IRandomClient>()).ToArray());
    }
        
    private static IServiceProvider BuildServices() => Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            services.AddSingleton<Tcp.Abstractions.IServer, Tcp.Server>();
            services.AddSingleton<IRandomServer, RandomServer>();
            
            services.AddTransient<Tcp.Abstractions.IClient, Tcp.Client>();
            services.AddTransient<IRandomClient, RandomClient>();
        })
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Trace);
            logging.AddConsole();
        })
        .Build()
        .Services;
}
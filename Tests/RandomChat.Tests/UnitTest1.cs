using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
        
        var client1Matched = new TaskCompletionSource<bool>();
        var client2Matched = new TaskCompletionSource<bool>();
        
        client1.OnPartnerConnected(() =>
        {
            client1Matched.SetResult(true);
            return Task.CompletedTask;
        });
        
        client2.OnPartnerConnected(() =>
        {
            client2Matched.SetResult(true);
            return Task.CompletedTask;
        });
        
        await server.StartAsync();
        
        await client1.ConnectAsync(server.Port);
        await client2.ConnectAsync(server.Port);
        
        await Task.WhenAny(client1Matched.Task, client2Matched.Task, Task.Delay(1000));

        await server.StopAsync();
        
        Assert.That(server.WaitingClients, Has.Count.EqualTo(0));
        Assert.That(server.Rooms, Has.Count.EqualTo(2));
    }
    
    private static (RandomServer server, RandomClient client) BuildServerClient()
    {
        var services = BuildServices();
        
        return (services.GetRequiredService<RandomServer>(),
            services.GetRequiredService<RandomClient>());
    }
    
    private static (RandomServer server, RandomClient[] clients) BuildServerClients(int amountOfClients)
    {
        var services = BuildServices();
        
        return (services.GetRequiredService<RandomServer>(),
            Enumerable.Range(0, amountOfClients).Select(_ => services.GetRequiredService<RandomClient>()).ToArray());
    }
        
    private static IServiceProvider BuildServices() => Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            services.AddSingleton<Tcp.Server>();
            services.AddSingleton<RandomServer>();
            
            services.AddTransient<Tcp.Client>();
            services.AddTransient<RandomClient>();
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
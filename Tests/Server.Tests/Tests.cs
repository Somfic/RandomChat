using Castle.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

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
        
        await client.SendAsync(data);
        
        await server.StopAsync();
        
        Assert.Pass();
    }
    
    [Test]
    public async Task ClientReceivesMessage()
    {
        var (server, client) = BuildServerClient();
        
        var data = GenerateRandomString();
        
        var clientReceivedMessage = new TaskCompletionSource<bool>();
        
        client.OnServerResponded(message =>
        {
            clientReceivedMessage.SetResult(message == data);
            return Task.CompletedTask;
        });
        
        await server.StartAsync();
        await client.ConnectAsync(server.Port);
        
        await client.SendAsync(data);
        
        await Task.WhenAny(clientReceivedMessage.Task, Task.Delay(1000));
        
        Assert.That(clientReceivedMessage.Task.Result, Is.EqualTo(true));
        
        await server.StopAsync();
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
}
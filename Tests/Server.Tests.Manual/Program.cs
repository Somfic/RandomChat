using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tcp;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<Server>();
        services.AddSingleton<Client>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Trace);
    })
    .Build();

var server = host.Services.GetRequiredService<Server>();
var client = host.Services.GetRequiredService<Client>();

client.OnServerConnected(async () =>
{
    await client.SendAsync(new TestMessage("Hello from client"));
});

server.OnClientRequested(async (client, data) =>
{
    await server.SendAsync(client, new TestMessage("Hello from server"));
});

await server.StartAsync();
await client.ConnectAsync(server.Port);

Console.WriteLine("Press any key to exit");
Console.ReadKey();

await server.StopAsync();

struct TestMessage
{
    public TestMessage(string data)
    {
        Data = data;
    }
    
    public string Data { get; }
}
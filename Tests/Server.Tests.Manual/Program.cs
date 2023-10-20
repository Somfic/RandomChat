using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Server.Tests.Manual;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<Tcp.Abstractions.IServer, Tcp.Server>();
        services.AddSingleton<Tcp.Abstractions.IClient, Tcp.Client>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Trace);
    })
    .Build();

var server = host.Services.GetRequiredService<Tcp.Abstractions.IServer>();
var client = host.Services.GetRequiredService<Tcp.Abstractions.IClient>();

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
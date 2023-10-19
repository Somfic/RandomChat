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
    await client.SendAsync("Hello from client");
});

server.OnClientRequested(async (client, data) =>
{
    await server.SendAsync(client, "Hello back from server");
});

await server.StartAsync();
await client.ConnectAsync(server.Port);

Console.WriteLine("Press any key to exit");
Console.ReadKey();
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RandomChat;
using RandomChat.Abstractions;

var services = Host.CreateDefaultBuilder()
    .ConfigureServices(services =>
    {
        services.AddSingleton<Tcp.Abstractions.IServer, Tcp.Server>();
        services.AddSingleton<IRandomServer, RandomServer>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.SetMinimumLevel(LogLevel.Trace);
        logging.AddConsole();
    })
    .Build()
    .Services;

var server = services.GetRequiredService<IRandomServer>();

await server.StartAsync();

Console.ReadKey();

await server.StopAsync();
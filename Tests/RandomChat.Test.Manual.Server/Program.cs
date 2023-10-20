using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RandomChat;

var services = BuildServices();
var server = services.GetRequiredService<RandomServer>();

await server.StartAsync();

Console.ReadKey();

IServiceProvider BuildServices() => Host.CreateDefaultBuilder()
    .ConfigureServices(services =>
    {
        services.AddSingleton<Tcp.Server>();
        services.AddSingleton<RandomServer>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.SetMinimumLevel(LogLevel.Trace);
        logging.AddConsole();
    })
    .Build()
    .Services;
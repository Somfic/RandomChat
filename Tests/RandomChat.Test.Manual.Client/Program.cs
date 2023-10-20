using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RandomChat;

var services = BuildServices();
var client = services.GetRequiredService<RandomClient>();

Console.Write("Enter port: ");
var port = int.Parse(Console.ReadLine()!);

await client.ConnectAsync(port);

while (true)
{
    var message = Console.ReadLine();
    await client.SendAsync(message);
}

IServiceProvider BuildServices() => Host.CreateDefaultBuilder()
    .ConfigureServices(services =>
    {
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
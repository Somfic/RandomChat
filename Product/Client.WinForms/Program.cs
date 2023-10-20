using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RandomChat;
using RandomChat.Abstractions;

namespace Client.WinForms;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var services = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<Tcp.Abstractions.IClient, Tcp.Client>();
                services.AddSingleton<IRandomClient, RandomClient>();
                services.AddSingleton<ConnectToServerForm>();
                services.AddSingleton<ChatForm>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Error);
                logging.AddConsole();
            })
            .Build()
            .Services;
        
        var connectToServerForm = services.GetRequiredService<ConnectToServerForm>();

        Application.Run(connectToServerForm);
    }
}
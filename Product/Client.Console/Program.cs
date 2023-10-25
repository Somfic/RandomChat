using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RandomChat;
using RandomChat.Abstractions;
using Spectre.Console;

var draftMessage = string.Empty;

var client = Host.CreateDefaultBuilder()
    .ConfigureServices(services =>
    {
        services.AddSingleton<Tcp.Abstractions.IClient, Tcp.Client>();
        services.AddSingleton<IRandomClient, RandomClient>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.SetMinimumLevel(LogLevel.Error);
        logging.AddConsole();
    })
    .Build()
    .Services
    .GetRequiredService<IRandomClient>();

AnsiConsole.Write(
    new FigletText("RandomChat")
        .Centered()
        .Color(Color.Pink1));

var port = client.LastPort is null
    ? AnsiConsole.Ask<int>("[Purple]Server port[/]:")
    : AnsiConsole.Ask("[Purple]Server port[/]:", client.LastPort!.Value);

client.OnWaitingForPartner(() =>
{
    AnsiConsole.MarkupLine("[Yellow]Waiting for stranger...[/]");
    return Task.CompletedTask;
});

client.OnPartnerConnected(() =>
{
    DrawGui();
    return Task.CompletedTask;
});

client.OnPartnerStartedTyping(async () => DrawGui());
client.OnPartnerStoppedTyping(async () => DrawGui());

client.OnPartnerMessage(async _ =>
{
    DrawGui();
    await Task.CompletedTask;
});

client.OnOutgoingMessage(async message =>
{
    if(string.IsNullOrWhiteSpace(message.Content))
        return;
    
    DrawGui();
    await Task.CompletedTask;
});

await client.ConnectAsync(port);

// Refresh GUI every second in a background thread for animations
Task.Run(async () =>
{
    while (true)
    {
        await Task.Delay(1000);
        DrawGui();
    }
});

while (true)
{
    while (true)
    {
        var key = Console.ReadKey(true);
        if (key.Key == ConsoleKey.Enter)
            break;
        
        if (key.Key == ConsoleKey.Backspace)
        {
            if (draftMessage.Length == 0)
                continue;
            
            draftMessage = draftMessage[..^1];
            DrawGui();
            continue;
        }
        
        draftMessage += key.KeyChar;
        DrawGui();
        await client.MarkAsTypingAsync();
    }
    
    await client.SendAsync(draftMessage);
    draftMessage = string.Empty;
    DrawGui();
}

void DrawGui()
{
    Console.Clear();
    
    AnsiConsole.Write(
        new FigletText("RandomChat")
            .Centered()
            .Color(Color.Pink1));
    
    AnsiConsole.MarkupLine("[Green]Stranger connected![/]");
    
    foreach (var message in client.History)
    {
        if (message.IsSender)
            AnsiConsole.MarkupLine($"[Gray]{message.Message.Timestamp.ToShortTimeString()}[/]      [Silver italic]You:[/] [Blue]{message.Message.Content}[/]");
        else
            AnsiConsole.MarkupLine($"[Gray]{message.Message.Timestamp.ToShortTimeString()}[/] [Silver italic]Stranger:[/] [Red]{message.Message.Content}[/]");
    }
    
    if(client.PartnerIsTyping)
        AnsiConsole.MarkupLine($"      [Gray italic]Stranger is typing[/]");
    
    if(!string.IsNullOrWhiteSpace(draftMessage))
        AnsiConsole.MarkupLine($"           [Silver italic]You:[/] [Gray]{draftMessage}[/][Gray]_[/]");
}
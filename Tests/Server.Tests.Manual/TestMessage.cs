using Tcp.Abstractions;

namespace Server.Tests.Manual;

struct TestMessage : ITcpMessage
{
    public TestMessage(string data)
    {
        Data = data;
    }
    
    public string Data { get; }
}
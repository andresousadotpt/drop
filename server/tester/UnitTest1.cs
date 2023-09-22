using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

namespace tester;

public class Tests
{
    private TcpClient _client;
    private IPEndPoint _ipEndPoint;

    private string _ip = "127.0.0.1";
    private int _port = 8080;
    
    [SetUp]
    public void Setup()
    {
        _client = new TcpClient();
        _ipEndPoint = new IPEndPoint(IPAddress.Parse(_ip), _port);
        _client.Connect(_ipEndPoint);
    }

    [Test]
    public void SendMessageToWebSocket()
    {
        string message = "ping";
        Assert.AreEqual("pong", Message.Send(_client, message));
    }
    
    [TearDown]
    public void GlobalTeardown()
    {
        _client.Close();
    }
}
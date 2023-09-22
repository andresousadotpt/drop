using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using tester;

TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 8080);

server.Start();
Console.WriteLine("Starting server at 127.0.0.1:8080. \nWaiting for a conection...");
List<Room> rooms = new List<Room>();


while (true)
{
    TcpClient client = server.AcceptTcpClient();
    if (client.Connected)
    {
        Console.WriteLine("A client has connected");
        
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[client.ReceiveBufferSize];

        string dataReceived = buffer.Read(stream);
        Console.WriteLine("Received : " + dataReceived);
        
        IPEndPoint remoteIpEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
        Console.WriteLine("Client IP Address is: {0}", remoteIpEndPoint.Address);
        rooms.Add(new Room(remoteIpEndPoint.Address.ToString()));
        
        switch (dataReceived)
        {
            case "ping":
                Encoding.UTF8.GetBytes("pong").Send(stream);
                Console.WriteLine("Sending pong!");
                break;
            default:
                Encoding.UTF8.GetBytes("404").Send(stream);
                break;
        }
    }   
}
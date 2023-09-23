using System.Net.Sockets;
using System.Text;
using vtortola.WebSockets;

namespace server;

public class Message
{
    public async static void Send(WebSocket client, string message)
    {
        if (client.IsConnected)
        {
            using (var messageWriterStream = client.CreateMessageWriter(WebSocketMessageType.Text))
            {
                using (var sw = new StreamWriter(messageWriterStream, Encoding.UTF8))
                {
                    await sw.WriteAsync(message);
                }
            }   
        }
        else
        {
            Console.WriteLine(message);
        }
    }
}
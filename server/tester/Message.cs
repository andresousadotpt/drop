using System.Net.Sockets;
using System.Text;

namespace tester;

public class Message
{
    public static string? Send(TcpClient client, string message)
    {
        NetworkStream stream = client.GetStream();
        byte[] bytesToSend = Encoding.UTF8.GetBytes(message);

        try
        {
            bytesToSend.Send(stream);
            
            byte[] bytesToRead = new byte[client.ReceiveBufferSize];
            var received = bytesToRead.Read(stream);
            return received;
        }
        catch (Exception e)
        {
            return null;
        }
    }
}
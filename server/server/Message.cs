using System.Net.Sockets;
using System.Text;

namespace tester;

public class Message
{
    public static bool Send(TcpClient client, string message)
    {
        NetworkStream stream = client.GetStream();
        byte[] bytesToSend = Encoding.UTF8.GetBytes(message);

        try
        {
            bytesToSend.Send(stream);
            return true;
        }
        catch (Exception e)
        {
            return false;
        }

    }
}
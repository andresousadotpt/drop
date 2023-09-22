using System.Net.Sockets;
using System.Text;

namespace tester;

public static class BytesHelper
{
    public static string Read(this byte[] bytes, NetworkStream stream)
    {
        
        int bytesRead = stream.Read(bytes, 0, bytes.Length);
        return Encoding.ASCII.GetString(bytes, 0, bytesRead);
    }
    
    public static void Send(this byte[] bytes, NetworkStream stream)
    {
        
        stream.Write(bytes, 0, bytes.Length);
    }
}
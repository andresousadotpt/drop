using System.Collections.Concurrent;
using vtortola.WebSockets;

namespace server;

public class Room
{
    public string name { get; set; }
    public ConcurrentDictionary<Client, WebSocket> clients { get; set; }

    public Room(string name)
    {
        this.name = name;
        clients = new ConcurrentDictionary<Client, WebSocket>();
    }
}
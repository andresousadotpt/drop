using System.Collections.Concurrent;
using System.Net;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using vtortola.WebSockets;

namespace server.service;

public class RoomService
{
    private WebSocketListener _server;
    private List<Room> _rooms;
    private CancellationTokenSource _cts;


    public RoomService(int port)
    {
        _server = new WebSocketListener(new IPEndPoint(IPAddress.Any, 8006));
        _server.Standards.RegisterStandard(new WebSocketFactoryRfc6455());
        _server.StartAsync();
        _cts = new CancellationTokenSource();
        _rooms = new List<Room>();
        
        Console.WriteLine("Starting server...");
    }

    public async Task StartAsync()
    {
        while (true)
        {
            try
            {
                WebSocket socket = await _server.AcceptWebSocketAsync(_cts.Token);
                try
                {
                    HandleAFKEmptyRooms();
                    await HandleClientAsync(socket);
                    WebSocketMessageStream messageStream = null;
                    
                    // Add this to a method at Message class like Message.handle whatever idk
                    Task.Run(async () =>
                    {
                        messageStream = await socket.ReadMessageAsync(_cts.Token);
                    }).ContinueWith(async t =>
                    {
                        if (t.IsCompletedSuccessfully)
                        {
                            var msgContent = string.Empty;
                            using (var sr = new StreamReader(messageStream, Encoding.UTF8))
                                msgContent = await sr.ReadToEndAsync();
                            // await KeepClientAlive(socket);
                            await HandleClientMessages(socket, msgContent);
                        }
                        else if (t.IsFaulted)
                        {
                            Console.WriteLine("Error reading message: " + t.Exception.InnerException.Message);
                        }
                    });


                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Accepting clients: " + ex.GetBaseException().Message);
                _cts.Cancel();
            }
        }
    }
    public void HandleAFKEmptyRooms()
    {
        // Have to verify this
        _rooms.RemoveAll(room => room.clients.IsEmpty);

        int timeout = 10000;

        Parallel.ForEach(_rooms, room =>
        {
            var keysToRemove = room.clients
                .Where(kv => (DateTime.Now - kv.Key.lastBeat).TotalMilliseconds > (2 * timeout))
                .ToList();
            
            foreach (var (key, value) in keysToRemove)
            {
                value.Close();
                room.clients.TryRemove(key, out _);
            }
        });
    }

    public async Task HandleClientAsync(WebSocket socket)
    {
        string clientIp = GetClientIP(socket).Split(":").First();
        
        Client client;
        
        if (_rooms.Any(x => x.name.Equals(clientIp)))
        {
            var room = _rooms.Find(x => x.name.Equals(clientIp));
            client = new Client(Guid.NewGuid().ToString(), DateTime.Now);
            Message.Send(socket, JsonSerializer.Serialize(client));
            
            room.clients.TryAdd(client, socket);
            
            NotifyPeers(room, client.guid);
            GetPeers(room, client.guid);
        }
        else
        {
            Room newRoom = new Room(clientIp);
            Console.WriteLine("Room name: " + newRoom.name);
            
            client = new Client(Guid.NewGuid().ToString(), DateTime.Now);
            
            newRoom.clients.TryAdd(client, socket);
            _rooms.Add(newRoom);
            
            Message.Send(socket, JsonSerializer.Serialize(client));
        }
    }
    
    public async Task HandleClientMessages(WebSocket socket, string msgContent)
    {
        WebSocketMessageStream messageStream = null;
        
        Task.Run(async () =>
        {
            messageStream = await socket.ReadMessageAsync(_cts.Token);
        }).ContinueWith(async t =>
        {
            if (t.IsCompletedSuccessfully)
            {
                using (var sr = new StreamReader(messageStream, Encoding.UTF8))
                    await HandleClientMessages(socket, await sr.ReadToEndAsync());
            }
            else if (t.IsFaulted)
            {
                Console.WriteLine("Error reading message: " + t.Exception.InnerException.Message);
            }
        });
        
        if(_rooms.Any(_ => _.clients.Values.Any(c => c.Equals(socket))))
        {
            Room clientRoom = _rooms.Find(_ => _.clients.Any(x => x.Value.Equals(socket)));
            Client client = clientRoom.clients.Keys.First();
            var msg = msgContent.Split(" ");
            switch (msg[0])
            {
                case "ping":
                    client.lastBeat = DateTime.Now;
                    Message.Send(socket, "pong");
                    break;
                case "disconnect":
                    LeaveRoom(clientRoom, client, socket);
                    break;
            }    
        }
    }

    public void LeaveRoom(Room room, Client clientObj, WebSocket socket)
    {
        socket.Close();
        room.clients.TryRemove(clientObj, out WebSocket _);
    }
    
    public void NotifyPeers(Room? room, string guid)
    {
        if (room == null) return;
        
        if (room.clients.Any())
        {
            var originalClient = room.clients.Where(c => c.Key.guid == guid).Select(kv => kv.Key).FirstOrDefault();
            if (originalClient != null)
            {
                foreach (var client in room.clients.Where(c => c.Key.guid != guid))
                {
                    Message.Send(client.Value, String.Format("User with id: {0} is connecting to your peer \n", originalClient.guid));
                }
            }
        }
    }
    
    public void GetPeers(Room? room, string guid)
    {
        if (room == null) return;
        
        if (room.clients.Any())
        {
            var originalClient = room.clients.Where(c => c.Key.guid == guid).Select(kv => kv.Value).FirstOrDefault();
            if (originalClient != null)
            {
                foreach (var client in room.clients.Where(c => c.Key.guid != guid))
                {
                    Message.Send(originalClient, String.Format("User with id: {0} is connected to your peer \n", client.Key.guid));
                }
            }
        }
    }

    public string GetClientIP(WebSocket socket)
    {
        return (socket.RemoteEndpoint as IPEndPoint).ToString();
    }
}
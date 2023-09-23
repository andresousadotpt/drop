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
            Console.WriteLine("Waiting for client to connect");
            try
            {
                WebSocket socket = await _server.AcceptWebSocketAsync(_cts.Token);
                try
                {
                    await HandleEmptyRooms();
                    await HandleClientAsync(socket);
                    WebSocketMessageStream messageStream = null;
                    Task.Run(async () =>
                    {
                        messageStream = await socket.ReadMessageAsync(_cts.Token);
                    }).ContinueWith(async t =>
                    {
                        if (t.IsCompletedSuccessfully)
                        {
                            Console.WriteLine("Is completed");
                            var msgContent = string.Empty;
                            using (var sr = new StreamReader(messageStream, Encoding.UTF8))
                                msgContent = await sr.ReadToEndAsync();
                            await KeepClientAlive(socket);
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

    public async Task HandleEmptyRooms()
    {
        _rooms.RemoveAll(_ => _.clients.IsEmpty);
    }

    public async Task HandleClientAsync(WebSocket socket)
    {
        string clientIp = GetClientIP(socket).Split(":").First();
        
        Console.WriteLine("Handled client connection at " + clientIp);
        Client client;
        
        if (_rooms.Any(x => x.name.Equals(clientIp)))
        {
            var room = _rooms.Find(x => x.name.Equals(clientIp));
            var guid = Guid.NewGuid().ToString();
            
            room?.clients.TryAdd(new Client(guid, DateTime.Now), socket);
            Console.WriteLine("Added client " + guid + " to " + room?.name);
            
            NotifyPeers(room, guid);
            GetPeers(room, guid);
        }
        else
        {
            Room newRoom = new Room(clientIp);
            Console.WriteLine("Room name: " + newRoom.name);
            
            client = new Client(Guid.NewGuid().ToString(), DateTime.Now);
            
            newRoom.clients.TryAdd(client, socket);
            _rooms.Add(newRoom);
            
            // Message.Send(client, String.Format("Your id is: {0} \n", newClient.uuid));
            Message.Send(socket, JsonSerializer.Serialize(client));
            
            Console.WriteLine("Room created with name: " + newRoom.name);
        }
    }
    
    public async Task HandleClientMessages(WebSocket socket, string msgContent)
    {
        if(_rooms.Any(_ => _.clients.Values.Any(c => c.Equals(socket))))
        {
            Room clientRoom = _rooms.Find(_ => _.clients.Any(x => x.Value.Equals(socket)));
            Client client = clientRoom.clients.Keys.First();
            switch (msgContent)
            {
                case "pong":
                    Console.WriteLine("PONG");
                    client.lastBeat = DateTime.Now;
                    break;
                case "disconnect":
                    socket.Close();
                    clientRoom.clients.TryRemove(client, out WebSocket _);
                    break;
            }    
        }
        
    }
    
    public async Task KeepClientAlive(WebSocket socket)
    {
        Console.WriteLine("Keeping client alive");
        int timeout = 10000;
        Room room = _rooms.Find(_ => _.clients.Values.First().Equals(socket));
        Client client = room.clients.Keys.First();
        WebSocket clientSocket = room.clients.Values.First();

        if ((DateTime.Now - client.lastBeat).TotalMilliseconds > (2 * timeout))
        {
            Console.WriteLine("Leaving Room");
            LeaveRoom(room, client, clientSocket);
        }
        
        Message.Send(socket, "ping");
        Console.WriteLine("Sending ping");
    }

    public void LeaveRoom(Room room, Client clientObj, WebSocket socket)
    {
        socket.Close();
        room.clients.TryRemove(clientObj, out WebSocket _);
    }
    
    public void NotifyPeers(Room? room, string guid)
    {
        if (room == null) return;
        
        var clients = room.clients.Where(x => x.Key.uuid != guid);
        if (clients.Any())
        {
            foreach (var client in clients)
            {
                Message.Send(client.Value, String.Format("User with id: {0} connected \n", client.Key.uuid));
                Console.WriteLine("Notified");
            }
        }
        else
        {
            Console.WriteLine("No one to notify");
        }
    }
    
    public void GetPeers(Room? room, string guid)
    {
        if (room == null) return;
        
        var clientDictionary = room.clients.Where(x => x.Key.uuid == guid);
        if (clientDictionary.Any())
        {
            var client = clientDictionary.First();
            foreach (var clientInside in room.clients.Where(x => x.Key.uuid != guid))
            {
                Message.Send(client.Value, String.Format("User with id: {0} is connected to your peer \n", clientInside.Key.uuid));
            }
        }
        else
        {
            Console.WriteLine("No clients connected to room");
        }
    }

    public string GetClientIP(WebSocket socket)
    {
        
        return (socket.RemoteEndpoint as IPEndPoint).ToString();
    }
}
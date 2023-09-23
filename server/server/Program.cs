using server.service;

RoomService roomService = new RoomService(8080);
await roomService.StartAsync();
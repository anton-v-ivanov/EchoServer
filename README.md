# Simple echo server
Echo server contains rooms.
Each room has a string identifier.

When connecting to a server, the client sends ClientId and RoomId.
If the room with this id doesn't exist on the server, it will be created.
After successfull connection the client starts to send message every 100ms.
Echo-messages are sent to all the clients in the room.

If the room in not receiving messages for 1 minutes, this room will be destroyed.

Server and client use TCP protocol and raw sockets.

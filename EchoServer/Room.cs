using System;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace EchoServer
{
	internal class Room
	{
		public string Id { get; }
		public ConcurrentDictionary<string, Socket> Connections { get; }
		public DateTime LastMessage { get; set; }

		public Room(string id)
		{
			Id = id;
			Connections = new ConcurrentDictionary<string, Socket>();
			LastMessage = DateTime.Now;
		}

		public void AddConnection(string clientId, Socket socket)
		{
			Connections[clientId] = socket;
		}
	}
}

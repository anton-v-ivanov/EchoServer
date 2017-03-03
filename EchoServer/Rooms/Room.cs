using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;

namespace EchoServer.Rooms
{
	internal class Room
	{
		public string Id { get; }
		public ConcurrentDictionary<string, Lazy<Socket>> Connections { get; }
		public DateTime LastMessage { get; set; }

		public Room(string id)
		{
			Id = id;
			Connections = new ConcurrentDictionary<string, Lazy<Socket>>();
			LastMessage = DateTime.Now;
		}

		public void AddConnection(string clientId, Socket socket)
		{
			// ConcurrentDictionary is not guaranteed to invoke add or update method once, so we use lazy behavior
			Connections.AddOrUpdate(clientId, 
				new Lazy<Socket>(() => socket, LazyThreadSafetyMode.ExecutionAndPublication), 
				(key, oldValue) => new Lazy<Socket>(() => socket, LazyThreadSafetyMode.ExecutionAndPublication));
		}
	}
}

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using EchoServer.Messages;

namespace EchoServer.Rooms
{
	public class RoomsPool: IDisposable, IRoomsPool
	{
		private readonly ConcurrentDictionary<string, Lazy<Room>> _rooms;
		private static Timer _timer;
		private static readonly object SyncObj = new object();

		#region Events
		public event Action<string> OnRoomCreated;
		public event Action<string> OnRoomDestroyed;
		public event Action<string> OnClientDisconnected;
		#endregion

		public RoomsPool()
		{
			_rooms = new ConcurrentDictionary<string, Lazy<Room>>();
			_timer = new Timer(DestroyUnusedRooms, null, 0, 1000);
		}

		public void Add(Message message, Socket socket)
		{
			var room = _rooms.GetOrAdd(message.RoomId, _ => new Lazy<Room>(() =>
			{
				var r = new Room(message.RoomId);
				r.AddConnection(message.ClientId, socket);
				FireRoomCreated(message.RoomId);
				return r;
			}, LazyThreadSafetyMode.ExecutionAndPublication));

			room.Value.AddConnection(message.ClientId, socket);
		}

		public void Remove(string clientId)
		{
			var removed = 0;
			foreach (var roomInfo in _rooms)
			{
				Socket s;
				if (roomInfo.Value.Value.Connections.TryRemove(clientId, out s))
					removed++;
			}

			if(removed > 0)
				FireClientDisconnected(clientId);
		}

		public void SendMessage(Message message)
		{
			Lazy<Room> room;
			if (!_rooms.TryGetValue(message.RoomId, out room))
			{
				return;
			}

			room.Value.LastMessage = DateTime.Now;

			foreach (var roomConnection in room.Value.Connections)
			{
				var socket = roomConnection.Value;
				var buffer = Encoding.UTF8.GetBytes(message.ToString());
				var arg = new SocketAsyncEventArgs();
				arg.SetBuffer(buffer, 0, buffer.Length);
				arg.Completed += SendCallback;

				var completed = false;
				try
				{
					completed = socket.SendAsync(arg);
				}
				catch (SocketException)
				{
				}
				if (!completed)
				{
					SendCallback(EventArgs.Empty, arg);
				}
			}
		}

		public void Clear()
		{
			foreach (var roomInfo in _rooms)
			{
				DestroyRoom(roomInfo.Key);
			}
		}

		private void DestroyUnusedRooms(object state)
		{
			var toRemove = (from roomInfo in _rooms
				where (DateTime.Now - roomInfo.Value.Value.LastMessage).TotalSeconds >= 60
				select roomInfo.Key).ToList();

			if (toRemove.Count == 0)
				return;

			foreach (var roomId in toRemove)
			{
				DestroyRoom(roomId);
			}
		}

		private void DestroyRoom(string roomId)
		{
			var connections = _rooms[roomId].Value.Connections;
			foreach (var socket in connections.Values)
			{
				try
				{
					socket.Close();
				}
				catch
				{
					// ignored
				}
			}

			Lazy<Room> r;
			_rooms.TryRemove(roomId, out r);
			FireRoomDestroyed(roomId);
		}

		private static void SendCallback(object sender, SocketAsyncEventArgs e)
		{
		}
		
		#region Event invocators
		private void FireRoomCreated(string roomId)
		{
			OnRoomCreated?.Invoke(roomId);
		}

		private void FireRoomDestroyed(string roomId)
		{
			OnRoomDestroyed?.Invoke(roomId);
		}

		private void FireClientDisconnected(string clientId)
		{
			OnClientDisconnected?.Invoke(clientId);
		}
		#endregion

		public void Dispose()
		{
			Clear();
		}
	}
}
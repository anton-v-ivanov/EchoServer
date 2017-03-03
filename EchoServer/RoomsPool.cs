using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace EchoServer
{
	public class RoomsPool: IDisposable
	{
		private readonly ConcurrentDictionary<string, Room> _rooms;
		private readonly Timer _timer;

		#region Events
		public event Action<string> OnRoomCreated;
		public event Action<string> OnRoomDestroyed;
		public event Action<string> OnClientDisconnected;
		#endregion

		public RoomsPool()
		{
			_rooms = new ConcurrentDictionary<string, Room>();
			_timer = new Timer(Cleanup, null, 0, 1000);
		}

		public void Add(string roomId, string clientId, Socket socket)
		{
			Room room;
			if(_rooms.TryGetValue(roomId, out room))
			{
				room.AddConnection(clientId, socket);
			}
			else
			{
				room = new Room(roomId);
				room.AddConnection(clientId, socket);
				_rooms[roomId] = room;
				FireRoomCreated(roomId);
			}
		}

		public void Remove(string clientId)
		{
			var removed = 0;
			foreach (var roomInfo in _rooms)
			{
				Socket s;
				if (roomInfo.Value.Connections.TryRemove(clientId, out s))
					removed++;
			}

			if(removed > 0)
				FireClientDisconnected(clientId);
		}

		private void Cleanup(object state)
		{
			var toRemove = (from roomInfo in _rooms
				where (DateTime.Now - roomInfo.Value.LastMessage).TotalSeconds >= 60
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
			var connections = _rooms[roomId].Connections;
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
			Room r;
			_rooms.TryRemove(roomId, out r);
			FireRoomDestroyed(roomId);
		}

		public void SendMessage(Message message)
		{
			Room room;
			if (!_rooms.TryGetValue(message.RoomId, out room))
			{
				return;
			}

			room.LastMessage = DateTime.Now;
			
			foreach (var roomConnection in room.Connections)
			{
				var socket = roomConnection.Value;
				{
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
		}

		private static void SendCallback(object sender, SocketAsyncEventArgs e)
		{
		}

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

		public void Dispose()
		{
			foreach (var roomInfo in _rooms)
			{
				DestroyRoom(roomInfo.Key);
			}
		}
	}
}
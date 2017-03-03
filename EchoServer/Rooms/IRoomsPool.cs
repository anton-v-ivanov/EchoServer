using System;
using System.Net.Sockets;
using EchoServer.Messages;

namespace EchoServer.Rooms
{
	internal interface IRoomsPool
	{
		/// <summary>
		/// Create new room
		/// </summary>
		/// <param name="message">Message</param>
		/// <param name="socket">Socket for connection</param>
		void Add(Message message, Socket socket);

		/// <summary>
		/// Remove client from all rooms
		/// </summary>
		/// <param name="clientId">Client Id</param>
		void Remove(string clientId);

		/// <summary>
		/// Send message to all clients in room
		/// </summary>
		/// <param name="message">Message</param>
		void SendMessage(Message message);

		/// <summary>
		/// Close all rooms and connections in it
		/// </summary>
		void Clear();

		#region Events
		event Action<string> OnRoomCreated;
		event Action<string> OnRoomDestroyed;
		event Action<string> OnClientDisconnected;
		#endregion
	}
}
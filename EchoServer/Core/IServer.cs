using System;

namespace EchoServer.Core
{
	/// <summary>
	/// Server interface
	/// </summary>
	public interface IServer
	{
		/// <summary>
		/// Run server
		/// </summary>
		/// <param name="serverName">Server name</param>
		/// <param name="port">Server port</param>
		void Run(string serverName, ushort port);

		/// <summary>
		/// Stop server and release resources
		/// </summary>
		void Stop();

		#region Events
		event Action<string, string> OnClientConnected;
		event Action<string, string, string> OnMessageReceived;
		event Action<Exception> OnError;
		event Action<string> OnRoomCreated;
		event Action<string> OnRoomDestroyed;
		event Action<string> OnClientDisconnected;
		#endregion
	}
}
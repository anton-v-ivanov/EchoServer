using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using EchoServer.Messages;
using EchoServer.Rooms;

namespace EchoServer.Core
{
	public sealed class SocketServer : IDisposable, IServer
	{
		private const int MaxConnections = 100;
		private readonly IMessageParser _messageParser;
		private readonly IRoomsPool _rooms;
		private Socket _serverSocket;

		#region Events
		public event Action<string, string> OnClientConnected;
		public event Action<string, string, string> OnMessageReceived;
		public event Action<Exception> OnError;
		public event Action<string> OnRoomCreated;
		public event Action<string> OnRoomDestroyed;
		public event Action<string> OnClientDisconnected;
		#endregion

		public SocketServer()
		{
			_messageParser = new MessageParser();

			_rooms = new RoomsPool();
			_rooms.OnRoomCreated += FireRoomCreated;
			_rooms.OnRoomDestroyed += FireRoomDestroyed;
			_rooms.OnClientDisconnected += FireClientDisconnected;
		}

		public void Run(string serverName, ushort port)
		{
			var hostInfo = Dns.GetHostEntry(serverName);
			var serverAddr = hostInfo.AddressList.FirstOrDefault(t => t.AddressFamily == AddressFamily.InterNetwork);
			if (serverAddr == null)
				throw new Exception("No IPv4 address for server");

			EndPoint endpoint = new IPEndPoint(serverAddr, port);

			_serverSocket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			_serverSocket.Bind(endpoint);
			_serverSocket.Listen(MaxConnections);
			_serverSocket.BeginAccept(ClientConnected, _serverSocket);
		}

		public void Stop()
		{
			_rooms.Clear();
			_serverSocket.Close();
		}

		private void ClientConnected(IAsyncResult ar)
		{
			var connection = new SocketConnectionInfo();
			try
			{
				connection.Socket = ((Socket)ar.AsyncState).EndAccept(ar);
				connection.Socket.BeginReceive(connection.Buffer, 0, connection.Buffer.Length, SocketFlags.None, DataReceived, connection);
				_serverSocket.BeginAccept(ClientConnected, _serverSocket);
			}
			catch (ObjectDisposedException)
			{
			}
			catch (Exception exc)
			{
				FireOnError(exc);
				connection.Socket.BeginReceive(connection.Buffer, 0, connection.Buffer.Length, SocketFlags.None, DataReceived, connection);
				_serverSocket.BeginAccept(ClientConnected, _serverSocket);
			}
		}

		private void DataReceived(IAsyncResult ar)
		{
			var connection = (SocketConnectionInfo)ar.AsyncState;
			var socket = ((SocketConnectionInfo) ar.AsyncState).Socket;

			try
			{
				var bytesRead = connection.Socket.EndReceive(ar);
				connection.BytesRead += bytesRead;

				if (!IsSocketConnected(connection.Socket))
				{
					// client disconnected but we have some data in buffer to read
					if (connection.BytesRead <= 0)
						return;

					// there is a chance that we have more than one packet in buffer
					var messages = _messageParser.Parse(connection.Buffer);
					foreach (var message in messages)
					{
						ProcessMessage(message, socket);
					}
					return;
				}

				if (bytesRead == 0 || (bytesRead > 0 && bytesRead < SocketConnectionInfo.BufferSize))
				{
					var buffer = new byte[connection.Buffer.Length];
					connection.Buffer.CopyTo(buffer, 0);

					// there is a chance that we have more than one packet in buffer
					var messages = _messageParser.Parse(buffer).ToList();
					foreach (var message in messages)
					{
						ProcessMessage(message, socket);
					}

					if (connection.ClientId == null)
					{
						// associate connection to client id
						var msg = messages.FirstOrDefault(m => m != null);
						if (msg != null)
							connection.ClientId = msg.ClientId;
					}

					// clear buffer for next packet
					Array.Clear(connection.Buffer, 0, connection.Buffer.Length);
					connection.BytesRead = 0;

					connection.Socket.BeginReceive(connection.Buffer, 0, connection.Buffer.Length, SocketFlags.None, DataReceived,
						connection);
				}
				else
				{
					// resize array if we have a large amount of data to receive
					Array.Resize(ref connection.Buffer, connection.Buffer.Length + SocketConnectionInfo.BufferSize);
					connection.Socket.BeginReceive(connection.Buffer, 0, connection.Buffer.Length, SocketFlags.None, DataReceived,
						connection);
				}
			}
			catch (SocketException)
			{
				// client disconnected
				if(connection.ClientId != null)
					_rooms.Remove(connection.ClientId);
				
				// client disconnected but we might have some data in buffer to read
				if (connection.BytesRead > 0)
				{
					var messages = _messageParser.Parse(connection.Buffer);
					foreach (var message in messages)
					{
						ProcessMessage(message, socket);
					}
				}
			}
			catch (ObjectDisposedException)
			{
				// ignore the situation when socket was disposed while trying to read from it
			}
			catch (Exception ex)
			{
				FireOnError(ex);
			}
			finally
			{
				if (IsSocketConnected(socket))
					socket.BeginReceive(connection.Buffer, 0, connection.Buffer.Length, SocketFlags.None, DataReceived, connection);
			}
		}

		private void ProcessMessage(Message message, Socket socket)
		{
			if (message == null)
				return;

			switch (message.OperationType)
			{
				case OperationType.Message:
					FireMessageReceived(message.RoomId, message.ClientId, message.Text);
					_rooms.SendMessage(message);
					break;
				case OperationType.Connect:
					_rooms.Add(message, socket);
					FireClientConnected(message.RoomId, message.ClientId);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private static bool IsSocketConnected(Socket socket)
		{
			try
			{
				return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
			}
			catch
			{
				return false;
			}
		}

		public void Dispose()
		{
			Stop();
		}
		
		#region events invocators
		private void FireClientConnected(string roomId, string clientId)
		{
			OnClientConnected?.Invoke(roomId, clientId);
		}

		private void FireMessageReceived(string roomId, string clientId, string text)
		{
			OnMessageReceived?.Invoke(roomId, clientId, text);
		}

		private void FireOnError(Exception exc)
		{
			OnError?.Invoke(exc);
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
		#endregion
	}
}
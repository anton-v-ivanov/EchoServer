using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using EchoServer;
using EchoServer.Messages;

namespace EchoClient
{
	class Program
	{
		private const int DefaultPort = 38254;
		private const string DefaultServerName = "localhost";
		private static string _roomId;
		private static string _clientId;
		private static ushort _port;
		private static string _serverName;
		private static Timer _timer;
		private static Socket _socket;
		private static IPEndPoint _endpoint;
		private static bool _isReconnecting;
		private static IMessageParser _messageParser;

		static void Main(string[] args)
		{
			_port = DefaultPort;
			_serverName = DefaultServerName;

			if (args.Length > 1)
			{
				_serverName = args[0];

				if (!ushort.TryParse(args[1], out _port) || _port == 0)
				{
					Console.WriteLine("Port must be a number between 1 and 65535");
					Console.Read();
					return;
				}
			}

			Console.WriteLine("Enter room id");
			_roomId = Console.ReadLine();
			if (string.IsNullOrWhiteSpace(_roomId))
			{
				Console.WriteLine("Room id can't be empty");
				Console.Read();
				return;
			}

			_messageParser = new MessageParser();

			var random = new Random();
			_clientId = random.Next().ToString();

			SetupSocket();

			Connect(_roomId, _clientId);

			_timer = new Timer(e =>
			{
				var textMessage = new Message(OperationType.Message, _roomId, _clientId, "Text " + _clientId);
				if (!SendMessage(textMessage))
				{
					Connect(_roomId, _clientId);
				}
			}, null, 0, 100);

			Console.WriteLine("Press any key to exit");
			Console.Read();
		}

		private static void DataReceived(IAsyncResult ar)
		{
			var connection = (SocketConnectionInfo)ar.AsyncState;

			try
			{
				var bytesRead = connection.Socket.EndReceive(ar);
				connection.BytesRead += bytesRead;

				if (!IsSocketConnected(connection.Socket))
				{
					// client disconnected but we have some data in buffer to read
					if (connection.BytesRead > 0)
					{
						Process(connection.Buffer);
					}
				}


				if (bytesRead == 0 || (bytesRead > 0 && bytesRead < SocketConnectionInfo.BufferSize))
				{
					var buffer = connection.Buffer;

					if (bytesRead < buffer.Length)
					{
						Array.Resize(ref buffer, bytesRead);
					}

					Process(buffer);

					connection = new SocketConnectionInfo
					{
						Socket = ((SocketConnectionInfo)ar.AsyncState).Socket
					};
					connection.Socket.BeginReceive(connection.Buffer, 0, connection.Buffer.Length, SocketFlags.None, DataReceived,
						connection);
				}
				else
				{
					Array.Resize(ref connection.Buffer, connection.Buffer.Length + SocketConnectionInfo.BufferSize);
					connection.Socket.BeginReceive(connection.Buffer, 0, connection.Buffer.Length, SocketFlags.None, DataReceived, connection);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				if (IsSocketConnected(connection.Socket))
					connection.Socket.BeginReceive(connection.Buffer, 0, connection.Buffer.Length, SocketFlags.None, DataReceived, connection);
				Connect(_roomId, _clientId);
			}
		}

		private static void Process(byte[] buffer)
		{
			var messages = _messageParser.Parse(buffer);
			foreach (var message in messages)
			{
				if (message != null)
					Console.WriteLine($"{DateTime.Now.ToLongTimeString()} RoomId: {message.RoomId}, ClientId: {message.ClientId}, Text = {message.Text}");
			}
		}

		private static bool IsSocketConnected(Socket socket)
		{
			return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
		}

		private static void Connect(string roomId, string clientId)
		{
			if (_socket == null || !_socket.Connected)
			{
				SetupSocket();
			}

			// Sending connect message
			var connectMessage = new Message(OperationType.Connect, roomId, clientId, "Connect " + clientId);
			SendMessage(connectMessage);
		}

		private static bool SendMessage(Message message)
		{
			var str = message.ToString();
			var buffer = Encoding.UTF8.GetBytes(str);
			var arg = new SocketAsyncEventArgs();
			arg.SetBuffer(buffer, 0, buffer.Length);
			arg.Completed += SendCallback;
			bool completed;
			try
			{
				completed = _socket.SendAsync(arg);
			}
			catch (SocketException)
			{
				Console.WriteLine("Unable to send message");
				return false;
			}
			if (!completed)
			{
				SendCallback(EventArgs.Empty, arg);
				return false;
			}
			return true;
		}

		private static void SendCallback(object sender, SocketAsyncEventArgs e)
		{
		}

		private static void SetupSocket()
		{
			if (_isReconnecting)
				return;

			_isReconnecting = true;
			var hostInfo = Dns.GetHostEntry(_serverName);
			var serverAddr = hostInfo.AddressList.FirstOrDefault(t => t.AddressFamily == AddressFamily.InterNetwork);
			if (serverAddr == null)
				throw new Exception("No IPv4 address for server");

			_endpoint = new IPEndPoint(serverAddr, _port);
			_socket = new Socket(_endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			ConnectSocket();
			_isReconnecting = false;
		}

		private static void ConnectSocket()
		{
			while (true)
			{
				try
				{
					_socket.Connect(_endpoint);

					var connection = new SocketConnectionInfo { Socket = _socket };
					_socket.BeginReceive(connection.Buffer, 0, connection.Buffer.Length, SocketFlags.None, DataReceived, connection);
					break;
				}
				catch (Exception)
				{
					Console.WriteLine("Unable to connect");
					Thread.Sleep(500);
				}
			}
		}
	}
}

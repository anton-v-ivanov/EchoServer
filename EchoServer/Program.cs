using System;
using EchoServer.Core;

namespace EchoServer
{
	class Program
	{
		private const int DefaultPort = 38254;
		private const string DefaultServerName = "localhost";

		static void Main(string[] args)
		{
			ushort port = DefaultPort;
			var serverName = DefaultServerName;

			if (args.Length > 1)
			{
				serverName = args[0];

				if (!ushort.TryParse(args[1], out port) || port == 0)
				{
					Console.WriteLine("Port must be a number between 1 and 65535");
					Console.Read();
					return;
				}
			}
			
			IServer server = new SocketServer();
			server.OnClientConnected += (roomId, clientId) => Console.WriteLine($"{DateTime.Now.ToLongTimeString()} Client connected. RoomId = {roomId}, ClientId = {clientId}");
			server.OnClientDisconnected += c => Console.WriteLine($"{DateTime.Now.ToLongTimeString()} Client disconnected: {c}");
			server.OnError += exc => Console.WriteLine($"{DateTime.Now.ToLongTimeString()} Error: {exc.Message}");
			server.OnRoomCreated += r => Console.WriteLine($"{DateTime.Now.ToLongTimeString()} Room created: {r}");
			server.OnRoomDestroyed += r => Console.WriteLine($"{DateTime.Now.ToLongTimeString()} Room destroyed: {r}");
			//server.OnMessageReceived += (roomId, clientId, text) => Console.WriteLine($"{DateTime.Now.ToLongTimeString()} Message received. RoomId = {roomId}, ClientId = {clientId}, Text = {text}");

			try
			{
				server.Run(serverName, port);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}

			Console.WriteLine($"Server started on {serverName}:{port}");

			

			Console.WriteLine("Press any key to exit");
			Console.Read();
			server.Stop();
		}
	}
}

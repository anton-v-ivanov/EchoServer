using System.Net.Sockets;

namespace EchoServer
{
	public class SocketConnectionInfo
	{
		public const int BufferSize = 1024 * 10; //10 mb
		public Socket Socket { get; set; }
		public byte[] Buffer;
		public int BytesRead { get; set; }
		public string ClientId { get; set; }

		public SocketConnectionInfo()
		{
			Buffer = new byte[BufferSize];
		}
	}
}
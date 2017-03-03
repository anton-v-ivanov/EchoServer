using System.Collections.Generic;

namespace EchoServer.Messages
{
	public interface IMessageParser
	{
		IEnumerable<Message> Parse(byte[] buffer);
		IEnumerable<Message> Parse(string message);
	}
}

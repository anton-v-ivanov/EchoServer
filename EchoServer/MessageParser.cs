using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace EchoServer
{
	public class MessageParser
	{
		public static IEnumerable<Message> Parse(byte[] buffer)
		{
			var bufferStr = Encoding.UTF8.GetString(buffer);
			return Parse(bufferStr);
		}

		public static IEnumerable<Message> Parse(string message)
		{
			var lines = message.Split(new[] {"\r\n", "\n"}, StringSplitOptions.RemoveEmptyEntries);
			var result = new List<Message>();
			foreach (var line in lines)
			{
				try
				{
					result.Add(JsonConvert.DeserializeObject<Message>(line));
				}
				catch
				{
					// ignored
				}
			}
			return result;
		}
	}
}
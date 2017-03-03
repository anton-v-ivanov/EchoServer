using System;
using Newtonsoft.Json;

namespace EchoServer
{
	public enum OperationType
	{
		Message = 0,
		Connect = 1	
	}

	public class Message
	{
		public OperationType OperationType { get; }
		public string RoomId { get; }
		public string ClientId { get; }
		public string Text { get; }

		public Message(OperationType operationType, string roomId, string clientId, string text)
		{
			OperationType = operationType;
			RoomId = roomId;
			ClientId = clientId;
			Text = text;
		}

		public override string ToString()
		{
			return JsonConvert.SerializeObject(this) + Environment.NewLine;
		}
	}
}
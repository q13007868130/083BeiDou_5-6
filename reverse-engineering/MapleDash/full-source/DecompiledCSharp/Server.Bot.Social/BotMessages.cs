using System;
using System.Collections.Generic;

namespace Server.Bot.Social;

public sealed class BotMessages
{
	private readonly Dictionary<QueueType, Queue<ChatMessage>> queues = new Dictionary<QueueType, Queue<ChatMessage>>
	{
		[QueueType.PRIMARY] = new Queue<ChatMessage>(),
		[QueueType.SECONDARY] = new Queue<ChatMessage>(),
		[QueueType.TERTIARY] = new Queue<ChatMessage>()
	};

	public static QueueType FromString(string name)
	{
		return name switch
		{
			"primary" => QueueType.PRIMARY, 
			"secondary" => QueueType.SECONDARY, 
			"tertiary" => QueueType.TERTIARY, 
			_ => throw new ArgumentException("Queue not found: " + name), 
		};
	}

	public void Add(ChatMessage message, QueueType type = QueueType.SECONDARY)
	{
		if (message == null)
		{
			throw new ArgumentNullException("message");
		}
		queues[type].Enqueue(message);
	}

	public ChatMessage Poll(QueueType type = QueueType.SECONDARY)
	{
		if (queues[type].Count != 0)
		{
			return queues[type].Dequeue();
		}
		return null;
	}

	public ChatMessage Peek(QueueType type = QueueType.PRIMARY)
	{
		if (queues[type].Count != 0)
		{
			return queues[type].Peek();
		}
		return null;
	}

	public bool RemoveIf(bool condition, QueueType type = QueueType.PRIMARY)
	{
		ChatMessage chatMessage = Poll(type);
		if (chatMessage == null)
		{
			return false;
		}
		if (condition)
		{
			return true;
		}
		Add(chatMessage, type);
		return false;
	}

	public int Count(QueueType type)
	{
		return queues[type].Count;
	}

	public void Clear()
	{
		foreach (Queue<ChatMessage> value in queues.Values)
		{
			value.Clear();
		}
	}

	public void Clean(long now, long expirationMs = 10000L)
	{
		foreach (Queue<ChatMessage> value in queues.Values)
		{
			int count = value.Count;
			for (int i = 0; i < count; i++)
			{
				ChatMessage chatMessage = value.Dequeue();
				if (now - chatMessage.timestamp <= expirationMs)
				{
					value.Enqueue(chatMessage);
				}
			}
		}
	}
}

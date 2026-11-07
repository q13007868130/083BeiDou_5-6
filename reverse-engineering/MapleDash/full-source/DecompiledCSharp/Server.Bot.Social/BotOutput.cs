using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Bot.Social;

public sealed class BotOutput
{
	public enum Kind
	{
		Chat,
		Bubble,
		Emote,
		Hint,
		EnableActions,
		Chair,
		Skill,
		Chalkboard,
		Guide,
		GuideTalk,
		YellowMessage,
		Movement,
		Appearance,
		Attack,
		Mount,
		TradeChat,
		Scroll,
		ItemMegaphone,
		Whisper
	}

	public sealed class Message
	{
		public Kind kind;

		public int actorId;

		public int mapId;

		public int targetId;

		public int value;

		public int width;

		public int duration;

		public string text;

		public float x;

		public float y;

		public InventoryEntry item;
	}

	private readonly Queue<Message> messages = new Queue<Message>();

	public int Count => messages.Count;

	public event Action<Message> Published;

	public void Send(Kind kind, BotActor actor, string text = null, int value = 0, int targetId = 0, int width = 0, int duration = 0, InventoryEntry item = null)
	{
		Message message = new Message
		{
			kind = kind,
			actorId = actor.id,
			mapId = actor.mapId,
			text = text,
			value = value,
			targetId = targetId,
			width = width,
			duration = duration,
			x = actor.x,
			y = actor.y,
			item = item?.Copy()
		};
		messages.Enqueue(message);
		Published?.Invoke(message);
	}

	public void IdleStanding(BotCharacter bot)
	{
		Send(Kind.Movement, bot, null, (bot.stance % 2 == 0) ? 4 : 5, 0, 0, 200);
	}

	public Message Poll()
	{
		if (messages.Count != 0)
		{
			return messages.Dequeue();
		}
		return null;
	}

	public void Clear()
	{
		messages.Clear();
	}

	public void Speak(BotCharacter bot, string text)
	{
		Send(Kind.Chat, bot, text);
	}

	public void Emote(BotCharacter bot, int emote)
	{
		Send(Kind.Emote, bot, null, emote);
	}

	public void ShowOptions(BotActor player, IList<string> options)
	{
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		for (int i = 0; i < options.Count; i++)
		{
			num = Math.Max(num, options[i].Length);
			stringBuilder.Append(i + 1).Append(". ").Append(options[i])
				.Append("\r\n");
		}
		Send(Kind.Hint, player, stringBuilder.ToString(), 0, player.id, (num < 5) ? 60 : (num * 10), 25);
		Send(Kind.EnableActions, player, null, 0, player.id);
	}

	public void ExpireOptions(BotActor player)
	{
		Send(Kind.Hint, player, ".", 0, player.id, 40);
		Send(Kind.EnableActions, player, null, 0, player.id);
	}

	public void Sit(BotCharacter bot, int chair)
	{
		bot.chair = chair;
		Send(Kind.Chair, bot, null, chair);
	}

	public void CancelChair(BotCharacter bot)
	{
		bot.chair = 0;
		Send(Kind.Chair, bot);
	}
}

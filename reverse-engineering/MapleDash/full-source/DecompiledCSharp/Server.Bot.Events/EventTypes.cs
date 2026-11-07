using System;

namespace Server.Bot.Events;

public static class EventTypes
{
	public static string Description(this EventType type)
	{
		return type switch
		{
			EventType.GACHAPON_REWARD => "Gachapon Reward", 
			EventType.LEVEL_UP => "Level Up", 
			EventType.SCROLLING => "Scrolling Result", 
			EventType.ITEM_MEGAPHONE => "Item Megaphone Announcement", 
			EventType.CHAT_MEGAPHONE => "Chat Megaphone", 
			EventType.MASTERY_RESULT => "Mastery Result", 
			EventType.EMOTE => "Emote", 
			EventType.CHAT_GENERAL => "General Chat", 
			EventType.MAP_ENTERED => "Player Entered Map", 
			_ => throw new ArgumentOutOfRangeException("type"), 
		};
	}
}

using System;
using Server.Bot;
using UnityEngine;

public static class PqBotNpcDriver
{
	private const int MaxSteps = 24;

	public static bool Talk(BotCharacter bot, int npcId, out string say)
	{
		say = null;
		if (bot == null || npcId == 0 || !GameSession.InGame)
		{
			return false;
		}
		bool silent = NpcSession.Silent;
		NpcSession.Silent = true;
		NpcSession npcSession = null;
		try
		{
			npcSession = NpcSession.TalkAs(npcId, bot);
			if (npcSession == null)
			{
				return false;
			}
			int num = 0;
			while (!npcSession.Disposed && !npcSession.Closed && num++ < 24 && npcSession.Advance())
			{
			}
			say = npcSession.LastSay;
			if (!string.IsNullOrEmpty(npcSession.LastError))
			{
				Debug.LogWarning((object)("[BotNpc] " + bot.name + " 与 NPC " + npcId + " 对话出错：" + npcSession.LastError));
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			Debug.LogWarning((object)("[BotNpc] " + bot.name + " 与 NPC " + npcId + " 对话异常：" + ex.Message));
			return false;
		}
		finally
		{
			try
			{
				npcSession?.Close();
			}
			catch (Exception)
			{
			}
			NpcSession.Silent = silent;
		}
	}
}

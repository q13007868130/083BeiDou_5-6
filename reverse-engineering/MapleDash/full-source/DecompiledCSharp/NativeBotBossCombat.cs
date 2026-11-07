using System;
using Server.Bot;
using UnityEngine;

public static class NativeBotBossCombat
{
	public const int FmEntrance = 910000000;

	private const long InitialDelayMs = 800L;

	private const long AttackIntervalMs = 1200L;

	private static readonly string[] TriggerPhrases = new string[5] { "假人打boss", "假人攻击", "攻击boss", "打boss", "botattack" };

	private static readonly string[] StartLines = new string[3] { "来了，集火Boss！", "开打开打，注意躲技能！", "老板出现了，兄弟们上！" };

	private static EnemyUnit boss;

	private static long nextTick;

	public static bool HandleChatTrigger(int mapId, string message)
	{
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		if (string.IsNullOrEmpty(message) || mapId != 910000000)
		{
			return false;
		}
		string text = message.Trim().Replace(" ", "").ToLowerInvariant();
		bool flag = false;
		string[] triggerPhrases = TriggerPhrases;
		foreach (string text2 in triggerPhrases)
		{
			if (text == text2)
			{
				flag = true;
			}
		}
		if (!flag)
		{
			return false;
		}
		FieldDirector fieldDirector = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.field : null);
		if ((Object)(object)fieldDirector == (Object)null || fieldDirector.Map == null || fieldDirector.Map.id != mapId)
		{
			return false;
		}
		PlayerCtrl player = GameLoop.I.player;
		float playerX = (((Object)(object)player != (Object)null) ? (((Component)player).transform.position.x * 40f) : 0f);
		EnemyUnit enemyUnit = fieldDirector.NearestAliveBoss(playerX);
		if ((Object)(object)enemyUnit == (Object)null)
		{
			NativeSystemMessage.Drop(12, "当前市场没有可攻击的Boss。");
			return true;
		}
		if (BotRegistry.Count() == 0)
		{
			NativeSystemMessage.Drop(12, "当前市场没有可用假人。");
			return true;
		}
		boss = enemyUnit;
		nextTick = NativeMobController.Now + 800;
		NativeSystemMessage.Drop(12, StartLines[Random.Range(0, StartLines.Length)]);
		NativeSystemMessage.Drop(12, "假人已开始集火Boss。");
		return true;
	}

	public static void Tick(long now)
	{
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)boss == (Object)null))
		{
			if ((Object)(object)boss == (Object)null || boss.dead || !((Behaviour)boss).isActiveAndEnabled || (Object)(object)GameLoop.I == (Object)null || (Object)(object)GameLoop.I.field == (Object)null)
			{
				boss = null;
			}
			else if (now >= nextTick)
			{
				nextTick = now + 1200;
				long num = (((Object)(object)boss.NativeController != (Object)null) ? boss.NativeController.MaximumHp : boss.hpMax);
				long damage = Math.Clamp(Math.Max(50000L, num / 180), 50000L, 2000000L);
				PlayerCtrl player = GameLoop.I.player;
				int fromDir = ((!((Object)(object)player != (Object)null) || !(((Component)boss).transform.position.x < ((Component)player).transform.position.x)) ? 1 : (-1));
				boss.TakeBotHit(GameSession.CharacterId, damage, fromDir);
			}
		}
	}

	public static void Stop()
	{
		boss = null;
	}
}

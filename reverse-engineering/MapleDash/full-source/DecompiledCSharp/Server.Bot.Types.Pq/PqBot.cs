using System;
using System.Collections.Generic;
using Server.Bot.Environment;
using Server.Bot.Social;
using Server.Field;

namespace Server.Bot.Types.Pq;

public sealed class PqBot : BotStateMachine
{
	private enum Mode
	{
		Lobby,
		Run
	}

	private readonly Random random = new Random();

	private Mode mode;

	private int recruitMap;

	private string pqKey;

	private long nextIdleAt;

	private long nextEmoteAt;

	private long nextSkillAt;

	private long nextDropAt;

	private long nextChatAt;

	private bool wanderStarted;

	private long lastMapCheck;

	private static readonly int[] Emotes = new int[6] { 2, 3, 4, 9, 10, 11 };

	private static readonly int[] LobbyDrops = new int[4] { 2000000, 2000001, 4020000, 4000004 };

	private int[] ambientPool;

	private static readonly Dictionary<int, bool> questFlags = new Dictionary<int, bool>();

	public override bool SkipsSpawnChoreography => true;

	public PqBot(BotCharacter chr)
		: base(chr)
	{
		botType = "PQBot";
		PqDef pqDef = PqCatalog.ForMap(chr.mapId);
		if (pqDef != null)
		{
			recruitMap = pqDef.recruitMap;
			pqKey = pqDef.key;
		}
		long num = BotScheduler.Now();
		nextIdleAt = num + random.Next(1500);
		nextChatAt = num + 2000 + random.Next(6000);
		nextEmoteAt = num + 3000 + random.Next(15000);
		nextSkillAt = num + 5000 + random.Next(20000);
		nextDropAt = num + 8000 + random.Next(30000);
	}

	public override bool IsAvailableForAmbientActions()
	{
		return mode == Mode.Lobby;
	}

	protected override long DefaultPeriodMs()
	{
		return 700L;
	}

	public override void CheckPrioritySpeed()
	{
		if (!CheckMainPlayersOnMap())
		{
			SetPriorityLow();
		}
		else
		{
			BotScheduler.Reschedule(chr.id, DefaultPeriodMs());
		}
	}

	public override void UpdateState()
	{
		base.UpdateState();
		if (CheckIfNotRunningOrPaused() || chr == null)
		{
			return;
		}
		long num = BotScheduler.Now();
		PqRun pqRun = PqDirector.RunOfBot(chr.id);
		if (pqRun != null)
		{
			mode = Mode.Run;
			if (wanderStarted)
			{
				BotWander.Stop(chr);
				wanderStarted = false;
			}
			pqRun.DriveBot(chr, num);
			return;
		}
		if (mode == Mode.Run)
		{
			mode = Mode.Lobby;
			BotMovementService.Stop(chr);
		}
		if (recruitMap == 0)
		{
			PqDef pqDef = PqCatalog.ForMap(chr.mapId);
			if (pqDef == null)
			{
				return;
			}
			recruitMap = pqDef.recruitMap;
			pqKey = pqDef.key;
		}
		if (chr.mapId != recruitMap)
		{
			if (num - lastMapCheck > 2000)
			{
				lastMapCheck = num;
				PqDirector.WarpBot(chr, recruitMap);
			}
		}
		else
		{
			Lobby(num);
		}
	}

	private void Lobby(long now)
	{
		BotActor botActor = PqDirector.LocalPlayer();
		bool flag = BotMovementService.Observed(chr.mapId);
		if (botActor == null || botActor.mapId != chr.mapId)
		{
			if (wanderStarted)
			{
				BotWander.Stop(chr);
				wanderStarted = false;
			}
			return;
		}
		if (flag && !wanderStarted)
		{
			MapRuntime mapRuntime = MapService.Get(chr.mapId);
			if (mapRuntime != null && mapRuntime.footholds.Count > 0)
			{
				int num = Math.Max((int)mapRuntime.minX + 40, (int)chr.x - 220);
				int num2 = Math.Min((int)mapRuntime.maxX - 40, (int)chr.x + 220);
				if (!BotMovementService.PrepareNavigation(chr))
				{
					return;
				}
				if (num2 - num > 80)
				{
					BotWander.Start(chr, banded: true, num, num2);
					wanderStarted = true;
				}
				else
				{
					BotWander.Start(chr, banded: false, 0f, 0f);
					wanderStarted = true;
				}
			}
		}
		if (now >= nextChatAt)
		{
			nextChatAt = now + 7000 + random.Next(9000);
			BotPartyService.Party party = BotTypeManager.Parties?.For(chr.id);
			output.Speak(chr, (party != null) ? PqChatLines.Role(pqKey, random) : PqChatLines.Recruit(pqKey, random));
		}
		if (now >= nextEmoteAt)
		{
			nextEmoteAt = now + 9000 + random.Next(12000);
			output.Emote(chr, Emotes[random.Next(Emotes.Length)]);
		}
		if (now >= nextSkillAt)
		{
			nextSkillAt = now + 12000 + random.Next(16000);
			CastFlourish();
		}
		if (now >= nextDropAt)
		{
			nextDropAt = now + 20000 + random.Next(40000);
			DropSomething();
		}
		if (now >= nextIdleAt)
		{
			nextIdleAt = now + 2500 + random.Next(3000);
			if (!chr.moving)
			{
				output.IdleStanding(chr);
			}
		}
	}

	private void CastFlourish()
	{
		chr.equips.TryGetValue(-11, out var value);
		BotAttack.Skill skill = BotAttack.GrindSkill(chr.job, value);
		if (skill.id > 0)
		{
			output.Send(BotOutput.Kind.Skill, chr, null, skill.id);
		}
		else
		{
			BotAttack.BasicSwing(chr, random);
		}
	}

	private int[] AmbientPool()
	{
		if (ambientPool != null)
		{
			return ambientPool;
		}
		List<int> list = new List<int>(LobbyDrops.Length);
		int[] lobbyDrops = LobbyDrops;
		foreach (int num in lobbyDrops)
		{
			if (!IsQuestItem(num))
			{
				list.Add(num);
			}
		}
		ambientPool = list.ToArray();
		return ambientPool;
	}

	private static bool IsQuestItem(int itemId)
	{
		if (questFlags.TryGetValue(itemId, out var value))
		{
			return value;
		}
		bool flag;
		try
		{
			Dictionary<string, object> dictionary = BotItemCatalog.Shared.Info(itemId);
			flag = dictionary == null || BotItemCatalog.Int(dictionary, "quest") == 1 || BotItemCatalog.Int(dictionary, "pquest") == 1;
		}
		catch (Exception)
		{
			return true;
		}
		questFlags[itemId] = flag;
		return flag;
	}

	private void DropSomething()
	{
		int[] array = AmbientPool();
		if (array.Length == 0)
		{
			return;
		}
		int itemId = array[random.Next(array.Length)];
		if (IsQuestItem(itemId))
		{
			return;
		}
		int num = (int)chr.x + random.Next(-40, 41);
		int throwY = (int)MapService.SnapToGround(chr.mapId, num, chr.y);
		try
		{
			DropCommands.BotThrowItemNoExpire(chr, itemId, num, throwY);
		}
		catch (Exception)
		{
		}
	}

	public override void StopScheduledTask()
	{
		if (wanderStarted)
		{
			BotWander.Stop(chr);
			wanderStarted = false;
		}
		BotMovementService.Stop(chr);
		base.StopScheduledTask();
	}
}

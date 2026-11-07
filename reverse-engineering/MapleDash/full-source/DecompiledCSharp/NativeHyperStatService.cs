using System;
using System.Collections.Generic;

public static class NativeHyperStatService
{
	private static HyperStatResponse Reject(string error)
	{
		return new HyperStatResponse
		{
			ok = false,
			error = error
		};
	}

	public static HyperStatResponse Handle(DbStore db, HyperStatRequest request)
	{
		if (db == null || request == null)
		{
			return Reject("无效极限属性请求");
		}
		lock (db)
		{
			CharRecord charRecord = CharactersDao.FindById(db, request.characterId);
			if (charRecord == null)
			{
				return Reject("角色不存在");
			}
			GrowthState growthState = GrowthDao.Load(db, request.characterId);
			int num = ((request.preset >= 0 && request.preset < 3) ? request.preset : growthState.hyperPreset);
			List<HyperStatEntry> lines = Rows(GrowthDao.HyperStats(db, request.characterId), num);
			HyperStatResponse hyperStatResponse = Snapshot(charRecord, growthState, lines, num);
			if (request.operation == HyperStatOperation.Preview)
			{
				return hyperStatResponse;
			}
			if (charRecord.level < 140)
			{
				return Reject(140 + " 级才能使用极限属性");
			}
			switch (request.operation)
			{
			case HyperStatOperation.Apply:
			{
				HyperStatInfo hyperStatInfo = HyperStatCatalog.Of(request.skillId);
				if (hyperStatInfo == null)
				{
					return Reject("没有这条极限属性");
				}
				int num5 = ((request.amount <= 0) ? 1 : request.amount);
				int num6 = LevelOf(lines, request.skillId);
				int num7 = num6 + num5;
				if (num7 > hyperStatInfo.maxLevel)
				{
					return Reject("已到这条属性的上限");
				}
				int num8 = (hyperStatResponse.neededSp = SpentBetween(num6, num7));
				if (!request.commit)
				{
					return hyperStatResponse;
				}
				if (hyperStatResponse.availableSp < num8)
				{
					return Reject("极限属性点数不足");
				}
				Upsert(lines, request.skillId, num7);
				Write(db, request.characterId, num, lines);
				return Done(db, request, num);
			}
			case HyperStatOperation.Remove:
			{
				if (HyperStatCatalog.Of(request.skillId) == null)
				{
					return Reject("没有这条极限属性");
				}
				int num2 = LevelOf(lines, request.skillId);
				int num3 = ((request.amount <= 0) ? 1 : request.amount);
				if (num2 <= 0)
				{
					return Reject("这条属性还没有加点");
				}
				int num4 = Math.Max(0, num2 - num3);
				hyperStatResponse.neededSp = -SpentBetween(num4, num2);
				if (!request.commit)
				{
					return hyperStatResponse;
				}
				Upsert(lines, request.skillId, num4);
				Write(db, request.characterId, num, lines);
				return Done(db, request, num);
			}
			case HyperStatOperation.Reset:
				hyperStatResponse.resetCost = 10000000L;
				if (!request.commit)
				{
					return hyperStatResponse;
				}
				if (charRecord.meso < 10000000)
				{
					return Reject("枫币不足");
				}
				db.BeginTransaction();
				try
				{
					charRecord.meso -= 10000000L;
					CharactersDao.Save(db, charRecord);
					GrowthDao.SaveHyperStats(db, request.characterId, num, new HyperStatEntry[0]);
					db.Save();
					db.Commit();
				}
				catch
				{
					db.Rollback();
					throw;
				}
				return Done(db, request, num);
			case HyperStatOperation.SwitchPreset:
				if (num == growthState.hyperPreset)
				{
					return Reject("已经在使用这套预设");
				}
				hyperStatResponse.switchCost = 2000000L;
				if (!request.commit)
				{
					return hyperStatResponse;
				}
				if (charRecord.meso < 2000000)
				{
					return Reject("枫币不足");
				}
				db.BeginTransaction();
				try
				{
					charRecord.meso -= 2000000L;
					CharactersDao.Save(db, charRecord);
					growthState.hyperPreset = num;
					GrowthDao.Save(db, request.characterId, growthState);
					db.Save();
					db.Commit();
				}
				catch
				{
					db.Rollback();
					throw;
				}
				return Done(db, request, num);
			default:
				return Reject("未支持的极限属性操作");
			}
		}
	}

	public static int LevelOf(List<HyperStatEntry> lines, int skillId)
	{
		foreach (HyperStatEntry line in lines)
		{
			if (line.skillId == skillId)
			{
				return line.slv;
			}
		}
		return 0;
	}

	private static List<HyperStatEntry> Rows(List<HyperStatEntry> all, int preset)
	{
		List<HyperStatEntry> list = new List<HyperStatEntry>();
		foreach (HyperStatEntry item in all)
		{
			if (item.preset == preset)
			{
				list.Add(new HyperStatEntry
				{
					preset = preset,
					skillId = item.skillId,
					slv = item.slv
				});
			}
		}
		return list;
	}

	public static int SpentBetween(int from, int to)
	{
		int num = 0;
		for (int i = from + 1; i <= to; i++)
		{
			num += HyperStatCatalog.NeededSp(i);
		}
		return num;
	}

	public static int SpentSp(List<HyperStatEntry> lines)
	{
		int num = 0;
		foreach (HyperStatEntry line in lines)
		{
			num += SpentBetween(0, line.slv);
		}
		return num;
	}

	private static void Upsert(List<HyperStatEntry> lines, int skillId, int slv)
	{
		foreach (HyperStatEntry line in lines)
		{
			if (line.skillId == skillId)
			{
				line.slv = slv;
				return;
			}
		}
		lines.Add(new HyperStatEntry
		{
			skillId = skillId,
			slv = slv
		});
	}

	private static void Write(DbStore db, int characterId, int preset, List<HyperStatEntry> lines)
	{
		db.BeginTransaction();
		try
		{
			GrowthDao.SaveHyperStats(db, characterId, preset, lines);
			db.Save();
			db.Commit();
		}
		catch
		{
			db.Rollback();
			throw;
		}
	}

	private static HyperStatResponse Done(DbStore db, HyperStatRequest request, int preset)
	{
		CharRecord character = CharactersDao.FindById(db, request.characterId);
		GrowthState state = GrowthDao.Load(db, request.characterId);
		List<HyperStatEntry> lines = Rows(GrowthDao.HyperStats(db, request.characterId), preset);
		return Snapshot(character, state, lines, preset);
	}

	private static HyperStatResponse Snapshot(CharRecord character, GrowthState state, List<HyperStatEntry> lines, int preset)
	{
		int num = HyperStatCatalog.TotalSp(character.level);
		int num2 = SpentSp(lines);
		HyperStatResponse hyperStatResponse = new HyperStatResponse
		{
			preset = preset,
			activePreset = state.hyperPreset,
			level = character.level,
			totalSp = num,
			spentSp = num2,
			availableSp = Math.Max(0, num - num2),
			meso = character.meso,
			resetCost = 10000000L,
			switchCost = 2000000L
		};
		foreach (HyperStatEntry line in lines)
		{
			HyperStatInfo hyperStatInfo = HyperStatCatalog.Of(line.skillId);
			if (hyperStatInfo != null)
			{
				hyperStatResponse.lines.Add(new HyperStatLine
				{
					skillId = hyperStatInfo.skillId,
					name = hyperStatInfo.name,
					key = hyperStatInfo.key,
					slv = line.slv,
					maxLevel = hyperStatInfo.maxLevel,
					value = hyperStatInfo.Value(line.slv)
				});
			}
		}
		return hyperStatResponse;
	}
}

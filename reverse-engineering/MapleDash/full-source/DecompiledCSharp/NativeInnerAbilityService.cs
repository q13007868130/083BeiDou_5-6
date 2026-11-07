using System;
using System.Collections.Generic;

public static class NativeInnerAbilityService
{
	private static AbilityResponse Reject(string error)
	{
		return new AbilityResponse
		{
			ok = false,
			error = error
		};
	}

	public static AbilityResponse Handle(DbStore db, AbilityRequest request, Random random = null)
	{
		if (db == null || request == null)
		{
			return Reject("无效内在能力请求");
		}
		if (random == null)
		{
			random = new Random();
		}
		lock (db)
		{
			CharRecord charRecord = CharactersDao.FindById(db, request.characterId);
			if (charRecord == null)
			{
				return Reject("角色不存在");
			}
			GrowthState growthState = GrowthDao.Load(db, request.characterId);
			int preset = ((request.preset >= 0 && request.preset < 3) ? request.preset : growthState.abilityPreset);
			List<PotentialEntry> list = Rows(GrowthDao.Potentials(db, request.characterId), preset);
			AbilityResponse abilityResponse = Snapshot(growthState, list, preset, LockedCount(request, list.Count));
			abilityResponse.lineCount = Math.Min(list.Count, UnlockedLines(charRecord.level));
			if (request.operation == AbilityOperation.Preview)
			{
				return abilityResponse;
			}
			return request.operation switch
			{
				AbilityOperation.Honor => Honor(db, request, charRecord, growthState, list, preset, abilityResponse, random), 
				AbilityOperation.Choose => Choose(db, request, growthState, preset, abilityResponse), 
				AbilityOperation.SwitchPreset => SwitchPreset(db, request, charRecord, growthState, preset, abilityResponse), 
				AbilityOperation.Circulator => Circulator(db, request, charRecord, growthState, list, preset, abilityResponse, random), 
				_ => Reject("未支持的内在能力操作"), 
			};
		}
	}

	private static AbilityResponse Circulator(DbStore db, AbilityRequest request, CharRecord character, GrowthState state, List<PotentialEntry> lines, int preset, AbilityResponse response, Random random)
	{
		if (UnlockedLines(character.level) < 3)
		{
			return Reject("需要解锁 3 行内在能力才能使用循环器");
		}
		if (lines.Count < 3)
		{
			return Reject("需要 3 行内在能力才能使用循环器");
		}
		if (response.pending)
		{
			return Reject("请先确认上一次循环器的结果");
		}
		DbRow dbRow = db.Table("inventoryitems").Find((DbRow x) => DbStore.Int(x, "characterid") == request.characterId && DbStore.Long(x, "inventoryitemid", 0L) == request.circulatorInventoryItemId);
		if (dbRow == null || DbStore.Int(dbRow, "quantity") < 1 || DbStore.Int(dbRow, "position") <= 0)
		{
			return Reject("循环器不存在");
		}
		bool flag = DbStore.Int(dbRow, "itemid") / 1000 == 5062;
		int grade = response.grade;
		if (flag && grade < 1)
		{
			return Reject("该循环器只能用于稀有以上等级的内在能力");
		}
		if (!flag && grade >= 3)
		{
			return Reject("该循环器不能用于传说等级的内在能力");
		}
		int grade2;
		if (flag)
		{
			grade2 = ((grade < 3 && random.Next(100) < AbilityCatalog.GradeUpRate(grade)) ? (grade + 1) : grade);
		}
		else
		{
			grade2 = RollGrade(grade, random);
		}
		List<PotentialEntry> rows = RollLines(lines, new HashSet<int>(), grade2, random);
		response.candidate = Describe(rows);
		response.pending = true;
		if (!request.commit)
		{
			return response;
		}
		db.BeginTransaction();
		try
		{
			int num = DbStore.Int(dbRow, "quantity");
			if (num <= 1)
			{
				CharService.RemoveItem(db, request.circulatorInventoryItemId);
			}
			else
			{
				dbRow["quantity"] = (long)num - 1L;
			}
			state.abilityChoice = Serialize(rows);
			GrowthDao.Save(db, request.characterId, state);
			db.Save();
			db.Commit();
		}
		catch
		{
			db.Rollback();
			throw;
		}
		AbilityResponse abilityResponse = Snapshot(GrowthDao.Load(db, request.characterId), Rows(GrowthDao.Potentials(db, request.characterId), preset), preset, LockedCount(request, lines.Count));
		abilityResponse.candidate = response.candidate;
		abilityResponse.pending = true;
		return abilityResponse;
	}

	private static AbilityResponse Honor(DbStore db, AbilityRequest request, CharRecord character, GrowthState state, List<PotentialEntry> lines, int preset, AbilityResponse response, Random random)
	{
		int num = UnlockedLines(character.level);
		if (lines.Count < num)
		{
			return Reject("请先解锁 " + num + " 行内在能力");
		}
		HashSet<int> hashSet = new HashSet<int>();
		foreach (int lockedRow in request.lockedRows)
		{
			if (lockedRow >= 0 && lockedRow < lines.Count)
			{
				hashSet.Add(lockedRow);
			}
		}
		if (hashSet.Count >= lines.Count)
		{
			return Reject("至少要留一行可以洗练");
		}
		int grade = response.grade;
		int num2 = (response.cost = AbilityCatalog.CostOf(grade, hashSet.Count));
		if (!request.commit)
		{
			return response;
		}
		if (state.honorExp < num2)
		{
			return Reject("荣誉值不足");
		}
		int grade2 = RollGrade(grade, random);
		List<PotentialEntry> rows = RollLines(lines, hashSet, grade2, random);
		response.candidate = Describe(rows);
		db.BeginTransaction();
		try
		{
			state.honorExp -= num2;
			GrowthDao.Save(db, request.characterId, state);
			GrowthDao.SavePotentials(db, request.characterId, preset, rows);
			db.Save();
			db.Commit();
		}
		catch
		{
			db.Rollback();
			throw;
		}
		List<PotentialEntry> lines2 = Rows(GrowthDao.Potentials(db, request.characterId), preset);
		AbilityResponse abilityResponse = Snapshot(GrowthDao.Load(db, request.characterId), lines2, preset, LockedCount(request, lines.Count));
		abilityResponse.cost = num2;
		abilityResponse.candidate = response.candidate;
		return abilityResponse;
	}

	private static AbilityResponse Choose(DbStore db, AbilityRequest request, GrowthState state, int preset, AbilityResponse response)
	{
		if (string.IsNullOrEmpty(state.abilityChoice))
		{
			return Reject("没有待确认的内在能力结果");
		}
		List<PotentialEntry> list = Parse(state.abilityChoice);
		response.pending = true;
		response.candidate = Describe(list);
		if (!request.commit)
		{
			return response;
		}
		db.BeginTransaction();
		try
		{
			if (request.accept)
			{
				GrowthDao.SavePotentials(db, request.characterId, preset, list);
			}
			state.abilityChoice = "";
			GrowthDao.Save(db, request.characterId, state);
			db.Save();
			db.Commit();
		}
		catch
		{
			db.Rollback();
			throw;
		}
		AbilityResponse abilityResponse = Snapshot(GrowthDao.Load(db, request.characterId), Rows(GrowthDao.Potentials(db, request.characterId), preset), preset, LockedCount(request, list.Count));
		abilityResponse.pending = false;
		return abilityResponse;
	}

	private static AbilityResponse SwitchPreset(DbStore db, AbilityRequest request, CharRecord character, GrowthState state, int preset, AbilityResponse response)
	{
		if (preset == state.abilityPreset)
		{
			return Reject("已经在使用这套预设");
		}
		if (!request.commit)
		{
			return response;
		}
		db.BeginTransaction();
		try
		{
			state.abilityPreset = preset;
			GrowthDao.Save(db, request.characterId, state);
			db.Save();
			db.Commit();
		}
		catch
		{
			db.Rollback();
			throw;
		}
		return Snapshot(GrowthDao.Load(db, request.characterId), Rows(GrowthDao.Potentials(db, request.characterId), preset), preset, LockedCount(request, response.lines.Count));
	}

	public static int UnlockedLines(int level)
	{
		if (level < 50)
		{
			if (level < 30)
			{
				return 1;
			}
			return 2;
		}
		return 3;
	}

	public static int RollGrade(int grade, Random random)
	{
		if (grade < 3 && random.Next(100) < AbilityCatalog.GradeUpRate(grade))
		{
			return grade + 1;
		}
		if (grade > 0 && random.Next(100) < AbilityCatalog.GradeDownRate(grade))
		{
			return grade - 1;
		}
		return grade;
	}

	private static List<PotentialEntry> RollLines(List<PotentialEntry> lines, HashSet<int> locked, int grade, Random random)
	{
		HashSet<int> hashSet = new HashSet<int>();
		foreach (PotentialEntry line in lines)
		{
			hashSet.Add(line.skillId);
		}
		List<PotentialEntry> list = new List<PotentialEntry>();
		for (int i = 0; i < lines.Count; i++)
		{
			if (locked.Contains(i))
			{
				list.Add(lines[i]);
				continue;
			}
			int num = 0;
			int num2 = 0;
			do
			{
				num = 70000000 + random.Next(63);
			}
			while (hashSet.Contains(num) && ++num2 < 500);
			hashSet.Add(num);
			AbilityInfo abilityInfo = AbilityCatalog.Of(num);
			int num3 = 1 + random.Next(grade * 10, grade * 10 + 10);
			if (abilityInfo != null)
			{
				num3 = AbilityCatalog.ClampLevel(abilityInfo, grade, num3);
			}
			list.Add(new PotentialEntry
			{
				potKey = i,
				skillId = num,
				slv = num3,
				grade = grade
			});
		}
		return list;
	}

	private static List<PotentialEntry> Rows(List<PotentialEntry> all, int preset)
	{
		List<PotentialEntry> list = new List<PotentialEntry>();
		foreach (PotentialEntry item in all)
		{
			if (item.preset == preset)
			{
				list.Add(item);
			}
		}
		list.Sort((PotentialEntry a, PotentialEntry b) => a.potKey.CompareTo(b.potKey));
		return list;
	}

	private static int LockedCount(AbilityRequest request, int lineCount)
	{
		int valueOrDefault = (request?.lockedRows?.Count).GetValueOrDefault();
		return Math.Max(0, Math.Min(valueOrDefault, Math.Max(0, lineCount - 1)));
	}

	private static AbilityResponse Snapshot(GrowthState state, List<PotentialEntry> lines, int preset, int lockedCount = 0)
	{
		int num = 0;
		foreach (PotentialEntry line in lines)
		{
			num = Math.Max(num, line.grade);
		}
		bool flag = !string.IsNullOrEmpty(state.abilityChoice);
		AbilityResponse abilityResponse = new AbilityResponse
		{
			preset = preset,
			activePreset = state.abilityPreset,
			grade = num,
			honorExp = state.honorExp,
			lineCount = lines.Count,
			pending = flag,
			cost = AbilityCatalog.CostOf(num, Math.Min(Math.Max(0, lockedCount), Math.Max(0, lines.Count - 1)))
		};
		abilityResponse.lines = Describe(lines);
		if (flag)
		{
			abilityResponse.candidate = Describe(Parse(state.abilityChoice));
		}
		return abilityResponse;
	}

	private static List<AbilityLine> Describe(List<PotentialEntry> rows)
	{
		List<AbilityLine> list = new List<AbilityLine>();
		foreach (PotentialEntry row in rows)
		{
			AbilityInfo abilityInfo = AbilityCatalog.Of(row.skillId);
			AbilityLine abilityLine = new AbilityLine
			{
				potKey = row.potKey,
				skillId = row.skillId,
				slv = row.slv,
				grade = row.grade
			};
			if (abilityInfo != null)
			{
				abilityLine.name = abilityInfo.name;
				if (abilityInfo.keys != null && abilityInfo.keys.Length != 0 && abilityInfo.formulas != null && abilityInfo.formulas.Length != 0)
				{
					abilityLine.key = abilityInfo.keys[0];
					abilityLine.value = GrowthFormula.Evaluate(abilityInfo.formulas[0], row.slv);
				}
			}
			list.Add(abilityLine);
		}
		return list;
	}

	public static string Serialize(List<PotentialEntry> rows)
	{
		List<string> list = new List<string>();
		foreach (PotentialEntry row in rows)
		{
			list.Add(row.potKey + ":" + row.skillId + ":" + row.slv + ":" + row.grade);
		}
		return string.Join(",", list);
	}

	public static List<PotentialEntry> Parse(string packed)
	{
		List<PotentialEntry> list = new List<PotentialEntry>();
		if (string.IsNullOrEmpty(packed))
		{
			return list;
		}
		string[] array = packed.Split(',');
		for (int i = 0; i < array.Length; i++)
		{
			string[] array2 = array[i].Split(':');
			if (array2.Length == 4 && int.TryParse(array2[0], out var result) && int.TryParse(array2[1], out var result2) && int.TryParse(array2[2], out var result3) && int.TryParse(array2[3], out var result4))
			{
				list.Add(new PotentialEntry
				{
					potKey = result,
					skillId = result2,
					slv = result3,
					grade = result4
				});
			}
		}
		return list;
	}
}

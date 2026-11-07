using System;

public static class NativeVMatrixService
{
	private static VMatrixResponse Reject(string error)
	{
		return new VMatrixResponse
		{
			ok = false,
			error = error
		};
	}

	public static VMatrixResponse Handle(DbStore db, VMatrixRequest request)
	{
		if (db == null || request == null)
		{
			return Reject("无效 V 矩阵请求");
		}
		VMatrixCatalog.Ensure();
		lock (db)
		{
			CharRecord charRecord = CharactersDao.FindById(db, request.characterId);
			if (charRecord == null)
			{
				return Reject("角色不存在");
			}
			VMatrixState vMatrixState = Load(db, request.characterId);
			VMatrixResponse vMatrixResponse = Snapshot(db, charRecord, vMatrixState);
			if (request.operation != VMatrixOperation.Transform)
			{
				if (request.operation == VMatrixOperation.Snapshot)
				{
					if (!vMatrixResponse.eligible)
					{
						vMatrixResponse.error = LockText(db, request.characterId, vMatrixResponse.level);
					}
				}
				else if (!vMatrixResponse.eligible)
				{
					return Reject(LockText(db, request.characterId, vMatrixResponse.level));
				}
			}
			switch (request.operation)
			{
			case VMatrixOperation.Snapshot:
				return vMatrixResponse;
			case VMatrixOperation.Transform:
			{
				if (request.count <= 0)
				{
					return Reject("转换数量不正确。");
				}
				long num2 = (long)request.count * (long)VMatrixCatalog.TransformCost;
				if (vMatrixState.points < num2)
				{
					return Reject("V 点数不足，无法转换。");
				}
				if (!request.commit)
				{
					vMatrixResponse.points = vMatrixState.points - num2;
					return vMatrixResponse;
				}
				vMatrixState.points -= num2;
				GrantGemstones(db, charRecord, VMatrixCatalog.TransformItem, request.count, VMatrixCatalog.TransformMinutes);
				break;
			}
			case VMatrixOperation.UseGemstone:
			{
				VMatrixGemstone vMatrixGemstone = VMatrixCatalog.Gemstone(request.itemId);
				if (vMatrixGemstone == null)
				{
					return Reject("没有这种核心宝石。");
				}
				if (request.count <= 0)
				{
					return Reject("转换数量不正确。");
				}
				if (InventoryDao.CountOf(db, request.characterId, request.itemId) < request.count)
				{
					return Reject("没有足够的核心宝石。");
				}
				if (vMatrixState.points >= 9999)
				{
					return Reject("V 点数已达 9999 点上限。");
				}
				if (vMatrixGemstone.reqQuest != 0 && QuestStatusDao.StatusOf(db, request.characterId, vMatrixGemstone.reqQuest) != 2)
				{
					return Reject(string.IsNullOrEmpty(vMatrixGemstone.questAlert) ? "尚未完成使用此核心宝石所需的任务。" : vMatrixGemstone.questAlert);
				}
				if (!request.commit)
				{
					vMatrixResponse.points = vMatrixState.points + (long)request.count * (long)vMatrixGemstone.points;
					return vMatrixResponse;
				}
				int num5 = request.count;
				if (vMatrixGemstone.openCore != 0)
				{
					if (!VMatrixCatalog.Allowed(charRecord.job, vMatrixGemstone.openCore))
					{
						return Reject("此职业无法使用所选核心。");
					}
					VMatrixCoreView vMatrixCoreView2 = vMatrixState.Find(vMatrixGemstone.openCore);
					if (vMatrixCoreView2 == null || vMatrixCoreView2.level == 0)
					{
						SetLevel(vMatrixState, vMatrixGemstone.openCore, 1);
						num5--;
					}
				}
				InventoryDao.ConsumeItem(db, request.characterId, request.itemId, request.count);
				long num6 = vMatrixState.points + (long)num5 * (long)vMatrixGemstone.points;
				if (num6 > 9999)
				{
					return Reject("使用后将超过 V 点数 9999 点上限。");
				}
				vMatrixState.points = num6;
				break;
			}
			case VMatrixOperation.Upgrade:
			{
				VMatrixCore vMatrixCore3 = VMatrixCatalog.Core(request.coreId);
				if (vMatrixCore3 == null)
				{
					return Reject("没有这个核心。");
				}
				if (!VMatrixCatalog.Allowed(charRecord.job, vMatrixCore3.id))
				{
					return Reject("此职业无法使用所选核心。");
				}
				int num3 = vMatrixState.LevelOf(vMatrixCore3.id);
				if (vMatrixCore3.IsSpecial)
				{
					if (request.targetLevel != 1 || num3 > 0)
					{
						return Reject("特殊核心已经开启。");
					}
					if (vMatrixState.points < VMatrixCatalog.RegisterSpecialCost)
					{
						return Reject("V 点数不足。");
					}
					if (!request.commit)
					{
						return vMatrixResponse;
					}
					vMatrixState.points -= VMatrixCatalog.RegisterSpecialCost;
					SetLevel(vMatrixState, vMatrixCore3.id, 1);
					break;
				}
				if (vMatrixCore3.type > 1)
				{
					return Reject("核心目标等级不正确。");
				}
				if (request.targetLevel <= num3 || request.targetLevel > vMatrixCore3.maxLevel)
				{
					return Reject("核心目标等级不正确。");
				}
				if (!vMatrixCore3.selfOpen && num3 == 0)
				{
					return Reject("此核心必须先使用指定道具开启。");
				}
				long num4 = VMatrixCatalog.CostBetween(vMatrixCore3.id, num3, request.targetLevel);
				if (num4 < 0)
				{
					return Reject("核心目标等级不正确。");
				}
				if (vMatrixState.points < num4)
				{
					return Reject("V 点数不足。");
				}
				vMatrixResponse.points = vMatrixState.points - num4;
				if (!request.commit)
				{
					return vMatrixResponse;
				}
				vMatrixState.points -= num4;
				SetLevel(vMatrixState, vMatrixCore3.id, request.targetLevel);
				break;
			}
			case VMatrixOperation.ExtendSpecial:
			{
				VMatrixCore vMatrixCore2 = VMatrixCatalog.Core(request.coreId);
				if (vMatrixCore2 == null || !vMatrixCore2.IsSpecial)
				{
					return Reject("没有这个特殊核心。");
				}
				return Reject("本服特殊核心永久有效，无需延期。");
			}
			case VMatrixOperation.SelectSpecial:
			case VMatrixOperation.UnselectSpecial:
			{
				VMatrixCore vMatrixCore4 = VMatrixCatalog.Core(request.coreId);
				if (vMatrixCore4 == null || !vMatrixCore4.IsSpecial)
				{
					return Reject("没有这个特殊核心。");
				}
				VMatrixCoreView vMatrixCoreView = vMatrixState.Find(vMatrixCore4.id);
				if (vMatrixCoreView == null || vMatrixCoreView.level <= 0)
				{
					return Reject("特殊核心尚未开启或已到期。");
				}
				bool flag = request.operation == VMatrixOperation.SelectSpecial;
				if (flag && vMatrixCoreView.selected)
				{
					return Reject("此特殊核心已经启用。");
				}
				if (!flag && !vMatrixCoreView.selected)
				{
					return Reject("此特殊核心尚未启用。");
				}
				if (!request.commit)
				{
					return vMatrixResponse;
				}
				foreach (VMatrixCoreView value in vMatrixState.cores.Values)
				{
					value.selected = false;
				}
				vMatrixCoreView.selected = flag;
				break;
			}
			case VMatrixOperation.Reset:
				if (charRecord.meso < VMatrixCatalog.ResetMeso)
				{
					return Reject("枫币不足，无法重置 V 矩阵。");
				}
				if (!request.commit)
				{
					vMatrixResponse.points = vMatrixState.points + Refundable(vMatrixState, charRecord.job);
					vMatrixResponse.meso = charRecord.meso - VMatrixCatalog.ResetMeso;
					return vMatrixResponse;
				}
				charRecord.meso -= VMatrixCatalog.ResetMeso;
				foreach (VMatrixCoreView value2 in vMatrixState.cores.Values)
				{
					VMatrixCore vMatrixCore = VMatrixCatalog.Core(value2.coreId);
					if (vMatrixCore != null && vMatrixCore.type <= 1)
					{
						int num = Math.Min(value2.level, Math.Max(value2.protectedLevel, VMatrixCatalog.ResetFloor(charRecord.job, value2.coreId)));
						if (value2.level > num)
						{
							vMatrixState.points += VMatrixCatalog.CostBetween(value2.coreId, num, value2.level);
						}
						value2.level = num;
						value2.selected = false;
					}
				}
				break;
			default:
				return Reject("不支援的 V 矩阵操作。");
			}
			if (vMatrixState.points > 9999)
			{
				return Reject("操作后将超过 V 点数 9999 点上限。");
			}
			vMatrixState.revision++;
			db.BeginTransaction();
			try
			{
				Save(db, request.characterId, vMatrixState);
				CharactersDao.Save(db, charRecord);
				db.Save();
				db.Commit();
			}
			catch
			{
				db.Rollback();
				throw;
			}
			return Snapshot(db, charRecord, vMatrixState);
		}
	}

	private static string LockText(DbStore db, int characterId, int level)
	{
		if (level >= 200)
		{
			if (QuestStatusDao.StatusOf(db, characterId, 1465) == 2)
			{
				return "当前职业无法使用 V 矩阵";
			}
			return "尚未完成五转任务，无法使用 V 矩阵";
		}
		return "200 级才能使用 V 矩阵";
	}

	private static long Refundable(VMatrixState state, int job)
	{
		long num = 0L;
		foreach (VMatrixCoreView value in state.cores.Values)
		{
			VMatrixCore vMatrixCore = VMatrixCatalog.Core(value.coreId);
			if (vMatrixCore != null && vMatrixCore.type <= 1)
			{
				int num2 = Math.Min(value.level, Math.Max(value.protectedLevel, VMatrixCatalog.ResetFloor(job, value.coreId)));
				if (value.level > num2)
				{
					num += VMatrixCatalog.CostBetween(value.coreId, num2, value.level);
				}
			}
		}
		return num;
	}

	private static void SetLevel(VMatrixState state, int coreId, int level)
	{
		if (!state.cores.TryGetValue(coreId, out var value))
		{
			value = new VMatrixCoreView
			{
				coreId = coreId
			};
			state.cores[coreId] = value;
		}
		value.level = level;
	}

	private static void GrantGemstones(DbStore db, CharRecord character, int itemId, int count, int minutes)
	{
		long expiration = ((minutes <= 0) ? (-1) : DateTimeOffset.UtcNow.AddMinutes(minutes).ToUnixTimeMilliseconds());
		NativeInventoryService.AddItems(db, character, itemId, count, expiration);
	}

	private static VMatrixState Load(DbStore db, int characterId)
	{
		VMatrixState vMatrixState = new VMatrixState();
		DbRow dbRow = db.Table("vmatrix_state").Find(DbStore.Eq("characterid", (long)characterId));
		if (dbRow != null)
		{
			vMatrixState.points = DbStore.Long(dbRow, "points", 0L);
			vMatrixState.revision = DbStore.Int(dbRow, "revision");
		}
		foreach (DbRow item in db.Table("vmatrix_cores").Select(DbStore.Eq("characterid", (long)characterId)))
		{
			int num = DbStore.Int(item, "coreid");
			vMatrixState.cores[num] = new VMatrixCoreView
			{
				coreId = num,
				level = DbStore.Int(item, "level"),
				selected = (DbStore.Int(item, "selected") != 0),
				protectedLevel = DbStore.Int(item, "protectedlevel")
			};
		}
		return vMatrixState;
	}

	private static void Save(DbStore db, int characterId, VMatrixState state)
	{
		DbTable dbTable = db.Table("vmatrix_state");
		DbRow dbRow = new DbRow
		{
			["points"] = state.points,
			["revision"] = (long)state.revision
		};
		if (dbTable.Update(DbStore.Eq("characterid", (long)characterId), dbRow) == 0)
		{
			dbRow["characterid"] = (long)characterId;
			dbTable.Insert(dbRow);
		}
		DbTable dbTable2 = db.Table("vmatrix_cores");
		dbTable2.Delete(DbStore.Eq("characterid", (long)characterId));
		foreach (VMatrixCoreView value in state.cores.Values)
		{
			if (value.level > 0 || value.selected)
			{
				dbTable2.Insert(new DbRow
				{
					["characterid"] = (long)characterId,
					["coreid"] = (long)value.coreId,
					["level"] = (long)value.level,
					["selected"] = (long)(value.selected ? 1 : 0),
					["expiresat"] = 0L,
					["protectedlevel"] = (long)value.protectedLevel
				});
			}
		}
		NativeMatrixSkills.Sync(db, characterId);
	}

	private static VMatrixResponse Snapshot(DbStore db, CharRecord character, VMatrixState state)
	{
		int job = character.job;
		VMatrixResponse vMatrixResponse = new VMatrixResponse
		{
			points = state.points,
			meso = character.meso,
			level = character.level,
			job = job,
			revision = state.revision,
			resetMeso = VMatrixCatalog.ResetMeso,
			transformCost = VMatrixCatalog.TransformCost,
			transformItem = VMatrixCatalog.TransformItem,
			transformMinutes = VMatrixCatalog.TransformMinutes,
			registerCost = VMatrixCatalog.RegisterSpecialCost,
			eligible = (VMatrixCatalog.Group(job) != null && (character.gm > 0 || (character.level >= 200 && QuestStatusDao.StatusOf(db, (int)character.id, 1465) == 2)))
		};
		VMatrixGroup vMatrixGroup = VMatrixCatalog.Group(job);
		vMatrixResponse.slots = VMatrixSlots.Assign(vMatrixGroup);
		int[] slots = vMatrixResponse.slots;
		foreach (int num in slots)
		{
			if (num != 0)
			{
				VMatrixCore vMatrixCore = VMatrixCatalog.Core(num);
				if (vMatrixCore != null)
				{
					VMatrixCoreView view = state.Find(num);
					vMatrixResponse.cores.Add(View(vMatrixCore, view));
				}
			}
		}
		foreach (VMatrixCore allCore in VMatrixCatalog.AllCores)
		{
			if (allCore.IsSpecial)
			{
				VMatrixCoreView vMatrixCoreView = state.Find(allCore.id);
				if (vMatrixCoreView != null)
				{
					vMatrixResponse.cores.Add(View(allCore, vMatrixCoreView));
				}
			}
		}
		return vMatrixResponse;
	}

	private static VMatrixCoreView View(VMatrixCore core, VMatrixCoreView view)
	{
		return new VMatrixCoreView
		{
			coreId = core.id,
			type = core.type,
			maxLevel = core.maxLevel,
			name = core.name,
			desc = core.desc,
			skills = core.skills,
			level = (view?.level ?? 0),
			selected = (view?.selected ?? false),
			protectedLevel = (view?.protectedLevel ?? 0)
		};
	}
}

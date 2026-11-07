using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

public static class AdminBackend
{
	public const int HenesysMap = 100000000;

	public const string DefaultAdminName = "admin";

	public const string DefaultAdminPassword = "admin";

	public static Func<long> CurrentCharacterId;

	public static Action<string> Broadcast;

	public static DbStore DbOverride;

	public const string AdminSeedCode = "webadmin_seed";

	public static bool Ready => Db != null;

	public static DbStore Db
	{
		get
		{
			if (DbOverride != null)
			{
				return DbOverride;
			}
			return GameServer.Local?.Db;
		}
	}

	public static string Version => "MapleDash 单机版 " + UnityEngineVersion();

	private static string UnityEngineVersion()
	{
		return Application.version;
	}

	public static string Camel(string snake)
	{
		if (string.IsNullOrEmpty(snake) || snake.IndexOf('_') < 0)
		{
			return snake;
		}
		StringBuilder stringBuilder = new StringBuilder(snake.Length);
		bool flag = false;
		foreach (char c in snake)
		{
			if (c == '_')
			{
				flag = true;
				continue;
			}
			stringBuilder.Append(flag ? char.ToUpperInvariant(c) : c);
			flag = false;
		}
		return stringBuilder.ToString();
	}

	public static Dictionary<string, object> ToDto(DbRow row, params string[] omit)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>(row.Count);
		foreach (KeyValuePair<string, object> item in row)
		{
			if (omit == null || Array.IndexOf(omit, item.Key) < 0)
			{
				dictionary[Camel(item.Key)] = item.Value;
			}
		}
		return dictionary;
	}

	public static string EnsureWebAdmin()
	{
		return AdminMainThread.Run(() =>
		{
			DbStore db = Db;
			if (db == null)
			{
				return "后台账号未初始化：数据库还没就绪";
			}
			DbTable dbTable = db.Table("game_config");
			DbRow seed = dbTable.Find((DbRow r) => DbStore.Str(r, "config_code") == "webadmin_seed");
			if (seed != null && DbStore.Str(seed, "config_value") == "1")
			{
				return (string)null;
			}
			string bootstrapPassword = Environment.GetEnvironmentVariable("MAPLEDASH_ADMIN_BOOTSTRAP_PASSWORD");
			if (string.IsNullOrEmpty(bootstrapPassword) || bootstrapPassword.Length < 16)
			{
				return "未设置 MAPLEDASH_ADMIN_BOOTSTRAP_PASSWORD（至少16位），不初始化管理员";
			}
			DbTable dbTable2 = db.Table("accounts");
			DbRow dbRow = AccountsDao.FindByName(db, "admin");
			string result;
			if (dbRow == null)
			{
				AccountsDao.AutoRegister(db, "admin", bootstrapPassword);
				dbRow = AccountsDao.FindByName(db, "admin");
				result = "已创建后台管理员账号 admin（凭据不写入日志）";
			}
			else if (!AccountsDao.VerifyPassword(bootstrapPassword, DbStore.Str(dbRow, "password")))
			{
				dbTable2.Update(DbStore.Eq("id", Convert.ToInt64(dbRow["id"])), new DbRow { ["password"] = AccountsDao.HashPassword(bootstrapPassword) });
				result = "内置管理员密码已根据首次配置更新（凭据不写入日志）";
			}
			else
			{
				result = "内置后台账号已就绪（凭据不写入日志）";
			}
			if (dbRow != null)
			{
				dbTable2.Update(DbStore.Eq("id", Convert.ToInt64(dbRow["id"])), new DbRow
				{
					["webadmin"] = 1L,
					["banned"] = 0L
				});
			}
			if (seed != null)
			{
				dbTable.Update((DbRow r) => r == seed, new DbRow { ["config_value"] = "1" });
			}
			else
			{
				dbTable.Insert(new DbRow
				{
					["config_type"] = "webadmin",
					["config_sub_type"] = "0",
					["config_clazz"] = "java.lang.String",
					["config_code"] = "webadmin_seed",
					["config_value"] = "1",
					["config_desc"] = "内置后台账号是否已初始化（1=是，改过密码后不再覆盖）"
				});
			}
			db.Save();
			return result;
		});
	}

	public static AdminAccount Authenticate(string name, string password)
	{
		return AdminMainThread.Run(() =>
		{
			DbStore db = Db;
			if (db == null)
			{
				return (AdminAccount)null;
			}
			if (string.IsNullOrEmpty(name))
			{
				return (AdminAccount)null;
			}
			DbRow dbRow = AccountsDao.FindByName(db, name);
			if (dbRow == null)
			{
				return (AdminAccount)null;
			}
			if (DbStore.Int(dbRow, "webadmin") == 0)
			{
				return (AdminAccount)null;
			}
			if (DbStore.Int(dbRow, "banned") != 0)
			{
				return (AdminAccount)null;
			}
			if (!AccountsDao.VerifyPassword(password ?? "", DbStore.Str(dbRow, "password")))
			{
				return (AdminAccount)null;
			}
			long num = Convert.ToInt64(dbRow["id"]);
			db.Table("accounts").Update(DbStore.Eq("id", num), new DbRow { ["lastlogin"] = DbStore.NowText() });
			db.Save();
			return new AdminAccount
			{
				Id = num,
				Name = DbStore.Str(dbRow, "name"),
				WebAdmin = true
			};
		});
	}

	public static Dictionary<string, object> AccountInfo(long id)
	{
		return AdminMainThread.Run(() =>
		{
			DbStore db = Db;
			if (db == null)
			{
				return (Dictionary<string, object>)null;
			}
			DbRow dbRow = AccountsDao.FindById(db, (int)id);
			if (dbRow != null)
			{
				Dictionary<string, object> dictionary = ToDto(dbRow, "password");
				dictionary["webadmin"] = true;
				return dictionary;
			}
			return (Dictionary<string, object>)null;
		});
	}

	public static Dictionary<string, object> AccountPage(int page, int size, long id, string name, string lastLoginStart, string lastLoginEnd, string createdAtStart, string createdAtEnd)
	{
		return AdminMainThread.Run(() =>
		{
			DbStore db = Db;
			List<DbRow> list = new List<DbRow>();
			if (db != null)
			{
				foreach (DbRow row in db.Table("accounts").rows)
				{
					if ((id <= 0 || Convert.ToInt64(row["id"]) == id) && (string.IsNullOrEmpty(name) || DbStore.Str(row, "name").IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) && InRange(DbStore.Str(row, "lastlogin"), lastLoginStart, lastLoginEnd) && InRange(DbStore.Str(row, "createdat"), createdAtStart, createdAtEnd))
					{
						list.Add(row);
					}
				}
			}
			list.Sort((DbRow a, DbRow b) => Convert.ToInt64(b["id"]).CompareTo(Convert.ToInt64(a["id"])));
			int num = ((page <= 0) ? 1 : page);
			int num2 = ((size <= 0) ? 20 : size);
			List<object> list2 = new List<object>();
			for (int num3 = (num - 1) * num2; num3 < list.Count && num3 < num * num2; num3++)
			{
				list2.Add(ToDto(list[num3], "password"));
			}
			return AdminJson.Page(list2, num, num2, list.Count);
		});
	}

	private static bool InRange(string value, string from, string to)
	{
		if (string.IsNullOrEmpty(value))
		{
			if (string.IsNullOrEmpty(from))
			{
				return string.IsNullOrEmpty(to);
			}
			return false;
		}
		string strA = ((value.Length >= 10) ? value.Substring(0, 10) : value);
		if (!string.IsNullOrEmpty(from) && string.CompareOrdinal(strA, from.Substring(0, Math.Min(10, from.Length))) < 0)
		{
			return false;
		}
		if (!string.IsNullOrEmpty(to) && string.CompareOrdinal(strA, to.Substring(0, Math.Min(10, to.Length))) > 0)
		{
			return false;
		}
		return true;
	}

	public static List<object> CharacterMaps(long accountId)
	{
		return AdminMainThread.Run(() =>
		{
			List<object> list = new List<object>();
			DbStore db = Db;
			if (db == null)
			{
				return list;
			}
			foreach (DbRow item in db.Table("characters").Select(DbStore.Eq("accountid", accountId)))
			{
				long num = DbStore.Long(item, "map", 0L);
				list.Add(AdminJson.Obj("characterId", Convert.ToInt64(item["id"]), "characterName", DbStore.Str(item, "name"), "mapId", num, "mapName", MapName(num)));
			}
			return list;
		});
	}

	private static string MapName(long mapId)
	{
		if (mapId > 0)
		{
			return mapId.ToString(CultureInfo.InvariantCulture);
		}
		return "-";
	}

	public static void AddAccount(string name, string password, string birthday, long? language)
	{
		AdminMainThread.Run(() =>
		{
			DbStore dbStore = RequireDb();
			if (string.IsNullOrEmpty(name))
			{
				throw new AdminApiException("账号不能为空");
			}
			if (string.IsNullOrEmpty(password))
			{
				throw new AdminApiException("密码不能为空");
			}
			if (AccountsDao.FindByName(dbStore, name) != null)
			{
				throw new AdminApiException("账号已存在: " + name);
			}
			DbRow dbRow = new DbRow
			{
				["name"] = name,
				["password"] = AccountsDao.HashPassword(password),
				["createdat"] = DbStore.NowText()
			};
			if (!string.IsNullOrEmpty(birthday))
			{
				dbRow["birthday"] = birthday;
			}
			if (language.HasValue)
			{
				dbRow["language"] = language.Value;
			}
			dbStore.Table("accounts").Insert(dbRow);
			dbStore.Save();
		});
	}

	public static void UpdateAccount(long id, Dictionary<string, object> form)
	{
		AdminMainThread.Run(() =>
		{
			DbStore dbStore = RequireDb();
			DbTable dbTable = dbStore.Table("accounts");
			if (dbTable.Find(DbStore.Eq("id", id)) == null)
			{
				throw new AdminApiException("账号不存在: " + id);
			}
			DbRow dbRow = new DbRow();
			string text = AdminJson.GetString(form, "newPwd");
			if (!string.IsNullOrEmpty(text))
			{
				dbRow["password"] = AccountsDao.HashPassword(text);
			}
			Copy(form, dbRow, "pin", "pin");
			Copy(form, dbRow, "pic", "pic");
			Copy(form, dbRow, "birthday", "birthday");
			Copy(form, dbRow, "nxCredit", "nxCredit");
			Copy(form, dbRow, "maplePoint", "maplePoint");
			Copy(form, dbRow, "nxPrepaid", "nxPrepaid");
			Copy(form, dbRow, "characterslots", "characterslots");
			Copy(form, dbRow, "gender", "gender");
			Copy(form, dbRow, "webadmin", "webadmin");
			Copy(form, dbRow, "nick", "nick");
			Copy(form, dbRow, "mute", "mute");
			Copy(form, dbRow, "email", "email");
			Copy(form, dbRow, "rewardpoints", "rewardpoints");
			Copy(form, dbRow, "votepoints", "votepoints");
			Copy(form, dbRow, "language", "language");
			if (dbRow.Count != 0)
			{
				dbTable.Update(DbStore.Eq("id", id), dbRow);
				dbStore.Save();
			}
		});
	}

	private static void Copy(Dictionary<string, object> from, DbRow to, string fromKey, string column)
	{
		if (from != null && from.ContainsKey(fromKey))
		{
			object obj = from[fromKey];
			if (obj != null)
			{
				to[column] = ((!(obj is bool)) ? obj : ((object)(long)(((bool)obj) ? 1 : 0)));
			}
		}
	}

	public static void DeleteAccount(long id)
	{
		AdminMainThread.Run(() =>
		{
			DbStore dbStore = RequireDb();
			DbTable dbTable = dbStore.Table("characters");
			List<long> list = new List<long>();
			foreach (DbRow item in dbTable.Select(DbStore.Eq("accountid", id)))
			{
				list.Add(Convert.ToInt64(item["id"]));
			}
			foreach (long item2 in list)
			{
				DeleteCharacterRows(dbStore, item2);
			}
			dbTable.Delete(DbStore.Eq("accountid", id));
			dbStore.Table("accounts").Delete(DbStore.Eq("id", id));
			dbStore.Save();
		});
	}

	private static void DeleteCharacterRows(DbStore db, long characterId)
	{
		string[] array = new string[5] { "keymap", "skills", "skillmacros", "queststatus", "savedlocations" };
		foreach (string name in array)
		{
			if (db.HasTable(name))
			{
				db.Table(name).Delete(DbStore.Eq("characterid", characterId));
			}
		}
		foreach (DbRow item in db.Table("inventoryitems").Select(DbStore.Eq("characterid", characterId)))
		{
			long num = Convert.ToInt64(item["inventoryitemid"]);
			if (db.HasTable("inventoryequipment"))
			{
				db.Table("inventoryequipment").Delete(DbStore.Eq("inventoryitemid", num));
			}
		}
		db.Table("inventoryitems").Delete(DbStore.Eq("characterid", characterId));
	}

	public static void BanAccount(long id, string reason)
	{
		AdminMainThread.Run(() =>
		{
			DbStore dbStore = RequireDb();
			dbStore.Table("accounts").Update(DbStore.Eq("id", id), new DbRow
			{
				["banned"] = 1L,
				["banreason"] = (string.IsNullOrEmpty(reason) ? "违规操作" : reason),
				["loggedin"] = 0L
			});
			dbStore.Save();
			LiveKick(id, reason);
		});
	}

	public static void UnbanAccount(long id)
	{
		AdminMainThread.Run(() =>
		{
			DbStore dbStore = RequireDb();
			dbStore.Table("accounts").Update(DbStore.Eq("id", id), new DbRow
			{
				["banned"] = 0L,
				["banreason"] = null,
				["tempban"] = "2005-05-11 00:00:00"
			});
			dbStore.Save();
		});
	}

	public static void ResetLoggedIn(long id)
	{
		AdminMainThread.Run(() =>
		{
			DbStore dbStore = RequireDb();
			dbStore.Table("accounts").Update(DbStore.Eq("id", id), new DbRow { ["loggedin"] = 0L });
			dbStore.Save();
		});
	}

	public static void MoveAccountToHenesys(long accountId)
	{
		AdminMainThread.Run(() =>
		{
			DbStore dbStore = RequireDb();
			dbStore.Table("characters").Update(DbStore.Eq("accountid", accountId), new DbRow
			{
				["map"] = 100000000L,
				["spawnpoint"] = 0L
			});
			dbStore.Save();
			long num = CurrentCharId();
			if (num > 0)
			{
				DbRow dbRow = dbStore.Table("characters").Find(DbStore.Eq("id", num));
				if (dbRow != null && DbStore.Long(dbRow, "accountid", 0L) == accountId)
				{
					LiveWarp(num);
				}
			}
		});
	}

	public static Dictionary<string, object> OnlineCharacterPage(int page, int size, long id, string name, long map)
	{
		return AdminMainThread.Run(() =>
		{
			DbStore db = Db;
			List<DbRow> list = new List<DbRow>();
			if (db != null)
			{
				foreach (DbRow row in db.Table("characters").rows)
				{
					if ((id <= 0 || Convert.ToInt64(row["id"]) == id) && (string.IsNullOrEmpty(name) || DbStore.Str(row, "name").IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) && (map <= 0 || DbStore.Long(row, "map", 0L) == map))
					{
						list.Add(row);
					}
				}
			}
			list.Sort((DbRow a, DbRow b) => Convert.ToInt64(b["id"]).CompareTo(Convert.ToInt64(a["id"])));
			int num = ((page <= 0) ? 1 : page);
			int num2 = ((size <= 0) ? 20 : size);
			long num3 = 0L;
			try
			{
				num3 = ((CurrentCharacterId != null) ? CurrentCharacterId() : 0);
			}
			catch (Exception)
			{
			}
			List<object> list2 = new List<object>();
			for (int num4 = (num - 1) * num2; num4 < list.Count && num4 < num * num2; num4++)
			{
				DbRow dbRow = list[num4];
				int job = DbStore.Int(dbRow, "job");
				Dictionary<string, object> dictionary = ToDto(dbRow);
				dictionary["jobName"] = SafeJobName(job);
				dictionary["online"] = Convert.ToInt64(dbRow["id"]) == num3 && num3 > 0;
				list2.Add(dictionary);
			}
			return AdminJson.Page(list2, num, num2, list.Count);
		});
	}

	private static string SafeJobName(int job)
	{
		try
		{
			return JobDefs.NameOf(job);
		}
		catch (Exception)
		{
			return job.ToString(CultureInfo.InvariantCulture);
		}
	}

	public static void MoveCharacterToHenesys(long id)
	{
		AdminMainThread.Run(() =>
		{
			DbStore dbStore = RequireDb();
			DbTable dbTable = dbStore.Table("characters");
			if (dbTable.Find(DbStore.Eq("id", id)) == null)
			{
				throw new AdminApiException("角色不存在: " + id);
			}
			dbTable.Update(DbStore.Eq("id", id), new DbRow
			{
				["map"] = 100000000L,
				["spawnpoint"] = 0L
			});
			dbStore.Save();
			if (CurrentCharId() == id)
			{
				LiveWarp(id);
			}
		});
	}

	public static int MoveCharactersToHenesys(long id, string name)
	{
		return AdminMainThread.Run(() =>
		{
			DbStore dbStore = RequireDb();
			DbTable dbTable = dbStore.Table("characters");
			int num = 0;
			bool flag = false;
			long num2 = CurrentCharId();
			foreach (DbRow row in dbTable.rows)
			{
				if ((id <= 0 || Convert.ToInt64(row["id"]) == id) && (string.IsNullOrEmpty(name) || DbStore.Str(row, "name").IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0))
				{
					row["map"] = 100000000L;
					row["spawnpoint"] = 0L;
					if (num2 > 0 && Convert.ToInt64(row["id"]) == num2)
					{
						flag = true;
					}
					num++;
				}
			}
			if (num > 0)
			{
				dbStore.Save();
			}
			if (flag)
			{
				LiveWarp(num2);
			}
			return num;
		});
	}

	public static void ChangeCharacterName(long id, string newName)
	{
		AdminMainThread.Run(() =>
		{
			DbStore dbStore = RequireDb();
			if (!CharNameRules.Valid(newName))
			{
				throw new AdminApiException("角色名必须是 " + 2 + "~" + 12 + " 位中文、英文或数字");
			}
			DbTable dbTable = dbStore.Table("characters");
			if (dbTable.Find(DbStore.Eq("id", id)) == null)
			{
				throw new AdminApiException("角色不存在: " + id);
			}
			foreach (DbRow row in dbTable.rows)
			{
				if (Convert.ToInt64(row["id"]) != id && string.Equals(DbStore.Str(row, "name"), newName, StringComparison.OrdinalIgnoreCase))
				{
					throw new AdminApiException("角色名已被占用: " + newName);
				}
			}
			dbTable.Update(DbStore.Eq("id", id), new DbRow { ["name"] = newName });
			dbStore.Save();
			if (CurrentCharId() == id)
			{
				LiveRename(id, newName);
			}
		});
	}

	public static List<object> InventoryTypes()
	{
		List<object> list = new List<object>();
		string[] array = new string[5] { "装备", "消耗", "设置", "其他", "现金" };
		for (int i = 1; i <= array.Length; i++)
		{
			list.Add(AdminJson.Obj("inventoryType", i, "name", array[i - 1]));
		}
		return list;
	}

	public static Dictionary<string, object> InventoryCharacterPage(long characterId, string characterName, long accountId, int page, int size)
	{
		return AdminMainThread.Run(() =>
		{
			DbStore db = Db;
			List<DbRow> list = new List<DbRow>();
			if (db != null)
			{
				foreach (DbRow row in db.Table("characters").rows)
				{
					if ((characterId <= 0 || Convert.ToInt64(row["id"]) == characterId) && (string.IsNullOrEmpty(characterName) || DbStore.Str(row, "name").IndexOf(characterName, StringComparison.OrdinalIgnoreCase) >= 0) && (accountId <= 0 || DbStore.Long(row, "accountid", 0L) == accountId))
					{
						list.Add(row);
					}
				}
			}
			list.Sort((DbRow a, DbRow b) => Convert.ToInt64(b["id"]).CompareTo(Convert.ToInt64(a["id"])));
			int num = ((page <= 0) ? 1 : page);
			int num2 = ((size <= 0) ? 20 : size);
			List<object> list2 = new List<object>();
			for (int num3 = (num - 1) * num2; num3 < list.Count && num3 < num * num2; num3++)
			{
				Dictionary<string, object> dictionary = ToDto(list[num3]);
				dictionary["jobName"] = SafeJobName(DbStore.Int(list[num3], "job"));
				list2.Add(dictionary);
			}
			return AdminJson.Page(list2, num, num2, list.Count);
		});
	}

	public static Dictionary<string, object> InventoryPage(long characterId, long accountId, int inventoryType, int page, int size)
	{
		return AdminMainThread.Run(() =>
		{
			DbStore db = Db;
			List<DbRow> list = new List<DbRow>();
			if (db != null)
			{
				foreach (DbRow row in db.Table("inventoryitems").rows)
				{
					if ((characterId <= 0 || DbStore.Long(row, "characterid", 0L) == characterId) && (characterId > 0 || accountId <= 0 || DbStore.Long(row, "accountid", 0L) == accountId) && (inventoryType <= 0 || DbStore.Int(row, "inventorytype") == inventoryType))
					{
						list.Add(row);
					}
				}
			}
			list.Sort((DbRow a, DbRow b) =>
			{
				int num4 = DbStore.Int(a, "inventorytype").CompareTo(DbStore.Int(b, "inventorytype"));
				return (num4 == 0) ? DbStore.Int(a, "position").CompareTo(DbStore.Int(b, "position")) : num4;
			});
			int num = ((page <= 0) ? 1 : page);
			int num2 = ((size <= 0) ? 20 : size);
			List<object> list2 = new List<object>();
			for (int num3 = (num - 1) * num2; num3 < list.Count && num3 < num * num2; num3++)
			{
				list2.Add(InventoryDto(db, list[num3]));
			}
			return AdminJson.Page(list2, num, num2, list.Count);
		});
	}

	private static Dictionary<string, object> InventoryDto(DbStore db, DbRow row)
	{
		long num = DbStore.Long(row, "inventoryitemid", 0L);
		Dictionary<string, object> dictionary = new Dictionary<string, object>
		{
			{ "id", num },
			{
				"characterId",
				DbStore.Long(row, "characterid", 0L)
			},
			{
				"accountId",
				DbStore.Long(row, "accountid", 0L)
			},
			{
				"itemId",
				DbStore.Long(row, "itemid", 0L)
			},
			{
				"itemType",
				DbStore.Int(row, "type")
			},
			{
				"inventoryType",
				DbStore.Int(row, "inventorytype")
			},
			{
				"position",
				DbStore.Int(row, "position")
			},
			{
				"quantity",
				DbStore.Long(row, "quantity", 0L)
			},
			{
				"owner",
				DbStore.Str(row, "owner")
			},
			{
				"petId",
				DbStore.Long(row, "petid", 0L)
			},
			{
				"flag",
				DbStore.Int(row, "flag")
			},
			{
				"expiration",
				DbStore.Long(row, "expiration", 0L)
			},
			{
				"giftFrom",
				DbStore.Str(row, "giftFrom")
			}
		};
		DbRow dbRow = (db.HasTable("inventoryequipment") ? db.Table("inventoryequipment").Find(DbStore.Eq("inventoryitemid", num)) : null);
		dictionary["equipment"] = dbRow != null;
		if (dbRow != null)
		{
			dictionary["inventoryEquipment"] = AdminJson.Obj("id", Convert.ToInt64(dbRow["inventoryequipmentid"]), "inventoryItemId", num, "upgradeSlots", DbStore.Int(dbRow, "upgradeslots"), "level", DbStore.Int(dbRow, "level"), "attStr", DbStore.Int(dbRow, "str"), "attDex", DbStore.Int(dbRow, "dex"), "attInt", DbStore.Int(dbRow, "int"), "attLuk", DbStore.Int(dbRow, "luk"), "hp", DbStore.Int(dbRow, "hp"), "mp", DbStore.Int(dbRow, "mp"), "patk", DbStore.Int(dbRow, "watk"), "matk", DbStore.Int(dbRow, "matk"), "pdef", DbStore.Int(dbRow, "wdef"), "mdef", DbStore.Int(dbRow, "mdef"), "acc", DbStore.Int(dbRow, "acc"), "avoid", DbStore.Int(dbRow, "avoid"), "hands", DbStore.Int(dbRow, "hands"), "speed", DbStore.Int(dbRow, "speed"), "jump", DbStore.Int(dbRow, "jump"), "locked", DbStore.Int(dbRow, "locked"), "vicious", DbStore.Int(dbRow, "vicious"), "itemLevel", DbStore.Int(dbRow, "itemlevel"), "itemExp", DbStore.Int(dbRow, "itemexp"), "ringId", DbStore.Int(dbRow, "ringid"));
		}
		return dictionary;
	}

	public static void UpdateInventory(Dictionary<string, object> form)
	{
		AdminMainThread.Run(() =>
		{
			DbStore dbStore = RequireDb();
			long num = AdminJson.GetLong(form, "id", 0L);
			DbTable dbTable = dbStore.Table("inventoryitems");
			long num2 = DbStore.Long(dbTable.Find(DbStore.Eq("inventoryitemid", num)) ?? throw new AdminApiException("道具不存在: " + num), "characterid", 0L);
			DbRow dbRow = new DbRow();
			Copy(form, dbRow, "itemId", "itemid");
			Copy(form, dbRow, "inventoryType", "inventorytype");
			Copy(form, dbRow, "position", "position");
			Copy(form, dbRow, "quantity", "quantity");
			Copy(form, dbRow, "owner", "owner");
			Copy(form, dbRow, "flag", "flag");
			Copy(form, dbRow, "expiration", "expiration");
			Copy(form, dbRow, "giftFrom", "giftFrom");
			if (dbRow.Count > 0)
			{
				dbTable.Update(DbStore.Eq("inventoryitemid", num), dbRow);
			}
			Dictionary<string, object> dict = AdminJson.GetDict(form, "inventoryEquipment");
			if (dict != null && dbStore.HasTable("inventoryequipment"))
			{
				DbTable dbTable2 = dbStore.Table("inventoryequipment");
				DbRow dbRow2 = dbTable2.Find(DbStore.Eq("inventoryitemid", num));
				DbRow dbRow3 = new DbRow();
				Copy(dict, dbRow3, "upgradeSlots", "upgradeslots");
				Copy(dict, dbRow3, "level", "level");
				Copy(dict, dbRow3, "attStr", "str");
				Copy(dict, dbRow3, "attDex", "dex");
				Copy(dict, dbRow3, "attInt", "int");
				Copy(dict, dbRow3, "attLuk", "luk");
				Copy(dict, dbRow3, "hp", "hp");
				Copy(dict, dbRow3, "mp", "mp");
				Copy(dict, dbRow3, "patk", "watk");
				Copy(dict, dbRow3, "matk", "matk");
				Copy(dict, dbRow3, "pdef", "wdef");
				Copy(dict, dbRow3, "mdef", "mdef");
				Copy(dict, dbRow3, "acc", "acc");
				Copy(dict, dbRow3, "avoid", "avoid");
				Copy(dict, dbRow3, "hands", "hands");
				Copy(dict, dbRow3, "speed", "speed");
				Copy(dict, dbRow3, "jump", "jump");
				Copy(dict, dbRow3, "locked", "locked");
				Copy(dict, dbRow3, "vicious", "vicious");
				Copy(dict, dbRow3, "itemLevel", "itemlevel");
				Copy(dict, dbRow3, "itemExp", "itemexp");
				if (dbRow2 != null && dbRow3.Count > 0)
				{
					dbTable2.Update(DbStore.Eq("inventoryitemid", num), dbRow3);
				}
				else if (dbRow2 == null)
				{
					dbRow3["inventoryitemid"] = num;
					dbTable2.Insert(dbRow3);
				}
			}
			dbStore.Save();
			if (num2 > 0)
			{
				LiveInventory(dbStore, num2, num, deleted: false);
			}
		});
	}

	public static void DeleteInventory(Dictionary<string, object> form)
	{
		AdminMainThread.Run(() =>
		{
			DbStore dbStore = RequireDb();
			long num = AdminJson.GetLong(form, "id", 0L);
			if (num <= 0)
			{
				throw new AdminApiException("缺少道具 id");
			}
			DbRow dbRow = dbStore.Table("inventoryitems").Find(DbStore.Eq("inventoryitemid", num));
			long num2 = ((dbRow != null) ? DbStore.Long(dbRow, "characterid", 0L) : 0);
			if (dbStore.HasTable("inventoryequipment"))
			{
				dbStore.Table("inventoryequipment").Delete(DbStore.Eq("inventoryitemid", num));
			}
			dbStore.Table("inventoryitems").Delete(DbStore.Eq("inventoryitemid", num));
			dbStore.Save();
			if (num2 > 0)
			{
				LiveInventory(dbStore, num2, num, deleted: true);
			}
		});
	}

	public static Dictionary<string, object> ConfigTypeList()
	{
		return AdminMainThread.Run(() =>
		{
			List<object> list = new List<object>();
			List<object> list2 = new List<object>();
			DbStore db = Db;
			if (db != null)
			{
				foreach (DbRow row in db.Table("game_config").rows)
				{
					string text = DbStore.Str(row, "config_type");
					if (!string.IsNullOrEmpty(text) && !list.Contains(text))
					{
						list.Add(text);
					}
					string text2 = DbStore.Str(row, "config_sub_type");
					if (!string.IsNullOrEmpty(text2) && !list2.Contains(text2))
					{
						list2.Add(text2);
					}
				}
			}
			return AdminJson.Obj("types", list, "subTypes", list2);
		});
	}

	public static Dictionary<string, object> ConfigPage(string type, string subType, string filter, int page, int size)
	{
		return AdminMainThread.Run(() =>
		{
			DbStore db = Db;
			List<DbRow> list = new List<DbRow>();
			if (db != null)
			{
				foreach (DbRow row in db.Table("game_config").rows)
				{
					if ((string.IsNullOrEmpty(type) || !(DbStore.Str(row, "config_type") != type)) && (string.IsNullOrEmpty(subType) || !(DbStore.Str(row, "config_sub_type") != subType)) && (string.IsNullOrEmpty(filter) || DbStore.Str(row, "config_code").IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 || DbStore.Str(row, "config_desc").IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 || DbStore.Str(row, "config_value").IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0))
					{
						list.Add(row);
					}
				}
			}
			list.Sort((DbRow a, DbRow b) => Convert.ToInt64(a["id"]).CompareTo(Convert.ToInt64(b["id"])));
			int num = ((page <= 0) ? 1 : page);
			int num2 = ((size <= 0) ? 20 : size);
			List<object> list2 = new List<object>();
			for (int num3 = (num - 1) * num2; num3 < list.Count && num3 < num * num2; num3++)
			{
				list2.Add(ToDto(list[num3]));
			}
			return AdminJson.Page(list2, num, num2, list.Count);
		});
	}

	public static void AddConfig(Dictionary<string, object> form)
	{
		AdminMainThread.Run(() =>
		{
			DbStore dbStore = RequireDb();
			DbRow dbRow = new DbRow
			{
				["config_type"] = AdminJson.GetString(form, "configType"),
				["config_sub_type"] = AdminJson.GetString(form, "configSubType"),
				["config_clazz"] = AdminJson.GetString(form, "configClazz", "java.lang.String"),
				["config_code"] = AdminJson.GetString(form, "configCode"),
				["config_value"] = AdminJson.GetString(form, "configValue"),
				["config_desc"] = AdminJson.GetString(form, "configDesc"),
				["update_time"] = DbStore.NowText()
			};
			if (string.IsNullOrEmpty(Convert.ToString(dbRow["config_code"])))
			{
				throw new AdminApiException("参数名不能为空");
			}
			dbStore.Table("game_config").Insert(dbRow);
			dbStore.Save();
		});
	}

	public static void UpdateConfig(Dictionary<string, object> form)
	{
		AdminMainThread.Run(() =>
		{
			DbStore dbStore = RequireDb();
			long num = AdminJson.GetLong(form, "id", 0L);
			DbTable dbTable = dbStore.Table("game_config");
			if (dbTable.Find(DbStore.Eq("id", num)) == null)
			{
				throw new AdminApiException("参数不存在: " + num);
			}
			DbRow dbRow = new DbRow();
			Copy(form, dbRow, "configType", "config_type");
			Copy(form, dbRow, "configSubType", "config_sub_type");
			Copy(form, dbRow, "configClazz", "config_clazz");
			Copy(form, dbRow, "configCode", "config_code");
			Copy(form, dbRow, "configValue", "config_value");
			Copy(form, dbRow, "configDesc", "config_desc");
			dbRow["update_time"] = DbStore.NowText();
			dbTable.Update(DbStore.Eq("id", num), dbRow);
			dbStore.Save();
		});
	}

	public static void DeleteConfig(long id)
	{
		AdminMainThread.Run(() =>
		{
			DbStore dbStore = RequireDb();
			dbStore.Table("game_config").Delete(DbStore.Eq("id", id));
			dbStore.Save();
		});
	}

	public static int DeleteConfigList(IEnumerable<object> ids)
	{
		return AdminMainThread.Run(() =>
		{
			DbStore dbStore = RequireDb();
			int num = 0;
			foreach (object id in ids)
			{
				long num2 = Convert.ToInt64(id, CultureInfo.InvariantCulture);
				num += dbStore.Table("game_config").Delete(DbStore.Eq("id", num2));
			}
			dbStore.Save();
			return num;
		});
	}

	public static DbStore RequireDb()
	{
		return Db ?? throw new AdminApiException("服务端未启动（请先进入游戏）");
	}

	private static void Live(Action action)
	{
		try
		{
			action();
		}
		catch (Exception ex)
		{
			Debug.LogWarning((object)("[Admin] 实时同步失败: " + ex.Message));
		}
	}

	private static void LiveWarp(long characterId)
	{
		Live(() =>
		{
			AdminLive.WarpToHenesys(characterId);
		});
	}

	public static void LiveWarpTo(long characterId, long mapId)
	{
		Live(() =>
		{
			AdminLive.WarpTo(characterId, mapId);
		});
	}

	private static void LiveRename(long characterId, string newName)
	{
		Live(() =>
		{
			AdminLive.Rename(characterId, newName);
		});
	}

	public static void LiveInventory(DbStore db, long characterId, long itemRowId, bool deleted)
	{
		Live(() =>
		{
			AdminLive.SyncInventory(db, characterId, itemRowId, deleted);
		});
	}

	private static void LiveKick(long accountId, string reason)
	{
		Live(() =>
		{
			AdminLive.KickAccount(accountId, reason);
		});
	}

	private static long CurrentCharId()
	{
		try
		{
			return (CurrentCharacterId != null) ? CurrentCharacterId() : 0;
		}
		catch (Exception)
		{
			return 0L;
		}
	}
}
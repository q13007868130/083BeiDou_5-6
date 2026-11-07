using System;

public static class CharFactory
{
	public sealed class Result
	{
		public int code;

		public int characterId;

		public string error;
	}

	public const int JobGroupCygnus = 0;

	public const int JobGroupAdventurer = 1;

	public const int JobGroupAran = 2;

	public const int MapBeidouBeginner = 4;

	public const int MapAranTutorialStart = 914000000;

	public const int ItemLegendsGuide = 4161048;

	public const int MapMushroomTown = 10000;

	public const int MapStartingNoblesse = 130030000;

	public const int ItemBeginnersGuide = 4161001;

	public const int ItemNoblesseGuide = 4161047;

	public const int EquipPosTop = -5;

	public const int EquipPosBottom = -6;

	public const int EquipPosShoes = -7;

	public const int EquipPosWeapon = -11;

	public const int Ok = 0;

	public const int ErrNoSlot = -3;

	public const int ErrBadName = -1;

	public const int ErrInvalid = -2;

	public const int ErrJobDisabled = -4;

	public static bool IsDefaultFace(int job, int gender, int face)
	{
		if (job == 2)
		{
			if (gender != 0)
			{
				if (face != 21700 && face != 21201)
				{
					return face == 21002;
				}
				return true;
			}
			if (face != 20100 && face != 20401)
			{
				return face == 20402;
			}
			return true;
		}
		if (gender != 0)
		{
			if (face != 21000 && face != 21001)
			{
				return face == 21002;
			}
			return true;
		}
		if (face != 20000 && face != 20001)
		{
			return face == 20002;
		}
		return true;
	}

	public static bool IsDefaultHair(int gender, int hair)
	{
		if (gender != 0)
		{
			if (hair != 31000 && hair != 31040)
			{
				return hair == 31050;
			}
			return true;
		}
		if (hair != 30290 && hair != 30300)
		{
			return hair == 30310;
		}
		return true;
	}

	public static bool IsDefaultHairColor(int hairColor)
	{
		if (hairColor != 0 && hairColor != 2 && hairColor != 3)
		{
			return hairColor == 7;
		}
		return true;
	}

	public static bool IsDefaultSkinColor(int skinColor)
	{
		if (skinColor >= 0)
		{
			return skinColor < 4;
		}
		return false;
	}

	public static bool IsDefaultTop(int job, int gender, int top)
	{
		if (job == 2)
		{
			return top == 1042167;
		}
		if (gender != 0)
		{
			if (top != 1041002 && top != 1041006 && top != 1041010)
			{
				return top == 1041011;
			}
			return true;
		}
		if (top != 1040002 && top != 1040006)
		{
			return top == 1040010;
		}
		return true;
	}

	public static bool IsDefaultBottom(int job, int gender, int bottom)
	{
		if (job == 2)
		{
			return bottom == 1062115;
		}
		if (gender != 0)
		{
			if (bottom != 1061002)
			{
				return bottom == 1061008;
			}
			return true;
		}
		if (bottom != 1060002)
		{
			return bottom == 1060006;
		}
		return true;
	}

	public static bool IsDefaultShoes(int job, int shoes)
	{
		if (job == 2)
		{
			return shoes == 1072383;
		}
		if (shoes != 1072001 && shoes != 1072005 && shoes != 1072037)
		{
			return shoes == 1072038;
		}
		return true;
	}

	public static bool IsDefaultWeapon(int job, int weapon)
	{
		if (job == 2)
		{
			return weapon == 1442079;
		}
		if (weapon != 1302000 && weapon != 1322005)
		{
			return weapon == 1312004;
		}
		return true;
	}

	public static Result CreateCharacter(DbStore db, int accountId, int world, string name, int job, int face, int hair, int hairColor, int skinColor, int top, int bottom, int shoes, int weapon, int gender)
	{
		if (job == 0 && !GameConfigDao.GetBool(db, "enable_knights_of_cygnus", fallback: true))
		{
			return Fail(-4, "骑士团创建已关闭");
		}
		if (job == 1 && !GameConfigDao.GetBool(db, "enable_adventurers", fallback: true))
		{
			return Fail(-4, "冒险家创建已关闭");
		}
		if (job != 0 && job != 1 && job != 2)
		{
			return Fail(-2, "未知职业群");
		}
		if (!IsDefaultFace(job, gender, face))
		{
			return Fail(-2, "非默认脸型");
		}
		if (!IsDefaultHair(gender, hair))
		{
			return Fail(-2, "非默认发型");
		}
		if (!IsDefaultHairColor(hairColor))
		{
			return Fail(-2, "非默认发色");
		}
		if (!IsDefaultSkinColor(skinColor))
		{
			return Fail(-2, "非默认肤色");
		}
		if (!IsDefaultTop(job, gender, top))
		{
			return Fail(-2, "非默认上衣");
		}
		if (!IsDefaultBottom(job, gender, bottom))
		{
			return Fail(-2, "非默认下装");
		}
		if (!IsDefaultShoes(job, shoes))
		{
			return Fail(-2, "非默认鞋子");
		}
		if (!IsDefaultWeapon(job, weapon))
		{
			return Fail(-2, "非默认武器");
		}
		if (CharactersDao.ByAccount(db, accountId, world).Count >= AccountsDao.CharacterSlots(db, accountId))
		{
			return Fail(-3, "角色位已满");
		}
		if (!CharactersDao.CanCreateChar(name))
		{
			return Fail(-1, "名字需 2-12 位中文、英文或数字");
		}
		if (CharactersDao.NameTaken(db, name))
		{
			return Fail(-1, "名字已被使用");
		}
		bool flag = job == 0;
		bool flag2 = job == 2;
		int num = (GameConfigDao.GetBool(db, "use_beidou_beginner_map") ? 4 : 10000);
		CharRecord c = new CharRecord
		{
			accountid = accountId,
			gm = AccountsDao.DefaultGmLevel(db, accountId),
			world = world,
			name = name,
			level = 1,
			exp = 0L,
			str = 12,
			dex = 5,
			int_ = 4,
			luk = 4,
			hp = 50,
			maxhp = 50,
			mp = 5,
			maxmp = 5,
			job = (flag ? 1000 : (flag2 ? 2000 : 0)),
			skincolor = skinColor,
			gender = gender,
			hair = hair + hairColor,
			face = face,
			ap = 0,
			map = (flag ? 130030000 : (flag2 ? 914000000 : num)),
			spawnpoint = 0,
			equipslots = 24,
			useslots = 24,
			setupslots = 24,
			etcslots = 24,
			fishLevel = 1
		};
		db.BeginTransaction();
		try
		{
			long num2 = CharactersDao.Insert(db, c);
			KeymapDao.SeedDefaults(db, (int)num2);
			QuickslotDao.Save(db, accountId, 0L);
			CharService.AddItem(db, (int)num2, top, 1, -5, 1);
			CharService.AddItem(db, (int)num2, bottom, 1, -6, 1);
			CharService.AddItem(db, (int)num2, shoes, 1, -7, 1);
			CharService.AddItem(db, (int)num2, weapon, 1, -11, 1);
			DbStore db2 = db;
			int characterId = (int)num2;
			int itemId;
			if (flag)
			{
				itemId = 4161047;
			}
			else
			{
				itemId = (flag2 ? 4161048 : 4161001);
			}
			InventoryDao.InsertItem(db2, characterId, itemId, 4, 1, 1);
			db.Commit();
			return new Result
			{
				code = 0,
				characterId = (int)num2
			};
		}
		catch (Exception ex)
		{
			db.Rollback();
			return Fail(-2, ex.Message);
		}
	}

	private static Result Fail(int code, string error)
	{
		return new Result
		{
			code = code,
			error = error
		};
	}
}

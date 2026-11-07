using System;

public static class NativeDamageCapService
{
	public static int Increment(int id)
	{
		return id switch
		{
			2431152 => 300000, 
			2431153 => 500000, 
			2431154 => 2000000, 
			2431155 => 10000000, 
			2431156 => 100000000, 
			2431157 => 500000000, 
			_ => 0, 
		};
	}

	public static long Resolve(long current, int increment, bool success)
	{
		current = Math.Max(19999999L, current);
		if (!success || current + increment < current)
		{
			return current;
		}
		return current + increment;
	}

	public static InventoryChangeResponse Use(DbStore db, CharRecord character, int id, Random random = null)
	{
		int num = Increment(id);
		if (num == 0 || InventoryDao.CountOf(db, (int)character.id, id) < 1)
		{
			return new InventoryChangeResponse
			{
				ok = false,
				error = "突破石不存在，无法使用。"
			};
		}
		if (random == null)
		{
			random = new Random();
		}
		bool flag = random.Next(2) != 0;
		long num2 = Math.Max(19999999L, character.damageCap);
		long num3 = (character.damageCap = Resolve(num2, num, flag));
		CharactersDao.Save(db, character);
		InventoryDao.ConsumeItem(db, (int)character.id, id, 1);
		db.Save();
		string message;
		if (flag)
		{
			message = ((num2 == num3) ? "突破成功，但增加后会超出 long 范围，当前伤害上限未变化。" : ("突破成功，伤害上限提升至 " + num3 + "。"));
		}
		else
		{
			message = "突破失败，当前伤害上限仍为 " + num3 + "。";
		}
		return new InventoryChangeResponse
		{
			inventory = CharService.Load(db, (int)character.id).inventory,
			damageCap = num3,
			message = message
		};
	}
}

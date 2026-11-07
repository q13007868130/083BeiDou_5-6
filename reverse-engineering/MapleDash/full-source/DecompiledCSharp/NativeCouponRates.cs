using System;
using System.Collections.Generic;

public static class NativeCouponRates
{
	private static List<object> rules;

	public static void Apply(DbStore db, int cid, CommunityResponse rates, DateTime now)
	{
		if (rules == null)
		{
			rules = (List<object>)((Dictionary<string, object>)MiniJsonDb.Parse(PlatformRes.LoadText("ItemCouponSchedules")))["rules"];
		}
		int num = 1 << (int)(now.DayOfWeek + 1);
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		Dictionary<int, int> dictionary2 = new Dictionary<int, int>();
		long num2 = new DateTimeOffset(now).ToUnixTimeMilliseconds();
		foreach (DbRow item in InventoryDao.ItemsByCharacter(db, cid))
		{
			int num3 = DbStore.Int(item, "itemid");
			if (DbStore.Int(item, "position") <= 0 || (DbStore.Long(item, "expiration", 0L) > 0 && DbStore.Long(item, "expiration", 0L) <= num2) || DbStore.Int(item, "quantity") <= 0)
			{
				continue;
			}
			foreach (Dictionary<string, object> rule in rules)
			{
				if (Convert.ToInt32(rule["item"]) == num3 && (Convert.ToInt32(rule["days"]) & num) == num && Convert.ToInt32(rule["start"]) <= now.Hour && Convert.ToInt32(rule["end"]) > now.Hour)
				{
					dictionary[num3] = Convert.ToInt32(rule["rate"]);
					dictionary2[num3] = ((!dictionary2.TryGetValue(num3, out var value)) ? 1 : (value + 1));
					break;
				}
			}
		}
		bool flag = GameConfigDao.GetBool(db, "use_stack_coupon_rates");
		int num4 = 1;
		int num5 = 1;
		foreach (KeyValuePair<int, int> item2 in dictionary)
		{
			if (GameConfigDao.GetBool(db, "show_coupon_buff", fallback: true))
			{
				rates.couponBuffs.Add(item2.Key);
			}
			if (item2.Key / 1000 == 5211)
			{
				num4 = (flag ? (num4 * item2.Value * dictionary2[item2.Key]) : Math.Max(num4, item2.Value));
			}
			else
			{
				num5 = (flag ? (num5 * item2.Value * dictionary2[item2.Key]) : Math.Max(num5, item2.Value));
			}
		}
		rates.expRate *= num4;
		rates.dropRate *= num5;
		rates.mesoRate *= num5;
		rates.familyDropRate *= num5;
	}
}

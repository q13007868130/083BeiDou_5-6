using MapleDash.Wz;
using UnityEngine;

public static class NativeCatchItems
{
	private static double lastAttempt = -100.0;

	private static int owner;

	public static string Use(int id, int position)
	{
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_0446: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ee: Unknown result type (might be due to invalid IL or missing references)
		PlayerCtrl playerCtrl = GameLoop.I?.player;
		ImgNode imgNode = NativeItemSource.Node(id)?.Get("info");
		if ((Object)(object)playerCtrl == (Object)null || imgNode == null)
		{
			return "当前无法使用捕捉道具";
		}
		int num = imgNode.Int("mob");
		int num2 = imgNode.Int("create");
		int num3 = imgNode.Int("mobHP");
		int num4 = imgNode.Int("useDelay");
		if (owner != GameSession.CharacterId)
		{
			owner = GameSession.CharacterId;
			lastAttempt = -100.0;
		}
		switch (id)
		{
		case 2270000:
			num = 9300101;
			num2 = 1902000;
			num3 = 0;
			break;
		case 2270001:
			num = 9500197;
			num2 = 4031830;
			num3 = 40;
			num4 = 1000;
			break;
		case 2270002:
			num = 9300157;
			num2 = 4031868;
			num3 = 40;
			num4 = 800;
			break;
		case 2270003:
			num = 9500320;
			num2 = 4031887;
			num3 = 40;
			break;
		case 2270004:
			num = 9300175;
			num2 = 4001169;
			num3 = 40;
			break;
		case 2270005:
			num = 9300187;
			num2 = 2109001;
			num3 = 30;
			break;
		case 2270006:
			num = 9300189;
			num2 = 2109002;
			num3 = 30;
			break;
		case 2270007:
			num = 9300191;
			num2 = 2109003;
			num3 = 30;
			break;
		case 2270008:
			num = 9500336;
			num2 = 2022323;
			num3 = 0;
			num4 = 3000;
			break;
		default:
			if (num4 == 0 || num3 == 0)
			{
				return null;
			}
			break;
		}
		EnemyUnit enemyUnit = null;
		float num5 = float.MaxValue;
		foreach (EnemyUnit item in EnemyUnit.All)
		{
			if (!item.dead && item.fieldMode && item.fieldMobId == num)
			{
				Vector2 val = (Vector2.op_Implicit(((Component)item).transform.position) - playerCtrl.FeetPos) * 40f;
				if (!(val.x < (float)imgNode.Int("left")) && !(val.x > (float)imgNode.Int("right")) && !(0f - val.y < (float)imgNode.Int("top")) && !(0f - val.y > (float)imgNode.Int("bottom")) && val.sqrMagnitude < num5)
				{
					enemyUnit = item;
					num5 = val.sqrMagnitude;
				}
			}
		}
		bool flag = id == 2270001 || id == 2270002 || id >= 2270008;
		if ((Object)(object)enemyUnit == (Object)null || num2 <= 0 || (flag && NativeBuffs.Now - lastAttempt <= (double)num4 / 1000.0))
		{
			return null;
		}
		long num6 = enemyUnit.NativeController?.CurrentHp ?? enemyUnit.hp;
		long num7 = enemyUnit.NativeController?.MaximumHp ?? enemyUnit.hpMax;
		if (num3 > 0 && num6 >= num7 / ((id >= 2270009) ? 100 : 10) * ((id >= 2270009) ? num3 : (num3 / 10)))
		{
			if (id == 2270001 || id >= 2270009)
			{
				lastAttempt = NativeBuffs.Now;
			}
			return "怪物体力还太多，无法捕捉";
		}
		if (id == 2270002)
		{
			if (!GameSession.Net.ChangeInventory(new InventoryChangeRequest
			{
				characterId = GameSession.CharacterId,
				checkItems = new int[1] { num2 },
				checkQuantities = new int[1] { 1 }
			}).ok)
			{
				return "请先在其他栏保留空位";
			}
			lastAttempt = NativeBuffs.Now;
			if (Random.value >= 0.5f)
			{
				NativeImgAnimation.Spawn("Effect/ItemEff.img/" + id + "/fail", null, ((Component)enemyUnit).transform.position, enemyUnit.BodySortingOrder + 1);
				return null;
			}
		}
		if (id == 2270008)
		{
			lastAttempt = NativeBuffs.Now;
		}
		if (!Profile.RemoveItem(id, 1, position))
		{
			return "背包里没有这个物品";
		}
		NativeImgAnimation.Spawn("Effect/ItemEff.img/" + id, null, ((Component)enemyUnit).transform.position, enemyUnit.BodySortingOrder + 1);
		enemyUnit.ExpireNative(1);
		Profile.AddItem(num2, 1);
		return null;
	}
}

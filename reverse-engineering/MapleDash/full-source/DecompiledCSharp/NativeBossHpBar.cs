using System;
using UnityEngine;

public static class NativeBossHpBar
{
	private const string Root = "wz://UI/UIWindow.img/MobGage/";

	public static void Draw()
	{
		EnemyUnit enemyUnit = null;
		foreach (EnemyUnit item in EnemyUnit.All)
		{
			if ((Object)(object)item != (Object)null && item.fieldMode && !item.dead && item.IsBoss && Art.Num(item.CombatStats, "hpTagColor") > 0f)
			{
				enemyUnit = item;
			}
		}
		float s;
		float x;
		if (!((Object)(object)enemyUnit == (Object)null))
		{
			s = BottomBar.S;
			x = (UiKit.W - 800f * s) * 0.5f;
			Image("backgrnd", 0f, 0f);
			Image("backgrnd2", 35f, 0f);
			Image("backgrnd3", 39f, 0f, 758f);
			Image("backgrnd4", 797f, 0f);
			int num = enemyUnit.fieldMobId;
			if (num == 8880700 || num == 8880803)
			{
				num = 8870000;
			}
			Image("Mob/" + num, 5f, 6f);
			int num2 = (int)Art.Num(enemyUnit.CombatStats, "hpTagColor");
			int num3 = (int)Art.Num(enemyUnit.CombatStats, "hpTagBgcolor");
			long num4 = (((Object)(object)enemyUnit.NativeController != (Object)null) ? enemyUnit.NativeController.CurrentHp : enemyUnit.hp);
			long num5 = (((Object)(object)enemyUnit.NativeController != (Object)null) ? enemyUnit.NativeController.MaximumHp : enemyUnit.hpMax);
			int num6 = ((num5 > 0) ? ((int)Math.Min(758.0, Math.Max(0.0, 758.0 * (double)num4 / (double)num5))) : 0);
			Image("Gage/" + num2 + "/0", 38f, 4f);
			if (num6 > 0)
			{
				Image("Gage/" + num2 + "/1", 39f, 4f, num6);
			}
			if (num6 < 758)
			{
				Image("Gage/" + num3 + "/1", 39 + num6, 4f, 758 - num6);
			}
			Image("Gage/" + ((num6 == 758) ? num2 : num3) + "/0", 797f, 4f);
		}
		void Image(string node, float px, float py, float width = -1f)
		{
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			Texture2D val = Art.Tex("wz://UI/UIWindow.img/MobGage/" + node);
			if (!((Object)(object)val == (Object)null))
			{
				GUI.DrawTexture(new Rect(x + px * s, py * s, ((width < 0f) ? ((float)((Texture)val).width) : width) * s, (float)((Texture)val).height * s), (Texture)(object)val);
			}
		}
	}
}

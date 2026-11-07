using System;
using System.Collections.Generic;
using UnityEngine;

public static class NativeSoulWeapon
{
	public static bool IsSkill(int id)
	{
		if (id >= 80012853)
		{
			return id <= 80012856;
		}
		return false;
	}

	public static int LevelOf(int id)
	{
		FullCharacter character = GameSession.Character;
		SoulWeaponWz2State soulWeaponWz2State = character?.soulWeapon;
		SoulGrowth soulGrowth = soulWeaponWz2State?.Active;
		if (soulGrowth == null || !soulWeaponWz2State.Equipped(character) || SoulWeaponWz2Catalog.Int(SoulWeaponWz2Catalog.Grade(soulGrowth.grade), "ActiveSkillID") != id)
		{
			return 0;
		}
		return SoulWeaponWz2Catalog.Int(SoulWeaponWz2Catalog.Grade(soulGrowth.grade), "ActiveSkillLevel");
	}

	public static SkillData Definition(int id)
	{
		Dictionary<string, object> d = SoulWeaponWz2Catalog.Map(SoulWeaponWz2Catalog.Map(SoulWeaponWz2Catalog.Data, "skills"), id.ToString());
		Dictionary<string, object> dictionary = SoulWeaponWz2Catalog.Map(d, "common");
		Dictionary<string, object> d2 = SoulWeaponWz2Catalog.Map(d, "string");
		SkillData skillData = new SkillData
		{
			id = id,
			job = 8001,
			maxLevel = SoulWeaponWz2Catalog.Int(dictionary, "maxLevel"),
			type = "buff",
			autoLearn = true,
			name = SoulWeaponWz2Catalog.Text(d2, "name", "灵魂的斗志"),
			desc = SoulWeaponWz2Catalog.Text(d2, "desc"),
			icon = "wz://Skill/800128.img/skill/" + id + "/icon"
		};
		for (int i = 1; i <= skillData.maxLevel; i++)
		{
			Dictionary<string, object> dictionary2 = new Dictionary<string, object>();
			foreach (KeyValuePair<string, object> item in dictionary)
			{
				if (item.Key != "maxLevel")
				{
					dictionary2[item.Key] = GrowthFormula.Evaluate(item.Value.ToString(), i);
				}
			}
			skillData.levels[i.ToString()] = dictionary2;
		}
		return skillData;
	}

	public static string Cast(int id)
	{
		PlayerCtrl playerCtrl = GameLoop.I?.player;
		if (!GameSession.InGame || (Object)(object)playerCtrl == (Object)null || playerCtrl.hp <= 0f || GameLoop.I.gameOver || GameLoop.I.state != GameLoop.GameState.Playing)
		{
			return "当前无法施放灵魂技能";
		}
		if (LevelOf(id) <= 0)
		{
			return "请装备已启用的灵魂武器";
		}
		SoulWeaponWz2State soulWeapon = GameSession.Character.soulWeapon;
		SoulResponse soulResponse = GameSession.Net.Soul(new SoulRequest
		{
			characterId = GameSession.CharacterId,
			operation = SoulOperation.CastModern,
			commit = true,
			revision = soulWeapon.revision
		});
		if (soulResponse.modern != null)
		{
			GameSession.Character.soulWeapon = soulResponse.modern;
		}
		if (!soulResponse.ok)
		{
			return soulResponse.error;
		}
		NativeGrowthStats.Invalidate();
		Tick();
		playerCtrl.PlayNativeSkill(id);
		playerCtrl.RefreshBuffAttributes();
		return null;
	}

	public static void Tick()
	{
		PlayerCtrl playerCtrl = GameLoop.I?.player;
		if ((Object)(object)playerCtrl == (Object)null)
		{
			return;
		}
		NativeBuffs nativeBuffs = ((Component)playerCtrl).GetComponent<NativeBuffs>();
		SoulWeaponWz2State soulWeaponWz2State = GameSession.Character?.soulWeapon;
		long num = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		for (int i = 80012853; i <= 80012856; i++)
		{
			if (soulWeaponWz2State != null && soulWeaponWz2State.activeUntil > num && LevelOf(i) > 0)
			{
				nativeBuffs = nativeBuffs ?? NativeBuffs.For(playerCtrl);
				if (!nativeBuffs.HasSource(i))
				{
					nativeBuffs.ApplySoulWeapon(i, LevelOf(i), (int)Math.Min(40000L, soulWeaponWz2State.activeUntil - num));
				}
			}
			else if (nativeBuffs != null && nativeBuffs.HasSource(i))
			{
				nativeBuffs.Cancel(i);
			}
		}
	}
}

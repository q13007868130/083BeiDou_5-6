using System;
using System.Collections.Generic;
using MapleDash.Wz;
using Server.Bot;
using UnityEngine;

public static class NativeItemUseEffects
{
	private static int banishCharacter;

	private static int banishMap = -1;

	private static int banishPortal = -1;

	private static long banishTime;

	public static void CashEquipment(int id, InventoryEntry target)
	{
		EquipPatchResponse equipPatchResponse = GameSession.Net.UpdateEquip(new EquipPatchRequest
		{
			characterId = GameSession.CharacterId,
			inventoryItemId = target.itemRowId,
			cashItemId = id
		});
		if (!equipPatchResponse.ok)
		{
			BottomBar.Toast(equipPatchResponse.error);
			return;
		}
		GameSession.Character.inventory = equipPatchResponse.inventory;
		GameSession.ApplyInventoryToProfile();
		GameLoop.I?.player?.RefreshBuffAttributes();
		Profile.Save();
		if (equipPatchResponse.rewardItemId != 0)
		{
			BottomBar.Toast("孵化成功：" + BotItemCatalog.Shared.Name(equipPatchResponse.rewardItemId) + " x" + equipPatchResponse.rewardQuantity);
		}
	}

	public static void Scroll(int id, InventoryEntry target)
	{
		if (id == 2049400 || id == 2049401)
		{
			InventoryEntry inventoryEntry = GameSession.Character.inventory.Find((InventoryEntry x) => x.itemId == id && x.position > 0);
			if (inventoryEntry != null)
			{
				NativeEnhancementWindow.Open(1, inventoryEntry);
				string text = NativeEnhancementWindow.Select(target);
				if (text != null)
				{
					BottomBar.Toast(text);
				}
			}
		}
		else if (id != 2040727 && id != 2041058 && (id < 2049000 || id > 2049003) && !NativeScrollService.IsHeartScroll(id) && Profile.ItemCount(2340000) > 0)
		{
			NativeUtilPrompt.Confirm("你想使用祝福卷轴吗？", (bool white) =>
			{
				ApplyScroll(id, target, white);
			});
		}
		else
		{
			ApplyScroll(id, target, white: false);
		}
	}

	private static void ApplyScroll(int id, InventoryEntry target, bool white)
	{
		List<int> list = new List<int>();
		ImgNode imgNode = NativeItemSource.Node(id)?.Get("req");
		if (imgNode != null)
		{
			foreach (ImgNode child in imgNode.Children)
			{
				list.Add(Convert.ToInt32(child.Value));
			}
		}
		EquipPatchResponse equipPatchResponse = GameSession.Net.UpdateEquip(new EquipPatchRequest
		{
			characterId = GameSession.CharacterId,
			inventoryItemId = target.itemRowId,
			scrollId = id,
			scrollRequirements = list.ToArray(),
			whiteScroll = white
		});
		if (!equipPatchResponse.ok)
		{
			NativeUtilPrompt.Notice(equipPatchResponse.error);
			return;
		}
		GameSession.Character.inventory = equipPatchResponse.inventory;
		GameSession.ApplyInventoryToProfile();
		GameLoop.I?.player?.RefreshBuffAttributes();
		Profile.Save();
		PlayScrollEffect(equipPatchResponse.scrollResult);
		string text;
		if (equipPatchResponse.scrollResult == "success")
		{
			text = "卷轴使用成功。";
		}
		else
		{
			text = ((equipPatchResponse.scrollResult == "curse") ? "卷轴失败，装备已被摧毁。" : "卷轴使用失败。");
		}
		BottomBar.Toast(text);
	}

	private static void PlayScrollEffect(string result)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		PlayerCtrl playerCtrl = GameLoop.I?.player;
		if (!((Object)(object)playerCtrl == (Object)null))
		{
			string text = ((result == "success") ? "Success" : "Failure");
			NativeImgAnimation.Spawn("Effect/BasicEff.img/Enchant/" + text, ((Component)playerCtrl).transform, Vector2.op_Implicit(playerCtrl.FeetPos), playerCtrl.SortingOrder + 1);
			AudioManager.Play("Game/Enchant" + text, 1f, pitchVary: false);
		}
	}

	public static string RewardBox(int id)
	{
		ImgNode imgNode = NativeItemSource.Node(id)?.Get("reward");
		if (imgNode == null)
		{
			return "源端缺少奖励数据";
		}
		int num = 0;
		foreach (ImgNode child in imgNode.Children)
		{
			num += (sbyte)child.Int("prob");
		}
		if (num <= 0)
		{
			return null;
		}
		foreach (ImgNode child2 in imgNode.Children)
		{
			int num2 = child2.Int("item");
			int num3 = (short)child2.Int("count");
			if (!GameSession.Net.ChangeInventory(new InventoryChangeRequest
			{
				characterId = GameSession.CharacterId,
				checkItems = new int[1] { num2 },
				checkQuantities = new int[1] { num3 }
			}).ok)
			{
				return "背包空间不足";
			}
			if (Random.Range(0, num) < (sbyte)child2.Int("prob"))
			{
				int num4 = child2.Int("period", -1);
				InventoryChangeResponse inventoryChangeResponse = GameSession.Net.ChangeInventory(new InventoryChangeRequest
				{
					characterId = GameSession.CharacterId,
					itemId = id,
					grantItemId = num2,
					grantQuantity = num3,
					grantMinutes = -1,
					consumeGrantSource = true,
					grantDurationMs = ((num2 / 1000000 == 1 && num4 != -1) ? ((long)num4 * 60L * 60 * 10) : (-1))
				});
				if (!inventoryChangeResponse.ok)
				{
					return inventoryChangeResponse.error;
				}
				GameSession.Character.inventory = inventoryChangeResponse.inventory;
				GameSession.ApplyInventoryToProfile();
				Profile.Save();
				string text = child2.Text("worldMsg");
				if (!string.IsNullOrEmpty(text))
				{
					NativeSystemMessage.Drop(6, text.Replace("/name", Profile.playerName).Replace("/item", WzNames.Item(num2)));
				}
				return null;
			}
		}
		return null;
	}

	public static string SummonBag(int id, int position)
	{
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		ImgNode imgNode = NativeItemSource.Node(id)?.Get("mob");
		if (imgNode == null)
		{
			return "源端道具缺少召唤怪物数据";
		}
		if (GameLoop.I?.field?.Map == null)
		{
			return "当前无法使用物品";
		}
		if (!Profile.RemoveItem(id, 1, position))
		{
			return "背包里没有这个物品";
		}
		foreach (ImgNode child in imgNode.Children)
		{
			if (Random.Range(0, 100) < child.Int("prob"))
			{
				GameLoop.I.field.SpawnNativeMob(child.Int("id"), Vector2.op_Implicit(GameLoop.I.player.FeetPos));
			}
		}
		return null;
	}

	public static string SkillBook(int id, int position)
	{
		ImgNode imgNode = NativeItemSource.Node(id)?.Get("info");
		int skill = 0;
		ImgNode imgNode2 = imgNode?.Get("skill");
		if (imgNode2 != null)
		{
			foreach (ImgNode child in imgNode2.Children)
			{
				int num = Convert.ToInt32(child.Value);
				if (num / 10000 == Profile.JobId)
				{
					skill = num;
					break;
				}
			}
		}
		if (skill == 0)
		{
			return "当前职业无法使用这本技能册";
		}
		int num2 = GameSession.Character.skills.Find((SkillEntry s) => s.skillId == skill)?.masterLevel ?? 0;
		if (SkillProgress.BaseLevelOf(skill) < imgNode.Int("reqSkillLevel") || num2 >= imgNode.Int("masterLevel"))
		{
			return "尚未满足技能等级要求，或技能上限已达到";
		}
		if (!Profile.RemoveItem(id, 1, position))
		{
			return "背包里没有这个物品";
		}
		int num3 = ((GameServer.Local == null) ? 1 : GameConfigDao.GetInt(GameServer.Local.Db, "scroll_chance_rolls", 1));
		bool flag = (double)Random.value >= Math.Pow(1.0 - (double)imgNode.Int("success") / 100.0, num3);
		if (flag)
		{
			SkillEntry skillEntry = GameSession.Character.skills.Find((SkillEntry s) => s.skillId == skill);
			if (skillEntry == null)
			{
				skillEntry = new SkillEntry
				{
					skillId = skill
				};
				GameSession.Character.skills.Add(skillEntry);
			}
			skillEntry.masterLevel = Math.Max(num2, imgNode.Int("masterLevel"));
			GameSession.SaveToDb();
		}
		BottomBar.Toast(flag ? "技能册使用成功。" : "技能册使用失败。");
		return null;
	}

	public static void RememberBanish()
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		FieldDirector fieldDirector = GameLoop.I?.field;
		if (fieldDirector?.Map != null)
		{
			banishCharacter = GameSession.CharacterId;
			banishMap = fieldDirector.Map.id;
			banishPortal = ClosestSpawn(fieldDirector.Map, Vector2.op_Implicit(GameLoop.I.player.FeetPos));
			banishTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		}
	}

	private static int ClosestSpawn(FieldMap map, Vector3 point)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		int result = -1;
		float num = float.PositiveInfinity;
		for (int i = 0; i < map.portals.Count; i++)
		{
			FieldPortal fieldPortal = map.portals[i];
			if (fieldPortal.IsSpawn)
			{
				Vector2 val = new Vector2(fieldPortal.x / 40f, (0f - fieldPortal.y) / 40f) - Vector2.op_Implicit(point);
				float sqrMagnitude = val.sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					result = i;
				}
			}
		}
		return result;
	}

	public static string TownScroll(int id, int position)
	{
		FieldDirector fieldDirector = GameLoop.I?.field;
		if (fieldDirector?.Map == null)
		{
			return "当前无法使用物品";
		}
		int num = NativeItemSource.Node(id).Int("spec/moveTo", -1);
		int num2 = -1;
		if (num < 0)
		{
			return "此卷轴没有传送目的地";
		}
		if (num == fieldDirector.Map.id)
		{
			return "当前地图无法使用这个卷轴";
		}
		if (num == 999999999)
		{
			if (id == 2030100)
			{
				if (banishCharacter != GameSession.CharacterId || banishMap < 0 || DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - banishTime >= 300000)
				{
					return "当前无法返回被驱逐前的地图";
				}
				num = banishMap;
				num2 = banishPortal;
			}
			else
			{
				num = fieldDirector.Map.returnMap;
			}
		}
		else
		{
			int num3 = num / 10000000;
			int num4 = fieldDirector.Map.id / 10000000;
			if (num3 != 60 && num4 != 61 && num3 != num4 && num3 != 21 && num3 != 20 && num3 != 12 && num4 != 10 && num4 != 12)
			{
				return "当前地图无法使用这个卷轴";
			}
		}
		FieldMap map = FieldCatalog.GetMap(num);
		if (map == null)
		{
			return "传送目的地不存在";
		}
		if (num2 < 0)
		{
			List<int> list = new List<int>();
			for (int i = 0; i < map.portals.Count; i++)
			{
				if (map.portals[i].IsSpawn)
				{
					list.Add(i);
				}
			}
			if (list.Count > 0)
			{
				num2 = list[Random.Range(0, list.Count)];
			}
		}
		bool flag = id != 2030100 && GameServer.Local != null && GameConfigDao.GetBool(GameServer.Local.Db, "use_banishable_town_scroll");
		if (!Profile.RemoveItem(id, 1, position))
		{
			return "背包里没有这个物品";
		}
		if (flag)
		{
			RememberBanish();
		}
		fieldDirector.WarpTo(num, null, num2);
		return null;
	}
}

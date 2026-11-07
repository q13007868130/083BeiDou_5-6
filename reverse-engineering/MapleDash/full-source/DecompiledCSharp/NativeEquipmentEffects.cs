using System;
using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public sealed class NativeEquipmentEffects : MonoBehaviour
{
	private int character;

	private int pendant;

	private double equippedAt;

	private double nextExpiryCheck;

	private int notified;

	private readonly HashSet<int> couponBuffs = new HashSet<int>();

	private static readonly int[] ringCells = new int[4] { -12, -13, -15, -16 };

	private void RefreshCouponBuffs()
	{
		CommunityResponse communityResponse = GameSession.Net.Community(new CommunityRequest
		{
			characterId = GameSession.CharacterId,
			operation = CommunityOperation.Read
		});
		if (!communityResponse.ok)
		{
			return;
		}
		NativeBuffs nativeBuffs = NativeBuffs.For(((Component)this).GetComponent<PlayerCtrl>());
		HashSet<int> hashSet = (ShopWindow.Visible ? new HashSet<int>() : new HashSet<int>(communityResponse.couponBuffs));
		foreach (int couponBuff in couponBuffs)
		{
			if (!hashSet.Contains(couponBuff))
			{
				nativeBuffs.Cancel(-couponBuff);
			}
		}
		foreach (int item in hashSet)
		{
			if (!couponBuffs.Contains(item))
			{
				ImgNode imgNode = NativeItemSource.Node(item)?.Get("spec");
				Dictionary<string, int> dictionary = new Dictionary<string, int>();
				int num = imgNode?.Int("expR") ?? 0;
				int num2 = imgNode?.Int("drpR") ?? 0;
				if (num >= 1 && num <= 4)
				{
					dictionary["COUPON_EXP" + num] = 1;
				}
				if (num2 >= 1 && num2 <= 3)
				{
					dictionary["COUPON_DRP" + num2] = 1;
				}
				if (dictionary.Count > 0)
				{
					nativeBuffs.ApplyItem(item, dictionary, imgNode.Int("time", int.MaxValue), NativeBuffs.Now);
				}
			}
		}
		couponBuffs.Clear();
		foreach (int item2 in hashSet)
		{
			couponBuffs.Add(item2);
		}
	}

	public int PendantStage(double now)
	{
		int num = ((GameSession.InGame && Profile.EquipOfSlot(-17) == 1122017) ? 1122017 : 0);
		if (character != GameSession.CharacterId || pendant != num)
		{
			character = GameSession.CharacterId;
			pendant = num;
			equippedAt = now;
			notified = 0;
		}
		if (pendant != 0)
		{
			return Math.Min(3, (int)((now - equippedAt) / 3600.0));
		}
		return 0;
	}

	public static long PendantBonus(long gain)
	{
		PlayerCtrl playerCtrl = GameLoop.I?.player;
		if ((Object)(object)playerCtrl == (Object)null || gain <= 0)
		{
			return 0L;
		}
		NativeEquipmentEffects component = ((Component)playerCtrl).GetComponent<NativeEquipmentEffects>();
		if (!((Object)(object)component == (Object)null))
		{
			return gain / 10 * component.PendantStage(NativeBuffs.Now);
		}
		return 0L;
	}

	public static int RingExpPercent()
	{
		if (!GameSession.InGame)
		{
			return 0;
		}
		int num = 0;
		int[] array = ringCells;
		for (int i = 0; i < array.Length; i++)
		{
			int num2 = Profile.EquipOfSlot(EquipSlot.WornSlotOf(array[i]));
			if (num2 <= 0)
			{
				continue;
			}
			ImgNode imgNode = EquipCatalog.Info(num2)?.Get("bonusExp");
			if (imgNode != null)
			{
				int num3 = (imgNode.Get("0") ?? ((imgNode.Children.Count > 0) ? imgNode.Children[0] : null))?.Int("incExpR") ?? 0;
				if (num3 > num)
				{
					num = num3;
				}
			}
		}
		return num;
	}

	public static long RingExpBonus(long gain)
	{
		int num = RingExpPercent();
		if (gain > 0 && num != 0)
		{
			return gain / 100 * num + gain % 100 * num / 100;
		}
		return 0L;
	}

	public static void EquipmentChanged()
	{
		GameLoop i = GameLoop.I;
		if (i != null)
		{
			PlayerCtrl player = i.player;
			if (player != null)
			{
				((Component)player).GetComponent<NativeEquipmentEffects>()?.PendantStage(NativeBuffs.Now);
			}
		}
	}

	private void Update()
	{
		int num = PendantStage(NativeBuffs.Now);
		if (num > notified)
		{
			notified = num;
			BottomBar.Toast("精灵吊坠经验加成已提升至 " + num * 10 + "%。");
		}
		if (!GameSession.InGame || GameSession.Character == null || NativeBuffs.Now < nextExpiryCheck)
		{
			return;
		}
		nextExpiryCheck = NativeBuffs.Now + 1.0;
		long num2 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		foreach (InventoryEntry item in GameSession.Character.inventory)
		{
			if (item.expiration > 0 && item.expiration <= num2)
			{
				InventoryChangeResponse inventoryChangeResponse = GameSession.Net.ChangeInventory(new InventoryChangeRequest
				{
					characterId = GameSession.CharacterId,
					itemId = item.itemId
				});
				if (inventoryChangeResponse.ok)
				{
					GameSession.Character.inventory = inventoryChangeResponse.inventory;
					GameSession.ApplyInventoryToProfile();
					Profile.Save();
					((Component)this).GetComponent<PlayerCtrl>()?.RefreshBuffAttributes();
				}
				break;
			}
		}
		InventoryChangeResponse inventoryChangeResponse2 = GameSession.Net.ChangeInventory(new InventoryChangeRequest
		{
			characterId = GameSession.CharacterId,
			itemId = 5000000,
			petTick = true
		});
		if (inventoryChangeResponse2.ok && inventoryChangeResponse2.inventoryChanged)
		{
			GameSession.Character.inventory = inventoryChangeResponse2.inventory;
			GameSession.ApplyInventoryToProfile();
			if (!string.IsNullOrEmpty(inventoryChangeResponse2.message))
			{
				NativeSystemMessage.Drop(6, inventoryChangeResponse2.message);
			}
		}
		RefreshCouponBuffs();
	}
}

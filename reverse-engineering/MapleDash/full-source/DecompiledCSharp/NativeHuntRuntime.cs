using System.Collections.Generic;

public static class NativeHuntRuntime
{
	private static NativeHuntTasks service;

	public static void Tick()
	{
		NativeHuntTasks nativeHuntTasks = GameServer.Local?.Scripts.Hunts;
		if (nativeHuntTasks == null)
		{
			service = null;
			return;
		}
		if (nativeHuntTasks != service)
		{
			service = nativeHuntTasks;
			service.ItemName = WzNames.Item;
			service.MobName = WzNames.Mob;
			service.Notice = (string text) =>
			{
				if (GameSession.InGame && GameSession.World == 0)
				{
					NativeSystemMessage.Drop(6, text);
				}
			};
		}
		service.Tick();
	}

	public static void MonsterKilled(int mob)
	{
		if (!GameSession.InGame)
		{
			return;
		}
		Tick();
		string player = GameSession.Character.record.name;
		service.MonsterKilled(mob, player, () =>
		{
			List<string> list = new List<string>();
			List<string> list2 = new List<string>();
			Profile.AddGold(service.Gold);
			list.Add("金币 +" + service.Gold);
			Profile.GrantExperience(service.Exp);
			list.Add("经验 +" + service.Exp);
			if (GameSession.Net.Cash(new CashRequest
			{
				characterId = GameSession.CharacterId,
				operation = CashOperation.AdjustBalance,
				currency = 1,
				amount = service.Cash
			}).ok)
			{
				list.Add("点券 +" + service.Cash);
			}
			else
			{
				list2.Add("点券 +" + service.Cash + "(发放失败)");
			}
			foreach (NativeHuntTasks.Reward reward in service.Rewards)
			{
				if (service.RewardWon(reward))
				{
					HashSet<int> hashSet = new HashSet<int>();
					foreach (InventoryEntry item in GameSession.Character.inventory)
					{
						if (item.inventoryType == 2 && item.position > 0)
						{
							hashSet.Add(item.position);
						}
					}
					if (Profile.InventoryCapacity(1) - hashSet.Count >= 1)
					{
						Profile.AddItem(reward.itemId, (short)reward.count);
						list.Add((string.IsNullOrEmpty(reward.name) ? (WzNames.Item(reward.itemId) ?? "物品") : reward.name) + " +" + reward.count);
					}
					else
					{
						list2.Add((string.IsNullOrEmpty(reward.name) ? "物品" : reward.name) + " +" + reward.count + "(背包已满)");
					}
				}
			}
			service.CompletionNotice(player, list);
			AudioManager.Play("MobCarnival/Win", 1f, pitchVary: false);
			NativeSystemMessage.Drop(6, "恭喜你完成了狩猎任务！\r\n\r\n获得奖励：\r\n" + string.Join("\r\n", list) + "\r\n" + string.Join("\r\n", list2));
		});
	}
}

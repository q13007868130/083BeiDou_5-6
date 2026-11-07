using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameWindows
{
	private enum Panel
	{
		MiniMap,
		Stat,
		Equip,
		Android,
		Inven,
		Shop,
		Quest,
		Help,
		Skill,
		World,
		Social,
		MonsterBook,
		CommunityBoard,
		AdBoard,
		Community,
		Guild,
		GuildGrade,
		Medal,
		Trade,
		Market,
		NpcShop,
		Delivery,
		Merchant,
		Maker,
		Ranking,
		PlayerShop,
		ItemSearch,
		Storage,
		Claim,
		Rps,
		Memo,
		MapleTv,
		CarnivalHud,
		CarnivalLobby,
		RemoteGachapon,
		CharacterInfo,
		CharacterMenu,
		GameOpt,
		SysOpt,
		Channel,
		GameMenu,
		Shortcut,
		Symbol,
		HyperStat,
		InnerAbility,
		VMatrix,
		HexaMatrix,
		Enhancement,
		Vega,
		ApReset,
		SpReset,
		Teleport,
		KeyConfig,
		PetExclusion
	}

	private static readonly Panel[] panels = (Panel[])Enum.GetValues(typeof(Panel));

	private static readonly List<Panel> openOrder = new List<Panel>();

	private static readonly Dictionary<Panel, bool> lastVisible = new Dictionary<Panel, bool>();

	private static Panel raisedPanel;

	private static bool raisedPending;

	private static readonly List<Panel> zOrder = new List<Panel>
	{
		Panel.MiniMap,
		Panel.Stat,
		Panel.Equip,
		Panel.Android,
		Panel.Inven,
		Panel.Shop,
		Panel.Quest,
		Panel.Help,
		Panel.Skill,
		Panel.World,
		Panel.Social,
		Panel.MonsterBook,
		Panel.CommunityBoard,
		Panel.AdBoard,
		Panel.Community,
		Panel.Guild,
		Panel.GuildGrade,
		Panel.Medal,
		Panel.Trade,
		Panel.Market,
		Panel.NpcShop,
		Panel.Delivery,
		Panel.Merchant,
		Panel.Maker,
		Panel.Ranking,
		Panel.PlayerShop,
		Panel.ItemSearch,
		Panel.Storage,
		Panel.Claim,
		Panel.Rps,
		Panel.Memo,
		Panel.MapleTv,
		Panel.CarnivalHud,
		Panel.CarnivalLobby,
		Panel.RemoteGachapon,
		Panel.CharacterInfo,
		Panel.CharacterMenu,
		Panel.GameOpt,
		Panel.SysOpt,
		Panel.Channel,
		Panel.GameMenu,
		Panel.Shortcut,
		Panel.Vega,
		Panel.Enhancement,
		Panel.Symbol,
		Panel.HyperStat,
		Panel.InnerAbility,
		Panel.VMatrix,
		Panel.HexaMatrix,
		Panel.ApReset,
		Panel.SpReset,
		Panel.Teleport,
		Panel.KeyConfig
	};

	private static readonly Dictionary<Panel, string[]> hitKeys = new Dictionary<Panel, string[]>
	{
		{
			Panel.Stat,
			new string[1] { "StatWindow" }
		},
		{
			Panel.Equip,
			new string[1] { "EquipWindow" }
		},
		{
			Panel.Android,
			new string[1] { "AndroidWindow" }
		},
		{
			Panel.Inven,
			new string[1] { "InvenWindow" }
		},
		{
			Panel.Quest,
			new string[1] { "QuestWindow" }
		},
		{
			Panel.Skill,
			new string[1] { "SkillWindow" }
		},
		{
			Panel.World,
			new string[1] { "NativeMapWindows" }
		},
		{
			Panel.Social,
			new string[1] { "NativeSocialWindow" }
		},
		{
			Panel.MonsterBook,
			new string[1] { "NativeMonsterBook" }
		},
		{
			Panel.Community,
			new string[1] { "NativeCommunityWindow" }
		},
		{
			Panel.Guild,
			new string[1] { "NativeGuildWindow" }
		},
		{
			Panel.GuildGrade,
			new string[1] { "NativeGuildGradeWindow" }
		},
		{
			Panel.Trade,
			new string[1] { "NativeTradeWindow" }
		},
		{
			Panel.Market,
			new string[1] { "NativeMarketWindow" }
		},
		{
			Panel.NpcShop,
			new string[1] { "NativeNpcShopWindow" }
		},
		{
			Panel.Delivery,
			new string[1] { "NativeDeliveryWindow" }
		},
		{
			Panel.Merchant,
			new string[1] { "NativeMerchantWindow" }
		},
		{
			Panel.Maker,
			new string[1] { "NativeMakerWindow" }
		},
		{
			Panel.Ranking,
			new string[1] { "NativeRankingWindow" }
		},
		{
			Panel.PlayerShop,
			new string[1] { "NativePlayerShopWindow" }
		},
		{
			Panel.ItemSearch,
			new string[1] { "NativeItemSearchWindow" }
		},
		{
			Panel.Storage,
			new string[1] { "NativeStorageWindow" }
		},
		{
			Panel.Claim,
			new string[1] { "NativeClaimWindow" }
		},
		{
			Panel.Rps,
			new string[1] { "NativeRpsWindow" }
		},
		{
			Panel.RemoteGachapon,
			new string[1] { "NativeRemoteGachaponWindow" }
		},
		{
			Panel.CharacterInfo,
			new string[1] { "NativeCharacterInfo" }
		},
		{
			Panel.CharacterMenu,
			new string[1] { "NativeCharacterMenu" }
		},
		{
			Panel.Memo,
			new string[1] { "NativeMemoWindow" }
		},
		{
			Panel.Channel,
			new string[1] { "ChannelWindow" }
		},
		{
			Panel.CarnivalLobby,
			new string[1] { "NativeCarnivalLobby" }
		},
		{
			Panel.Vega,
			new string[1] { "NativeVegaWindow" }
		},
		{
			Panel.ApReset,
			new string[1] { "NativeResetWindow" }
		},
		{
			Panel.SpReset,
			new string[1] { "NativeSpResetWindow" }
		},
		{
			Panel.Teleport,
			new string[1] { "NativeTeleportWindow" }
		},
		{
			Panel.Symbol,
			new string[1] { "NativeSymbolWindow" }
		},
		{
			Panel.HyperStat,
			new string[1] { "NativeHyperStatWindow" }
		},
		{
			Panel.InnerAbility,
			new string[1] { "NativeInnerAbilityWindow" }
		},
		{
			Panel.VMatrix,
			new string[1] { "NativeVMatrixWindow" }
		},
		{
			Panel.HexaMatrix,
			new string[1] { "NativeHexaMatrixWindow" }
		},
		{
			Panel.Enhancement,
			new string[3] { "NativeEnhancementWindow", "NativeEnhancementChoice", "SoulWeaponWz2Modal" }
		},
		{
			Panel.KeyConfig,
			new string[1] { "KeyConfigWindow" }
		}
	};

	public static bool AnyVisible
	{
		get
		{
			if (NativeUtilPrompt.Visible || KeyConfigWindow.Visible)
			{
				return true;
			}
			Panel[] array = panels;
			for (int i = 0; i < array.Length; i++)
			{
				if (VisibleOf(array[i]))
				{
					return true;
				}
			}
			return false;
		}
	}

	private static bool VisibleOf(Panel p)
	{
		switch (p)
		{
		case Panel.Symbol:
			return NativeSymbolWindow.Visible;
		case Panel.HyperStat:
			return NativeHyperStatWindow.Visible;
		case Panel.InnerAbility:
			return NativeInnerAbilityWindow.Visible;
		case Panel.VMatrix:
			return NativeVMatrixWindow.Visible;
		case Panel.HexaMatrix:
			return NativeHexaMatrixWindow.Visible;
		case Panel.Enhancement:
			return NativeEnhancementWindow.Visible;
		case Panel.Vega:
			return NativeVegaWindow.Visible;
		case Panel.ApReset:
			return NativeResetWindow.Visible;
		case Panel.SpReset:
			return NativeSpResetWindow.Visible;
		case Panel.Teleport:
			return NativeTeleportWindow.Visible;
		case Panel.CharacterMenu:
			return NativeCharacterMenu.Visible;
		case Panel.CharacterInfo:
			return NativeCharacterInfo.Visible;
		case Panel.Trade:
			return NativeTradeWindow.Visible;
		case Panel.Market:
			return NativeMarketWindow.Visible;
		case Panel.Android:
			return NativeAndroidWindow.Visible;
		case Panel.Memo:
			return NativeMemoWindow.Visible;
		case Panel.PetExclusion:
			return NativePetExclusionWindow.Visible;
		case Panel.GuildGrade:
			return NativeGuildGradeWindow.Visible;
		case Panel.Stat:
			return StatWindow.Visible;
		case Panel.Equip:
			return EquipWindow.Visible;
		case Panel.Inven:
			return InvenWindow.Visible;
		case Panel.Shop:
			return ShopWindow.Visible;
		case Panel.NpcShop:
			return NativeNpcShopWindow.Visible;
		case Panel.Delivery:
			return NativeDeliveryWindow.Visible;
		case Panel.Merchant:
			return NativeMerchantWindow.Visible;
		case Panel.Maker:
			return NativeMakerWindow.Visible;
		case Panel.Ranking:
			return NativeRankingWindow.Visible;
		case Panel.PlayerShop:
			return NativePlayerShopWindow.Visible;
		case Panel.ItemSearch:
			return NativeItemSearchWindow.Visible;
		case Panel.Storage:
			return NativeStorageWindow.Visible;
		case Panel.Claim:
			return NativeClaimWindow.Visible;
		case Panel.Rps:
			return NativeRpsWindow.Visible;
		case Panel.RemoteGachapon:
			return NativeRemoteGachaponWindow.Visible;
		case Panel.Quest:
			return QuestWindow.Visible;
		case Panel.Shortcut:
			return ShortcutWindow.Visible;
		case Panel.Help:
			return HelpWindow.Visible;
		case Panel.GameMenu:
			return GameMenuWindow.Visible;
		case Panel.Skill:
			return SkillWindow.Visible;
		case Panel.World:
			return NativeMapWindows.WorldVisible;
		case Panel.Social:
			return NativeSocialWindow.Visible;
		case Panel.MonsterBook:
			return NativeMonsterBook.Visible;
		case Panel.Community:
			return NativeCommunityWindow.Visible;
		case Panel.Guild:
			return NativeGuildWindow.Visible;
		case Panel.Medal:
			return NativeMedalWindow.Visible;
		case Panel.GameOpt:
			return GameOptWindow.Visible;
		case Panel.SysOpt:
			return SysOptWindow.Visible;
		case Panel.Channel:
			return ChannelWindow.Visible;
		case Panel.KeyConfig:
			return KeyConfigWindow.Visible;
		case Panel.MiniMap:
		case Panel.CommunityBoard:
		case Panel.AdBoard:
		case Panel.MapleTv:
		case Panel.CarnivalHud:
		case Panel.CarnivalLobby:
			return false;
		default:
			return false;
		}
	}

	private static void ClosePanel(Panel p)
	{
		switch (p)
		{
		case Panel.Symbol:
			NativeSymbolWindow.Close();
			break;
		case Panel.HyperStat:
			NativeHyperStatWindow.Close();
			break;
		case Panel.InnerAbility:
			NativeInnerAbilityWindow.Close();
			break;
		case Panel.VMatrix:
			NativeVMatrixWindow.Escape();
			break;
		case Panel.HexaMatrix:
			NativeHexaMatrixWindow.Escape();
			break;
		case Panel.Enhancement:
			NativeEnhancementWindow.Close();
			break;
		case Panel.Vega:
			if (!NativeVegaWindow.Busy)
			{
				NativeVegaWindow.Close();
			}
			break;
		case Panel.ApReset:
			NativeResetWindow.Close();
			break;
		case Panel.SpReset:
			NativeSpResetWindow.Close();
			break;
		case Panel.Teleport:
			NativeTeleportWindow.Close();
			break;
		case Panel.CharacterMenu:
			NativeCharacterMenu.Close();
			break;
		case Panel.CharacterInfo:
			NativeCharacterInfo.Close();
			break;
		case Panel.Trade:
			NativeTradeWindow.Close();
			break;
		case Panel.Market:
			NativeMarketWindow.Close();
			break;
		case Panel.Android:
			NativeAndroidWindow.Visible = false;
			break;
		case Panel.Memo:
			NativeMemoWindow.Close();
			break;
		case Panel.PetExclusion:
			NativePetExclusionWindow.Close();
			break;
		case Panel.GuildGrade:
			NativeGuildGradeWindow.Reset();
			break;
		case Panel.Stat:
			StatWindow.Visible = false;
			break;
		case Panel.Equip:
			EquipWindow.Visible = false;
			break;
		case Panel.Inven:
			InvenWindow.Visible = false;
			break;
		case Panel.Shop:
			NativeCashWindow.Reset();
			break;
		case Panel.NpcShop:
			NativeNpcShopWindow.Close();
			break;
		case Panel.Delivery:
			NativeDeliveryWindow.Close();
			break;
		case Panel.Merchant:
			NativeMerchantWindow.Close();
			break;
		case Panel.Maker:
			NativeMakerWindow.Close();
			break;
		case Panel.Ranking:
			NativeRankingWindow.Close();
			break;
		case Panel.PlayerShop:
			NativePlayerShopWindow.Close();
			break;
		case Panel.ItemSearch:
			NativeItemSearchWindow.Close();
			break;
		case Panel.Storage:
			NativeStorageWindow.Close();
			break;
		case Panel.Claim:
			NativeClaimWindow.Close();
			break;
		case Panel.Rps:
			NativeRpsWindow.RequestLeave();
			break;
		case Panel.RemoteGachapon:
			NativeRemoteGachaponWindow.Close();
			break;
		case Panel.Quest:
			QuestWindow.Visible = false;
			break;
		case Panel.Shortcut:
			ShortcutWindow.Visible = false;
			break;
		case Panel.Help:
			HelpWindow.Visible = false;
			break;
		case Panel.GameMenu:
			GameMenuWindow.Visible = false;
			break;
		case Panel.Skill:
			SkillWindow.Visible = false;
			break;
		case Panel.World:
			NativeMapWindows.CloseWorld();
			break;
		case Panel.Social:
			NativeSocialWindow.Close();
			break;
		case Panel.MonsterBook:
			NativeMonsterBook.Visible = false;
			break;
		case Panel.Community:
			NativeCommunityWindow.Reset();
			break;
		case Panel.Guild:
			NativeGuildWindow.Reset();
			break;
		case Panel.Medal:
			NativeMedalWindow.Visible = false;
			break;
		case Panel.GameOpt:
			GameOptWindow.Close();
			break;
		case Panel.SysOpt:
			SysOptWindow.Close();
			break;
		case Panel.Channel:
			ChannelWindow.Close();
			break;
		case Panel.KeyConfig:
			KeyConfigWindow.Close();
			break;
		case Panel.CommunityBoard:
		case Panel.AdBoard:
		case Panel.MapleTv:
		case Panel.CarnivalHud:
		case Panel.CarnivalLobby:
			break;
		}
	}

	public static void TrackPanels()
	{
		Panel[] array = panels;
		foreach (Panel panel in array)
		{
			bool flag = VisibleOf(panel);
			lastVisible.TryGetValue(panel, out var value);
			if (flag != value)
			{
				lastVisible[panel] = flag;
				openOrder.Remove(panel);
				if (flag)
				{
					openOrder.Add(panel);
					raisedPanel = panel;
					raisedPending = true;
				}
			}
		}
	}

	public static bool CloseTop()
	{
		if (NativeUtilPrompt.Visible)
		{
			NativeUtilPrompt.Cancel();
			return true;
		}
		if (NativeSoulWeaponPanel.HasModal)
		{
			NativeSoulWeaponPanel.CloseModal();
			return true;
		}
		TrackPanels();
		if (openOrder.Count == 0)
		{
			return false;
		}
		Panel panel = openOrder[openOrder.Count - 1];
		ClosePanel(panel);
		if (!VisibleOf(panel))
		{
			openOrder.Remove(panel);
		}
		lastVisible[panel] = VisibleOf(panel);
		return true;
	}

	public static void DrawAll()
	{
		TrackPanels();
		if (raisedPending)
		{
			BringToFront(raisedPanel);
			raisedPending = false;
		}
		ItemTooltip.Reset();
		NativeSoulWeapon.Tick();
		NativeCashWindow.Tick();
		NativeDeliveryWindow.Tick();
		NativeItemSearchWindow.Tick();
		Panel? panel = TopPanelAtPointer();
		if (panel.HasValue && Pointer.Pressed)
		{
			BringToFront(panel.Value);
		}
		int num = (panel.HasValue ? zOrder.IndexOf(panel.Value) : (-1));
		bool enabled = GUI.enabled;
		Pointer.InputMask inputMask = new Pointer.InputMask(NativeUtilPrompt.Visible);
		GUI.enabled &= !NativeUtilPrompt.Visible;
		try
		{
			for (int i = 0; i < zOrder.Count; i++)
			{
				Panel p = zOrder[i];
				if (num < 0 || i >= num)
				{
					DrawPanel(p);
					continue;
				}
				Pointer.InputMask inputMask2 = new Pointer.InputMask(hide: true);
				try
				{
					DrawPanel(p);
				}
				finally
				{
					inputMask2.Dispose();
				}
			}
			ItemTooltip.Flush();
		}
		finally
		{
			GUI.enabled = enabled;
			inputMask.Dispose();
		}
		NativeUtilPrompt.Draw();
		UiPreviewHook.Emit();
	}

	private static bool IsFollower(Panel p)
	{
		if (p != Panel.Android)
		{
			return p == Panel.GuildGrade;
		}
		return true;
	}

	private static Panel HostOf(Panel p)
	{
		return p switch
		{
			Panel.GuildGrade => Panel.Guild, 
			Panel.Android => Panel.Equip, 
			_ => p, 
		};
	}

	private static void BringToFront(Panel p)
	{
		if (IsFollower(p))
		{
			p = HostOf(p);
		}
		int num = zOrder.IndexOf(p);
		if (num < 0)
		{
			return;
		}
		List<Panel> list = new List<Panel> { p };
		switch (p)
		{
		case Panel.Equip:
			list.Add(Panel.Android);
			break;
		case Panel.Guild:
			list.Add(Panel.GuildGrade);
			break;
		}
		if (num == zOrder.Count - list.Count)
		{
			return;
		}
		foreach (Panel item in list)
		{
			zOrder.Remove(item);
		}
		zOrder.AddRange(list);
	}

	private static bool PanelAtPointer(Panel p)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		if (!hitKeys.TryGetValue(p, out var value))
		{
			return false;
		}
		for (int i = 0; i < value.Length; i++)
		{
			if (NativeUi.TryWindowRect(value[i], out var rect) && Pointer.HitsWindow(rect))
			{
				return true;
			}
		}
		return false;
	}

	private static Panel? TopPanelAtPointer()
	{
		for (int num = zOrder.Count - 1; num >= 0; num--)
		{
			Panel panel = zOrder[num];
			if (VisibleOf(panel) && PanelAtPointer(panel))
			{
				return panel;
			}
		}
		return null;
	}

	private static void DrawPanel(Panel p)
	{
		switch (p)
		{
		case Panel.MiniMap:
			NativeMapWindows.DrawMini();
			break;
		case Panel.Stat:
			StatWindow.Draw();
			break;
		case Panel.Equip:
			EquipWindow.Draw();
			break;
		case Panel.Android:
			NativeAndroidWindow.Draw();
			break;
		case Panel.Inven:
			InvenWindow.Draw();
			break;
		case Panel.Shop:
			ShopWindow.Draw();
			break;
		case Panel.Quest:
			QuestWindow.Draw();
			break;
		case Panel.Help:
			HelpWindow.Draw();
			break;
		case Panel.Skill:
			SkillWindow.Draw();
			break;
		case Panel.World:
			NativeMapWindows.DrawWorld();
			break;
		case Panel.Social:
			NativeSocialWindow.Draw();
			break;
		case Panel.MonsterBook:
			NativeMonsterBook.Draw();
			break;
		case Panel.CommunityBoard:
			NativeCommunityWindow.DrawBoard();
			break;
		case Panel.AdBoard:
			NativeAdBoard.Draw();
			break;
		case Panel.Community:
			NativeCommunityWindow.Draw();
			break;
		case Panel.Guild:
			NativeGuildWindow.Draw();
			break;
		case Panel.GuildGrade:
			NativeGuildGradeWindow.Draw();
			break;
		case Panel.Medal:
			NativeMedalWindow.Draw();
			break;
		case Panel.Trade:
			NativeTradeWindow.Draw();
			break;
		case Panel.Market:
			NativeMarketWindow.Draw();
			break;
		case Panel.NpcShop:
			NativeNpcShopWindow.Draw();
			break;
		case Panel.Delivery:
			NativeDeliveryWindow.Draw();
			break;
		case Panel.Merchant:
			NativeMerchantWindow.Draw();
			break;
		case Panel.Maker:
			NativeMakerWindow.Draw();
			break;
		case Panel.Ranking:
			NativeRankingWindow.Draw();
			break;
		case Panel.PlayerShop:
			NativePlayerShopWindow.Draw();
			break;
		case Panel.ItemSearch:
			NativeItemSearchWindow.Draw();
			break;
		case Panel.Storage:
			NativeStorageWindow.Draw();
			break;
		case Panel.Claim:
			NativeClaimWindow.Draw();
			break;
		case Panel.Rps:
			NativeRpsWindow.Draw();
			break;
		case Panel.Memo:
			NativeMemoWindow.Draw();
			break;
		case Panel.MapleTv:
			NativeMapleTvDisplay.Draw();
			break;
		case Panel.CarnivalHud:
			NativeCarnivalHud.Draw();
			break;
		case Panel.CarnivalLobby:
			NativeCarnivalLobby.Draw();
			break;
		case Panel.RemoteGachapon:
			NativeRemoteGachaponWindow.Draw();
			break;
		case Panel.CharacterInfo:
			NativeCharacterInfo.Draw();
			break;
		case Panel.CharacterMenu:
			NativeCharacterMenu.Draw();
			break;
		case Panel.GameOpt:
			GameOptWindow.Draw();
			break;
		case Panel.SysOpt:
			SysOptWindow.Draw();
			break;
		case Panel.Channel:
			ChannelWindow.Draw();
			break;
		case Panel.GameMenu:
			GameMenuWindow.Draw();
			break;
		case Panel.Shortcut:
			ShortcutWindow.Draw();
			break;
		case Panel.Vega:
			NativeVegaWindow.Draw();
			break;
		case Panel.Enhancement:
			NativeEnhancementWindow.Draw();
			break;
		case Panel.Symbol:
			NativeSymbolWindow.Draw();
			break;
		case Panel.HyperStat:
			NativeHyperStatWindow.Draw();
			break;
		case Panel.InnerAbility:
			NativeInnerAbilityWindow.Draw();
			break;
		case Panel.VMatrix:
			NativeVMatrixWindow.Draw();
			break;
		case Panel.HexaMatrix:
			NativeHexaMatrixWindow.Draw();
			break;
		case Panel.ApReset:
			NativeResetWindow.Draw();
			break;
		case Panel.SpReset:
			NativeSpResetWindow.Draw();
			break;
		case Panel.Teleport:
			NativeTeleportWindow.Draw();
			break;
		case Panel.KeyConfig:
			KeyConfigWindow.Draw();
			break;
		}
	}
}

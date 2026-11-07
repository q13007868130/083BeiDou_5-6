using System;
using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public static class NativeInventoryActions
{
	private static int carriedItem;

	private static int carriedPosition;

	private static int carriedType;

	private static bool swallowDrop;

	private static bool carriedFromInventory;

	public static bool Carrying => carriedItem != 0;

	public static void CancelCarry()
	{
		carriedItem = 0;
	}

	public static void PickEquipped(int id, int slot)
	{
		if (!Carrying && slot < 0 && slot != -11 && Profile.EquipOfSlot(slot) == id)
		{
			SkillWindow.CancelSkillDrag();
			carriedItem = id;
			carriedPosition = slot;
			carriedType = 1;
			carriedFromInventory = false;
		}
	}

	public static void Pick(int id, int inventoryPosition = 0)
	{
		Pick(id, inventoryPosition, fromInventory: false);
	}

	public static void PickFromInventory(int id, int inventoryPosition = 0)
	{
		Pick(id, inventoryPosition, fromInventory: true);
	}

	private static string PickOwnedItem(int id, int inventoryPosition)
	{
		InventoryEntry inventoryEntry = GameSession.Character?.inventory.Find((InventoryEntry x) => x.itemId == id && InventoryDao.IsBagPosition(x.position) && (inventoryPosition == 0 || x.position == inventoryPosition));
		if (inventoryEntry == null)
		{
			return "背包里没有这个物品";
		}
		PickFromInventory(id, inventoryEntry.position);
		return null;
	}

	private static void Pick(int id, int inventoryPosition, bool fromInventory)
	{
		if (id / 1000000 == 2 || id / 10000 == 301 || id / 1000000 == 5 || (NativeVegaWindow.Visible && id / 1000000 == 1) || (fromInventory && id > 0 && inventoryPosition >= 0))
		{
			SkillWindow.CancelSkillDrag();
			carriedItem = id;
			carriedPosition = inventoryPosition;
			carriedFromInventory = fromInventory;
			carriedType = (fromInventory ? (id / 1000000) : 0);
			if (fromInventory && carriedType == 1 && NativeAndroidWindow.Visible)
			{
				Debug.Log((object)("[Android/Drag] pick item=" + id + " source=" + inventoryPosition));
			}
		}
	}

	public static void CarryStep()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Invalid comparison between Unknown and I4
		if (Event.current != null && (int)Event.current.type == 7)
		{
			CarryPointerStep();
		}
	}

	public static void CarryPointerStep()
	{
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_048a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0499: Unknown result type (might be due to invalid IL or missing references)
		//IL_030a: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_04df: Unknown result type (might be due to invalid IL or missing references)
		//IL_043f: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0573: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0354: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0664: Unknown result type (might be due to invalid IL or missing references)
		//IL_068f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0694: Unknown result type (might be due to invalid IL or missing references)
		//IL_0698: Unknown result type (might be due to invalid IL or missing references)
		if (swallowDrop)
		{
			if (Pointer.Released)
			{
				if (Carrying && carriedFromInventory && carriedType == 1 && NativeAndroidWindow.Visible)
				{
					string text = carriedItem.ToString();
					Vector2 pos = Pointer.Pos;
					Debug.Log((object)("[Android/Drag] release swallowed item=" + text + " pointer=" + ((object)pos/*cast due to constrained. prefix*/).ToString()));
				}
				if (!Pointer.DoubleClicked)
				{
					Pointer.ConsumeClick();
				}
				swallowDrop = false;
			}
		}
		else
		{
			if (!Carrying)
			{
				return;
			}
			if (carriedFromInventory && carriedType == 1 && NativeAndroidWindow.Visible && (Pointer.Pressed || Pointer.Released))
			{
				bool flag = NativeAndroidWindow.SlotAt(Pointer.Pos, out var slot);
				string[] array = new string[20]
				{
					"[Android/Drag] item=",
					carriedItem.ToString(),
					" source=",
					carriedPosition.ToString(),
					" pressed=",
					Pointer.Pressed.ToString(),
					" released=",
					Pointer.Released.ToString(),
					" dragEnded=",
					Pointer.DragEnded.ToString(),
					" pointer=",
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null
				};
				Vector2 pos = Pointer.Pos;
				array[11] = ((object)pos/*cast due to constrained. prefix*/).ToString();
				array[12] = " hit=";
				array[13] = flag.ToString();
				array[14] = " target=";
				array[15] = slot.ToString();
				array[16] = " equipVisible=";
				array[17] = EquipWindow.Visible.ToString();
				array[18] = " npcBlocking=";
				array[19] = NpcSession.Blocking.ToString();
				Debug.Log((object)string.Concat(array));
			}
			if (Pointer.RightPressed || Input.GetKeyDown((KeyCode)27) || (NpcSession.Blocking && !SymbolCatalog.IsSymbolSlot(carriedPosition)))
			{
				carriedItem = 0;
				return;
			}
			if ((Pointer.Pressed || (Pointer.Released && Pointer.DragEnded)) && NativeEnhancementWindow.Drop(Pointer.Pos, carriedItem, carriedPosition))
			{
				carriedItem = 0;
				swallowDrop = Pointer.Pressed;
				Pointer.ConsumeClick();
				Pointer.Pressed = false;
				return;
			}
			if (!carriedFromInventory && carriedType == 1 && carriedPosition < 0)
			{
				int cellSlot;
				int position;
				if (Profile.EquipOfSlot(carriedPosition) != carriedItem)
				{
					carriedItem = 0;
				}
				else if (Pointer.Pressed && EquipWindow.SlotAt(Pointer.Pos, out cellSlot) && EquipSlot.WornSlotOf(cellSlot) == carriedPosition)
				{
					carriedItem = 0;
					swallowDrop = true;
					Pointer.ConsumeClick();
					Pointer.Pressed = false;
				}
				else if ((Pointer.Pressed || (Pointer.Released && Pointer.DragEnded)) && InvenWindow.Visible && InvenWindow.InventoryType == 1 && InvenWindow.SlotAt(Pointer.Pos, out position))
				{
					string text2 = EquipLoadout.UnequipToInventory(carriedPosition, position);
					if (text2 == null)
					{
						carriedItem = 0;
						swallowDrop = Pointer.Pressed;
					}
					else
					{
						BottomBar.Toast(text2);
					}
					Pointer.ConsumeClick();
					Pointer.Pressed = false;
				}
				return;
			}
			if (!Pointer.Pressed)
			{
				if (Pointer.Released && EquipWindow.DropPetPotion(Pointer.Pos, carriedItem))
				{
					carriedItem = 0;
					Pointer.ConsumeClick();
					return;
				}
				if (Pointer.Released && Pointer.DragEnded && carriedFromInventory && carriedType == 1 && EquipWindow.Visible && EquipWindow.SlotAt(Pointer.Pos, out var cellSlot2))
				{
					if (FitsEquipCell(cellSlot2, EquipSlot.SlotOf(carriedItem)))
					{
						int itemId = carriedItem;
						int inventoryPosition = carriedPosition;
						carriedItem = 0;
						string text3 = EquipLoadout.Equip(itemId, Profile.Attributes, inventoryPosition, cellSlot2);
						if (text3 != null)
						{
							NativeUtilPrompt.Notice(text3);
						}
					}
					else
					{
						NativeUtilPrompt.Notice("这件装备不能放在" + EquipSlot.NameOf(cellSlot2) + "这一格");
					}
					Pointer.ConsumeClick();
					return;
				}
				if (Pointer.Released && Pointer.DragEnded && carriedFromInventory && InvenWindow.Visible && InvenWindow.SlotAt(Pointer.Pos, out var position2) && InvenWindow.InventoryType == carriedType && position2 != carriedPosition)
				{
					int num = carriedItem;
					int num2 = carriedPosition;
					carriedItem = 0;
					if (MoveToSlot(num2, position2, carriedType))
					{
						Pointer.ConsumeClick();
						return;
					}
					carriedItem = num;
				}
				if (Pointer.Released && carriedFromInventory && FieldAcceptsDrop(Pointer.Pos))
				{
					int itemId2 = carriedItem;
					int position3 = carriedPosition;
					carriedItem = 0;
					if (DropToField(itemId2, position3))
					{
						Pointer.ConsumeClick();
						Pointer.Pressed = false;
					}
					else
					{
						carriedItem = itemId2;
					}
				}
				return;
			}
			int num3 = carriedItem;
			carriedItem = 0;
			bool flag2 = EquipWindow.DropPetPotion(Pointer.Pos, num3);
			if (!flag2)
			{
				flag2 = NativeVegaWindow.Drop(Pointer.Pos, num3, carriedPosition);
			}
			if (!flag2 && (num3 / 10000 == 204 || NativeScrollService.IsHeartScroll(num3) || IsCashEquipment(num3)))
			{
				InventoryEntry inventoryEntry = EquipWindow.EquipmentAt(Pointer.Pos) ?? InvenWindow.EquipmentAt(Pointer.Pos);
				if (inventoryEntry == null && NativeAndroidWindow.Visible && NativeAndroidWindow.SlotAt(Pointer.Pos, out var slot2) && slot2 == -54)
				{
					inventoryEntry = GameSession.Character.inventory.Find((InventoryEntry e) => e.inventoryType == 1 && e.position == -54);
				}
				if (inventoryEntry != null)
				{
					flag2 = true;
					if (IsCashEquipment(num3))
					{
						NativeItemUseEffects.CashEquipment(num3, inventoryEntry);
					}
					else
					{
						NativeItemUseEffects.Scroll(num3, inventoryEntry);
					}
				}
			}
			if (!flag2 && carriedFromInventory && carriedType == 1 && EquipWindow.Visible && EquipWindow.SlotAt(Pointer.Pos, out var cellSlot3))
			{
				if (!FitsEquipCell(cellSlot3, EquipSlot.SlotOf(num3)))
				{
					NativeUtilPrompt.Notice("这件装备不能放在" + EquipSlot.NameOf(cellSlot3) + "这一格");
					carriedItem = num3;
					return;
				}
				string text4 = EquipLoadout.Equip(num3, Profile.Attributes, carriedPosition, cellSlot3);
				if (text4 != null)
				{
					NativeUtilPrompt.Notice(text4);
				}
				flag2 = true;
			}
			if (!flag2 && carriedFromInventory && InvenWindow.Visible && InvenWindow.SlotAt(Pointer.Pos, out var position4))
			{
				if (InvenWindow.InventoryType != carriedType)
				{
					carriedItem = num3;
				}
				else if (position4 == carriedPosition)
				{
					swallowDrop = true;
					Pointer.ConsumeClick();
					Pointer.Pressed = false;
				}
				else if (MoveToSlot(carriedPosition, position4, carriedType))
				{
					swallowDrop = true;
					Pointer.ConsumeClick();
					Pointer.Pressed = false;
				}
				else
				{
					carriedItem = num3;
				}
				return;
			}
			if (!flag2 && num3 / 1000000 != 1)
			{
				flag2 = KeyConfigWindow.Visible && !KeyConfigWindow.Carrying && KeyConfigWindow.DropSkill(Pointer.Pos, -num3);
			}
			if (!flag2 && num3 / 1000000 != 1 && !KeyConfigWindow.Visible)
			{
				for (int num4 = 0; num4 < 26; num4++)
				{
					Rect val = BottomBar.SlotRect(num4);
					if (val.Contains(Pointer.Pos))
					{
						QuickBarKeys.BindSkill(num4, -num3);
						flag2 = true;
						break;
					}
				}
			}
			if (!flag2 && carriedFromInventory && FieldAcceptsDrop(Pointer.Pos))
			{
				flag2 = DropToField(num3, carriedPosition);
			}
			if (flag2)
			{
				swallowDrop = true;
				Pointer.ConsumeClick();
				Pointer.Pressed = false;
			}
			else
			{
				carriedItem = num3;
			}
		}
	}

	private static bool FitsEquipCell(int cellSlot, int itemSlot)
	{
		if (cellSlot == 0 || itemSlot == 0)
		{
			return false;
		}
		if (NativeAndroidService.IsApSlot(cellSlot))
		{
			return NativeAndroidService.ApAccepts(cellSlot, itemSlot);
		}
		int num = ((cellSlot <= -100) ? (cellSlot + 100) : cellSlot);
		int num2 = ((itemSlot <= -100) ? (itemSlot + 100) : itemSlot);
		if (num == num2)
		{
			return true;
		}
		if (IsRingSlot(num))
		{
			return IsRingSlot(num2);
		}
		return false;
	}

	private static bool IsRingSlot(int slot)
	{
		if (slot != -12 && slot != -13 && slot != -15)
		{
			return slot == -16;
		}
		return true;
	}

	private static bool MoveToSlot(int from, int to, int type)
	{
		if (!GameSession.InGame || GameSession.Character == null || from <= 0 || to <= 0 || from == to)
		{
			return false;
		}
		InventoryChangeResponse inventoryChangeResponse = GameSession.Net.ChangeInventory(new InventoryChangeRequest
		{
			characterId = GameSession.CharacterId,
			moveFrom = from,
			moveTo = to,
			moveInventoryType = type
		});
		if (inventoryChangeResponse == null || !inventoryChangeResponse.ok)
		{
			BottomBar.Toast((inventoryChangeResponse == null || string.IsNullOrEmpty(inventoryChangeResponse.error)) ? "无法移动这件物品" : inventoryChangeResponse.error);
			return false;
		}
		GameSession.Character.inventory = inventoryChangeResponse.inventory;
		GameSession.ApplyInventoryToProfile();
		Profile.Save();
		return true;
	}

	private static bool FieldAcceptsDrop(Vector2 pointer)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		if (!NativeUi.WindowAtPointer && !VirtualControls.BlockingPointer)
		{
			return pointer.y < UiKit.H - 80f * BottomBar.S;
		}
		return false;
	}

	private static bool DropToField(int itemId, int position)
	{
		GameLoop i = GameLoop.I;
		PlayerCtrl obj = (((Object)(object)i == (Object)null) ? null : i.player);
		FieldDirector fieldDirector = (((Object)(object)i == (Object)null) ? null : i.field);
		if ((Object)(object)obj == (Object)null || (Object)(object)fieldDirector == (Object)null || fieldDirector.StateId < 0 || GameSession.Character == null)
		{
			return false;
		}
		if (GameSession.Character.inventory.Find((InventoryEntry x) => x.itemId == itemId && InventoryDao.IsBagPosition(x.position) && (position == 0 || x.position == position)) == null)
		{
			return false;
		}
		ImgNode imgNode = NativeItemSource.Node(itemId)?.Get("info");
		if (imgNode != null && imgNode.Int("cash") == 1)
		{
			NativeUtilPrompt.Notice("这种道具不能扔掉。");
			return true;
		}
		if (imgNode != null && imgNode.Int("quest") == 1)
		{
			NativeUtilPrompt.Confirm("这种道具丢弃后无法再次获得，还想丢弃吗？", (bool yes) =>
			{
				if (yes)
				{
					DropToFieldAsk(itemId, position);
				}
			});
			return true;
		}
		if (imgNode != null && imgNode.Int("tradeBlock") == 1)
		{
			NativeUtilPrompt.Confirm("丢弃物品后获得的话，会变成无法交易状态。你确认要丢弃吗？", (bool yes) =>
			{
				if (yes)
				{
					DropToFieldAsk(itemId, position);
				}
			});
			return true;
		}
		DropToFieldAsk(itemId, position);
		return true;
	}

	private static void DropToFieldAsk(int itemId, int position)
	{
		InventoryEntry inventoryEntry = ((GameSession.Character == null) ? null : GameSession.Character.inventory.Find((InventoryEntry x) => x.itemId == itemId && InventoryDao.IsBagPosition(x.position) && (position == 0 || x.position == position)));
		if (inventoryEntry == null)
		{
			return;
		}
		if (inventoryEntry.quantity > 1 && itemId / 1000000 != 1)
		{
			NativeUtilPrompt.Number("扔掉多少？", inventoryEntry.quantity, 1, inventoryEntry.quantity, (int value) =>
			{
				DropToField(itemId, position, value);
			});
		}
		else
		{
			DropToField(itemId, position, 1);
		}
	}

	private static void DropToField(int itemId, int position, int quantity)
	{
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		GameLoop i = GameLoop.I;
		PlayerCtrl playerCtrl = (((Object)(object)i == (Object)null) ? null : i.player);
		FieldDirector fieldDirector = (((Object)(object)i == (Object)null) ? null : i.field);
		if (!((Object)(object)playerCtrl == (Object)null) && !((Object)(object)fieldDirector == (Object)null) && fieldDirector.StateId >= 0)
		{
			InventoryChangeResponse inventoryChangeResponse = GameSession.Net.ChangeInventory(new InventoryChangeRequest
			{
				characterId = GameSession.CharacterId,
				itemId = itemId,
				sourcePosition = position,
				dropToField = true,
				dropQuantity = quantity,
				dropMapId = fieldDirector.StateId,
				dropX = Mathf.RoundToInt(((Component)playerCtrl).transform.position.x * 40f),
				dropY = Mathf.RoundToInt((0f - ((Component)playerCtrl).transform.position.y) * 40f)
			});
			if (!inventoryChangeResponse.ok)
			{
				BottomBar.Toast(string.IsNullOrEmpty(inventoryChangeResponse.error) ? "无法丢弃这件物品" : inventoryChangeResponse.error);
				return;
			}
			GameSession.Character.inventory = inventoryChangeResponse.inventory;
			GameSession.ApplyInventoryToProfile();
			Profile.Save();
		}
	}

	private static bool IsCashEquipment(int id)
	{
		switch (id)
		{
		default:
			if (id != 5060002 && id / 10000 != 550 && id / 10000 != 552)
			{
				return id / 10000 == 557;
			}
			break;
		case 5060000:
		case 5060001:
		case 5061000:
		case 5061001:
		case 5061002:
		case 5061003:
			break;
		}
		return true;
	}

	public static void PromptDropMeso()
	{
		if (!GameSession.InGame || GameSession.Character == null)
		{
			return;
		}
		PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
		if ((Object)(object)playerCtrl == (Object)null || playerCtrl.hp <= 0f)
		{
			BottomBar.Toast("死亡状态中不能执行该操作");
			return;
		}
		int gm = GameSession.Character.record.gm;
		int num = GameConfigDao.GetInt((GameServer.Local == null) ? null : GameServer.Local.Db, "minimum_gm_level_to_drop", 4);
		if (gm > 0 && gm < num)
		{
			BottomBar.Toast("管理账号不能扔掉金币。");
			return;
		}
		long num2 = Math.Min(Profile.gold, 50000L);
		if (num2 < 10)
		{
			BottomBar.Toast("金币不足，无法丢弃");
		}
		else
		{
			NativeUtilPrompt.NumberLong("扔掉多少？", 10L, 10L, num2, DropMeso);
		}
	}

	private static void DropMeso(long meso)
	{
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		GameLoop i = GameLoop.I;
		PlayerCtrl playerCtrl = (((Object)(object)i == (Object)null) ? null : i.player);
		FieldDirector fieldDirector = (((Object)(object)i == (Object)null) ? null : i.field);
		if (!((Object)(object)playerCtrl == (Object)null) && !((Object)(object)fieldDirector == (Object)null) && fieldDirector.StateId >= 0)
		{
			InventoryChangeResponse inventoryChangeResponse = GameSession.Net.ChangeInventory(new InventoryChangeRequest
			{
				characterId = GameSession.CharacterId,
				dropMeso = meso,
				dropMapId = fieldDirector.StateId,
				dropX = Mathf.RoundToInt(((Component)playerCtrl).transform.position.x * 40f),
				dropY = Mathf.RoundToInt((0f - ((Component)playerCtrl).transform.position.y) * 40f)
			});
			if (inventoryChangeResponse == null || !inventoryChangeResponse.ok)
			{
				BottomBar.Toast((inventoryChangeResponse == null || string.IsNullOrEmpty(inventoryChangeResponse.error)) ? "无法丢弃金币" : inventoryChangeResponse.error);
				return;
			}
			GameSession.Character.record.meso = inventoryChangeResponse.meso;
			Profile.Data.gold = checked(Profile.Data.gold - meso);
			Profile.Save();
		}
	}

	public static void DrawCarry()
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		if (Carrying)
		{
			NativeCursor.Request(12);
			Texture2D val = NativeItemSource.Icon(carriedItem);
			float num = 32f * BottomBar.S;
			if ((Object)(object)val != (Object)null)
			{
				GUI.DrawTexture(new Rect(Pointer.Pos.x - num / 2f, Pointer.Pos.y - num / 2f, num, num), (Texture)(object)val, (ScaleMode)2);
			}
		}
	}

	public static void DoubleClick(int itemId, int inventoryPosition = 0)
	{
		carriedItem = 0;
		string text = TryUse(itemId, inventoryPosition);
		if (NativeAndroidService.IsRobot(itemId) || NativeAndroidService.IsHeart(itemId))
		{
			Debug.Log((object)("[Android/Equip] item=" + itemId + " source=" + inventoryPosition + " level=" + Profile.level + " result=" + (text ?? "ok") + " robot=" + Profile.EquipOfSlot(-32) + " heart=" + Profile.EquipOfSlot(-54)));
		}
		if (!string.IsNullOrEmpty(text))
		{
			NativeUtilPrompt.Notice(text);
		}
	}

	public static bool UseStatPotion(int id, PlayerCtrl player, int count = 1)
	{
		ImgNode imgNode = NativeItemSource.Node(id)?.Get("spec");
		if (imgNode == null)
		{
			return false;
		}
		string[] array = new string[10] { "pad", "pdd", "mad", "mdd", "acc", "eva", "speed", "jump", "expBuff", "barrier" };
		string[] array2 = new string[10] { "WATK", "WDEF", "MATK", "MDEF", "ACC", "AVOID", "SPEED", "JUMP", "EXP_BUFF", "AURA" };
		HashSet<string> hashSet = new HashSet<string>(array)
		{
			"time", "hp", "mp", "hpR", "mpR", "poison", "seal", "darkness", "weakness", "curse",
			"exp", "inflation", "morphRandom", "consumeOnPickup", "onlyPickup", "prob", "BFSkill", "party", "expinc", "mhpR",
			"mmpR", "mhpRRate", "mmpRRate", "berserk", "booster", "repeatEffect", "morph", "thaw", "padRate", "madRate",
			"pddRate", "mddRate", "accRate", "evaRate", "speedRate"
		};
		foreach (ImgNode child in imgNode.Children)
		{
			if (!hashSet.Contains(child.Name))
			{
				return false;
			}
		}
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		switch (id)
		{
		case 2022001:
		case 2022186:
			dictionary["MAP_PROTECTION"] = 1;
			break;
		case 2022040:
			dictionary["MAP_PROTECTION"] = 2;
			break;
		}
		for (int i = 0; i < array.Length; i++)
		{
			int num = ((i < 8) ? ((short)imgNode.Int(array[i])) : imgNode.Int(array[i]));
			if (num != 0)
			{
				dictionary[array2[i]] = num;
			}
		}
		if (id >= 2022450 && id <= 2022452 && imgNode.Int("expinc") != 0)
		{
			dictionary["EXP_INCREASE"] = imgNode.Int("expinc");
		}
		if ((id >= 2022585 && id <= 2022588) || (id >= 2022616 && id <= 2022617))
		{
			if (imgNode.Int("berserk") != 0)
			{
				dictionary["BERSERK"] = imgNode.Int("berserk");
			}
			if (imgNode.Int("booster") != 0)
			{
				dictionary["BOOSTER"] = imgNode.Int("booster");
			}
		}
		if ((id >= 2022359 && id <= 2022421) || id == 2022198 || id == 2022337)
		{
			if (imgNode.Int("mhpR") != 0)
			{
				dictionary["HPREC"] = (sbyte)imgNode.Int("mhpR");
			}
			if (imgNode.Int("mmpR") != 0)
			{
				dictionary["MPREC"] = (sbyte)imgNode.Int("mmpR");
			}
		}
		if (imgNode.Int("morph") > 0)
		{
			dictionary["MORPH"] = imgNode.Int("morph");
		}
		bool flag = imgNode.Int("poison") > 0 || imgNode.Int("seal") > 0 || imgNode.Int("darkness") > 0 || imgNode.Int("weakness") > 0 || imgNode.Int("curse") > 0;
		if (dictionary.Count == 0 && !flag && imgNode.Int("hp") == 0 && imgNode.Int("mp") == 0 && imgNode.Int("hpR") == 0 && imgNode.Int("mpR") == 0)
		{
			return false;
		}
		int num2 = imgNode.Int("time", -1);
		int num3 = (short)imgNode.Int("hp");
		int num4 = (short)imgNode.Int("mp");
		int jobId = Profile.JobId;
		int num5 = ((jobId / 10 == 41) ? 4110000 : ((jobId / 10 == 141) ? 14110003 : 0));
		int num6 = ((num5 != 0) ? SkillProgress.LevelOf(num5) : 0);
		if (num6 > 0)
		{
			Dictionary<string, object> d = SkillBook.Get(num5)?.Level(num6);
			num2 = (int)((double)num2 * ((double)Art.Int(d, "y") / 100.0));
			num3 = (int)((double)num3 * ((double)Art.Int(d, "x") / 100.0));
			num4 = (int)((double)num4 * ((double)Art.Int(d, "x") / 100.0));
		}
		int num7 = (int)((double)player.hpMax * ((double)imgNode.Int("hpR") / 100.0));
		NativeMobDebuffs component = ((Component)player).GetComponent<NativeMobDebuffs>();
		if (component != null && component.Has(133))
		{
			num3 /= 2;
			num7 /= 2;
		}
		if (count <= 0 || !Profile.RemoveItem(id, count))
		{
			return false;
		}
		player.hp = Mathf.Clamp(player.hp + (float)(((long)num3 + (long)num7) * count), 0f, player.hpMax);
		player.mp = Mathf.Clamp(player.mp + (float)((num4 + (long)((double)player.mpMax * ((double)imgNode.Int("mpR") / 100.0))) * count), 0f, player.mpMax);
		NativeMobDebuffs component2 = ((Component)player).GetComponent<NativeMobDebuffs>();
		if (imgNode.Int("poison") > 0)
		{
			component2?.Clear(125);
		}
		if (imgNode.Int("seal") > 0)
		{
			component2?.Clear(120);
		}
		if (imgNode.Int("darkness") > 0)
		{
			component2?.Clear(121);
		}
		if (imgNode.Int("weakness") > 0)
		{
			component2?.Clear(122);
			component2?.Clear(126);
		}
		if (imgNode.Int("curse") > 0)
		{
			component2?.Clear(124);
		}
		if (dictionary.Count > 0 && num2 >= 0)
		{
			NativeBuffs.For(player).ApplyItem(id, dictionary, num2, NativeBuffs.Now);
		}
		GameSession.SaveToDb();
		return true;
	}

	public static string TryUse(int itemId, int inventoryPosition = 0)
	{
		if (!GameSession.InGame || NpcSession.Blocking)
		{
			return "当前无法使用物品";
		}
		PlayerCtrl playerCtrl = GameLoop.I?.player;
		if ((Object)(object)playerCtrl == (Object)null || playerCtrl.hp <= 0f)
		{
			return "当前无法使用物品";
		}
		if (CubeRules.GetFamily(itemId) != null || itemId == 2049400 || itemId == 2049401 || FlameCatalog.IsFlameItem(itemId) || itemId / 1000 == 2590 || itemId / 1000 == 2591)
		{
			InventoryEntry inventoryEntry = GameSession.Character.inventory.Find((InventoryEntry x) => x.itemId == itemId && x.position > 0 && (inventoryPosition == 0 || x.position == inventoryPosition));
			if (inventoryEntry == null)
			{
				return "背包里没有这个物品";
			}
			if (NativeEnhancementWindow.Visible)
			{
				return NativeEnhancementWindow.Select(inventoryEntry);
			}
			int mode;
			if (!FlameCatalog.IsFlameItem(itemId))
			{
				if (itemId / 1000 != 2590 && itemId / 1000 != 2591)
				{
					CubeRules.Family family = CubeRules.GetFamily(itemId);
					mode = ((family == null || !family.bonus) ? 1 : 2);
				}
				else
				{
					mode = 5;
				}
			}
			else
			{
				mode = 4;
			}
			NativeEnhancementWindow.Open(mode, inventoryEntry);
			return null;
		}
		if (NativeEnhancementWindow.Visible && itemId / 1000000 == 1)
		{
			return NativeEnhancementWindow.Select(GameSession.Character.inventory.Find((InventoryEntry x) => x.itemId == itemId && (inventoryPosition == 0 || x.position == inventoryPosition)));
		}
		if (NativeVegaWindow.Visible && (itemId / 1000000 == 1 || itemId / 10000 == 204))
		{
			return NativeVegaWindow.Select(GameSession.Character.inventory.Find((InventoryEntry x) => x.itemId == itemId && InventoryDao.IsBagPosition(x.position) && (inventoryPosition == 0 || x.position == inventoryPosition)));
		}
		if (itemId / 1000000 == 1)
		{
			return EquipLoadout.Equip(itemId, Profile.Attributes, inventoryPosition);
		}
		if (Profile.ItemCount(itemId) <= 0)
		{
			return "背包里没有这个物品";
		}
		if (itemId / 10000 == 212 || itemId / 10000 == 524)
		{
			InventoryChangeResponse inventoryChangeResponse = GameSession.Net.ChangeInventory(new InventoryChangeRequest
			{
				characterId = GameSession.CharacterId,
				itemId = itemId,
				sourcePosition = inventoryPosition
			});
			if (!inventoryChangeResponse.ok)
			{
				return inventoryChangeResponse.error;
			}
			GameSession.Character.inventory = inventoryChangeResponse.inventory;
			GameSession.ApplyInventoryToProfile();
			Profile.Save();
			return null;
		}
		if (itemId / 1000000 == 2)
		{
			if (itemId == 2430680)
			{
				InventoryChangeResponse inventoryChangeResponse2 = GameSession.Net.ChangeInventory(new InventoryChangeRequest
				{
					characterId = GameSession.CharacterId,
					itemId = itemId,
					sourcePosition = inventoryPosition,
					worldQuestDraw = true
				});
				if (!inventoryChangeResponse2.ok)
				{
					return inventoryChangeResponse2.error;
				}
				GameSession.Character.inventory = inventoryChangeResponse2.inventory;
				GameSession.ApplyInventoryToProfile();
				Profile.Save();
				if (!string.IsNullOrEmpty(inventoryChangeResponse2.message))
				{
					NativeSystemMessage.Drop(5, inventoryChangeResponse2.message);
				}
				return null;
			}
			if (itemId / 10000 == 237)
			{
				ImgNode imgNode = NativeItemSource.Node(itemId);
				InventoryChangeResponse inventoryChangeResponse3 = GameSession.Net.ChangeInventory(new InventoryChangeRequest
				{
					characterId = GameSession.CharacterId,
					itemId = itemId,
					bankExp = (imgNode?.Int("spec/exp") ?? 0),
					maxBankLevel = (imgNode?.Int("info/maxLevel", 256) ?? 256)
				});
				if (!inventoryChangeResponse3.ok)
				{
					return inventoryChangeResponse3.error;
				}
				GameSession.Character.inventory = inventoryChangeResponse3.inventory;
				GameSession.Character.record.gachaexp = inventoryChangeResponse3.bankedExp;
				GameSession.ApplyInventoryToProfile();
				Profile.Save();
				return null;
			}
			if (itemId == 2003609)
			{
				int num = NativeItemSource.Node(itemId)?.Int("spec/exp") ?? 0;
				if (num <= 0)
				{
					return null;
				}
				if (Profile.RemoveItem(itemId, 1, inventoryPosition))
				{
					Profile.GrantExperience(num);
				}
				return null;
			}
			if (itemId / 10000 == 204 || NativeScrollService.IsHeartScroll(itemId))
			{
				return PickOwnedItem(itemId, inventoryPosition);
			}
			if (VMatrixCatalog.Gemstone(itemId) != null)
			{
				NativeVMatrixWindow.OpenGemstone(itemId);
				return null;
			}
			if (itemId / 10000 == 227)
			{
				return NativeCatchItems.Use(itemId, inventoryPosition);
			}
			if (itemId / 10000 == 210)
			{
				return NativeItemUseEffects.SummonBag(itemId, inventoryPosition);
			}
			if (itemId / 10000 == 228 || itemId / 10000 == 229)
			{
				return NativeItemUseEffects.SkillBook(itemId, inventoryPosition);
			}
			if (itemId / 10000 == 238)
			{
				if (!NativeMonsterBook.Collect(itemId, 1))
				{
					return "无法收录这张怪物卡片";
				}
				if (!Profile.RemoveItem(itemId, 1, inventoryPosition))
				{
					return "背包里没有这个物品";
				}
				return null;
			}
			if (itemId >= 2030000 && itemId <= 2030100)
			{
				return NativeItemUseEffects.TownScroll(itemId, inventoryPosition);
			}
			if (itemId / 10000 == 226)
			{
				int num2 = NativeItemSource.Node(itemId)?.Int("spec/incFatigue") ?? 0;
				if (!Profile.RemoveItem(itemId, 1, inventoryPosition))
				{
					return "背包里没有这个物品";
				}
				CharRecord record = GameSession.Character.record;
				record.mounttiredness = Math.Max(0, record.mounttiredness + num2);
				GameSession.SaveToDb();
				return null;
			}
			if (itemId >= 2050001 && itemId <= 2050004)
			{
				if (!Profile.RemoveItem(itemId, 1))
				{
					return "背包里没有这个物品";
				}
				NativeMobDebuffs component = ((Component)playerCtrl).GetComponent<NativeMobDebuffs>();
				if (itemId == 2050001)
				{
					component?.Clear(121);
				}
				else if (itemId == 2050002)
				{
					component?.Clear(122);
					component?.Clear(126);
				}
				else if (itemId == 2050003)
				{
					component?.Clear(120);
					component?.Clear(124);
				}
				else
				{
					int[] array = new int[6] { 120, 121, 122, 124, 125, 126 };
					foreach (int id in array)
					{
						component?.Clear(id);
					}
				}
				GameSession.SaveToDb();
				return null;
			}
			ImgNode imgNode2 = NativeItemSource.Node(itemId)?.Get("spec");
			if (imgNode2 != null && imgNode2.Int("isHasItem") == 1)
			{
				InventoryChangeResponse inventoryChangeResponse4 = GameSession.Net.ChangeInventory(new InventoryChangeRequest
				{
					characterId = GameSession.CharacterId,
					itemId = itemId,
					grantItemId = imgNode2.Int("hasItemId"),
					grantQuantity = imgNode2.Int("hasItemQuantity"),
					grantMinutes = imgNode2.Int("hasItemTime", -1),
					consumeGrantSource = (imgNode2.Int("remove") == 1)
				});
				if (!inventoryChangeResponse4.ok)
				{
					return inventoryChangeResponse4.error;
				}
				GameSession.Character.inventory = inventoryChangeResponse4.inventory;
				GameSession.ApplyInventoryToProfile();
				Profile.Save();
				return null;
			}
			if (imgNode2 != null && imgNode2.Int("isNpc") == 1)
			{
				if (imgNode2.Int("remove") == 1 && !Profile.RemoveItem(itemId, 1))
				{
					return "背包里没有这个物品";
				}
				if (NpcSession.Talk(imgNode2.Int("npc"), imgNode2.Text("script")) == null)
				{
					return "物品NPC脚本尚未接入";
				}
				return null;
			}
			if (!string.IsNullOrEmpty(imgNode2?.Text("script")))
			{
				if (string.IsNullOrEmpty(ScriptSource.Load("item", imgNode2.Text("script"))))
				{
					return "源端缺失此物品的使用脚本，无法使用";
				}
				if (NpcSession.UseItem(itemId) == null)
				{
					return "物品脚本无法启动";
				}
				return null;
			}
			if (NativeItemSource.Node(itemId)?.Get("reward") != null)
			{
				return NativeItemUseEffects.RewardBox(itemId);
			}
			ImgNode imgNode3 = NativeItemSource.Node(itemId)?.Get("specEx");
			if (imgNode3 != null)
			{
				ImgNode imgNode4 = imgNode3.Get("0");
				int num4 = imgNode4?.Int("mobSkill") ?? 0;
				int num5 = imgNode4?.Int("level") ?? 0;
				ImgNode imgNode5 = ((num4 > 0 && num5 > 0) ? WzAssets.Node("Skill/MobSkill.img/" + num4 + "/level/" + num5) : null);
				if (imgNode5 == null)
				{
					return "源端物品效果数据缺失";
				}
				if (!Profile.RemoveItem(itemId, 1, inventoryPosition))
				{
					return "背包里没有这个物品";
				}
				if (imgNode4.Int("target") == 0)
				{
					NativeMobDebuffs.For(playerCtrl).Apply(num4, imgNode5.Int("x"), imgNode5.Int("y"), (long)imgNode5.Int("time") * 1000L);
				}
				GameSession.SaveToDb();
				return null;
			}
			if (NpcSession.UseRestorative(itemId))
			{
				return null;
			}
			if (UseStatPotion(itemId, playerCtrl))
			{
				return null;
			}
			return "该消耗品的特殊使用功能尚未接入";
		}
		if (itemId / 10000 == 301)
		{
			if (!playerCtrl.UseChair(itemId))
			{
				return "请站在地面上使用椅子";
			}
			return null;
		}
		if (itemId / 1000000 == 5)
		{
			int num6 = itemId / 10000;
			if (num6 == 515 || num6 == 522 || num6 == 525 || num6 == 538 || num6 == 542 || num6 == 546 || num6 == 549)
			{
				return null;
			}
			if (itemId == 5610000 || itemId == 5610001)
			{
				NativeVegaWindow.Open(itemId);
				return null;
			}
			if (itemId == 5050000)
			{
				NativeResetWindow.Open(itemId, inventoryPosition);
				return null;
			}
			if (itemId >= 5050001 && itemId <= 5050004)
			{
				NativeSpResetWindow.Open(itemId, inventoryPosition);
				return null;
			}
			if (itemId / 10000 == 503)
			{
				NativeMerchantWindow.Open(itemId);
				return null;
			}
			if (itemId / 10000 == 514)
			{
				NativePlayerShopWindow.Open(itemId);
				return null;
			}
			if (itemId / 10000 == 533)
			{
				NativeDeliveryWindow.Open(quick: true);
				return null;
			}
			if (itemId / 10000 == 504)
			{
				NativeTeleportWindow.Open(itemId, inventoryPosition);
				return null;
			}
			if (itemId / 10000 == 517)
			{
				NativeRenameWindow.Open(itemId, inventoryPosition);
				return null;
			}
			if (itemId / 10000 == 537)
			{
				NativeAdBoard.Open(itemId);
				return null;
			}
			if (itemId / 10000 == 507)
			{
				int num7 = itemId / 1000 % 10;
				if (itemId >= 5075000 && itemId <= 5075005)
				{
					NativeUtilPrompt.Text("输入要在 MapleTV 上播放的消息。", "", 0, 60, (string text) =>
					{
						//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
						if (!string.IsNullOrEmpty(text))
						{
							TvResponse tvResponse = GameSession.Net.Tv(new TvRequest
							{
								characterId = GameSession.CharacterId,
								itemId = itemId,
								message = text
							});
							if (tvResponse == null || !tvResponse.ok)
							{
								BottomBar.Toast(tvResponse?.error ?? "MapleTV 无法开始");
							}
							else if (!Profile.RemoveItem(itemId, 1, inventoryPosition))
							{
								BottomBar.Toast("背包里没有这个物品");
							}
							else
							{
								GameSession.SaveToDb();
								NativeMapleTvDisplay.Start(tvResponse);
								if (tvResponse.tvType >= 3)
								{
									BottomBar.AddChat(GameSession.Character.record.name + " : " + text, BottomBar.ChatWhite);
								}
							}
						}
					});
					return null;
				}
				switch (num7)
				{
				case 5:
					return "MapleTV 效果尚未迁移";
				default:
					return "源端未启用此喇叭";
				case 1:
				case 2:
				case 6:
				case 7:
					if (num7 == 1 && GameSession.Character.record.level <= 9)
					{
						return "10 级以上才能使用喇叭";
					}
					NativeUtilPrompt.Text((num7 == 2) ? "输入要全服广播的消息（超级喇叭）。" : "输入要广播的消息。", "", 0, 70, (string text) =>
					{
						//IL_0047: Unknown result type (might be due to invalid IL or missing references)
						if (!string.IsNullOrEmpty(text))
						{
							if (!Profile.RemoveItem(itemId, 1, inventoryPosition))
							{
								BottomBar.Toast("背包里没有这个物品");
							}
							else
							{
								GameSession.SaveToDb();
								BottomBar.AddChat(GameSession.Character.record.name + " : " + text, BottomBar.ChatWhite);
							}
						}
					});
					return null;
				}
			}
			if (itemId / 10000 == 523)
			{
				NativeItemSearchWindow.Open(itemId, inventoryPosition);
				return null;
			}
			if (itemId == 5451000)
			{
				NativeRemoteGachaponWindow.Open();
				return null;
			}
			if (itemId == 5510000)
			{
				return "死亡后可在复活确认窗口使用原地复活术";
			}
			if (itemId / 1000 == 5000)
			{
				InventoryChangeResponse inventoryChangeResponse5 = GameSession.Net.ChangeInventory(new InventoryChangeRequest
				{
					characterId = GameSession.CharacterId,
					itemId = itemId,
					sourcePosition = inventoryPosition,
					togglePet = true
				});
				if (!inventoryChangeResponse5.ok)
				{
					return inventoryChangeResponse5.error;
				}
				GameSession.Character.inventory = inventoryChangeResponse5.inventory;
				GameSession.ApplyInventoryToProfile();
				Profile.Save();
				return null;
			}
			if (itemId / 10000 == 516)
			{
				playerCtrl.Express(itemId - 5159992);
				return null;
			}
			if (itemId / 10000 == 510)
			{
				if (!Profile.RemoveItem(itemId, 1, inventoryPosition))
				{
					return "背包里没有这个物品";
				}
				AudioManager.PlayBgm("Jukebox/Congratulation");
				return null;
			}
			if (itemId == 5180000)
			{
				if (NpcSession.Talk(1032102, "waterOfLife") == null)
				{
					return "生命之水脚本无法启动";
				}
				return null;
			}
			if (itemId / 10000 == 519)
			{
				return null;
			}
			if (itemId / 10000 == 529 || (itemId / 10000 == 528 && itemId != 5281000))
			{
				return "源端未启用此道具";
			}
			if (itemId / 1000 == 5211 || itemId / 1000 == 5360)
			{
				return "此倍率券在背包中按指定时段自动生效";
			}
			if (IsCashEquipment(itemId))
			{
				return PickOwnedItem(itemId, inventoryPosition);
			}
			if (itemId / 10000 == 520)
			{
				int num8 = NativeItemSource.Node(itemId)?.Int("info/meso", -1) ?? (-1);
				if (!Profile.RemoveItem(itemId, 1, inventoryPosition))
				{
					return "背包里没有这个物品";
				}
				Profile.AddGold(num8);
				return null;
			}
			if (itemId / 10000 == 545)
			{
				if (NativeNpcShopWindow.Visible)
				{
					return null;
				}
				NativeNpcShopWindow.OpenShop(1338, fallback: false);
				if (NativeNpcShopWindow.Visible)
				{
					Profile.RemoveItem(itemId, 1, inventoryPosition);
				}
				return null;
			}
			if (itemId / 10000 == 530)
			{
				if (!UseStatPotion(itemId, playerCtrl))
				{
					return "无法应用此变身道具";
				}
				return null;
			}
			return "该现金道具的使用功能尚未接入";
		}
		return "这个物品不能直接使用";
	}
}

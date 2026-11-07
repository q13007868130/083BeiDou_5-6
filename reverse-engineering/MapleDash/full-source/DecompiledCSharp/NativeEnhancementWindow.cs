using System;
using System.Collections.Generic;
using System.Globalization;
using MapleDash.Wz;
using Server.Bot;
using UnityEngine;

public static class NativeEnhancementWindow
{
	private const string Root = "UI/Enchant.img/";

	private static readonly string[] Pages = new string[6] { "starForce", "potential", "additionalPotential", "traceTransfer", "bonusStat", "soulWeapon" };

	private static readonly string[] Titles = new string[6] { "星力强化", "主潜能", "附加潜能", "痕迹复原", "轮回星火", "灵魂武器" };

	private static readonly string[] Grades = new string[5] { "Rare", "Rare", "Epic", "Unique", "Legendary" };

	private static InventoryEntry equipment;

	private static InventoryEntry material;

	private static StarForceResponse star;

	private static PotentialResponse potential;

	private static FlameResponse flame;

	private static SoulResponse soul;

	private static int page;

	private static int owner;

	private static int gridRow;

	private static int statRow;

	private static bool safeguard;

	private static bool dragging;

	private static bool positioned;

	private static Vector2 position;

	private static Vector2 grab;

	private static readonly Dictionary<string, Texture2D> compactTextures = new Dictionary<string, Texture2D>();

	private static string error;

	private const string LegacySoulMaterialMessage = "已切换为282新版灵魂武器，不再消耗旧附魔石或灵魂宝珠。请放入武器后点击武器格启用，成长和重设烙印使用灵魂碎片。";

	private static readonly float[] PotentialCuts = new float[6] { 214f, 240f, 400f, 455f, 580f, 599f };

	private static readonly float[] StarCuts = new float[8] { 214f, 228f, 383f, 455f, 572f, 578f, 606f, 614f };

	private static readonly float[] TraceCuts = new float[6] { 280f, 310f, 370f, 420f, 580f, 600f };

	private static readonly float[] NoCuts = new float[0];

	public static bool Visible { get; private set; }

	private static float Width => (page == 5) ? 612 : 565;

	private static float Height => (page >= 4) ? 720 : 620;

	private static float[] Cuts
	{
		get
		{
			if (page != 0)
			{
				if (page != 3)
				{
					if (page < 4)
					{
						return PotentialCuts;
					}
					return NoCuts;
				}
				return TraceCuts;
			}
			return StarCuts;
		}
	}

	private static float S => Mathf.Min(BottomBar.S, Mathf.Min((UiKit.W - 24f) / Width, UiKit.H * 0.8f / Height));

	private static Rect Window
	{
		get
		{
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_005a: Unknown result type (might be due to invalid IL or missing references)
			//IL_005b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0067: Unknown result type (might be due to invalid IL or missing references)
			//IL_0089: Unknown result type (might be due to invalid IL or missing references)
			Vector2 val = (Vector2)(positioned ? NativeUi.ClampWindowPosition(position, new Vector2(Width, Height), S) : new Vector2((UiKit.W / S - Width) / 2f, (UiKit.H / S - Height) / 2f));
			return new Rect(val.x * S, val.y * S, Width * S, Height * S);
		}
	}

	private static Rect ChoiceArea
	{
		get
		{
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			return RawAt(new Rect((UiKit.W - 478f * S) / 2f, UiKit.H / 2f - 140f * S, 0f, 0f), 0f, 0f, 478f, 255f);
		}
	}

	public static bool AtPointer
	{
		get
		{
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			if (Visible)
			{
				if (!dragging)
				{
					Rect val = Window;
					if (!val.Contains(Pointer.Pos))
					{
						PotentialResponse potentialResponse = potential;
						if (potentialResponse != null && potentialResponse.pending)
						{
							val = ChoiceArea;
							if (val.Contains(Pointer.Pos))
							{
								goto IL_0059;
							}
						}
						if (page == 5)
						{
							return NativeSoulWeaponPanel.AtPointer;
						}
						return false;
					}
				}
				goto IL_0059;
			}
			return false;
			IL_0059:
			return true;
		}
	}

	private static Rect PageRect
	{
		get
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			return At(Window, 147f, 0f, 418f, 720f);
		}
	}

	private static bool Repaint
	{
		get
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Invalid comparison between Unknown and I4
			return (int)Event.current.type == 7;
		}
	}

	private static bool IsLegacySoulMaterial(int id)
	{
		if (id / 1000 != 2590)
		{
			return id / 1000 == 2591;
		}
		return true;
	}

	private static float Y(float y)
	{
		float[] cuts = Cuts;
		float num = y;
		for (int i = 0; i < cuts.Length; i += 2)
		{
			num -= Mathf.Clamp(y - cuts[i], 0f, cuts[i + 1] - cuts[i]);
		}
		return num;
	}

	private static Rect RawAt(Rect r, float x, float y, float w, float h)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(r.x + x * S, r.y + y * S, w * S, h * S);
	}

	private static Rect At(Rect r, float x, float y, float w, float h)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		return RawAt(r, x, Y(y), w, Y(y + h) - Y(y));
	}

	private static int Value(InventoryEntry item, string key)
	{
		if (item?.equipmentData == null || !item.equipmentData.TryGetValue(key, out var value))
		{
			return 0;
		}
		return Convert.ToInt32(value);
	}

	private static InventoryEntry Find(long id)
	{
		return GameSession.Character?.inventory.Find((InventoryEntry x) => x.itemRowId == id);
	}

	public static void Close()
	{
		NativeSoulWeaponPanel.Reset();
		Visible = (dragging = (positioned = false));
		equipment = null;
		material = null;
		star = null;
		potential = null;
		flame = null;
		soul = null;
		error = null;
		foreach (Texture2D value in compactTextures.Values)
		{
			Object.Destroy((Object)(object)value);
		}
		compactTextures.Clear();
	}

	public static void Open(int mode = 0, InventoryEntry cost = null)
	{
		Close();
		if (!GameSession.InGame)
		{
			return;
		}
		Visible = true;
		owner = GameSession.CharacterId;
		page = Mathf.Clamp(mode, 0, 5);
		material = cost;
		gridRow = (statRow = 0);
		safeguard = false;
		foreach (InventoryEntry item in GameSession.Character.inventory)
		{
			if (item.equipmentData != null && item.equipmentData.TryGetValue("potentialChoice", out var value) && value is string text && !string.IsNullOrEmpty(text) && MiniJsonDb.Parse(text) is Dictionary<string, object> dictionary)
			{
				equipment = item;
				page = ((!(bool)dictionary["bonus"]) ? 1 : 2);
				potential = GameSession.Net.Potential(new PotentialRequest
				{
					characterId = owner,
					inventoryItemId = item.itemRowId,
					choose = true
				});
				if (!potential.ok)
				{
					error = potential.error;
					potential = null;
				}
				break;
			}
		}
		if (page == 5)
		{
			NativeSoulWeaponPanel.Refresh();
		}
		if (potential != null)
		{
			return;
		}
		foreach (InventoryEntry item2 in GameSession.Character.inventory)
		{
			if (!string.IsNullOrEmpty(item2.flameChoice))
			{
				equipment = item2;
				material = null;
				page = 4;
				Preview();
				break;
			}
		}
		if (page == 5 && material != null && IsLegacySoulMaterial(material.itemId))
		{
			material = null;
			BottomBar.Toast("已切换为282新版灵魂武器，不再消耗旧附魔石或灵魂宝珠。请放入武器后点击武器格启用，成长和重设烙印使用灵魂碎片。");
		}
	}

	public static string Select(InventoryEntry item)
	{
		if (!Visible || item == null)
		{
			return "请选择持有的装备或方块";
		}
		PotentialResponse potentialResponse = potential;
		if (potentialResponse != null && potentialResponse.pending)
		{
			return "请先选择上一次方块结果";
		}
		FlameResponse flameResponse = flame;
		if (flameResponse != null && flameResponse.pending)
		{
			return "请先选择上一次星火结果";
		}
		if (item.itemId / 1000000 == 1)
		{
			if (page == 3 && equipment != null && Value(item, "starForceVestige") == 0 && item.itemId == equipment.itemId && item.itemRowId != equipment.itemRowId)
			{
				material = item;
			}
			else
			{
				equipment = item;
				material = ((page == 3) ? null : material);
				safeguard = false;
				statRow = 0;
			}
		}
		else
		{
			if (page == 4 && FlameCatalog.CreateTypeOf(item.itemId) >= 0)
			{
				material = item;
				Preview();
				return error;
			}
			if (IsLegacySoulMaterial(item.itemId))
			{
				page = 5;
				material = null;
				Preview();
				return "已切换为282新版灵魂武器，不再消耗旧附魔石或灵魂宝珠。请放入武器后点击武器格启用，成长和重设烙印使用灵魂碎片。";
			}
			CubeRules.Family family = CubeRules.GetFamily(item.itemId);
			bool flag = item.itemId == 2049400 || item.itemId == 2049401;
			if (FlameCatalog.CreateTypeOf(item.itemId) >= 0)
			{
				page = 4;
				material = item;
				Preview();
				return error;
			}
			if (family == null && !flag)
			{
				return "此物品不是已接入的强化道具";
			}
			page = ((family == null || !family.bonus) ? 1 : 2);
			material = item;
		}
		Preview();
		return error;
	}

	public static bool Drop(Vector2 pointer, int id, int position)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		if (Visible)
		{
			Rect val = Window;
			if (val.Contains(pointer))
			{
				InventoryEntry item = GameSession.Character.inventory.Find((InventoryEntry x) => x.itemId == id && x.position == position);
				val = At(PageRect, 9f, 32f, 409f, (page == 3) ? 199 : 219);
				if (val.Contains(pointer) || id / 1000000 != 1)
				{
					string text = Select(item);
					if (text != null)
					{
						BottomBar.Toast(text);
					}
				}
				return true;
			}
		}
		return false;
	}

	private static void Preview()
	{
		star = null;
		potential = null;
		flame = null;
		soul = null;
		error = null;
		if (page == 5)
		{
			NativeSoulWeaponPanel.Refresh();
		}
		else
		{
			if (equipment == null)
			{
				return;
			}
			equipment = Find(equipment.itemRowId);
			if (equipment == null)
			{
				error = "装备已不在身上";
				return;
			}
			if (material != null)
			{
				material = Find(material.itemRowId);
			}
			if (page >= 4)
			{
				if (page == 4 && !string.IsNullOrEmpty(equipment.flameChoice))
				{
					flame = GameSession.Net.Flame(new FlameRequest
					{
						characterId = owner,
						inventoryItemId = equipment.itemRowId,
						choose = true
					});
					if (!flame.ok)
					{
						error = flame.error;
					}
					return;
				}
				if (material == null)
				{
					error = "请点击下方材料格，选择要消耗的道具";
					return;
				}
				if (page == 4)
				{
					flame = GameSession.Net.Flame(new FlameRequest
					{
						characterId = owner,
						inventoryItemId = equipment.itemRowId,
						flameInventoryItemId = material.itemRowId
					});
					if (!flame.ok)
					{
						error = flame.error;
					}
					return;
				}
				soul = GameSession.Net.Soul(new SoulRequest
				{
					characterId = owner,
					inventoryItemId = equipment.itemRowId,
					soulInventoryItemId = material.itemRowId,
					operation = SoulOp(material.itemId)
				});
				if (!soul.ok)
				{
					error = soul.error;
				}
			}
			else if (page == 0 || page == 3)
			{
				if (page != 3 || material != null)
				{
					star = GameSession.Net.StarForce(new StarForceRequest
					{
						characterId = owner,
						inventoryItemId = equipment.itemRowId,
						sacrificeInventoryItemId = ((page == 3) ? material.itemRowId : 0),
						safeguard = safeguard
					});
					if (!star.ok)
					{
						error = star.error;
					}
				}
			}
			else if (material != null)
			{
				potential = GameSession.Net.Potential(new PotentialRequest
				{
					characterId = owner,
					inventoryItemId = equipment.itemRowId,
					cubeInventoryItemId = material.itemRowId
				});
				if (!potential.ok)
				{
					error = potential.error;
				}
			}
		}
	}

	private static void Settle(List<InventoryEntry> inventory, long meso)
	{
		GameSession.Character.inventory = inventory;
		GameSession.Character.record.meso = meso;
		Profile.Data.gold = meso;
		GameSession.ApplyInventoryToProfile();
		GameLoop.I?.player?.RefreshBuffAttributes();
		Profile.Save();
		if (equipment != null)
		{
			equipment = Find(equipment.itemRowId);
		}
		if (material != null)
		{
			material = Find(material.itemRowId);
		}
	}

	public static string Confirm()
	{
		if (!Visible || owner != GameSession.CharacterId || equipment == null)
		{
			return "请先选择装备";
		}
		PotentialResponse potentialResponse = potential;
		if (potentialResponse != null && potentialResponse.pending)
		{
			return "请先选择方块结果";
		}
		FlameResponse flameResponse = flame;
		if (flameResponse != null && flameResponse.pending)
		{
			return "请先选择星火结果";
		}
		if (page >= 4)
		{
			return ConfirmGrowth();
		}
		if (page == 0 || page == 3)
		{
			StarForceResponse starForceResponse = star;
			if (starForceResponse == null || !starForceResponse.ok || (page == 3 && material == null))
			{
				return error ?? "请选择复原材料";
			}
			StarForceResponse starForceResponse2 = GameSession.Net.StarForce(new StarForceRequest
			{
				characterId = owner,
				inventoryItemId = equipment.itemRowId,
				sacrificeInventoryItemId = ((page == 3) ? material.itemRowId : 0),
				safeguard = safeguard,
				commit = true,
				revision = star.revision
			});
			if (!starForceResponse2.ok)
			{
				Preview();
				return starForceResponse2.error;
			}
			Settle(starForceResponse2.inventory, starForceResponse2.meso);
			if (page == 0)
			{
				StarForceRules.Result result = starForceResponse2.result;
				if (result != null && result.outcome == StarForceRules.Outcome.Success)
				{
					NativeTitleTrials.AddStar(1);
				}
			}
			string text;
			if (starForceResponse2.restored)
			{
				text = "装备痕迹已复原";
			}
			else if (starForceResponse2.result.vestige)
			{
				text = "装备已损坏，痕迹已保留";
			}
			else
			{
				text = ((starForceResponse2.result.outcome == StarForceRules.Outcome.Success) ? "强化成功" : "强化失败");
			}
			BottomBar.Toast(text);
			StarForceRules.Result result2 = starForceResponse2.result;
			if (result2 != null && result2.vestige)
			{
				page = 3;
				material = null;
			}
			if (page == 0 && equipment != null && !StarForceRules.Shared.CanSafeguard(equipment.starLevel, (NativeItemSource.Node(equipment.itemId)?.Int("info/superiorEqp") ?? 0) != 0))
			{
				safeguard = false;
			}
			Preview();
			return null;
		}
		PotentialResponse potentialResponse2 = potential;
		if (potentialResponse2 == null || !potentialResponse2.ok || material == null)
		{
			return error ?? "请选择方块或潜能赋予卷轴";
		}
		PotentialResponse potentialResponse3 = GameSession.Net.Potential(new PotentialRequest
		{
			characterId = owner,
			inventoryItemId = equipment.itemRowId,
			cubeInventoryItemId = material.itemRowId,
			commit = true,
			revision = potential.revision
		});
		if (!potentialResponse3.ok)
		{
			Preview();
			return potentialResponse3.error;
		}
		Settle(potentialResponse3.inventory, potentialResponse3.meso);
		potential = potentialResponse3;
		if (!string.IsNullOrEmpty(potentialResponse3.scrollResult))
		{
			BottomBar.Toast((potentialResponse3.scrollResult == "success") ? "潜能赋予成功" : "潜能赋予失败");
		}
		if (!potentialResponse3.pending)
		{
			Preview();
			if (string.IsNullOrEmpty(potentialResponse3.scrollResult))
			{
				BottomBar.Toast("潜能已重新设定");
			}
		}
		return null;
	}

	private static SoulOperation SoulOp(int itemId)
	{
		if (itemId / 1000 != 2590)
		{
			return SoulOperation.Enchant;
		}
		return SoulOperation.Socket;
	}

	private static string ConfirmGrowth()
	{
		if (material == null)
		{
			return "请选择要消耗的道具";
		}
		if (page == 4)
		{
			FlameResponse flameResponse = flame;
			if (flameResponse == null || !flameResponse.ok)
			{
				return error ?? "请先取得有效预览";
			}
			FlameResponse flameResponse2 = GameSession.Net.Flame(new FlameRequest
			{
				characterId = owner,
				inventoryItemId = equipment.itemRowId,
				flameInventoryItemId = material.itemRowId,
				commit = true,
				revision = flame.revision
			});
			if (!flameResponse2.ok)
			{
				Preview();
				return flameResponse2.error;
			}
			Settle(flameResponse2.inventory, GameSession.Character.record.meso);
			if (flameResponse2.pending)
			{
				flame = flameResponse2;
				BottomBar.Toast("请选择要保留的星火结果");
				return null;
			}
			BottomBar.Toast((flameResponse2.candidate != null && flameResponse2.candidate.Count > 0) ? "星火已附加" : "星火没有产生任何属性");
			Preview();
			return null;
		}
		SoulOperation soulOperation = SoulOp(material.itemId);
		SoulResponse soulResponse = GameSession.Net.Soul(new SoulRequest
		{
			characterId = owner,
			inventoryItemId = equipment.itemRowId,
			soulInventoryItemId = material.itemRowId,
			operation = soulOperation,
			commit = true
		});
		if (!soulResponse.ok)
		{
			Preview();
			return soulResponse.error;
		}
		Settle(soulResponse.inventory, GameSession.Character.record.meso);
		string text;
		if (soulOperation != SoulOperation.Socket)
		{
			text = "灵魂已镶嵌：" + soulResponse.optionKey;
		}
		else
		{
			text = (soulResponse.success ? "灵魂插槽已开启" : "开孔失败，开孔器已消耗");
		}
		BottomBar.Toast(text);
		Preview();
		return null;
	}

	public static string ChooseFlame(bool keepNew)
	{
		if (Visible && equipment != null && page == 4)
		{
			FlameResponse flameResponse = flame;
			if (flameResponse != null && flameResponse.pending)
			{
				FlameResponse flameResponse2 = GameSession.Net.Flame(new FlameRequest
				{
					characterId = owner,
					inventoryItemId = equipment.itemRowId,
					choose = true,
					commit = true,
					keepNew = keepNew,
					revision = flame.revision
				});
				if (!flameResponse2.ok)
				{
					return flameResponse2.error;
				}
				Settle(flameResponse2.inventory, GameSession.Character.record.meso);
				BottomBar.Toast(keepNew ? "已使用新的星火结果" : "已保留原来的星火结果");
				Preview();
				return null;
			}
		}
		return "没有待确认的星火结果";
	}

	public static string Choose(bool keepNew)
	{
		if (Visible && equipment != null)
		{
			PotentialResponse potentialResponse = potential;
			if (potentialResponse != null && potentialResponse.pending)
			{
				PotentialResponse potentialResponse2 = GameSession.Net.Potential(new PotentialRequest
				{
					characterId = owner,
					inventoryItemId = equipment.itemRowId,
					choose = true,
					commit = true,
					keepNew = keepNew,
					revision = potential.revision
				});
				if (!potentialResponse2.ok)
				{
					return potentialResponse2.error;
				}
				Settle(potentialResponse2.inventory, potentialResponse2.meso);
				Preview();
				return null;
			}
		}
		return "没有待选择的结果";
	}

	private static void Image(Rect r, string path, float x = 0f, float y = 0f, bool remap = true)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		ImgNode imgNode = WzAssets.Node("UI/Enchant.img/" + path);
		if (imgNode == null)
		{
			return;
		}
		Texture2D val = WzAssets.Texture(imgNode);
		if ((Object)(object)val == (Object)null)
		{
			return;
		}
		Vector2 val2 = WzAssets.Origin(imgNode);
		float num = y - val2.y;
		float x2 = x - val2.x;
		if (!remap)
		{
			GUI.DrawTexture(RawAt(r, x2, num, ((Texture)val).width, ((Texture)val).height), (Texture)(object)val);
			return;
		}
		int num2 = Mathf.RoundToInt(Y(num + (float)((Texture)val).height) - Y(num));
		if (num2 <= 0)
		{
			return;
		}
		if (num2 != ((Texture)val).height)
		{
			string text = path + "|" + page + "|" + num;
			if (!compactTextures.TryGetValue(text, out var value))
			{
				byte[] src = WzAssets.Pixels(imgNode);
				int num3 = ((Texture)val).width * 4;
				int num4 = 0;
				byte[] array = new byte[num3 * num2];
				for (int i = 0; i < ((Texture)val).height; i++)
				{
					if (Y(num + (float)i + 1f) > Y(num + (float)i))
					{
						Buffer.BlockCopy(src, i * num3, array, num4 * num3, num3);
						num4++;
					}
				}
				value = WzAssets.Upload(array, ((Texture)val).width, num2, "Enchant/" + text);
				compactTextures.Add(text, value);
			}
			val = value;
		}
		GUI.DrawTexture(RawAt(r, x2, Y(num), ((Texture)val).width, ((Texture)val).height), (Texture)(object)val);
	}

	private static void ChineseTitle(Rect r, string background, string title, float blankX, float width, bool remap = true)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = WzAssets.Texture(WzAssets.Node("UI/Enchant.img/" + background));
		if (!((Object)(object)val == (Object)null))
		{
			GUI.DrawTextureWithTexCoords(RawAt(r, 8f, 7f, width, 18f), (Texture)(object)val, new Rect(blankX / (float)((Texture)val).width, 1f - 25f / (float)((Texture)val).height, 1f / (float)((Texture)val).width, 18f / (float)((Texture)val).height));
			Text(r, title, 10f, 8f, width - 4f, "common/font:meso", (TextAnchor)0, 20f, remap);
		}
	}

	private static bool Button(Rect r, string path)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		ImgNode imgNode = WzAssets.Node("UI/Enchant.img/" + path + "/normal/0");
		if (imgNode == null)
		{
			return false;
		}
		Texture2D val = WzAssets.Texture(imgNode);
		if ((Object)(object)val == (Object)null)
		{
			return false;
		}
		Vector2 val2 = WzAssets.Origin(imgNode);
		Rect val3 = At(r, 0f - val2.x, 0f - val2.y, ((Texture)val).width, ((Texture)val).height);
		bool flag = val3.Contains(Pointer.Pos);
		bool flag2 = GUI.enabled && (!NativeUtilPrompt.Visible || NativeUtilPrompt.Drawing);
		string text;
		if (flag2)
		{
			if (flag)
			{
				text = (Pointer.Down ? "pressed" : "mouseOver");
			}
			else
			{
				text = "normal";
			}
		}
		else
		{
			text = "disabled";
		}
		Texture2D val4 = WzAssets.Texture(WzAssets.Node("UI/Enchant.img/" + path + "/" + text + "/0")) ?? val;
		GUI.DrawTexture(val3, (Texture)(object)val4);
		if (!flag2 || !Repaint || !Pointer.Clicked || !flag)
		{
			return false;
		}
		Pointer.ConsumeClick();
		AudioManager.Play("UI/BtMouseClick", 0.7f, pitchVary: false);
		return true;
	}

	private static void Text(Rect r, string text, float x, float y, float w, string font, TextAnchor align = (TextAnchor)0, float height = 20f, bool remap = true)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Expected Obj, but got Unknown
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		ImgNode imgNode = WzAssets.Node("UI/Enchant.img/" + font);
		GUIStyle val = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = Mathf.Max(1, Mathf.RoundToInt((float)Mathf.Max(14, imgNode?.Int("fontSize", 12) ?? 12) * S)),
			fontStyle = (FontStyle)1,
			alignment = align,
			wordWrap = true
		};
		if (uint.TryParse(imgNode?.Text("fontColor") ?? "FFFFFFFF", NumberStyles.HexNumber, null, out var result))
		{
			val.normal.textColor = Color32.op_Implicit(new Color32((byte)(result >> 16), (byte)(result >> 8), (byte)result, (byte)(result >> 24)));
		}
		GUI.Label(remap ? At(r, x, y, w, height) : RawAt(r, x, y, w, height), text, val);
	}

	private static void Icon(Rect r, InventoryEntry item, float x, float y, float size = 32f)
	{
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		if (item == null)
		{
			return;
		}
		if (size == 64f)
		{
			ImgNode imgNode = NativeItemSource.IconNode(item.itemId);
			if (imgNode != null)
			{
				Texture2D val = WzAssets.Texture(imgNode);
				if ((Object)(object)val != (Object)null)
				{
					GUI.DrawTexture(At(r, x + 11f, y - 33f, 64f, 64f), (Texture)(object)val, (ScaleMode)2);
				}
			}
		}
		else
		{
			Texture2D val2 = NativeItemSource.Icon(item.itemId);
			if ((Object)(object)val2 != (Object)null)
			{
				GUI.DrawTexture(At(r, x, y, size, size), (Texture)(object)val2, (ScaleMode)2);
			}
		}
	}

	private static string Describe(int option)
	{
		if (option == 0)
		{
			return "";
		}
		if (option < 0)
		{
			return "未鉴定潜能";
		}
		int rawLevel = (NativeItemSource.Node(equipment.itemId)?.Int("info/reqLevel") ?? 0) + Value(equipment, "iIncReq");
		return PotentialCatalog.Shared.Describe(option, PotentialCatalog.OptionLevel(rawLevel)).Replace("STR", "力量").Replace("DEX", "敏捷")
			.Replace("INT", "智力")
			.Replace("LUK", "幸运")
			.Replace("BOSS", "首领");
	}

	private static int[] Options(bool bonus)
	{
		return new int[3]
		{
			Value(equipment, bonus ? "bonusOption1" : "potentialOption1"),
			Value(equipment, bonus ? "bonusOption2" : "potentialOption2"),
			Value(equipment, bonus ? "bonusOption3" : "potentialOption3")
		};
	}

	private static void DrawChoice()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		string text = "fullScreen_" + Pages[page] + "/choiceBox/";
		NativeUi.RegisterWindow("NativeEnhancementChoice", ChoiceArea, () => Visible && (potential?.pending ?? false));
		int[] array = Options(page == 2);
		for (int num = 0; num < 2; num++)
		{
			Rect r = RawAt(ChoiceArea, num * 244, 0f, 234f, 255f);
			Image(r, text + "canvas:base" + ((num == 0) ? "Before" : "After"), 0f, 0f, remap: false);
			ChineseTitle(r, text + "canvas:base" + ((num == 0) ? "Before" : "After"), (num == 0) ? "保留原潜能" : "使用新潜能", 220f, 210f, remap: false);
			bool flag = r.Contains(Pointer.Pos);
			if (flag)
			{
				Image(r, text + "layer:mouseOver", 0f, 0f, remap: false);
			}
			int num2 = ((num == 0) ? Value(equipment, (page == 2) ? "bonusGrade" : "potentialGrade") : potential.grade);
			Image(r, text + "title/canvas:title_" + Grades[Mathf.Clamp(num2, 1, 4)].ToLowerInvariant(), 0f, 0f, remap: false);
			int[] array2 = ((num == 0) ? array : potential.candidate);
			for (int num3 = 0; num3 < 3; num3++)
			{
				Text(r, Describe(array2[num3]), 24f, 93 + num3 * 29, 184f, text + "detail/font:detail", (TextAnchor)0, 29f, remap: false);
			}
			if ((GUI.enabled && Repaint && Pointer.Clicked) & flag)
			{
				Pointer.ConsumeClick();
				string text2 = Choose(num == 1);
				if (text2 != null)
				{
					BottomBar.Toast(text2);
				}
				break;
			}
		}
	}

	private static void DrawMaterials(Rect r)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0246: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0312: Unknown result type (might be due to invalid IL or missing references)
		//IL_02df: Unknown result type (might be due to invalid IL or missing references)
		//IL_034c: Unknown result type (might be due to invalid IL or missing references)
		//IL_035b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0360: Unknown result type (might be due to invalid IL or missing references)
		//IL_0364: Unknown result type (might be due to invalid IL or missing references)
		bool flag = page == 3;
		string text = Pages[page];
		float num = (flag ? 19 : ((page == 2) ? 8 : 20));
		float num2 = (flag ? 489 : ((page == 2) ? 461 : 492));
		if (!flag)
		{
			Image(r, (page == 2) ? (text + "/layer:costInvenBox") : (text + "/costItem/layer:costItemBox"), (page != 2) ? 10 : 0, (page != 2) ? 467 : 0);
		}
		List<InventoryEntry> list = new List<InventoryEntry>();
		foreach (InventoryEntry item in GameSession.Character.inventory)
		{
			if (item.position <= 0 || item.itemRowId == equipment?.itemRowId)
			{
				continue;
			}
			CubeRules.Family family = CubeRules.GetFamily(item.itemId);
			bool num3;
			if (!flag)
			{
				if (family == null || family.bonus != (page == 2))
				{
					if (page != 1)
					{
						continue;
					}
					if (item.itemId != 2049400)
					{
						num3 = item.itemId == 2049401;
						goto IL_0186;
					}
				}
				goto IL_0188;
			}
			if (equipment == null || item.itemId != equipment.itemId)
			{
				continue;
			}
			num3 = Value(item, "starForceVestige") == 0;
			goto IL_0186;
			IL_0186:
			if (!num3)
			{
				continue;
			}
			goto IL_0188;
			IL_0188:
			list.Add(item);
		}
		list.Sort((InventoryEntry a, InventoryEntry b) => a.position.CompareTo(b.position));
		int num4 = Math.Max(0, (list.Count + 8) / 9 - 2);
		gridRow = Mathf.Clamp(gridRow, 0, num4);
		Rect val;
		if (GUI.enabled && Repaint)
		{
			val = At(r, num, num2, 380f, 83f);
			if (val.Contains(Pointer.Pos) && Mathf.Abs(Input.mouseScrollDelta.y) > 0.01f)
			{
				gridRow = Mathf.Clamp(gridRow - (int)Mathf.Sign(Input.mouseScrollDelta.y), 0, num4);
			}
		}
		for (int num5 = 0; num5 < 18 && num5 + gridRow * 9 < list.Count; num5++)
		{
			InventoryEntry inventoryEntry = list[num5 + gridRow * 9];
			float num6 = num + (float)(num5 % 9 * 41);
			float num7 = num2 + 2f + (float)(num5 / 9 * 41);
			if (material?.itemRowId == inventoryEntry.itemRowId)
			{
				Image(r, text + "/costItem/inven/selected", num6, num7);
			}
			Icon(r, inventoryEntry, num6 + 4f, num7 - 1f);
			Text(r, inventoryEntry.quantity.ToString(), num6, num7 + 25f, 38f, text + "/costItem/font:costItem", (TextAnchor)0);
			if (!Repaint)
			{
				continue;
			}
			val = At(r, num6, num7, 38f, 38f);
			if (val.Contains(Pointer.Pos))
			{
				ItemTooltip.SetHover(inventoryEntry.itemId, equipped: false, inventoryEntry);
				if (GUI.enabled && Pointer.Clicked)
				{
					Pointer.ConsumeClick();
					material = inventoryEntry;
					Preview();
				}
			}
		}
		if (material != null)
		{
			Text(r, BotItemCatalog.Shared.Name(material.itemId), 19f, flag ? 467 : 474, 380f, text + "/costItem/font:costItem", (TextAnchor)1);
		}
	}

	public static void Draw()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		//IL_020a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0312: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0366: Unknown result type (might be due to invalid IL or missing references)
		//IL_034e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0273: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0293: Unknown result type (might be due to invalid IL or missing references)
		//IL_0427: Unknown result type (might be due to invalid IL or missing references)
		//IL_0441: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_040b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0498: Unknown result type (might be due to invalid IL or missing references)
		//IL_048c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e9e: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0eb0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0512: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dc7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d9c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f25: Unknown result type (might be due to invalid IL or missing references)
		//IL_0df7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ff3: Unknown result type (might be due to invalid IL or missing references)
		//IL_102c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1041: Unknown result type (might be due to invalid IL or missing references)
		//IL_1046: Unknown result type (might be due to invalid IL or missing references)
		//IL_104a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e59: Unknown result type (might be due to invalid IL or missing references)
		//IL_06d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0654: Unknown result type (might be due to invalid IL or missing references)
		//IL_0680: Unknown result type (might be due to invalid IL or missing references)
		//IL_069d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c0f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0761: Unknown result type (might be due to invalid IL or missing references)
		//IL_078d: Unknown result type (might be due to invalid IL or missing references)
		//IL_07b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0812: Unknown result type (might be due to invalid IL or missing references)
		//IL_0881: Unknown result type (might be due to invalid IL or missing references)
		//IL_08da: Unknown result type (might be due to invalid IL or missing references)
		//IL_092b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c6b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c90: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a90: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aa5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aaa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aae: Unknown result type (might be due to invalid IL or missing references)
		//IL_0abf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b3b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b6e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ce9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cfe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d03: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d07: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible)
		{
			return;
		}
		if (!GameSession.InGame || owner != GameSession.CharacterId)
		{
			Close();
			return;
		}
		Rect window = Window;
		Rect val;
		if (Repaint && GUI.enabled)
		{
			if (Pointer.Pressed)
			{
				val = RawAt(window, 0f, 0f, Width - 30f, 28f);
				if (val.Contains(Pointer.Pos))
				{
					dragging = (positioned = true);
					position = window.position / S;
					grab = Pointer.Pos / S - position;
					Pointer.ConsumeClick();
				}
			}
			if (dragging)
			{
				if (Pointer.Down)
				{
					position = NativeUi.ClampWindowPosition(Pointer.Pos / S - grab, new Vector2(Width, Height), S);
				}
				else
				{
					dragging = false;
					Pointer.ConsumeClick();
				}
				window = Window;
			}
		}
		Rect val2 = At(window, 147f, 0f, 418f, 720f);
		string text = Pages[page];
		NativeUi.RegisterWindow("NativeEnhancementWindow", window, () => Visible);
		Image(window, "common/backgrnd");
		if (page != 5)
		{
			Image(val2, text + "/backgrnd");
		}
		ChineseTitle(window, "common/backgrnd", "装备强化", 120f, 125f);
		if (page != 5)
		{
			ChineseTitle(val2, text + "/backgrnd", Titles[page], 330f, 300f);
		}
		for (int num = 0; num < Pages.Length; num++)
		{
			Rect val3 = RawAt(window, 13f, 66 + num * 62, 120f, 59f);
			ImgNode imgNode = WzAssets.Node("UI/Enchant.img/" + Pages[num] + "/tab/" + ((num == page) ? "checked" : "normal") + "/0");
			Texture2D val4 = ((imgNode != null) ? WzAssets.Texture(imgNode) : null);
			if ((Object)(object)val4 != (Object)null)
			{
				GUI.DrawTexture(val3, (Texture)(object)val4);
			}
			if (!GUI.enabled || !Repaint || !Pointer.Clicked || !val3.Contains(Pointer.Pos))
			{
				continue;
			}
			PotentialResponse potentialResponse = potential;
			if (potentialResponse == null || !potentialResponse.pending)
			{
				FlameResponse flameResponse = flame;
				if ((flameResponse == null || !flameResponse.pending) && (page != 5 || !NativeSoulWeaponPanel.HasModal))
				{
					Pointer.ConsumeClick();
					page = num;
					material = null;
					safeguard = false;
					gridRow = 0;
					Preview();
					return;
				}
			}
		}
		Text(window, Profile.gold.ToString("N0"), 4f, 701f, 138f, "common/font:meso", (TextAnchor)1, 18f);
		if (page == 5)
		{
			if (NativeSoulWeaponPanel.Draw(val2, S, equipment))
			{
				Close();
			}
			return;
		}
		if (Button(val2, "common/commonButton/button:close"))
		{
			Close();
			return;
		}
		PotentialResponse potentialResponse2 = potential;
		if (potentialResponse2 != null && potentialResponse2.pending)
		{
			DrawChoice();
			return;
		}
		if (page < 4 && equipment == null)
		{
			Image(val2, text + "/layer:waitEquip");
		}
		else if (page < 4)
		{
			if (page == 3)
			{
				Image(val2, text + "/layer:onEquip");
				Icon(val2, equipment, 81f, 112f, 64f);
				Icon(val2, material, 251f, 112f, 64f);
			}
			else
			{
				Icon(val2, equipment, 166f, 112f, 64f);
				Text(val2, BotItemCatalog.Shared.Name(equipment.itemId), 9f, 184f, 400f, text + "/item/font:itemName", (TextAnchor)1);
			}
		}
		if (page >= 4)
		{
			if (page == 4)
			{
				DrawFlameTab(val2, text);
			}
			else
			{
				DrawSoulTab(val2, text);
			}
		}
		else if (page == 0)
		{
			if (equipment == null)
			{
				Image(val2, text + "/layer:waitEquipMeso");
			}
			else
			{
				string[] array = new string[5] { "summaryBox", "diffBox", "statBox", "probBox", "costMesoBox" };
				foreach (string text2 in array)
				{
					Image(val2, text + "/layer:" + text2);
				}
				bool flag = (NativeItemSource.Node(equipment.itemId)?.Int("info/superiorEqp") ?? 0) != 0;
				StarForceResponse starForceResponse = star;
				int num3 = ((starForceResponse != null && starForceResponse.cap > 0) ? star.cap : StarForceRules.Shared.Cap(equipment.itemId, (NativeItemSource.Node(equipment.itemId)?.Int("info/reqLevel") ?? 0) + Value(equipment, "iIncReq"), flag));
				StarForceStars.Draw(val2.x + 209f * S, val2.y + Y(260f) * S, S, equipment.starLevel, num3, flag);
				StarForceResponse starForceResponse2 = star;
				if (starForceResponse2 != null && starForceResponse2.ok && star.quote != null)
				{
					string text3 = text + "/diff/" + (flag ? "blue" : "normal");
					Image(val2, text3 + "/before/" + equipment.starLevel);
					Image(val2, text3 + "/layer:arrow");
					Image(val2, text3 + "/after/" + (equipment.starLevel + 1));
				}
				else
				{
					Text(val2, (equipment.starLevel >= num3) ? ("已达 " + num3 + " 星上限") : ("当前 " + equipment.starLevel + " 星"), 66f, 285f, 286f, text + "/font:statValue", (TextAnchor)1);
				}
				StarForceResponse starForceResponse3 = star;
				if (starForceResponse3 != null && starForceResponse3.ok && star.quote != null)
				{
					Text(val2, "失败（不破坏）", 18f, 489f, 180f, text + "/font:probName", (TextAnchor)0, 16f);
					Text(val2, "破坏概率", 18f, 505f, 180f, text + "/font:probName", (TextAnchor)0, 16f);
					Text(val2, ((double)star.quote.success / 100.0).ToString("0.##") + "%", 214f, 473f, 180f, text + "/font:probNormal", (TextAnchor)0, 16f);
					Text(val2, ((double)(10000 - star.quote.success - star.quote.destroy) / 100.0).ToString("0.##") + "%", 214f, 489f, 180f, text + "/font:probNormal", (TextAnchor)0, 16f);
					Text(val2, ((double)star.quote.destroy / 100.0).ToString("0.##") + "%", 214f, 505f, 180f, text + "/font:probNormal", (TextAnchor)0, 16f);
					Text(val2, star.quote.cost.ToString("N0"), 96f, 549f, 240f, text + "/costMeso/font:costMesoNormal", (TextAnchor)1);
					if (star.quote.chanceTime)
					{
						Image(val2, text + "/layer:chanceTime");
					}
					string[] array2 = new string[14]
					{
						"watk", "matk", "str", "dex", "int", "luk", "hp", "mp", "wdef", "mdef",
						"acc", "avoid", "speed", "jump"
					};
					string[] array3 = new string[14]
					{
						"星力攻击力", "星力魔法力", "星力力量", "星力敏捷", "星力智力", "星力运气", "星力HP", "星力MP", "星力防御力", "星力魔法防御力",
						"星力命中率", "星力回避率", "星力移动速度", "星力跳跃力"
					};
					List<int> list = new List<int>();
					for (int num4 = 0; num4 < array2.Length; num4++)
					{
						if (star.nextStats.TryGetValue(array2[num4], out var value) && value != 0)
						{
							list.Add(num4);
						}
					}
					if (GUI.enabled && Repaint)
					{
						val = At(val2, 18f, 318f, 382f, 64f);
						if (val.Contains(Pointer.Pos))
						{
							statRow -= (int)Mathf.Sign(Input.mouseScrollDelta.y);
						}
					}
					statRow = Mathf.Clamp(statRow, 0, Math.Max(0, list.Count - 4));
					for (int num5 = 0; num5 < 4 && num5 + statRow < list.Count; num5++)
					{
						int num6 = list[num5 + statRow];
						string key = array2[num6];
						star.derivedStats.TryGetValue(key, out var value2);
						int num7 = star.nextStats[key];
						Text(val2, array3[num6], 18f, 318 + num5 * 16, 169f, text + "/font:statName", (TextAnchor)2, 16f);
						Text(val2, "+" + value2 + " → +" + num7 + "（增加 " + (num7 - value2) + "）", 196f, 318 + num5 * 16, 204f, text + "/font:statValue", (TextAnchor)0, 16f);
					}
				}
				if (flag)
				{
					Image(val2, text + "/layer:superior");
				}
				StarForceResponse starForceResponse4 = star;
				bool flag2 = starForceResponse4 != null && starForceResponse4.ok && StarForceRules.Shared.CanSafeguard(equipment.starLevel, flag);
				bool enabled = GUI.enabled;
				GUI.enabled &= flag2;
				if (Button(val2, text + "/button_protectDestroy/button"))
				{
					safeguard = !safeguard;
					Preview();
				}
				Rect r = val2;
				string text4 = text;
				string text5;
				if (safeguard)
				{
					text5 = (flag2 ? "checked" : "checkedAndDisabled");
				}
				else
				{
					text5 = "unchecked";
				}
				Image(r, text4 + "/button_protectDestroy/" + text5);
				GUI.enabled = enabled;
				if (enabled && Repaint && Pointer.Clicked)
				{
					val = At(val2, 111f, 579f, 195f, 22f);
					if (val.Contains(Pointer.Pos) && !flag2)
					{
						Pointer.ConsumeClick();
						string text6;
						if (!flag)
						{
							StarForceResponse starForceResponse5 = star;
							text6 = ((starForceResponse5 == null || !starForceResponse5.ok) ? (error ?? "请先取得有效强化预览") : "仅当前15、16、17星的普通装备可开启，费用为3倍");
						}
						else
						{
							text6 = "尊贵装备不能使用防破坏保护";
						}
						BottomBar.Toast(text6);
					}
				}
			}
		}
		else
		{
			if (page != 3 && equipment != null)
			{
				int num8 = Value(equipment, (page == 2) ? "bonusGrade" : "potentialGrade");
				if (num8 > 0)
				{
					Image(val2, text + "/summaryBox/layer:summary" + Grades[Mathf.Clamp(num8, 1, 4)]);
				}
				Image(val2, text + "/layer:statBox");
				int[] array4 = Options(page == 2);
				for (int num9 = 0; num9 < 3; num9++)
				{
					Text(val2, Describe(array4[num9]), 24f, 313 + num9 * 24, 370f, text + "/font:stat", (TextAnchor)0, 24f);
				}
				PotentialResponse potentialResponse3 = potential;
				if (potentialResponse3 != null && potentialResponse3.ok && string.IsNullOrEmpty(error))
				{
					Text(val2, "费用：" + potential.cost.ToString("N0"), 104f, 644f, 210f, text + "/font:stat", (TextAnchor)1);
				}
			}
			DrawMaterials(val2);
		}
		if (!string.IsNullOrEmpty(error))
		{
			Rect r2 = val2;
			string text7 = error;
			float y = ((page >= 4) ? 640 : 615);
			string text8 = text;
			string text9;
			if (page == 5)
			{
				text9 = "/detailSoul/font:statNormal";
			}
			else
			{
				text9 = ((page == 4) ? "/font:statNormal" : "/font:stat");
			}
			Text(r2, text7, 18f, y, 382f, text8 + text9, (TextAnchor)1, 32f);
		}
		if (page == 4)
		{
			FlameResponse flameResponse2 = flame;
			if (flameResponse2 != null && flameResponse2.pending)
			{
				DrawFlameChoice(val2);
				return;
			}
		}
		bool enabled2 = GUI.enabled;
		GUI.enabled &= equipment != null && ((page != 0 && page != 3) ? ((page != 4) ? ((page != 5) ? (potential?.ok ?? false) : (soul?.ok ?? false)) : (flame?.ok ?? false)) : (star?.ok ?? false));
		if (Button(path: text + "/" + ((page == 4) ? "button:confirm" : ((page != 5) ? "button:confirm" : ((SoulOp(material?.itemId ?? 0) == SoulOperation.Socket) ? "button:confirmEnchanter" : "button:confirmSoul"))), r: val2))
		{
			string text10 = Confirm();
			if (text10 != null)
			{
				BottomBar.Toast(text10);
			}
		}
		GUI.enabled = enabled2;
		if (Repaint && equipment != null)
		{
			val = At(val2, 9f, 32f, 409f, 219f);
			if (val.Contains(Pointer.Pos))
			{
				ItemTooltip.SetHover(equipment.itemId, equipped: false, equipment);
			}
		}
	}

	private static void DrawFlameTab(Rect r, string mode)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0353: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_038b: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_025e: Unknown result type (might be due to invalid IL or missing references)
		if (equipment == null)
		{
			Image(r, mode + "/layer:waitEquip");
			DrawGrowthGrid(r, flameTab: true);
			return;
		}
		Icon(r, equipment, 166f, 112f, 64f);
		Text(r, BotItemCatalog.Shared.Name(equipment.itemId), 9f, 184f, 400f, mode + "/item/font:itemName", (TextAnchor)1);
		Image(r, mode + "/layer:summaryBox");
		Text(r, "轮回星火", 30f, 246f, 160f, mode + "/font:statHighlight", (TextAnchor)0);
		List<FlameLine> list = NativeGrowthStats.DescribeFlame(equipment);
		Text(r, list.Count + " 条属性", 206f, 246f, 140f, mode + "/font:statNormal", (TextAnchor)2);
		Image(r, mode + "/layer:statBox");
		bool flag = flame?.pending ?? false;
		float x = (flag ? 18 : 40);
		float w = (flag ? 176 : 330);
		for (int i = 0; i < list.Count && i < 4; i++)
		{
			FlameLine flameLine = list[i];
			if (flameLine.value > 0)
			{
				Text(r, flameLine.label, x, 293 + i * 30, w, mode + "/font:statNormal", (TextAnchor)0, 26f);
				Text(r, "+" + flameLine.value, x, 293 + i * 30, w, mode + "/font:statHighlight", (TextAnchor)2, 26f);
			}
		}
		if (flag)
		{
			List<FlameLine> candidate = flame.candidate;
			for (int j = 0; j < candidate.Count && j < 4; j++)
			{
				FlameLine flameLine2 = candidate[j];
				if (flameLine2.value > 0)
				{
					Text(r, flameLine2.label, 214f, 293 + j * 30, 176f, mode + "/font:statNormal", (TextAnchor)0, 26f);
					Text(r, "+" + flameLine2.value, 214f, 293 + j * 30, 176f, mode + "/font:statHighlight", (TextAnchor)2, 26f);
				}
			}
		}
		Image(r, mode + "/layer:atkPowBox");
		int num = Power(list);
		int num2 = (flag ? Power(flame.candidate) : num);
		if (flag)
		{
			Text(r, "攻击力 " + num + " → " + num2, 60f, 422f, 300f, (num2 >= num) ? (mode + "/font:atkPow") : (mode + "/font:atkPowMinus"), (TextAnchor)1, 22f);
		}
		else
		{
			Text(r, "攻击力 " + num, 60f, 422f, 300f, mode + "/font:atkPow", (TextAnchor)1, 22f);
		}
		Image(r, mode + "/tab:cost/normal/0");
		Image(r, mode + "/tab:cost/selected/1");
		DrawGrowthGrid(r, flameTab: true);
	}

	private static int Power(List<FlameLine> lines)
	{
		if (lines == null)
		{
			return 0;
		}
		int num = 0;
		foreach (FlameLine line in lines)
		{
			if (line.value > 0)
			{
				switch (line.statVal)
				{
				case 10:
				case 11:
					num += line.value * 4;
					break;
				case 0:
				case 1:
				case 2:
				case 3:
					num += line.value;
					break;
				case 4:
				case 5:
				case 6:
				case 7:
				case 8:
				case 9:
					num += line.value * 2;
					break;
				case 13:
					num += line.value / 10;
					break;
				default:
					num += line.value;
					break;
				}
			}
		}
		return num;
	}

	private static void DrawFlameChoice(Rect r)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		string text = Pages[page];
		Text(r, "请选择要保留的结果", 10f, 636f, 398f, text + "/font:statNormal", (TextAnchor)1, 18f);
		if (Button(r, text + "/button:confirm1"))
		{
			string text2 = ChooseFlame(keepNew: false);
			if (text2 != null)
			{
				BottomBar.Toast(text2);
			}
		}
		else if (Button(r, text + "/button:confirm"))
		{
			string text3 = ChooseFlame(keepNew: true);
			if (text3 != null)
			{
				BottomBar.Toast(text3);
			}
		}
	}

	private static void DrawSoulTab(Rect r, string mode)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_030b: Unknown result type (might be due to invalid IL or missing references)
		//IL_039e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0376: Unknown result type (might be due to invalid IL or missing references)
		if (equipment == null)
		{
			Image(r, mode + "/layer:waitEquip");
			DrawGrowthGrid(r, flameTab: false);
			return;
		}
		Icon(r, equipment, 166f, 112f, 64f);
		Text(r, BotItemCatalog.Shared.Name(equipment.itemId), 9f, 184f, 400f, mode + "/item/font:itemName", (TextAnchor)1);
		Image(r, mode + "/layer:summaryBox");
		if (equipment.soulSocketId <= 0)
		{
			Text(r, "尚未开孔", 66f, 250f, 286f, mode + "/font:summaryMagnificentSoul", (TextAnchor)1, 22f);
		}
		else if (equipment.soulItemId <= 0)
		{
			Text(r, "插槽已开启，尚未镶魂", 66f, 250f, 286f, mode + "/font:summaryMagnificentSoul", (TextAnchor)1, 22f);
		}
		else
		{
			Text(r, BotItemCatalog.Shared.Name(equipment.soulItemId), 66f, 250f, 286f, mode + "/font:summaryMagnificentSoul", (TextAnchor)1, 22f);
		}
		if (material != null && SoulOp(material.itemId) == SoulOperation.Socket)
		{
			Image(r, mode + "/detailEnchanter/layer:detailEnchanterBox");
			Text(r, "灵魂开孔", 20f, 290f, 200f, mode + "/detailEnchanter/font:probName", (TextAnchor)0);
			Text(r, "成功率", 176f, 340f, 140f, mode + "/detailEnchanter/font:probName", (TextAnchor)0, 18f);
			Text(r, SoulSuccessText(), 210f, 340f, 180f, mode + "/detailEnchanter/font:probDetail", (TextAnchor)0, 18f);
			Text(r, (equipment.soulSocketId > 0) ? "这件武器已经有插槽" : ("武器等级需达到 " + 75 + " 级"), 20f, 360f, 370f, mode + "/detailEnchanter/font:probDetail", (TextAnchor)0);
		}
		else
		{
			Image(r, mode + "/detailSoul/layer:detailSoulBox");
			Text(r, "灵魂属性", 20f, 290f, 200f, mode + "/detailSoul/font:statNormal", (TextAnchor)0);
			SoulOption soulOption = SoulCatalog.Of(equipment.soulOption);
			Text(r, (soulOption == null) ? "—" : (soulOption.key + " +" + soulOption.value), 20f, 360f, 120f, mode + "/detailSoul/font:statNormal", (TextAnchor)0);
			Text(r, "→", 150f, 360f, 30f, mode + "/detailSoul/font:statNormal", (TextAnchor)1);
			string text = soul?.optionKey;
			string text2 = ((text != null && !string.IsNullOrEmpty(text)) ? (text + " +" + soul.optionValue) : "—");
			Text(r, text2, 200f, 360f, 190f, mode + "/detailSoul/font:statHighlight", (TextAnchor)0);
		}
		DrawGrowthGrid(r, flameTab: false);
	}

	private static string SoulSuccessText()
	{
		if (material == null)
		{
			return "—";
		}
		Dictionary<string, object> dictionary = BotItemCatalog.Shared.Info(material.itemId);
		int num = ((dictionary == null) ? 100 : BotItemCatalog.Int(dictionary, "success"));
		return ((num <= 0) ? 100 : num) + "%";
	}

	private static void DrawGrowthGrid(Rect r, bool flameTab)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0366: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0284: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0258: Unknown result type (might be due to invalid IL or missing references)
		//IL_02eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0303: Unknown result type (might be due to invalid IL or missing references)
		string text = Pages[page];
		if (flameTab)
		{
			Image(r, text + "/costItem/layer:costItemBox", 10f, 490f);
		}
		else
		{
			Image(r, text + "/layer:costInvenBox");
		}
		List<InventoryEntry> list = new List<InventoryEntry>();
		foreach (InventoryEntry item in GameSession.Character.inventory)
		{
			if (item.position <= 0 || item.itemRowId == equipment?.itemRowId)
			{
				continue;
			}
			int itemId = item.itemId;
			bool num;
			if (!flameTab)
			{
				if (itemId / 1000 == 2590)
				{
					goto IL_00f4;
				}
				num = itemId / 1000 == 2591;
			}
			else
			{
				num = FlameCatalog.CreateTypeOf(itemId) >= 0;
			}
			if (!num)
			{
				continue;
			}
			goto IL_00f4;
			IL_00f4:
			list.Add(item);
		}
		list.Sort((InventoryEntry a, InventoryEntry b) => a.position.CompareTo(b.position));
		float num2 = 20f;
		float num3 = (flameTab ? 517 : 444);
		int num4 = Math.Max(0, (list.Count + 8) / 9 - 2);
		gridRow = Mathf.Clamp(gridRow, 0, num4);
		Rect val;
		if (GUI.enabled && Repaint)
		{
			val = At(r, num2, num3, 369f, 83f);
			if (val.Contains(Pointer.Pos) && Mathf.Abs(Input.mouseScrollDelta.y) > 0.01f)
			{
				gridRow = Mathf.Clamp(gridRow - (int)Mathf.Sign(Input.mouseScrollDelta.y), 0, num4);
			}
		}
		for (int num5 = 0; num5 < 18 && num5 + gridRow * 9 < list.Count; num5++)
		{
			InventoryEntry inventoryEntry = list[num5 + gridRow * 9];
			float num6 = num2 + (float)(num5 % 9 * 41);
			float num7 = num3 + (float)(num5 / 9 * 41);
			if (material?.itemRowId == inventoryEntry.itemRowId)
			{
				Image(r, text + (flameTab ? "/costItem/inven/selected" : "/costInven/selected"), num6 - 1f, num7 - 1f);
			}
			Icon(r, inventoryEntry, num6 + 4f, num7 + 2f);
			Text(r, inventoryEntry.quantity.ToString(), num6 + 2f, num7 + 24f, 36f, text + (flameTab ? "/costItem/font:costItem" : "/font:costName"), (TextAnchor)2, 16f);
			if (!Repaint)
			{
				continue;
			}
			val = At(r, num6, num7, 38f, 38f);
			if (val.Contains(Pointer.Pos))
			{
				ItemTooltip.SetHover(inventoryEntry.itemId, equipped: false, inventoryEntry);
				if (GUI.enabled && Pointer.Clicked)
				{
					Pointer.ConsumeClick();
					material = inventoryEntry;
					Preview();
				}
			}
		}
		if (material != null)
		{
			Text(r, BotItemCatalog.Shared.Name(material.itemId), 20f, flameTab ? 606 : 530, 388f, text + (flameTab ? "/costItem/font:costItem" : "/font:costName"), (TextAnchor)1, 18f);
		}
		else
		{
			Text(r, (list.Count == 0) ? "背包中没有对应的强化道具" : "点击材料格选择道具（每次消耗1个）", 20f, flameTab ? 606 : 530, 388f, text + (flameTab ? "/costItem/font:costItem" : "/font:costName"), (TextAnchor)1, 32f);
		}
	}
}

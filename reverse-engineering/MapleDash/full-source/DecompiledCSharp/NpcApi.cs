using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using MapleDash.Js;
using MapleDash.Wz;
using Server.Bot;
using Server.Bot.Types.Blackjack;
using Server.Field;
using UnityEngine;

public static class NpcApi
{
	private struct Totals
	{
		public int str;

		public int dex;

		public int intelligence;

		public int luk;

		public int watk;

		public int matk;

		public int weaponDefense;

		public int magicDefense;
	}

	private sealed class ScriptSound
	{
		public string path;

		public bool music;
	}

	private sealed class ScriptNotice
	{
		public int type;

		public string text;
	}

	private sealed class ScriptEffect
	{
		public string path;
	}

	private sealed class ScriptDamage
	{
		public long damage;

		public int mobId;

		public int objectId;
	}

	private sealed class ScriptMovePlayer
	{
	}

	private sealed class ScriptInventoryUpdate
	{
		public int mode;

		public InventoryEntry item;

		public short oldPosition;
	}

	private sealed class ScriptInventoryPacket
	{
		public readonly List<ScriptInventoryUpdate> updates = new List<ScriptInventoryUpdate>();
	}

	private sealed class ScriptRpsOpen
	{
		public int npcId;
	}

	private sealed class ScriptClock
	{
		public int seconds;
	}

	private sealed class ScriptProgressMessage
	{
		public string text;
	}

	private static int headlessHp = -1;

	private static int headlessMp = -1;

	private static FullCharacter headlessOwner;

	internal static Vector2 HeadlessPosition;

	private static bool useCS;

	private static readonly HashSet<string> missingApi = new HashSet<string>();

	private static readonly string[] SavedLocationTypes = new string[12]
	{
		"FREE_MARKET", "WORLDTOUR", "FLORINA", "INTRO", "SUNDAY_MARKET", "MIRROR", "EVENT", "BOSSPQ", "HAPPYVILLE", "MONSTER_CARNIVAL",
		"DEVELOPER", "JAIL"
	};

	private static readonly HashSet<string> headlessPqItems = new HashSet<string>();

	private static int dojoEnergy;

	private static Totals totalsCache;

	private static HashSet<int> newHairBases;

	private static readonly Dictionary<long, InventoryEntry> dirtyEquip = new Dictionary<long, InventoryEntry>();

	private static JsValue U => JsUndefined.Value;

	private static PlayerCtrl Pc
	{
		get
		{
			if (!((Object)(object)GameLoop.I != (Object)null))
			{
				return null;
			}
			return GameLoop.I.player;
		}
	}

	private static int MapId => Profile.LastMapId;

	private static int MaxHpFallback => Math.Max(1, Profile.Data.baseMaxHp);

	private static int MaxMpFallback => Math.Max(1, Profile.Data.baseMaxMp);

	private static int CurrentHp
	{
		get
		{
			if ((Object)(object)Pc != (Object)null)
			{
				return Mathf.RoundToInt(Pc.hp);
			}
			SyncHeadless();
			return headlessHp;
		}
		set
		{
			if ((Object)(object)Pc != (Object)null)
			{
				Pc.hp = value;
				return;
			}
			SyncHeadless();
			headlessHp = ClampHpMp(value, MaxHpFallback);
		}
	}

	private static int CurrentMp
	{
		get
		{
			if ((Object)(object)Pc != (Object)null)
			{
				return Mathf.RoundToInt(Pc.mp);
			}
			SyncHeadless();
			return headlessMp;
		}
		set
		{
			if ((Object)(object)Pc != (Object)null)
			{
				Pc.mp = value;
				return;
			}
			SyncHeadless();
			headlessMp = ClampHpMp(value, MaxMpFallback);
		}
	}

	internal static int ScriptHp
	{
		get
		{
			return CurrentHp;
		}
		set
		{
			CurrentHp = value;
		}
	}

	internal static int ScriptMaxHp
	{
		get
		{
			if (!((Object)(object)Pc != (Object)null))
			{
				return MaxHpFallback;
			}
			return Mathf.RoundToInt(Pc.hpMax);
		}
	}

	internal static bool ScriptAlive
	{
		get
		{
			if (!((Object)(object)Pc != (Object)null))
			{
				return CurrentHp > 0;
			}
			return Pc.hp > 0f;
		}
	}

	internal static int ScriptMapId => MapId;

	internal static Vector2 ScriptPosition
	{
		get
		{
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			return new Vector2(((Object)(object)Pc != (Object)null) ? (Pc.worldX * 40f) : HeadlessPosition.x, ((Object)(object)Pc != (Object)null) ? ((0f - Pc.FeetPos.y) * 40f) : HeadlessPosition.y);
		}
	}

	private static JsNumber N(double v)
	{
		return new JsNumber(v);
	}

	private static JsString S(string v)
	{
		return new JsString(v ?? "");
	}

	private static JsBoolean B(bool v)
	{
		return JsBoolean.Of(v);
	}

	private static JsFunction Fn(Action<JsCall> body)
	{
		return JsFunction.FromNative("", (JsCall c) =>
		{
			body(c);
			return JsUndefined.Value;
		});
	}

	private static JsFunction Ret(Func<JsCall, JsValue> body)
	{
		return JsFunction.FromNative("", body);
	}

	private static int Int(JsCall c, int i, int def = 0)
	{
		if (i >= c.ArgCount)
		{
			return def;
		}
		return JsConvert.ToInt32(c.Arg(i));
	}

	private static string Str(JsCall c, int i, string def = "")
	{
		if (i >= c.ArgCount)
		{
			return def;
		}
		return JsConvert.ToString(c.Arg(i));
	}

	private static void SetNativeAppearance(string part, int id)
	{
		CharRecord charRecord = GameSession.Character?.record;
		if (charRecord != null)
		{
			if (part == "hair")
			{
				charRecord.hair = id;
			}
			else if (part == "face")
			{
				charRecord.face = id;
			}
			else
			{
				charRecord.skincolor = id;
			}
			Pc?.RefreshFieldAppearance();
			GameSession.SaveToDb();
		}
	}

	private static bool CanHold(int[] items, int[] quantities, int removeItem = 0, int removeQuantity = 0)
	{
		if (GameSession.InGame)
		{
			return GameSession.Net.ChangeInventory(new InventoryChangeRequest
			{
				characterId = GameSession.CharacterId,
				checkItems = items,
				checkQuantities = quantities,
				removeCheckItem = removeItem,
				removeCheckQuantity = removeQuantity
			}).ok;
		}
		return CanHoldLocal(items, quantities, removeItem);
	}

	private static bool CanHoldLocal(int[] items, int[] quantities, int removeItem)
	{
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		for (int i = 0; i < 5; i++)
		{
			dictionary[i] = Profile.InventoryItems(i).Count;
		}
		if (removeItem > 0)
		{
			int num = removeItem / 1000000 - 1;
			if (num >= 0 && num < 5)
			{
				dictionary[num] = Math.Max(0, dictionary[num] - 1);
			}
		}
		for (int j = 0; j < items.Length; j++)
		{
			int num2 = items[j];
			int num3 = ((quantities == null || j >= quantities.Length) ? 1 : quantities[j]);
			if (num2 <= 0 || num3 == 0)
			{
				continue;
			}
			int num4 = num2 / 1000000 - 1;
			if (num4 >= 0 && num4 <= 4 && (num4 == 0 || Profile.ItemCount(num2) <= 0))
			{
				if (dictionary[num4] >= Profile.InventoryCapacity(num4))
				{
					return false;
				}
				dictionary[num4]++;
			}
		}
		return true;
	}

	private static int ClampHpMp(int value, int max)
	{
		if (value >= 0)
		{
			if (value <= max)
			{
				return value;
			}
			return max;
		}
		return 0;
	}

	private static void SyncHeadless()
	{
		FullCharacter character = GameSession.Character;
		if (!((Object)(object)Pc != (Object)null) && headlessOwner != character)
		{
			headlessOwner = character;
			headlessHp = ClampHpMp(character?.record.hp ?? MaxHpFallback, MaxHpFallback);
			headlessMp = ClampHpMp(character?.record.mp ?? MaxMpFallback, MaxMpFallback);
		}
	}

	private static void Lenient(JsHostObject host, string owner)
	{
		host.Missing = (string name) =>
		{
			if (missingApi.Add(owner + "." + name))
			{
				Debug.LogError((object)("[Script] 未迁移服务端接口：" + owner + "." + name));
			}
			return JsFunction.FromNative(name, (JsCall c) =>
			{
				throw new JsRuntimeException("Unmigrated server API: " + owner + "." + name);
			});
		};
	}

	public static JsHostObject Build(NpcSession session, int npcId, int questId, bool isQuest)
	{
		JsHostObject jsHostObject = new JsHostObject(session);
		Lenient(jsHostObject, "cm");
		JsHostObject player = BuildPlayer(session);
		JsHostObject map = BuildMap();
		jsHostObject.Set("startMonsterVac", Ret((JsCall _) => B((Object)(object)Pc != (Object)null && NativeMonsterVac.For(Pc).StartVac())));
		jsHostObject.Set("isMonsterVacActive", Ret((JsCall _) =>
		{
			PlayerCtrl pc = Pc;
			return B(pc != null && ((Component)pc).GetComponent<NativeMonsterVac>()?.Active == true);
		}));
		jsHostObject.Set("stopMonsterVac", Fn((JsCall _) =>
		{
			PlayerCtrl pc = Pc;
			if (pc != null)
			{
				((Component)pc).GetComponent<NativeMonsterVac>()?.StopVac();
			}
		}));
		jsHostObject.Set("getPlayer", Ret((JsCall _) => player));
		jsHostObject.Set("getChar", Ret((JsCall _) => player));
		jsHostObject.Set("getNpc", Ret((JsCall _) => N(npcId)));
		jsHostObject.Set("getItemId", Ret((JsCall _) => N(session.ItemId)));
		jsHostObject.Set("isItemScript", Ret((JsCall _) => B(session.ItemId > 0)));
		jsHostObject.Set("useDamageCapBreakthroughStone", Ret((JsCall _) =>
		{
			InventoryChangeResponse inventoryChangeResponse = GameSession.Net.ChangeInventory(new InventoryChangeRequest
			{
				characterId = GameSession.CharacterId,
				itemId = session.ItemId,
				breakthrough = true
			});
			if (!inventoryChangeResponse.ok)
			{
				return S(inventoryChangeResponse.error);
			}
			GameSession.Character.record.damageCap = inventoryChangeResponse.damageCap;
			GameSession.Character.inventory = inventoryChangeResponse.inventory;
			GameSession.ApplyInventoryToProfile();
			Profile.Save();
			return S(inventoryChangeResponse.message);
		}));
		jsHostObject.Set("getNpcObjectId", Ret((JsCall _) => N(npcId)));
		jsHostObject.Set("getMap", Ret((JsCall _) => map));
		jsHostObject.Set("getMapId", Ret((JsCall _) => N(MapId)));
		jsHostObject.Set("getClient", Ret((JsCall _) => BuildClient(player)));
		jsHostObject.Set("spawnMonster", Fn((JsCall c) =>
		{
			SpawnScriptMob(Int(c, 0), Int(c, 1), Int(c, 2));
		}));
		jsHostObject.Set("showSkillBackdrop", Fn((JsCall c) =>
		{
			NativeSkillBackdrop.ShowMap(Str(c, 0), Int(c, 1));
		}));
		jsHostObject.Set("clearSkillBackdrop", Fn((JsCall c) =>
		{
			NativeSkillBackdrop.ShowMap(null, 0);
		}));
		jsHostObject.Set("showLocalSkillBackdrop", Fn((JsCall c) =>
		{
			NativeSkillBackdrop.ShowLocal(Str(c, 0), Int(c, 1));
		}));
		jsHostObject.Set("clearLocalSkillBackdrop", Fn((JsCall c) =>
		{
			NativeSkillBackdrop.ShowLocal(null, 0);
		}));
		jsHostObject.Set("getDamageSkinIds", Ret((JsCall _) =>
		{
			List<JsValue> list = new List<JsValue>();
			int[] array = NativeDamageSkinCatalog.Ids();
			foreach (int num in array)
			{
				list.Add(N(num));
			}
			return new JsArray(list);
		}));
		jsHostObject.Set("getDamageSkinName", Ret((JsCall c) => S(NativeDamageSkinCatalog.Name(Int(c, 0)))));
		jsHostObject.Set("getDamageSkinId", Ret((JsCall _) => N(NativeDamageSkinCatalog.Selected)));
		jsHostObject.Set("setDamageSkin", Ret((JsCall c) => B(NativeDamageSkinCatalog.Select(Int(c, 0)))));
		jsHostObject.Set("sendNext", Fn((JsCall c) =>
		{
			session.SetDialog(NpcDialogMode.Next, Str(c, 0));
		}));
		jsHostObject.Set("sendPrev", Fn((JsCall c) =>
		{
			session.SetDialog(NpcDialogMode.Prev, Str(c, 0));
		}));
		jsHostObject.Set("sendNextPrev", Fn((JsCall c) =>
		{
			session.SetDialog(NpcDialogMode.NextPrev, Str(c, 0));
		}));
		jsHostObject.Set("sendOk", Fn((JsCall c) =>
		{
			session.SetDialog(NpcDialogMode.Ok, Str(c, 0));
		}));
		jsHostObject.Set("sendYesNo", Fn((JsCall c) =>
		{
			session.SetDialog(NpcDialogMode.YesNo, Str(c, 0));
		}));
		jsHostObject.Set("sendAcceptDecline", Fn((JsCall c) =>
		{
			session.SetDialog(NpcDialogMode.AcceptDecline, Str(c, 0));
		}));
		jsHostObject.Set("sendSimple", Fn((JsCall c) =>
		{
			session.SetDialog(NpcDialogMode.Simple, Str(c, 0));
		}));
		jsHostObject.Set("sendGetText", Fn((JsCall c) =>
		{
			session.SetDialog(NpcDialogMode.GetText, Str(c, 0));
			session.Dialog.Input = "";
		}));
		jsHostObject.Set("sendGetNumber", Fn((JsCall c) =>
		{
			session.SetDialog(NpcDialogMode.GetNumber, Str(c, 0));
			session.Dialog.NumberDef = Int(c, 1);
			session.Dialog.NumberMin = Int(c, 2);
			session.Dialog.NumberMax = Int(c, 3, 999999999);
			session.Dialog.Input = session.Dialog.NumberDef.ToString();
		}));
		jsHostObject.Set("sendStyle", Fn((JsCall c) =>
		{
			JsArray jsArray = c.Arg(1) as JsArray;
			List<int> list = new List<int>();
			if (jsArray != null)
			{
				foreach (JsValue item in jsArray.Items)
				{
					list.Add(JsConvert.ToInt32(item));
				}
			}
			session.SetDialog(NpcDialogMode.Style, Str(c, 0));
			session.Dialog.Styles = list.ToArray();
		}));
		jsHostObject.Set("sendDefault", Fn((JsCall c) =>
		{
			session.SetDialog(NpcDialogMode.Ok, WzAssets.FileRoot("String/Npc.img").Get(npcId.ToString())?.Text("d0", "(...)") ?? "(...)");
		}));
		jsHostObject.Set("sendImage", Fn((JsCall c) =>
		{
			session.SetDialog(NpcDialogMode.Ok, Str(c, 0));
		}));
		jsHostObject.Set("sendNoExit", Fn((JsCall c) =>
		{
			session.SetDialog(NpcDialogMode.Ok, Str(c, 0));
		}));
		jsHostObject.Set("sendDirectionInfo", Fn((JsCall c) =>
		{
		}));
		jsHostObject.Set("sendDimensionalMirror", Fn((JsCall c) =>
		{
		}));
		jsHostObject.Set("dispose", Fn((JsCall c) =>
		{
			FlushEquip();
			session.DisposeFromScript();
		}));
		jsHostObject.Set("enableActions", Fn((JsCall c) =>
		{
		}));
		jsHostObject.Set("sendNextLevel", Fn((JsCall c) =>
		{
			session.SetDialog(NpcDialogMode.Next, Str(c, 1));
			session.Route.Set(NativeScriptRouteType.Next, null, Str(c, 0));
		}));
		jsHostObject.Set("sendLastLevel", Fn((JsCall c) =>
		{
			session.SetDialog(NpcDialogMode.Prev, Str(c, 1));
			session.Route.Set(NativeScriptRouteType.Previous, Str(c, 0));
		}));
		jsHostObject.Set("sendLastNextLevel", Fn((JsCall c) =>
		{
			session.SetDialog(NpcDialogMode.NextPrev, Str(c, 2));
			session.Route.Set(NativeScriptRouteType.PreviousNext, Str(c, 0), Str(c, 1));
		}));
		jsHostObject.Set("sendOkLevel", Fn((JsCall c) =>
		{
			session.SetDialog(NpcDialogMode.Ok, Str(c, 1));
			session.Route.Set(NativeScriptRouteType.Ok, null, Str(c, 0));
		}));
		jsHostObject.Set("sendSelectLevel", Fn((JsCall c) =>
		{
			bool flag = c.ArgCount > 1 && c.Arg(1) is JsString;
			session.SetDialog(NpcDialogMode.Simple, Str(c, flag ? 1 : 0));
			session.Route.Set(NativeScriptRouteType.Select, null, null, flag ? Str(c, 0) : "");
		}));
		jsHostObject.Set("sendNextSelectLevel", Fn((JsCall c) =>
		{
			session.SetDialog(NpcDialogMode.Simple, Str(c, 1));
			session.Route.Set(NativeScriptRouteType.NextSelect, null, Str(c, 0));
		}));
		jsHostObject.Set("getInputTextLevel", Fn((JsCall c) =>
		{
			session.SetDialog(NpcDialogMode.GetText, Str(c, 1));
			session.Dialog.Input = "";
			session.Route.Set(NativeScriptRouteType.Text, null, Str(c, 0));
		}));
		jsHostObject.Set("getInputNumberLevel", Fn((JsCall c) =>
		{
			session.SetDialog(NpcDialogMode.GetNumber, Str(c, 1));
			session.Dialog.NumberDef = Int(c, 2);
			session.Dialog.NumberMin = Int(c, 3);
			session.Dialog.NumberMax = Int(c, 4);
			session.Dialog.Input = session.Dialog.NumberDef.ToString();
			session.Route.Set(NativeScriptRouteType.Number, null, Str(c, 0));
		}));
		jsHostObject.Set("sendAcceptDeclineLevel", Fn((JsCall c) =>
		{
			session.SetDialog(NpcDialogMode.AcceptDecline, Str(c, 2));
			session.Route.Set(NativeScriptRouteType.AcceptDecline, Str(c, 0), Str(c, 1));
		}));
		jsHostObject.Set("sendYesNoLevel", Fn((JsCall c) =>
		{
			session.SetDialog(NpcDialogMode.YesNo, Str(c, 2));
			session.Route.Set(NativeScriptRouteType.YesNo, Str(c, 0), Str(c, 1));
		}));
		jsHostObject.Set("getText", Ret((JsCall _) => S(session.Dialog.Input)));
		jsHostObject.Set("setGetText", Fn((JsCall c) =>
		{
			session.Dialog.Input = Str(c, 0);
		}));
		jsHostObject.Set("getMeso", Ret((JsCall _) => N(Profile.gold)));
		jsHostObject.Set("getNameplateSeries", Ret((JsCall _) => N(Math.Max(0, Math.Min(8, ReadCharacterInteger("nameplate_title_series"))))));
		jsHostObject.Set("hasNameplateSeries", Ret((JsCall _) => B(ReadCharacterRaw("nameplate_title_series") != null)));
		jsHostObject.Set("setNameplateSeries", Fn((JsCall c) =>
		{
			int series = Math.Max(0, Math.Min(8, Int(c, 0)));
			WriteCharacterValue("nameplate_title_series", series.ToString());
			NativeNameplate.SetSeries(series);
		}));
		jsHostObject.Set("getTitleTrial", Ret((JsCall c) =>
		{
			int num = Math.Max(0, Math.Min(8, Int(c, 0)));
			int num2 = NativeTitleTrials.Lit(num);
			int num3 = Math.Min(10, num2 + 1);
			long num4 = NativeTitleTrials.Count(num);
			long num5 = NativeTitleTrials.Need(num, num3);
			bool v = num2 < 10 && CanClaimTitleTier(num);
			JsHostObject jsHostObject2 = new JsHostObject(null);
			Lenient(jsHostObject2, "titleTrial");
			jsHostObject2.Set("series", N(num));
			jsHostObject2.Set("lit", N(num2));
			jsHostObject2.Set("next", N(num3));
			jsHostObject2.Set("cnt", N(num4));
			jsHostObject2.Set("need", N(num5));
			jsHostObject2.Set("canClaim", B(v));
			return jsHostObject2;
		}));
		jsHostObject.Set("getTitleTrialNeed", Ret((JsCall c) => N(NativeTitleTrials.Need(Math.Max(0, Math.Min(8, Int(c, 0))), Math.Max(1, Math.Min(10, Int(c, 1)))))));
		jsHostObject.Set("claimTitleTier", Ret((JsCall c) =>
		{
			string text = NativeTitleTrials.Claim(Math.Max(0, Math.Min(8, Int(c, 0))), out var granted);
			string v = text ?? ("点亮成功！" + granted);
			JsHostObject jsHostObject2 = new JsHostObject(null);
			Lenient(jsHostObject2, "titleTrialClaim");
			jsHostObject2.Set("ok", B(text == null));
			jsHostObject2.Set("message", S(v));
			return jsHostObject2;
		}));
		jsHostObject.Set("getPqPoints", Ret((JsCall _) => N(ReadCharacterInteger("副本积分"))));
		jsHostObject.Set("getPqTotalPoints", Ret((JsCall _) => N(ReadCharacterInteger("副本总积分"))));
		jsHostObject.Set("gainMeso", Fn((JsCall c) =>
		{
			Profile.AddGold(Int(c, 0));
		}));
		jsHostObject.Set("gainExp", Fn((JsCall c) =>
		{
			Profile.GrantExperience(Int(c, 0));
		}));
		jsHostObject.Set("getLevel", Ret((JsCall _) => N(Profile.level)));
		jsHostObject.Set("getJobId", Ret((JsCall _) => N(QuestLogic.PlayerJob)));
		jsHostObject.Set("getJob", Ret((JsCall _) => BuildJob(QuestLogic.PlayerJob)));
		jsHostObject.Set("getGender", Ret((JsCall _) => N(GameSession.Character?.record.gender ?? 0)));
		jsHostObject.Set("getName", Ret((JsCall _) => S(Profile.playerName)));
		jsHostObject.Set("getFame", Ret((JsCall _) => N(GameSession.Character?.record.fame ?? 0)));
		jsHostObject.Set("gainFame", Fn((JsCall c) =>
		{
			ChangeFame(ScriptOperation.GainFame, Int(c, 0));
		}));
		jsHostObject.Set("haveItem", Ret((JsCall c) => B(HasItem(Int(c, 0), Int(c, 1, 1)))));
		jsHostObject.Set("hasItem", Ret((JsCall c) => B(HasItem(Int(c, 0), Int(c, 1, 1)))));
		jsHostObject.Set("haveItemWithId", Ret((JsCall c) => B(HasItemWithId(Int(c, 0), c.ArgCount > 1 && JsConvert.ToBoolean(c.Arg(1))))));
		jsHostObject.Set("itemQuantity", Ret((JsCall c) => N(ItemQuantity(Int(c, 0)))));
		jsHostObject.Set("getItemQuantity", Ret((JsCall c) => N(ItemQuantity(Int(c, 0)))));
		jsHostObject.Set("gainItem", Ret((JsCall c) =>
		{
			int num = Int(c, 0);
			int num2 = Int(c, 1, 1);
			if (num <= 0 || num2 == 0)
			{
				return JsNull.Value;
			}
			if (num2 < 0)
			{
				Profile.RemoveItem(num, -num2);
				return JsNull.Value;
			}
			if (!CanHold(new int[1] { num }, new int[1] { num2 }))
			{
				string[] array = new string[6] { "", "EQUIP", "USE", "SETUP", "ETC", "CASH" };
				int num3 = num / 1000000;
				NativeSystemMessage.Drop(1, "您的背包已满，请从" + ((num3 >= 1 && num3 <= 5) ? array[num3] : "") + "栏移除一件物品。");
				return JsNull.Value;
			}
			int num4 = Profile.ItemCount(num);
			Profile.AddItem(num, num2);
			return (Profile.ItemCount(num) - num4 != num2) ? JsNull.Value : NewItemRef(num, num2);
		}));
		jsHostObject.Set("canHold", Ret((JsCall c) => B(CanHold(new int[1] { Int(c, 0) }, new int[1] { Int(c, 1, 1) }, Int(c, 2), Int(c, 3)))));
		jsHostObject.Set("canHoldAll", Ret((JsCall c) =>
		{
			JsArray jsArray = c.Arg(0) as JsArray;
			JsArray jsArray2 = c.Arg(1) as JsArray;
			if (jsArray == null)
			{
				return B(v: false);
			}
			List<int> list = new List<int>();
			List<int> list2 = new List<int>();
			for (int i = 0; i < jsArray.Items.Count; i++)
			{
				list.Add(JsConvert.ToInt32(jsArray.Items[i]));
				list2.Add((jsArray2 == null || i >= jsArray2.Items.Count) ? 1 : JsConvert.ToInt32(jsArray2.Items[i]));
			}
			return B(CanHold(list.ToArray(), list2.ToArray()));
		}));
		jsHostObject.Set("canGenerateItem", Ret((JsCall c) => B(WzNames.HasItem(Int(c, 0)))));
		jsHostObject.Set("isNotCanHold", Ret((JsCall c) =>
		{
			int num = Int(c, 0);
			int num2 = Int(c, 1, 2);
			if (num < 1 || num > 5)
			{
				return B(v: true);
			}
			HashSet<int> hashSet = new HashSet<int>();
			if (GameSession.Character?.inventory != null)
			{
				foreach (InventoryEntry item2 in GameSession.Character.inventory)
				{
					if (item2.inventoryType == num && item2.position > 0)
					{
						hashSet.Add(item2.position);
					}
				}
			}
			bool flag = Profile.InventoryCapacity(num - 1) - hashSet.Count < num2;
			if (flag)
			{
				string[] array = new string[5] { "装备", "消耗", "设置", "其他", "特殊" };
				session.SetDialog(NpcDialogMode.Ok, "#r背包空间不足，请确保#b" + array[num - 1] + "栏#r至少有#b" + num2 + "#r个空格！");
				session.DisposeFromScript();
			}
			return B(flag);
		}));
		jsHostObject.Set("getCosmeticItem", Ret((JsCall c) =>
		{
			int num = Int(c, 0);
			if (WzNames.HasItem(num))
			{
				return N(num);
			}
			int num2 = ((num < 30000) ? (num / 1000 * 1000 + num % 100) : (num / 10 * 10));
			return N((num != num2 && WzNames.HasItem(num2)) ? num2 : (-1));
		}));
		jsHostObject.Set("isCosmeticEquipped", Ret((JsCall c) =>
		{
			int num = Int(c, 0);
			CharRecord charRecord = GameSession.Character?.record;
			return B(charRecord != null && ((num < 30000) ? charRecord.face : charRecord.hair) == num);
		}));
		jsHostObject.Set("itemExists", Ret((JsCall c) => B(WzNames.HasItem(Int(c, 0)))));
		jsHostObject.Set("hasItemData", Ret((JsCall c) => B(!string.IsNullOrEmpty(WzNames.Item(Int(c, 0))))));
		jsHostObject.Set("getInventory", Ret((JsCall c) => BuildInventory(Int(c, 0))));
		jsHostObject.Set("removeAll", Fn((JsCall c) =>
		{
			int num = Int(c, 0);
			if (!GameSession.InGame)
			{
				int num2 = Profile.ItemCount(num);
				if (num2 > 0)
				{
					Profile.RemoveItem(num, num2);
				}
			}
			else
			{
				InventoryChangeResponse inventoryChangeResponse = GameSession.Net.ChangeInventory(new InventoryChangeRequest
				{
					characterId = GameSession.CharacterId,
					itemId = num,
					removeAll = true
				});
				if (!inventoryChangeResponse.ok)
				{
					throw new JsRuntimeException(inventoryChangeResponse.error);
				}
				GameSession.Character.inventory = inventoryChangeResponse.inventory;
				GameSession.ApplyInventoryToProfile();
				Profile.Save();
			}
		}));
		jsHostObject.Set("removeAllByInventory", Fn((JsCall c) =>
		{
			RemoveInventory(Int(c, 0));
		}));
		jsHostObject.Set("removeAllByInventorySlot", Fn((JsCall c) =>
		{
			RemoveInventorySlot(Int(c, 0), Int(c, 1));
		}));
		jsHostObject.Set("warp", Fn((JsCall c) =>
		{
			int num = Int(c, 0);
			if (num < 0)
			{
				throw new JsRuntimeException("warp: 无效的地图 id " + num);
			}
			if (!MapExists(num))
			{
				string text = "[系统] 你无法去到不存在的\r\n地图(" + num + ")，请联系GM处理。";
				NativeSystemMessage.Drop(5, text);
				NativeSystemMessage.Drop(1, text);
			}
			else
			{
				session.RequestWarp(num, WarpName(c, 1), WarpIndex(c, 1));
			}
		}));
		jsHostObject.Set("warpParty", Fn((JsCall c) =>
		{
			session.RequestWarp(Int(c, 0), WarpName(c, 1), WarpIndex(c, 1));
		}));
		jsHostObject.Set("getPortal", Ret((JsCall _) =>
		{
			FieldPortal portal = session?.Portal;
			if (portal != null)
			{
				JsHostObject jsHostObject2 = new JsHostObject(portal);
				jsHostObject2.Set("getPosition", Ret((JsCall c) => BuildPoint(portal.x, portal.y)));
				jsHostObject2.Set("getMapId", Ret((JsCall c) => N(MapId)));
				jsHostObject2.Set("getName", Ret((JsCall c) => S(portal.name)));
				jsHostObject2.Set("getScriptName", Ret((JsCall c) => S(portal.script)));
				jsHostObject2.Set("getType", Ret((JsCall c) => N(portal.type)));
				jsHostObject2.Set("getId", Ret((JsCall c) => N(portal.id)));
				return jsHostObject2;
			}
			return JsNull.Value;
		}));
		jsHostObject.Set("getWarpMap", Ret((JsCall c) => BuildWarpMap(Int(c, 0))));
		jsHostObject.Set("getMarketPortalId", Ret((JsCall c) => N(MarketPortalId(Int(c, 0)))));
		jsHostObject.Set("playPortalSound", Fn((JsCall c) =>
		{
		}));
		BindSavedLocations(jsHostObject);
		jsHostObject.Set("getCharacterExtendValue", Ret((JsCall c) => ExtendValue(c, account: false, write: false)));
		jsHostObject.Set("getAccountExtendValue", Ret((JsCall c) => ExtendValue(c, account: true, write: false)));
		jsHostObject.Set("saveOrUpdateCharacterExtendValue", Fn((JsCall c) =>
		{
			ExtendValue(c, account: false, write: true);
		}));
		jsHostObject.Set("saveOrUpdateAccountExtendValue", Fn((JsCall c) =>
		{
			ExtendValue(c, account: true, write: true);
		}));
		jsHostObject.Set("getCurrentTime", Ret((JsCall _) => N(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())));
		jsHostObject.Set("getOnlineTime", Ret((JsCall _) => N(OnlineTime.Seconds)));
		jsHostObject.Set("getScriptName", Ret((JsCall _) => S(session.ScriptName)));
		jsHostObject.Set("isQuestStarted", Ret((JsCall c) => B(Profile.IsQuestStarted(Int(c, 0)))));
		jsHostObject.Set("isQuestCompleted", Ret((JsCall c) => B(Profile.IsQuestCompleted(Int(c, 0)))));
		jsHostObject.Set("isQuestActive", Ret((JsCall c) => B(Profile.IsQuestStarted(Int(c, 0)))));
		jsHostObject.Set("getQuestRecord", Ret((JsCall c) =>
		{
			int id = Int(c, 0);
			JsHostObject jsHostObject2 = new JsHostObject(id);
			Lenient(jsHostObject2, "questRecord");
			jsHostObject2.Set("getCustomData", Ret((JsCall args) => Data(write: false, args)));
			jsHostObject2.Set("setCustomData", Fn((JsCall args) =>
			{
				Data(write: true, args);
			}));
			jsHostObject2.Set("getStatus", Ret((JsCall args) => N(Profile.QuestStatus(id))));
			return jsHostObject2;
			JsValue Data(bool write, JsCall args)
			{
				if (!GameSession.InGame)
				{
					throw new JsRuntimeException("Quest record requires an active character");
				}
				return GameSession.Net.Script(new ScriptRequest
				{
					characterId = GameSession.CharacterId,
					questId = id,
					operation = (write ? ScriptOperation.SetQuestCustomData : ScriptOperation.GetQuestCustomData),
					customData = ((args.Arg(0) is JsNull) ? null : Str(args, 0))
				}).value;
			}
		}));
		jsHostObject.Set("getQuestStatus", Ret((JsCall c) => N(Profile.QuestStatus(Int(c, 0)))));
		jsHostObject.Set("getQuestProgressInt", Ret((JsCall c) => N(Profile.QuestProgressInt(Int(c, 0), Key(c, 1), Int(c, 2)))));
		jsHostObject.Set("getQuestProgress", Ret((JsCall c) => S(Profile.QuestProgress(Int(c, 0), Key(c, 1), ""))));
		jsHostObject.Set("setQuestProgress", Fn((JsCall c) =>
		{
			Profile.SetQuestProgress(Int(c, 0), Key(c, 1), Str(c, 2));
		}));
		jsHostObject.Set("startQuest", Fn((JsCall c) =>
		{
			StartQuest(Int(c, 0, questId), npcId);
		}));
		jsHostObject.Set("completeQuest", Fn((JsCall c) =>
		{
			CompleteQuest(Int(c, 0, questId), npcId);
		}));
		jsHostObject.Set("forceStartQuest", Fn((JsCall c) =>
		{
			StartQuest(Int(c, 0, questId), npcId);
		}));
		jsHostObject.Set("forceCompleteQuest", Fn((JsCall c) =>
		{
			CompleteQuest(Int(c, 0, questId), npcId);
		}));
		jsHostObject.Set("teachSkill", Fn((JsCall c) =>
		{
			int num = Int(c, 0);
			int num2 = Int(c, 1, 1);
			int num3 = Int(c, 2);
			if ((c.ArgCount <= 4 || !JsConvert.ToBoolean(c.Arg(4))) && num2 >= 0)
			{
				num2 = Math.Max(num2, SkillProgress.LevelOf(num));
				num3 = Math.Max(num3, SkillBookItems.MasterLevelFor(num));
			}
			SetScriptSkill(num, num2, num3, (c.ArgCount > 3) ? ((long)JsConvert.ToNumber(c.Arg(3))) : (-1));
		}));
		jsHostObject.Set("changeJobById", Fn((JsCall c) =>
		{
			ChangeJob(Int(c, 0));
		}));
		jsHostObject.Set("getJobName", Ret((JsCall c) => S(SkillBook.JobName(Int(c, 0)))));
		jsHostObject.Set("setHair", Fn((JsCall c) =>
		{
			SetNativeAppearance("hair", Int(c, 0));
		}));
		jsHostObject.Set("setFace", Fn((JsCall c) =>
		{
			SetNativeAppearance("face", Int(c, 0));
		}));
		jsHostObject.Set("setSkin", Fn((JsCall c) =>
		{
			SetNativeAppearance("skin", Int(c, 0));
		}));
		jsHostObject.Set("resetStats", Fn((JsCall c) =>
		{
			JobSystem.ResetStats(Profile.JobId);
		}));
		jsHostObject.Set("dropMessage", Fn((JsCall c) =>
		{
			DropNotice(c);
		}));
		jsHostObject.Set("message", Fn((JsCall c) =>
		{
			NativeSystemMessage.Drop(5, Str(c, 0));
		}));
		jsHostObject.Set("playerMessage", Fn((JsCall c) =>
		{
			DropNotice(c);
		}));
		jsHostObject.Set("mapMessage", Fn((JsCall c) =>
		{
			DropNotice(c);
		}));
		jsHostObject.Set("topMessage", Fn((JsCall c) =>
		{
			ToastIfAny(Str(c, 0));
		}));
		jsHostObject.Set("numberWithCommas", Ret((JsCall c) => S(Int(c, 0).ToString("N0"))));
		jsHostObject.Set("showInfo", Fn((JsCall c) =>
		{
			NativeImgAnimation.ShowInfo(Str(c, 0));
		}));
		jsHostObject.Set("showIntro", Fn((JsCall c) =>
		{
			NativeMapIntro.Show(Str(c, 0));
		}));
		jsHostObject.Set("showVideo", Fn((JsCall c) =>
		{
		}));
		jsHostObject.Set("playSound", Fn((JsCall c) =>
		{
			AudioManager.Play(Str(c, 0), 1f, pitchVary: false);
		}));
		jsHostObject.Set("guideHint", Fn((JsCall c) =>
		{
		}));
		jsHostObject.Set("showQuestCompleteEffect", Fn((JsCall c) =>
		{
			QuestNotice.ShowCompleteEffect();
		}));
		jsHostObject.Set("showItemGain", Fn((JsCall c) =>
		{
		}));
		jsHostObject.Set("openNpc", Fn((JsCall c) =>
		{
			session.RequestOpenNpc(Int(c, 0), (c.ArgCount > 1) ? Str(c, 1, null) : null);
		}));
		jsHostObject.Set("openShopNPC", Fn((JsCall c) =>
		{
			NativeNpcShopWindow.OpenShop(Int(c, 0), fallback: true);
		}));
		jsHostObject.Set("getPlayerCount", Ret((JsCall _) => N(1.0)));
		jsHostObject.Set("isLeader", Ret((JsCall _) => B(IsPartyLeader())));
		jsHostObject.Set("isPartyLeader", Ret((JsCall _) => B(IsPartyLeader())));
		jsHostObject.Set("isEventLeader", Ret((JsCall _) => B(NativeEventScripts.IsEventLeader(GameSession.CharacterId))));
		jsHostObject.Set("isUsingOldPqNpcStyle", Ret((JsCall _) => B(v: false)));
		jsHostObject.Set("getHourOfDay", Ret((JsCall _) => N(DateTime.Now.Hour)));
		jsHostObject.Set("canSpawnPlayerNpc", Ret((JsCall _) => B(v: false)));
		jsHostObject.Set("canGetFirstJob", Ret((JsCall c) => B(JobSystem.CanGetFirstJob(Int(c, 0)))));
		jsHostObject.Set("getFirstJobStatRequirement", Ret((JsCall c) => S(JobSystem.FirstJobStatRequirement(Int(c, 0)))));
		BindCommonCm(jsHostObject, session);
		jsHostObject.Set("getEventManager", Ret((JsCall c) => NativeEventScripts.Manager(Str(c, 0))));
		jsHostObject.Set("getEventInstance", Ret((JsCall _) => NativeEventScripts.Instance()));
		jsHostObject.Set("getParty", Ret((JsCall _) => BuildParty(session)));
		jsHostObject.Set("getExpedition", Ret((JsCall c) => BuildExpedition(ExpeditionRegistry.GetExpedition(ExpeditionTypeArgument(c.Arg(0))), session)));
		jsHostObject.Set("getFamilyEntry", Ret((JsCall _) => JsNull.Value));
		jsHostObject.Set("getEvent", Ret((JsCall _) => JsNull.Value));
		jsHostObject.Set("getStorage", Ret((JsCall _) => JsNull.Value));
		jsHostObject.Set("getMTS", Ret((JsCall _) => JsNull.Value));
		jsHostObject.Set("getCashShop", Ret((JsCall _) => (!GameSession.InGame) ? ((JsValue)JsNull.Value) : ((JsValue)BuildCashShop())));
		jsHostObject.Set("c", BuildClient(player));
		if (isQuest)
		{
			jsHostObject.Set("getMedalName", Ret((JsCall _) => S(WzNames.Item(QuestCatalog.Get(questId)?.medalItem ?? 0))));
			jsHostObject.Set("earnTitle", Fn((JsCall c) =>
			{
				BottomBar.Toast(Str(c, 0));
			}));
			jsHostObject.Set("getQuest", Ret((JsCall _) => N(questId)));
			jsHostObject.Set("isStart", Ret((JsCall _) => B(!session.QuestEnd)));
		}
		return jsHostObject;
	}

	private static int ReadCharacterInteger(string key)
	{
		if (!GameSession.InGame)
		{
			throw new JsRuntimeException("Script values require an active character");
		}
		if (!int.TryParse(GameSession.Net.ScriptValue(new ScriptValueRequest
		{
			characterId = GameSession.CharacterId,
			name = key
		}).value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var result))
		{
			return 0;
		}
		return result;
	}

	private static string ReadCharacterRaw(string key)
	{
		if (!GameSession.InGame)
		{
			return null;
		}
		return GameSession.Net.ScriptValue(new ScriptValueRequest
		{
			characterId = GameSession.CharacterId,
			name = key
		}).value;
	}

	private static bool CanClaimTitleTier(int series)
	{
		PlayerCtrl playerCtrl = GameLoop.I?.player;
		if ((Object)(object)playerCtrl == (Object)null)
		{
			return false;
		}
		int num = NativeTitleTrials.Lit(series) + 1;
		if (num > 10)
		{
			return false;
		}
		if (NativeNameplate.PowerOf(playerCtrl) < NativeNameplate.TierRequirement(num))
		{
			return false;
		}
		return NativeTitleTrials.Count(series) >= NativeTitleTrials.Need(series, num);
	}

	private static void WriteCharacterValue(string key, string value)
	{
		if (!GameSession.InGame)
		{
			throw new JsRuntimeException("Script values require an active character");
		}
		GameSession.Net.ScriptValue(new ScriptValueRequest
		{
			characterId = GameSession.CharacterId,
			write = true,
			name = key,
			value = value
		});
	}

	private static JsValue ExtendValue(JsCall call, bool account, bool write)
	{
		if (!GameSession.InGame)
		{
			throw new JsRuntimeException("Script values require an active character");
		}
		int num = ((!write) ? 1 : 2);
		int period = ((call.ArgCount > num) ? (JsConvert.ToBoolean(call.Arg(num)) ? 1 : 2) : 0);
		ScriptValueResponse scriptValueResponse = GameSession.Net.ScriptValue(new ScriptValueRequest
		{
			characterId = GameSession.CharacterId,
			account = account,
			write = write,
			period = period,
			name = Str(call, 0),
			value = (write ? Str(call, 1) : null)
		});
		if (scriptValueResponse.value != null)
		{
			return S(scriptValueResponse.value);
		}
		return JsNull.Value;
	}

	private static void BindSavedLocations(JsHostObject host)
	{
		host.Set("peekSavedLocation", Ret((JsCall c) => Location(ScriptOperation.PeekLocation, Str(c, 0))));
		host.Set("getSavedLocation", Ret((JsCall c) => Location(ScriptOperation.GetLocation, Str(c, 0))));
		host.Set("clearSavedLocation", Fn((JsCall c) =>
		{
			Location(ScriptOperation.ClearLocation, Str(c, 0));
		}));
	}

	private static void SaveLocation(string type)
	{
		if (!string.IsNullOrEmpty(type))
		{
			if (!GameSession.InGame)
			{
				throw new JsRuntimeException("Saved locations require an active character");
			}
			GameSession.Net.Script(new ScriptRequest
			{
				operation = ScriptOperation.SaveLocation,
				characterId = GameSession.CharacterId,
				location = type,
				mapId = MapId,
				portalId = ClosestPortalId()
			});
		}
	}

	private static void SaveLocationOnWarp()
	{
		string[] savedLocationTypes = SavedLocationTypes;
		foreach (string type in savedLocationTypes)
		{
			if (JsConvert.ToInt32(Location(ScriptOperation.PeekLocation, type)) < 0)
			{
				SaveLocation(type);
			}
		}
	}

	private static int ClosestPortalId()
	{
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		FieldDirector fieldDirector = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.field : null);
		FieldMap fieldMap = (((Object)(object)fieldDirector != (Object)null) ? fieldDirector.Map : null);
		PlayerCtrl pc = Pc;
		if (fieldMap == null || (Object)(object)pc == (Object)null || fieldMap.portals.Count == 0)
		{
			return 0;
		}
		float num = pc.worldX * 40f;
		float num2 = (0f - pc.FeetPos.y) * 40f;
		int result = 0;
		float num3 = float.MaxValue;
		for (int i = 0; i < fieldMap.portals.Count; i++)
		{
			FieldPortal fieldPortal = fieldMap.portals[i];
			float num4 = fieldPortal.x - num;
			float num5 = fieldPortal.y - num2;
			float num6 = num4 * num4 + num5 * num5;
			if (num6 < num3)
			{
				num3 = num6;
				result = i;
			}
		}
		return result;
	}

	private static Totals PlayerTotals()
	{
		WeaponDef weapon = WeaponDefs.Get(Profile.equipWeapon);
		CharacterAttributes characterAttributes = Profile.Attributes.WithWeapon(weapon, 0, out var watk, out var matk);
		return new Totals
		{
			str = characterAttributes.str,
			dex = characterAttributes.dex,
			intelligence = characterAttributes.intelligence,
			luk = characterAttributes.luk,
			watk = watk,
			matk = matk,
			weaponDefense = characterAttributes.weaponDefense,
			magicDefense = characterAttributes.magicDefense
		};
	}

	private static void RemoveInventory(int type)
	{
		if (type < 1 || type > 5 || GameSession.Character == null)
		{
			return;
		}
		foreach (InventoryEntry item in new List<InventoryEntry>(GameSession.Character.inventory))
		{
			if (item.inventoryType == type && item.position > 0)
			{
				RemoveInventorySlot(type, item.position);
			}
		}
	}

	private static void RemoveInventorySlot(int type, int slot)
	{
		InventoryEntry inventoryEntry = SlotEntry(type, slot);
		if (inventoryEntry != null)
		{
			GameSession.SaveToDb();
			if (!RemoveSlotItem(type, slot, inventoryEntry.quantity))
			{
				throw new JsRuntimeException("删除道具失败");
			}
			GameSession.ApplyInventoryToProfile();
			Profile.Save();
		}
	}

	private static JsValue Location(ScriptOperation operation, string type)
	{
		if (!GameSession.InGame)
		{
			throw new JsRuntimeException("Saved locations require an active character");
		}
		return GameSession.Net.Script(new ScriptRequest
		{
			operation = operation,
			characterId = GameSession.CharacterId,
			location = type,
			mapId = MapId,
			portalId = 0
		}).value;
	}

	private static void BindCommonCm(JsHostObject cm, NpcSession session)
	{
		cm.Set("changeJob", Fn((JsCall c) =>
		{
			ChangeJob(Int(c, 0));
		}));
		cm.Set("getItemCount", Ret((JsCall c) => N(Profile.ItemCount(Int(c, 0)))));
		cm.Set("getSpace", Ret((JsCall _) => N(24.0)));
		cm.Set("useItem", Ret((JsCall c) =>
		{
			int id = Int(c, 0);
			if (Profile.ItemCount(id) < 1)
			{
				return B(v: false);
			}
			Profile.RemoveItem(id, 1);
			return B(v: true);
		}));
		cm.Set("removeItem", Ret((JsCall c) => B(Profile.RemoveItem(Int(c, 0), Math.Max(1, Int(c, 1, 1))))));
		cm.Set("gainNX", Fn((JsCall c) =>
		{
			GameSession.Net.Cash(new CashRequest
			{
				characterId = GameSession.CharacterId,
				operation = CashOperation.AdjustBalance,
				currency = 1,
				amount = Int(c, 0)
			});
		}));
		cm.Set("gainItemPeriod", Fn((JsCall c) =>
		{
			int id = Int(c, 0);
			int num = Int(c, 1, 1);
			if (num > 0)
			{
				Profile.AddItem(id, num);
			}
			else if (num < 0)
			{
				Profile.RemoveItem(id, -num);
			}
		}));
		cm.Set("serverNotice", Fn((JsCall c) =>
		{
			DropNotice(c);
		}));
		cm.Set("countMonster", Ret((JsCall c) =>
		{
			int num = Int(c, 0);
			int num2 = 0;
			foreach (EnemyUnit item in EnemyUnit.All)
			{
				if (item.fieldMode && !item.dead && item.fieldMobId == num)
				{
					num2++;
				}
			}
			return N(num2);
		}));
		cm.Set("removeNpc", Ret((JsCall c) =>
		{
			(int, int) tuple = RemoveScriptTarget(c);
			return RemoveScriptNpc(tuple.Item1, tuple.Item2);
		}));
		cm.Set("removeNPC", cm.Get("removeNpc"));
		cm.Set("spawnNpc", Fn((JsCall c) =>
		{
			SpawnScriptNpc(c);
		}));
		cm.Set("mapMobCount", Ret((JsCall _) => N(CountFieldMonsters())));
		cm.Set("resetMapObjects", Fn((JsCall c) =>
		{
			GameLoop.I?.field?.ClearMobs();
		}));
		cm.Set("spawnMonsterOnGroundBelowIfMissing", Fn((JsCall c) =>
		{
			int num = Int(c, 0);
			foreach (EnemyUnit item2 in EnemyUnit.All)
			{
				if (item2.fieldMode && !item2.dead && item2.fieldMobId == num)
				{
					return;
				}
			}
			SpawnScriptMob(num, Int(c, 1), Int(c, 2), ground: true);
		}));
		cm.Set("scheduleMonsterOnGroundBelowIfMissing", Fn((JsCall c) =>
		{
			int num = Int(c, 0);
			foreach (EnemyUnit item3 in EnemyUnit.All)
			{
				if (item3.fieldMode && !item3.dead && item3.fieldMobId == num)
				{
					return;
				}
			}
			SpawnScriptMob(num, Int(c, 1), Int(c, 2), ground: true);
		}));
		cm.Set("hitReactor", Fn((JsCall c) =>
		{
		}));
		cm.Set("forceHitReactor", Fn((JsCall c) =>
		{
		}));
		cm.Set("forceStartReactor", Fn((JsCall c) =>
		{
		}));
		cm.Set("isAllReactorState", Ret((JsCall _) => B(v: false)));
		cm.Set("getReactorByName", Ret((JsCall _) => JsNull.Value));
		cm.Set("blockPortal", Fn((JsCall c) =>
		{
			string text = Str(c, 0);
			if (!string.IsNullOrEmpty(text))
			{
				FieldDirector.BlockedPortalScripts.Add(text);
			}
		}));
		cm.Set("unblockPortal", Fn((JsCall c) =>
		{
			FieldDirector.BlockedPortalScripts.Remove(Str(c, 0));
		}));
		cm.Set("updateAreaInfo", Fn((JsCall c) =>
		{
			ScriptValueStore("areaInfo_" + Int(c, 0), Str(c, 1), write: true);
		}));
		cm.Set("containsAreaInfo", Ret((JsCall c) =>
		{
			string text = ScriptValueStore("areaInfo_" + Int(c, 0), null);
			string value = Str(c, 1);
			return B(!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(value) && text.Contains(value));
		}));
		cm.Set("addPqPoints", Fn((JsCall c) =>
		{
			int num = ((c.ArgCount <= 0) ? 1 : Int(c, 0));
			SavePqExtend("副本积分", ReadPqExtend("副本积分") + num);
			SavePqExtend("副本总积分", ReadPqExtend("副本总积分") + num);
		}));
		cm.Set("getPQEnteredCount", Ret((JsCall c) => N((c.Arg(0) is JsString) ? ReadPqExtend(Str(c, 0)) : ReadPqExtend(PqEnteredName(Int(c, 0))))));
		cm.Set("recordEntryPQ", Fn((JsCall c) =>
		{
			string text = ((c.Arg(0) is JsString) ? Str(c, 0) : PqEnteredName(Int(c, 0)));
			if (!string.IsNullOrEmpty(text))
			{
				SavePqExtend(text, ReadPqExtend(text) + 1);
			}
		}));
		cm.Set("canEnterPQ", Ret((JsCall c) => B(((c.Arg(0) is JsString jsString) ? ReadPqExtend(jsString.Value) : ReadPqExtend(PqEnteredName(Int(c, 0)))) >= PqEnterMax())));
		cm.Set("getPQEnterMaxCount", Ret((JsCall _) => N(PqEnterMax())));
		cm.Set("gainPQExp", Fn((JsCall c) =>
		{
			long num = (long)Int(c, 0) * 10000L;
			int level = Profile.level;
			long num2 = ExpTable.Needed(level);
			if (num2 < num)
			{
				num = ((level > 50 && level <= 70) ? ((long)((double)num2 * 0.8)) : ((level > 70 && level <= 100) ? ((long)((double)num2 * 0.5)) : ((level <= 100 || level > 124) ? num2 : ((long)((double)num2 * 0.3)))));
			}
			Profile.GrantExperience(num);
		}));
		cm.Set("doGachapon", Fn((JsCall c) =>
		{
			GachaDraw(session, NpcId(session));
		}));
		cm.Set("doRemoteGachapon", Fn((JsCall c) =>
		{
			GachaDraw(session, Int(c, 0));
		}));
		cm.Set("getTotalGachaponCount", Ret((JsCall _) => N(0.0)));
		cm.Set("showEffect", Fn((JsCall c) =>
		{
		}));
		cm.Set("mapEffect", Fn((JsCall c) =>
		{
		}));
		cm.Set("showWZEffect", Fn((JsCall c) =>
		{
		}));
		cm.Set("showInstruction", Fn((JsCall c) =>
		{
			ToastIfAny(Str(c, 0));
		}));
		cm.Set("showInfoText", Fn((JsCall c) =>
		{
			ToastIfAny(Str(c, 0));
		}));
		cm.Set("openEnhancement", Fn((JsCall c) =>
		{
			NativeEnhancementWindow.Open(Int(c, 0));
		}));
		cm.Set("openFlame", Fn((JsCall c) =>
		{
			NativeEnhancementWindow.Open(4);
		}));
		cm.Set("openSoul", Fn((JsCall c) =>
		{
			NativeEnhancementWindow.Open(5);
		}));
		cm.Set("openSymbol", Fn((JsCall c) =>
		{
			NativeSymbolWindow.Open();
		}));
		cm.Set("openHyperStat", Fn((JsCall c) =>
		{
			NativeHyperStatWindow.Open();
		}));
		cm.Set("openInnerAbility", Fn((JsCall c) =>
		{
			NativeInnerAbilityWindow.Open();
		}));
		cm.Set("openVMatrix", Fn((JsCall c) =>
		{
			NativeVMatrixWindow.Open();
		}));
		cm.Set("openHexaMatrix", Fn((JsCall c) =>
		{
			NativeHexaMatrixWindow.Open();
		}));
		cm.Set("openUI", Fn((JsCall c) =>
		{
		}));
		cm.Set("changeMusic", Fn((JsCall c) =>
		{
		}));
		cm.Set("lockUI", Fn((JsCall c) =>
		{
			PlayerCtrl.ScriptUiLock++;
		}));
		cm.Set("lockUI2", Fn((JsCall c) =>
		{
			PlayerCtrl.ScriptUiLock++;
		}));
		cm.Set("unlockUI", Fn((JsCall c) =>
		{
			PlayerCtrl.ScriptUiLock = 0;
		}));
		cm.Set("setDirectionStatus", Fn((JsCall c) =>
		{
		}));
		cm.Set("spawnGuide", Fn((JsCall c) =>
		{
		}));
		cm.Set("removeGuide", Fn((JsCall c) =>
		{
		}));
		cm.Set("talkGuide", Fn((JsCall c) =>
		{
		}));
		cm.Set("updateInfo", Fn((JsCall c) =>
		{
		}));
		cm.Set("getDisconnected", Ret((JsCall _) => B(v: false)));
		cm.Set("getConnection", Ret((JsCall _) =>
		{
			throw new JsRuntimeException("Unmigrated server API: cm.getConnection");
		}));
		cm.Set("divideTeams", Fn((JsCall c) =>
		{
		}));
		cm.Set("warpMembers", Fn((JsCall c) =>
		{
			session.RequestWarp(Int(c, 0));
		}));
		cm.Set("givePartyExp", Fn((JsCall c) =>
		{
			Profile.GrantExperience(Int(c, 0));
		}));
		cm.Set("givePartyItems", Fn((JsCall c) =>
		{
			int id = Int(c, 0);
			int num = Int(c, 1, 1);
			if (num > 0)
			{
				Profile.AddItem(id, num);
			}
			else if (num < 0)
			{
				Profile.RemoveItem(id, -num);
			}
		}));
		cm.Set("removePartyItems", Fn((JsCall c) =>
		{
			Profile.RemoveItem(Int(c, 0), Math.Max(1, Int(c, 1, 1)));
		}));
		cm.Set("hasLevel30Character", Ret((JsCall _) => B(v: false)));
		cm.Set("getPlayerNPCByScriptid", Ret((JsCall _) => JsNull.Value));
		cm.Set("getHallOfFameMapid", Ret((JsCall _) => N(0.0)));
		cm.Set("maxMastery", Fn((JsCall c) =>
		{
		}));
		cm.Set("spawnPlayerNPC", Fn((JsCall c) =>
		{
		}));
		cm.Set("getTest", Ret((JsCall _) => BuildJavaList(new JsArray())));
		cm.Set("weakenAreaBoss", Fn((JsCall c) =>
		{
		}));
		cm.Set("getJailTimeLeft", Ret((JsCall _) => N(0.0)));
		cm.Set("getSkillBookInfo", Ret((JsCall _) => N(0.0)));
		cm.Set("getNamesWhoDropsItem", Ret((JsCall _) => BuildJavaList(new JsArray())));
		cm.Set("linkSystemOverview", Ret((JsCall _) => S(NativeLinkService.Overview(GameServer.Local.Db, GameSession.CharacterId))));
		cm.Set("linkSystemAdd", Ret((JsCall c) => S(NativeLinkService.AddLink(GameServer.Local.Db, GameSession.CharacterId, Int(c, 0)))));
		cm.Set("mentorshipOverview", Ret((JsCall _) => S(NativeMentorshipService.Overview(GameServer.Local.Db, GameSession.CharacterId))));
		cm.Set("mentorshipCreate", Ret((JsCall c) => S(NativeMentorshipService.CreateRelation(GameServer.Local.Db, GameSession.CharacterId, Str(c, 0)))));
		cm.Set("mentorshipPartyMasterMenu", Ret((JsCall _) => S(NativeMentorshipService.PartyMasterMenu(GameServer.Local.Db, GameSession.CharacterId, PartyMemberIds()))));
		cm.Set("mentorshipHasPartyMasterCandidate", Ret((JsCall _) => B(NativeMentorshipService.HasPartyMasterCandidate(GameServer.Local.Db, PartyMemberIds()))));
		cm.Set("mentorshipCreateFromParty", Ret((JsCall c) => S(NativeMentorshipService.CreateFromParty(GameServer.Local.Db, GameSession.CharacterId, Int(c, 0), PartyMemberIds()))));
		cm.Set("mentorshipCancel", Ret((JsCall _) => S(NativeMentorshipService.Cancel(GameServer.Local.Db, GameSession.CharacterId))));
		cm.Set("mentorshipClaimStages", Ret((JsCall _) => S(NativeMentorshipService.ClaimStages(GameServer.Local.Db, GameSession.CharacterId))));
		cm.Set("mentorshipClaimWeekly", Ret((JsCall _) => S(NativeMentorshipService.ClaimWeekly(GameServer.Local.Db, GameSession.CharacterId))));
		cm.Set("mentorshipStartDuel", Ret((JsCall c) => S(NativeMentorshipService.StartDuel(GameServer.Local.Db, GameSession.CharacterId, (long)JsConvert.ToNumber(c.Arg(0))))));
		cm.Set("mentorshipJoinDuel", Ret((JsCall c) => S(NativeMentorshipService.JoinDuel(GameServer.Local.Db, GameSession.CharacterId, (long)JsConvert.ToNumber(c.Arg(0))))));
		cm.Set("mentorshipDuelStatus", Ret((JsCall _) => S(NativeMentorshipService.DuelStatus(GameServer.Local.Db, GameSession.CharacterId))));
		cm.Set("mentorshipRanking", Ret((JsCall c) => S(NativeMentorshipService.Ranking(GameServer.Local.Db, GameSession.CharacterId, Str(c, 0)))));
		cm.Set("mentorshipVirtueCoins", Ret((JsCall _) => N(NativeMentorshipService.CoinsOf(GameServer.Local.Db, GameSession.CharacterId, virtue: true))));
		cm.Set("mentorshipApprenticeCoins", Ret((JsCall _) => N(NativeMentorshipService.CoinsOf(GameServer.Local.Db, GameSession.CharacterId, virtue: false))));
		cm.Set("mentorshipSpendVirtue", Ret((JsCall c) => B(NativeMentorshipService.SpendCoins(GameServer.Local.Db, GameSession.CharacterId, virtue: true, Int(c, 0)))));
		cm.Set("mentorshipSpendApprentice", Ret((JsCall c) => B(NativeMentorshipService.SpendCoins(GameServer.Local.Db, GameSession.CharacterId, virtue: false, Int(c, 0)))));
		cm.Set("getAvailableMasteryBooks", Ret((JsCall _) => new JsArray()));
		cm.Set("getAvailableSkillBooks", Ret((JsCall _) => new JsArray()));
		cm.Set("getEquipInSlot", Ret((JsCall c) =>
		{
			int num = Profile.EquipOfSlot(Int(c, 0));
			return (num <= 0) ? ((JsValue)JsNull.Value) : ((JsValue)BuildEquipItem(num));
		}));
		cm.Set("eatEquip", Fn((JsCall c) =>
		{
		}));
		cm.Set("removeAllByInventorySlot", Fn((JsCall c) =>
		{
			RemoveInventorySlot(Int(c, 0), Int(c, 1));
		}));
		cm.Set("getGuild", Ret((JsCall _) => JsNull.Value));
		cm.Set("isGuildLeader", Ret((JsCall _) => B(v: false)));
		cm.Set("displayGuildRanks", Fn((JsCall c) =>
		{
		}));
		cm.Set("createAlliance", Ret((JsCall _) => NotAvailable("联盟")));
		cm.Set("disbandAlliance", Ret((JsCall _) => NotAvailable("联盟")));
		cm.Set("upgradeAlliance", Ret((JsCall _) => NotAvailable("联盟")));
		cm.Set("getAllianceCapacity", Ret((JsCall _) => N(0.0)));
		cm.Set("canBeUsedAllianceName", Ret((JsCall _) => B(v: false)));
		cm.Set("createExpedition", Ret((JsCall c) => N(CreateExpedition(c, session))));
		cm.Set("endExpedition", Fn((JsCall c) =>
		{
			if (!((c.Arg(0) as JsHostObject)?.Host is Expedition expedition))
			{
				NotAvailable("远征队");
			}
			else
			{
				expedition.Dispose(log: true);
				ExpeditionRegistry.RemoveExpedition(expedition);
			}
		}));
		cm.Set("isLeaderExpedition", Ret((JsCall c) => B(ExpeditionRegistry.GetExpedition(ExpeditionTypeArgument(c.Arg(0)))?.IsLeader(GameSession.CharacterId) ?? false)));
		cm.Set("getExpeditionMemberNames", Ret((JsCall c) =>
		{
			Expedition expedition = ExpeditionRegistry.GetExpedition(ExpeditionTypeArgument(c.Arg(0))) ?? throw new JsRuntimeException("Missing expedition: " + Str(c, 0));
			StringBuilder stringBuilder = new StringBuilder();
			foreach (string value2 in expedition.GetMembers().Values)
			{
				stringBuilder.Append(value2).Append(", ");
			}
			return S(stringBuilder.ToString());
		}));
		cm.Set("fieldLobbied", Ret((JsCall _) => B(v: false)));
		cm.Set("fieldLobbied2", Ret((JsCall _) => B(v: false)));
		cm.Set("fieldTaken", Ret((JsCall _) => B(v: false)));
		cm.Set("fieldTaken2", Ret((JsCall _) => B(v: false)));
		cm.Set("cpqLobby", Ret((JsCall c) =>
		{
			NativeCarnivalLobby.Open(Int(c, 0));
			return JsNull.Value;
		}));
		cm.Set("cpqLobby2", Ret((JsCall _) => JsNull.Value));
		cm.Set("challengeParty", Ret((JsCall _) => NotAvailable("怪物嘉年华")));
		cm.Set("challengeParty2", Ret((JsCall _) => NotAvailable("怪物嘉年华")));
		cm.Set("sendCPQMapLists", Ret((JsCall _) =>
		{
			NativeCarnivalLobby.Open();
			return B(v: true);
		}));
		cm.Set("sendCPQMapLists2", Ret((JsCall _) =>
		{
			NativeCarnivalLobby.Open();
			return B(v: true);
		}));
		cm.Set("answerCPQChallenge", Fn((JsCall c) =>
		{
			NotAvailable("怪物嘉年华");
		}));
		cm.Set("cancelCPQLobby", Fn((JsCall c) =>
		{
		}));
		cm.Set("isCPQLoserMap", Ret((JsCall _) => B(Profile.LastMapId == 980031400 || Profile.LastMapId == 980032400 || Profile.LastMapId == 980033400 || (Profile.LastMapId >= 980000104 && Profile.LastMapId <= 980000604 && (Profile.LastMapId - 980000104) % 100 == 0))));
		cm.Set("isCPQWinnerMap", Ret((JsCall _) => B(Profile.LastMapId == 980031300 || Profile.LastMapId == 980032300 || Profile.LastMapId == 980033300 || (Profile.LastMapId >= 980000103 && Profile.LastMapId <= 980000603 && (Profile.LastMapId - 980000103) % 100 == 0))));
		cm.Set("getFestivalPoints", Ret((JsCall _) => N(NativeCarnivalService.FestivalPoints)));
		cm.Set("partyMembersInMap", Ret((JsCall _) => N(HasParty() ? 1 : 0)));
		cm.Set("getUnclaimedMarriageGifts", Ret((JsCall _) => BuildJavaList(new JsArray())));
		cm.Set("createMarriageWishlist", Ret((JsCall _) => NotAvailable("婚礼")));
		cm.Set("sendMarriageGifts", Ret((JsCall _) => NotAvailable("婚礼")));
		cm.Set("sendMarriageWishlist", Ret((JsCall _) => NotAvailable("婚礼")));
		cm.Set("getOngoingWedding", Ret((JsCall _) => JsNull.Value));
		cm.Set("getWeddingReservationTimeLeft", Ret((JsCall _) => N(0.0)));
		cm.Set("hasMerchant", Ret((JsCall _) => B(v: false)));
		cm.Set("hasMerchantItems", Ret((JsCall _) => B(v: false)));
		cm.Set("showFredrick", Fn((JsCall c) =>
		{
		}));
		cm.Set("resetDojoEnergy", Fn((JsCall c) =>
		{
			dojoEnergy = 0;
		}));
		cm.Set("resetPartyDojoEnergy", Fn((JsCall c) =>
		{
			dojoEnergy = 0;
		}));
		cm.Set("goDojoUp", Fn((JsCall c) =>
		{
		}));
		cm.Set("startAriantBattle", Ret((JsCall _) => NotAvailable("阿里安特竞技场")));
		cm.Set("createPyramid", Ret((JsCall _) => NotAvailable("金字塔")));
		cm.Set("gainTameness", Fn((JsCall c) =>
		{
			InventoryChangeResponse inventoryChangeResponse = GameSession.Net.ChangeInventory(new InventoryChangeRequest
			{
				characterId = GameSession.CharacterId,
				itemId = 5000000,
				petTameness = Int(c, 0)
			});
			if (inventoryChangeResponse.inventory != null)
			{
				GameSession.Character.inventory = inventoryChangeResponse.inventory;
				GameSession.ApplyInventoryToProfile();
				Profile.Save();
			}
		}));
		cm.Set("getDriedPets", Ret((JsCall _) =>
		{
			JsArray jsArray = new JsArray();
			long num = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			if (GameSession.Character != null)
			{
				foreach (InventoryEntry pet in GameSession.Character.inventory)
				{
					if (pet.itemId / 1000 == 5000 && pet.position > 0 && pet.expiration < num && pet.petData != null)
					{
						JsHostObject jsHostObject = new JsHostObject(pet);
						jsHostObject.Set("getName", Ret((JsCall args) => S(pet.petData.TryGetValue("name", out var value) ? value.ToString() : WzNames.Item(pet.itemId))));
						jsHostObject.Set("getLevel", Ret((JsCall args) => N(Convert.ToInt32(pet.petData["level"]))));
						jsHostObject.Set("getTameness", Ret((JsCall args) => N(Convert.ToInt32(pet.petData["closeness"]))));
						jsHostObject.Set("getPosition", Ret((JsCall args) => N(pet.position)));
						jsArray.Items.Add(jsHostObject);
					}
				}
			}
			return BuildJavaList(jsArray);
		}));
		cm.Set("getPet", Ret((JsCall c) => PetView(Int(c, 0))));
		cm.Set("evolvePet", Ret((JsCall c) =>
		{
			InventoryChangeResponse inventoryChangeResponse = GameSession.Net.ChangeInventory(new InventoryChangeRequest
			{
				characterId = GameSession.CharacterId,
				itemId = Int(c, 1),
				evolvePetId = Int(c, 1),
				petSlot = Int(c, 0)
			});
			if (!inventoryChangeResponse.ok)
			{
				BottomBar.Toast(inventoryChangeResponse.error);
				return JsNull.Value;
			}
			GameSession.Character.inventory = inventoryChangeResponse.inventory;
			GameSession.ApplyInventoryToProfile();
			Profile.Save();
			return JsNull.Value;
		}));
	}

	private static JsValue NotAvailable(string system)
	{
		BottomBar.Toast(system + "系统未接入");
		return B(v: false);
	}

	private static int ReadPqExtend(string key)
	{
		if (!int.TryParse(ScriptValueStore(key, null, write: false, 1), out var result))
		{
			return 0;
		}
		return result;
	}

	private static void SavePqExtend(string key, int value)
	{
		ScriptValueStore(key, value.ToString(CultureInfo.InvariantCulture), write: true, 1);
	}

	private static string PqEnteredName(int type)
	{
		return type switch
		{
			1 => "废弃已完成次数", 
			2 => "玩具已完成次数", 
			3 => "水晶已完成次数", 
			_ => "", 
		};
	}

	private static int PqEnterMax()
	{
		return 1;
	}

	private static void GachaDraw(NpcSession session, int gachaponId)
	{
		if (!GameSession.InGame)
		{
			throw new JsRuntimeException("Gachapon requires an active character");
		}
		if (!(GameSession.Net.Script(new ScriptRequest
		{
			operation = ScriptOperation.GachaDraw,
			characterId = GameSession.CharacterId,
			gachaponId = gachaponId
		}).value is JsArray jsArray) || jsArray.Items.Count < 2)
		{
			BottomBar.Toast("百宝箱为空，请联系管理员，百宝箱id: " + gachaponId);
			return;
		}
		int id = JsConvert.ToInt32(jsArray.Items[0]);
		int val = JsConvert.ToInt32(jsArray.Items[1]);
		Profile.AddItem(id, Math.Max(1, val));
		NativeSystemMessage.Drop(0, "你获得了 " + val + " 个 " + WzNames.Item(id));
	}

	private static int NpcId(NpcSession session)
	{
		return session?.NpcId ?? 0;
	}

	private static string ScriptValueStore(string name, string value, bool write = false, int period = 0)
	{
		if (!GameSession.InGame)
		{
			throw new JsRuntimeException("Script values require an active character");
		}
		return GameSession.Net.ScriptValue(new ScriptValueRequest
		{
			characterId = GameSession.CharacterId,
			write = write,
			period = period,
			name = name,
			value = value
		}).value;
	}

	private static JsHostObject BuildJavaList(JsArray items)
	{
		JsHostObject jsHostObject = new JsHostObject(items);
		Lenient(jsHostObject, "list");
		jsHostObject.Set("size", Ret((JsCall _) => N(items.Items.Count)));
		jsHostObject.Set("get", Ret((JsCall c) => (Int(c, 0) < 0 || Int(c, 0) >= items.Items.Count) ? JsNull.Value : items.Items[Int(c, 0)]));
		jsHostObject.Set("isEmpty", Ret((JsCall _) => B(items.Items.Count == 0)));
		jsHostObject.Set("contains", Ret((JsCall c) => B(items.Items.Contains(c.Arg(0)))));
		jsHostObject.Set("toArray", Ret((JsCall _) => items));
		jsHostObject.Set("iterator", Ret((JsCall _) =>
		{
			int cursor = 0;
			JsHostObject jsHostObject2 = new JsHostObject(null);
			jsHostObject2.Set("hasNext", Ret((JsCall jsCall) => B(cursor < items.Items.Count)));
			jsHostObject2.Set("next", Ret((JsCall jsCall) => (cursor >= items.Items.Count) ? JsNull.Value : items.Items[cursor++]));
			return jsHostObject2;
		}));
		return jsHostObject;
	}

	private static ExpeditionType ExpeditionTypeArgument(JsValue value)
	{
		if (value is JsHostObject { Host: var host } jsHostObject)
		{
			if (host is ExpeditionType)
			{
				return (ExpeditionType)host;
			}
			if (jsHostObject.Host is string name && ExpeditionTypes.TryParse(name, out var type))
			{
				return type;
			}
		}
		string text = ((value is JsString jsString) ? jsString.Value : JsConvert.ToString(value));
		if (!string.IsNullOrEmpty(text) && ExpeditionTypes.TryParse(text, out var type2))
		{
			return type2;
		}
		throw new JsRuntimeException("Missing expedition type: " + text);
	}

	private static JsValue CallMember(JsCall call, JsValue target, string name)
	{
		return call.Interp.Call((target is JsHostObject jsHostObject && jsHostObject.Members.Has(name)) ? jsHostObject.Get(name) : call.Interp.GetMember(target, name), target, new JsValue[0]);
	}

	private static int ScriptPlayerId(JsCall call, JsValue value)
	{
		if (!(value is JsNumber jsNumber))
		{
			return JsConvert.ToInt32(CallMember(call, value, "getId"));
		}
		return (int)jsNumber.Value;
	}

	private static string ScriptPlayerName(JsCall call, JsValue value)
	{
		return JsConvert.ToString(CallMember(call, value, "getName"));
	}

	private static int CreateExpedition(JsCall call, NpcSession session)
	{
		ExpeditionType type = ExpeditionTypeArgument(call.Arg(0));
		bool silent = call.ArgCount > 1 && JsConvert.ToBoolean(call.Arg(1));
		int minPlayers = ((call.ArgCount > 2) ? JsConvert.ToInt32(call.Arg(2)) : 0);
		int maxPlayers = ((call.ArgCount > 3) ? JsConvert.ToInt32(call.Arg(3)) : 0);
		int num = ((!GameSession.InGame) ? 1 : GameSession.CharacterId);
		Expedition exped = new Expedition(num, Profile.playerName, MapId, type, silent, minPlayers, maxPlayers);
		if (!ExpeditionBossLog.AttemptBoss(num, type, log: false))
		{
			return 1;
		}
		if (!ExpeditionRegistry.AddExpedition(exped))
		{
			return -1;
		}
		return 0;
	}

	private static JsValue BuildExpedition(Expedition exped, NpcSession session)
	{
		if (exped == null)
		{
			return JsNull.Value;
		}
		JsHostObject jsHostObject = new JsHostObject(exped);
		Lenient(jsHostObject, "expedition");
		jsHostObject.Set("isLeader", Ret((JsCall c) => B(exped.IsLeader(ScriptPlayerId(c, c.Arg(0))))));
		jsHostObject.Set("isRegistering", Ret((JsCall _) => B(exped.Registering)));
		jsHostObject.Set("isInProgress", Ret((JsCall _) => B(exped.IsInProgress())));
		jsHostObject.Set("contains", Ret((JsCall c) => B(exped.Contains(ScriptPlayerId(c, c.Arg(0))))));
		jsHostObject.Set("getLeader", Ret((JsCall _) => MemberOf(exped.LeaderId, exped.LeaderName)));
		jsHostObject.Set("getName", Ret((JsCall _) => S(exped.LeaderName)));
		jsHostObject.Set("getType", Ret((JsCall _) => S(exped.Type.ToString())));
		jsHostObject.Set("getMinSize", Ret((JsCall _) => N(exped.MinSize)));
		jsHostObject.Set("getMaxSize", Ret((JsCall _) => N(exped.MaxSize)));
		jsHostObject.Set("getRecruitingMapId", Ret((JsCall _) => N(exped.StartMapId)));
		jsHostObject.Set("getStartTime", Ret((JsCall _) => N(exped.StartTime)));
		jsHostObject.Set("getBossLogs", Ret((JsCall _) =>
		{
			JsArray jsArray = new JsArray();
			foreach (string bossLog in exped.BossLogs)
			{
				jsArray.Items.Add(S(bossLog));
			}
			return BuildJavaList(jsArray);
		}));
		jsHostObject.Set("addMember", Ret((JsCall c) => S(exped.AddMember(ScriptPlayerId(c, c.Arg(0)), ScriptPlayerName(c, c.Arg(0))))));
		jsHostObject.Set("addMemberInt", Ret((JsCall c) => N(exped.AddMemberInt(ScriptPlayerId(c, c.Arg(0)), ScriptPlayerName(c, c.Arg(0))))));
		jsHostObject.Set("removeMember", Ret((JsCall c) => B(exped.RemoveMember(ScriptPlayerId(c, c.Arg(0))))));
		jsHostObject.Set("ban", Ret((JsCall c) =>
		{
			exped.Ban(JsConvert.ToInt32(CallMember(c, c.Arg(0), "getKey")));
			return JsNull.Value;
		}));
		jsHostObject.Set("getMemberList", Ret((JsCall _) =>
		{
			JsArray jsArray = new JsArray();
			foreach (KeyValuePair<int, string> member in exped.GetMemberList())
			{
				int key = member.Key;
				string value = member.Value;
				JsHostObject jsHostObject2 = new JsHostObject(member);
				jsHostObject2.Set("getKey", Ret((JsCall jsCall) => N(key)));
				jsHostObject2.Set("getValue", Ret((JsCall jsCall) => S(value)));
				jsArray.Items.Add(jsHostObject2);
			}
			return BuildJavaList(jsArray);
		}));
		jsHostObject.Set("getMembers", Ret((JsCall _) =>
		{
			Dictionary<int, string> snapshot = exped.GetMembers();
			JsHostObject jsHostObject2 = new JsHostObject(null);
			Lenient(jsHostObject2, "expedition.members");
			jsHostObject2.Set("size", Ret((JsCall jsCall) => N(snapshot.Count)));
			jsHostObject2.Set("isEmpty", Ret((JsCall jsCall) => B(snapshot.Count == 0)));
			jsHostObject2.Set("get", Ret((JsCall c) => (!snapshot.TryGetValue(Int(c, 0), out var value)) ? ((JsValue)JsNull.Value) : ((JsValue)S(value))));
			jsHostObject2.Set("values", Ret((JsCall jsCall) =>
			{
				JsArray jsArray = new JsArray();
				foreach (string value2 in snapshot.Values)
				{
					jsArray.Items.Add(S(value2));
				}
				return BuildJavaList(jsArray);
			}));
			jsHostObject2.Set("keySet", Ret((JsCall jsCall) =>
			{
				JsArray jsArray = new JsArray();
				foreach (int key2 in snapshot.Keys)
				{
					jsArray.Items.Add(N(key2));
				}
				return BuildJavaList(jsArray);
			}));
			return jsHostObject2;
		}));
		jsHostObject.Set("getProperty", Ret((JsCall c) =>
		{
			string property = exped.GetProperty(Str(c, 0));
			return (property == null) ? ((JsValue)JsNull.Value) : ((JsValue)S(property));
		}));
		jsHostObject.Set("setProperty", Fn((JsCall c) =>
		{
			exped.SetProperty(Str(c, 0), Str(c, 1));
		}));
		jsHostObject.Set("isExpeditionTeamTogether", Ret((JsCall _) => B(exped.IsExpeditionTeamTogether())));
		jsHostObject.Set("warpExpeditionTeam", Fn((JsCall c) =>
		{
			if (GameSession.InGame)
			{
				(session ?? NpcSession.MapContext()).RequestWarp(Int(c, c.ArgCount - 1));
			}
		}));
		return jsHostObject;
		JsValue MemberOf(int id, string name)
		{
			if (id != GameSession.CharacterId)
			{
				JsHostObject jsHostObject2 = new JsHostObject(id);
				jsHostObject2.Set("getId", Ret((JsCall _) => N(id)));
				jsHostObject2.Set("getName", Ret((JsCall _) => S(name)));
				return jsHostObject2;
			}
			return BuildPlayer(session);
		}
	}

	private static JsHostObject BuildEquipItem(int itemId)
	{
		JsHostObject jsHostObject = new JsHostObject(itemId);
		jsHostObject.Set("getItemId", Ret((JsCall _) => N(itemId)));
		jsHostObject.Set("getQuantity", Ret((JsCall _) => N(1.0)));
		return jsHostObject;
	}

	private static bool IsPartyLeader()
	{
		if (!GameSession.InGame)
		{
			return false;
		}
		SocialResponse socialResponse = GameSession.Net.Social(new SocialRequest
		{
			characterId = GameSession.CharacterId
		});
		if (socialResponse.party)
		{
			return socialResponse.partyLeader == GameSession.CharacterId;
		}
		return false;
	}

	private static string WarpName(JsCall c, int i)
	{
		if (c.ArgCount <= i || !(c.Arg(i) is JsString jsString) || string.IsNullOrEmpty(jsString.Value))
		{
			return null;
		}
		return jsString.Value;
	}

	private static int WarpIndex(JsCall c, int i)
	{
		if (c.ArgCount <= i || c.Arg(i) is JsString)
		{
			return -1;
		}
		int num = JsConvert.ToInt32(c.Arg(i));
		if (num < 0)
		{
			return -1;
		}
		return num;
	}

	internal static bool MapExists(int mapId)
	{
		if (mapId < 0)
		{
			return false;
		}
		FieldCatalog.Ensure();
		return FieldCatalog.Maps.ContainsKey(mapId);
	}

	private static int MarketPortalId(int mapId)
	{
		FieldMap map = FieldCatalog.GetMap(mapId);
		if (map == null || map.portals.Count == 0)
		{
			return 0;
		}
		for (int i = 0; i < map.portals.Count; i++)
		{
			if (!string.IsNullOrEmpty(map.portals[i].script) && map.portals[i].script.Contains("market"))
			{
				return i;
			}
		}
		for (int j = 0; j < map.portals.Count; j++)
		{
			if (map.portals[j].type == 0)
			{
				return j;
			}
		}
		return 0;
	}

	private static bool HasParty()
	{
		if (GameSession.InGame)
		{
			return GameSession.Net.Social(new SocialRequest
			{
				characterId = GameSession.CharacterId,
				operation = SocialOperation.Read
			}).party;
		}
		return false;
	}

	private static JsValue BuildParty(NpcSession session)
	{
		if (!GameSession.InGame)
		{
			return JsNull.Value;
		}
		SocialResponse state = GameSession.Net.Social(new SocialRequest
		{
			characterId = GameSession.CharacterId
		});
		if (!state.party)
		{
			return JsNull.Value;
		}
		JsHostObject jsHostObject = new JsHostObject(null);
		jsHostObject.Set("getId", Ret((JsCall _) => N(state.partyId)));
		jsHostObject.Set("getLeaderId", Ret((JsCall _) => N(state.partyLeader)));
		jsHostObject.Set("getLeader", Ret((JsCall _) => Member(state.partyMembers.Find((SocialMember m) => m.id == state.partyLeader))));
		jsHostObject.Set("getMembers", Ret((JsCall _) =>
		{
			JsArray jsArray = new JsArray();
			foreach (SocialMember partyMember in state.partyMembers)
			{
				jsArray.Items.Add(Member(partyMember));
			}
			return jsArray;
		}));
		jsHostObject.Set("getMemberById", Ret((JsCall c) => Member(state.partyMembers.Find((SocialMember m) => m.id == Int(c, 0)))));
		jsHostObject.Set("getPartyMembers", Ret((JsCall _) => PartyCharacters()));
		jsHostObject.Set("getPartyMembersOnline", Ret((JsCall _) => PartyCharacters()));
		return jsHostObject;
		JsValue Member(SocialMember member)
		{
			if (member == null)
			{
				return JsNull.Value;
			}
			if (member.id != GameSession.CharacterId)
			{
				JsHostObject jsHostObject2 = new JsHostObject(null);
				jsHostObject2.Set("getId", Ret((JsCall _) => N(member.id)));
				jsHostObject2.Set("getName", Ret((JsCall _) => S(member.name)));
				jsHostObject2.Set("getLevel", Ret((JsCall _) => N(member.level)));
				jsHostObject2.Set("getJobId", Ret((JsCall _) => N(member.job)));
				jsHostObject2.Set("getMapId", Ret((JsCall _) => N(member.mapId)));
				jsHostObject2.Set("getChannel", Ret((JsCall _) => N(member.channel)));
				jsHostObject2.Set("isLeader", Ret((JsCall _) => B(member.id == state.partyLeader)));
				jsHostObject2.Set("getPlayer", Ret((JsCall _) => JsNull.Value));
				return jsHostObject2;
			}
			return BuildPlayer(session);
		}
		JsValue PartyCharacter(SocialMember member)
		{
			if (member != null)
			{
				JsHostObject jsHostObject2 = new JsHostObject(null);
				jsHostObject2.Set("getId", Ret((JsCall _) => N(member.id)));
				jsHostObject2.Set("getName", Ret((JsCall _) => S(member.name)));
				jsHostObject2.Set("getLevel", Ret((JsCall _) => N(member.level)));
				jsHostObject2.Set("getJobId", Ret((JsCall _) => N(member.job)));
				jsHostObject2.Set("getMapId", Ret((JsCall _) => N(member.mapId)));
				jsHostObject2.Set("getChannel", Ret((JsCall _) => N(member.channel)));
				jsHostObject2.Set("isLeader", Ret((JsCall _) => B(member.id == state.partyLeader)));
				jsHostObject2.Set("isOnline", Ret((JsCall _) => B(v: true)));
				jsHostObject2.Set("getPlayer", Ret((JsCall _) => (member.id != GameSession.CharacterId) ? ((JsValue)JsNull.Value) : ((JsValue)BuildPlayer(session))));
				return jsHostObject2;
			}
			return JsNull.Value;
		}
		JsValue PartyCharacters()
		{
			JsArray jsArray = new JsArray();
			foreach (SocialMember partyMember2 in state.partyMembers)
			{
				jsArray.Items.Add(PartyCharacter(partyMember2));
			}
			return BuildJavaList(jsArray);
		}
	}

	private static JsHostObject BuildStorage()
	{
		JsHostObject jsHostObject = new JsHostObject(null);
		jsHostObject.Set("sendStorage", Fn((JsCall c) =>
		{
			if (Profile.level < 15)
			{
				BottomBar.Toast("15级以后才可以使用仓库服务");
			}
			else
			{
				NativeStorageWindow.Open(Int(c, 1));
			}
		}));
		string[] array = new string[7] { "sendStored", "sendTakenOut", "sendMeso", "arrange", "close", "save", "increaseSlots" };
		foreach (string key in array)
		{
			jsHostObject.Set(key, Fn((JsCall _) =>
			{
			}));
		}
		jsHostObject.Set("getSlots", Ret((JsCall _) => N(0.0)));
		jsHostObject.Set("getMeso", Ret((JsCall _) => N(0.0)));
		jsHostObject.Set("getItems", Ret((JsCall _) => new JsArray()));
		jsHostObject.Set("isEmpty", Ret((JsCall _) => B(v: true)));
		return jsHostObject;
	}

	private static JsHostObject BuildCashShop()
	{
		JsHostObject jsHostObject = new JsHostObject(null);
		jsHostObject.Set("getCash", Ret((JsCall c) =>
		{
			CashResponse cashResponse = GameSession.Net.Cash(new CashRequest
			{
				characterId = GameSession.CharacterId
			});
			return N(Int(c, 0) switch
			{
				4 => cashResponse.prepaid, 
				2 => cashResponse.points, 
				1 => cashResponse.credit, 
				_ => 0, 
			});
		}));
		jsHostObject.Set("gainCash", Fn((JsCall c) =>
		{
			CashResponse cashResponse = GameSession.Net.Cash(new CashRequest
			{
				characterId = GameSession.CharacterId,
				operation = CashOperation.AdjustBalance,
				currency = Int(c, 0),
				amount = Int(c, 1)
			});
			if (!cashResponse.ok)
			{
				BottomBar.Toast(cashResponse.error);
			}
		}));
		return jsHostObject;
	}

	private static void ToastIfAny(string text)
	{
		if (!string.IsNullOrEmpty(text))
		{
			BottomBar.Toast(text);
		}
	}

	private static void DropNotice(JsCall c)
	{
		if (c.ArgCount > 1)
		{
			NativeSystemMessage.Drop(Int(c, 0), Str(c, 1));
		}
		else
		{
			NativeSystemMessage.Drop(0, Str(c, 0));
		}
	}

	private static string Key(JsCall c, int i)
	{
		if (i >= c.ArgCount)
		{
			return "";
		}
		JsValue jsValue = c.Arg(i);
		if (jsValue is JsString jsString)
		{
			return jsString.Value;
		}
		return JsConvert.ToString(jsValue);
	}

	private static void StartQuest(int id, int npcId)
	{
		if (id > 0)
		{
			if (!QuestCatalog.Exists(id))
			{
				Profile.StartQuest(id);
			}
			else
			{
				QuestLogic.Start(QuestCatalog.Get(id), npcId, applyActions: false);
			}
		}
	}

	private static void CompleteQuest(int id, int npcId)
	{
		if (id > 0)
		{
			if (!QuestCatalog.Exists(id))
			{
				Profile.CompleteQuest(id);
			}
			else
			{
				QuestLogic.Complete(QuestCatalog.Get(id), npcId, applyActions: false);
			}
		}
	}

	private static bool HasPqFlag(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return false;
		}
		FullCharacter character = GameSession.Character;
		if (character == null)
		{
			return headlessPqItems.Contains(key);
		}
		return PqList(character.record.dataString).Contains(key);
	}

	private static void SetPqFlag(string key, bool on)
	{
		if (string.IsNullOrEmpty(key))
		{
			return;
		}
		FullCharacter character = GameSession.Character;
		if (character == null)
		{
			if (on)
			{
				headlessPqItems.Add(key);
			}
			else
			{
				headlessPqItems.Remove(key);
			}
			return;
		}
		List<string> list = PqList(character.record.dataString);
		list.RemoveAll((string x) => x == key);
		if (on)
		{
			list.Add(key);
		}
		character.record.dataString = string.Join(";", list);
		GameSession.SaveToDb();
	}

	private static List<string> PqList(string raw)
	{
		List<string> list = new List<string>();
		if (string.IsNullOrEmpty(raw))
		{
			return list;
		}
		string[] array = raw.Split(';');
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim();
			if (text.Length > 0 && !list.Contains(text))
			{
				list.Add(text);
			}
		}
		return list;
	}

	private static bool StaleLetter(int id)
	{
		if (id >= 4031008 && id <= 4031011)
		{
			return Profile.ItemCount(4031012) > 0;
		}
		return false;
	}

	private static int ItemQuantity(int id)
	{
		if (!StaleLetter(id))
		{
			return Profile.ItemCount(id);
		}
		return 0;
	}

	private static bool HasItem(int id, int count)
	{
		return ItemQuantity(id) >= Math.Max(1, count);
	}

	private static bool ItemEquipped(int id)
	{
		if (id <= 0)
		{
			return false;
		}
		PlayerProfileData data = Profile.Data;
		for (int num = -1; num >= -11; num--)
		{
			if (data.EquipOfSlot(num) == id)
			{
				return true;
			}
		}
		if (data.accessorySlots != null)
		{
			foreach (int accessorySlot in data.accessorySlots)
			{
				if (data.EquipOfSlot(accessorySlot) == id)
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool HasItemWithId(int id, bool checkEquipped)
	{
		if (ItemQuantity(id) <= 0)
		{
			if (checkEquipped)
			{
				return ItemEquipped(id);
			}
			return false;
		}
		return true;
	}

	private static long PartyIdOf(JsCall c, int index)
	{
		if (!(c.Arg(index) is JsHostObject jsHostObject))
		{
			return 0L;
		}
		if (!(c.Interp.Call(c.Interp.GetMember(jsHostObject, "getId"), jsHostObject, new JsValue[0]) is JsNumber jsNumber))
		{
			return 0L;
		}
		return (long)jsNumber.Value;
	}

	private static List<long> PartyMemberIds()
	{
		List<long> list = new List<long>();
		if (!GameSession.InGame)
		{
			return list;
		}
		SocialResponse socialResponse = GameSession.Net.Social(new SocialRequest
		{
			characterId = GameSession.CharacterId
		});
		if (!socialResponse.party)
		{
			return list;
		}
		foreach (SocialMember partyMember in socialResponse.partyMembers)
		{
			list.Add(partyMember.id);
		}
		return list;
	}

	private static void ClearStaleLetters()
	{
		for (int i = 4031008; i <= 4031011; i++)
		{
			int num = Profile.ItemCount(i);
			if (num > 0)
			{
				Profile.RemoveItem(i, num);
			}
		}
	}

	private static void ChangeJob(int jobId)
	{
		if (jobId > 0 && JobSystem.ChangeJobById(jobId))
		{
			ClearStaleLetters();
			Pc?.RefreshProgressionCaps();
			BottomBar.Toast("转职成功：" + JobDefs.NameOf(jobId));
			Debug.Log((object)("[Script] changeJobById(" + jobId + ") → " + JobDefs.NameOf(jobId)));
		}
	}

	private static JsValue PetView(int slot)
	{
		if (!GameSession.InGame)
		{
			return JsNull.Value;
		}
		InventoryEntry pet = GameSession.Net.ChangeInventory(new InventoryChangeRequest
		{
			characterId = GameSession.CharacterId,
			itemId = 5000000,
			getPet = true,
			petSlot = slot
		}).pet;
		if (pet == null)
		{
			return JsNull.Value;
		}
		JsHostObject jsHostObject = new JsHostObject(pet);
		jsHostObject.Set("getItemId", Ret((JsCall _) => N(pet.itemId)));
		jsHostObject.Set("getPosition", Ret((JsCall _) => N(pet.position)));
		jsHostObject.Set("getName", Ret((JsCall _) => S(pet.petData["name"].ToString())));
		jsHostObject.Set("getLevel", Ret((JsCall _) => N(Convert.ToInt32(pet.petData["level"]))));
		jsHostObject.Set("getTameness", Ret((JsCall _) => N(Convert.ToInt32(pet.petData["closeness"]))));
		jsHostObject.Set("getCloseness", Ret((JsCall _) => N(Convert.ToInt32(pet.petData["closeness"]))));
		jsHostObject.Set("getFullness", Ret((JsCall _) => N(Convert.ToInt32(pet.petData["fullness"]))));
		return jsHostObject;
	}

	public static JsHostObject BuildPlayer(NpcSession session)
	{
		JsHostObject p = new JsHostObject(session);
		Lenient(p, "player");
		p.Set("getPet", Ret((JsCall c) => PetView(Int(c, 0))));
		p.Set("getHp", Ret((JsCall _) => N(CurrentHp)));
		p.Set("getMp", Ret((JsCall _) => N(CurrentMp)));
		p.Set("getMaxHp", Ret((JsCall _) => N(((Object)(object)Pc != (Object)null) ? Pc.hpMax : ((float)MaxHpFallback))));
		p.Set("getMaxMp", Ret((JsCall _) => N(((Object)(object)Pc != (Object)null) ? Pc.mpMax : ((float)MaxMpFallback))));
		p.Set("getLevel", Ret((JsCall _) => N(Profile.level)));
		p.Set("getMaxClassLevel", Ret((JsCall _) => GameSession.Net.Script(new ScriptRequest
		{
			operation = ScriptOperation.GetMaxClassLevel,
			characterId = GameSession.CharacterId
		}).value));
		p.Set("getWorld", Ret((JsCall _) => N(GameSession.World)));
		p.Set("gainExp", Fn((JsCall c) =>
		{
			Profile.GrantExperience(Int(c, 0));
		}));
		p.Set("gainMeso", Fn((JsCall c) =>
		{
			Profile.AddGold(Int(c, 0));
		}));
		p.Set("getJobId", Ret((JsCall _) => N(QuestLogic.PlayerJob)));
		p.Set("getJob", Ret((JsCall _) => BuildJob(QuestLogic.PlayerJob)));
		p.Set("isCygnus", Ret((JsCall _) => B(QuestLogic.PlayerJob / 1000 == 1)));
		p.Set("getFishLevel", Ret((JsCall _) => N(GameSession.Character?.record.fishLevel ?? 1)));
		p.Set("getHair", Ret((JsCall _) => N(GameSession.Character?.record.hair ?? 0)));
		p.Set("getFace", Ret((JsCall _) => N(GameSession.Character?.record.face ?? 0)));
		p.Set("getBuddylist", Ret((JsCall _) =>
		{
			JsHostObject jsHostObject = new JsHostObject(null);
			Lenient(jsHostObject, "buddylist");
			jsHostObject.Set("getCapacity", Ret((JsCall c) => N(GameSession.Character?.record.buddyCapacity ?? 25)));
			return jsHostObject;
		}));
		p.Set("setBuddyCapacity", Fn((JsCall c) =>
		{
			if (!GameSession.InGame)
			{
				throw new JsRuntimeException("Buddy capacity requires an active character");
			}
			int buddyCapacity = Int(c, 0);
			SocialResponse socialResponse = GameSession.Net.Social(new SocialRequest
			{
				characterId = GameSession.CharacterId,
				operation = SocialOperation.SetBuddyCapacity,
				buddyCapacity = buddyCapacity
			});
			if (!socialResponse.ok)
			{
				throw new JsRuntimeException(socialResponse.error);
			}
			GameSession.Character.record.buddyCapacity = buddyCapacity;
		}));
		p.Set("getMonsterBook", Ret((JsCall _) =>
		{
			JsHostObject jsHostObject = new JsHostObject(null);
			Lenient(jsHostObject, "monsterBook");
			jsHostObject.Set("getTotalCards", Ret((JsCall c) => N(GameSession.InGame ? GameSession.Net.Social(new SocialRequest
			{
				characterId = GameSession.CharacterId
			}).cards.Count : 0)));
			return jsHostObject;
		}));
		p.Set("isRecvPartySearchInviteEnabled", Ret((JsCall _) =>
		{
			if (!GameSession.InGame || GameSession.Character == null)
			{
				throw new JsRuntimeException("Party search preference requires an active character");
			}
			return B(GameSession.Character.record.partySearch != 0);
		}));
		p.Set("toggleRecvPartySearchInvite", Ret((JsCall _) =>
		{
			if (!GameSession.InGame || GameSession.Character == null)
			{
				throw new JsRuntimeException("Party search preference requires an active character");
			}
			CharRecord record = GameSession.Character.record;
			bool flag = record.partySearch == 0;
			record.partySearch = (flag ? 1 : 0);
			if (GameSession.Net != null)
			{
				int level = Profile.level;
				GameSession.Net.Social(new SocialRequest
				{
					characterId = GameSession.CharacterId,
					operation = (flag ? SocialOperation.SearchParty : SocialOperation.StopSearch),
					minLevel = Math.Max(1, level - 30),
					maxLevel = level + 30
				});
			}
			GameSession.SaveToDb();
			return B(flag);
		}));
		p.Set("gmLevel", Ret((JsCall _) => N(GameSession.Character?.record.gm ?? 0)));
		p.Set("gotPartyQuestItem", Ret((JsCall c) => B(HasPqFlag(Str(c, 0)))));
		p.Set("setPartyQuestItemObtained", Fn((JsCall c) =>
		{
			SetPqFlag(Str(c, 0), on: true);
		}));
		p.Set("removePartyQuestItem", Fn((JsCall c) =>
		{
			SetPqFlag(Str(c, 0), on: false);
		}));
		p.Set("getRemainingSp", Ret((JsCall _) => N(Profile.Sp)));
		p.Set("getGuildId", Ret((JsCall _) => N(GameSession.Character?.record.guildid ?? 0)));
		p.Set("getGuildRank", Ret((JsCall _) => N(GameSession.Character?.record.guildrank ?? 5)));
		p.Set("getGuild", Ret((JsCall _) =>
		{
			JsHostObject jsHostObject = new JsHostObject(null);
			Lenient(jsHostObject, "guild");
			jsHostObject.Set("getId", Ret((JsCall jsCall) => N(GameSession.Character?.record.guildid ?? 0)));
			jsHostObject.Set("getCapacity", Ret((JsCall jsCall) => N(NativeCommunityWindow.Request(CommunityOperation.Read).capacity)));
			jsHostObject.Set("getName", Ret((JsCall jsCall) => S(NativeCommunityWindow.Request(CommunityOperation.Read).guildName)));
			return jsHostObject;
		}));
		p.Set("genericGuildMessage", Fn((JsCall c) =>
		{
			int num2 = Int(c, 0);
			if (num2 != 1)
			{
				throw new JsRuntimeException("Unmigrated guild message code: " + num2);
			}
			if (!GameSession.InGame)
			{
				throw new JsRuntimeException("Guild creation requires an active character");
			}
			NativeDlgInput.Open("请输入您想要创建的家族名称。（3～12 个字）", "", 12, (string value) =>
			{
				NativeCommunityWindow.InputName = value;
				NativeCommunityWindow.Request(CommunityOperation.CreateGuild);
			});
		}));
		p.Set("disbandGuild", Fn((JsCall _) =>
		{
			B(NativeCommunityWindow.Request(CommunityOperation.DisbandGuild).ok);
		}));
		p.Set("increaseGuildCapacity", Fn((JsCall _) =>
		{
			B(NativeCommunityWindow.Request(CommunityOperation.ExpandGuild).ok);
		}));
		p.Set("getGender", Ret((JsCall _) => N(GameSession.Character?.record.gender ?? 0)));
		p.Set("getName", Ret((JsCall _) => S(Profile.playerName)));
		p.Set("isGM", Ret((JsCall _) => B((GameSession.Character?.record.gm ?? 0) > 1)));
		p.Set("getDamageCap", Ret((JsCall _) => N(GameSession.Character?.record.damageCap ?? 19999999)));
		Totals totals = default;
		bool totalsReady = false;
		p.Set("getTotalStr", Ret((JsCall _) => N(Total().str)));
		p.Set("getTotalDex", Ret((JsCall _) => N(Total().dex)));
		p.Set("getTotalInt", Ret((JsCall _) => N(Total().intelligence)));
		p.Set("getTotalLuk", Ret((JsCall _) => N(Total().luk)));
		p.Set("getTotalWatk", Ret((JsCall _) => N(Total().watk)));
		p.Set("getTotalMagic", Ret((JsCall _) => N(Total().matk)));
		p.Set("getJobStyle", Ret((JsCall _) => JobEnum.Js(JobEnum.Style(QuestLogic.PlayerJob, Total().str, Total().dex))));
		p.Set("getItemQuantity", Ret((JsCall c) => N(Profile.ItemCount(Int(c, 0)))));
		p.Set("getItemName", Ret((JsCall c) => S(WzNames.Item(Int(c, 0)))));
		p.Set("checkoutBroadcast", Ret((JsCall _) => B(v: false)));
		p.Set("sendFullServerBroadcast", Fn((JsCall c) =>
		{
			ToastIfAny(Str(c, 0));
		}));
		p.Set("sendBroadcast", Fn((JsCall c) =>
		{
			ToastIfAny((Int(c, 0) == 0) ? Str(c, 2) : (Str(c, 1) + "：" + Str(c, 2)));
		}));
		p.Set("sendAllWordPQNotice", Fn((JsCall c) =>
		{
			if (GameSession.InGame)
			{
				ToastIfAny("[副本公告] : 恭喜玩家 " + Profile.playerName + " 完成" + Str(c, 0) + "。");
			}
		}));
		BindSavedLocations(p);
		p.Set("setCS", Fn((JsCall c) =>
		{
			useCS = JsConvert.ToBoolean(c.Arg(0));
		}));
		p.Set("saveLocation", Fn((JsCall c) =>
		{
			SaveLocation(Str(c, 0));
		}));
		p.Set("saveLocationOnWarp", Fn((JsCall _) =>
		{
			SaveLocationOnWarp();
		}));
		p.Set("startMonsterVac", Ret((JsCall _) => B((Object)(object)GameLoop.I?.player != (Object)null && NativeMonsterVac.For(GameLoop.I.player).StartVac())));
		p.Set("isMonsterVacActive", Ret((JsCall _) =>
		{
			GameLoop i = GameLoop.I;
			int v;
			if (i == null)
			{
				v = 0;
			}
			else
			{
				PlayerCtrl player = i.player;
				v = ((((player == null) ? ((bool?)null) : ((Component)player).GetComponent<NativeMonsterVac>()?.Active) == true) ? 1 : 0);
			}
			return B((byte)v != 0);
		}));
		p.Set("stopMonsterVac", Fn((JsCall _) =>
		{
			GameLoop i = GameLoop.I;
			if (i != null)
			{
				PlayerCtrl player = i.player;
				if (player != null)
				{
					((Component)player).GetComponent<NativeMonsterVac>()?.StopVac();
				}
			}
		}));
		p.Set("startPartyGrindCompanions", Ret((JsCall _) => B(GameSession.InGame && GameSession.Net.PartyGrind(new PartyGrindRequest
		{
			characterId = GameSession.CharacterId,
			operation = PartyGrindOperation.Start
		}).ok)));
		p.Set("isPartyGrindActive", Ret((JsCall _) => B(GameSession.InGame && GameSession.Net.PartyGrind(new PartyGrindRequest
		{
			characterId = GameSession.CharacterId
		}).active)));
		p.Set("stopPartyGrindCompanions", Fn((JsCall _) =>
		{
			if (GameSession.InGame)
			{
				GameSession.Net.PartyGrind(new PartyGrindRequest
				{
					characterId = GameSession.CharacterId,
					operation = PartyGrindOperation.Stop
				});
			}
		}));
		p.Set("getParty", Ret((JsCall _) => BuildParty(session)));
		p.Set("getId", Ret((JsCall _) => N((!GameSession.InGame) ? 1 : GameSession.CharacterId)));
		p.Set("getMeso", Ret((JsCall _) => N(Profile.gold)));
		p.Set("getMapId", Ret((JsCall _) => N(MapId)));
		p.Set("getMap", Ret((JsCall _) => BuildMap()));
		p.Set("getPosition", Ret((JsCall _) => BuildPosition()));
		p.Set("getClient", Ret((JsCall _) => BuildClient(p)));
		p.Set("getEventInstance", Ret((JsCall _) => NativeEventScripts.Instance()));
		p.Set("getAbstractPlayerInteraction", Ret((JsCall _) =>
		{
			NpcSession npcSession = session ?? NpcSession.MapContext();
			return Build(npcSession, npcSession.NpcId, npcSession.QuestId, npcSession.IsQuest);
		}));
		p.Set("haveItem", Ret((JsCall c) => B(HasItem(Int(c, 0), Int(c, 1, 1)))));
		p.Set("haveItemWithId", Ret((JsCall c) => B(HasItemWithId(Int(c, 0), c.ArgCount > 1 && JsConvert.ToBoolean(c.Arg(1))))));
		NativeMentorshipService.OnlinePowerOfActiveCharacter = () =>
		{
			CharRecord charRecord = GameSession.Character?.record;
			if (charRecord == null)
			{
				return 0L;
			}
			Totals totals2 = Total();
			return (long)(totals2.str + totals2.dex + totals2.intelligence + totals2.luk) * (long)Math.Max(1, Math.Max(totals2.watk, totals2.matk)) + (long)charRecord.maxhp / 10L + (long)charRecord.maxmp / 20L + NativeMentorshipService.CashOfActiveAccount() / 100;
		};
		p.Set("canHold", Ret((JsCall c) => B(CanHold(new int[1] { Int(c, 0) }, new int[1] { Int(c, 1, 1) }))));
		p.Set("getInventory", Ret((JsCall c) => BuildInventory(Int(c, 0))));
		p.Set("hasEmptySlot", Ret((JsCall c) => B(FreeInventorySlots(Int(c, 0)) > 0)));
		p.Set("getQuestStatus", Ret((JsCall c) => N(Profile.QuestStatus(Int(c, 0)))));
		p.Set("updateHp", Fn((JsCall c) =>
		{
			CurrentHp = Int(c, 0);
		}));
		p.Set("updateMp", Fn((JsCall c) =>
		{
			CurrentMp = Int(c, 0);
		}));
		p.Set("addHP", Fn((JsCall c) =>
		{
			CurrentHp += Int(c, 0);
		}));
		p.Set("addMP", Fn((JsCall c) =>
		{
			CurrentMp += Int(c, 0);
		}));
		p.Set("updateHpMp", Fn((JsCall c) =>
		{
			CurrentHp = Int(c, 0);
			CurrentMp = Int(c, 1);
		}));
		p.Set("dropMessage", Fn((JsCall c) =>
		{
			ToastIfAny((c.ArgCount > 1) ? Str(c, 1) : Str(c, 0));
		}));
		p.Set("message", Fn((JsCall c) =>
		{
			ToastIfAny(Str(c, 0));
		}));
		p.Set("changeMap", Fn((JsCall c) =>
		{
			int mapId = (((c.Arg(0) as JsHostObject)?.Host is NativeEventMap nativeEventMap) ? nativeEventMap.Id : Int(c, 0));
			int portalId = -1;
			if ((c.Arg(1) as JsHostObject)?.Host is MapPortal portal)
			{
				portalId = eventMapPortalIndex(c.Arg(0), portal);
			}
			else if ((c.Arg(1) as JsHostObject)?.Host is FieldPortal fieldPortal)
			{
				portalId = fieldPortal.id;
			}
			else if (c.ArgCount > 1 && !(c.Arg(1) is JsNull) && !(c.Arg(1) is JsUndefined))
			{
				portalId = Int(c, 1);
			}
			session.RequestWarp(mapId, null, portalId);
		}));
		p.Set("setMap", Fn((JsCall c) =>
		{
			if (NpcSession.Active != null)
			{
				NpcSession.Active.RequestWarp(Int(c, 0));
			}
		}));
		p.Set("isAlive", Ret((JsCall _) => B(((Object)(object)Pc != (Object)null) ? (Pc.hp > 0f) : (CurrentHp > 0))));
		p.Set("getEvents", Ret((JsCall _) => new JsArray()));
		p.Set("getAreaInfos", Ret((JsCall _) =>
		{
			JsHostObject jsHostObject = new JsHostObject(null);
			jsHostObject.Set("get", Ret(Read));
			jsHostObject.Set("put", Ret((JsCall c) =>
			{
				JsValue result = Read(c);
				ScriptValueStore(Key(c), Str(c, 1), write: true);
				return result;
			}));
			jsHostObject.Set("containsKey", Ret((JsCall c) => B(!(Read(c) is JsNull))));
			return jsHostObject;
		}));
		p.Set("getPartnerId", Ret((JsCall _) => N(GameSession.Character?.record.partnerId ?? 0)));
		p.Set("getCashShop", Ret((JsCall _) => (!GameSession.InGame) ? ((JsValue)JsNull.Value) : ((JsValue)BuildCashShop())));
		p.Set("getStorage", Ret((JsCall _) => BuildStorage()));
		p.Set("getExtraStorage", Ret((JsCall _) =>
		{
			JsHostObject jsHostObject = new JsHostObject(null);
			Lenient(jsHostObject, "ExtraStorage");
			jsHostObject.Set("getItemQuantity", Ret((JsCall c) =>
			{
				InventoryChangeResponse inventoryChangeResponse = GameSession.Net.ChangeInventory(new InventoryChangeRequest
				{
					characterId = GameSession.CharacterId,
					itemId = Int(c, 0),
					extraStorageType = Int(c, 1),
					extraStorage = ExtraStorageOperation.Read
				});
				if (!inventoryChangeResponse.ok)
				{
					throw new JsRuntimeException(inventoryChangeResponse.error);
				}
				return N(inventoryChangeResponse.extraStorageQuantity);
			}));
			return jsHostObject;
		}));
		ExtraStorageOperation[] array = new ExtraStorageOperation[2]
		{
			ExtraStorageOperation.Store,
			ExtraStorageOperation.Withdraw
		};
		foreach (ExtraStorageOperation extraStorageOperation in array)
		{
			ExtraStorageOperation action = extraStorageOperation;
			p.Set((action == ExtraStorageOperation.Store) ? "storeExtraItem" : "takeOutExtraItem", Ret((JsCall c) =>
			{
				InventoryChangeResponse inventoryChangeResponse = GameSession.Net.ChangeInventory(new InventoryChangeRequest
				{
					characterId = GameSession.CharacterId,
					itemId = Int(c, 0),
					quantity = Int(c, 1),
					extraStorageType = Int(c, 2),
					extraStorage = action
				});
				if (inventoryChangeResponse.ok)
				{
					GameSession.Character.inventory = inventoryChangeResponse.inventory;
					GameSession.ApplyInventoryToProfile();
					Profile.Save();
				}
				else if (!string.IsNullOrEmpty(inventoryChangeResponse.error))
				{
					NativeSystemMessage.Drop(5, inventoryChangeResponse.error);
				}
				return B(inventoryChangeResponse.ok);
			}));
		}
		p.Set("getFamilyEntry", Ret((JsCall _) => JsNull.Value));
		p.Set("sendPacket", Fn((JsCall c) =>
		{
			ApplyScriptPacket(c.Arg(0));
		}));
		p.Set("cancelEffect", Fn((JsCall c) =>
		{
		}));
		p.Set("cancelAllBuffs", Fn((JsCall c) =>
		{
		}));
		p.Set("respawn", Fn((JsCall c) =>
		{
			NativeEventScriptService.Instance instance = (c.Arg(0) as JsHostObject)?.Host as NativeEventScriptService.Instance;
			int num2 = JsConvert.ToInt32(c.Arg(1));
			instance?.Unregister(p);
			if (num2 > 0)
			{
				FieldDirector fieldDirector = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.field : null);
				if ((Object)(object)fieldDirector != (Object)null)
				{
					fieldDirector.WarpTo(num2);
				}
				else
				{
					Profile.LastMapId = num2;
				}
			}
		}));
		p.Set("enableActions", Fn((JsCall c) =>
		{
		}));
		p.Set("setStance", Fn((JsCall c) =>
		{
		}));
		p.Set("resetEnteredScript", Fn((JsCall c) =>
		{
		}));
		p.Set("getIdleMovement", Ret((JsCall _) => new JsHostObject(null)));
		p.Set("startMapEffect", Fn((JsCall c) =>
		{
		}));
		p.Set("haveItemEquipped", Ret((JsCall c) => B(ItemEquipped(Int(c, 0)))));
		p.Set("isMarried", Ret((JsCall _) => B((GameSession.Character?.record.partnerId ?? 0) > 0)));
		p.Set("getMarriageRing", Ret((JsCall _) =>
		{
			int num2 = GameSession.Character?.record.marriageItemId ?? 0;
			if (num2 <= 0 || (GameSession.Character?.record.partnerId ?? 0) <= 0)
			{
				return JsNull.Value;
			}
			foreach (InventoryEntry item in GameSession.Character.inventory ?? new List<InventoryEntry>())
			{
				if (item.itemId == num2 && item.inventoryType == 1)
				{
					JsHostObject jsHostObject = new JsHostObject(item);
					Lenient(jsHostObject, "item");
					jsHostObject.Set("getItemId", Ret((JsCall jsCall) => N(item.itemId)));
					jsHostObject.Set("getPosition", Ret((JsCall jsCall) => N(item.position)));
					jsHostObject.Set("equipped", Ret((JsCall jsCall) => B(item.position < 0)));
					return jsHostObject;
				}
			}
			return JsNull.Value;
		}));
		p.Set("getMarriageInstance", Ret((JsCall _) => JsNull.Value));
		p.Set("getAriantPoints", Ret((JsCall _) => N(GameSession.Character?.record.ariantPoints ?? 0)));
		p.Set("gainAriantPoints", Fn((JsCall c) =>
		{
			(GameSession.Character?.record ?? throw new JsRuntimeException("Ariant points require an active character")).ariantPoints += Int(c, 0);
			GameSession.SaveToDb();
		}));
		p.Set("getAriantColiseum", Ret((JsCall _) => JsNull.Value));
		p.Set("getDojoPoints", Ret((JsCall _) => N(GameSession.Character?.record.dojoPoints ?? 0)));
		p.Set("getDojoStage", Ret((JsCall _) => N(GameSession.Character?.record.lastDojoStage ?? 0)));
		p.Set("setDojoStage", Fn((JsCall c) =>
		{
			(GameSession.Character?.record ?? throw new JsRuntimeException("Dojo stage requires an active character")).lastDojoStage = Int(c, 0);
			GameSession.SaveToDb();
		}));
		p.Set("setDojoEnergy", Fn((JsCall c) =>
		{
			dojoEnergy = Math.Min(Int(c, 0), 10000);
		}));
		p.Set("getDojoEnergy", Ret((JsCall _) => N(dojoEnergy)));
		p.Set("getBuffSource", Ret((JsCall c) =>
		{
			string stat = ((c.Arg(0) as JsHostObject)?.Host as string) ?? Str(c, 0);
			NativeBuffs nativeBuffs = (((Object)(object)Pc != (Object)null) ? ((Component)Pc).GetComponent<NativeBuffs>() : null);
			if ((Object)(object)nativeBuffs == (Object)null)
			{
				return JsNull.Value;
			}
			int num2 = nativeBuffs.SourceOf(stat);
			return (num2 != 0) ? ((JsValue)N((num2 < 0) ? (-num2) : num2)) : ((JsValue)JsNull.Value);
		}));
		p.Set("getMarriageItemId", Ret((JsCall _) => N(int.TryParse(GameSession.InGame ? ScriptValueStore("marriageItemId", null) : null, out var result) ? result : 0)));
		BindCommonPlayer(p, session);
		return p;
		static string Key(JsCall call)
		{
			return "areaInfo_" + (short)Int(call, 0);
		}
		static JsValue Read(JsCall call)
		{
			string text = ScriptValueStore(Key(call), null);
			if (!string.IsNullOrEmpty(text))
			{
				return S(text);
			}
			return JsNull.Value;
		}
		Totals Total()
		{
			if (!totalsReady)
			{
				totals = PlayerTotals();
				totalsReady = true;
			}
			return totals;
		}
	}

	private static JsArray RebirthSettings()
	{
		return (JsArray)GameSession.Net.Script(new ScriptRequest
		{
			operation = ScriptOperation.GetRebirthSettings,
			characterId = GameSession.CharacterId
		}).value;
	}

	private static int RebornCount()
	{
		if (!JsConvert.ToBoolean(RebirthSettings().Items[0]))
		{
			return 0;
		}
		return GameSession.Character?.record.reborns ?? 0;
	}

	private static void Rebirth(JsCall call)
	{
		if (!GameSession.InGame)
		{
			throw new JsRuntimeException("Rebirth requires an active character");
		}
		if (JsConvert.ToBoolean(call.Arg(0)) || JsConvert.ToBoolean(call.Arg(1)))
		{
			throw new JsRuntimeException("Unmigrated rebirth skill-reset flags");
		}
		JsArray jsArray = RebirthSettings();
		if (!JsConvert.ToBoolean(jsArray.Items[0]))
		{
			throw new JsRuntimeException("重生系统未启用");
		}
		int num = Math.Min(JsConvert.ToInt32(GameSession.Net.Script(new ScriptRequest
		{
			operation = ScriptOperation.GetMaxClassLevel,
			characterId = GameSession.CharacterId
		}).value), JsConvert.ToInt32(jsArray.Items[1]));
		bool completedCygnusFifth = NativeRebirthRules.IsCygnusFifth(Profile.JobId) || ScriptValueStore("cygnus_fifth_job_completed", null) == "1";
		int jobId = NativeRebirthRules.JobAtLevel(Profile.JobId, num, Int(call, 2), completedCygnusFifth);
		using (Profile.DeferSaves())
		{
			if (!JobSystem.ChangeJobById(jobId))
			{
				throw new JsRuntimeException("Unknown rebirth job: " + jobId);
			}
			GameSession.Character.record.reborns++;
			LevelSystem.ResetForRebirth(Profile.Data, num);
			Profile.Save();
		}
		Pc?.ApplyProgression();
	}

	private static void BindCommonPlayer(JsHostObject p, NpcSession session)
	{
		p.Set("getStr", Ret((JsCall _) => N(Profile.Data.attributes.str)));
		p.Set("getDex", Ret((JsCall _) => N(Profile.Data.attributes.dex)));
		p.Set("getInt", Ret((JsCall _) => N(Profile.Data.attributes.intelligence)));
		p.Set("getLuk", Ret((JsCall _) => N(Profile.Data.attributes.luk)));
		p.Set("getWatk", Ret((JsCall _) => N(PlayerCombat().watk)));
		p.Set("getMatk", Ret((JsCall _) => N(PlayerCombat().matk)));
		p.Set("getWdef", Ret((JsCall _) => N(PlayerCombat().weaponDefense)));
		p.Set("getMdef", Ret((JsCall _) => N(PlayerCombat().magicDefense)));
		p.Set("setStr", Fn((JsCall c) =>
		{
			SetBaseStat(Int(c, 0));
		}));
		p.Set("setDex", Fn((JsCall c) =>
		{
			SetBaseStat(-1, Int(c, 0));
		}));
		p.Set("setInt", Fn((JsCall c) =>
		{
			SetBaseStat(-1, -1, Int(c, 0));
		}));
		p.Set("setLuk", Fn((JsCall c) =>
		{
			SetBaseStat(-1, -1, -1, Int(c, 0));
		}));
		p.Set("getCurrentMaxHp", Ret((JsCall _) => N(((Object)(object)Pc != (Object)null) ? Pc.hpMax : ((float)Profile.Data.baseMaxHp))));
		p.Set("getCurrentMaxMp", Ret((JsCall _) => N(((Object)(object)Pc != (Object)null) ? Pc.mpMax : ((float)Profile.Data.baseMaxMp))));
		p.Set("getClientMaxHp", Ret((JsCall _) => N(Profile.Data.baseMaxHp)));
		p.Set("getClientMaxMp", Ret((JsCall _) => N(Profile.Data.baseMaxMp)));
		p.Set("addMaxHP", Fn((JsCall c) =>
		{
			AdjustMaxHpMp(Int(c, 0), 0);
		}));
		p.Set("addMaxMP", Fn((JsCall c) =>
		{
			AdjustMaxHpMp(0, Int(c, 0));
		}));
		p.Set("updateHpMaxHp", Fn((JsCall c) =>
		{
			AdjustMaxHpMp(Int(c, 1) - BaseMaxHp(), 0);
			if ((Object)(object)Pc != (Object)null)
			{
				Pc.hp = Int(c, 0);
			}
		}));
		p.Set("updateMpMaxMp", Fn((JsCall c) =>
		{
			AdjustMaxHpMp(0, Int(c, 1) - BaseMaxMp());
			if ((Object)(object)Pc != (Object)null)
			{
				Pc.mp = Int(c, 0);
			}
		}));
		p.Set("updateMaxHpMaxMp", Fn((JsCall c) =>
		{
			AdjustMaxHpMp(Int(c, 0) - BaseMaxHp(), Int(c, 1) - BaseMaxMp());
		}));
		p.Set("setRemainingAp", Fn((JsCall c) =>
		{
			Profile.AddAp(Int(c, 0) - Profile.ap);
		}));
		p.Set("gainSp", Fn((JsCall c) =>
		{
			Profile.AddSp(Int(c, 0));
		}));
		p.Set("setLevel", Fn((JsCall c) =>
		{
			int num = Int(c, 0);
			if (num > 0 && num != Profile.level)
			{
				LevelSystem.SetLevel(Profile.Data, num);
				Profile.Save();
				Pc?.ApplyProgression();
			}
		}));
		p.Set("levelUp", Fn((JsCall _) =>
		{
			LevelSystem.SetLevel(Profile.Data, Profile.level + 1);
			Profile.Save();
			Pc?.ApplyProgression();
		}));
		p.Set("changeJob", Fn((JsCall c) =>
		{
			ChangeJob(Int(c, 0));
		}));
		p.Set("getReborns", Ret((JsCall _) => N(RebornCount())));
		p.Set("getRebornsCount", Ret((JsCall _) => N(RebornCount())));
		p.Set("rebirth", Fn((JsCall c) =>
		{
			Rebirth(c);
		}));
		p.Set("executeRebornAsId", Fn((JsCall c) =>
		{
			int jobId = Int(c, 0);
			if (GameSession.Character == null)
			{
				throw new JsRuntimeException("Rebirth requires an active character");
			}
			GameSession.Character.record.reborns++;
			ChangeJob(jobId);
			Profile.Data.level = 1;
			Profile.Data.experience = 0L;
			LevelSystem.EnsureBaseline(Profile.Data);
			Profile.Save();
			Pc?.ApplyProgression();
		}));
		p.Set("getSkillLevel", Ret((JsCall c) => N(SkillProgress.LevelOf(SkillIdOf(c, 0)))));
		p.Set("getSkills", Ret((JsCall _) =>
		{
			JsArray jsArray = new JsArray();
			foreach (KeyValuePair<int, int> kv in SkillProgress.AllLearned())
			{
				JsHostObject jsHostObject = new JsHostObject(kv.Key);
				jsHostObject.Set("id", N(kv.Key));
				jsHostObject.Set("getId", Ret((JsCall jsCall) => N(kv.Key)));
				jsHostObject.Set("getLevel", Ret((JsCall jsCall) => N(SkillProgress.LevelOf(kv.Key))));
				jsArray.Items.Add(jsHostObject);
			}
			return jsArray;
		}));
		p.Set("changeSkillLevel", Fn((JsCall c) =>
		{
			int num = SkillIdOf(c, 0);
			if (num > 0)
			{
				SetScriptSkill(num, Int(c, 1), Int(c, 2), (c.ArgCount > 3) ? ((long)JsConvert.ToNumber(c.Arg(3))) : (-1));
			}
		}));
		p.Set("removeBySkillId", Ret((JsCall c) =>
		{
			int id = Int(c, 0);
			List<KeymapEntry> list = KeyMap.ExportCharacter();
			bool flag = list.RemoveAll((KeymapEntry e) => e.type == 1 && e.action == id) > 0;
			if (flag)
			{
				KeyMap.LoadCharacter(list);
				GameSession.SaveToDb();
			}
			return B(flag);
		}));
		p.Set("addSkillToKeyboard", Fn((JsCall c) =>
		{
			int key = Int(c, 0);
			int action = Int(c, 1);
			List<KeymapEntry> list = KeyMap.ExportCharacter();
			list.RemoveAll((KeymapEntry e) => e.key == key);
			list.Add(new KeymapEntry
			{
				key = key,
				type = 1,
				action = action
			});
			KeyMap.LoadCharacter(list);
			GameSession.SaveToDb();
		}));
		p.Set("gainEquip", Fn((JsCall c) =>
		{
			Profile.AddItem(Int(c, 0), Math.Max(1, Int(c, 1, 1)));
		}));
		p.Set("equipChanged", Fn((JsCall c) =>
		{
			Pc?.ApplyProgression();
		}));
		p.Set("forceUpdateItem", Fn((JsCall c) =>
		{
			FlushEquip();
		}));
		p.Set("setMarriageItemId", Fn((JsCall c) =>
		{
			ScriptValueStore("marriageItemId", Int(c, 0).ToString(CultureInfo.InvariantCulture), write: true);
		}));
		p.Set("getLastBanishData", Ret((JsCall _) =>
		{
			JsHostObject jsHostObject = new JsHostObject(null);
			int map = PqExtendInt("banishMap");
			int portal = PqExtendInt("banishSp");
			jsHostObject.Set("left", N(map));
			jsHostObject.Set("getLeft", Ret((JsCall jsCall) => N(map)));
			jsHostObject.Set("right", N(portal));
			jsHostObject.Set("getRight", Ret((JsCall jsCall) => N(portal)));
			return jsHostObject;
		}));
		p.Set("canRecoverLastBanish", Ret((JsCall _) => B(PqExtendInt("banishMap") >= 0)));
		p.Set("clearBanishPlayerData", Fn((JsCall _) =>
		{
			SavePqExtend("banishMap", -1);
			SavePqExtend("banishSp", -1);
		}));
		p.Set("getExpRate", Ret((JsCall _) => N(Rates().expRate)));
		p.Set("getDropRate", Ret((JsCall _) => N(Rates().dropRate)));
		p.Set("getFamilyDrop", Ret((JsCall _) => N(Rates().familyDropRate)));
		p.Set("getWorldServer", Ret((JsCall _) =>
		{
			JsHostObject jsHostObject = new JsHostObject(null);
			Lenient(jsHostObject, "worldServer");
			jsHostObject.Set("getExpRate", Ret((JsCall jsCall) => N(Rates().expRate)));
			jsHostObject.Set("broadcastPacket", Fn((JsCall c) =>
			{
				for (int num = c.ArgCount - 1; num >= 0; num--)
				{
					if (c.Arg(num) is JsString jsString && !string.IsNullOrEmpty(jsString.Value))
					{
						NativeSystemMessage.Drop(6, jsString.Value);
						break;
					}
				}
			}));
			return jsHostObject;
		}));
		p.Set("hasLevel30Character", Ret((JsCall _) => B(v: false)));
		p.Set("getSpace", Ret((JsCall _) => N(24.0)));
		p.Set("getDisconnected", Ret((JsCall _) => B(v: false)));
	}

	private static Totals PlayerCombat()
	{
		totalsCache = PlayerTotals();
		return totalsCache;
	}

	private static int BaseMaxHp()
	{
		return Profile.Data.baseMaxHp;
	}

	private static int BaseMaxMp()
	{
		return Profile.Data.baseMaxMp;
	}

	private static void SetBaseStat(int str = -1, int dex = -1, int intl = -1, int luk = -1)
	{
		CharacterAttributes attributes = Profile.Data.attributes;
		Profile.SetBaseStats((str >= 0) ? str : attributes.str, (dex >= 0) ? dex : attributes.dex, (intl >= 0) ? intl : attributes.intelligence, (luk >= 0) ? luk : attributes.luk, Profile.Data.ap, Profile.Data.sp);
		Pc?.ApplyProgression();
	}

	private static void AdjustMaxHpMp(int deltaHp, int deltaMp)
	{
		if (deltaHp != 0 || deltaMp != 0)
		{
			Profile.Data.baseMaxHp = Math.Max(50, BaseMaxHp() + deltaHp);
			Profile.Data.baseMaxMp = Math.Max(5, BaseMaxMp() + deltaMp);
			Profile.Save();
			Pc?.ApplyProgression();
			if ((Object)(object)Pc != (Object)null)
			{
				Pc.hp = Mathf.Clamp(Pc.hp, 0f, (float)Mathf.RoundToInt(Pc.hpMax));
				Pc.mp = Mathf.Clamp(Pc.mp, 0f, (float)Mathf.RoundToInt(Pc.mpMax));
			}
		}
	}

	private static int PqExtendInt(string key)
	{
		if (!int.TryParse(ScriptValueStore(key, null), out var result))
		{
			return 0;
		}
		return result;
	}

	private static void SetScriptSkill(int id, int level, int master, long expiration)
	{
		if (id <= 0 || SkillBook.Get(id) == null)
		{
			return;
		}
		List<SkillEntry> list = GameSession.Character?.skills;
		if (list != null)
		{
			SkillEntry skillEntry = list.Find((SkillEntry s) => s.skillId == id);
			if (skillEntry == null)
			{
				skillEntry = new SkillEntry
				{
					skillId = id
				};
				list.Add(skillEntry);
			}
			skillEntry.masterLevel = master;
			skillEntry.expiration = expiration;
		}
		SkillProgress.SetLevel(id, Math.Max(0, level));
		GameSession.SaveToDb();
	}

	private static int SkillIdOf(JsCall c, int i)
	{
		if (c.Arg(i) is JsHostObject jsHostObject)
		{
			JsValue jsValue = jsHostObject.Get("id");
			if (jsValue is JsNumber)
			{
				return JsConvert.ToInt32(jsValue);
			}
		}
		return Int(c, i);
	}

	private static CommunityResponse Rates()
	{
		if (!GameSession.InGame)
		{
			return new CommunityResponse();
		}
		return GameSession.Net.Community(new CommunityRequest
		{
			characterId = GameSession.CharacterId,
			operation = CommunityOperation.Read
		});
	}

	private static JsArray CharactersOnScriptMap(int mapId)
	{
		JsArray jsArray = new JsArray();
		if (GameSession.InGame && MapId == mapId)
		{
			jsArray.Items.Add(BuildPlayer(NpcSession.MapContext()));
		}
		foreach (BotCharacter actor in BotRegistry.OnMap(mapId))
		{
			if (actor.online && actor.world == GameSession.World && actor.channel == GameSession.Channel)
			{
				JsHostObject jsHostObject = new JsHostObject(actor);
				Lenient(jsHostObject, "mapCharacter");
				jsHostObject.Set("getId", Ret((JsCall _) => N(actor.id)));
				jsHostObject.Set("getName", Ret((JsCall _) => S(actor.name)));
				jsHostObject.Set("getLevel", Ret((JsCall _) => N(actor.level)));
				jsHostObject.Set("getMapId", Ret((JsCall _) => N(actor.mapId)));
				jsArray.Items.Add(jsHostObject);
			}
		}
		return jsArray;
	}

	internal static void ChangeFame(ScriptOperation operation, int delta = 0)
	{
		CharRecord charRecord = GameSession.Character?.record;
		if (charRecord != null)
		{
			ScriptRequest scriptRequest = new ScriptRequest
			{
				characterId = GameSession.CharacterId,
				operation = operation,
				variableValue = delta
			};
			if (!(((GameSession.Net != null) ? GameSession.Net.Script(scriptRequest) : ((GameSession.CharacterId > 0 && GameServer.Local != null) ? GameServer.Local.Scripts.Handle(scriptRequest) : null))?.value is JsArray jsArray))
			{
				throw new JsRuntimeException("Missing fame response");
			}
			charRecord.fame = JsConvert.ToInt32(jsArray.GetIndex(0));
			charRecord.fquest = JsConvert.ToInt32(jsArray.GetIndex(1));
		}
	}

	internal static JsValue BuildWarpMap(int id)
	{
		FieldCatalog.Ensure();
		if (!FieldCatalog.Maps.ContainsKey(id))
		{
			return JsNull.Value;
		}
		if (id == MapId)
		{
			return BuildMap();
		}
		JsHostObject jsHostObject = new JsHostObject(id);
		Lenient(jsHostObject, "map." + id);
		jsHostObject.Set("getId", Ret((JsCall _) => N(id)));
		jsHostObject.Set("getMapId", Ret((JsCall _) => N(id)));
		jsHostObject.Set("getFieldLimit", Ret((JsCall _) => N(FieldCatalog.GetMap(id).fieldLimit)));
		jsHostObject.Set("getMapName", Ret((JsCall _) => S(WzNames.Map(id))));
		jsHostObject.Set("getCharacters", Ret((JsCall _) => CharactersOnScriptMap(id)));
		jsHostObject.Set("getAllCharacters", jsHostObject.Get("getCharacters"));
		jsHostObject.Set("getCharactersSize", Ret((JsCall _) => N(CharactersOnScriptMap(id).Items.Count)));
		jsHostObject.Set("countPlayers", jsHostObject.Get("getCharactersSize"));
		jsHostObject.Set("getMonsterById", Ret((JsCall c) => (!FieldDirector.HasWildBoss(id, Int(c, 0))) ? JsNull.Value : RemoteMonster(Int(c, 0))));
		jsHostObject.Set("getMobs", Ret((JsCall _) => new JsArray()));
		jsHostObject.Set("getMonsters", jsHostObject.Get("getMobs"));
		jsHostObject.Set("getAllMonsters", jsHostObject.Get("getMobs"));
		jsHostObject.Set("countMonsters", Ret((JsCall _) => N(0.0)));
		jsHostObject.Set("getSpawnedMonstersOnMap", jsHostObject.Get("countMonsters"));
		jsHostObject.Set("spawnMonsterOnGroundBelow", Fn((JsCall c) =>
		{
			SpawnBossInto(id, c);
		}));
		jsHostObject.Set("spawnMonster", jsHostObject.Get("spawnMonsterOnGroundBelow"));
		jsHostObject.Set("getNPCById", Ret((JsCall c) => ScriptNpcView(id, Int(c, 0))));
		jsHostObject.Set("getNpcById", jsHostObject.Get("getNPCById"));
		jsHostObject.Set("containsNPC", Ret((JsCall c) => B(MapHasNpc(id, Int(c, 0)))));
		jsHostObject.Set("addMonsterSpawn", Fn((JsCall c) =>
		{
			if (!(c.Arg(0) is JsHostObject jsHostObject2))
			{
				throw new JsRuntimeException("addMonsterSpawn requires a monster");
			}
			if (!(jsHostObject2.Get("position") is JsHostObject jsHostObject3))
			{
				throw new JsRuntimeException("addMonsterSpawn requires setPosition");
			}
			FieldDirector.SpawnWildBoss(id, JsConvert.ToInt32(jsHostObject2.Get("id")), (float)JsConvert.ToNumber(jsHostObject3.Get("x")), (float)JsConvert.ToNumber(jsHostObject3.Get("y")));
		}));
		jsHostObject.Set("broadcastMessage", Fn((JsCall c) =>
		{
			if (id == MapId)
			{
				ApplyScriptPacket(c.Arg(c.ArgCount - 1));
			}
		}));
		jsHostObject.Set("getPortal", Ret((JsCall c) =>
		{
			List<FieldPortal> portals = FieldCatalog.GetMap(id).portals;
			JsValue jsValue = c.Arg(0);
			JsString name = jsValue as JsString;
			int index = ((name != null) ? portals.FindIndex((FieldPortal p) => p.name == JsConvert.ToString(name)) : Int(c, 0));
			if (index >= 0 && index < portals.Count)
			{
				FieldPortal portal = portals[index];
				JsHostObject jsHostObject2 = new JsHostObject(portal);
				Lenient(jsHostObject2, "portal");
				jsHostObject2.Set("getId", Ret((JsCall _) => N(index)));
				jsHostObject2.Set("getName", Ret((JsCall _) => S(portal.name)));
				jsHostObject2.Set("getPosition", Ret((JsCall _) => BuildPoint(portal.x, portal.y)));
				return jsHostObject2;
			}
			return JsNull.Value;
		}));
		return jsHostObject;
	}

	private static (int mapId, int npcId) RemoveScriptTarget(JsCall c)
	{
		if (c.ArgCount < 2)
		{
			return (mapId: MapId, npcId: Int(c, 0));
		}
		return (mapId: Int(c, 0), npcId: Int(c, 1));
	}

	private static JsValue RemoveScriptNpc(int mapId, int npcId)
	{
		MapRuntime mapRuntime = MapService.Get(mapId);
		if (mapRuntime == null)
		{
			return B(v: false);
		}
		bool v = mapRuntime.RemoveNpc(npcId);
		if (mapId == MapId)
		{
			FieldDirector fieldDirector = GameLoop.I?.field;
			if (fieldDirector?.Map != null && fieldDirector.Map.id == mapId)
			{
				fieldDirector.Map.npcs.RemoveAll((FieldNpc n) => n.id == npcId);
				fieldDirector.RemoveNpcView(npcId);
			}
		}
		return B(v);
	}

	private static void SpawnScriptNpc(JsCall c)
	{
		int id = Int(c, 0);
		int num = 0;
		int num2 = 0;
		if (c.Arg(1) is JsHostObject jsHostObject)
		{
			num = JsConvert.ToInt32(jsHostObject.Get("x"));
			num2 = JsConvert.ToInt32(jsHostObject.Get("y"));
		}
		else if (c.ArgCount > 2)
		{
			num = Int(c, 1);
			num2 = Int(c, 2);
		}
		MapRuntime mapRuntime = MapService.Get(MapId);
		if (mapRuntime == null)
		{
			mapRuntime = new MapRuntime
			{
				mapId = MapId
			};
			MapService.Register(mapRuntime);
		}
		float num3 = num2;
		float? num4 = mapRuntime.PointBelow(num, (float)num2 - 12f) ?? mapRuntime.PointBelow(num, float.NegativeInfinity);
		if (num4.HasValue)
		{
			num3 = num4.Value;
		}
		mapRuntime.npcs.Add(new MapNpcPosition
		{
			id = id,
			x = num,
			y = Mathf.RoundToInt(num3),
			dynamic = true
		});
	}

	private static JsValue ScriptNpcView(int mapId, int npcId)
	{
		MapNpcPosition npc = null;
		FieldDirector fieldDirector = GameLoop.I?.field;
		if (mapId == MapId && fieldDirector?.Map != null)
		{
			foreach (FieldNpc npc2 in fieldDirector.Map.npcs)
			{
				if (npc2.id == npcId)
				{
					npc = new MapNpcPosition
					{
						id = npc2.id,
						x = Mathf.RoundToInt(npc2.x),
						y = Mathf.RoundToInt(npc2.y)
					};
					break;
				}
			}
		}
		if (npc == null)
		{
			MapRuntime mapRuntime = MapService.Get(mapId);
			if (mapRuntime != null && !mapRuntime.removedNpcs.Contains(npcId))
			{
				npc = mapRuntime.GetNpc(npcId);
			}
		}
		if (npc == null)
		{
			return JsNull.Value;
		}
		JsHostObject jsHostObject = new JsHostObject(npc);
		Lenient(jsHostObject, "npc");
		jsHostObject.Set("getId", Ret((JsCall _) => N(npc.id)));
		jsHostObject.Set("getObjectId", Ret((JsCall _) => N(npc.id)));
		jsHostObject.Set("getPosition", Ret((JsCall _) => BuildPoint(npc.x, npc.y)));
		return jsHostObject;
	}

	private static bool MapHasNpc(int mapId, int npcId)
	{
		FieldDirector fieldDirector = GameLoop.I?.field;
		if (mapId == MapId && fieldDirector?.Map != null)
		{
			return fieldDirector.Map.npcs.Exists((FieldNpc n) => n.id == npcId);
		}
		MapRuntime mapRuntime = MapService.Get(mapId);
		if (mapRuntime != null && !mapRuntime.removedNpcs.Contains(npcId))
		{
			return mapRuntime.GetNpc(npcId) != null;
		}
		return false;
	}

	private static JsValue RemoteMonster(int mobId)
	{
		JsHostObject jsHostObject = new JsHostObject(null);
		Lenient(jsHostObject, "monster");
		jsHostObject.Set("id", N(mobId));
		jsHostObject.Set("getId", Ret((JsCall _) => N(mobId)));
		jsHostObject.Set("getName", Ret((JsCall _) => S(WzNames.Mob(mobId))));
		return jsHostObject;
	}

	private static void SpawnBossInto(int mapId, JsCall c)
	{
		int mobId = ((c.Arg(0) is JsHostObject jsHostObject && jsHostObject.Get("id") is JsNumber) ? JsConvert.ToInt32(jsHostObject.Get("id")) : Int(c, 0));
		int num;
		int num2;
		if (c.Arg(1) is JsHostObject jsHostObject2)
		{
			num = JsConvert.ToInt32(jsHostObject2.Get("x"));
			num2 = JsConvert.ToInt32(jsHostObject2.Get("y"));
		}
		else
		{
			num = Int(c, 1);
			num2 = Int(c, 2);
		}
		FieldDirector.SpawnWildBoss(mapId, mobId, num, num2);
	}

	private static int eventMapPortalIndex(JsValue value, MapPortal portal)
	{
		if (!((value as JsHostObject)?.Host is NativeEventMap nativeEventMap))
		{
			return -1;
		}
		return nativeEventMap.Geometry.portals.IndexOf(portal);
	}

	private static JsHostObject BuildMap()
	{
		NativeEventMap nativeEventMap = NativeEventScripts.ResolveMap(MapId);
		if (nativeEventMap != null)
		{
			return nativeEventMap.Host;
		}
		JsHostObject jsHostObject = new JsHostObject(null);
		Lenient(jsHostObject, "map");
		jsHostObject.Set("broadcastMessage", Fn((JsCall c) =>
		{
			ApplyScriptPacket(c.Arg(c.ArgCount - 1));
		}));
		jsHostObject.Set("getId", Ret((JsCall _) => N(MapId)));
		jsHostObject.Set("getMapId", Ret((JsCall _) => N(MapId)));
		jsHostObject.Set("getFieldLimit", Ret((JsCall _) => N(FieldCatalog.GetMap(MapId)?.fieldLimit ?? 0)));
		jsHostObject.Set("getReturnMap", Ret((JsCall _) =>
		{
			FieldDirector fieldDirector = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.field : null);
			return N(((Object)(object)fieldDirector != (Object)null && fieldDirector.Map != null) ? fieldDirector.Map.returnMap : MapId);
		}));
		jsHostObject.Set("getCharactersSize", Ret((JsCall _) => N(CharactersOnScriptMap(MapId).Items.Count)));
		jsHostObject.Set("countPlayers", jsHostObject.Get("getCharactersSize"));
		jsHostObject.Set("getCharacters", Ret((JsCall _) => CharactersOnScriptMap(MapId)));
		jsHostObject.Set("getAllCharacters", jsHostObject.Get("getCharacters"));
		jsHostObject.Set("getPortal", Ret((JsCall c) =>
		{
			FieldMap map = FieldCatalog.GetMap(MapId);
			FieldPortal portal = null;
			int index = -1;
			if (map != null)
			{
				if (c.Arg(0) is JsString)
				{
					index = map.portals.FindIndex((FieldPortal x) => x.name == Str(c, 0));
				}
				else
				{
					index = Int(c, 0, -1);
				}
				if (index >= 0 && index < map.portals.Count)
				{
					portal = map.portals[index];
				}
			}
			if (portal != null)
			{
				JsHostObject jsHostObject2 = new JsHostObject(null);
				jsHostObject2.Set("getPosition", Ret((JsCall _) => BuildPoint(portal.x, portal.y)));
				jsHostObject2.Set("getMapId", Ret((JsCall _) => N(MapId)));
				jsHostObject2.Set("getName", Ret((JsCall _) => S(portal.name)));
				jsHostObject2.Set("getId", Ret((JsCall _) => N(index)));
				return jsHostObject2;
			}
			return JsNull.Value;
		}));
		jsHostObject.Set("getMobs", Ret((JsCall _) =>
		{
			JsArray jsArray = new JsArray();
			foreach (EnemyUnit item in EnemyUnit.All)
			{
				if (item.fieldMode && !item.dead)
				{
					jsArray.Items.Add(BuildMonster(item));
				}
			}
			return BuildJavaList(jsArray);
		}));
		jsHostObject.Set("getMonsters", jsHostObject.Get("getMobs"));
		jsHostObject.Set("getAllMonsters", jsHostObject.Get("getMobs"));
		jsHostObject.Set("countMonster", Ret((JsCall c) =>
		{
			int num = Int(c, 0);
			int num2 = ((c.ArgCount > 1) ? Int(c, 1) : num);
			int num3 = 0;
			foreach (EnemyUnit item2 in EnemyUnit.All)
			{
				if (item2.fieldMode && !item2.dead && item2.fieldMobId >= num && item2.fieldMobId <= num2)
				{
					num3++;
				}
			}
			return N(num3);
		}));
		jsHostObject.Set("isCPQWinnerMap", Ret((JsCall _) => B(MapId == 980031300 || MapId == 980032300 || MapId == 980033300 || (MapId >= 980000103 && MapId <= 980000603 && (MapId - 980000103) % 100 == 0))));
		jsHostObject.Set("isCPQLoserMap", Ret((JsCall _) => B(MapId == 980031400 || MapId == 980032400 || MapId == 980033400 || (MapId >= 980000104 && MapId <= 980000604 && (MapId - 980000104) % 100 == 0))));
		jsHostObject.Set("getMapName", Ret((JsCall _) => S(WzNames.Map(MapId))));
		jsHostObject.Set("countMonsters", Ret((JsCall _) => N(CountFieldMonsters())));
		jsHostObject.Set("getSpawnedMonstersOnMap", Ret((JsCall _) => N(CountFieldMonsters())));
		jsHostObject.Set("spawnMonsterOnGroundBelow", Fn((JsCall c) =>
		{
			SpawnBossInto(MapId, c);
		}));
		jsHostObject.Set("addMonsterSpawn", Fn((JsCall c) =>
		{
			if (!(c.Arg(0) is JsHostObject jsHostObject2))
			{
				throw new JsRuntimeException("addMonsterSpawn requires a monster");
			}
			if (!(jsHostObject2.Get("position") is JsHostObject jsHostObject3))
			{
				throw new JsRuntimeException("addMonsterSpawn requires setPosition");
			}
			GameLoop.I?.field?.AddScriptMonsterSpawn(JsConvert.ToInt32(jsHostObject2.Get("id")), JsConvert.ToInt32(jsHostObject3.Get("x")), JsConvert.ToInt32(jsHostObject3.Get("y")), Int(c, 1), Int(c, 2, -1));
		}));
		jsHostObject.Set("killAllMonsters", Fn((JsCall c) =>
		{
			GameLoop.I?.field?.ClearMobs();
		}));
		jsHostObject.Set("getReactorByName", Ret((JsCall c) => Reactor((MapReactor reactor) => reactor.name == Str(c, 0))));
		jsHostObject.Set("getReactorById", Ret((JsCall c) => Reactor((MapReactor reactor) => reactor.id == Int(c, 0))));
		jsHostObject.Set("getReactorByOid", Ret((JsCall c) => Reactor((MapReactor reactor) => reactor.objectId == Int(c, 0))));
		jsHostObject.Set("getMonsterById", Ret((JsCall c) =>
		{
			foreach (EnemyUnit item3 in EnemyUnit.All)
			{
				if (item3.fieldMode && !item3.dead && item3.fieldMobId == Int(c, 0))
				{
					return BuildMonster(item3);
				}
			}
			return JsNull.Value;
		}));
		jsHostObject.Set("getNPCById", Ret((JsCall c) => ScriptNpcView(MapId, Int(c, 0))));
		jsHostObject.Set("getNpcById", jsHostObject.Get("getNPCById"));
		jsHostObject.Set("containsNPC", Ret((JsCall c) => B(MapHasNpc(MapId, Int(c, 0)))));
		return jsHostObject;
		static JsValue Reactor(Func<MapReactor, bool> matches)
		{
			foreach (MapReactor reactor in MapReactorService.Get(MapId))
			{
				if (reactor.alive && matches(reactor))
				{
					JsHostObject jsHostObject2 = new JsHostObject(reactor);
					Lenient(jsHostObject2, "reactor");
					jsHostObject2.Set("getId", Ret((JsCall c) => N(reactor.id)));
					jsHostObject2.Set("getObjectId", Ret((JsCall c) => N(reactor.objectId)));
					jsHostObject2.Set("getName", Ret((JsCall c) => S(reactor.name)));
					jsHostObject2.Set("getState", Ret((JsCall c) => N(reactor.state)));
					return jsHostObject2;
				}
			}
			return JsNull.Value;
		}
	}

	internal static void ApplyScriptPacket(JsValue packet)
	{
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		if (packet is JsHostObject { Host: ScriptSound host })
		{
			if (host.music)
			{
				AudioManager.PlayBgm(host.path);
			}
			else
			{
				AudioManager.Play(host.path, 1f, pitchVary: false);
			}
		}
		else if (packet is JsHostObject { Host: ScriptNotice host2 })
		{
			NativeSystemMessage.Drop(host2.type, host2.text);
		}
		else if (packet is JsHostObject { Host: ScriptEffect host3 })
		{
			NativeScriptEffect.Play(host3.path);
		}
		else if (packet is JsHostObject { Host: ScriptDamage host4 })
		{
			PlayerCtrl pc = Pc;
			if ((Object)(object)pc != (Object)null)
			{
				DamageText.Show(Vector2.op_Implicit(pc.FeetPos) + Vector3.up * 0.6f, host4.damage);
			}
		}
		else
		{
			if (packet is JsHostObject jsHostObject5 && jsHostObject5.Host is ScriptMovePlayer)
			{
				return;
			}
			if (packet is JsHostObject jsHostObject6 && jsHostObject6.Host is ScriptRpsOpen)
			{
				NativeRpsWindow.Open();
				return;
			}
			if (packet is JsHostObject { Host: ScriptClock host5 })
			{
				NativeEventClock.Show(host5.seconds);
				return;
			}
			if (packet is JsHostObject { Host: ScriptProgressMessage host6 })
			{
				BottomBar.AddChat(host6.text, BottomBar.ChatLightBlue);
				return;
			}
			if (!(packet is JsHostObject jsHostObject9) || !(jsHostObject9.Host is ScriptInventoryPacket))
			{
				throw new JsRuntimeException("Unmigrated script packet effect");
			}
			FlushEquip();
			GameSession.ApplyToProfile();
		}
	}

	internal static string DescribeScriptPacket(JsValue packet)
	{
		JsHostObject jsHostObject = packet as JsHostObject;
		if (jsHostObject?.Host is ScriptEffect scriptEffect)
		{
			return "effect:" + scriptEffect.path;
		}
		if (jsHostObject?.Host is ScriptNotice scriptNotice)
		{
			return "notice:" + scriptNotice.text;
		}
		if (jsHostObject?.Host is ScriptDamage scriptDamage)
		{
			return "damage:" + scriptDamage.damage;
		}
		return null;
	}

	private static int CountFieldMonsters()
	{
		int num = 0;
		foreach (EnemyUnit item in EnemyUnit.All)
		{
			if (item.fieldMode && !item.dead)
			{
				num++;
			}
		}
		return num;
	}

	private static void SpawnScriptMob(int id, int x, int y, bool ground = false)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		GameLoop.I?.field?.SpawnNativeMob(id, new Vector3((float)x / 40f, (float)(-y) / 40f, 0f), ground);
	}

	public static JsHostObject BuildJava()
	{
		JsHostObject jsHostObject = new JsHostObject(null);
		jsHostObject.Set("type", Ret((JsCall c) =>
		{
			string text = Str(c, 0);
			if (text == "org.gms.client.inventory.ModifyInventory")
			{
				return JsFunction.FromNative("ModifyInventory", (JsCall args) =>
				{
					if (args.ArgCount < 2 || args.ArgCount > 3 || !((args.Arg(1) as JsHostObject)?.Host is InventoryEntry inventoryEntry))
					{
						throw new JsRuntimeException("ModifyInventory requires an inventory item");
					}
					ScriptInventoryUpdate update = new ScriptInventoryUpdate
					{
						mode = Int(args, 0),
						item = inventoryEntry.Copy(),
						oldPosition = (short)Int(args, 2)
					};
					JsHostObject jsHostObject31 = new JsHostObject(update);
					Lenient(jsHostObject31, "ModifyInventory");
					jsHostObject31.Set("getMode", Ret((JsCall _) => N(update.mode)));
					jsHostObject31.Set("getInventoryType", Ret((JsCall _) => N(update.item.itemId / 1000000)));
					jsHostObject31.Set("getPosition", Ret((JsCall _) => N(update.item.position)));
					jsHostObject31.Set("getOldPosition", Ret((JsCall _) => N(update.oldPosition)));
					jsHostObject31.Set("getQuantity", Ret((JsCall _) => N(update.item.isEquip ? 1 : update.item.quantity)));
					jsHostObject31.Set("getItem", Ret((JsCall _) => (update.item != null) ? BuildItemView(update.item, live: false) : JsNull.Value));
					jsHostObject31.Set("clear", Fn((JsCall _) =>
					{
						update.item = null;
					}));
					return jsHostObject31;
				});
			}
			if (text == "java.util.Collections")
			{
				JsHostObject jsHostObject2 = new JsHostObject(null);
				Lenient(jsHostObject2, "Collections");
				jsHostObject2.Set("unmodifiableList", Ret((JsCall args) =>
				{
					JsHostObject jsHostObject31 = BuildJavaList((args.Arg(0) as JsArray) ?? throw new JsRuntimeException("unmodifiableList requires a list"));
					string[] array4 = new string[7] { "add", "set", "remove", "clear", "addAll", "removeAll", "retainAll" };
					foreach (string key in array4)
					{
						jsHostObject31.Set(key, Ret((JsCall _) =>
						{
							throw new JsRuntimeException("UnsupportedOperationException: unmodifiable list");
						}));
					}
					return jsHostObject31;
				}));
				return jsHostObject2;
			}
			if (text == "soloMapling.ArtificialPlayer.BotTypes.Blackjack.BlackjackDealerBot")
			{
				JsHostObject jsHostObject3 = new JsHostObject(null);
				Lenient(jsHostObject3, "BlackjackDealerBot");
				jsHostObject3.Set("joinNearestTable", Ret((JsCall args) =>
				{
					JsValue jsValue = args.Arg(0);
					int num3 = JsConvert.ToInt32(args.Interp.Call(args.Interp.GetMember(jsValue, "getId"), jsValue, new JsValue[0]));
					if (!GameSession.InGame || num3 != GameSession.CharacterId)
					{
						throw new JsRuntimeException("Blackjack player is not the active character");
					}
					BotActor botActor = GameServer.Local?.ActiveBotPlayer(num3);
					if (botActor == null || !botActor.online || botActor.mapId != MapId)
					{
						throw new JsRuntimeException("Blackjack requires the active field player");
					}
					return S(BlackjackDealerBot.JoinNearestTable(botActor));
				}));
				return jsHostObject3;
			}
			if (text == "org.gms.client.command.CommandsExecutor")
			{
				JsHostObject executor = new JsHostObject(null);
				Lenient(executor, "CommandsExecutor");
				executor.Set("getInstance", Ret((JsCall _) => executor));
				executor.Set("getCommandsNameDesc", Ret((JsCall _) =>
				{
					JsArray jsArray = new JsArray();
					List<KeyValuePair<string, string>>[] array4 = AdminCommands.ScriptCommandDescriptions(GameServer.Local.Db);
					foreach (List<KeyValuePair<string, string>> obj in array4)
					{
						JsArray names = new JsArray();
						JsArray descriptions = new JsArray();
						foreach (KeyValuePair<string, string> item in obj)
						{
							names.Items.Add(S(item.Key));
							descriptions.Items.Add(S(item.Value));
						}
						JsHostObject jsHostObject31 = new JsHostObject(null);
						Lenient(jsHostObject31, "CommandNameDescription");
						jsHostObject31.Set("getLeft", Ret((JsCall args) => BuildJavaList(names)));
						jsHostObject31.Set("getRight", Ret((JsCall args) => BuildJavaList(descriptions)));
						jsArray.Items.Add(jsHostObject31);
					}
					return BuildJavaList(jsArray);
				}));
				return executor;
			}
			if (text == "org.gms.server.ShopFactory")
			{
				JsHostObject factory = new JsHostObject(null);
				factory.Set("getInstance", Ret((JsCall _) => factory));
				factory.Set("getShop", Ret((JsCall args) =>
				{
					int id = Int(args, 0);
					JsHostObject jsHostObject31 = new JsHostObject(null);
					jsHostObject31.Set("sendShop", Fn((JsCall _) =>
					{
						NativeNpcShopWindow.OpenShop(id, fallback: false);
					}));
					jsHostObject31.Set("sell", Fn((JsCall c2) =>
					{
						NativeNpcShopWindow.SellFromScript(id, Int(c2, 1), Int(c2, 2), Int(c2, 3));
					}));
					return jsHostObject31;
				}));
				return factory;
			}
			if (text == "java.awt.Point")
			{
				return JsFunction.FromNative("Point", (JsCall args) =>
				{
					JsHostObject point = new JsHostObject(null);
					point.Set("x", N(Int(args, 0)));
					point.Set("y", N(Int(args, 1)));
					point.Set("getX", Ret((JsCall _) => point.Get("x")));
					point.Set("getY", Ret((JsCall _) => point.Get("y")));
					return point;
				});
			}
			if (text == "org.gms.server.ItemInformationProvider")
			{
				JsHostObject provider = new JsHostObject(null);
				Lenient(provider, "ItemInformationProvider");
				provider.Set("getInstance", Ret((JsCall _) => provider));
				provider.Set("getName", Ret((JsCall args) => S(WzNames.Item(Int(args, 0)))));
				provider.Set("getPrice", Ret((JsCall args) => N(GameServer.Local.NpcShops.SellPrice(Int(args, 0), Int(args, 1, 1)))));
				provider.Set("isSamePart", Ret((JsCall args) =>
				{
					string text2 = OriginalItemPart(Int(args, 0));
					return B(text2 != null && text2 == OriginalItemPart(Int(args, 1)));
				}));
				provider.Set("isCashItem", Ret((JsCall c2) =>
				{
					ImgNode imgNode = NativeItemSource.Node(Int(c2, 0));
					ImgNode obj = imgNode?.Get("info/cash") ?? imgNode?.Get("cash");
					return B(obj != null && obj.Int("") == 1);
				}));
				provider.Set("isQuestItem", Ret((JsCall c2) =>
				{
					ImgNode imgNode = NativeItemSource.Node(Int(c2, 0));
					return B(imgNode != null && imgNode.Int("info/quest") == 1);
				}));
				provider.Set("isPartyQuestItem", Ret((JsCall c2) =>
				{
					ImgNode imgNode = NativeItemSource.Node(Int(c2, 0));
					return B(imgNode != null && imgNode.Int("info/pquest") == 1);
				}));
				provider.Set("getEquipStats", Ret((JsCall args) =>
				{
					Dictionary<string, int> stats = BotItemCatalog.Shared.EquipStats(Int(args, 0));
					if (stats != null)
					{
						JsHostObject jsHostObject31 = new JsHostObject(stats);
						jsHostObject31.Set("get", Ret((JsCall c2) => (!stats.TryGetValue(Str(c2, 0), out var value)) ? ((JsValue)JsNull.Value) : ((JsValue)N(value))));
						jsHostObject31.Set("containsKey", Ret((JsCall c2) => B(stats.ContainsKey(Str(c2, 0)))));
						return jsHostObject31;
					}
					return JsNull.Value;
				}));
				provider.Set("getEquipLevelReq", Ret((JsCall args) => N(EquipCatalog.Info(Int(args, 0))?.Int("reqLevel") ?? (-1))));
				return provider;
			}
			if (text == "org.gms.server.BossDamageGrowth")
			{
				JsHostObject jsHostObject4 = new JsHostObject(null);
				Lenient(jsHostObject4, "BossDamageGrowth");
				jsHostObject4.Set("QUEST_REQUIREMENT", N(1000.0));
				jsHostObject4.Set("BOSS_KILL_REQUIREMENT", N(10.0));
				jsHostObject4.Set("MONSTER_CARD_REQUIREMENT", N(300.0));
				jsHostObject4.Set("QUEST_MAX_BONUS_PERCENT", N(30.0));
				jsHostObject4.Set("BOSS_MAX_BONUS_PERCENT", N(40.0));
				jsHostObject4.Set("MONSTER_CARD_MAX_BONUS_PERCENT", N(30.0));
				jsHostObject4.Set("MAX_BONUS_PERCENT", N(100.0));
				jsHostObject4.Set("MAX_MULTIPLIER", N(2.0));
				jsHostObject4.Set("getQuestCount", Ret((JsCall call) => N(ReadGrowth(call).questCount)));
				jsHostObject4.Set("getMonsterCardCount", Ret((JsCall call) => N(ReadGrowth(call).monsterCardCount)));
				jsHostObject4.Set("getQuestBonusPercent", Ret((JsCall call) => N(ReadGrowth(call).questBonusPercent)));
				jsHostObject4.Set("getBossBonusPercent", Ret((JsCall call) => N(ReadGrowth(call).bossBonusPercent)));
				jsHostObject4.Set("getMonsterCardBonusPercent", Ret((JsCall call) => N(ReadGrowth(call).monsterCardBonusPercent)));
				jsHostObject4.Set("getBonusPercent", Ret((JsCall call) => N(ReadGrowth(call).bonusPercent)));
				jsHostObject4.Set("getMultiplier", Ret((JsCall call) => N(ReadGrowth(call).multiplier)));
				jsHostObject4.Set("calculateQuestBonusPercent", Ret((JsCall c2) => N(Math.Min(1.0, (double)Math.Max(0, Int(c2, 0)) / 1000.0) * 30.0)));
				jsHostObject4.Set("calculateMonsterCardBonusPercent", Ret((JsCall c2) => N(Math.Min(1.0, (double)Math.Max(0, Int(c2, 0)) / 300.0) * 30.0)));
				jsHostObject4.Set("calculateBonusPercent", Ret((JsCall c2) => N(NativeBossService.Bonus(Int(c2, 0), Int(c2, 1), Int(c2, 2)))));
				jsHostObject4.Set("calculateMultiplier", Ret((JsCall c2) => N(1.0 + NativeBossService.Bonus(Int(c2, 0), Int(c2, 1), Int(c2, 2)) / 100.0)));
				jsHostObject4.Set("invalidate", Fn((JsCall c2) =>
				{
					GameServer.Local.Bosses.Invalidate(Int(c2, 0));
				}));
				return jsHostObject4;
			}
			if (text == "org.gms.server.EquipmentCubeManager")
			{
				JsHostObject jsHostObject5 = new JsHostObject(null);
				Lenient(jsHostObject5, "EquipmentCubeManager");
				jsHostObject5.Set("isCube", Ret((JsCall c2) => B(NativeEquipmentCube.IsCube(Int(c2, 0)))));
				jsHostObject5.Set("cubeName", Ret((JsCall c2) => S(NativeEquipmentCube.CubeName(Int(c2, 0)))));
				jsHostObject5.Set("cubeSummary", Ret((JsCall jsCall) => S((jsCall.ArgCount == 1) ? NativeEquipmentCube.Summary(Int(jsCall, 0)) : NativeEquipmentCube.Summary(Equip(jsCall), Int(jsCall, 1)))));
				jsHostObject5.Set("canRoll", Ret((JsCall call) => B(NativeEquipmentCube.CanRoll(Equip(call)))));
				jsHostObject5.Set("canUseCube", Ret((JsCall jsCall) => B(NativeEquipmentCube.CanUse(Equip(jsCall), Int(jsCall, 1)))));
				jsHostObject5.Set("describe", Ret((JsCall call) => S(NativeEquipmentCube.Describe(Equip(call)))));
				jsHostObject5.Set("roll", Ret((JsCall jsCall) =>
				{
					NativeEquipmentCube.Roll roll = NativeEquipmentCube.CreateRoll(Equip(jsCall), Int(jsCall, 1));
					JsHostObject jsHostObject31 = new JsHostObject(roll);
					Lenient(jsHostObject31, "cube roll");
					jsHostObject31.Set("itemId", Ret((JsCall _) => N(roll.itemId)));
					jsHostObject31.Set("cubeItemId", Ret((JsCall _) => N(roll.cubeItemId)));
					jsHostObject31.Set("grade", Ret((JsCall _) => N(roll.grade)));
					jsHostObject31.Set("data", Ret((JsCall _) => S(roll.data)));
					jsHostObject31.Set("description", Ret((JsCall _) => S(roll.description)));
					jsHostObject31.Set("rankedUp", Ret((JsCall _) => B(roll.rankedUp)));
					jsHostObject31.Set("canKeepOld", Ret((JsCall _) => B(roll.canKeepOld)));
					return jsHostObject31;
				}));
				jsHostObject5.Set("apply", Fn((JsCall call) =>
				{
					InventoryEntry inventoryEntry = Equip(call);
					NativeEquipmentCube.Apply(inventoryEntry, (call.Arg(1) as JsHostObject)?.Host as NativeEquipmentCube.Roll);
					MarkDirty(inventoryEntry);
				}));
				jsHostObject5.Set("inherit", Fn((JsCall call) =>
				{
					InventoryEntry inventoryEntry = Equip(call, 1);
					NativeEquipmentCube.Inherit(Equip(call), inventoryEntry);
					MarkDirty(inventoryEntry);
				}));
				return jsHostObject5;
			}
			if (text == "org.gms.server.maps.FieldLimit")
			{
				JsHostObject jsHostObject6 = new JsHostObject(null);
				Lenient(jsHostObject6, "FieldLimit");
				string[] array = new string[12]
				{
					"JUMP", "MOVEMENTSKILLS", "SUMMON", "DOOR", "CANNOTMIGRATE", "CANNOTVIPROCK", "CANNOTMINIGAME", "CANNOTUSEMOUNTS", "CANNOTUSEPOTION", "CANNOTJUMPDOWN",
					"NO_EXP_DECREASE", "DROP_LIMIT"
				};
				int[] array2 = new int[12]
				{
					1, 2, 4, 8, 16, 64, 128, 512, 4096, 131072,
					524288, 4194304
				};
				for (int num = 0; num < array.Length; num++)
				{
					int mask = array2[num];
					JsHostObject jsHostObject7 = new JsHostObject(null);
					Lenient(jsHostObject7, "FieldLimit." + array[num]);
					jsHostObject7.Set("getValue", Ret((JsCall _) => N(mask)));
					jsHostObject7.Set("check", Ret((JsCall c2) => B((Int(c2, 0) & mask) == mask)));
					jsHostObject6.Set(array[num], jsHostObject7);
				}
				return jsHostObject6;
			}
			if (text == "org.gms.server.life.MonsterInformationProvider")
			{
				JsHostObject provider2 = new JsHostObject(null);
				Lenient(provider2, "MonsterInformationProvider");
				provider2.Set("getInstance", Ret((JsCall _) => provider2));
				provider2.Set("getMobNameFromId", Ret((JsCall args) => S(WzNames.Mob(Int(args, 0)) ?? "")));
				provider2.Set("isBoss", Ret((JsCall c2) =>
				{
					try
					{
						int num3 = Int(c2, 0);
						FieldCatalog.LoadMob(num3);
						FieldMob value;
						return B(FieldCatalog.Mobs.TryGetValue(num3, out value) && Art.Num(value.stats, "boss") > 0f);
					}
					catch (Exception)
					{
						return B(v: false);
					}
				}));
				provider2.Set("retrieveDrop", Ret((JsCall args) =>
				{
					FieldCatalog.Ensure();
					JsArray jsArray = new JsArray();
					foreach (DropEntry item2 in DropStore.ForMob(Int(args, 0)))
					{
						JsHostObject jsHostObject31 = new JsHostObject(item2);
						Lenient(jsHostObject31, "MonsterDropEntry");
						jsHostObject31.Set("itemId", N(item2.item));
						jsHostObject31.Set("chance", N(item2.chance));
						jsHostObject31.Set("Minimum", N(item2.min));
						jsHostObject31.Set("Maximum", N(item2.max));
						jsHostObject31.Set("questid", N(item2.quest));
						jsArray.Items.Add(jsHostObject31);
					}
					JsHostObject jsHostObject32 = BuildJavaList(jsArray);
					jsArray.Set("size", jsHostObject32.Get("size"));
					jsArray.Set("get", jsHostObject32.Get("get"));
					jsArray.Set("iterator", jsHostObject32.Get("iterator"));
					return jsArray;
				}));
				return provider2;
			}
			if (text == "org.gms.util.I18nUtil" || text == "org.gms.net.server.Server" || text == "org.gms.util.PacketCreator")
			{
				JsHostObject type = new JsHostObject(null);
				Lenient(type, text);
				if (text == "org.gms.util.PacketCreator")
				{
					type.Set("modifyInventory", Ret((JsCall args) =>
					{
						JsArray obj = (((args.Arg(1) as JsHostObject)?.Host as JsArray) ?? (args.Arg(1) as JsArray)) ?? throw new JsRuntimeException("modifyInventory requires an update list");
						ScriptInventoryPacket scriptInventoryPacket = new ScriptInventoryPacket();
						foreach (JsValue item3 in obj.Items)
						{
							ScriptInventoryUpdate scriptInventoryUpdate = (item3 as JsHostObject)?.Host as ScriptInventoryUpdate;
							if (scriptInventoryUpdate?.item == null || scriptInventoryUpdate.mode < 0 || scriptInventoryUpdate.mode > 3)
							{
								throw new JsRuntimeException("Invalid inventory update");
							}
							scriptInventoryPacket.updates.Add(scriptInventoryUpdate);
						}
						return new JsHostObject(scriptInventoryPacket);
					}));
					type.Set("serverNotice", Ret((JsCall args) => new JsHostObject(new ScriptNotice
					{
						type = Int(args, 0),
						text = Str(args, (args.ArgCount < 3) ? 1 : 2)
					})));
					type.Set("musicChange", Ret((JsCall args) => new JsHostObject(new ScriptSound
					{
						path = Str(args, 0),
						music = true
					})));
					type.Set("playSound", Ret((JsCall args) => new JsHostObject(new ScriptSound
					{
						path = Str(args, 0)
					})));
					type.Set("getClock", Ret((JsCall args) => new JsHostObject(new ScriptClock
					{
						seconds = Int(args, 0)
					})));
					type.Set("earnTitleMessage", Ret((JsCall args) => new JsHostObject(new ScriptProgressMessage
					{
						text = Str(args, 0)
					})));
					type.Set("openRPSNPC", Ret((JsCall _) => new JsHostObject(new ScriptRpsOpen
					{
						npcId = 9000019
					})));
					type.Set("showEffect", Ret((JsCall args) => new JsHostObject(new ScriptEffect
					{
						path = Str(args, 0)
					})));
					type.Set("damagePlayer", Ret((JsCall args) => new JsHostObject(new ScriptDamage
					{
						damage = (long)JsConvert.ToNumber(args.Arg((args.ArgCount >= 4) ? 3 : 0)),
						mobId = Int(args, 1),
						objectId = Int(args, 9)
					})));
					type.Set("movePlayer", Ret((JsCall _) => new JsHostObject(new ScriptMovePlayer())));
				}
				if (text == "org.gms.net.server.Server")
				{
					type.Set("getInstance", Ret((JsCall _) => type));
					type.Set("broadcastMessage", Fn((JsCall args) =>
					{
						ApplyScriptPacket(args.Arg(args.ArgCount - 1));
					}));
				}
				return type;
			}
			if (text == "org.gms.constants.id.MapId")
			{
				JsHostObject jsHostObject8 = new JsHostObject(null);
				Lenient(jsHostObject8, "MapId");
				jsHostObject8.Set("isPartyDojo", Ret((JsCall args) => B(Int(args, 0) >= 925030100 && Int(args, 0) <= 925033804)));
				return jsHostObject8;
			}
			if (text == "org.gms.server.quest.Quest")
			{
				JsHostObject jsHostObject9 = new JsHostObject(null);
				Lenient(jsHostObject9, "Quest");
				jsHostObject9.Set("getInstance", Ret((JsCall args) =>
				{
					int id = Int(args, 0);
					QuestDef definition = QuestCatalog.Get(id);
					JsHostObject jsHostObject31 = new JsHostObject(id);
					Lenient(jsHostObject31, "Quest." + id);
					jsHostObject31.Set("getId", Ret((JsCall jsCall) => N(id)));
					jsHostObject31.Set("canStart", Ret((JsCall jsCall) =>
					{
						OwnPlayer(jsCall);
						return B(definition != null && QuestLogic.CanStart(definition, Int(jsCall, 1)));
					}));
					jsHostObject31.Set("canComplete", Ret((JsCall jsCall) =>
					{
						OwnPlayer(jsCall);
						return B(definition != null && QuestLogic.CanComplete(definition, Int(jsCall, 1)));
					}));
					jsHostObject31.Set("start", Fn((JsCall jsCall) =>
					{
						OwnPlayer(jsCall);
						if (definition != null && QuestLogic.CanStart(definition, Int(jsCall, 1)))
						{
							QuestLogic.Start(definition, Int(jsCall, 1), applyActions: true);
						}
					}));
					jsHostObject31.Set("complete", Fn((JsCall jsCall) =>
					{
						OwnPlayer(jsCall);
						if (definition != null && QuestLogic.CanComplete(definition, Int(jsCall, 1)))
						{
							QuestLogic.Complete(definition, Int(jsCall, 1), applyActions: true);
						}
					}));
					jsHostObject31.Set("reset", Fn((JsCall call) =>
					{
						OwnPlayer(call);
						Profile.ResetQuest(id);
					}));
					return jsHostObject31;
				}));
				return jsHostObject9;
			}
			if (text == "org.gms.server.expeditions.ExpeditionType")
			{
				JsHostObject jsHostObject10 = new JsHostObject(null);
				Lenient(jsHostObject10, "ExpeditionType");
				ExpeditionType[] all = ExpeditionTypes.All;
				for (int num2 = 0; num2 < all.Length; num2++)
				{
					ExpeditionType expeditionType = all[num2];
					JsHostObject jsHostObject11 = new JsHostObject(expeditionType);
					Lenient(jsHostObject11, "ExpeditionType." + expeditionType);
					string enumName = expeditionType.ToString();
					ExpeditionType kind = expeditionType;
					jsHostObject11.Set("name", Ret((JsCall _) => S(enumName)));
					jsHostObject11.Set("toString", Ret((JsCall _) => S(enumName)));
					jsHostObject11.Set("getMinSize", Ret((JsCall _) => N(ExpeditionTypes.MinSize(kind))));
					jsHostObject11.Set("getMaxSize", Ret((JsCall _) => N(ExpeditionTypes.MaxSize(kind))));
					jsHostObject11.Set("getMinLevel", Ret((JsCall _) => N(ExpeditionTypes.MinLevel(kind))));
					jsHostObject11.Set("getMaxLevel", Ret((JsCall _) => N(ExpeditionTypes.MaxLevel(kind))));
					jsHostObject11.Set("getRegistrationMinutes", Ret((JsCall _) => N(ExpeditionTypes.RegistrationMinutes(kind))));
					jsHostObject10.Set(enumName, jsHostObject11);
				}
				return jsHostObject10;
			}
			if (text == "org.gms.server.life.LifeFactory")
			{
				return ScriptMonster.Resolve(text);
			}
			switch (text)
			{
			case "org.gms.server.life.LucidBossCompat":
				return LucidBossCompat.Resolve(text);
			case "java.util.concurrent.ThreadLocalRandom":
			{
				JsHostObject jsHostObject13 = new JsHostObject(null);
				Lenient(jsHostObject13, "ThreadLocalRandom");
				JsHostObject current = new JsHostObject(null);
				Lenient(current, "ThreadLocalRandom.current()");
				current.Set("nextInt", Ret((JsCall args) => new JsNumber((args.ArgCount != 0) ? Random.Range(0, Math.Max(1, Int(args, 0))) : 0)));
				current.Set("nextLong", Ret((JsCall _) => new JsNumber(0.0)));
				current.Set("nextDouble", Ret((JsCall _) => new JsNumber(Random.value)));
				jsHostObject13.Set("current", Ret((JsCall _) => current));
				return jsHostObject13;
			}
			case "org.gms.server.maps.AbstractAnimatedMapObject":
			{
				JsHostObject jsHostObject17 = new JsHostObject(null);
				Lenient(jsHostObject17, "AbstractAnimatedMapObject");
				jsHostObject17.Set("IDLE_MOVEMENT_PACKET_LENGTH", N(15.0));
				return jsHostObject17;
			}
			case "org.gms.constants.game.GameConstants":
			{
				JsHostObject jsHostObject30 = new JsHostObject(null);
				Lenient(jsHostObject30, "GameConstants");
				jsHostObject30.Set("isAriantColiseumArena", Ret((JsCall args) =>
				{
					int num3 = Int(args, 0);
					return B(num3 / 1000 == 980010 && num3 % 10 == 1);
				}));
				jsHostObject30.Set("isCygnus", Ret((JsCall args) => B(Int(args, 0) / 1000 == 1)));
				jsHostObject30.Set("isAran", Ret((JsCall args) =>
				{
					int num3 = Int(args, 0);
					return B(num3 == 2000 || (num3 >= 2100 && num3 <= 2112));
				}));
				jsHostObject30.Set("getJobBranch", Ret((JsCall args) =>
				{
					int num3 = JobId(args);
					return N((num3 % 1000 != 0) ? ((num3 % 100 == 0) ? 1 : (2 + num3 % 10)) : 0);
				}));
				jsHostObject30.Set("getHallOfFameMapid", Ret((JsCall args) =>
				{
					int num3 = JobId(args);
					if (num3 / 1000 == 1)
					{
						return N(130000100.0);
					}
					switch (num3)
					{
					case 2000:
					case 2100:
					case 2101:
					case 2102:
					case 2103:
					case 2104:
					case 2105:
					case 2106:
					case 2107:
					case 2108:
					case 2109:
					case 2110:
					case 2111:
					case 2112:
						return N(140010110.0);
					default:
						return (num3 / 100) switch
						{
							1 => (JsValue)N(102000004.0), 
							2 => N(101000004.0), 
							3 => N(100000204.0), 
							4 => N(103000008.0), 
							5 => N(120000105.0), 
							_ => N(130000110.0), 
						};
					}
				}));
				return jsHostObject30;
			}
			case "org.gms.client.SkillFactory":
			{
				JsHostObject jsHostObject29 = new JsHostObject(null);
				Lenient(jsHostObject29, "SkillFactory");
				jsHostObject29.Set("getSkill", Ret((JsCall args) =>
				{
					SkillData data = SkillBook.Get(Int(args, 0));
					if (data != null)
					{
						JsHostObject jsHostObject31 = new JsHostObject(data);
						jsHostObject31.Set("id", N(data.id));
						Lenient(jsHostObject31, "skill");
						jsHostObject31.Set("getId", Ret((JsCall _) => N(data.id)));
						jsHostObject31.Set("getMaxLevel", Ret((JsCall _) => N(data.maxLevel)));
						jsHostObject31.Set("getName", Ret((JsCall _) => S(data.name)));
						return jsHostObject31;
					}
					return JsNull.Value;
				}));
				return jsHostObject29;
			}
			case "org.gms.server.life.LifeFactory":
			{
				JsHostObject jsHostObject27 = new JsHostObject(null);
				jsHostObject27.Set("getMonster", Ret((JsCall args) =>
				{
					int id = Int(args, 0);
					FieldCatalog.LoadMob(id);
					JsHostObject monster = new JsHostObject(null);
					Lenient(monster, "monster");
					monster.Set("id", N(id));
					monster.Set("getId", Ret((JsCall _) => N(id)));
					monster.Set("getName", Ret((JsCall _) => S(WzNames.Mob(id))));
					monster.Set("setPosition", Fn((JsCall jsCall) =>
					{
						monster.Set("position", jsCall.Arg(0));
					}));
					monster.Set("setFh", Fn((JsCall c2) =>
					{
						monster.Set("fh", N(Int(c2, 0)));
					}));
					monster.Set("getFh", Ret((JsCall _) => (!(monster.Get("fh") is JsNumber)) ? N(0.0) : monster.Get("fh")));
					return monster;
				}));
				return jsHostObject27;
			}
			case "org.gms.constants.inventory.ItemConstants":
			{
				JsHostObject jsHostObject22 = new JsHostObject(null);
				Lenient(jsHostObject22, "ItemConstants");
				jsHostObject22.Set("isAccessory", Ret((JsCall c2) => B(Int(c2, 0) >= 1110000 && Int(c2, 0) < 1140000)));
				jsHostObject22.Set("isNewHair", Ret((JsCall c2) =>
				{
					if (newHairBases == null)
					{
						TextAsset val = Resources.Load<TextAsset>("Scripts/hair_base_ids");
						if ((Object)(object)val == (Object)null)
						{
							throw new JsRuntimeException("Missing original Hair ID catalog");
						}
						newHairBases = new HashSet<int>();
						string[] array4 = val.text.Split('\n');
						for (int i = 0; i < array4.Length; i++)
						{
							if (int.TryParse(array4[i].Trim(), out var result))
							{
								newHairBases.Add(result);
							}
						}
					}
					int num3 = Int(c2, 0);
					int num4 = num3 % 10;
					return B(num4 >= 0 && num4 <= 7 && newHairBases.Contains(num3 - num4));
				}));
				return jsHostObject22;
			}
			case "org.gms.client.inventory.Pet":
			{
				JsHostObject jsHostObject23 = new JsHostObject(null);
				jsHostObject23.Set("createPet", Ret((JsCall args) =>
				{
					InventoryChangeResponse inventoryChangeResponse = GameSession.Net.ChangeInventory(new InventoryChangeRequest
					{
						characterId = GameSession.CharacterId,
						itemId = Int(args, 0),
						createPet = true
					});
					if (!inventoryChangeResponse.ok)
					{
						throw new JsRuntimeException(inventoryChangeResponse.error);
					}
					return N(inventoryChangeResponse.createdPetId);
				}));
				return jsHostObject23;
			}
			case "org.gms.client.inventory.InventoryType":
			{
				JsHostObject jsHostObject20 = new JsHostObject(null);
				jsHostObject20.Set("EQUIP", N(1.0));
				jsHostObject20.Set("USE", N(2.0));
				jsHostObject20.Set("SETUP", N(3.0));
				jsHostObject20.Set("UNDEFINED", N(0.0));
				jsHostObject20.Set("ETC", N(4.0));
				jsHostObject20.Set("CASH", N(5.0));
				jsHostObject20.Set("EQUIPPED", N(-1.0));
				return jsHostObject20;
			}
			case "org.gms.client.inventory.manipulator.InventoryManipulator":
			{
				JsHostObject jsHostObject26 = new JsHostObject(null);
				Lenient(jsHostObject26, "InventoryManipulator");
				jsHostObject26.Set("addById", Ret((JsCall args) =>
				{
					int num3 = Int(args, 1);
					int num4 = Int(args, 2, 1);
					if (num3 <= 0 || num4 <= 0)
					{
						return B(v: false);
					}
					InventoryChangeResponse inventoryChangeResponse = GameSession.Net.ChangeInventory(new InventoryChangeRequest
					{
						characterId = GameSession.CharacterId,
						itemId = num3,
						quantity = num4,
						petId = ((args.ArgCount > 4) ? Int(args, 4, -1) : (-1)),
						itemExpiration = ((args.ArgCount > 5) ? ((long)JsConvert.ToNumber(args.Arg(5))) : (-1))
					});
					if (!inventoryChangeResponse.ok)
					{
						return B(v: false);
					}
					GameSession.Character.inventory = inventoryChangeResponse.inventory;
					GameSession.ApplyInventoryToProfile();
					Profile.Save();
					return B(v: true);
				}));
				jsHostObject26.Set("removeFromSlot", Ret((JsCall args) => B(RemoveSlotItem(Int(args, 1), Int(args, 2), Math.Max(1, Int(args, 3, 1))))));
				return jsHostObject26;
			}
			case "org.gms.client.Job":
			{
				JsHostObject jsHostObject16 = new JsHostObject(null);
				Lenient(jsHostObject16, "Job");
				foreach (JobEnum.Const item4 in JobEnum.All)
				{
					jsHostObject16.Set(item4.Name, JobEnum.Js(item4));
				}
				jsHostObject16.Set("getById", Ret((JsCall c2) => JobEnum.Js(JobEnum.Of(Int(c2, 0)) ?? JobEnum.Of(0))));
				jsHostObject16.Set("getJobStyleInternal", Ret((JsCall args) => JobEnum.Js(JobEnum.Of(JobEnum.StyleOf(Int(args, 0), Int(args, 1, 64) == 128)))));
				return jsHostObject16;
			}
			case "org.gms.constants.id.ItemId":
			{
				JsHostObject jsHostObject14 = new JsHostObject(null);
				Lenient(jsHostObject14, "ItemId");
				jsHostObject14.Set("RELAXER", N(3010000.0));
				jsHostObject14.Set("isChair", Ret((JsCall args) =>
				{
					int num3 = Int(args, 0);
					return B(num3 >= 3010000 && num3 <= 3019999);
				}));
				return jsHostObject14;
			}
			case "org.gms.constants.game.ExpTable":
			{
				JsHostObject jsHostObject28 = new JsHostObject(null);
				Lenient(jsHostObject28, "ExpTable");
				jsHostObject28.Set("getExpNeededForLevel", Ret((JsCall args) => N(ExpTable.Needed(Int(args, 0)))));
				return jsHostObject28;
			}
			case "org.slf4j.LoggerFactory":
			{
				JsHostObject jsHostObject25 = new JsHostObject(null);
				jsHostObject25.Set("getLogger", Ret((JsCall args) => BuildLogger(Str(args, 0))));
				return jsHostObject25;
			}
			case "org.gms.server.CashShop":
			{
				JsHostObject jsHostObject24 = new JsHostObject(null);
				Lenient(jsHostObject24, "CashShop");
				jsHostObject24.Set("NX_CREDIT", N(1.0));
				jsHostObject24.Set("MAPLE_POINT", N(2.0));
				jsHostObject24.Set("NX_PREPAID", N(4.0));
				jsHostObject24.Set("MAX_CASH_INVENTORY_SAFE", N(1000.0));
				return jsHostObject24;
			}
			case "org.gms.net.server.guild.Guild":
			{
				JsHostObject jsHostObject21 = new JsHostObject(null);
				Lenient(jsHostObject21, "Guild");
				jsHostObject21.Set("getIncreaseGuildCost", Ret((JsCall args) =>
				{
					DbStore dbStore = GameServer.Local?.Db;
					return (dbStore != null) ? N(NativeCommunityService.IncreaseGuildCost(dbStore, Int(args, 0))) : N(0.0);
				}));
				return jsHostObject21;
			}
			case "org.gms.client.BuffStat":
			{
				JsHostObject jsHostObject18 = new JsHostObject(null);
				Lenient(jsHostObject18, "BuffStat");
				string[] array3 = new string[2] { "MORPH", "HPREC" };
				foreach (string constant in array3)
				{
					JsHostObject jsHostObject19 = new JsHostObject(constant);
					jsHostObject19.Set("name", Ret((JsCall _) => S(constant)));
					jsHostObject19.Set("toString", Ret((JsCall _) => S(constant)));
					jsHostObject18.Set(constant, jsHostObject19);
				}
				return jsHostObject18;
			}
			case "org.gms.net.server.channel.handlers.RingActionHandler":
			{
				JsHostObject jsHostObject15 = new JsHostObject(null);
				Lenient(jsHostObject15, "RingActionHandler");
				jsHostObject15.Set("breakMarriageRing", Ret((JsCall args) =>
				{
					CharRecord obj = GameSession.Character?.record ?? throw new JsRuntimeException("Divorce requires an active character");
					obj.partnerId = 0;
					obj.marriageItemId = 0;
					GameSession.SaveToDb();
					return JsNull.Value;
				}));
				return jsHostObject15;
			}
			case "soloMapling.ArtificialPlayer.BotBossCombatManager":
			{
				JsHostObject jsHostObject12 = new JsHostObject(null);
				Lenient(jsHostObject12, "BotBossCombatManager");
				jsHostObject12.Set("handleChatTrigger", Ret((JsCall args) => B(NativeBotBossCombat.HandleChatTrigger(MapId, (args.ArgCount > 1) ? Str(args, 1) : Str(args, 0)))));
				return jsHostObject12;
			}
			default:
				return JsNull.Value;
			}
		}));
		return jsHostObject;
		static InventoryEntry Equip(JsCall call, int index = 0)
		{
			return (call.Arg(index) as JsHostObject)?.Host as InventoryEntry;
		}
		static int JobId(JsCall args)
		{
			if (!(args.Arg(0) is JsHostObject jsHostObject2))
			{
				return Int(args, 0);
			}
			return JsConvert.ToInt32(args.Interp.Call(jsHostObject2.Get("getId"), jsHostObject2, new JsValue[0]));
		}
		static void OwnPlayer(JsCall call)
		{
			JsValue jsValue = call.Arg(0);
			int num = JsConvert.ToInt32(call.Interp.Call(call.Interp.GetMember(jsValue, "getId"), jsValue, new JsValue[0]));
			if (!GameSession.InGame || num != GameSession.CharacterId)
			{
				throw new JsRuntimeException("Quest player is not the active character");
			}
		}
		static BossResponse ReadGrowth(JsCall call)
		{
			if (call.Arg(0) is JsNull || call.Arg(0) is JsUndefined)
			{
				return new BossResponse();
			}
			int characterId = JsConvert.ToInt32(call.Interp.Call(call.Interp.GetMember(call.Arg(0), "getId"), call.Arg(0), Array.Empty<JsValue>()));
			BossResponse bossResponse = GameSession.Net.Boss(new BossRequest
			{
				characterId = characterId,
				operation = BossOperation.ReadGrowth
			});
			if (!bossResponse.ok)
			{
				throw new JsRuntimeException(bossResponse.error);
			}
			return bossResponse;
		}
	}

	private static JsHostObject BuildLogger(string name)
	{
		JsHostObject jsHostObject = new JsHostObject(null);
		Lenient(jsHostObject, "logger");
		string[] array = new string[5] { "trace", "debug", "info", "warn", "error" };
		foreach (string text in array)
		{
			string tag = text;
			jsHostObject.Set(text, Fn((JsCall c) =>
			{
				StringBuilder stringBuilder = new StringBuilder();
				for (int j = 0; j < c.ArgCount; j++)
				{
					if (j > 0)
					{
						stringBuilder.Append(' ');
					}
					stringBuilder.Append(JsConvert.ToString(c.Arg(j)));
				}
				if (tag == "warn" || tag == "error")
				{
					Debug.LogWarning((object)("[Script/" + name + "] " + stringBuilder));
				}
				else
				{
					Debug.Log((object)("[Script/" + name + "] " + stringBuilder));
				}
			}));
		}
		jsHostObject.Set("isDebugEnabled", Ret((JsCall _) => B(v: false)));
		jsHostObject.Set("isInfoEnabled", Ret((JsCall _) => B(v: true)));
		return jsHostObject;
	}

	private static JsHostObject BuildMonster(EnemyUnit mob)
	{
		JsHostObject jsHostObject = new JsHostObject(mob);
		jsHostObject.Set("id", N(mob.fieldMobId));
		jsHostObject.Set("getId", Ret((JsCall _) => N(mob.fieldMobId)));
		jsHostObject.Set("getObjectId", Ret((JsCall _) => N(((Object)mob).GetInstanceID())));
		jsHostObject.Set("isAlive", Ret((JsCall _) => B((Object)(object)mob != (Object)null && !mob.dead)));
		jsHostObject.Set("getHp", Ret((JsCall _) => N(mob.hp)));
		jsHostObject.Set("getMaxHp", Ret((JsCall _) => N(mob.hpMax)));
		jsHostObject.Set("getName", Ret((JsCall _) => S(WzNames.Mob(mob.fieldMobId))));
		jsHostObject.Set("getLevel", Ret((JsCall _) => N(NativeCombatData.Int(mob.CombatStats, "level", 1))));
		jsHostObject.Set("isBoss", Ret((JsCall _) => B(mob.IsBoss)));
		jsHostObject.Set("getBoss", jsHostObject.Get("isBoss"));
		jsHostObject.Set("getMaxMp", Ret((JsCall _) => N(NativeCombatData.Int(mob.CombatStats, "maxMP"))));
		return jsHostObject;
	}

	private static JsHostObject BuildPosition()
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		return BuildPoint(((Object)(object)Pc != (Object)null) ? (Pc.worldX * 40f) : HeadlessPosition.x, ((Object)(object)Pc != (Object)null) ? ((0f - Pc.FeetPos.y) * 40f) : HeadlessPosition.y);
	}

	internal static JsHostObject BuildPoint(double x, double y)
	{
		JsHostObject jsHostObject = new JsHostObject(null);
		jsHostObject.Set("x", N(x));
		jsHostObject.Set("y", N(y));
		jsHostObject.Set("getX", Ret((JsCall _) => N(x)));
		jsHostObject.Set("getY", Ret((JsCall _) => N(y)));
		jsHostObject.Set("distance", Ret((JsCall c) =>
		{
			if (!(c.Arg(0) is JsHostObject jsHostObject2))
			{
				return N(double.PositiveInfinity);
			}
			double num = x - JsConvert.ToNumber(jsHostObject2.Get("x"));
			double num2 = y - JsConvert.ToNumber(jsHostObject2.Get("y"));
			return N(Math.Sqrt(num * num + num2 * num2));
		}));
		return jsHostObject;
	}

	private static JsHostObject BuildClient(JsHostObject player)
	{
		JsHostObject jsHostObject = new JsHostObject(player);
		Lenient(jsHostObject, "client");
		jsHostObject.Set("getPlayer", Ret((JsCall _) => player));
		jsHostObject.Set("getLogger", Ret((JsCall _) => BuildLogger("Client")));
		jsHostObject.Set("sendPacket", Fn((JsCall args) =>
		{
			ApplyScriptPacket(args.Arg(0));
		}));
		jsHostObject.Set("getChannel", Ret((JsCall _) => N(GameSession.Channel)));
		jsHostObject.Set("getWorld", Ret((JsCall _) => N(GameSession.World)));
		jsHostObject.Set("getChannelServer", Ret((JsCall _) => BuildChannelServer(player)));
		jsHostObject.Set("getEventManager", Ret((JsCall args) => NativeEventScripts.Manager(Str(args, 0))));
		return jsHostObject;
	}

	internal static JsHostObject BuildChannelServer(JsHostObject player)
	{
		JsHostObject jsHostObject = new JsHostObject(null);
		Lenient(jsHostObject, "channelServer");
		JsHostObject storage = new JsHostObject(null);
		Lenient(storage, "playerStorage");
		storage.Set("getAllCharacters", Ret((JsCall _) => new JsArray(new JsValue[1] { player })));
		storage.Set("getAllCharactersCount", Ret((JsCall _) => N(1.0)));
		storage.Set("getCharacterById", Ret((JsCall _) => player));
		string[] array = new string[2] { "getStoredVar", "setStoredVar" };
		foreach (string text in array)
		{
			bool write = text == "setStoredVar";
			jsHostObject.Set(text, Ret((JsCall c) =>
			{
				if (!GameSession.InGame)
				{
					throw new JsRuntimeException("Channel variables require an active character");
				}
				return GameSession.Net.Script(new ScriptRequest
				{
					characterId = GameSession.CharacterId,
					operation = (write ? ScriptOperation.SetStoredVar : ScriptOperation.GetStoredVar),
					world = GameSession.World,
					channel = GameSession.Channel,
					variableKey = Int(c, 0),
					variableValue = Int(c, 1)
				}).value;
			}));
		}
		jsHostObject.Set("getMapFactory", Ret((JsCall _) =>
		{
			JsHostObject jsHostObject2 = new JsHostObject(null);
			Lenient(jsHostObject2, "mapFactory");
			jsHostObject2.Set("getMap", Ret((JsCall c) => BuildWarpMap(Int(c, 0))));
			return jsHostObject2;
		}));
		jsHostObject.Set("getPlayerStorage", Ret((JsCall _) => storage));
		jsHostObject.Set("lookupPartyDojo", Ret((JsCall c) => N(NativeDojoService.LookupPartyDojo(PartyIdOf(c, 0)))));
		jsHostObject.Set("ingressDojo", Ret((JsCall c) => N(NativeDojoService.IngressDojo(JsConvert.ToBoolean(c.Arg(0)), (c.ArgCount > 2) ? PartyIdOf(c, 1) : 0, Int(c, (c.ArgCount <= 2) ? 1 : 2)))));
		jsHostObject.Set("resetDojoMap", Fn((JsCall c) =>
		{
			NativeDojoService.ResetDojoMap(Int(c, 0));
		}));
		jsHostObject.Set("freeDojoSectionIfEmpty", Fn((JsCall c) =>
		{
			NativeDojoService.FreeDojoSectionIfEmpty(Int(c, 0));
		}));
		jsHostObject.Set("dismissDojoSchedule", Fn((JsCall c) =>
		{
			NativeDojoService.DismissDojoSchedule(Int(c, 0), PartyIdOf(c, 1));
		}));
		jsHostObject.Set("getDojoFinishTime", Ret((JsCall c) => N(NativeDojoService.GetDojoFinishTime(Int(c, 0)))));
		jsHostObject.Set("getId", Ret((JsCall _) => N(GameSession.Channel)));
		jsHostObject.Set("getChannel", Ret((JsCall _) => N(GameSession.Channel)));
		jsHostObject.Set("getWorld", Ret((JsCall _) => N(GameSession.World)));
		return jsHostObject;
	}

	private static JsHostObject BuildJob(int jobId)
	{
		JsHostObject jsHostObject = new JsHostObject(jobId);
		jsHostObject.Set("getId", Ret((JsCall _) => N(jobId)));
		jsHostObject.Set("getName", Ret((JsCall _) => S(SkillBook.JobName(jobId))));
		return jsHostObject;
	}

	private static string OriginalItemPart(int id)
	{
		if ((id >= 1670000 && id < 1680000) || (id >= 1010000 && id < 1040000) || (id >= 1122000 && id < 1123000) || (id >= 1132000 && id < 1133000) || (id >= 1142000 && id < 1143000) || (id >= 1152000 && id < 1153000) || (id >= 1180000 && id < 1200000))
		{
			return "Accessory";
		}
		if (id >= 1000000 && id < 1010000)
		{
			return "Cap";
		}
		if (id >= 1102000 && id < 1103000)
		{
			return "Cape";
		}
		if (id >= 1040000 && id < 1050000)
		{
			return "Coat";
		}
		if (id / 10000 == 2 || id / 10000 == 5)
		{
			return "Face";
		}
		if (id >= 1080000 && id < 1090000)
		{
			return "Glove";
		}
		if (id / 10000 == 3 || id / 10000 == 4 || id / 10000 == 6)
		{
			return "Hair";
		}
		if (id >= 1050000 && id < 1060000)
		{
			return "Longcoat";
		}
		if (id >= 1060000 && id < 1070000)
		{
			return "Pants";
		}
		if (id >= 1802000 && id < 1842000)
		{
			return "PetEquip";
		}
		if (id >= 1112000 && id < 1120000)
		{
			return "Ring";
		}
		if (id >= 1092000 && id < 1100000)
		{
			return "Shield";
		}
		if (id >= 1070000 && id < 1080000)
		{
			return "Shoes";
		}
		if (id >= 1900000 && id < 2000000)
		{
			return "Taming";
		}
		if (id >= 1300000 && id < 1800000)
		{
			return "Weapon";
		}
		if (id >= 4000000 && id < 5000000)
		{
			return "Etc";
		}
		return null;
	}

	private static int FreeInventorySlots(int type)
	{
		if (type < 1 || type > 5)
		{
			throw new JsRuntimeException("Invalid inventory type: " + type);
		}
		int num = Profile.InventoryCapacity(type - 1);
		HashSet<int> hashSet = new HashSet<int>();
		if (GameSession.Character != null)
		{
			foreach (InventoryEntry item in GameSession.Character.inventory)
			{
				if (item.inventoryType == type && item.position > 0 && item.position <= num)
				{
					hashSet.Add(item.position);
				}
			}
		}
		return Math.Max(0, num - hashSet.Count);
	}

	private static JsHostObject BuildInventory(int type)
	{
		JsHostObject jsHostObject = new JsHostObject(type);
		Lenient(jsHostObject, "inventory");
		object inventoryLock = GameServer.Local?.Db ?? throw new JsRuntimeException("Inventory requires an active server");
		jsHostObject.Set("lockInventory", Fn((JsCall _) =>
		{
			Monitor.Enter(inventoryLock);
		}));
		jsHostObject.Set("unlockInventory", Fn((JsCall _) =>
		{
			Monitor.Exit(inventoryLock);
		}));
		jsHostObject.Set("countById", Ret((JsCall c) =>
		{
			int num = Int(c, 0);
			int num2 = 0;
			if (GameSession.Character?.inventory != null)
			{
				foreach (InventoryEntry item in GameSession.Character.inventory)
				{
					if (InScriptInventory(item, type) && item.itemId == num)
					{
						num2 += (item.isEquip ? 1 : item.quantity);
					}
				}
			}
			return N(num2);
		}));
		jsHostObject.Set("getSlotLimit", Ret((JsCall _) => N(Profile.InventoryCapacity(type - 1))));
		jsHostObject.Set("getNumFreeSlot", Ret((JsCall _) => N(FreeInventorySlots(type))));
		jsHostObject.Set("getNextFreeSlot", Ret((JsCall _) =>
		{
			int num = Profile.InventoryCapacity(type - 1);
			HashSet<int> hashSet = new HashSet<int>();
			if (GameSession.Character?.inventory != null)
			{
				foreach (InventoryEntry item2 in GameSession.Character.inventory)
				{
					if (item2.inventoryType == type && item2.position > 0)
					{
						hashSet.Add(item2.position);
					}
				}
			}
			for (int i = 1; i <= num; i++)
			{
				if (!hashSet.Contains(i))
				{
					return N(i);
				}
			}
			return N(-1.0);
		}));
		jsHostObject.Set("getItem", Ret((JsCall c) => BuildItemView(type, Int(c, 0))));
		jsHostObject.Set("getItemBySlot", Ret((JsCall c) => BuildItemView(type, Int(c, 0))));
		jsHostObject.Set("iterator", Ret((JsCall _) => BuildInventoryIterator(type)));
		jsHostObject.Set("listById", Ret((JsCall c) =>
		{
			JsArray jsArray = new JsArray();
			int num = Int(c, 0);
			if (GameSession.Character?.inventory != null)
			{
				foreach (InventoryEntry item3 in GameSession.Character.inventory)
				{
					if (InScriptInventory(item3, type) && item3.itemId == num)
					{
						jsArray.Items.Add(BuildItemView(type, item3.position));
					}
				}
			}
			return jsArray;
		}));
		return jsHostObject;
	}

	private static JsHostObject BuildInventoryIterator(int type)
	{
		List<int> slots = new List<int>();
		if (GameSession.Character?.inventory != null)
		{
			foreach (InventoryEntry item in GameSession.Character.inventory)
			{
				if (InScriptInventory(item, type))
				{
					slots.Add(item.position);
				}
			}
		}
		slots.Sort();
		int index = 0;
		JsHostObject jsHostObject = new JsHostObject(null);
		jsHostObject.Set("hasNext", Ret((JsCall _) => B(index < slots.Count)));
		jsHostObject.Set("next", Ret((JsCall _) =>
		{
			if (index >= slots.Count)
			{
				throw new JsRuntimeException("Inventory iterator has no next item");
			}
			return BuildItemView(type, slots[index++]);
		}));
		return jsHostObject;
	}

	private static void MarkDirty(InventoryEntry e)
	{
		if (e != null && e.itemRowId > 0)
		{
			dirtyEquip[e.itemRowId] = e;
		}
	}

	private static JsValue NewItemRef(int id, int count)
	{
		JsHostObject jsHostObject = new JsHostObject(null);
		Lenient(jsHostObject, "item");
		jsHostObject.Set("getItemId", Ret((JsCall _) => N(id)));
		jsHostObject.Set("getId", Ret((JsCall _) => N(id)));
		jsHostObject.Set("getQuantity", Ret((JsCall _) => N(count)));
		jsHostObject.Set("getPosition", Ret((JsCall _) => N(-1.0)));
		return jsHostObject;
	}

	private static InventoryEntry SlotEntry(int type, int slot)
	{
		FullCharacter character = GameSession.Character;
		if (character?.inventory == null)
		{
			return null;
		}
		foreach (InventoryEntry item in character.inventory)
		{
			if (InScriptInventory(item, type) && item.position == slot)
			{
				return item;
			}
		}
		return null;
	}

	private static bool InScriptInventory(InventoryEntry item, int type)
	{
		if (type != -1)
		{
			if (item.inventoryType == type)
			{
				return item.position > 0;
			}
			return false;
		}
		if (item.inventoryType == 1)
		{
			return item.position < 0;
		}
		return false;
	}

	private static JsValue BuildItemView(int type, int slot)
	{
		return BuildItemView(SlotEntry(type, slot));
	}

	private static JsValue BuildItemView(InventoryEntry e, bool live = true)
	{
		if (e == null)
		{
			return JsNull.Value;
		}
		JsHostObject jsHostObject = new JsHostObject(e);
		Lenient(jsHostObject, "item");
		jsHostObject.Set("getItemId", Ret((JsCall _) => N(e.itemId)));
		jsHostObject.Set("getId", Ret((JsCall _) => N(e.itemId)));
		jsHostObject.Set("copy", Ret((JsCall _) => BuildItemView(e.Copy(), live: false)));
		jsHostObject.Set("getQuantity", Ret((JsCall _) => N(e.isEquip ? 1 : e.quantity)));
		jsHostObject.Set("getPosition", Ret((JsCall _) => N(e.position)));
		jsHostObject.Set("getCashId", Ret((JsCall _) => N(e.itemRowId)));
		jsHostObject.Set("getExpandAttribute4", Ret((JsCall _) => S(NativeEquipmentCube.Raw(e))));
		jsHostObject.Set("setExpandAttribute4", Fn((JsCall c) =>
		{
			if (e.equipmentData == null)
			{
				e.equipmentData = new Dictionary<string, object>();
			}
			e.equipmentData["expandAttribute4"] = Str(c, 0);
			Dirty();
		}));
		jsHostObject.Set("getUpgradeSlots", Ret((JsCall _) => N(e.upgradeslots)));
		jsHostObject.Set("getLevel", Ret((JsCall _) => N((e.equipmentData != null && e.equipmentData.TryGetValue("level", out var value)) ? Convert.ToInt32(value) : 0)));
		jsHostObject.Set("getOwner", Ret((JsCall _) => S(e.owner)));
		jsHostObject.Set("getExpiration", Ret((JsCall _) => N(e.expiration)));
		jsHostObject.Set("setExpiration", Fn((JsCall c) =>
		{
			e.expiration = (long)JsConvert.ToNumber(c.Arg(0));
			Dirty();
		}));
		jsHostObject.Set("getStarLevel", Ret((JsCall _) => N(e.starLevel)));
		jsHostObject.Set("setStarLevel", Fn((JsCall c) =>
		{
			e.starLevel = Math.Max(0, Int(c, 0));
			Dirty();
		}));
		jsHostObject.Set("getStarCount", Ret((JsCall _) => N(e.starCount)));
		jsHostObject.Set("setStarCount", Fn((JsCall c) =>
		{
			e.starCount = Math.Max(0, Int(c, 0));
			Dirty();
		}));
		jsHostObject.Set("getMaxStar", Ret((JsCall _) => N(e.maxStar)));
		jsHostObject.Set("setMaxStar", Fn((JsCall c) =>
		{
			e.maxStar = Math.Max(0, Int(c, 0));
			Dirty();
		}));
		jsHostObject.Set("setOwner", Fn((JsCall c) =>
		{
			e.owner = Str(c, 0);
			Dirty();
		}));
		jsHostObject.Set("getStr", Ret((JsCall _) => N(e.str)));
		jsHostObject.Set("setStr", Fn((JsCall c) =>
		{
			e.str = Math.Max(0, Int(c, 0));
			Dirty();
		}));
		jsHostObject.Set("getDex", Ret((JsCall _) => N(e.dex)));
		jsHostObject.Set("setDex", Fn((JsCall c) =>
		{
			e.dex = Math.Max(0, Int(c, 0));
			Dirty();
		}));
		jsHostObject.Set("getInt", Ret((JsCall _) => N(e.int_)));
		jsHostObject.Set("setInt", Fn((JsCall c) =>
		{
			e.int_ = Math.Max(0, Int(c, 0));
			Dirty();
		}));
		jsHostObject.Set("getLuk", Ret((JsCall _) => N(e.luk)));
		jsHostObject.Set("setLuk", Fn((JsCall c) =>
		{
			e.luk = Math.Max(0, Int(c, 0));
			Dirty();
		}));
		jsHostObject.Set("getHp", Ret((JsCall _) => N(e.hp)));
		jsHostObject.Set("setHp", Fn((JsCall c) =>
		{
			e.hp = Math.Max(0, Int(c, 0));
			Dirty();
		}));
		jsHostObject.Set("getMp", Ret((JsCall _) => N(e.mp)));
		jsHostObject.Set("setMp", Fn((JsCall c) =>
		{
			e.mp = Math.Max(0, Int(c, 0));
			Dirty();
		}));
		jsHostObject.Set("getWatk", Ret((JsCall _) => N(e.watk)));
		jsHostObject.Set("setWatk", Fn((JsCall c) =>
		{
			e.watk = Math.Max(0, Int(c, 0));
			Dirty();
		}));
		jsHostObject.Set("getMatk", Ret((JsCall _) => N(e.matk)));
		jsHostObject.Set("setMatk", Fn((JsCall c) =>
		{
			e.matk = Math.Max(0, Int(c, 0));
			Dirty();
		}));
		jsHostObject.Set("getWdef", Ret((JsCall _) => N(e.wdef)));
		jsHostObject.Set("setWdef", Fn((JsCall c) =>
		{
			e.wdef = Math.Max(0, Int(c, 0));
			Dirty();
		}));
		jsHostObject.Set("getMdef", Ret((JsCall _) => N(e.mdef)));
		jsHostObject.Set("setMdef", Fn((JsCall c) =>
		{
			e.mdef = Math.Max(0, Int(c, 0));
			Dirty();
		}));
		jsHostObject.Set("getAcc", Ret((JsCall _) => N(e.acc)));
		jsHostObject.Set("setAcc", Fn((JsCall c) =>
		{
			e.acc = Math.Max(0, Int(c, 0));
			Dirty();
		}));
		jsHostObject.Set("getAvoid", Ret((JsCall _) => N(e.avoid)));
		jsHostObject.Set("setAvoid", Fn((JsCall c) =>
		{
			e.avoid = Math.Max(0, Int(c, 0));
			Dirty();
		}));
		jsHostObject.Set("getHands", Ret((JsCall _) => N(e.hands)));
		jsHostObject.Set("setHands", Fn((JsCall c) =>
		{
			e.hands = Math.Max(0, Int(c, 0));
			Dirty();
		}));
		jsHostObject.Set("getSpeed", Ret((JsCall _) => N(e.speed)));
		jsHostObject.Set("setSpeed", Fn((JsCall c) =>
		{
			e.speed = Math.Max(0, Int(c, 0));
			Dirty();
		}));
		jsHostObject.Set("getJump", Ret((JsCall _) => N(e.jump)));
		jsHostObject.Set("setJump", Fn((JsCall c) =>
		{
			e.jump = Math.Max(0, Int(c, 0));
			Dirty();
		}));
		return jsHostObject;
		void Dirty()
		{
			if (live)
			{
				MarkDirty(e);
			}
		}
	}

	private static void FlushEquip()
	{
		if (dirtyEquip.Count == 0)
		{
			return;
		}
		int characterId = GameSession.CharacterId;
		List<InventoryEntry> list = null;
		if (characterId > 0)
		{
			foreach (InventoryEntry value in dirtyEquip.Values)
			{
				EquipPatchResponse equipPatchResponse = GameSession.Net.UpdateEquip(new EquipPatchRequest
				{
					characterId = characterId,
					inventoryItemId = value.itemRowId,
					expiration = ((value.itemId / 1000000 == 1) ? ((long?)null) : new long?(value.expiration)),
					starLevel = value.starLevel,
					starCount = value.starCount,
					maxStar = value.maxStar,
					owner = value.owner,
					expandAttribute4 = NativeEquipmentCube.Raw(value),
					str = value.str,
					dex = value.dex,
					int_ = value.int_,
					luk = value.luk,
					hp = value.hp,
					mp = value.mp,
					watk = value.watk,
					matk = value.matk,
					wdef = value.wdef,
					mdef = value.mdef,
					acc = value.acc,
					avoid = value.avoid,
					hands = value.hands,
					speed = value.speed,
					jump = value.jump,
					upgradeslots = value.upgradeslots
				});
				if (!equipPatchResponse.ok)
				{
					Debug.LogError((object)("[Script] 装备回写失败：" + equipPatchResponse.error));
				}
				else if (equipPatchResponse.inventory != null)
				{
					list = equipPatchResponse.inventory;
				}
			}
		}
		if (list != null && GameSession.Character != null)
		{
			GameSession.Character.inventory = list;
		}
		dirtyEquip.Clear();
		Pc?.ApplyProgression();
	}

	private static bool RemoveSlotItem(int type, int slot, int quantity)
	{
		int characterId = GameSession.CharacterId;
		if (characterId <= 0)
		{
			return false;
		}
		SlotRemoveResponse slotRemoveResponse = GameSession.Net.RemoveSlot(new SlotRemoveRequest
		{
			characterId = characterId,
			inventoryType = type,
			slot = slot,
			quantity = quantity
		});
		if (!slotRemoveResponse.ok)
		{
			Debug.LogError((object)("[Script] 删除格位失败：" + slotRemoveResponse.error));
			return false;
		}
		if (slotRemoveResponse.inventory != null && GameSession.Character != null)
		{
			GameSession.Character.inventory = slotRemoveResponse.inventory;
		}
		return true;
	}
}

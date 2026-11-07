using System;
using System.Collections.Generic;
using MapleDash.Js;
using Server.Bot;
using UnityEngine;

public static class BotNpcApi
{
	private static readonly HashSet<string> reported = new HashSet<string>();

	private static readonly Dictionary<int, JsHostObject> fallbackCm = new Dictionary<int, JsHostObject>();

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

	public static int Quantity(BotCharacter bot, int itemId)
	{
		if (bot == null || itemId <= 0)
		{
			return 0;
		}
		int num = 0;
		foreach (InventoryEntry item in bot.inventory)
		{
			if (item != null && item.itemId == itemId)
			{
				num += item.quantity;
			}
		}
		return num;
	}

	public static int Gain(BotCharacter bot, int itemId, int count)
	{
		if (bot == null || itemId <= 0 || count == 0)
		{
			return Quantity(bot, itemId);
		}
		if (count < 0)
		{
			int num = -count;
			int num2 = bot.inventory.Count - 1;
			while (num2 >= 0 && num > 0)
			{
				InventoryEntry inventoryEntry = bot.inventory[num2];
				if (inventoryEntry != null && inventoryEntry.itemId == itemId)
				{
					int num3 = Math.Min(inventoryEntry.quantity, num);
					inventoryEntry.quantity -= num3;
					num -= num3;
					if (inventoryEntry.quantity <= 0)
					{
						bot.inventory.RemoveAt(num2);
					}
				}
				num2--;
			}
			return Quantity(bot, itemId);
		}
		foreach (InventoryEntry item in bot.inventory)
		{
			if (item != null && item.itemId == itemId)
			{
				if (itemId / 1000000 != 1)
				{
					item.quantity += count;
					return Quantity(bot, itemId);
				}
				break;
			}
		}
		bot.inventory.Add(new InventoryEntry
		{
			itemId = itemId,
			inventoryType = Math.Max(1, itemId / 1000000),
			quantity = count
		});
		return Quantity(bot, itemId);
	}

	public static bool CanHold(int itemId, int count)
	{
		if (itemId > 0)
		{
			return count != 0;
		}
		return false;
	}

	public static JsHostObject Build(NpcSession session, BotCharacter bot)
	{
		JsHostObject jsHostObject = new JsHostObject(bot);
		jsHostObject.Missing = (string key) =>
		{
			if (reported.Add(key))
			{
				Debug.LogWarning((object)("[BotNpc] " + ((bot != null) ? bot.name : "?") + " 调用了未建模的 cm." + key + "（按空操作处理）"));
			}
			return JsFunction.FromNative(key, (JsCall _) => JsUndefined.Value);
		};
		jsHostObject.Set("getPlayer", Ret((JsCall _) => Player(session, bot)));
		jsHostObject.Set("getMapId", Ret((JsCall _) => N((bot != null) ? bot.mapId : 0)));
		jsHostObject.Set("getNpc", Ret((JsCall _) => N((session != null) ? session.NpcId : 0)));
		jsHostObject.Set("isEventLeader", Ret((JsCall _) => B(v: false)));
		jsHostObject.Set("isLeader", Ret((JsCall _) => B(v: false)));
		jsHostObject.Set("isPartyLeader", Ret((JsCall _) => B(v: false)));
		jsHostObject.Set("getPlayerCount", Ret((JsCall _) => N(1.0)));
		jsHostObject.Set("getEventInstance", Ret((JsCall _) => NativeEventScripts.Instance()));
		jsHostObject.Set("getEventManager", Ret((JsCall _) => JsNull.Value));
		jsHostObject.Set("getParty", Ret((JsCall _) => JsNull.Value));
		jsHostObject.Set("itemQuantity", Ret((JsCall c) => N(Quantity(bot, Int(c, 0)))));
		jsHostObject.Set("getItemQuantity", Ret((JsCall c) => N(Quantity(bot, Int(c, 0)))));
		jsHostObject.Set("hasItem", Ret((JsCall c) => B(Quantity(bot, Int(c, 0)) >= Math.Max(1, Int(c, 1, 1)))));
		jsHostObject.Set("haveItem", Ret((JsCall c) => B(Quantity(bot, Int(c, 0)) >= Math.Max(1, Int(c, 1, 1)))));
		jsHostObject.Set("canHold", Ret((JsCall c) => B(CanHold(Int(c, 0), Int(c, 1, 1)))));
		jsHostObject.Set("gainItem", Fn((JsCall c) =>
		{
			Gain(bot, Int(c, 0), Int(c, 1, 1));
		}));
		jsHostObject.Set("getItemCount", Ret((JsCall c) => N(Quantity(bot, Int(c, 0)))));
		jsHostObject.Set("sendNext", Say(NpcDialogMode.Next));
		jsHostObject.Set("sendPrev", Say(NpcDialogMode.Prev));
		jsHostObject.Set("sendNextPrev", Say(NpcDialogMode.NextPrev));
		jsHostObject.Set("sendOk", Say(NpcDialogMode.Ok));
		jsHostObject.Set("sendYesNo", Say(NpcDialogMode.YesNo));
		jsHostObject.Set("sendSimple", Say(NpcDialogMode.Simple));
		jsHostObject.Set("sendGetNumber", Say(NpcDialogMode.GetNumber));
		jsHostObject.Set("sendStyle", Say(NpcDialogMode.Style));
		jsHostObject.Set("dispose", Fn((JsCall _) =>
		{
			session?.DisposeFromScript();
		}));
		jsHostObject.Set("dropMessage", Fn((JsCall _) =>
		{
		}));
		jsHostObject.Set("message", Fn((JsCall _) =>
		{
		}));
		jsHostObject.Set("playPortalSound", Fn((JsCall _) =>
		{
		}));
		return jsHostObject;
		JsFunction Say(NpcDialogMode mode)
		{
			return Fn((JsCall c) =>
			{
				if (session != null)
				{
					session.LastSay = Str(c, 0);
					session.SetDialog(mode, Str(c, 0));
				}
			});
		}
	}

	public static JsValue Player(NpcSession session, BotCharacter bot)
	{
		JsHostObject jsHostObject = new JsHostObject(bot);
		jsHostObject.Missing = (string key) =>
		{
			if (reported.Add("player." + key))
			{
				Debug.LogWarning((object)("[BotNpc] " + ((bot != null) ? bot.name : "?") + " 调用了未建模的 player." + key + "（按空操作处理）"));
			}
			return JsFunction.FromNative(key, (JsCall _) => JsUndefined.Value);
		};
		jsHostObject.Set("getId", Ret((JsCall _) => N((bot != null) ? bot.id : 0)));
		jsHostObject.Set("getName", Ret((JsCall _) => S((bot != null) ? bot.name : "")));
		jsHostObject.Set("getMapId", Ret((JsCall _) => N((bot != null) ? bot.mapId : 0)));
		jsHostObject.Set("getLevel", Ret((JsCall _) => N((bot == null) ? 1 : bot.level)));
		jsHostObject.Set("getJob", Ret((JsCall _) => N((bot != null) ? bot.job : 0)));
		jsHostObject.Set("getJobId", Ret((JsCall _) => N((bot != null) ? bot.job : 0)));
		jsHostObject.Set("isLeader", Ret((JsCall _) => B(v: false)));
		jsHostObject.Set("isLoggedInWorld", Ret((JsCall _) => B(bot != null && bot.online)));
		jsHostObject.Set("getEventInstance", Ret((JsCall _) => NativeEventScripts.Instance()));
		jsHostObject.Set("getPosition", Ret((JsCall _) =>
		{
			JsHostObject jsHostObject2 = new JsHostObject(null);
			jsHostObject2.Set("getX", Ret((JsCall jsCall) => N((bot != null) ? bot.x : 0f)));
			jsHostObject2.Set("getY", Ret((JsCall jsCall) => N((bot != null) ? bot.y : 0f)));
			jsHostObject2.Set("x", N((bot != null) ? bot.x : 0f));
			jsHostObject2.Set("y", N((bot != null) ? bot.y : 0f));
			return jsHostObject2;
		}));
		jsHostObject.Set("dropMessage", Fn((JsCall _) =>
		{
		}));
		jsHostObject.Set("message", Fn((JsCall _) =>
		{
		}));
		jsHostObject.Set("changeMap", Fn((JsCall _) =>
		{
		}));
		jsHostObject.Set("warp", Fn((JsCall _) =>
		{
		}));
		jsHostObject.Set("getAbstractPlayerInteraction", Ret((JsCall _) => (session == null || session.CmHost == null) ? FallbackCm(bot) : session.CmHost));
		return jsHostObject;
	}

	private static JsHostObject FallbackCm(BotCharacter bot)
	{
		if (bot == null)
		{
			return null;
		}
		if (fallbackCm.TryGetValue(bot.id, out var value) && value != null)
		{
			return value;
		}
		value = (fallbackCm[bot.id] = Build(null, bot));
		value.Set("removeAll", Fn((JsCall c) =>
		{
			Gain(bot, Int(c, 0), -Quantity(bot, Int(c, 0)));
		}));
		return value;
	}
}

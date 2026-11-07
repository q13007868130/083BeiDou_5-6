using System;
using MapleDash.Js;
using Server.Field;
using UnityEngine;

public static class SeedTowerCompat
{
	public const int TowerMapBase = 992000000;

	public const int YellowBat = 9309116;

	public const int RedBat = 9309117;

	public const int HazardFirst = 9309400;

	public const int HazardLast = 9309402;

	private static Func<JsValue> Player => () => NpcApi.BuildPlayer(NpcSession.MapContext());

	private static NativeEventScriptService.Instance Instance()
	{
		NativeEventScriptService.Instance instance = NativeEventScripts.CurrentInstance();
		if (instance == null || !(instance.Property("seedTower") == "1"))
		{
			return null;
		}
		return instance;
	}

	private static int Number(NativeEventScriptService.Instance instance, string key)
	{
		if (instance == null || !int.TryParse(instance.Property(key), out var result))
		{
			return 0;
		}
		return result;
	}

	public static JsValue Resolve(string name)
	{
		if (!(name == "org.gms.server.life.SeedTowerCompat"))
		{
			return null;
		}
		return Type();
	}

	private static JsValue Type()
	{
		JsHostObject jsHostObject = new JsHostObject(null);
		jsHostObject.Missing = (string key) =>
		{
			throw new JsRuntimeException("Unmigrated SeedTowerCompat API: " + key);
		};
		jsHostObject.Set("move", JsFunction.FromNative("move", (JsCall c) =>
		{
			(NativeEventMap.FindByHost(c.Arg(0)) ?? throw new JsRuntimeException("SeedTowerCompat.move requires an event monster")).ScriptMove((float)JsConvert.ToNumber(c.Arg(1)), (float)JsConvert.ToNumber(c.Arg(2)), JsConvert.ToBoolean((c.ArgCount > 4) ? c.Arg(4) : JsBoolean.False));
			return JsUndefined.Value;
		}));
		jsHostObject.Set("spiderWeb", JsFunction.FromNative("spiderWeb", (JsCall c) =>
		{
			NativeEventMap.FindByHost(c.Arg(0))?.Visible?.NativeController?.ScriptReflect(4000L);
			return JsUndefined.Value;
		}));
		return jsHostObject;
	}

	public static bool ProtectsTarget(EnemyUnit mob)
	{
		if ((Object)(object)mob != (Object)null && mob.fieldMode)
		{
			return ProtectsTarget(Profile.LastMapId, mob.fieldMobId);
		}
		return false;
	}

	public static bool ProtectsTarget(int mapId, int mobId)
	{
		if (Instance() == null)
		{
			return false;
		}
		switch (mapId)
		{
		case 992014000:
			if (mobId != 9309116)
			{
				return mobId == 9309117;
			}
			return true;
		case 992008000:
		case 992013000:
			if (mobId >= 9309400)
			{
				return mobId <= 9309402;
			}
			return false;
		default:
			return false;
		}
	}

	public static bool HandleAttack(int skillId, EnemyUnit target)
	{
		int oid = 0;
		if (skillId == 0 && (Object)(object)target != (Object)null && !target.dead)
		{
			int fieldMobId = target.fieldMobId;
			if (fieldMobId == 9309116 || fieldMobId == 9309117)
			{
				NativeEventMap.Mob mob = NativeEventMap.FindByVisible(target);
				if (mob != null)
				{
					oid = mob.ObjectId;
				}
			}
		}
		return HandleAttack(skillId, oid);
	}

	public static bool HandleAttack(int skillId, int oid)
	{
		NativeEventScriptService.Instance instance = Instance();
		if (instance == null || Number(instance, "floor") != 14)
		{
			return false;
		}
		if (Profile.LastMapId != 992014000)
		{
			return false;
		}
		instance.Invoke("seedAttack", instance.Host, Player(), new JsNumber(skillId), new JsNumber(oid));
		return true;
	}

	public static bool Controls(NativeEventMap.Mob mob)
	{
		return mob?.Controlled ?? false;
	}

	public static bool AllowReactorHit(MapReactor reactor)
	{
		NativeEventScriptService.Instance instance = Instance();
		if (instance == null || reactor == null)
		{
			return true;
		}
		int lastMapId = Profile.LastMapId;
		if (lastMapId == 992002000 && reactor.id >= 9260004 && reactor.id <= 9260007)
		{
			return false;
		}
		if (lastMapId != 992018000)
		{
			return true;
		}
		try
		{
			return instance.Invoke("seedReactorHit", instance.Host, Player(), ReactorValue(reactor)) is JsBoolean jsBoolean && jsBoolean.Value;
		}
		catch (Exception ex)
		{
			Debug.LogError((object)("[SeedTower] reactor hook: " + ex.Message));
			return false;
		}
	}

	private static JsValue ReactorValue(MapReactor reactor)
	{
		NativeEventMap nativeEventMap = NativeEventScripts.ResolveMap(Profile.LastMapId);
		if (nativeEventMap != null)
		{
			JsHostObject jsHostObject = nativeEventMap.ReactorFor(reactor);
			if (jsHostObject != null)
			{
				return jsHostObject;
			}
		}
		JsHostObject jsHostObject2 = new JsHostObject(reactor);
		jsHostObject2.Set("getId", JsFunction.FromNative("getId", (JsCall _) => new JsNumber(reactor.id)));
		jsHostObject2.Set("getObjectId", JsFunction.FromNative("getObjectId", (JsCall _) => new JsNumber(reactor.objectId)));
		jsHostObject2.Set("getState", JsFunction.FromNative("getState", (JsCall _) => new JsNumber(reactor.state)));
		jsHostObject2.Set("getPosition", JsFunction.FromNative("getPosition", (JsCall _) => NpcApi.BuildPoint(reactor.x, reactor.y)));
		return jsHostObject2;
	}
}

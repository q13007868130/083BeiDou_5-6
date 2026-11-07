using System;
using System.Collections.Generic;
using MapleDash.Js;
using Server.Field;

public sealed class NativeReactorScriptService : IDisposable
{
	private readonly Func<string, string> source;

	private readonly List<(MapRuntime map, MapNpcPosition npc)> spawned = new List<(MapRuntime, MapNpcPosition)>();

	public Action<int, int, string> Message;

	public Func<int, JsValue> EventInstance;

	public Func<int, JsValue> Player;

	public Func<int, JsValue> Map;

	public Func<int, int, float, float, int, bool> SpawnMonster;

	public Action<Exception> Error;

	public void Dispose()
	{
		foreach (var item in spawned)
		{
			item.map.npcs.Remove(item.npc);
		}
		spawned.Clear();
	}

	public NativeReactorScriptService(Func<string, string> source)
	{
		this.source = source;
	}

	public bool Act(MapReactor reactor, int owner)
	{
		if (reactor == null)
		{
			return false;
		}
		string text = "reactor/" + reactor.id;
		string value = source?.Invoke(text);
		if (string.IsNullOrEmpty(value))
		{
			return false;
		}
		try
		{
			JsObject rm = new JsObject();
			Bind(rm, "dropItems", (JsCall c) =>
			{
				Drop(reactor, owner, c);
				return JsUndefined.Value;
			});
			Bind(rm, "sprayItems", (JsCall c) =>
			{
				Drop(reactor, owner, c);
				return JsUndefined.Value;
			});
			Bind(rm, "mapMessage", (JsCall c) =>
			{
				Message?.Invoke(reactor.mapId, JsConvert.ToInt32(c.Arg(0)), JsConvert.ToString(c.Arg(1)));
				return JsUndefined.Value;
			});
			Bind(rm, "playerMessage", (JsCall c) =>
			{
				Message?.Invoke(reactor.mapId, JsConvert.ToInt32(c.Arg(0)), JsConvert.ToString(c.Arg(1)));
				return JsUndefined.Value;
			});
			Bind(rm, "spawnMonster", (JsCall c) =>
			{
				Spawn(reactor, c);
				return JsUndefined.Value;
			});
			Bind(rm, "spawnNpc", (JsCall c) =>
			{
				MapRuntime mapRuntime = MapService.Get(reactor.mapId);
				if (mapRuntime == null)
				{
					throw new JsRuntimeException("Reactor map is no longer loaded");
				}
				MapNpcPosition mapNpcPosition = new MapNpcPosition
				{
					id = JsConvert.ToInt32(c.Arg(0)),
					x = reactor.x,
					y = reactor.y,
					dynamic = true
				};
				mapRuntime.npcs.Add(mapNpcPosition);
				spawned.Add((mapRuntime, mapNpcPosition));
				return JsUndefined.Value;
			});
			Bind(rm, "getEventInstance", (JsCall c) => EventInstance?.Invoke(reactor.mapId) ?? JsNull.Value);
			Bind(rm, "getPlayer", (JsCall c) => Player?.Invoke(owner) ?? JsNull.Value);
			Bind(rm, "getMap", (JsCall c) => MapOf(reactor));
			Bind(rm, "getPosition", (JsCall c) => Point(reactor.x, reactor.y - 10));
			JsHostObject reactorHost = new JsHostObject(reactor);
			reactorHost.Missing = (string name) =>
			{
				throw new JsRuntimeException("Unmigrated reactor API: " + name);
			};
			BindReactor("getId", (JsCall c) => new JsNumber(reactor.id));
			BindReactor("getObjectId", (JsCall c) => new JsNumber(reactor.objectId));
			BindReactor("getState", (JsCall c) => new JsNumber(reactor.state));
			BindReactor("getName", (JsCall c) => new JsString(reactor.name ?? ""));
			BindReactor("isAlive", (JsCall c) => JsBoolean.Of(reactor.alive));
			BindReactor("getPosition", (JsCall c) => Point(reactor.x, reactor.y));
			BindReactor("getMap", (JsCall c) => MapOf(reactor));
			rm.Set("getReactor", JsFunction.FromNative("getReactor", (JsCall c) => reactorHost));
			JsEngine jsEngine = new JsEngine(value, text);
			jsEngine.Run((JsScope scope) =>
			{
				scope.Declare("rm", rm);
			});
			if (!jsEngine.HasFunction("act"))
			{
				return false;
			}
			jsEngine.Call("act");
			return true;
			void BindReactor(string name, Func<JsCall, JsValue> call)
			{
				reactorHost.Set(name, JsFunction.FromNative(name, call));
			}
		}
		catch (Exception obj)
		{
			Error?.Invoke(obj);
			return false;
		}
	}

	private static void Bind(JsObject host, string name, Func<JsCall, JsValue> action)
	{
		host.Set(name, JsFunction.FromNative(name, action));
	}

	private static JsValue Point(float x, float y)
	{
		JsHostObject jsHostObject = new JsHostObject(null);
		jsHostObject.Set("x", new JsNumber(x));
		jsHostObject.Set("y", new JsNumber(y));
		jsHostObject.Set("getX", JsFunction.FromNative("getX", (JsCall _) => new JsNumber(x)));
		jsHostObject.Set("getY", JsFunction.FromNative("getY", (JsCall _) => new JsNumber(y)));
		return jsHostObject;
	}

	private JsValue MapOf(MapReactor reactor)
	{
		JsValue jsValue = Map?.Invoke(reactor.mapId);
		if (jsValue != null && !(jsValue is JsNull) && !(jsValue is JsUndefined))
		{
			return jsValue;
		}
		JsHostObject jsHostObject = new JsHostObject(null);
		jsHostObject.Missing = (string name) =>
		{
			throw new JsRuntimeException("Unmigrated map API: " + name);
		};
		jsHostObject.Set("getId", JsFunction.FromNative("getId", (JsCall _) => new JsNumber(reactor.mapId)));
		jsHostObject.Set("getMapId", JsFunction.FromNative("getMapId", (JsCall _) => new JsNumber(reactor.mapId)));
		return jsHostObject;
	}

	private void Drop(MapReactor reactor, int owner, JsCall call)
	{
		bool meso = false;
		int mesoChance = 1;
		int minMeso = 0;
		int maxMeso = 0;
		int minItems = 0;
		int num = ((call.ArgCount >= 6) ? 2 : ((call.ArgCount < 4) ? (-1) : 0));
		if (num >= 0)
		{
			meso = JsConvert.ToBoolean(call.Arg(num));
			mesoChance = Math.Max(1, JsConvert.ToInt32(call.Arg(num + 1)));
			minMeso = JsConvert.ToInt32(call.Arg(num + 2));
			maxMeso = JsConvert.ToInt32(call.Arg(num + 3));
			if (call.ArgCount > num + 4)
			{
				minItems = Math.Max(0, JsConvert.ToInt32(call.Arg(num + 4)));
			}
		}
		try
		{
			NativeReactorDrops.Drop(reactor, owner, meso, mesoChance, minMeso, maxMeso, minItems);
		}
		catch (Exception obj)
		{
			Error?.Invoke(obj);
		}
	}

	private void Spawn(MapReactor reactor, JsCall call)
	{
		int arg = JsConvert.ToInt32(call.Arg(0));
		int arg2 = 1;
		float arg3 = reactor.x;
		float arg4 = reactor.y;
		if (call.ArgCount == 2)
		{
			arg2 = Math.Max(1, JsConvert.ToInt32(call.Arg(1)));
		}
		else if (call.ArgCount == 3)
		{
			arg3 = (float)JsConvert.ToNumber(call.Arg(1));
			arg4 = (float)JsConvert.ToNumber(call.Arg(2));
		}
		else if (call.ArgCount >= 4)
		{
			arg2 = Math.Max(1, JsConvert.ToInt32(call.Arg(1)));
			arg3 = (float)JsConvert.ToNumber(call.Arg(2));
			arg4 = (float)JsConvert.ToNumber(call.Arg(3));
		}
		if (SpawnMonster == null)
		{
			Error?.Invoke(new InvalidOperationException("Reactor spawnMonster is not wired: reactor " + reactor.id + " -> mob " + arg));
		}
		else if (!SpawnMonster(reactor.mapId, arg, arg3, arg4, arg2))
		{
			Error?.Invoke(new InvalidOperationException("Reactor spawnMonster failed: map " + reactor.mapId + " mob " + arg));
		}
	}
}

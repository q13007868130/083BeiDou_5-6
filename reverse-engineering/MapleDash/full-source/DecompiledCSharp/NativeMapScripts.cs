using System;
using System.Collections.Generic;
using MapleDash.Js;
using UnityEngine;

public sealed class NativeMapScripts : MonoBehaviour
{
	private readonly NativeMapScriptService scripts = new NativeMapScriptService();

	private readonly NativeBossTimeline timers = new NativeBossTimeline();

	private object character;

	private int characterId;

	private FieldDirector field;

	private static readonly Dictionary<int, string> ServerOnlyEnterScripts = new Dictionary<int, string>
	{
		{ 800040208, "armoredSamuraiBoss" },
		{ 800040401, "emperorToadBoss" },
		{ 800040410, "emperorToadBoss" }
	};

	private static long Now => (long)(Time.realtimeSinceStartupAsDouble * 1000.0);

	public static void Enter(FieldDirector field)
	{
		NativeMapIntro.Enter(field);
		NativeMapScripts nativeMapScripts = ((Component)field).GetComponent<NativeMapScripts>() ?? ((Component)field).gameObject.AddComponent<NativeMapScripts>();
		nativeMapScripts.field = field;
		if (nativeMapScripts.character != GameSession.Character)
		{
			nativeMapScripts.scripts.EndCharacter(nativeMapScripts.characterId);
			nativeMapScripts.scripts.Reload();
			nativeMapScripts.timers.Clear();
			nativeMapScripts.character = GameSession.Character;
			nativeMapScripts.characterId = GameSession.CharacterId;
		}
		nativeMapScripts.Run(ScriptName(field.Map.id, field.Map.onFirstUserEnter), first: true);
		nativeMapScripts.Run(ScriptName(field.Map.id, field.Map.onUserEnter), first: false);
	}

	public void Reload()
	{
		scripts.Reload();
		timers.Clear();
	}

	private static string ScriptName(int mapId, string declared)
	{
		if (!string.IsNullOrEmpty(declared))
		{
			return declared;
		}
		if (ServerOnlyEnterScripts.TryGetValue(mapId, out var value))
		{
			return value;
		}
		return mapId.ToString();
	}

	private void Run(string name, bool first)
	{
		if (string.IsNullOrEmpty(name))
		{
			return;
		}
		int scriptUiLock = PlayerCtrl.ScriptUiLock;
		try
		{
			NpcSession session = NpcSession.MapContext();
			JsHostObject ms = NpcApi.Build(session, 0, 0, isQuest: false);
			string[] array = new string[12]
			{
				"showEffect", "mapEffect", "showWZEffect", "openUI", "setDirectionStatus", "spawnGuide", "removeGuide", "talkGuide", "resetDojoEnergy", "displayCygnusIntro",
				"displayAranIntro", "startExplorerExperience"
			};
			foreach (string text in array)
			{
				string missing = text;
				ms.Set(text, JsFunction.FromNative(text, (JsCall _) =>
				{
					throw new JsRuntimeException("Unmigrated map API: " + missing);
				}));
			}
			ms.Set("goAdventure", JsFunction.FromNative("goAdventure", (JsCall c) =>
			{
				NativeMapIntro.Show("Effect/Direction3.img/goAdventure/Scene" + (GameSession.Character?.record.gender ?? 0), lockUi: true);
				return JsUndefined.Value;
			}));
			ms.Set("goLith", JsFunction.FromNative("goLith", (JsCall c) =>
			{
				NativeMapIntro.Show("Effect/Direction3.img/goLith/Scene" + (GameSession.Character?.record.gender ?? 0), lockUi: true);
				return JsUndefined.Value;
			}));
			ms.Set("playSound", JsFunction.FromNative("playSound", (JsCall c) =>
			{
				AudioManager.Play(JsConvert.ToString(c.Arg(0)));
				return JsUndefined.Value;
			}));
			ms.Set("changeMusic", JsFunction.FromNative("changeMusic", (JsCall c) =>
			{
				AudioManager.PlayBgm(JsConvert.ToString(c.Arg(0)));
				return JsUndefined.Value;
			}));
			JsValue mapGetter = ms.Get("getMap");
			JsInterpreter interpreter = new JsInterpreter();
			ms.Set("getMap", JsFunction.FromNative("getMap", (JsCall c) =>
			{
				if (c.ArgCount > 0 && JsConvert.ToInt32(c.Arg(0)) != field.Map.id)
				{
					throw new JsRuntimeException("Unmigrated map API: getMap(remote field)");
				}
				return interpreter.Call(mapGetter, ms, Array.Empty<JsValue>());
			}));
			ms.Set("scheduleMapWarp", JsFunction.FromNative("scheduleMapWarp", (JsCall c) =>
			{
				int expected = JsConvert.ToInt32(c.Arg(0));
				int target = JsConvert.ToInt32(c.Arg(1));
				long delay = (long)JsConvert.ToNumber(c.Arg(2));
				timers.Schedule(Now, delay, () =>
				{
					if ((Object)(object)field != (Object)null)
					{
						FieldMap map = field.Map;
						if (map == null)
						{
							return false;
						}
						return map.id == expected;
					}
					return false;
				}, () =>
				{
					field.WarpTo(target, null, 0);
				});
				return JsUndefined.Value;
			}));
			scripts.Run(characterId, field.Map.id, (first ? "onFirstUserEnter/" : "onUserEnter/") + name, first, ScriptSource.LoadKey, ms, (JsEngine engine) =>
			{
				JsHostObject java = NpcApi.BuildJava();
				engine.Interp.ResolveHostType = (string type) => engine.Interp.Call(engine.Interp.GetMember(java, "type"), java, new JsValue[1]
				{
					new JsString(type)
				});
			});
		}
		catch (Exception ex)
		{
			PlayerCtrl.ScriptUiLock = scriptUiLock;
			Debug.LogError((object)("[MapScript] " + field.Map.id + "/" + name + ": " + ex.Message));
		}
	}

	private void Update()
	{
		if (character != GameSession.Character)
		{
			timers.Clear();
		}
		else
		{
			timers.Tick(Now);
		}
	}
}

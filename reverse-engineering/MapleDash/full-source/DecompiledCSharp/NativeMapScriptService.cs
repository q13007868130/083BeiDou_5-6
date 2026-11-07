using System;
using System.Collections.Generic;
using MapleDash.Js;

public sealed class NativeMapScriptService
{
	private readonly Dictionary<string, JsEngine> scripts = new Dictionary<string, JsEngine>();

	private readonly Dictionary<int, HashSet<string>> entered = new Dictionary<int, HashSet<string>>();

	public bool Run(int character, int map, string path, bool firstUser, Func<string, string> source, JsValue methods, Action<JsEngine> bind = null)
	{
		if (firstUser)
		{
			if (!entered.TryGetValue(character, out var value))
			{
				value = (entered[character] = new HashSet<string>());
			}
			if (!value.Add(map + ":" + path))
			{
				return false;
			}
		}
		if (scripts.TryGetValue(path, out var value2))
		{
			try
			{
				value2.Call("start", methods);
				return true;
			}
			catch (JsRuntimeException)
			{
				scripts.Remove(path);
			}
		}
		string text = source("map/" + path);
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		JsEngine jsEngine = new JsEngine(text, "map/" + path);
		bind?.Invoke(jsEngine);
		jsEngine.Run();
		scripts[path] = jsEngine;
		jsEngine.Call("start", methods);
		return true;
	}

	public void EndCharacter(int character)
	{
		entered.Remove(character);
	}

	public void Reload()
	{
		scripts.Clear();
	}
}

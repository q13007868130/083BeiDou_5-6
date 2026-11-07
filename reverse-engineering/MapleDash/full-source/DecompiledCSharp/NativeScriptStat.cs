using System;
using MapleDash.Js;

public static class NativeScriptStat
{
	public static JsValue Type()
	{
		string[] names = new string[20]
		{
			"SKIN", "FACE", "HAIR", "LEVEL", "JOB", "STR", "DEX", "INT", "LUK", "HP",
			"MAXHP", "MP", "MAXMP", "AVAILABLEAP", "AVAILABLESP", "EXP", "FAME", "MESO", "PET", "GACHAEXP"
		};
		int[] masks = new int[20]
		{
			1, 2, 4, 16, 32, 64, 128, 256, 512, 1024,
			2048, 4096, 8192, 16384, 32768, 65536, 131072, 262144, 1572872, 2097152
		};
		JsHostObject jsHostObject = new JsHostObject(null);
		jsHostObject.Missing = (string key) =>
		{
			throw new JsRuntimeException("Unmigrated Stat API: " + key);
		};
		JsValue[] values = new JsValue[names.Length];
		for (int num = 0; num < names.Length; num++)
		{
			int ordinal = num;
			int value = masks[num];
			string name = names[num];
			JsHostObject jsHostObject2 = new JsHostObject(name);
			jsHostObject2.Missing = (string key) =>
			{
				throw new JsRuntimeException("Unmigrated Stat." + name + " API: " + key);
			};
			jsHostObject2.Set("getValue", JsFunction.FromNative("getValue", (JsCall _) => new JsNumber(value)));
			jsHostObject2.Set("name", JsFunction.FromNative("name", (JsCall _) => new JsString(name)));
			jsHostObject2.Set("toString", jsHostObject2.Get("name"));
			jsHostObject2.Set("ordinal", JsFunction.FromNative("ordinal", (JsCall _) => new JsNumber(ordinal)));
			values[num] = jsHostObject2;
			jsHostObject.Set(name, jsHostObject2);
		}
		jsHostObject.Set("getByValue", JsFunction.FromNative("getByValue", (JsCall c) => Find(Array.IndexOf(masks, JsConvert.ToInt32(c.Arg(0))))));
		jsHostObject.Set("getByString", JsFunction.FromNative("getByString", (JsCall c) => Find(Array.IndexOf(names, JsConvert.ToString(c.Arg(0))))));
		jsHostObject.Set("getBy5ByteEncoding", JsFunction.FromNative("getBy5ByteEncoding", (JsCall c) =>
		{
			int num2 = JsConvert.ToInt32(c.Arg(0));
			return (num2 != 64 && num2 != 128 && num2 != 256 && num2 != 512) ? JsNull.Value : Find(Array.IndexOf(masks, num2));
		}));
		jsHostObject.Set("values", JsFunction.FromNative("values", (JsCall _) => new JsArray(values)));
		jsHostObject.Set("valueOf", JsFunction.FromNative("valueOf", (JsCall c) =>
		{
			int num2 = Array.IndexOf(names, JsConvert.ToString(c.Arg(0)));
			if (num2 < 0)
			{
				throw new JsRuntimeException("No enum constant Stat." + JsConvert.ToString(c.Arg(0)));
			}
			return values[num2];
		}));
		return jsHostObject;
		JsValue Find(int index)
		{
			if (index >= 0)
			{
				return values[index];
			}
			return JsNull.Value;
		}
	}
}

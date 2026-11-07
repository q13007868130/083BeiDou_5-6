using System;
using MapleDash.Js;

public sealed class NativeScriptItem
{
	public readonly int Id;

	public short Position;

	public short Quantity;

	public short Flag;

	public string Owner = "";

	public string GiftFrom = "";

	public long Expiration = -1L;

	public NativeScriptItem(int id, short position, short quantity)
	{
		Id = id;
		Position = position;
		Quantity = quantity;
	}

	private static void Bind(JsHostObject host, string name, Func<JsCall, JsValue> call)
	{
		host.Set(name, JsFunction.FromNative(name, call));
	}

	public JsHostObject Host()
	{
		JsHostObject jsHostObject = new JsHostObject(this)
		{
			Missing = (string key) =>
			{
				throw new JsRuntimeException("Unmigrated item API: " + key);
			}
		};
		Bind(jsHostObject, "getItemId", (JsCall c) => new JsNumber(Id));
		Bind(jsHostObject, "getPosition", (JsCall c) => new JsNumber(Position));
		Bind(jsHostObject, "getQuantity", (JsCall c) => new JsNumber(Quantity));
		Bind(jsHostObject, "getFlag", (JsCall c) => new JsNumber(Flag));
		Bind(jsHostObject, "getOwner", (JsCall c) => new JsString(Owner));
		Bind(jsHostObject, "getGiftFrom", (JsCall c) => new JsString(GiftFrom));
		Bind(jsHostObject, "getExpiration", (JsCall c) => new JsNumber(Expiration));
		Bind(jsHostObject, "getPetId", (JsCall c) => new JsNumber(-1.0));
		Bind(jsHostObject, "setPosition", (JsCall c) =>
		{
			Position = (short)JsConvert.ToInt32(c.Arg(0));
			return JsUndefined.Value;
		});
		Bind(jsHostObject, "setQuantity", (JsCall c) =>
		{
			Quantity = (short)JsConvert.ToInt32(c.Arg(0));
			return JsUndefined.Value;
		});
		Bind(jsHostObject, "setFlag", (JsCall c) =>
		{
			Flag = (short)JsConvert.ToInt32(c.Arg(0));
			return JsUndefined.Value;
		});
		Bind(jsHostObject, "setOwner", (JsCall c) =>
		{
			Owner = JsConvert.ToString(c.Arg(0));
			return JsUndefined.Value;
		});
		Bind(jsHostObject, "setGiftFrom", (JsCall c) =>
		{
			GiftFrom = JsConvert.ToString(c.Arg(0));
			return JsUndefined.Value;
		});
		Bind(jsHostObject, "setExpiration", (JsCall c) =>
		{
			Expiration = (long)JsConvert.ToNumber(c.Arg(0));
			return JsUndefined.Value;
		});
		Bind(jsHostObject, "copy", (JsCall c) => new NativeScriptItem(Id, Position, Quantity)
		{
			Flag = Flag,
			Owner = Owner,
			Expiration = Expiration
		}.Host());
		return jsHostObject;
	}

	public static JsValue Constructor()
	{
		return JsFunction.FromNative("Item", (JsCall c) =>
		{
			if (c.ArgCount != 3)
			{
				throw new JsRuntimeException("Unmigrated Item constructor overload: " + c.ArgCount);
			}
			return new NativeScriptItem(JsConvert.ToInt32(c.Arg(0)), (short)JsConvert.ToInt32(c.Arg(1)), (short)JsConvert.ToInt32(c.Arg(2))).Host();
		});
	}
}

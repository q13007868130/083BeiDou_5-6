using System;
using System.Globalization;

namespace MapleDash.Js;

public sealed class JsDate : JsValue
{
	internal static Func<long> Clock = () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

	public override string TypeName => "object";

	public double Milliseconds { get; private set; }

	public JsDate(double milliseconds)
	{
		Milliseconds = milliseconds;
	}

	public static JsFunction Constructor()
	{
		JsFunction jsFunction = JsFunction.FromNative("Date", (JsCall c) =>
		{
			if (c.ArgCount == 0)
			{
				return new JsDate(Clock());
			}
			if (c.Arg(0) is JsDate jsDate)
			{
				return new JsDate(jsDate.Milliseconds);
			}
			if (c.Arg(0) is JsString)
			{
				throw new JsRuntimeException("Unmigrated Date string constructor");
			}
			if (c.ArgCount > 1)
			{
				throw new JsRuntimeException("Unmigrated Date component constructor");
			}
			return new JsDate(JsConvert.ToNumber(c.Arg(0)));
		});
		jsFunction.Members.Set("now", JsFunction.FromNative("now", (JsCall c) => new JsNumber(Clock())));
		return jsFunction;
	}

	public JsValue Get(string key)
	{
		if (key == "getTime" || key == "valueOf")
		{
			return JsFunction.FromNative(key, (JsCall c) => new JsNumber(Milliseconds));
		}
		if (key == "getHours" || key == "getMinutes")
		{
			return JsFunction.FromNative(key, (JsCall c) =>
			{
				if (double.IsNaN(Milliseconds) || double.IsInfinity(Milliseconds))
				{
					return new JsNumber(double.NaN);
				}
				try
				{
					DateTime localDateTime = DateTimeOffset.FromUnixTimeMilliseconds((long)Milliseconds).LocalDateTime;
					return new JsNumber((key == "getHours") ? localDateTime.Hour : localDateTime.Minute);
				}
				catch (ArgumentOutOfRangeException)
				{
					return new JsNumber(double.NaN);
				}
			});
		}
		if (key == "setHours")
		{
			return JsFunction.FromNative(key, (JsCall c) =>
			{
				if (double.IsNaN(Milliseconds))
				{
					return new JsNumber(double.NaN);
				}
				DateTime localDateTime = DateTimeOffset.FromUnixTimeMilliseconds((long)Milliseconds).LocalDateTime;
				double num = JsConvert.ToNumber(c.Arg(0));
				double num2 = ((c.ArgCount > 1) ? JsConvert.ToNumber(c.Arg(1)) : ((double)localDateTime.Minute));
				double num3 = ((c.ArgCount > 2) ? JsConvert.ToNumber(c.Arg(2)) : ((double)localDateTime.Second));
				double num4 = ((c.ArgCount > 3) ? JsConvert.ToNumber(c.Arg(3)) : ((double)localDateTime.Millisecond));
				if (double.IsNaN(num + num2 + num3 + num4) || double.IsInfinity(num + num2 + num3 + num4))
				{
					Milliseconds = double.NaN;
				}
				else
				{
					try
					{
						DateTime dateTime = localDateTime.Date.AddHours(Math.Truncate(num)).AddMinutes(Math.Truncate(num2)).AddSeconds(Math.Truncate(num3))
							.AddMilliseconds(Math.Truncate(num4));
						Milliseconds = new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
					}
					catch (ArgumentOutOfRangeException)
					{
						Milliseconds = double.NaN;
					}
				}
				return new JsNumber(Milliseconds);
			});
		}
		return JsUndefined.Value;
	}

	public override string ToString()
	{
		if (!double.IsNaN(Milliseconds))
		{
			return DateTimeOffset.FromUnixTimeMilliseconds((long)Milliseconds).ToLocalTime().ToString("r", CultureInfo.InvariantCulture);
		}
		return "Invalid Date";
	}
}

using System;
using System.Text.RegularExpressions;

namespace MapleDash.Js;

public sealed class JsRegExp : JsValue
{
	public readonly Regex Regex;

	public readonly string Pattern;

	public readonly string Flags;

	public readonly bool Global;

	private int lastIndex;

	public override string TypeName => "object";

	public JsRegExp(string pattern, string flags)
	{
		Pattern = pattern;
		Flags = flags ?? "";
		Global = Flags.Contains("g");
		RegexOptions regexOptions = RegexOptions.ECMAScript | RegexOptions.CultureInvariant;
		if (Flags.Contains("i"))
		{
			regexOptions |= RegexOptions.IgnoreCase;
		}
		if (Flags.Contains("m"))
		{
			regexOptions |= RegexOptions.Multiline;
		}
		string flags2 = Flags;
		for (int i = 0; i < flags2.Length; i++)
		{
			char value = flags2[i];
			if ("gim".IndexOf(value) < 0)
			{
				throw new JsRuntimeException("Unsupported regular expression flag: " + value);
			}
		}
		try
		{
			Regex = new Regex(pattern, regexOptions, TimeSpan.FromSeconds(1.0));
		}
		catch (ArgumentException ex)
		{
			throw new JsRuntimeException("SyntaxError: " + ex.Message);
		}
	}

	public JsValue Get(string key)
	{
		if (key == "source")
		{
			return new JsString(Pattern);
		}
		if (key == "global")
		{
			return JsBoolean.Of(Global);
		}
		if (key == "lastIndex")
		{
			return new JsNumber(lastIndex);
		}
		if (key == "test" || key == "exec")
		{
			return JsFunction.FromNative(key, (JsCall c) =>
			{
				string text = JsConvert.ToString(c.Arg(0));
				Match match = ((Global && lastIndex > text.Length) ? Match.Empty : Regex.Match(text, Global ? lastIndex : 0));
				if (Global)
				{
					lastIndex = (match.Success ? (match.Index + match.Length) : 0);
				}
				if (key == "test")
				{
					return JsBoolean.Of(match.Success);
				}
				if (!match.Success)
				{
					return JsNull.Value;
				}
				JsArray jsArray = new JsArray();
				foreach (Group group in match.Groups)
				{
					jsArray.Items.Add(group.Success ? ((JsValue)new JsString(group.Value)) : ((JsValue)JsUndefined.Value));
				}
				jsArray.Set("index", new JsNumber(match.Index));
				jsArray.Set("input", new JsString(text));
				return jsArray;
			});
		}
		return JsUndefined.Value;
	}

	public void SetLastIndex(JsValue value)
	{
		lastIndex = Math.Max(0, JsConvert.ToInt32(value));
	}
}

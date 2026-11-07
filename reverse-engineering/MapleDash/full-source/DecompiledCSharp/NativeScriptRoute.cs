using System;
using MapleDash.Js;

public sealed class NativeScriptRoute
{
	public NativeScriptRouteType type;

	public string previous;

	public string next;

	public string prefix;

	public void Set(NativeScriptRouteType kind, string before = null, string after = null, string selectionPrefix = null)
	{
		type = kind;
		previous = before;
		next = after;
		prefix = selectionPrefix;
	}

	public bool Resolve(int mode, int selection, string text, out string function, out JsValue[] arguments)
	{
		function = null;
		arguments = Array.Empty<JsValue>();
		if (mode == -1)
		{
			return false;
		}
		switch (type)
		{
		case NativeScriptRouteType.Select:
			if (mode == 0)
			{
				return false;
			}
			function = "level" + prefix + selection;
			break;
		case NativeScriptRouteType.NextSelect:
		case NativeScriptRouteType.Number:
			if (mode == 0)
			{
				return false;
			}
			function = "level" + next;
			arguments = new JsValue[1]
			{
				new JsNumber(selection)
			};
			break;
		case NativeScriptRouteType.Text:
			if (mode == 0)
			{
				return false;
			}
			function = "level" + next;
			arguments = new JsValue[1]
			{
				new JsString(text ?? "")
			};
			break;
		case NativeScriptRouteType.Next:
		case NativeScriptRouteType.Previous:
		case NativeScriptRouteType.PreviousNext:
		case NativeScriptRouteType.Ok:
		case NativeScriptRouteType.AcceptDecline:
		case NativeScriptRouteType.YesNo:
			function = "level" + (((mode == 0) ? previous : next) ?? "null");
			break;
		default:
			throw new JsRuntimeException("Unsupported nextLevel route: " + type);
		}
		return true;
	}
}

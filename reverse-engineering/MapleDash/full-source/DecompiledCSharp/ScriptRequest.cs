using System.Collections.Generic;
using MapleDash.Js;

public sealed class ScriptRequest
{
	public ScriptOperation operation;

	public int characterId;

	public int sessionId;

	public string name;

	public string source;

	public string function;

	public Dictionary<string, JsValue> bindings;

	public JsValue[] arguments;

	public string location;

	public int mapId;

	public int portalId;

	public int world;

	public int channel;

	public int variableKey;

	public int variableValue;

	public int questId;

	public string customData;

	public int gachaponId;
}

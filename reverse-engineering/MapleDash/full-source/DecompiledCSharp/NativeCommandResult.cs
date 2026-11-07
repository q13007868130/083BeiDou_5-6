using System.Collections.Generic;

public sealed class NativeCommandResult
{
	public bool ok = true;

	public bool changed;

	public string error = "";

	public readonly List<string> messages = new List<string>();

	public static NativeCommandResult Fail(string text)
	{
		return new NativeCommandResult
		{
			ok = false,
			error = text
		};
	}
}

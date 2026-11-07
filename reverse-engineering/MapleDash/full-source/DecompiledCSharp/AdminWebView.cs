using UnityEngine;

public static class AdminWebView
{
	public static bool Supported => false;

	public static void Open(string url)
	{
		Application.OpenURL(url);
	}

	public static void Close()
	{
	}
}

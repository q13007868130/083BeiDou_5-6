public static class NativeSystemMessage
{
	private const string NoticePrefix = "[Notice] ";

	public static void Drop(int type, string text)
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		if (!string.IsNullOrEmpty(text))
		{
			switch (type)
			{
			case 5:
				BottomBar.AddChat(text, BottomBar.ChatPink);
				break;
			case 1:
				NativeUtilPrompt.Notice(text);
				break;
			case 0:
				BottomBar.AddChat("[Notice] " + text, BottomBar.ChatLightBlue);
				break;
			case 6:
				BottomBar.AddChat(text, BottomBar.ChatLightBlue);
				break;
			case 12:
				BottomBar.AddChat(text, BottomBar.ChatYellow);
				break;
			case 8:
				BottomBar.AddChat(text, BottomBar.ChatWhite);
				break;
			default:
				BottomBar.AddChat(text, BottomBar.ChatWhite);
				break;
			}
		}
	}

	public static void DropMap(int type, string text)
	{
		Drop(type, text);
	}
}

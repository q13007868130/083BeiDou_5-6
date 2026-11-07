public static class HelpWindow
{
	public static bool Visible;

	public static void Toggle()
	{
		Visible = !Visible;
	}

	public static void Draw()
	{
		if (Visible)
		{
			Visible = false;
			if (!NpcSession.Blocking)
			{
				NpcSession.Talk(9900001);
			}
		}
	}
}

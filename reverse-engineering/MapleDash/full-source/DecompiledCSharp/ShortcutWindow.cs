public static class ShortcutWindow
{
	public static bool Visible
	{
		get
		{
			return BottomMenu.IsOpen(1);
		}
		set
		{
			if (value)
			{
				BottomMenu.Show(1);
			}
			else if (Visible)
			{
				BottomMenu.Close();
			}
		}
	}

	public static void Toggle()
	{
		BottomMenu.Toggle(1);
	}

	public static void Draw()
	{
		BottomMenu.Draw(1);
	}
}

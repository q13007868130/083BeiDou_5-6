public static class GameMenuWindow
{
	public static bool Visible
	{
		get
		{
			return BottomMenu.IsOpen(0);
		}
		set
		{
			if (value)
			{
				BottomMenu.Show(0);
			}
			else if (Visible)
			{
				BottomMenu.Close();
			}
		}
	}

	public static void Toggle()
	{
		BottomMenu.Toggle(0);
	}

	public static void Draw()
	{
		BottomMenu.Draw(0);
	}
}

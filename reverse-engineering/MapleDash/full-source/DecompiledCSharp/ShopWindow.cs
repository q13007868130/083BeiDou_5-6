public static class ShopWindow
{
	public static bool Visible;

	public static void Toggle()
	{
		NativeCashWindow.Toggle();
	}

	public static void Draw()
	{
		NativeCashWindow.Draw();
	}
}

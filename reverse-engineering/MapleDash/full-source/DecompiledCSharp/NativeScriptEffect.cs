public static class NativeScriptEffect
{
	public static string Last { get; private set; }

	public static void Play(string path)
	{
		if (!string.IsNullOrEmpty(path))
		{
			Last = path;
			if (!NativeBossVideoEffect.Handle(path))
			{
				NativeScreenEffect.Play("Map/Effect.img/" + path);
			}
		}
	}

	public static void Reset()
	{
		Last = null;
	}
}

public static class CharNameRules
{
	public const int MinLength = 2;

	public const int MaxLength = 12;

	public static bool Valid(string name)
	{
		if (string.IsNullOrEmpty(name) || name.Length < 2 || name.Length > 12)
		{
			return false;
		}
		foreach (char c in name)
		{
			if ((c < 'a' || c > 'z') && (c < 'A' || c > 'Z') && (c < '0' || c > '9') && (c < '一' || c > '龥'))
			{
				return false;
			}
		}
		return true;
	}
}

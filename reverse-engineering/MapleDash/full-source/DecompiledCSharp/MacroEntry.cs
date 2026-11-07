public sealed class MacroEntry
{
	public int position;

	public string name = "";

	public int shout;

	public int skill1;

	public int skill2;

	public int skill3;

	public bool IsEmpty
	{
		get
		{
			if (string.IsNullOrEmpty(name) && skill1 == 0 && skill2 == 0)
			{
				return skill3 == 0;
			}
			return false;
		}
	}
}

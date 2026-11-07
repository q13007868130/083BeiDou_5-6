public sealed class FieldPortal
{
	public int id = -1;

	public string name;

	public string targetName;

	public string script;

	public int type;

	public int tm = 999999999;

	public float x;

	public float y;

	public bool enabled = true;

	public bool IsSpawn => type == 0;

	public bool Warps
	{
		get
		{
			if (tm >= 0)
			{
				return tm < 999999999;
			}
			return false;
		}
	}

	public bool HasScript => !string.IsNullOrEmpty(script);

	public bool TouchTrigger
	{
		get
		{
			if (type != 3 && type != 9 && type != 12)
			{
				return type == 13;
			}
			return true;
		}
	}

	public bool PressUpTrigger
	{
		get
		{
			if (type != 1 && type != 2 && type != 4 && type != 5 && type != 6 && type != 7 && type != 8 && type != 10)
			{
				return type == 11;
			}
			return true;
		}
	}

	public bool Usable
	{
		get
		{
			if (enabled && (Warps || HasScript))
			{
				if (!TouchTrigger)
				{
					return PressUpTrigger;
				}
				return true;
			}
			return false;
		}
	}
}

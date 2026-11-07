public sealed class VMatrixCore
{
	public int id;

	public int type;

	public int maxLevel;

	public int expireDays;

	public bool selfOpen;

	public string name = "";

	public string desc = "";

	public int[] skills = new int[0];

	public int[] subSkills = new int[0];

	public int[] cooldownSkills = new int[0];

	public int[] removalSkills = new int[0];

	public int[] equipRemovalSkills = new int[0];

	public string[] jobs = new string[0];

	public string special = "";

	public bool IsSpecial => type == 2;

	public bool IsBoost => type == 1;
}

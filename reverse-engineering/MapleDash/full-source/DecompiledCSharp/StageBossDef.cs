using System.Collections.Generic;

public class StageBossDef
{
	public string id = "";

	public string name = "";

	public string type = "";

	public string hint = "";

	public float hp = 900f;

	public float spd = 46f;

	public float scale = 3f;

	public float contact = 16f;

	public SlamDef slam = new SlamDef();

	public SummonDef summon = new SummonDef();

	public List<PhaseDef> phases = new List<PhaseDef>();

	public List<string> pattern = new List<string>();
}

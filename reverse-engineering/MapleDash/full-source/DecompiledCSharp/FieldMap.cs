using System.Collections.Generic;

public sealed class FieldMap
{
	public int id;

	public int returnMap;

	public int fieldLimit;

	public int fieldType;

	public int createMobInterval = 5000;

	public int decHp;

	public int protectItem;

	public int forcedReturn = 999999999;

	public int timeLimit = -1;

	public int timeMobId = -1;

	public string timeMobMessage;

	public string onUserEnter;

	public string onFirstUserEnter;

	public bool everlast;

	public string name;

	public string street;

	public string bgm;

	public bool town;

	public float mobRate;

	public int[] vr = new int[4];

	public readonly List<FieldFoothold> footholds = new List<FieldFoothold>();

	public readonly List<FieldRope> ropes = new List<FieldRope>();

	public readonly List<FieldPortal> portals = new List<FieldPortal>();

	public readonly List<FieldSpawn> spawns = new List<FieldSpawn>();

	public readonly List<FieldNpc> npcs = new List<FieldNpc>();

	public readonly List<FieldBack> backs = new List<FieldBack>();

	public readonly List<FieldChunk> chunks = new List<FieldChunk>();

	public int Width => vr[2] - vr[0];

	public int Height => vr[3] - vr[1];

	public float ViewMinX => WorldX(vr[0]);

	public float ViewMaxX => WorldX(vr[2]);

	public float ViewMinY => WorldY(vr[3]);

	public float ViewMaxY => WorldY(vr[1]);

	public FieldPortal SpawnPortal
	{
		get
		{
			foreach (FieldPortal portal in portals)
			{
				if (portal.type == 0 && portal.name == "sp")
				{
					return portal;
				}
			}
			foreach (FieldPortal portal2 in portals)
			{
				if (portal2.type == 0)
				{
					return portal2;
				}
			}
			return null;
		}
	}

	public FieldPortal FirstPortal
	{
		get
		{
			if (portals.Count <= 0)
			{
				return null;
			}
			return portals[0];
		}
	}

	public float WorldX(float mapX)
	{
		return mapX / 40f;
	}

	public float WorldY(float mapY)
	{
		return (0f - mapY) / 40f;
	}

	public FieldPortal Portal(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		foreach (FieldPortal portal in portals)
		{
			if (portal.name == name)
			{
				return portal;
			}
		}
		return null;
	}
}

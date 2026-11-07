using System;
using System.Collections.Generic;

namespace Server.Field;

public sealed class MapRuntime
{
	public int mapId;

	public int templateMapId;

	public int returnMap;

	public int fieldLimit;

	public int left;

	public int right;

	public int top;

	public int bottom;

	public bool swim;

	public bool everlast;

	public float footholdSpeed = 1f;

	public readonly List<MapRope> ropes = new List<MapRope>();

	public readonly List<MapPortal> portals = new List<MapPortal>();

	public readonly List<MapNpcPosition> npcs = new List<MapNpcPosition>();

	public readonly HashSet<int> removedNpcs = new HashSet<int>();

	public readonly List<MapReactorSpawn> reactors = new List<MapReactorSpawn>();

	public readonly List<FootholdSegment> footholds = new List<FootholdSegment>();

	public float minX = float.MaxValue;

	public float maxX = float.MinValue;

	public float minY = float.MaxValue;

	public float maxY = float.MinValue;

	public MapNpcPosition GetNpc(int npcId)
	{
		return npcs.Find((MapNpcPosition n) => n.id == npcId);
	}

	public bool RemoveNpc(int npcId)
	{
		removedNpcs.Add(npcId);
		return npcs.RemoveAll((MapNpcPosition n) => n.id == npcId) > 0;
	}

	public void AddFoothold(float x1, float y1, float x2, float y2)
	{
		footholds.Add(new FootholdSegment(x1, y1, x2, y2));
		minX = Math.Min(minX, Math.Min(x1, x2));
		maxX = Math.Max(maxX, Math.Max(x1, x2));
		minY = Math.Min(minY, Math.Min(y1, y2));
		maxY = Math.Max(maxY, Math.Max(y1, y2));
	}

	public float? PointBelow(float x, float yGuess)
	{
		float? result = null;
		foreach (FootholdSegment foothold in footholds)
		{
			if (foothold.x1 == foothold.x2)
			{
				continue;
			}
			float num = Math.Min(foothold.x1, foothold.x2);
			float num2 = Math.Max(foothold.x1, foothold.x2);
			if (!(x < num) && !(x > num2))
			{
				float num3 = ((foothold.x2 == foothold.x1) ? 0f : ((x - foothold.x1) / (foothold.x2 - foothold.x1)));
				float num4 = foothold.y1 + (foothold.y2 - foothold.y1) * num3;
				if (num4 >= yGuess && (!result.HasValue || num4 < result.Value))
				{
					result = num4;
				}
			}
		}
		return result;
	}

	public float? SnapToGroundRaw(float x, float y)
	{
		float? result = PointBelow(x, y - 12f);
		if (!result.HasValue && footholds.Count > 0)
		{
			result = PointBelow(x, minY);
		}
		return result;
	}
}

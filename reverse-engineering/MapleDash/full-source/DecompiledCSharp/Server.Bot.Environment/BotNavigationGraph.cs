using System;
using System.Collections.Generic;
using Server.Field;

namespace Server.Bot.Environment;

public sealed class BotNavigationGraph
{
	public enum EdgeType
	{
		Walk,
		Jump,
		Drop,
		Climb,
		Portal
	}

	public readonly struct Point(int x, int y)
	{
		public readonly int x = x;

		public readonly int y = y;
	}

	public sealed class Segment
	{
		public readonly int footholdId;

		public readonly int x1;

		public readonly int y1;

		public readonly int x2;

		public readonly int y2;

		public readonly int minX;

		public readonly int maxX;

		public readonly bool forbidFallDown;

		public readonly bool collidableFromBelow;

		public Segment(FootholdSegment foothold, bool collidableFromBelow = false)
		{
			footholdId = foothold.id;
			x1 = (int)foothold.x1;
			y1 = (int)foothold.y1;
			x2 = (int)foothold.x2;
			y2 = (int)foothold.y2;
			minX = Math.Min(x1, x2);
			maxX = Math.Max(x1, x2);
			forbidFallDown = foothold.forbidFallDown;
			this.collidableFromBelow = collidableFromBelow;
		}

		public bool ContainsX(int x)
		{
			if (x >= minX)
			{
				return x <= maxX;
			}
			return false;
		}

		public int ClampX(int x)
		{
			return Math.Max(minX, Math.Min(maxX, x));
		}

		public Point PointAt(int x)
		{
			int num = ClampX(x);
			if (x1 == x2)
			{
				return new Point(num, Math.Min(y1, y2));
			}
			double num2 = (double)(num - x1) / (double)(x2 - x1);
			return new Point(num, BotGroundPhysics.JavaRound((double)y1 + (double)(y2 - y1) * num2));
		}
	}

	public sealed class Region
	{
		public readonly int id;

		public readonly int minX;

		public readonly int maxX;

		public readonly int minY;

		public readonly int maxY;

		public readonly List<Segment> segments;

		public readonly bool isRopeRegion;

		public readonly bool isLadder;

		public int Width => Math.Max(0, maxX - minX);

		public int Height => Math.Max(0, maxY - minY);

		public Point Left => PointAt(minX);

		public Point Right => PointAt(maxX);

		public Point Center
		{
			get
			{
				if (!isRopeRegion)
				{
					return PointAt(minX + Width / 2);
				}
				return new Point(minX, minY + Height / 2);
			}
		}

		public Region(int id, List<Segment> segments)
		{
			if (segments.Count == 0)
			{
				throw new ArgumentException("Bot nav region requires at least one segment");
			}
			this.id = id;
			this.segments = new List<Segment>(segments);
			minX = (minY = int.MaxValue);
			maxX = (maxY = int.MinValue);
			foreach (Segment segment in segments)
			{
				minX = Math.Min(minX, segment.minX);
				maxX = Math.Max(maxX, segment.maxX);
				minY = Math.Min(minY, Math.Min(segment.y1, segment.y2));
				maxY = Math.Max(maxY, Math.Max(segment.y1, segment.y2));
			}
		}

		public Region(int id, int ropeX, int topY, int bottomY, bool ladder)
		{
			this.id = id;
			minX = (maxX = ropeX);
			minY = topY;
			maxY = bottomY;
			isRopeRegion = true;
			isLadder = ladder;
			segments = new List<Segment>();
		}

		public Point PointAt(int x)
		{
			if (!isRopeRegion)
			{
				return BestSegment(x).PointAt(x);
			}
			return new Point(minX, minY + Height / 2);
		}

		public bool ForbidFallDownAt(int x)
		{
			if (!isRopeRegion && segments.Count > 0)
			{
				return BestSegment(x).forbidFallDown;
			}
			return false;
		}

		private Segment BestSegment(int x)
		{
			Segment segment = segments[0];
			int num = Distance(segment, x);
			for (int i = 1; i < segments.Count; i++)
			{
				int num2 = Distance(segments[i], x);
				if (num2 < num)
				{
					segment = segments[i];
					num = num2;
				}
			}
			return segment;
		}

		private static int Distance(Segment s, int x)
		{
			if (!s.ContainsX(x))
			{
				if (x >= s.minX)
				{
					return x - s.maxX;
				}
				return s.minX - x;
			}
			return 0;
		}
	}

	public sealed class Edge
	{
		public readonly int fromRegionId;

		public readonly int toRegionId;

		public readonly int launchMinX;

		public readonly int launchMaxX;

		public readonly int launchStepX;

		public readonly int portalId;

		public readonly int ropeX;

		public readonly int ropeTopY;

		public readonly int ropeBottomY;

		public readonly int cost;

		public readonly EdgeType type;

		public readonly Point startPoint;

		public readonly Point endPoint;

		public Edge(int from, int to, EdgeType type, Point start, Point end, int launchMin, int launchMax, int step, int portalId, int ropeX, int ropeTop, int ropeBottom, int cost)
		{
			fromRegionId = from;
			toRegionId = to;
			this.type = type;
			startPoint = start;
			endPoint = end;
			launchMinX = Math.Min(launchMin, launchMax);
			launchMaxX = Math.Max(launchMin, launchMax);
			launchStepX = step;
			this.portalId = portalId;
			this.ropeX = ropeX;
			ropeTopY = ropeTop;
			ropeBottomY = ropeBottom;
			this.cost = cost;
		}

		public Edge(int from, int to, EdgeType type, Point start, Point end, int step, int portalId, int ropeX, int ropeTop, int ropeBottom, int cost)
			: this(from, to, type, start, end, start.x, start.x, step, portalId, ropeX, ropeTop, ropeBottom, cost)
		{
		}

		public bool ContainsLaunchX(int x, int tolerance = 0)
		{
			if (x >= launchMinX - tolerance)
			{
				return x <= launchMaxX + tolerance;
			}
			return false;
		}

		public Point NearestLaunch(int x)
		{
			return new Point(Math.Max(launchMinX, Math.Min(launchMaxX, x)), startPoint.y);
		}
	}

	public readonly int mapId;

	public readonly int version;

	public readonly BotMovementProfile profile;

	public readonly List<Region> regions;

	public readonly Dictionary<int, Region> regionsById;

	public readonly Dictionary<int, int> regionByFoothold;

	public readonly Dictionary<int, List<Edge>> outgoing;

	public readonly HashSet<int> collidableWalls;

	public readonly HashSet<int> collidableFromBelow;

	public BotNavigationGraph(int mapId, int version, BotMovementProfile profile, List<Region> regions, Dictionary<int, Region> byId, Dictionary<int, int> byFoothold, Dictionary<int, List<Edge>> outgoing, HashSet<int> walls, HashSet<int> fromBelow = null)
	{
		this.mapId = mapId;
		this.version = version;
		this.profile = profile;
		this.regions = new List<Region>(regions);
		regionsById = new Dictionary<int, Region>(byId);
		regionByFoothold = new Dictionary<int, int>(byFoothold);
		this.outgoing = new Dictionary<int, List<Edge>>();
		foreach (KeyValuePair<int, List<Edge>> item in outgoing)
		{
			this.outgoing[item.Key] = new List<Edge>(item.Value);
		}
		collidableWalls = new HashSet<int>(walls);
		collidableFromBelow = ((fromBelow == null) ? new HashSet<int>() : new HashSet<int>(fromBelow));
	}

	public Region GetRegion(int id)
	{
		if (!regionsById.TryGetValue(id, out var value))
		{
			return null;
		}
		return value;
	}

	public IReadOnlyList<Edge> Outgoing(int id)
	{
		if (!outgoing.TryGetValue(id, out var value))
		{
			return Array.Empty<Edge>();
		}
		return value;
	}

	public bool HasInterRegionEdge(int from, int to)
	{
		foreach (Edge item in Outgoing(from))
		{
			if (item.fromRegionId != item.toRegionId && item.toRegionId == to)
			{
				return true;
			}
		}
		return false;
	}

	public HashSet<int> MutualAdjacent(int id)
	{
		HashSet<int> hashSet = new HashSet<int>();
		foreach (Edge item in Outgoing(id))
		{
			if (item.fromRegionId != item.toRegionId && HasInterRegionEdge(item.toRegionId, id))
			{
				hashSet.Add(item.toRegionId);
			}
		}
		return hashSet;
	}

	public int RopeRegion(Point position)
	{
		foreach (Region region in regions)
		{
			if (region.isRopeRegion && Math.Abs(position.x - region.minX) <= 8 && position.y >= region.minY && position.y <= region.maxY)
			{
				return region.id;
			}
		}
		return -1;
	}

	public int FindRegionId(BotGroundIndex ground, Point position)
	{
		if (ground == null)
		{
			return -1;
		}
		FootholdSegment footholdSegment = ground.Ground(position.x, position.y);
		if (footholdSegment != null && regionByFoothold.TryGetValue(footholdSegment.id, out var value) && value >= 0)
		{
			return value;
		}
		return RopeRegion(position);
	}
}

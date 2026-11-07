using System;
using Server.Field;

public static class NativeTeleportMovement
{
	public static bool IsSkill(int id)
	{
		if (id != 2101002 && id != 2201002 && id != 2301001 && id != 12101003)
		{
			return id == 22101001;
		}
		return true;
	}

	public static bool Destination(MapRuntime map, int x, int y, int horizontal, int vertical, int range, out int targetX, out int targetY)
	{
		targetX = x;
		targetY = y;
		if (map == null || (map.fieldLimit & 2) != 0 || range <= 0)
		{
			return false;
		}
		horizontal = Math.Sign(horizontal);
		vertical = Math.Sign(vertical);
		int num = x + horizontal * range;
		if ((horizontal == 0 && vertical == 0) || num < map.left || num > map.right)
		{
			return false;
		}
		FootholdSegment footholdSegment = null;
		FootholdSegment footholdSegment2 = null;
		int num2 = int.MaxValue;
		int num3 = int.MaxValue;
		int num4 = y;
		foreach (FootholdSegment foothold in map.footholds)
		{
			if (!(foothold.x1 >= foothold.x2) && !((float)x < foothold.x1) && !((float)x > foothold.x2))
			{
				int num5 = Math.Abs(Height(foothold, x) - y);
				if (num5 < num2)
				{
					footholdSegment = foothold;
					num2 = num5;
				}
			}
		}
		bool flag = map.bottom > map.top;
		foreach (FootholdSegment foothold2 in map.footholds)
		{
			if (foothold2.x1 >= foothold2.x2 || (float)num < foothold2.x1 || (float)num > foothold2.x2)
			{
				continue;
			}
			int num6 = Height(foothold2, num);
			if (flag && (num6 < map.top || num6 > map.bottom))
			{
				continue;
			}
			int num7;
			if (horizontal != 0)
			{
				num7 = Math.Abs(num6 - y);
				if (num7 > 80)
				{
					continue;
				}
			}
			else if (vertical < 0)
			{
				if (num6 < y - range || num6 >= y || foothold2 == footholdSegment)
				{
					continue;
				}
				num7 = num6 - (y - range);
			}
			else
			{
				if (num6 <= y || num6 > y + range * 2 || foothold2 == footholdSegment)
				{
					continue;
				}
				num7 = ((num6 <= y + range) ? (y + range - num6) : (range + num6 - (y + range)));
			}
			if (num7 < num3)
			{
				footholdSegment2 = foothold2;
				num3 = num7;
				num4 = num6;
			}
		}
		if (footholdSegment2 == null)
		{
			return false;
		}
		targetX = num;
		targetY = num4 - 1;
		return true;
	}

	private static int Height(FootholdSegment f, int x)
	{
		return (int)((double)f.y1 + (double)(f.y2 - f.y1) * ((double)((float)x - f.x1) / (double)(f.x2 - f.x1)));
	}
}

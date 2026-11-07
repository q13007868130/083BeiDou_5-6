using System;
using Server.Bot.Social;

namespace Server.Bot;

public static class BotAttack
{
	public enum PacketKind
	{
		CLOSE,
		RANGED,
		MAGIC
	}

	public readonly struct Skill(int id, int level, int hits, PacketKind kind)
	{
		public readonly int id = id;

		public readonly int level = level;

		public readonly int hits = hits;

		public readonly PacketKind kind = kind;
	}

	public static int ActionFor(int weaponId, Random random)
	{
		int[] array;
		switch (weaponId / 10000)
		{
		default:
			array = new int[5] { 16, 17, 5, 6, 7 };
			break;
		case 149:
			array = new int[2] { 13, 20 };
			break;
		case 146:
			array = new int[2] { 9, 19 };
			break;
		case 145:
			array = new int[2] { 9, 11 };
			break;
		case 147:
			array = new int[3] { 24, 25, 26 };
			break;
		case 137:
		case 138:
			array = new int[2] { 28, 29 };
			break;
		case 143:
		case 144:
			array = new int[2] { 13, 19 };
			break;
		case 140:
		case 141:
		case 142:
			array = new int[5] { 16, 17, 9, 10, 11 };
			break;
		}
		int[] array2 = array;
		return array2[random.Next(array2.Length)];
	}

	public static string PoseFor(int action)
	{
		switch (action)
		{
		case 5:
		case 24:
		case 28:
			return "swingO1";
		case 6:
		case 25:
			return "swingO2";
		case 7:
		case 26:
		case 29:
			return "swingO3";
		case 9:
			return "swingT1";
		case 10:
			return "swingT2";
		case 11:
			return "swingT3";
		case 13:
			return "swingP1";
		case 14:
			return "swingP2";
		case 16:
			return "stabO1";
		case 17:
			return "stabO2";
		case 19:
			return "stabT1";
		case 20:
			return "stabT2";
		default:
			throw new ArgumentOutOfRangeException("action");
		}
	}

	public static void BasicSwing(BotCharacter actor, Random random = null, BotOutput output = null)
	{
		if (actor != null)
		{
			actor.equips.TryGetValue(-11, out var value);
			int action = ActionFor(value, random ?? new Random());
			(output ?? BotTypeManager.Output).Send(BotOutput.Kind.Attack, actor, PoseFor(action), 4);
		}
	}

	public static int LineDamage(int jobTier, int level, long monsterMaxHp, Random random)
	{
		int[] array = new int[5] { 80, 150, 320, 700, 1200 };
		int[] array2 = new int[5] { 160, 360, 800, 1800, 3200 };
		int num = Math.Max(0, Math.Min(4, jobTier));
		int val = (int)(long)Math.Floor((double)(array[num] + random.Next(array2[num] - array[num] + 1)) * (1.0 + (double)Math.Max(0, level) / 80.0) + 0.5);
		int val2 = (int)((monsterMaxHp <= 0) ? 1 : Math.Min(99999L, Math.Max(1L, monsterMaxHp / 18)));
		return Math.Min(99999, Math.Max(val, val2));
	}

	public static int JobTier(int job)
	{
		if (job == 0)
		{
			return 0;
		}
		switch (job % 100)
		{
		case 0:
			return 1;
		case 10:
		case 20:
		case 30:
			return 2;
		case 11:
		case 21:
		case 31:
			return 3;
		case 12:
		case 22:
		case 32:
			return 4;
		case 13:
		case 23:
		case 33:
			return 4;
		default:
			return 0;
		}
	}

	private static bool Melee(int weapon)
	{
		if (weapon >= 130 && weapon <= 149 && weapon != 134 && weapon != 135 && weapon != 136 && weapon != 139 && weapon != 137 && weapon != 138 && weapon != 145 && weapon != 146 && weapon != 147)
		{
			return weapon != 149;
		}
		return false;
	}

	private static bool Supports(int weapon, Skill skill)
	{
		if (skill.id <= 0 || weapon < 130 || weapon > 149)
		{
			return false;
		}
		if (skill.kind == PacketKind.MAGIC)
		{
			if (weapon != 137)
			{
				return weapon == 138;
			}
			return true;
		}
		if (skill.kind == PacketKind.RANGED)
		{
			switch (skill.id)
			{
			case 4001344:
			case 4111005:
			case 4121007:
			case 14110005:
				return weapon == 147;
			case 5001003:
			case 5210000:
			case 5221007:
				return weapon == 149;
			case 3201005:
			case 3211006:
				return weapon == 146;
			case 3001004:
				if (weapon != 145)
				{
					return weapon == 146;
				}
				return true;
			default:
				return weapon == 145;
			}
		}
		switch (skill.id)
		{
		case 1311001:
			if (weapon != 143)
			{
				return weapon == 144;
			}
			return true;
		case 4001334:
		case 4201005:
		case 4211004:
		case 4221007:
			return weapon == 133;
		case 5001002:
		case 5101003:
		case 5111002:
		case 5121007:
		case 15101005:
		case 15111004:
			return weapon == 148;
		default:
			return Melee(weapon);
		}
	}

	public static Skill GrindSkill(int job, int weaponId)
	{
		int num = weaponId / 10000;
		Skill skill = Preferred(job);
		if (Supports(num, skill))
		{
			return skill;
		}
		Skill skill2 = new Skill(0, 0, 1, PacketKind.CLOSE);
		if ((IsA(job, 200) || IsA(job, 1200)) && (num == 137 || num == 138))
		{
			skill2 = new Skill(2001005, 20, 2, PacketKind.MAGIC);
		}
		else if (IsA(job, 300) || IsA(job, 1300))
		{
			switch (num)
			{
			case 145:
				skill2 = new Skill(3001004, 20, 2, PacketKind.RANGED);
				break;
			case 146:
				skill2 = new Skill(3201005, 20, 1, PacketKind.RANGED);
				break;
			}
		}
		else if (IsA(job, 400) || IsA(job, 1400))
		{
			switch (num)
			{
			case 147:
				skill2 = new Skill(4001344, 20, 2, PacketKind.RANGED);
				break;
			case 133:
				skill2 = new Skill(4001334, 20, 2, PacketKind.CLOSE);
				break;
			}
		}
		else if (IsA(job, 500) || IsA(job, 1500))
		{
			switch (num)
			{
			case 149:
				skill2 = new Skill(5001003, 20, 2, PacketKind.RANGED);
				break;
			case 148:
				skill2 = new Skill(5001002, 20, 1, PacketKind.CLOSE);
				break;
			}
		}
		else if ((IsA(job, 100) || IsA(job, 1100)) && Melee(num))
		{
			skill2 = new Skill(1001005, 20, 1, PacketKind.CLOSE);
		}
		if (!Supports(num, skill2))
		{
			return new Skill(0, 0, 1, PacketKind.CLOSE);
		}
		return skill2;
	}

	public static int[] SplitDamage(int total, int hitCount)
	{
		int num = Math.Max(1, hitCount);
		int val = Math.Max(1, total / num);
		int num2 = total;
		int[] array = new int[num];
		for (int i = 0; i < num; i++)
		{
			int num3 = ((i == num - 1) ? num2 : Math.Min(val, num2));
			array[i] = Math.Max(1, num3);
			num2 -= num3;
		}
		return array;
	}

	public static (int x, int y) EngageRange(int job, int weaponId)
	{
		if (GrindSkill(job, weaponId).kind != PacketKind.CLOSE)
		{
			return (x: 380, y: 120);
		}
		return (x: 70, y: 50);
	}

	private static bool IsA(int job, int parent)
	{
		if (job / 10 != parent / 10 || job < parent)
		{
			if (parent / 10 % 10 == 0)
			{
				return job / 100 == parent / 100;
			}
			return false;
		}
		return true;
	}

	private static Skill Preferred(int job)
	{
		if (job < 0)
		{
			return new Skill(0, 0, 1, PacketKind.CLOSE);
		}
		switch (job)
		{
		case 100:
		case 110:
		case 120:
		case 130:
			return new Skill(1001005, 20, 1, PacketKind.CLOSE);
		case 111:
			return new Skill(1111005, 30, 1, PacketKind.CLOSE);
		default:
			if (IsA(job, 110) || IsA(job, 1100))
			{
				return new Skill(IsA(job, 1100) ? 11111004 : 1121008, 30, 2, PacketKind.CLOSE);
			}
			if (job == 121)
			{
				return new Skill(1211002, 30, 1, PacketKind.CLOSE);
			}
			if (IsA(job, 120))
			{
				return new Skill(1221009, 30, 1, PacketKind.CLOSE);
			}
			if (job == 131)
			{
				return new Skill(1311001, 30, 3, PacketKind.CLOSE);
			}
			if (IsA(job, 130))
			{
				return new Skill(1311001, 30, 3, PacketKind.CLOSE);
			}
			switch (job)
			{
			case 200:
				return new Skill(2001005, 20, 2, PacketKind.MAGIC);
			case 210:
			case 1210:
				return new Skill(2101004, 30, 1, PacketKind.MAGIC);
			case 211:
			case 1211:
				return new Skill((job == 1211) ? 12111006 : 2111002, 30, 1, PacketKind.MAGIC);
			default:
				if (IsA(job, 210) || IsA(job, 1200))
				{
					return new Skill(IsA(job, 1200) ? 12111003 : 2121007, 30, 1, PacketKind.MAGIC);
				}
				switch (job)
				{
				case 220:
					return new Skill(2201004, 30, 1, PacketKind.MAGIC);
				case 221:
					return new Skill(2211002, 30, 1, PacketKind.MAGIC);
				default:
					if (IsA(job, 220))
					{
						return new Skill(2221006, 30, 1, PacketKind.MAGIC);
					}
					switch (job)
					{
					case 230:
						return new Skill(2301005, 30, 1, PacketKind.MAGIC);
					case 231:
						return new Skill(2311004, 30, 1, PacketKind.MAGIC);
					default:
						if (IsA(job, 230))
						{
							return new Skill(2321007, 30, 1, PacketKind.MAGIC);
						}
						switch (job)
						{
						case 300:
							return new Skill(3001004, 20, 2, PacketKind.RANGED);
						case 310:
						case 320:
							return new Skill((job == 310) ? 3101005 : 3201005, 30, 1, PacketKind.RANGED);
						case 311:
							return new Skill(3111006, 30, 4, PacketKind.RANGED);
						default:
							if (IsA(job, 310) || IsA(job, 1300))
							{
								return new Skill(IsA(job, 1300) ? 13111001 : 3111006, 30, 4, PacketKind.RANGED);
							}
							if (job == 321)
							{
								return new Skill(3211006, 30, 4, PacketKind.RANGED);
							}
							if (IsA(job, 320))
							{
								return new Skill(3211006, 30, 4, PacketKind.RANGED);
							}
							switch (job)
							{
							case 400:
								return new Skill(4001334, 20, 2, PacketKind.CLOSE);
							case 410:
								return new Skill(4001344, 20, 2, PacketKind.RANGED);
							case 411:
								return new Skill(4111005, 30, 1, PacketKind.RANGED);
							default:
								if (IsA(job, 410) || IsA(job, 1400))
								{
									return new Skill(IsA(job, 1400) ? 14110005 : 4121007, 30, 3, PacketKind.RANGED);
								}
								switch (job)
								{
								case 420:
									return new Skill(4201005, 30, 6, PacketKind.CLOSE);
								case 421:
									return new Skill(4211004, 30, 1, PacketKind.CLOSE);
								default:
									if (IsA(job, 420))
									{
										return new Skill(4221007, 30, 2, PacketKind.CLOSE);
									}
									switch (job)
									{
									case 500:
										return new Skill(5001002, 20, 1, PacketKind.CLOSE);
									case 510:
									case 1510:
										return new Skill((job == 1510) ? 15101005 : 5101003, 30, 1, PacketKind.CLOSE);
									case 511:
									case 1511:
										return new Skill((job == 1511) ? 15111004 : 5111002, 30, (job != 1511) ? 1 : 6, PacketKind.CLOSE);
									default:
										if (IsA(job, 510) || IsA(job, 1500))
										{
											return new Skill(IsA(job, 1500) ? 15111004 : 5121007, 30, 6, PacketKind.CLOSE);
										}
										switch (job)
										{
										case 520:
											return new Skill(5001003, 20, 2, PacketKind.RANGED);
										case 521:
											return new Skill(5210000, 30, 3, PacketKind.RANGED);
										default:
											if (IsA(job, 520))
											{
												return new Skill(5221007, 30, 4, PacketKind.RANGED);
											}
											return new Skill(0, 0, 1, PacketKind.CLOSE);
										}
									}
								}
							}
						}
					}
				}
			}
		}
	}
}

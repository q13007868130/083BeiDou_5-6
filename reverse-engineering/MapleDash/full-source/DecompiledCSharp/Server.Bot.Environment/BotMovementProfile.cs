using System;
using System.Collections.Generic;

namespace Server.Bot.Environment;

public sealed class BotMovementProfile : IEquatable<BotMovementProfile>
{
	public static readonly BotMovementProfile Base = new BotMovementProfile(100, 100);

	public readonly int speed;

	public readonly int jump;

	public readonly bool snowShoes;

	public double SpeedMultiplier => (double)speed / 100.0;

	public double JumpMultiplier => (double)jump / 100.0;

	public BotMovementProfile(int speed, int jump, bool snowShoes = false)
	{
		this.speed = Math.Min(Bucket(speed), 200);
		this.jump = Math.Min(Bucket(jump), 123);
		this.snowShoes = snowShoes;
	}

	private static int Bucket(int stat)
	{
		stat = Math.Max(1, stat);
		if (stat >= 5)
		{
			return (int)(Math.Floor((double)stat / 5.0 + 0.5) * 5.0);
		}
		return stat;
	}

	private static bool HasHaste(int job)
	{
		if (job / 10 != 41)
		{
			return job / 10 == 42;
		}
		return true;
	}

	public static BotMovementProfile FromCharacter(BotActor actor, bool forceBaseMovement, IEnumerable<BotActor> partyMembers = null, bool snowShoes = false)
	{
		if ((actor == null) | forceBaseMovement)
		{
			return Base;
		}
		int level = actor.level;
		int val;
		int num;
		if (HasHaste(actor.job))
		{
			val = ((level >= 55) ? 155 : 150);
			num = ((level >= 55) ? 123 : 115);
		}
		else if (level <= 9)
		{
			val = 115;
			num = 100;
		}
		else if (level <= 29)
		{
			val = 125;
			num = 105;
		}
		else if (level <= 50)
		{
			val = 130;
			num = 110;
		}
		else if (level <= 69)
		{
			val = 135;
			num = 115;
		}
		else if (level <= 100)
		{
			val = 145;
			num = 120;
		}
		else
		{
			val = 155;
			num = 123;
		}
		if (partyMembers != null)
		{
			foreach (BotActor partyMember in partyMembers)
			{
				if (partyMember != null && partyMember.id != actor.id && partyMember.online && partyMember.level >= 60 && HasHaste(partyMember.job))
				{
					val = Math.Max(val, 155);
					break;
				}
			}
		}
		return new BotMovementProfile(val, num, snowShoes);
	}

	public double WalkVelocity(double baseVelocity)
	{
		return baseVelocity * SpeedMultiplier;
	}

	public double HorizontalForce(double baseForce)
	{
		return baseForce * SpeedMultiplier;
	}

	public float JumpSpeed(float baseSpeed)
	{
		return (float)((double)baseSpeed * JumpMultiplier);
	}

	public bool Equals(BotMovementProfile other)
	{
		if (other != null && speed == other.speed && jump == other.jump)
		{
			return snowShoes == other.snowShoes;
		}
		return false;
	}

	public override bool Equals(object other)
	{
		return Equals(other as BotMovementProfile);
	}

	public override int GetHashCode()
	{
		return (speed * 31 + jump) * 31 + (snowShoes ? 1 : 0);
	}
}

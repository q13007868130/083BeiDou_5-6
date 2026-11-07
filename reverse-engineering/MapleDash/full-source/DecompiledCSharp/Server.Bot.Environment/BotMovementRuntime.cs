using System;
using System.IO;
using Server.Field;

namespace Server.Bot.Environment;

public sealed class BotMovementRuntime
{
	public readonly BotCharacter actor;

	public readonly MapRuntime map;

	public readonly BotGroundIndex ground;

	public readonly BotWalkRegions regions;

	public readonly BotGroundPhysics physics;

	private readonly BotGroundMotion groundMotion;

	public readonly BotAirCollision collision;

	private readonly BotAirMotion airMotion;

	private readonly BotRopeMotion ropeMotion;

	private readonly BotSwimMotion swimMotion;

	public readonly BotMovementProfile profile;

	public BotGroundPhysics.Travel travel;

	public BotAirMotion.State air;

	public BotRopeMotion.State climb;

	public BotSwimMotion.State swim;

	public MapRope blockedRope;

	public MapRope pendingRope;

	public int pendingRopeY;

	public int moveDir;

	public int brakeDir;

	public int velocityX;

	public int velocityY;

	public int attackCooldownMs;

	public int jumpCooldownMs;

	public int unstuckCooldownMs;

	public int climbDirection;

	public bool wasMoving;

	public bool crouching;

	public bool downJumpPending;

	public bool climbUpIntent;

	public bool resting;

	public bool grinding;

	public bool preciseTarget;

	public long alertedUntilMs;

	public long nextSwimJumpMs;

	public BotNavigationGraph.Edge navEdge;

	public BotNavigationGraph.Point? navigationTarget;

	public Action<BotCharacter, float> ApplyFallDamage;

	public Action<BotCharacter, byte[]> Broadcast;

	public Func<bool> SinkReady;

	public Func<int, bool> IsObserved;

	private bool broadcastValid;

	private bool flashBroadcast;

	private (int, int, int, int, int, int) lastBroadcast;

	private int lastFoothold;

	public BotNavigationGraph.Point Position => new BotNavigationGraph.Point((int)actor.x, (int)actor.y);

	public bool InAir
	{
		get
		{
			if (air != null)
			{
				return air.airborne;
			}
			return false;
		}
	}

	public bool Climbing
	{
		get
		{
			if (climb != null)
			{
				return climb.climbing;
			}
			return false;
		}
	}

	public bool Swimming
	{
		get
		{
			if (swim != null)
			{
				return swim.swimming;
			}
			return false;
		}
	}

	private bool DirectionalDrop
	{
		get
		{
			if (navEdge != null && navEdge.type == BotNavigationGraph.EdgeType.Drop)
			{
				return navEdge.launchStepX != 0;
			}
			return false;
		}
	}

	public BotMovementRuntime(BotCharacter actor, MapRuntime map, BotMovementProfile profile = null)
	{
		this.actor = actor;
		this.map = map;
		this.profile = profile ?? BotMovementProfile.Base;
		MapGeometry mapGeometry = MapGeometryCache.Get(map);
		ground = mapGeometry.ground;
		regions = mapGeometry.regions;
		collision = mapGeometry.collision;
		physics = new BotGroundPhysics();
		groundMotion = new BotGroundMotion(map, ground, regions, collision, physics);
		airMotion = new BotAirMotion(map, collision, physics, this.profile);
		ropeMotion = new BotRopeMotion(ground);
		swimMotion = new BotSwimMotion(collision, physics, this.profile);
		Reset();
	}

	public void Reset()
	{
		air = null;
		climb = null;
		swim = null;
		pendingRope = null;
		blockedRope = null;
		travel = new BotGroundPhysics.Travel(actor.x);
		moveDir = (brakeDir = (velocityX = (velocityY = 0)));
		downJumpPending = (crouching = (climbUpIntent = (wasMoving = false)));
		attackCooldownMs = (jumpCooldownMs = (unstuckCooldownMs = 0));
		ClearNavigation();
		broadcastValid = false;
		SyncStance();
	}

	public void ClearNavigation()
	{
		navEdge = null;
		navigationTarget = null;
		preciseTarget = false;
	}

	public void Teleport(BotNavigationGraph.Point point)
	{
		SetPosition(point);
		Reset();
	}

	public static int TickDown(int timer)
	{
		return Math.Max(0, timer - 50);
	}

	public void Tick(BotNavigationGraph.Point? target, long now, bool runAi = true)
	{
		if (actor.mapId == map.mapId)
		{
			flashBroadcast = false;
			if (InAir)
			{
				BotAirMotion.AdvanceTimers(air);
			}
			if (Climbing)
			{
				TickClimb(target, runAi);
			}
			else if (map.swim && (InAir || Swimming))
			{
				TickSwim(target, now);
			}
			else if (InAir)
			{
				TickAir(target);
			}
			else
			{
				TickGround(target);
			}
			if (!flashBroadcast)
			{
				Emit(now);
			}
		}
	}

	public int CalcStep(int targetX, int stopDist = 30, int followDist = 80)
	{
		int num = targetX - Position.x;
		int num2 = Math.Abs(num);
		if (num2 <= stopDist || (!wasMoving && num2 <= followDist))
		{
			wasMoving = false;
			return 0;
		}
		wasMoving = true;
		int num3 = Math.Min(num2, physics.WalkStep(profile)) * Math.Sign(num);
		if (DirectionalDrop)
		{
			return num3;
		}
		int overshootSlack = 0;
		if (navEdge != null && (navEdge.type == BotNavigationGraph.EdgeType.Jump || (navEdge.type == BotNavigationGraph.EdgeType.Drop && navEdge.launchStepX == 0)) && navEdge.ContainsLaunchX(targetX))
		{
			overshootSlack = Math.Max(0, Math.Min(physics.WalkStep(profile), (num > 0) ? (navEdge.launchMaxX - targetX) : (targetX - navEdge.launchMinX)));
		}
		int num4 = physics.Approach(map.footholdSpeed, profile, travel.Speed, num, overshootSlack);
		if (num4 != Math.Sign(num3))
		{
			return num4;
		}
		return num3;
	}

	public static int PreciseStop(BotNavigationGraph.Edge edge)
	{
		if (edge == null || (edge.type != BotNavigationGraph.EdgeType.Jump && (edge.type != BotNavigationGraph.EdgeType.Drop || edge.launchStepX != 0)))
		{
			if (edge == null || edge.type == BotNavigationGraph.EdgeType.Walk)
			{
				return 4;
			}
			return 1;
		}
		return 0;
	}

	private void TickGround(BotNavigationGraph.Point? target)
	{
		if (travel.Speed == 0.0 && BotGroundPhysics.JavaRound(travel.X) != Position.x)
		{
			travel = new BotGroundPhysics.Travel(Position.x, 0.0, travel.CarryMs);
		}
		FootholdSegment footholdSegment = ground.Ground(Position.x, Position.y);
		if (footholdSegment == null || BotGroundIndex.YAt(footholdSegment, Position.x) - Position.y > 16 || BotGroundIndex.YAt(footholdSegment, Position.x) - Position.y < -26)
		{
			Fall(0);
			return;
		}
		if (pendingRope != null)
		{
			MapRope mapRope = pendingRope;
			if (Math.Abs(Position.x - mapRope.x) <= 10)
			{
				pendingRope = null;
				Attach(mapRope, pendingRopeY);
				return;
			}
			if (Math.Abs(Position.x - mapRope.x) > 40)
			{
				pendingRope = null;
				return;
			}
		}
		if (downJumpPending)
		{
			downJumpPending = false;
			crouching = false;
			if (!footholdSegment.forbidFallDown)
			{
				Launch(0, ropeJump: false, downJump: true);
			}
			return;
		}
		int num = ((!DirectionalDrop) ? (preciseTarget ? PreciseStop(navEdge) : 30) : 0);
		int followDist = ((!DirectionalDrop) ? ((navEdge != null || preciseTarget) ? num : 80) : 0);
		int num2 = (target.HasValue ? CalcStep(target.Value.x, num, followDist) : 0);
		if (num2 != 0 && !groundMotion.CanStep(Position, num2))
		{
			bool flag = collision.Wall(Position, new BotNavigationGraph.Point(Position.x + num2, Position.y), allowWalkableEndpoint: true).kind == BotAirCollision.Kind.Wall;
			if (flag || !DirectionalDrop || Math.Sign(num2) != Math.Sign(navEdge.launchStepX))
			{
				if (flag || (navEdge != null && navEdge.type == BotNavigationGraph.EdgeType.Walk))
				{
					ClearNavigation();
				}
				num2 = 0;
			}
		}
		moveDir = Math.Sign(num2);
		int num3 = ((moveDir != 0) ? moveDir : groundMotion.SlipperyStopDirection(Position, footholdSegment, travel, profile));
		bool flag2 = num3 != 0 && travel.Speed * (double)num3 < 0.0;
		BotNavigationGraph.Point position = Position;
		BotGroundMotion.Result result = groundMotion.Step(position, footholdSegment, num3, travel, profile);
		SetPosition(result.point);
		if (result.lostGround)
		{
			Fall(result.stepX);
			return;
		}
		travel = new BotGroundPhysics.Travel((physics.SlipScale(map.footholdSpeed, profile) < 1.0) ? result.travel.X : ((double)result.point.x), result.travel.Speed, result.travel.CarryMs);
		air = null;
		climb = null;
		crouching = (climbUpIntent = false);
		velocityX = result.velocityX;
		velocityY = 0;
		bool flag3 = position.x != result.point.x;
		brakeDir = ((flag2 & flag3) ? num3 : 0);
		if (flag3 && num3 != 0)
		{
			actor.facing = num3;
		}
		if (result.stepX == 0 && velocityX == 0 && num2 == 0)
		{
			Idle();
		}
	}

	public void Idle()
	{
		air = null;
		climb = null;
		crouching = (climbUpIntent = (downJumpPending = false));
		pendingRope = null;
		moveDir = (brakeDir = (velocityX = (velocityY = 0)));
		travel = new BotGroundPhysics.Travel(Position.x);
		SyncStance();
	}

	public void QueueDownJump()
	{
		Idle();
		crouching = (downJumpPending = true);
		SyncStance();
	}

	public void QueueRopeEntry(MapRope rope, int y)
	{
		Idle();
		pendingRope = rope;
		pendingRopeY = y;
	}

	private int ResolveAirX(int dx)
	{
		if (dx != 0)
		{
			return Math.Sign(dx) * physics.WalkStep(profile);
		}
		if (!(physics.SlipScale(map.footholdSpeed, profile) < 1.0) || Climbing)
		{
			return 0;
		}
		return BotGroundPhysics.JavaRound(travel.Speed * (double)physics.TickMs / 8.0);
	}

	public void Jump(int dx, bool fixedArc = false, bool climbIntent = false)
	{
		blockedRope = null;
		if (map.swim)
		{
			climb = null;
			air = null;
			crouching = false;
			climbUpIntent = false;
			swim = new BotSwimMotion.State
			{
				position = Position,
				x = actor.x,
				y = actor.y,
				swimming = true,
				verticalSpeed = 0f - profile.JumpSpeed(555f),
				facing = actor.facing
			};
			travel = new BotGroundPhysics.Travel(Position.x);
			nextSwimJumpMs = BotScheduler.Now() + 500;
			velocityX = 0;
			velocityY = BotGroundPhysics.JavaRound(swim.verticalSpeed);
			SyncStance();
		}
		else
		{
			int x = ResolveAirX(dx);
			Launch(x, ropeJump: false, downJump: false);
			air.fixedArc = fixedArc;
			climbUpIntent = climbIntent;
		}
	}

	public void RopeJump(int dx, bool transfer)
	{
		MapRope mapRope = (Climbing ? climb.rope : null);
		int x = ResolveAirX(dx);
		Launch(x, ropeJump: true, downJump: false);
		blockedRope = (transfer ? mapRope : null);
		climbUpIntent = transfer;
	}

	private void Launch(int x, bool ropeJump, bool downJump)
	{
		air = airMotion.Launch(Position, x, ropeJump, downJump);
		air.facing = actor.facing;
		climb = null;
		swim = null;
		pendingRope = null;
		downJumpPending = (crouching = (climbUpIntent = false));
		moveDir = (brakeDir = 0);
		travel = new BotGroundPhysics.Travel(Position.x);
		velocityX = x * 20;
		velocityY = BotGroundPhysics.JavaRound(air.velocityY * 20f);
		SyncStance();
	}

	public void Fall(int x)
	{
		blockedRope = null;
		Launch(x, ropeJump: false, downJump: false);
		air.velocityY = 0f;
		velocityY = 0;
	}

	public void Attach(MapRope rope, int y)
	{
		climb = BotRopeMotion.Attach(rope, y);
		SetPosition(climb.position);
		air = null;
		swim = null;
		crouching = (climbUpIntent = (downJumpPending = false));
		pendingRope = null;
		velocityX = (velocityY = 0);
		travel = new BotGroundPhysics.Travel(Position.x);
		SyncStance();
	}

	private static bool SameRope(MapRope a, MapRope b)
	{
		if (a != null && b != null && a.x == b.x && a.Top == b.Top && a.Bottom == b.Bottom)
		{
			return a.ladder == b.ladder;
		}
		return false;
	}

	private bool GrabRope()
	{
		if (!climbUpIntent)
		{
			return false;
		}
		foreach (MapRope rope in map.ropes)
		{
			if (!SameRope(blockedRope, rope) && Math.Abs(rope.x - Position.x) <= 10 && Position.y >= rope.Top && Position.y <= rope.Bottom + 2)
			{
				Attach(rope, Position.y);
				return true;
			}
		}
		return false;
	}

	private void TickAir(BotNavigationGraph.Point? target)
	{
		if (GrabRope())
		{
			return;
		}
		if (moveDir == 0)
		{
			bool flag = !air.fixedArc && air.landingGraceMs == 0 && (navEdge == null || (navEdge.type != BotNavigationGraph.EdgeType.Jump && navEdge.type != BotNavigationGraph.EdgeType.Drop && (navEdge.type != BotNavigationGraph.EdgeType.Climb || navEdge.launchStepX == 0)));
			moveDir = ((!flag) ? Math.Sign(air.velocityX) : ((target.HasValue && Math.Abs(target.Value.x - Position.x) > 8) ? Math.Sign(target.Value.x - Position.x) : 0));
		}
		air.direction = moveDir;
		BotNavigationGraph.Point position = Position;
		BotAirCollision.Kind kind = airMotion.Step(air);
		SetPosition(air.Position);
		actor.facing = air.facing;
		if (kind == BotAirCollision.Kind.Land)
		{
			travel = new BotGroundPhysics.Travel(Position.x, air.groundSpeed);
			velocityX = BotGroundPhysics.JavaRound(air.groundSpeed * 50.0 / 8.0 * 20.0);
			velocityY = 0;
			float arg = (float)air.fallDistance;
			air = null;
			blockedRope = null;
			climbUpIntent = false;
			jumpCooldownMs = 0;
			ApplyFallDamage?.Invoke(actor, arg);
		}
		else
		{
			velocityX = BotGroundPhysics.JavaRound(((double)air.velocityX + air.steerVelocity) * 20.0);
			velocityY = BotGroundPhysics.JavaRound(air.velocityY * 20f);
			if ((kind == BotAirCollision.Kind.Ceiling || !GrabRope()) && air != null && air.flashFired)
			{
				EmitFlash(position);
				air.flashFired = false;
			}
		}
	}

	private void TickClimb(BotNavigationGraph.Point? target, bool runAi)
	{
		if (!target.HasValue)
		{
			climb.direction = 0;
			velocityX = (velocityY = 0);
			return;
		}
		int num = target.Value.y - Position.y;
		int num2 = target.Value.x - climb.rope.x;
		if (runAi && navEdge == null && Math.Abs(num2) > 80 && climb.rope.Bottom < target.Value.y)
		{
			RopeJump(num2, transfer: false);
			return;
		}
		if (navEdge == null && (resting || (!grinding && Math.Abs(num) < 30 && Math.Abs(num2) < 160)))
		{
			climbDirection = 0;
		}
		else
		{
			if (preciseTarget && num != 0 && target.Value.x == climb.rope.x && target.Value.y > climb.rope.Top && target.Value.y <= climb.rope.Bottom && Math.Abs(num) < 5)
			{
				Attach(climb.rope, target.Value.y);
				return;
			}
			if (runAi || navEdge != null)
			{
				climbDirection = Math.Sign(num);
			}
		}
		climb.direction = climbDirection;
		BotRopeMotion.Result result = ropeMotion.Step(climb);
		SetPosition(climb.position);
		velocityX = (velocityY = 0);
		switch (result)
		{
		case BotRopeMotion.Result.Fall:
			Fall(0);
			break;
		case BotRopeMotion.Result.Land:
			climb = null;
			travel = new BotGroundPhysics.Travel(Position.x);
			break;
		}
	}

	private void TickSwim(BotNavigationGraph.Point? target, long now)
	{
		if (swim == null)
		{
			swim = new BotSwimMotion.State
			{
				position = Position,
				facing = actor.facing
			};
		}
		int verticalHold = swim.verticalHold;
		swim.position = Position;
		swim.direction = (swim.verticalHold = 0);
		swim.jumpRequested = false;
		if (attackCooldownMs == 0)
		{
			if (!target.HasValue)
			{
				swim.verticalHold = -1;
			}
			else
			{
				int value = target.Value.x - Position.x;
				int num = target.Value.y - Position.y;
				swim.direction = ((Math.Abs(value) > 8) ? Math.Sign(value) : 0);
				if (Math.Abs(value) <= 8 && Math.Abs(num) <= 30)
				{
					swim.direction = 0;
					swim.verticalHold = -1;
				}
				else if (num <= -100 && now >= nextSwimJumpMs)
				{
					swim.jumpRequested = true;
					nextSwimJumpMs = now + 500;
					swim.verticalHold = -1;
				}
				else
				{
					swim.verticalHold = ((num <= 30) ? (-1) : ((num > 120 || verticalHold > 0) ? 1 : 0));
				}
			}
		}
		bool flag = swimMotion.Step(swim);
		SetPosition(swim.position);
		actor.facing = swim.facing;
		velocityX = BotGroundPhysics.JavaRound(swim.horizontalSpeed);
		velocityY = BotGroundPhysics.JavaRound(swim.verticalSpeed);
		if (flag)
		{
			travel = new BotGroundPhysics.Travel(Position.x, swim.groundSpeed);
			air = null;
		}
	}

	private void SetPosition(BotNavigationGraph.Point point)
	{
		actor.x = point.x;
		actor.y = point.y;
	}

	public int SyncStance()
	{
		int num;
		if (Climbing)
		{
			num = (climb.rope.ladder ? 14 : 16);
		}
		else if (Swimming)
		{
			num = ((actor.facing >= 0) ? 12 : 13);
		}
		else if (crouching)
		{
			num = ((actor.facing >= 0) ? 10 : 11);
		}
		else if (InAir)
		{
			num = ((actor.facing >= 0) ? 6 : 7);
		}
		else if (moveDir != 0)
		{
			num = ((moveDir > 0) ? 2 : 3);
		}
		else if (brakeDir != 0)
		{
			num = ((brakeDir > 0) ? 2 : 3);
		}
		else
		{
			num = ((actor.facing >= 0) ? 4 : 5);
		}
		actor.stance = num;
		actor.moving = velocityX != 0 || velocityY != 0;
		return num;
	}

	public void Publish(long now)
	{
		Emit(now);
	}

	private void Emit(long now)
	{
		int num = SyncStance();
		if (now < alertedUntilMs && (num == 4 || num == 5))
		{
			num += 4;
		}
		if (IsObserved != null && !IsObserved(map.mapId))
		{
			broadcastValid = false;
			return;
		}
		if (Broadcast == null || (SinkReady != null && !SinkReady()))
		{
			broadcastValid = false;
			return;
		}
		FootholdSegment footholdSegment = ground.Ground(Position.x, Position.y);
		if (footholdSegment != null)
		{
			lastFoothold = footholdSegment.id;
		}
		(int, int, int, int, int, int) other = (Position.x, Position.y, velocityX, velocityY, num, lastFoothold);
		if (!broadcastValid || !lastBroadcast.Equals(other))
		{
			broadcastValid = true;
			lastBroadcast = other;
			BotMovementRecording.Command command = new BotMovementRecording.Command(0, (short)actor.x, (short)actor.y, (short)velocityX, (short)velocityY, (short)lastFoothold, (sbyte)num, 50);
			byte[] array = BotMovementPacket.Reconstruct(new BotMovementRecording.Command[1] { command });
			byte[] array2 = new byte[array.Length - 9];
			Array.Copy(array, 9, array2, 0, array2.Length);
			Broadcast?.Invoke(actor, array2);
		}
	}

	private void EmitFlash(BotNavigationGraph.Point previous)
	{
		if (IsObserved != null && !IsObserved(map.mapId))
		{
			return;
		}
		FootholdSegment footholdSegment = ground.Ground(Position.x, Position.y);
		if (footholdSegment != null)
		{
			lastFoothold = footholdSegment.id;
		}
		flashBroadcast = true;
		if (Broadcast == null || (SinkReady != null && !SinkReady()))
		{
			SyncStance();
			broadcastValid = false;
			return;
		}
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write((byte)2);
		binaryWriter.Write((byte)0);
		binaryWriter.Write((short)actor.x);
		binaryWriter.Write((short)actor.y);
		binaryWriter.Write((short)velocityX);
		binaryWriter.Write((short)velocityY);
		binaryWriter.Write((short)lastFoothold);
		binaryWriter.Write((byte)SyncStance());
		binaryWriter.Write((short)50);
		binaryWriter.Write((byte)6);
		binaryWriter.Write((short)(Position.x - previous.x));
		binaryWriter.Write((short)(Position.y - previous.y));
		binaryWriter.Write((byte)actor.stance);
		binaryWriter.Write((short)0);
		Broadcast?.Invoke(actor, memoryStream.ToArray());
		broadcastValid = true;
		lastBroadcast = (Position.x, Position.y, velocityX, velocityY, actor.stance, lastFoothold);
	}
}

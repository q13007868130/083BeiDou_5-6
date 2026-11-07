using System;
using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public sealed class NativeSummon : MonoBehaviour
{
	private SkillSystem skills;

	private PlayerCtrl owner;

	private SkillDef skill;

	private SpriteRenderer body;

	private ImgNode animations;

	private List<ImgNode> frames;

	private string action;

	private string attackAction;

	private int frame;

	private int facing;

	private float frameUntil;

	private float attackAt;

	private static readonly string[] AttackActions = new string[6] { "attack1", "attack2", "skill1", "skill2", "attack", "fly" };

	public Vector2 Position
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			return Vector2.op_Implicit(((Component)this).transform.position);
		}
	}

	public static ImgNode Root(int id, int level)
	{
		ImgNode imgNode = WzAssets.FileRoot("Skill/" + (id / 10000).ToString("D3") + ".img").Get("skill/" + id.ToString("D7"));
		ImgNode imgNode2 = imgNode?.Get("level/" + level + "/summon");
		if (imgNode2 == null)
		{
			if (imgNode == null)
			{
				return null;
			}
			imgNode2 = imgNode.Get("summon");
		}
		return imgNode2;
	}

	public static bool Stationary(int id)
	{
		if (id != 3111002 && id != 3211002 && id != 13111004 && id != 5211001 && id != 5220002)
		{
			return id == 2321031;
		}
		return true;
	}

	public static string AttackName(int id, int level)
	{
		ImgNode imgNode = Root(id, level);
		if (imgNode == null)
		{
			return null;
		}
		string[] attackActions = AttackActions;
		foreach (string text in attackActions)
		{
			if (WzAssets.Frames(imgNode.Get(text)).Count > 0)
			{
				return text;
			}
		}
		return null;
	}

	public static ImgNode AttackInfo(NativeSkillDefinition skill)
	{
		string text = AttackName(skill.Id, skill.Level);
		if (text != null)
		{
			return Root(skill.Id, skill.Level)?.Get(text + "/info");
		}
		return null;
	}

	public static Nullable AttackRange(NativeSkillDefinition skill)
	{
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		ImgNode imgNode = AttackInfo(skill);
		if (imgNode != null)
		{
			return NativeMobController.Range(imgNode);
		}
		if (AttackName(skill.Id, skill.Level) == null)
		{
			return null;
		}
		if (skill.HasBounds)
		{
			return Rect.MinMaxRect(skill.Min.x * 40f, skill.Min.y * 40f, skill.Max.x * 40f, skill.Max.y * 40f);
		}
		return Rect.MinMaxRect(-300f, -160f, 0f, 60f);
	}

	public static NativeSummon Spawn(SkillSystem skills, PlayerCtrl owner, SkillDef skill)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		NativeSummon nativeSummon = new GameObject("Summon_" + skill.native.Id).AddComponent<NativeSummon>();
		nativeSummon.skills = skills;
		nativeSummon.owner = owner;
		nativeSummon.skill = skill;
		((Component)nativeSummon).transform.position = Vector2.op_Implicit(owner.FeetPos);
		nativeSummon.facing = owner.facing;
		nativeSummon.animations = Root(skill.native.Id, skill.native.Level);
		nativeSummon.attackAction = AttackName(skill.native.Id, skill.native.Level);
		nativeSummon.body = ((Component)nativeSummon).gameObject.AddComponent<SpriteRenderer>();
		((Renderer)nativeSummon.body).sortingOrder = owner.SortingOrder - 1;
		nativeSummon.Play(nativeSummon.Has("summoned") ? "summoned" : "stand");
		nativeSummon.attackAt = Time.unscaledTime + nativeSummon.Duration("summoned");
		return nativeSummon;
	}

	private float Duration(string name)
	{
		float num = 0f;
		foreach (ImgNode item in WzAssets.Frames(animations?.Get(name)))
		{
			num += Mathf.Max(0.001f, (float)Mathf.Abs(item.Resolve().Int("delay", 100)) / 1000f);
		}
		return num;
	}

	private bool Has(string name)
	{
		return WzAssets.Frames(animations?.Get(name)).Count > 0;
	}

	private void Play(string name)
	{
		if (!(action == name))
		{
			if (!Has(name))
			{
				name = "stand";
			}
			if (Has(name))
			{
				frames = WzAssets.Frames(animations.Get(name));
				action = name;
				frame = 0;
				ShowFrame();
			}
		}
	}

	private void ShowFrame()
	{
		ImgNode imgNode = frames[frame].Resolve();
		body.sprite = WzAssets.Sprite(imgNode);
		body.flipX = facing > 0;
		frameUntil = Time.unscaledTime + Mathf.Max(0.001f, (float)Mathf.Abs(imgNode.Int("delay", 100)) / 1000f);
	}

	private void Update()
	{
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_0287: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0292: Unknown result type (might be due to invalid IL or missing references)
		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
		//IL_029c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_030d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0312: Unknown result type (might be due to invalid IL or missing references)
		//IL_0318: Unknown result type (might be due to invalid IL or missing references)
		//IL_031d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0322: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_0375: Unknown result type (might be due to invalid IL or missing references)
		//IL_0380: Unknown result type (might be due to invalid IL or missing references)
		//IL_0338: Unknown result type (might be due to invalid IL or missing references)
		//IL_033d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0343: Unknown result type (might be due to invalid IL or missing references)
		//IL_0348: Unknown result type (might be due to invalid IL or missing references)
		//IL_034d: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)owner == (Object)null || owner.hp <= 0f || !((Behaviour)owner).isActiveAndEnabled || (Object)(object)skills == (Object)null || (Object)(object)GameLoop.I == (Object)null || GameLoop.I.gameOver || GameLoop.I.state != GameLoop.GameState.Playing || !NativeBuffs.For(owner).HasSource(skill.native.Id))
		{
			((Component)this).gameObject.SetActive(false);
			Object.Destroy((Object)(object)((Component)this).gameObject);
			return;
		}
		if (Time.unscaledTime >= frameUntil)
		{
			frame++;
			if (frames == null)
			{
				return;
			}
			if (frame >= frames.Count)
			{
				if (action == attackAction || action == "summoned")
				{
					action = null;
					Play("stand");
				}
				else
				{
					frame = 0;
					ShowFrame();
				}
			}
			else
			{
				ShowFrame();
			}
		}
		if (action == "summoned" || action == attackAction)
		{
			return;
		}
		if (!Stationary(skill.native.Id))
		{
			Vector2 val = owner.FeetPos + Vector2.left * (float)owner.facing * 1.2f;
			bool flag = Vector2.Distance(Position, val) > 0.1f;
			((Component)this).transform.position = Vector2.op_Implicit(Vector2.MoveTowards(Position, val, owner.moveSpeed * Time.deltaTime));
			facing = ((val.x >= Position.x) ? 1 : (-1));
			NativeSummon nativeSummon = this;
			string name;
			if (flag)
			{
				name = (Has("fly") ? "fly" : "move");
			}
			else
			{
				name = "stand";
			}
			nativeSummon.Play(name);
		}
		if (attackAction == null || !Has(attackAction) || !AttackRange(skill.native).HasValue || Time.unscaledTime < attackAt)
		{
			return;
		}
		Component val2 = null;
		float num = float.MaxValue;
		Vector2 val3;
		foreach (EnemyUnit item in EnemyUnit.All)
		{
			if (!item.dead)
			{
				val3 = Vector2.op_Implicit(((Component)item).transform.position) - Position;
				if (val3.sqrMagnitude < num)
				{
					val2 = (Component)(object)item;
					val3 = Vector2.op_Implicit(((Component)item).transform.position) - Position;
					num = val3.sqrMagnitude;
				}
			}
		}
		BossUnit[] array = Object.FindObjectsOfType<BossUnit>();
		foreach (BossUnit bossUnit in array)
		{
			if (!bossUnit.dead)
			{
				val3 = Vector2.op_Implicit(((Component)bossUnit).transform.position) - Position;
				if (val3.sqrMagnitude < num)
				{
					val2 = (Component)(object)bossUnit;
					val3 = Vector2.op_Implicit(((Component)bossUnit).transform.position) - Position;
					num = val3.sqrMagnitude;
				}
			}
		}
		if ((Object)(object)val2 != (Object)null)
		{
			facing = ((val2.transform.position.x >= Position.x) ? 1 : (-1));
		}
		if (skills.HitAt(skill, skill.native, Position, facing) > 0)
		{
			Play(attackAction);
			attackAt = Time.unscaledTime + Duration(attackAction) + (float)AttackInfo(skill.native).Int("attackAfter") / 1000f;
		}
		else
		{
			attackAt = Time.unscaledTime + 0.1f;
		}
	}
}

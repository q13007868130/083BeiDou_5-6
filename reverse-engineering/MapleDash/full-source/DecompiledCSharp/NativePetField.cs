using System;
using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public sealed class NativePetField : MonoBehaviour
{
	private sealed class Pet
	{
		public int id;

		public int template;

		public int slot;

		public int mode;

		public int facing = 1;

		public int direction;

		public int clothing;

		public int fullness;

		public float x;

		public float y;

		public float vx;

		public float vy;

		public float climbWait;

		public bool ground;

		public bool climbing;

		public string action;

		public double animationTime;

		public int frame;

		public GameObject root;

		public SpriteRenderer body;

		public SpriteRenderer coat;

		public ImgNode image;

		public ImgNode equipment;

		public List<ImgNode> frames;
	}

	private sealed class PetChat
	{
		public string text;

		public float until;
	}

	private readonly Dictionary<int, Pet> pets = new Dictionary<int, Pet>();

	private NativeMovementPhysics physics;

	private FieldMap map;

	private double readAt;

	private float accumulated;

	private float previousHp = -1f;

	private float previousMp = -1f;

	private PetChat petChat;

	public void RecordVitals(PlayerCtrl owner)
	{
		previousHp = owner.hp;
		previousMp = owner.mp;
	}

	public void Tick(PlayerCtrl owner, float dt)
	{
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		FieldDirector fieldDirector = GameLoop.I?.field;
		if (!GameSession.InGame || fieldDirector?.Map == null || (Object)(object)owner == (Object)null)
		{
			Clear();
			return;
		}
		if (map != fieldDirector.Map)
		{
			Clear();
			map = fieldDirector.Map;
			readAt = 0.0;
		}
		if (Time.realtimeSinceStartupAsDouble >= readAt)
		{
			readAt = Time.realtimeSinceStartupAsDouble + 0.25;
			Sync(owner);
		}
		NativePetAutopot.Tick(owner, previousHp, previousMp, pets.Count > 0);
		previousHp = owner.hp;
		previousMp = owner.mp;
		if (physics == null)
		{
			physics = NativeMovementPhysics.Load();
		}
		accumulated += dt;
		while (accumulated >= 0.03f)
		{
			accumulated -= 0.03f;
			foreach (Pet p in pets.Values)
			{
				Move(p, owner, 0.03f);
				if (!p.climbing)
				{
					GroundItem.TryPetPickup(new Vector2(p.x, p.y), p.slot, CanPickup(p.slot, meso: true), CanPickup(p.slot, meso: false), () => PickupTarget(p.slot));
				}
			}
		}
		foreach (Pet value in pets.Values)
		{
			Draw(value, owner, dt);
		}
	}

	private void Sync(PlayerCtrl owner)
	{
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Expected Obj, but got Unknown
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Expected Obj, but got Unknown
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		HashSet<int> hashSet = new HashSet<int>();
		for (int i = 0; i < 3; i++)
		{
			InventoryEntry pet = GameSession.Net.ChangeInventory(new InventoryChangeRequest
			{
				characterId = GameSession.CharacterId,
				getPet = true,
				petSlot = i
			}).pet;
			if (pet != null)
			{
				hashSet.Add(pet.petId);
				if (!pets.TryGetValue(pet.petId, out var value))
				{
					value = new Pet
					{
						id = pet.petId,
						template = pet.itemId,
						slot = i,
						image = WzAssets.FileRoot("Item/Pet/" + pet.itemId.ToString("D7") + ".img")
					};
					value.root = new GameObject("pet-" + pet.petId);
					value.root.transform.SetParent(((Component)this).transform, false);
					value.body = value.root.AddComponent<SpriteRenderer>();
					GameObject val = new GameObject("equipment");
					val.transform.SetParent(value.root.transform, false);
					value.coat = val.AddComponent<SpriteRenderer>();
					Place(value, owner.FeetPos);
					pets.Add(value.id, value);
				}
				value.slot = i;
				value.fullness = Convert.ToInt32(pet.petData["fullness"]);
				int num = Profile.EquipOfSlot(NativePetItems.EquipmentSlot(1802000, i));
				if (num != value.clothing)
				{
					value.clothing = num;
					value.equipment = ((num > 0) ? WzAssets.Node("Character/PetEquip/" + num.ToString("D8") + ".img/" + value.template) : null);
				}
			}
		}
		List<int> list = new List<int>();
		foreach (KeyValuePair<int, Pet> pet2 in pets)
		{
			if (!hashSet.Contains(pet2.Key))
			{
				list.Add(pet2.Key);
			}
		}
		foreach (int item in list)
		{
			Remove(pets[item].root);
			pets.Remove(item);
		}
		foreach (Pet value2 in pets.Values)
		{
			value2.mode = ((pets.Count == 3) ? (new int[3] { 5, 3, 4 })[value2.slot] : ((pets.Count == 2) ? (value2.slot + 1) : 0));
		}
	}

	private void Place(Pet p, Vector2 owner)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		p.x = owner.x;
		p.y = owner.y + 0.3f;
		p.vx = (p.vy = 0f);
		p.ground = false;
		if (PlayerCtrl.FindLanding(p.x, p.y, float.NegativeInfinity, -1, out var landing))
		{
			p.y = landing;
			p.ground = true;
		}
	}

	public Nullable PickupTarget(int slot)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		foreach (Pet value in pets.Values)
		{
			if (value.slot == slot)
			{
				return new Vector2(value.x, value.y);
			}
		}
		return null;
	}

	public static bool CanPickup(int slot, bool meso)
	{
		int num = Profile.EquipOfSlot(NativePetItems.EquipmentSlot(meso ? 1812000 : 1812001, slot));
		if (num > 0)
		{
			return (EquipCatalog.Info(num)?.Int(meso ? "pickupMeso" : "pickupItem") ?? 0) != 0;
		}
		return false;
	}

	private void Move(Pet p, PlayerCtrl owner, float dt)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02df: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0312: Unknown result type (might be due to invalid IL or missing references)
		//IL_0319: Unknown result type (might be due to invalid IL or missing references)
		//IL_04cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04da: Unknown result type (might be due to invalid IL or missing references)
		//IL_032a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0331: Unknown result type (might be due to invalid IL or missing references)
		//IL_033a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0342: Unknown result type (might be due to invalid IL or missing references)
		//IL_0349: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0369: Unknown result type (might be due to invalid IL or missing references)
		//IL_0370: Unknown result type (might be due to invalid IL or missing references)
		//IL_0379: Unknown result type (might be due to invalid IL or missing references)
		//IL_0380: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_050d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0514: Unknown result type (might be due to invalid IL or missing references)
		//IL_0524: Unknown result type (might be due to invalid IL or missing references)
		//IL_053a: Unknown result type (might be due to invalid IL or missing references)
		//IL_052f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0545: Unknown result type (might be due to invalid IL or missing references)
		//IL_054e: Unknown result type (might be due to invalid IL or missing references)
		Vector2 feetPos = owner.FeetPos;
		float num = (feetPos.x - p.x) * 40f;
		float num2 = (feetPos.y - p.y) * 40f;
		if ((Mathf.Abs(num) > 300f || Mathf.Abs(num2) > 200f) && PlayerCtrl.FindLanding(feetPos.x, feetPos.y + 0.25f, float.NegativeInfinity, -1, out var landing) && Mathf.Abs(landing - feetPos.y) < 1.25f)
		{
			NativeImgAnimation.Spawn("Effect/PetEff.img/Basic/Teleport", ((Component)this).transform, new Vector3(p.x, p.y, 0f), owner.SortingOrder - 1);
			Place(p, feetPos);
		}
		if (owner.IsClimbing)
		{
			p.x = feetPos.x;
			p.y = feetPos.y;
			p.vx = (p.vy = 0f);
			p.climbing = true;
			p.ground = false;
			p.climbWait = 0f;
			return;
		}
		if (p.climbing)
		{
			p.climbWait += dt;
			if (p.climbWait < 0.2f)
			{
				return;
			}
			p.climbing = false;
			p.vy = owner.jumpSpeed;
			p.ground = false;
		}
		float num3 = 80f;
		if (owner.facing < 0 && (p.mode == 1 || p.mode == 3))
		{
			feetPos.x -= 4f;
		}
		if (owner.facing > 0 && (p.mode == 2 || p.mode == 4))
		{
			feetPos.x += 4f;
		}
		if (owner.facing > 0 && p.mode == 3)
		{
			feetPos.x--;
			num3 = 70f;
		}
		if (owner.facing < 0 && p.mode == 4)
		{
			feetPos.x++;
			num3 = 70f;
		}
		if (p.mode == 5)
		{
			num3 = 60f;
		}
		num = (feetPos.x - p.x) * 40f;
		if (Mathf.Abs(num) < num3)
		{
			p.direction = 0;
		}
		else if (p.direction != 0 || Mathf.Abs(num) > 80f)
		{
			p.direction = ((!(num < 0f)) ? 1 : (-1));
		}
		if (p.direction != 0)
		{
			p.facing = p.direction;
		}
		float num4 = dt;
		while (num4 > 0f)
		{
			float num5 = Mathf.Min(0.008f, num4);
			num4 -= num5;
			float x = p.x;
			float y = p.y;
			float num6 = 0f;
			float num7 = 1f;
			float num8 = 0f;
			if (p.ground && PlayerCtrl.Terrain != null)
			{
				for (int i = 0; i < PlayerCtrl.Terrain.Count; i++)
				{
					Vector4 val = PlayerCtrl.Terrain[i];
					if (Mathf.Abs(val.z - val.x) > 0.001f && x >= Mathf.Min(val.x, val.z) && x <= Mathf.Max(val.x, val.z) && Mathf.Abs(Mathf.Lerp(val.y, val.w, (x - val.x) / (val.z - val.x)) - y) < 0.0375f)
					{
						num8 = (0f - (val.w - val.y)) / (val.z - val.x);
						if (PlayerCtrl.TerrainForces != null && i < PlayerCtrl.TerrainForces.Count)
						{
							num6 = (float)PlayerCtrl.TerrainForces[i] * 0.01f;
						}
						if (PlayerCtrl.TerrainDrag != null && i < PlayerCtrl.TerrainDrag.Count && PlayerCtrl.TerrainDrag[i] != 0)
						{
							num7 = (float)PlayerCtrl.TerrainDrag[i] * 0.01f;
						}
						break;
					}
				}
			}
			p.vx = (float)(p.ground ? physics.Ground(p.vx * 40f, p.direction, num6, num7, (double)(owner.moveSpeed * 40f) / physics.WalkSpeed, num8, num5) : physics.Air(p.vx * 40f, (0f - p.vy) * 40f, p.direction, num5)) / 40f;
			float num9 = Mathf.Clamp(x + p.vx * num5, PlayerCtrl.XMin, PlayerCtrl.XMax);
			bool flag = false;
			if (PlayerCtrl.Terrain != null)
			{
				foreach (Vector4 item in PlayerCtrl.Terrain)
				{
					if (Mathf.Abs(item.z - item.x) < 0.001f && y > Mathf.Min(item.y, item.w) + 0.001f && y < Mathf.Max(item.y, item.w) && ((x < item.x && num9 >= item.x) || (x > item.x && num9 <= item.x)))
					{
						num9 = item.x;
						flag = true;
						break;
					}
				}
			}
			bool flag2 = PlayerCtrl.FollowGround(x, y, num9, out var outY);
			if (p.ground && (JumpToOwnerGroup(p, owner) || (p.direction != 0 && (flag || (!flag2 && feetPos.y > y)))))
			{
				p.ground = false;
				p.vy = owner.jumpSpeed;
			}
			p.x = num9;
			if (p.ground & flag2)
			{
				p.y = outY;
				continue;
			}
			p.ground = false;
			p.vy = Mathf.Max(p.vy - (float)physics.Gravity / 40f * num5, (0f - (float)physics.FallSpeed) / 40f);
			p.y += p.vy * num5;
			if (p.vy <= 0f && PlayerCtrl.FindLanding(p.x, y, p.y, -1, out var landing2))
			{
				p.y = landing2;
				p.vy = 0f;
				p.ground = true;
			}
		}
	}

	private bool JumpToOwnerGroup(Pet p, PlayerCtrl owner)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		if (!owner.grounded || owner.FeetPos.y <= p.y)
		{
			return false;
		}
		FieldFoothold fieldFoothold = null;
		FieldFoothold fieldFoothold2 = null;
		foreach (FieldFoothold foothold in map.footholds)
		{
			FieldFoothold f = foothold;
			if (!(Mathf.Abs(f.x2 - f.x1) < 0.001f))
			{
				if (At(owner.FeetPos.x, owner.FeetPos.y))
				{
					fieldFoothold = f;
				}
				if (At(p.x, p.y))
				{
					fieldFoothold2 = f;
				}
			}
			bool At(float x, float y)
			{
				if (x * 40f >= Mathf.Min(f.x1, f.x2) && x * 40f <= Mathf.Max(f.x1, f.x2))
				{
					return Mathf.Abs((0f - y) * 40f - Mathf.Lerp(f.y1, f.y2, (x * 40f - f.x1) / (f.x2 - f.x1))) < 1.5f;
				}
				return false;
			}
		}
		if (fieldFoothold == null || fieldFoothold2 == null || (fieldFoothold.group == fieldFoothold2.group && fieldFoothold.layer == fieldFoothold2.layer))
		{
			return false;
		}
		float num = float.PositiveInfinity;
		float num2 = float.NegativeInfinity;
		foreach (FieldFoothold foothold2 in map.footholds)
		{
			if (foothold2.group == fieldFoothold.group && foothold2.layer == fieldFoothold.layer)
			{
				num = Mathf.Min(new float[3] { num, foothold2.x1, foothold2.x2 });
				num2 = Mathf.Max(new float[3] { num2, foothold2.x1, foothold2.x2 });
			}
		}
		if (p.x * 40f > num)
		{
			return p.x * 40f < num2;
		}
		return false;
	}

	private void Draw(Pet p, PlayerCtrl owner, float dt)
	{
		//IL_025e: Unknown result type (might be due to invalid IL or missing references)
		string text;
		if (p.climbing)
		{
			text = "hang";
		}
		else if (p.ground)
		{
			if (p.direction != 0)
			{
				text = "move";
			}
			else
			{
				text = ((p.fullness <= 50) ? "hungry" : "stand0");
			}
		}
		else
		{
			text = "jump";
		}
		if (text != p.action)
		{
			p.action = text;
			p.animationTime = 0.0;
			p.frame = 0;
			p.frames = WzAssets.Frames(p.image.Get(text));
		}
		p.animationTime += dt;
		if (p.frames.Count > 0)
		{
			double num = 0.0;
			foreach (ImgNode frame in p.frames)
			{
				num += (double)Math.Max(1, Math.Abs(frame.Int("delay", 100))) / 1000.0;
			}
			double num2 = p.animationTime % num;
			for (int i = 0; i < p.frames.Count; i++)
			{
				double num3 = (double)Math.Max(1, Math.Abs(p.frames[i].Int("delay", 100))) / 1000.0;
				if (num2 < num3)
				{
					p.frame = i;
					break;
				}
				num2 -= num3;
			}
			p.body.sprite = WzAssets.Sprite(p.frames[p.frame]);
			ImgNode imgNode = p.equipment?.Get(text + "/" + p.frames[p.frame].Name)?.Resolve();
			p.coat.sprite = ((imgNode != null) ? WzAssets.Sprite(imgNode) : null);
		}
		SpriteRenderer body = p.body;
		bool flipX = (p.coat.flipX = p.facing > 0);
		body.flipX = flipX;
		((Renderer)p.body).sortingOrder = owner.SortingOrder - 2 - p.slot * 2;
		((Renderer)p.coat).sortingOrder = ((Renderer)p.body).sortingOrder + 1;
		p.root.transform.position = new Vector3(p.x, p.y, 0f);
	}

	private static void Remove(GameObject go)
	{
		if (Application.isPlaying)
		{
			Object.Destroy((Object)(object)go);
		}
		else
		{
			Object.DestroyImmediate((Object)(object)go);
		}
	}

	public void Clear()
	{
		foreach (Pet value in pets.Values)
		{
			Remove(value.root);
		}
		pets.Clear();
		accumulated = 0f;
		previousHp = (previousMp = -1f);
		petChat = null;
	}

	public bool ShowChat(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		if (pets.Count == 0)
		{
			return false;
		}
		Pet pet = null;
		foreach (Pet value in pets.Values)
		{
			if (pet == null || value.slot < pet.slot)
			{
				pet = value;
			}
		}
		if (pet == null)
		{
			return false;
		}
		petChat = new PetChat
		{
			text = ((text.Length > 127) ? text.Substring(0, 127) : text),
			until = Time.unscaledTime + 5f
		};
		return true;
	}

	private void OnGUI()
	{
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Expected Obj, but got Unknown
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Expected Obj, but got Unknown
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_0261: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
		//IL_0271: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_0280: Unknown result type (might be due to invalid IL or missing references)
		//IL_0287: Unknown result type (might be due to invalid IL or missing references)
		//IL_0290: Expected Obj, but got Unknown
		//IL_0292: Unknown result type (might be due to invalid IL or missing references)
		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
		if (petChat == null)
		{
			return;
		}
		float unscaledTime = Time.unscaledTime;
		if (unscaledTime > petChat.until)
		{
			petChat = null;
			return;
		}
		Pet pet = null;
		foreach (Pet value in pets.Values)
		{
			if (pet == null || value.slot < pet.slot)
			{
				pet = value;
			}
		}
		Camera main = Camera.main;
		if (pet == null || (Object)(object)pet.body == (Object)null || (Object)(object)main == (Object)null)
		{
			return;
		}
		GUIStyle val = new GUIStyle(GUI.skin.label)
		{
			font = UiKit.Font,
			fontSize = 12,
			wordWrap = false
		};
		NativeChatBalloon.Layout layout = NativeChatBalloon.Prepare("pet/0", (int)Mathf.Max(24f, val.CalcSize(new GUIContent(petChat.text)).x), 1);
		if (layout != null)
		{
			float num = Mathf.Max(0.35f, UiKit.H / 768f);
			float num2;
			if (!((Object)(object)pet.body.sprite != (Object)null))
			{
				num2 = pet.y + 0.3f;
			}
			else
			{
				Bounds bounds = ((Renderer)pet.body).bounds;
				num2 = bounds.max.y;
			}
			float num3 = num2;
			Vector3 val2 = main.WorldToScreenPoint(new Vector3(pet.x, num3, 0f));
			if (!(val2.z < 0f))
			{
				Vector2 val3 = new Vector2(val2.x / UiKit.Scale / num, ((float)Screen.height - val2.y) / UiKit.Scale / num);
				float num4 = Mathf.Clamp01((petChat.until - unscaledTime) / 1f);
				UiKit.Begin();
				Matrix4x4 matrix = GUI.matrix;
				Color color = GUI.color;
				GUI.matrix = matrix * Matrix4x4.Scale(new Vector3(num, num, 1f));
				GUI.color = new Color(1f, 1f, 1f, num4);
				Rect val4 = new Rect(val3.x - (float)layout.anchorX, val3.y - (float)((Texture)layout.texture).height, (float)((Texture)layout.texture).width, (float)((Texture)layout.texture).height);
				GUI.DrawTexture(val4, (Texture)(object)layout.texture);
				GUIStyle val5 = new GUIStyle(GUI.skin.label)
				{
					font = UiKit.Font,
					fontSize = 12,
					alignment = (TextAnchor)4,
					wordWrap = false,
					clipping = (TextClipping)0
				};
				Color color2 = layout.color;
				val5.normal.textColor = new Color(color2.b, color2.g, color2.r, color2.a * num4);
				Rect text = layout.text;
				text.position += val4.position;
				GUI.Label(text, petChat.text, val5);
				GUI.color = color;
				GUI.matrix = matrix;
			}
		}
	}

	private void LateUpdate()
	{
		if (!GameSession.InGame || GameLoop.I?.field?.Map == null)
		{
			Clear();
		}
	}

	private void OnDisable()
	{
		Clear();
	}
}

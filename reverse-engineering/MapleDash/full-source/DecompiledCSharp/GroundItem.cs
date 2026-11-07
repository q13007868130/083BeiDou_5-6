using System;
using System.Collections.Generic;
using MapleDash.Wz;
using Server.Bot;
using Server.Field;
using UnityEngine;

public sealed class GroundItem : MonoBehaviour
{
	public static readonly List<GroundItem> All = new List<GroundItem>();

	public int itemId;

	public int amount;

	public MapItem bound;

	public static Func<MapItem, bool> AuthoritativePickup;

	public static Func<MapItem, int, bool> AuthoritativePetPickup;

	private SpriteRenderer sr;

	private Sprite[] frames;

	private Vector3 canvasOffset;

	private int sourceX;

	private int sourceY;

	private int targetX;

	private int targetY;

	private int landingTick = -1;

	private double born;

	private double removedAt = -1.0;

	private double lastPetRequest = double.NegativeInfinity;

	private double expiresAt = double.PositiveInfinity;

	private bool resting;

	private bool picking;

	private bool removing;

	private Func<Nullable> pickupTarget;

	private static string lastPickup = "";

	private static double lastPickupAt;

	private static double lastDropSoundAt = -1.0;

	private static readonly Dictionary<int, Sprite[]> icons = new Dictionary<int, Sprite[]>();

	public static string LastPickup
	{
		get
		{
			if (!(Time.realtimeSinceStartupAsDouble - lastPickupAt < 2.5))
			{
				return "";
			}
			return lastPickup;
		}
	}

	public int MapX => targetX;

	public int MapY => targetY;

	public bool Ready
	{
		get
		{
			if (bound == null && !removing && amount > 0)
			{
				return resting;
			}
			return false;
		}
	}

	public static GroundItem Spawn(int itemId, int amount, Vector3 position, MapItem bound = null, Nullable from = null, bool alreadyOnGround = false)
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Expected Obj, but got Unknown
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		if (amount <= 0)
		{
			return null;
		}
		Sprite[] array = LoadIcons(itemId, amount);
		if (array == null || array.Length == 0 || (Object)(object)array[0] == (Object)null)
		{
			Debug.LogError((object)("[GroundItem] Missing original iconRaw: " + itemId));
			return null;
		}
		GameObject val = new GameObject((itemId == 0) ? "MesoDrop" : ("ItemDrop_" + itemId));
		GroundItem groundItem = val.AddComponent<GroundItem>();
		groundItem.sr = val.AddComponent<SpriteRenderer>();
		groundItem.itemId = itemId;
		groundItem.amount = amount;
		groundItem.bound = bound;
		groundItem.frames = array;
		groundItem.sr.sprite = array[0];
		Bounds bounds = array[0].bounds;
		groundItem.canvasOffset = -bounds.center;
		Rect rect = array[0].rect;
		int num = (int)rect.height / 2;
		ref float y = ref groundItem.canvasOffset.y;
		float num2 = y;
		rect = array[0].rect;
		y = num2 + (rect.height * 0.5f - (float)num) / 40f;
		int num3 = Mathf.RoundToInt(0f - ((itemId == 0) ? array[0] : LoadIcons(0, 1)[0]).pivot.y);
		Vector3 val2 = from ?? position;
		groundItem.sourceX = (int)(val2.x * 40f);
		groundItem.sourceY = (int)((0f - val2.y) * 40f) - num - num3;
		groundItem.targetX = (int)(position.x * 40f);
		groundItem.targetY = (int)((0f - position.y) * 40f) - num - num3;
		groundItem.born = Time.realtimeSinceStartupAsDouble;
		if (bound == null)
		{
			GameLoop i = GameLoop.I;
			if (i == null || i.field?.Map?.everlast != true)
			{
				groundItem.expiresAt = groundItem.born + (double)((GameServer.Local == null) ? 180000 : GameConfigDao.GetInt(GameServer.Local.Db, "item_expire_time", 180000)) / 1000.0;
			}
		}
		groundItem.resting = alreadyOnGround;
		if (alreadyOnGround)
		{
			groundItem.landingTick = 0;
		}
		((Renderer)groundItem.sr).sortingOrder = 307;
		groundItem.RenderAt(0);
		if (!alreadyOnGround && groundItem.born - lastDropSoundAt > 0.3)
		{
			AudioManager.Play("Game/DropItem", 1f, pitchVary: false);
			lastDropSoundAt = groundItem.born;
		}
		return groundItem;
	}

	private void OnEnable()
	{
		All.Add(this);
	}

	private void OnDisable()
	{
		All.Remove(this);
	}

	private void Update()
	{
		double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
		if (!removing && bound == null && realtimeSinceStartupAsDouble > expiresAt)
		{
			Remove(0, null);
		}
		RenderAt((int)((realtimeSinceStartupAsDouble - born) * 1000.0));
	}

	private void RenderAt(int ageMs)
	{
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0268: Unknown result type (might be due to invalid IL or missing references)
		//IL_026e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0273: Unknown result type (might be due to invalid IL or missing references)
		//IL_0278: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		sr.sprite = frames[(itemId == 0) ? NativeDropMotion.MesoFrame(amount, ageMs) : 0];
		int x;
		int y;
		if (removing)
		{
			int num = (int)((Time.realtimeSinceStartupAsDouble - removedAt) * 1000.0);
			if (picking)
			{
				Nullable val = pickupTarget?.Invoke();
				if (!val.HasValue)
				{
					Object.Destroy((Object)(object)((Component)this).gameObject);
					return;
				}
				if (!NativeDropMotion.Pickup(targetX, targetY, (int)(val.Value.x * 40f), (int)((0f - val.Value.y) * 40f), num, out x, out y, out var alpha))
				{
					Object.Destroy((Object)(object)((Component)this).gameObject);
					return;
				}
				sr.color = Color32.op_Implicit(new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)alpha));
			}
			else
			{
				if (num >= 1000)
				{
					Object.Destroy((Object)(object)((Component)this).gameObject);
					return;
				}
				x = targetX;
				y = targetY;
				sr.color = new Color(1f, 1f, 1f, 1f - (float)num / 1000f);
			}
			((Component)this).transform.rotation = Quaternion.identity;
		}
		else
		{
			int num2 = ageMs / 30;
			if (!resting)
			{
				NativeDropMotion.Flight(sourceX, sourceY, targetX, targetY, ageMs, out x, out y, out resting);
				if (resting)
				{
					landingTick = NativeDropMotion.LandingTick(sourceY, targetY);
				}
			}
			else
			{
				x = targetX;
				y = targetY;
			}
			if (resting)
			{
				y = (int)((double)targetY + NativeDropMotion.Bob(num2 - landingTick));
				((Component)this).transform.rotation = Quaternion.identity;
			}
			else if (itemId != 0)
			{
				((Component)this).transform.rotation = Quaternion.Euler(0f, 0f, (float)(-(ageMs % 300)) * 360f / 300f);
			}
		}
		((Component)this).transform.position = new Vector3((float)x / 40f, (float)(-y) / 40f, 0f);
		Transform transform = ((Component)this).transform;
		transform.position += ((Component)this).transform.rotation * canvasOffset;
	}

	private static Sprite[] LoadIcons(int id, int amount)
	{
		int key = ((id == 0) ? (-1 - NativeDropMotion.MesoTier(amount)) : id);
		if (icons.TryGetValue(key, out var value))
		{
			return value;
		}
		List<Sprite> list = new List<Sprite>();
		if (id == 0)
		{
			string text = "Item/Special/0900.img/0900000" + NativeDropMotion.MesoTier(amount) + "/iconRaw/";
			for (int i = 0; i < 4; i++)
			{
				ImgNode imgNode = WzAssets.Node(text + i);
				if (imgNode == null)
				{
					return null;
				}
				list.Add(WzAssets.Sprite(imgNode, text + i));
			}
		}
		else
		{
			ImgNode imgNode2 = NativeItemSource.Node(id);
			ImgNode imgNode3 = imgNode2?.Get("info/iconRaw") ?? imgNode2?.Get("iconRaw") ?? NativeItemSource.IconNode(id);
			if (imgNode3 == null)
			{
				return null;
			}
			list.Add(WzAssets.Sprite(imgNode3, "drop:" + id));
		}
		return icons[key] = list.ToArray();
	}

	public int TakeForBot(int mapX, int mapY, long rangeSq, int max, Func<Nullable> target = null)
	{
		if (bound != null || removing || !resting || amount <= 0 || max <= 0)
		{
			return 0;
		}
		long num = mapX - targetX;
		long num2 = mapY - targetY;
		if (num * num + num2 * num2 > rangeSq)
		{
			return 0;
		}
		int num3 = Mathf.Min(amount, max);
		amount -= num3;
		if (amount <= 0)
		{
			Remove(2, target);
		}
		else if (target != null)
		{
			GroundItem groundItem = Object.Instantiate<GroundItem>(this);
			groundItem.amount = num3;
			groundItem.Remove(2, target);
		}
		return num3;
	}

	public bool PickupForCompanion(BotCharacter bot, long rangeSq)
	{
		if (bot == null || !((Behaviour)this).enabled || removing || !resting || amount <= 0)
		{
			return false;
		}
		long num = (long)bot.x - targetX;
		long num2 = (long)bot.y - targetY;
		if (num * num + num2 * num2 > rangeSq)
		{
			return false;
		}
		Func<Nullable> func = () => BotGroundPickup.PickupTarget(bot);
		if (bound != null)
		{
			GameLoop i = GameLoop.I;
			bool? flag;
			if (i == null)
			{
				flag = null;
			}
			else
			{
				FieldDirector field = i.field;
				flag = ((field == null) ? ((bool?)null) : ((Component)field).GetComponent<GroundItemBridge>()?.TryPickupForCompanion(bound, func));
			}
			bool? flag2 = flag;
			return flag2 == true;
		}
		return Pickup(-1, func);
	}

	public static void TryPickupNearest()
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		GameLoop i = GameLoop.I;
		if (!((Object)(object)i == (Object)null) && i.state == GameLoop.GameState.Playing && !i.gameOver && !((Object)(object)i.player == (Object)null))
		{
			PickupInRange(i.player.FeetPos);
		}
	}

	private static bool PickupInRange(Vector2 feet)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		int feetX = (int)(feet.x * 40f);
		int feetY = (int)((0f - feet.y) * 40f);
		foreach (GroundItem item in All)
		{
			if ((Object)(object)item == (Object)null || !((Behaviour)item).enabled || item.removing || !item.resting || item.amount <= 0)
			{
				continue;
			}
			MapItem mapItem = item.bound;
			if (mapItem != null && mapItem.dropperId != 0 && mapItem.ownerCharacterId > 0 && mapItem.type < 2 && mapItem.AgeMs((long)(Time.realtimeSinceStartupAsDouble * 1000.0)) < 15000)
			{
				int num = MapItemService.PartyOf?.Invoke(GameSession.CharacterId) ?? (-1);
				if (mapItem.ownerCharacterId != GameSession.CharacterId && (mapItem.partyOwnerId < 0 || mapItem.partyOwnerId != num))
				{
					continue;
				}
			}
			if (NativeDropMotion.InPickupRange(feetX, feetY, item.targetX, item.targetY))
			{
				return item.Pickup();
			}
		}
		return false;
	}

	public static bool TryPetPickup(Vector2 feet, int slot, bool meso, bool items, Func<Nullable> target)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		int feetX = (int)(feet.x * 40f);
		int feetY = (int)((0f - feet.y) * 40f);
		double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
		foreach (GroundItem item in All)
		{
			if (!((Object)(object)item == (Object)null) && ((Behaviour)item).enabled && !item.removing && item.resting && item.amount > 0 && !((item.itemId == 0) ? (!meso) : (!items)) && !(realtimeSinceStartupAsDouble - item.lastPetRequest < 3.0) && NativeDropMotion.InPickupRange(feetX, feetY, item.targetX, item.targetY))
			{
				item.lastPetRequest = realtimeSinceStartupAsDouble;
				return item.Pickup(slot, target);
			}
		}
		return false;
	}

	private bool Pickup(int petSlot = -1, Func<Nullable> petTarget = null)
	{
		if (bound != null)
		{
			if (petSlot < 0)
			{
				if (AuthoritativePickup != null)
				{
					return AuthoritativePickup(bound);
				}
				return false;
			}
			if (AuthoritativePetPickup != null)
			{
				return AuthoritativePetPickup(bound, petSlot);
			}
			return false;
		}
		if (itemId == 0)
		{
			if (amount > 0 && Profile.gold > long.MaxValue - amount)
			{
				return false;
			}
			Profile.AddGold(amount);
			lastPickup = "+" + amount + " 金币";
		}
		else
		{
			string text = ((FieldCatalog.Items.TryGetValue(itemId, out var value) && !string.IsNullOrEmpty(value.name)) ? value.name : itemId.ToString());
			if (itemId / 10000 == 238)
			{
				if (!NativeMonsterBook.Collect(itemId, amount))
				{
					return false;
				}
			}
			else if (MapItemPickup.IsNxCard(itemId))
			{
				if (!GameSession.Net.Cash(new CashRequest
				{
					characterId = GameSession.CharacterId,
					operation = CashOperation.AdjustBalance,
					currency = 1,
					amount = checked(((itemId == 4031865) ? 100 : 250) * amount)
				}).ok)
				{
					return false;
				}
			}
			else if (!Profile.TryAddItem(itemId, amount))
			{
				return false;
			}
			lastPickup = text + " x" + amount;
		}
		NativeGainNotice.Pickup(itemId, amount);
		lastPickupAt = Time.realtimeSinceStartupAsDouble;
		Remove((petSlot >= 0) ? 5 : 2, petTarget ?? new Func<Nullable>(LocalPickupTarget), playSound: true);
		return true;
	}

	internal static Nullable LocalPickupTarget()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
		if (!((Object)(object)playerCtrl != (Object)null))
		{
			return null;
		}
		return playerCtrl.FeetPos + Vector2.up * (playerCtrl.BodyHeight * 0.5f);
	}

	public void Remove(int reason, Func<Nullable> target, bool playSound = false)
	{
		if (!removing)
		{
			removing = true;
			removedAt = Time.realtimeSinceStartupAsDouble;
			picking = reason == 2 || reason == 3 || reason == 5;
			pickupTarget = target;
			if (reason == 1 || (picking && target == null))
			{
				Object.Destroy((Object)(object)((Component)this).gameObject);
			}
			else if (picking & playSound)
			{
				AudioManager.Play("Game/PickUpItem", 1f, pitchVary: false);
			}
		}
	}

	public static int Count()
	{
		int num = 0;
		foreach (GroundItem item in All)
		{
			if ((Object)(object)item != (Object)null && ((Behaviour)item).enabled && !item.removing)
			{
				num++;
			}
		}
		return num;
	}

	public static void Clear()
	{
		GroundItem[] array = All.ToArray();
		foreach (GroundItem groundItem in array)
		{
			if ((Object)(object)groundItem != (Object)null)
			{
				((Behaviour)groundItem).enabled = false;
				Object.Destroy((Object)(object)((Component)groundItem).gameObject);
			}
		}
		All.Clear();
	}
}

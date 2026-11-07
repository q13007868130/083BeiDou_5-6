using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public class DeathFlow : MonoBehaviour
{
	public enum Phase
	{
		Idle,
		Dying,
		Confirm
	}

	public Phase phase;

	private const float ConfirmDelay = 2.2f;

	private const float DropDuration = 0.88f;

	private GameObject tombGo;

	private SpriteRenderer tombSr;

	private readonly List<ImgNode> fall = new List<ImgNode>();

	private Sprite land;

	private Vector3 tombTarget;

	private float timer;

	private PlayerCtrl deadPlayer;

	private PlayerDeadResponse settlement;

	public static DeathFlow I { get; private set; }

	public bool Active => phase != Phase.Idle;

	public static bool OfferWheel
	{
		get
		{
			FieldMap fieldMap = GameLoop.I?.field?.Map;
			if (fieldMap != null && WheelAllowed(fieldMap.fieldType, fieldMap.id))
			{
				return Profile.ItemCount(5510000) > 0;
			}
			return false;
		}
	}

	private void Awake()
	{
		I = this;
	}

	private void OnDestroy()
	{
		RemoveTombstone();
		if ((Object)(object)I == (Object)(object)this)
		{
			I = null;
		}
	}

	private void RemoveTombstone()
	{
		deadPlayer?.ClearDeathPresentation();
		deadPlayer = null;
		if ((Object)(object)tombGo != (Object)null)
		{
			if (Application.isPlaying)
			{
				Object.Destroy((Object)(object)tombGo);
			}
			else
			{
				Object.DestroyImmediate((Object)(object)tombGo);
			}
		}
		tombGo = null;
		tombSr = null;
	}

	public void Begin(PlayerCtrl p)
	{
		if (phase == Phase.Idle)
		{
			phase = Phase.Dying;
			timer = 0f;
			p.PlayDeadPose();
			SpawnTombstone(p);
			int mapId = (((Object)(object)GameLoop.I != (Object)null && (Object)(object)GameLoop.I.field != (Object)null) ? GameLoop.I.field.Map.id : Profile.LastMapId);
			settlement = GameSession.PlayerDead(mapId);
			GameSession.SaveToDb();
		}
	}

	private void SpawnTombstone(PlayerCtrl p)
	{
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Expected Obj, but got Unknown
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		fall.Clear();
		fall.AddRange(WzAssets.Frames(WzAssets.Node("Effect/Tomb.img/fall")));
		land = WzAssets.Sprite(WzAssets.Node("Effect/Tomb.img/land/0"));
		RemoveTombstone();
		deadPlayer = p;
		deadPlayer?.PresentDeath(0f);
		tombGo = new GameObject("[Tombstone]");
		tombTarget = (((Object)(object)p != (Object)null) ? Vector2.op_Implicit(p.FeetPos) : Vector3.zero);
		tombGo.transform.position = tombTarget + Vector3.up * 15f;
		tombSr = tombGo.AddComponent<SpriteRenderer>();
		((Renderer)tombSr).sortingOrder = (((Object)(object)p != (Object)null) ? (p.SortingOrder + 1) : 320);
		tombSr.sprite = ((fall.Count > 0) ? WzAssets.Sprite(fall[0]) : land);
	}

	private void Update()
	{
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		if (phase == Phase.Idle)
		{
			return;
		}
		timer += Time.unscaledDeltaTime;
		deadPlayer?.PresentDeath(timer);
		if ((Object)(object)tombSr != (Object)null)
		{
			if ((Object)(object)deadPlayer != (Object)null)
			{
				((Renderer)tombSr).sortingOrder = deadPlayer.SortingOrder + 1;
			}
			tombGo.transform.position = tombTarget + Vector3.up * (15f * (1f - Mathf.Clamp01(timer / 0.88f)));
			float num = timer;
			Sprite sprite = land;
			foreach (ImgNode item in fall)
			{
				float num2 = (float)Mathf.Max(1, Mathf.Abs(item.Int("delay", 100))) / 1000f;
				if (num < num2)
				{
					sprite = WzAssets.Sprite(item);
					break;
				}
				num -= num2;
			}
			tombSr.sprite = sprite;
		}
		if (phase == Phase.Dying && timer > 2.2f)
		{
			phase = Phase.Confirm;
		}
	}

	public static bool WheelAllowed(int fieldType, int mapId)
	{
		switch (fieldType)
		{
		default:
			if (fieldType != 7 && fieldType != 10 && fieldType != 11 && fieldType != 15 && mapId / 100000000 != 9 && mapId / 1000 != 200090)
			{
				return mapId / 1000000 != 390;
			}
			break;
		case 1:
		case 3:
		case 4:
		case 5:
			break;
		}
		return false;
	}

	private void OnGUI()
	{
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Invalid comparison between Unknown and I4
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Invalid comparison between Unknown and I4
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Invalid comparison between Unknown and I4
		if (phase != Phase.Confirm)
		{
			return;
		}
		UiKit.Begin();
		Matrix4x4 matrix = GUI.matrix;
		int depth = GUI.depth;
		float s = BottomBar.S;
		GUI.matrix = matrix * Matrix4x4.Scale(new Vector3(s, s, 1f));
		GUI.depth = -100;
		try
		{
			float num = UiKit.W / s / 2f - 143f;
			float num2 = UiKit.H / s / 2f - 203f;
			bool offerWheel = OfferWheel;
			Texture2D val = Art.Tex("wz://UI/UIWindow.img/Notice/" + (offerWheel ? 2 : 0));
			if ((Object)(object)val != (Object)null)
			{
				GUI.DrawTexture(new Rect(num, num2, 286f, 146f), (Texture)(object)val);
			}
			bool flag = (offerWheel ? NativeUi.Button(new Rect(num + 92f, num2 + 115f, 47f, 19f), "UI/Basic.img/BtYes2", Pointer.Pos / s) : NativeUi.Button(new Rect(num + 124f, num2 + 115f, 47f, 19f), "UI/Basic.img/BtOK2", Pointer.Pos / s));
			if (offerWheel && NativeUi.Button(new Rect(num + 148f, num2 + 115f, 47f, 19f), "UI/Basic.img/BtNo2", Pointer.Pos / s))
			{
				Confirm();
				return;
			}
			Event current = Event.current;
			if ((int)current.type == 4 && ((int)current.keyCode == 13 || (int)current.keyCode == 271))
			{
				flag = true;
				current.Use();
			}
			if (flag)
			{
				Confirm(offerWheel);
			}
			else if (timer > 602.2f)
			{
				Confirm();
			}
		}
		finally
		{
			GUI.matrix = matrix;
			GUI.depth = depth;
		}
	}

	private void Confirm(bool wheel = false)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		bool usedSafetyCharm = settlement != null && settlement.usedSafetyCharm;
		PlayerCtrl playerCtrl = GameLoop.I?.player;
		ChangeMapResponse changeMapResponse = GameSession.Respawn(usedSafetyCharm, wheel, ((Object)(object)playerCtrl != (Object)null) ? ((int)(playerCtrl.FeetPos.x * 40f)) : 0, ((Object)(object)playerCtrl != (Object)null) ? ((int)((0f - playerCtrl.FeetPos.y) * 40f)) : 0);
		if (!changeMapResponse.ok)
		{
			LocalRespawn();
			return;
		}
		GameLoop i = GameLoop.I;
		if (changeMapResponse.wheelUsed && (Object)(object)i?.field != (Object)null)
		{
			i.field.WarpTo(changeMapResponse.mapId, null, changeMapResponse.spawnPoint);
		}
		else if ((Object)(object)i != (Object)null && (Object)(object)i.field != (Object)null && changeMapResponse.mapId != i.field.Map.id)
		{
			i.field.Enter(changeMapResponse.mapId, null);
		}
		else if ((Object)(object)i != (Object)null && (Object)(object)i.field != (Object)null)
		{
			i.field.RespawnPlayer();
		}
		if ((Object)(object)i != (Object)null && (Object)(object)i.player != (Object)null)
		{
			i.player.Revive(changeMapResponse.hp, changeMapResponse.mp);
			i.player.ResetTerrain();
			if ((Object)(object)i.camRig != (Object)null)
			{
				i.camRig.SnapTo(i.player.worldX);
			}
		}
		Profile.LastMapId = changeMapResponse.mapId;
		Profile.Save();
		Finish();
	}

	private void LocalRespawn()
	{
		GameLoop i = GameLoop.I;
		if ((Object)(object)i != (Object)null && (Object)(object)i.field != (Object)null)
		{
			i.field.RespawnPlayer();
		}
		if ((Object)(object)i != (Object)null && (Object)(object)i.player != (Object)null)
		{
			i.player.Revive(50, (i.player.mpMax > 0f) ? ((int)i.player.mpMax) : 5);
			i.player.ResetTerrain();
		}
		Finish();
	}

	private void Finish()
	{
		phase = Phase.Idle;
		timer = 0f;
		settlement = null;
		RemoveTombstone();
		GameLoop i = GameLoop.I;
		if ((Object)(object)i != (Object)null)
		{
			i.gameOver = false;
			i.deaths.Clear();
		}
	}

	public void ForceClear()
	{
		if (phase != Phase.Idle)
		{
			Finish();
		}
	}
}

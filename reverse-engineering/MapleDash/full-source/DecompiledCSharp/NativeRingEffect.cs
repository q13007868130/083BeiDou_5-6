using System.Collections;
using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public sealed class NativeRingEffect : MonoBehaviour
{
	private static readonly int[] cells = new int[4] { -12, -13, -15, -16 };

	private int current = -1;

	private float nextCheck;

	private readonly List<GameObject> owned = new List<GameObject>();

	private Coroutine emitter;

	private ImgNode effect;

	private string path;

	private PlayerCtrl player;

	private static bool debugTried;

	private static bool debugOn;

	private void Awake()
	{
		player = ((Component)this).GetComponent<PlayerCtrl>();
	}

	private void Update()
	{
		if (!(Time.unscaledTime < nextCheck))
		{
			nextCheck = Time.unscaledTime + 0.2f;
			int num = CurrentRing();
			if (num != current)
			{
				Rebuild(num);
			}
		}
	}

	private static int CurrentRing()
	{
		if (!GameSession.InGame)
		{
			return 0;
		}
		int[] array = cells;
		for (int i = 0; i < array.Length; i++)
		{
			int num = Profile.EquipOfSlot(EquipSlot.WornSlotOf(array[i]));
			if (num > 0 && EquipCatalog.Info(num)?.Get("effect")?.Get("path") != null)
			{
				return num;
			}
		}
		return 0;
	}

	private void Rebuild(int id)
	{
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		Clear();
		current = id;
		if (id <= 0)
		{
			return;
		}
		if ((Object)(object)player == (Object)null)
		{
			player = ((Component)this).GetComponent<PlayerCtrl>();
		}
		ImgNode imgNode = EquipCatalog.Info(id)?.Get("effect");
		if (imgNode == null)
		{
			return;
		}
		path = imgNode.Text("path");
		if (string.IsNullOrEmpty(path))
		{
			return;
		}
		effect = WzAssets.Node(path);
		DebugRing(id, imgNode);
		if (effect == null || effect.Children.Count == 0)
		{
			return;
		}
		int order = (((Object)(object)player != (Object)null) ? player.SortingOrder : 300) + ((imgNode.Int("z") >= 0) ? 1 : (-1));
		if (imgNode.Get("fixed") != null || imgNode.Get("animate") != null)
		{
			float num = ((imgNode.Int("pos") == 1) ? 1.5f : 0f);
			Vector3 position = (((Object)(object)player != (Object)null) ? Vector2.op_Implicit(player.FeetPos) : ((Component)this).transform.position) + Vector3.up * num;
			{
				foreach (ImgNode child in effect.Children)
				{
					if (IsNumber(child.Name))
					{
						NativeImgAnimation nativeImgAnimation = NativeImgAnimation.Spawn(path + "/" + child.Name, ((Object)(object)player != (Object)null) ? ((Component)player).transform : ((Component)this).transform, position, order, flip: false, repeat: true);
						if ((Object)(object)nativeImgAnimation != (Object)null)
						{
							owned.Add(((Component)nativeImgAnimation).gameObject);
						}
					}
				}
				return;
			}
		}
		if (imgNode.Get("follow") != null || imgNode.Get("interval") != null || imgNode.Get("genOnMove") != null)
		{
			emitter = ((MonoBehaviour)this).StartCoroutine(Emit(imgNode, order));
		}
	}

	private IEnumerator Emit(ImgNode fx, int order)
	{
		float interval = Mathf.Max(0.03f, (float)fx.Int("interval", 180) / 1000f);
		float life = Mathf.Clamp((float)fx.Int("delay", 3000) / 1000f, 0.1f, 30f);
		int emission = Mathf.Max(1, fx.Int("emission", 1));
		float left = fx.Int("left");
		float right = fx.Int("right");
		float top = fx.Int("top");
		float bottom = fx.Int("bottom");
		float dx = fx.Int("dx");
		float dy = fx.Int("dy");
		bool onlyOnMove = fx.Get("genOnMove") != null;
		List<string> groups = new List<string>();
		foreach (ImgNode child in effect.Children)
		{
			if (IsNumber(child.Name))
			{
				groups.Add(child.Name);
			}
		}
		if (groups.Count == 0)
		{
			yield break;
		}
		int cursor = 0;
		Vector3 last = (((Object)(object)player != (Object)null) ? Vector2.op_Implicit(player.FeetPos) : ((Component)this).transform.position);
		while (true)
		{
			Vector3 val = (((Object)(object)player != (Object)null) ? Vector2.op_Implicit(player.FeetPos) : ((Component)this).transform.position);
			Vector3 val2 = val - last;
			bool flag = val2.sqrMagnitude > 1E-06f;
			last = val;
			if (!onlyOnMove | flag)
			{
				for (int i = 0; i < emission; i++)
				{
					string text = groups[cursor++ % groups.Count];
					float num = Random.Range(Mathf.Min(left, right), Mathf.Max(left, right));
					float num2 = Random.Range(Mathf.Min(top, bottom), Mathf.Max(top, bottom));
					Vector3 position = val + new Vector3(num / 40f, (0f - num2) / 40f, 0f);
					NativeImgAnimation nativeImgAnimation = NativeImgAnimation.Spawn(path + "/" + text, null, position, order, flip: false, repeat: true, life);
					if (!((Object)(object)nativeImgAnimation == (Object)null))
					{
						((Component)nativeImgAnimation).gameObject.AddComponent<NativeRingParticle>().Init(((Object)(object)player != (Object)null) ? ((Component)player).transform : null, new Vector3(Random.Range(-0.35f, 0.35f) * dx / 40f, Mathf.Abs(dy) / 40f, 0f));
						owned.Add(((Component)nativeImgAnimation).gameObject);
					}
				}
			}
			yield return (object)new WaitForSeconds(interval);
		}
	}

	private static bool IsNumber(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return false;
		}
		foreach (char c in name)
		{
			if (c < '0' || c > '9')
			{
				return false;
			}
		}
		return true;
	}

	private void DebugRing(int id, ImgNode fx)
	{
		if (!debugTried)
		{
			debugTried = true;
			debugOn = PlayerPrefs.GetInt("debug_ringfx", 0) != 0;
			if (debugOn)
			{
				PlayerPrefs.DeleteKey("debug_ringfx");
			}
		}
		if (!debugOn)
		{
			return;
		}
		int num = 0;
		if (effect != null)
		{
			foreach (ImgNode child in effect.Children)
			{
				if (IsNumber(child.Name))
				{
					num++;
				}
			}
		}
		string text;
		if (fx.Get("fixed") != null || fx.Get("animate") != null)
		{
			text = "fixed";
		}
		else
		{
			text = ((fx.Get("interval") != null || fx.Get("follow") != null || fx.Get("genOnMove") != null) ? "emitter" : "none");
		}
		Debug.Log((object)("[Ring/Fx] ring=" + id + " path=" + path + " node=" + (effect != null) + " groups=" + num + " mode=" + text));
	}

	private void Clear()
	{
		if (emitter != null)
		{
			((MonoBehaviour)this).StopCoroutine(emitter);
			emitter = null;
		}
		foreach (GameObject item in owned)
		{
			if ((Object)(object)item != (Object)null)
			{
				if (Application.isPlaying)
				{
					Object.Destroy((Object)(object)item);
				}
				else
				{
					Object.DestroyImmediate((Object)(object)item);
				}
			}
		}
		owned.Clear();
	}

	private void OnDisable()
	{
		Clear();
		current = -1;
	}

	private void OnDestroy()
	{
		Clear();
	}
}

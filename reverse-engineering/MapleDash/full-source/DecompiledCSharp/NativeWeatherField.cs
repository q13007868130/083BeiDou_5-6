using System;
using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public sealed class NativeWeatherField : MonoBehaviour
{
	private sealed class Sheet
	{
		public SpriteRenderer Image;

		public string Group;
	}

	private static Dictionary<string, object> data;

	private static NativeWeatherService detached;

	private static NativeWeatherField current;

	public readonly NativeWeatherState State = new NativeWeatherState();

	private readonly Dictionary<string, float> levels = new Dictionary<string, float>();

	private readonly List<Sheet> sheets = new List<Sheet>();

	private readonly Dictionary<SpriteRenderer, Color> scenery = new Dictionary<SpriteRenderer, Color>();

	private NativeMapView view;

	private NativeWeatherService server;

	private ImgNode mapNode;

	private bool mounted;

	private int mapId;

	private int revision = -1;

	private int syncedRevision;

	private long syncAt;

	private long tickAt;

	private float night;

	private float boost;

	private Vector3 tint = Vector3.one * 255f;

	private bool primed;

	private NativeWeatherGround ground;

	private NativeWeatherSound sound;

	private NativeWeatherParticles particles;

	private Color groundTint = Color.white;

	private Color rainTint = Color.white;

	public static bool ClientEnabled { get; set; } = true;

	public bool HasSky { get; private set; }

	public bool Forced { get; private set; }

	public bool Active { get; private set; }

	public bool WaterBase { get; private set; }

	public static Dictionary<string, object> Data
	{
		get
		{
			if (data == null)
			{
				data = (Dictionary<string, object>)MiniJson.Parse(Resources.Load<TextAsset>("NativeWeather").text);
			}
			return data;
		}
	}

	private static long Now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

	private static long LocalNow => (long)(Time.realtimeSinceStartupAsDouble * 1000.0);

	private static double Number(object x)
	{
		return Convert.ToDouble(x);
	}

	private static int Int(object x)
	{
		return Convert.ToInt32(x);
	}

	private static NativeWeatherService Service()
	{
		if (GameSession.Server?.Weather != null)
		{
			NativeWeatherHost.Shared = GameSession.Server.Weather;
			return GameSession.Server.Weather;
		}
		if (NativeWeatherHost.Shared != null)
		{
			if (GameSession.Server != null)
			{
				GameSession.Server.Weather = NativeWeatherHost.Shared;
			}
			else
			{
				detached = NativeWeatherHost.Shared;
			}
			return NativeWeatherHost.Shared;
		}
		if (GameSession.Server == null && detached != null)
		{
			NativeWeatherHost.Shared = detached;
			return detached;
		}
		List<object> list = (List<object>)Data["regions"];
		NativeWeatherRegion[] array = new NativeWeatherRegion[list.Count];
		for (int i = 0; i < list.Count; i++)
		{
			Dictionary<string, object> dictionary = (Dictionary<string, object>)list[i];
			List<object> list2 = (List<object>)dictionary["weights"];
			double[] array2 = new double[list2.Count];
			for (int j = 0; j < array2.Length; j++)
			{
				array2[j] = Number(list2[j]);
			}
			array[i] = new NativeWeatherRegion
			{
				Name = (string)dictionary["name"],
				Forced = Int(dictionary["forced"]),
				Tint = Int(dictionary["tint"]),
				Palette = i,
				Weights = array2
			};
		}
		Random random = new Random();
		NativeWeatherService nativeWeatherService = new NativeWeatherService(array, Now, random.NextDouble);
		if (GameSession.Server != null)
		{
			GameSession.Server.Weather = nativeWeatherService;
		}
		else
		{
			detached = nativeWeatherService;
		}
		NativeWeatherHost.Shared = nativeWeatherService;
		if (NativeWeatherHost.FromGame == null)
		{
			NativeWeatherHost.FromGame = () => GameSession.Server?.Weather;
		}
		return nativeWeatherService;
	}

	public static bool SkyForMap(int id, ImgNode map)
	{
		foreach (object item in (List<object>)Data["excluded"])
		{
			if (Int(item) == id)
			{
				return false;
			}
		}
		ImgNode imgNode = map.Get("back/0");
		if (imgNode == null)
		{
			return false;
		}
		foreach (Dictionary<string, object> item2 in (List<object>)Data["skyBases"])
		{
			if (string.Equals((string)item2["bank"], imgNode.Text("bS"), StringComparison.OrdinalIgnoreCase) && Int(item2["no"]) == imgNode.Int("no"))
			{
				return true;
			}
		}
		return false;
	}

	public void Bind(NativeMapView view, int id, ImgNode map)
	{
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		_ = Data;
		this.view = view;
		mapId = id;
		mapNode = map;
		if (!ClientEnabled)
		{
			return;
		}
		current = this;
		HasSky = SkyForMap(id, map);
		server = Service();
		Forced = IsForcedByServer();
		Active = HasSky || id / 1000000 == 230 || Forced;
		WaterBase = string.Equals(map.Get("back/0")?.Text("bS"), "nautilusPort", StringComparison.OrdinalIgnoreCase);
		server.Roll(Now, DateTime.Now.Month);
		server.Sync(id, snap: true, State, Now);
		RebaseSync();
		foreach (SpriteRenderer item in view.WeatherTintBacks())
		{
			scenery[item] = item.color;
		}
		if (HasSky || Forced)
		{
			MountLayers();
		}
		Tick();
	}

	private bool IsForcedByServer()
	{
		if (server == null)
		{
			return false;
		}
		if (server.OverrideSky >= 0)
		{
			return true;
		}
		int num = NativeWeatherService.RegionForMap(mapId);
		if (num >= 0 && num < server.RegionCount)
		{
			return server.Region(num).Forced >= 0;
		}
		return false;
	}

	private void MountLayers()
	{
		if (mounted || (Object)(object)view == (Object)null || Data == null)
		{
			return;
		}
		mounted = true;
		int num = 0;
		foreach (Dictionary<string, object> item in (List<object>)Data["layers"])
		{
			if (!HasSky && (string)item["group"] == "sky")
			{
				num++;
				continue;
			}
			SpriteRenderer val = view.AddWeatherBack(item, -1100 + num++);
			if ((Object)(object)val != (Object)null)
			{
				sheets.Add(new Sheet
				{
					Image = val,
					Group = (string)item["group"]
				});
			}
		}
		if (HasSky)
		{
			view.PromoteWeatherBacks(mapNode);
		}
		ground = ((Component)this).gameObject.AddComponent<NativeWeatherGround>();
		FieldMap fieldMap = new FieldMap
		{
			id = mapId
		};
		ImgNode imgNode = mapNode.Get("foothold");
		if (imgNode != null)
		{
			foreach (ImgNode child in imgNode.Children)
			{
				foreach (ImgNode child2 in child.Children)
				{
					foreach (ImgNode child3 in child2.Children)
					{
						fieldMap.footholds.Add(new FieldFoothold
						{
							id = int.Parse(child3.Name),
							layer = int.Parse(child.Name),
							group = int.Parse(child2.Name),
							x1 = child3.Int("x1"),
							y1 = child3.Int("y1"),
							x2 = child3.Int("x2"),
							y2 = child3.Int("y2")
						});
					}
				}
			}
		}
		ground.Bind(fieldMap);
		sound = ((Component)this).gameObject.AddComponent<NativeWeatherSound>();
		particles = ((Component)this).gameObject.AddComponent<NativeWeatherParticles>();
	}

	private void RebaseSync()
	{
		int num = State.Age(Now);
		int rainbowSeconds = State.Rainbow(Now);
		State.Set((int)State.Minute, State.MsPerMinute, State.Sky, (State.Snap ? 1 : 0) | (State.Frozen ? 2 : 0) | (State.BareSky ? 4 : 0) | (State.Disabled ? 8 : 0), num / 1000, State.Tint, rainbowSeconds, State.Palette, num, State.Token, LocalNow);
		syncAt = LocalNow + 60000;
		if (server != null)
		{
			syncedRevision = server.Revision;
		}
	}

	public static bool HandlePacket(byte[] packet, int offset = 0)
	{
		if ((Object)(object)current != (Object)null)
		{
			return current.State.Read(packet, offset, LocalNow);
		}
		return false;
	}

	public void Tick()
	{
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_023a: Unknown result type (might be due to invalid IL or missing references)
		//IL_027c: Unknown result type (might be due to invalid IL or missing references)
		//IL_029b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0258: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0453: Unknown result type (might be due to invalid IL or missing references)
		//IL_0458: Unknown result type (might be due to invalid IL or missing references)
		//IL_045b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0460: Unknown result type (might be due to invalid IL or missing references)
		//IL_0485: Unknown result type (might be due to invalid IL or missing references)
		//IL_048a: Unknown result type (might be due to invalid IL or missing references)
		//IL_04bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0678: Unknown result type (might be due to invalid IL or missing references)
		//IL_0660: Unknown result type (might be due to invalid IL or missing references)
		if (!ClientEnabled || server == null)
		{
			return;
		}
		long localNow = LocalNow;
		bool flag = server.Roll(Now, DateTime.Now.Month);
		bool flag2 = server.Revision != syncedRevision;
		if ((localNow >= syncAt) | flag | flag2)
		{
			server.Sync(mapId, flag2, State, Now);
			RebaseSync();
		}
		Forced = IsForcedByServer();
		if (!mounted && (HasSky || Forced))
		{
			Active = true;
			MountLayers();
		}
		State.Tick(localNow);
		if (localNow < tickAt)
		{
			return;
		}
		tickAt = localNow + 30;
		bool flag3 = State.Revision != revision && State.Snap;
		if (State.Revision != revision)
		{
			revision = State.Revision;
			if (flag3)
			{
				primed = false;
			}
		}
		bool flag4 = HasSky || Forced;
		List<object> list = (List<object>)((Dictionary<string, object>)((List<object>)Data["profiles"])[flag4 ? State.Sky : 0])["values"];
		float num = (float)Number(list[7]);
		float num2 = NativeWeatherState.Night(State.Minute);
		night = (flag3 ? num2 : Mathf.MoveTowards(night, num2, 0.015f));
		Vector3 val = PaletteTint(night);
		Vector3 val2 = new Vector3(Mathf.Min(255f, val.x * (float)Number(list[3]) / 74f), Mathf.Min(255f, val.y * (float)Number(list[4]) / 90f), Mathf.Min(255f, val.z * (float)Number(list[5]) / 140f));
		float num3 = (float)Number(list[6]);
		if (!primed)
		{
			tint = val2;
			boost = num3;
			primed = true;
		}
		else
		{
			tint = new Vector3(Mathf.MoveTowards(tint.x, val2.x, num * 255f), Mathf.MoveTowards(tint.y, val2.y, num * 255f), Mathf.MoveTowards(tint.z, val2.z, num * 255f));
			boost = Mathf.MoveTowards(boost, num3, num);
		}
		SetLevel("cloud", (float)Number(list[0]), num, flag3);
		SetLevel("rain", (float)Number(list[1]), num, flag3);
		SetLevel("dust", (State.Sky == 8) ? 1 : 0, num, flag3);
		SetLevel("fog", (State.Sky == 5) ? 1 : 0, num, flag3);
		SetLevel("snow", (State.Sky == 2) ? 1 : 0, num, flag3);
		SetLevel("rainbow", (State.Rainbow(localNow) > 0) ? (0.22f * (1f - night)) : 0f, num, flag3);
		if (!Active)
		{
			return;
		}
		int num4 = State.Age(localNow);
		float num5 = ((flag4 && State.Sky == 4 && levels["rain"] >= 0.7f && num4 > 0) ? NativeWeatherLightning.Evaluate((uint)num4, State.Token, State.Palette) : 0f);
		float num6 = Mathf.Max(night, boost) * (1f - 0.697f * num5);
		Color val3 = TintColor(1f, num6);
		groundTint = Color.white;
		rainTint = new Color(1f, 1f, 1f, levels["rain"]);
		foreach (KeyValuePair<SpriteRenderer, Color> item in scenery)
		{
			if ((Object)(object)item.Key != (Object)null)
			{
				item.Key.color = item.Value * val3;
			}
		}
		view.SetWeatherSkyVisibility(State.BareSky);
		foreach (Sheet sheet in sheets)
		{
			float num7;
			if (sheet.Group == "sky")
			{
				if (WaterBase)
				{
					num7 = 0f;
				}
				else
				{
					num7 = (((Object)(object)Camera.main != (Object)null && Camera.main.orthographicSize * 2f * 40f >= 900f) ? ((float)((night > 0.5f) ? 1 : 0)) : night);
				}
			}
			else
			{
				num7 = ((!(sheet.Group == "night")) ? (levels[sheet.Group] * ((sheet.Group == "fog") ? 0.6f : 1f)) : night);
			}
			if (sheet.Group == "sky" || sheet.Group == "night")
			{
				num7 *= 1f - 0.578f * num5;
			}
			sheet.Image.color = (Color)((sheet.Group == "sky" || sheet.Group == "night") ? new Color(1f, 1f, 1f, num7) : TintColor(num7, num6 * ((sheet.Group == "rain") ? 0.55f : 1f)));
		}
	}

	private void SetLevel(string group, float target, float step, bool snap)
	{
		levels.TryGetValue(group, out var value);
		levels[group] = (snap ? target : Mathf.MoveTowards(value, target, step));
	}

	private Vector3 PaletteTint(float n)
	{
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		if (!State.HasPalette)
		{
			return new Vector3((float)((State.Tint >> 16) & 0xFF), (float)((State.Tint >> 8) & 0xFF), (float)(State.Tint & 0xFF));
		}
		List<object> list = (List<object>)((Dictionary<string, object>)((List<object>)Data["palettes"])[State.Palette])["colors"];
		Vector3 val = new Vector3((float)Int(list[0]), (float)Int(list[1]), (float)Int(list[2]));
		Vector3 val2 = new Vector3((float)Int(list[3]), (float)Int(list[4]), (float)Int(list[5]));
		if (!(n <= 0.35f))
		{
			return Vector3.Lerp(val, val2, (n - 0.35f) / 0.65f);
		}
		return Vector3.Lerp(Vector3.one * 255f, val, n / 0.35f);
	}

	private Color TintColor(float alpha, float darkness)
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		float num = Golden(State.Minute, 300, 420);
		float num2 = Golden(State.Minute, 1020, 1140);
		float num3 = Mathf.Max(num, num2) * 0.85f * Mathf.Max(0f, 1f - boost * 2f);
		Vector3 val = Vector3.Lerp(tint, (num > num2) ? new Vector3(242f, 158f, 184f) : new Vector3(255f, 162f, 72f), num3);
		return new Color((float)(255 - (int)(darkness * (255f - val.x))) / 255f, (float)(255 - (int)(darkness * (255f - val.y))) / 255f, (float)(255 - (int)(darkness * (255f - val.z))) / 255f, (float)(int)(Mathf.Clamp01(alpha) * 255f) / 255f);
	}

	private static float Golden(double minute, int start, int end)
	{
		if (!(minute < (double)start) && !(minute >= (double)end))
		{
			return Mathf.Sin((float)(minute - (double)start) / (float)(end - start) * MathF.PI);
		}
		return 0f;
	}

	private void LateUpdate()
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		if (ClientEnabled && Active)
		{
			levels.TryGetValue("rain", out var value);
			ground?.Tick(State.Sky, State.Age(LocalNow), value, LocalNow, groundTint, rainTint);
			Dictionary<string, object> dictionary = (Dictionary<string, object>)((List<object>)Data["profiles"])[State.Sky];
			sound?.Tick(Int(((List<object>)dictionary["values"])[8]), value);
			particles?.Tick(Int(((List<object>)dictionary["values"])[2]), State.Sky, State.Token, Time.realtimeSinceStartupAsDouble, groundTint);
		}
	}

	private void OnDisable()
	{
		if ((Object)(object)current == (Object)(object)this)
		{
			current = null;
		}
	}

	public NativeWeatherField()
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
	}
}

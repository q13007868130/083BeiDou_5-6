using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public sealed class NativeSkillVideo : MonoBehaviour
{
	public enum Channel
	{
		PlayerSkill,
		BossScene
	}

	private static readonly NativeSkillVideo[] active = new NativeSkillVideo[2];

	private static readonly List<NativeSkillVideo> layers = new List<NativeSkillVideo>();

	private static Dictionary<string, object> skillFiles;

	private static string pendingBoss;

	private static bool warnedNoMovie;

	private McvPlayback playback;

	private SpriteRenderer image;

	private Texture2D texture;

	private Sprite sprite;

	private byte[] flipped;

	private byte[] source;

	private int map;

	private PlayerCtrl caster;

	private bool hasCaster;

	private bool layer;

	private bool loop;

	private float delay;

	private static readonly Dictionary<string, byte[]> videoFiles = new Dictionary<string, byte[]>();

	public static bool PlaySkill(int skillId, PlayerCtrl player)
	{
		if (skillFiles == null)
		{
			TextAsset val = Resources.Load<TextAsset>("NativeSkillVideos");
			if ((Object)(object)val == (Object)null)
			{
				return false;
			}
			skillFiles = (Dictionary<string, object>)MiniJson.Parse(val.text);
		}
		if (skillFiles.TryGetValue(skillId.ToString(), out var value))
		{
			return Play(Channel.PlayerSkill, (string)value, player);
		}
		return false;
	}

	public static bool Play(Channel channel, string relative, PlayerCtrl player = null, int? sortingOrder = null)
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Expected Obj, but got Unknown
		int num = (int)channel;
		if (num < 0 || num >= active.Length)
		{
			throw new ArgumentOutOfRangeException("channel");
		}
		Stop(channel);
		if (Path.IsPathRooted(relative) || relative.Contains(".."))
		{
			throw new InvalidDataException("Invalid video path");
		}
		GameObject val = new GameObject("MCV_" + channel);
		NativeSkillVideo nativeSkillVideo = val.AddComponent<NativeSkillVideo>();
		if (!nativeSkillVideo.Attach(File.ReadAllBytes(Path.Combine(WzAssets.DataRoot, relative)), player, sortingOrder ?? ((channel == Channel.BossScene) ? 800 : 801), loop: false, 0f))
		{
			if (GamePlatform.MoviePlayback)
			{
				Debug.LogError((object)("[MCV] " + relative));
			}
			Delete((Object)(object)val);
			return false;
		}
		active[num] = nativeSkillVideo;
		return true;
	}

	public static NativeSkillVideo PlayNode(byte[] data, string name, PlayerCtrl player, int sortingOrder, bool loop = false, float delay = 0f)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected Obj, but got Unknown
		data = Resolve(data);
		if (data == null || data.Length == 0)
		{
			return null;
		}
		GameObject val = new GameObject("MCV_" + name);
		NativeSkillVideo nativeSkillVideo = val.AddComponent<NativeSkillVideo>();
		if (!nativeSkillVideo.Attach(data, player, sortingOrder, loop, delay))
		{
			Delete((Object)(object)val);
			return null;
		}
		nativeSkillVideo.layer = true;
		layers.RemoveAll((NativeSkillVideo item) => (Object)(object)item == (Object)null);
		layers.Add(nativeSkillVideo);
		return nativeSkillVideo;
	}

	private static byte[] Resolve(byte[] data)
	{
		if (data == null || data.Length < 5)
		{
			return data;
		}
		if (data[0] != 109 || data[1] != 99 || data[2] != 118 || data[3] != 58)
		{
			return data;
		}
		string text = Encoding.UTF8.GetString(data, 4, data.Length - 4);
		if (videoFiles.TryGetValue(text, out var value))
		{
			return value;
		}
		if (Path.IsPathRooted(text) || text.Contains(".."))
		{
			Debug.LogError((object)("[MCV] 非法视频路径 " + text));
			return null;
		}
		string path = Path.Combine(WzAssets.DataRoot, text);
		if (!File.Exists(path))
		{
			Debug.LogError((object)("[MCV] 缺少视频 " + text));
			return null;
		}
		byte[] array = File.ReadAllBytes(path);
		if (videoFiles.Count > 8)
		{
			videoFiles.Clear();
		}
		videoFiles[text] = array;
		return array;
	}

	public static void ClearLayers()
	{
		List<NativeSkillVideo> list = new List<NativeSkillVideo>(layers);
		layers.Clear();
		foreach (NativeSkillVideo item in list)
		{
			if ((Object)(object)item != (Object)null)
			{
				item.DestroyLayer();
			}
		}
	}

	private bool Attach(byte[] bytes, PlayerCtrl player, int sortingOrder, bool loop, float delay)
	{
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Expected Obj, but got Unknown
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		if (!GamePlatform.MoviePlayback)
		{
			if (!warnedNoMovie)
			{
				warnedNoMovie = true;
				Debug.LogWarning((object)"[MCV] 当前平台没有视频解码库，跳过所有 MCV 播放");
			}
			return false;
		}
		source = bytes;
		this.loop = loop;
		this.delay = delay;
		try
		{
			playback = new McvPlayback(bytes);
			map = Profile.LastMapId;
			caster = player;
			hasCaster = (Object)(object)player != (Object)null;
			image = ((Component)this).gameObject.AddComponent<SpriteRenderer>();
			((Renderer)image).sortingOrder = sortingOrder;
			texture = new Texture2D((int)playback.Video.Width, (int)playback.Video.Height, (TextureFormat)4, false);
			((Texture)texture).filterMode = (FilterMode)1;
			((Texture)texture).wrapMode = (TextureWrapMode)1;
			sprite = Sprite.Create(texture, new Rect(0f, 0f, (float)((Texture)texture).width, (float)((Texture)texture).height), new Vector2(0.5f, 0.5f), 40f, 0u, (SpriteMeshType)0);
			flipped = new byte[checked(((Texture)texture).width * ((Texture)texture).height * 4)];
			return true;
		}
		catch (Exception ex)
		{
			Debug.LogError((object)("[MCV] " + ex.Message));
			Release();
			return false;
		}
	}

	private void DestroyLayer()
	{
		layers.Remove(this);
		Release();
		((Component)this).gameObject.SetActive(false);
		Delete((Object)(object)((Component)this).gameObject);
	}

	private void Restart()
	{
		playback?.Dispose();
		playback = null;
		try
		{
			playback = new McvPlayback(source);
		}
		catch (Exception ex)
		{
			Debug.LogError((object)("[MCV loop] " + ex.Message));
			DestroyLayer();
		}
	}

	public static bool QueueBoss(string relative)
	{
		if ((Object)(object)active[1] == (Object)null)
		{
			return Play(Channel.BossScene, relative);
		}
		if (pendingBoss == null)
		{
			pendingBoss = relative;
		}
		return true;
	}

	public static void Stop(Channel channel)
	{
		if (channel == Channel.BossScene)
		{
			pendingBoss = null;
		}
		NativeSkillVideo nativeSkillVideo = active[(int)channel];
		if (!((Object)(object)nativeSkillVideo == (Object)null))
		{
			active[(int)channel] = null;
			nativeSkillVideo.Release();
			((Component)nativeSkillVideo).gameObject.SetActive(false);
			Delete((Object)(object)((Component)nativeSkillVideo).gameObject);
		}
	}

	private void LateUpdate()
	{
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_026f: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d2: Unknown result type (might be due to invalid IL or missing references)
		if (playback == null)
		{
			return;
		}
		if (delay > 0f)
		{
			delay -= Time.deltaTime;
			if (delay > 0f)
			{
				return;
			}
		}
		bool finished = playback.Finished;
		bool flag = Profile.LastMapId != map || (hasCaster && ((Object)(object)caster == (Object)null || caster.hp <= 0f || !((Component)caster).gameObject.activeInHierarchy)) || (Object)(object)GameLoop.I == (Object)null || GameLoop.I.gameOver || GameLoop.I.state != GameLoop.GameState.Playing;
		if (layer)
		{
			if (flag)
			{
				DestroyLayer();
				return;
			}
			if (finished)
			{
				if (!loop)
				{
					DestroyLayer();
					return;
				}
				Restart();
			}
		}
		else if (flag | finished)
		{
			if (playback.Error != null)
			{
				Debug.LogError((object)("[MCV] " + playback.Error.Message));
			}
			bool flag2 = (Object)(object)active[1] == (Object)(object)this;
			string text = (((flag2 & finished) && Profile.LastMapId == map && (Object)(object)GameLoop.I != (Object)null && !GameLoop.I.gameOver && GameLoop.I.state == GameLoop.GameState.Playing) ? pendingBoss : null);
			if (flag2)
			{
				active[1] = null;
				pendingBoss = null;
			}
			Release();
			Delete((Object)(object)((Component)this).gameObject);
			if (text != null)
			{
				Play(Channel.BossScene, text);
			}
			return;
		}
		if (playback == null)
		{
			return;
		}
		byte[] array = playback.TakeFrame();
		if (array != null)
		{
			int num = ((Texture)texture).width * 4;
			for (int i = 0; i < ((Texture)texture).height; i++)
			{
				Buffer.BlockCopy(array, i * num, flipped, (((Texture)texture).height - 1 - i) * num, num);
			}
			texture.LoadRawTextureData(flipped);
			texture.Apply(false, false);
			image.sprite = sprite;
		}
		Camera main = Camera.main;
		if (!((Object)(object)main == (Object)null) && main.orthographic)
		{
			((Component)this).transform.position = new Vector3(((Component)main).transform.position.x, ((Component)main).transform.position.y, 0f);
			float num2 = main.orthographicSize * 2f;
			((Component)this).transform.localScale = new Vector3(num2 * main.aspect * 40f / (float)((Texture)texture).width, num2 * 40f / (float)((Texture)texture).height, 1f);
		}
	}

	private static void Delete(Object value)
	{
		if (Application.isPlaying)
		{
			Object.Destroy(value);
		}
		else
		{
			Object.DestroyImmediate(value);
		}
	}

	private void Release()
	{
		playback?.Dispose();
		playback = null;
		if ((Object)(object)image != (Object)null)
		{
			image.sprite = null;
		}
		if ((Object)(object)sprite != (Object)null)
		{
			Delete((Object)(object)sprite);
		}
		if ((Object)(object)texture != (Object)null)
		{
			Delete((Object)(object)texture);
		}
		sprite = null;
		texture = null;
		flipped = null;
	}

	private void OnDisable()
	{
		Release();
	}

	private void OnDestroy()
	{
		Release();
	}
}

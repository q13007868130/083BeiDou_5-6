using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public sealed class AudioManager : MonoBehaviour
{
	private class Entry
	{
		public string path;

		public int ms;
	}

	private const string PrefBgm = "audio.bgm";

	private const string PrefSfx = "audio.sfx";

	private const string PrefBgmMute = "audio.bgm.mute";

	private const string PrefSfxMute = "audio.sfx.mute";

	private const int SfxPool = 10;

	private const float BgmFade = 0.7f;

	private const float BgmLoadTimeout = 15f;

	private static Dictionary<string, Entry> index;

	private static bool indexFailed;

	private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

	private readonly HashSet<string> loading = new HashSet<string>();

	private readonly HashSet<string> failedLoads = new HashSet<string>();

	private AudioSource bgmA;

	private AudioSource bgmB;

	private AudioSource[] pool;

	private int poolCursor;

	private string bgmKey;

	private Coroutine fadeCo;

	private Coroutine bgmCo;

	public static AudioManager I { get; private set; }

	public static float BgmVolume
	{
		get
		{
			return PlayerPrefs.GetFloat("audio.bgm", 0.6f);
		}
		set
		{
			PlayerPrefs.SetFloat("audio.bgm", Mathf.Clamp01(value));
			if ((Object)(object)I != (Object)null)
			{
				I.ApplyBgmVolume();
			}
		}
	}

	public static float SfxVolume
	{
		get
		{
			return PlayerPrefs.GetFloat("audio.sfx", 0.8f);
		}
		set
		{
			PlayerPrefs.SetFloat("audio.sfx", Mathf.Clamp01(value));
		}
	}

	public static bool BgmMuted
	{
		get
		{
			return PlayerPrefs.GetInt("audio.bgm.mute", 0) != 0;
		}
		set
		{
			PlayerPrefs.SetInt("audio.bgm.mute", value ? 1 : 0);
			PlayerPrefs.Save();
			if ((Object)(object)I != (Object)null)
			{
				I.ApplyBgmVolume();
			}
		}
	}

	public static bool SfxMuted
	{
		get
		{
			return PlayerPrefs.GetInt("audio.sfx.mute", 0) != 0;
		}
		set
		{
			PlayerPrefs.SetInt("audio.sfx.mute", value ? 1 : 0);
			PlayerPrefs.Save();
		}
	}

	private static Dictionary<string, Entry> Index
	{
		get
		{
			if (index == null && !indexFailed)
			{
				TextAsset val = Resources.Load<TextAsset>("SoundIndex");
				if ((Object)(object)val == (Object)null)
				{
					indexFailed = true;
					return index = new Dictionary<string, Entry>();
				}
				index = new Dictionary<string, Entry>();
				foreach (KeyValuePair<string, object> item in (Dictionary<string, object>)MiniJson.Parse(val.text))
				{
					Dictionary<string, object> dictionary = (Dictionary<string, object>)item.Value;
					index[item.Key] = new Entry
					{
						path = (string)dictionary["path"],
						ms = (int)(double)dictionary["ms"]
					};
				}
			}
			return index;
		}
	}

	public static string CurrentBgm
	{
		get
		{
			if (!((Object)(object)I != (Object)null))
			{
				return null;
			}
			return I.bgmKey;
		}
	}

	[RuntimeInitializeOnLoadMethod(/*Could not decode attribute arguments.*/)]
	private static void Bootstrap()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected Obj, but got Unknown
		if (!((Object)(object)I != (Object)null))
		{
			GameObject val = new GameObject("[AudioManager]");
			I = val.AddComponent<AudioManager>();
			Object.DontDestroyOnLoad((Object)val);
		}
	}

	private void Awake()
	{
		if ((Object)(object)I != (Object)null && (Object)(object)I != (Object)(object)this)
		{
			Object.Destroy((Object)(object)((Component)this).gameObject);
			return;
		}
		I = this;
		bgmA = ((Component)this).gameObject.AddComponent<AudioSource>();
		bgmA.loop = true;
		bgmA.playOnAwake = false;
		bgmA.priority = 0;
		bgmB = ((Component)this).gameObject.AddComponent<AudioSource>();
		bgmB.loop = true;
		bgmB.playOnAwake = false;
		bgmB.priority = 0;
		pool = new AudioSource[10];
		for (int i = 0; i < 10; i++)
		{
			pool[i] = ((Component)this).gameObject.AddComponent<AudioSource>();
			pool[i].playOnAwake = false;
		}
		ApplyBgmVolume();
	}

	private void ApplyBgmVolume()
	{
		float volume = (BgmMuted ? 0f : BgmVolume);
		if ((Object)(object)bgmA != (Object)null)
		{
			bgmA.volume = volume;
		}
		if ((Object)(object)bgmB != (Object)null)
		{
			bgmB.volume = volume;
		}
	}

	public static bool Has(string key)
	{
		return Index.ContainsKey(key);
	}

	public static AudioClip EffectClip(string key)
	{
		if (!((Object)(object)I != (Object)null) || !Index.TryGetValue(key, out var value))
		{
			return null;
		}
		return I.LoadOrFetch(key, value);
	}

	public static bool EffectFailed(string key)
	{
		if ((Object)(object)I != (Object)null)
		{
			return I.failedLoads.Contains(key);
		}
		return false;
	}

	public static void PlayBgm(string key)
	{
		if ((Object)(object)I == (Object)null)
		{
			Debug.LogWarning((object)("[Audio] PlayBgm 早于 AudioManager 初始化：" + key));
		}
		else
		{
			if (string.IsNullOrEmpty(key))
			{
				return;
			}
			if (!Index.TryGetValue(key, out var value))
			{
				Debug.LogWarning((object)("[Audio] BGM 未在 SoundIndex 登记：" + key));
			}
			else if (!(I.bgmKey == key) || (I.bgmCo == null && (!I.bgmA.isPlaying || !((Object)(object)I.bgmA.clip == (Object)(object)I.Cached(key))) && (!I.bgmB.isPlaying || !((Object)(object)I.bgmB.clip == (Object)(object)I.Cached(key)))))
			{
				I.bgmKey = key;
				if (I.bgmCo != null)
				{
					((MonoBehaviour)I).StopCoroutine(I.bgmCo);
					I.bgmCo = null;
				}
				I.bgmCo = ((MonoBehaviour)I).StartCoroutine(I.CrossFadeBgm(key, value));
			}
		}
	}

	public static void StopBgm(float fade = 0.7f)
	{
		if (!((Object)(object)I == (Object)null))
		{
			I.bgmKey = null;
			if (I.bgmCo != null)
			{
				((MonoBehaviour)I).StopCoroutine(I.bgmCo);
				I.bgmCo = null;
			}
			I.StopFade(fade);
		}
	}

	private IEnumerator CrossFadeBgm(string key, Entry e)
	{
		yield return null;
		AudioClip val = Cached(key);
		if ((Object)(object)val == (Object)null)
		{
			failedLoads.Remove(key);
			if (!loading.Contains(key))
			{
				((MonoBehaviour)this).StartCoroutine(Load(key, e));
			}
			float waited = 0f;
			while (loading.Contains(key) && waited < 15f)
			{
				if (bgmKey != key)
				{
					bgmCo = null;
					yield break;
				}
				waited += Time.unscaledDeltaTime;
				yield return null;
			}
			if (bgmKey != key)
			{
				bgmCo = null;
				yield break;
			}
			val = Cached(key);
			if ((Object)(object)val == (Object)null)
			{
				bgmKey = null;
				bgmCo = null;
				Debug.LogWarning((object)("[Audio] BGM 无法播放 " + key + "（" + (failedLoads.Contains(key) ? "加载失败" : "加载超时") + "）"));
				yield break;
			}
		}
		if (fadeCo != null)
		{
			((MonoBehaviour)this).StopCoroutine(fadeCo);
		}
		AudioSource val2;
		if (bgmA.isPlaying)
		{
			val2 = bgmA;
		}
		else
		{
			val2 = (bgmB.isPlaying ? bgmB : null);
		}
		AudioSource val3 = (((Object)(object)val2 == (Object)(object)bgmA) ? bgmB : bgmA);
		val3.clip = val;
		val3.volume = 0f;
		val3.Play();
		TrimBgmCache(val);
		Debug.Log((object)("[Audio] BGM " + key + "（音量 " + BgmVolume.ToString("0.00") + "）"));
		bgmCo = null;
		fadeCo = ((MonoBehaviour)this).StartCoroutine(Fade(val2, val3, 0.7f));
	}

	private void StopFade(float fade)
	{
		if (fadeCo != null)
		{
			((MonoBehaviour)this).StopCoroutine(fadeCo);
			fadeCo = null;
		}
		if (bgmA.isPlaying || bgmB.isPlaying)
		{
			fadeCo = ((MonoBehaviour)this).StartCoroutine(Fade(bgmA.isPlaying ? bgmA : bgmB, null, fade));
		}
	}

	private IEnumerator Fade(AudioSource from, AudioSource to, float seconds)
	{
		float t = 0f;
		float v0 = (((Object)(object)from != (Object)null) ? from.volume : 0f);
		AudioSource other;
		if ((Object)(object)to == (Object)null)
		{
			other = (((Object)(object)from == (Object)(object)bgmA) ? bgmB : bgmA);
		}
		else
		{
			other = null;
		}
		float otherVolume = (((Object)(object)other != (Object)null) ? other.volume : 0f);
		while (t < seconds)
		{
			t += Time.unscaledDeltaTime;
			float num = Mathf.Clamp01(t / seconds);
			if ((Object)(object)from != (Object)null)
			{
				from.volume = (BgmMuted ? 0f : Mathf.Lerp(v0, 0f, num));
			}
			if ((Object)(object)other != (Object)null)
			{
				other.volume = (BgmMuted ? 0f : Mathf.Lerp(otherVolume, 0f, num));
			}
			if ((Object)(object)to != (Object)null)
			{
				to.volume = (BgmMuted ? 0f : Mathf.Lerp(0f, BgmVolume, num));
			}
			yield return null;
		}
		if ((Object)(object)from != (Object)null)
		{
			from.Stop();
			from.clip = null;
		}
		if ((Object)(object)other != (Object)null)
		{
			other.Stop();
			other.clip = null;
		}
		if ((Object)(object)to != (Object)null)
		{
			to.volume = (BgmMuted ? 0f : BgmVolume);
		}
		TrimBgmCache(((Object)(object)to != (Object)null) ? to.clip : null);
		fadeCo = null;
	}

	private void TrimBgmCache(AudioClip keep)
	{
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, AudioClip> clip in clips)
		{
			if ((Object)(object)clip.Value != (Object)(object)keep && (Object)(object)clip.Value != (Object)(object)bgmA.clip && (Object)(object)clip.Value != (Object)(object)bgmB.clip && clip.Key.StartsWith("Bgm", StringComparison.Ordinal))
			{
				list.Add(clip.Key);
			}
		}
		foreach (string item in list)
		{
			Object.Destroy((Object)(object)clips[item]);
			clips.Remove(item);
		}
	}

	private void UnloadIfBgm(AudioClip clip)
	{
		foreach (KeyValuePair<string, AudioClip> clip2 in clips)
		{
			if ((Object)(object)clip2.Value == (Object)(object)clip && clip2.Key.StartsWith("Bgm", StringComparison.Ordinal))
			{
				Object.Destroy((Object)(object)clip);
				clips.Remove(clip2.Key);
				break;
			}
		}
	}

	public static void Play(string key, float vol = 1f, bool pitchVary = true)
	{
		if (!((Object)(object)I == (Object)null) && !string.IsNullOrEmpty(key) && Index.TryGetValue(key, out var value) && !SfxMuted && !(SfxVolume <= 0.001f))
		{
			AudioClip val = I.LoadOrFetch(key, value);
			if (!((Object)(object)val == (Object)null))
			{
				I.Spawn(val, vol, pitchVary);
			}
		}
	}

	public static void PlayAt(string key, Vector3 worldPos, float vol = 1f, float range = 6f)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)I == (Object)null)
		{
			return;
		}
		Camera main = Camera.main;
		if ((Object)(object)main == (Object)null)
		{
			Play(key, vol);
			return;
		}
		float num = Mathf.Abs(((Component)main).transform.position.x - worldPos.x);
		float num2 = Mathf.Clamp01(1f - num / range);
		if (!(num2 <= 0.02f))
		{
			Play(key, vol * num2);
		}
	}

	private void Spawn(AudioClip clip, float vol, bool pitchVary)
	{
		AudioSource val = pool[poolCursor];
		for (int i = 0; i < 10; i++)
		{
			AudioSource val2 = pool[(poolCursor + i) % 10];
			if (!val2.isPlaying)
			{
				val = val2;
				break;
			}
		}
		val.pitch = (pitchVary ? Random.Range(0.94f, 1.06f) : 1f);
		val.volume = Mathf.Clamp01(vol) * SfxVolume;
		val.clip = clip;
		val.Play();
		poolCursor = (poolCursor + 1) % 10;
	}

	private AudioClip LoadOrFetch(string key, Entry e)
	{
		if (clips.TryGetValue(key, out var value) && (Object)(object)value != (Object)null)
		{
			return value;
		}
		if (loading.Contains(key))
		{
			return null;
		}
		((MonoBehaviour)this).StartCoroutine(Load(key, e));
		return null;
	}

	private AudioClip Cached(string key)
	{
		if (!clips.TryGetValue(key, out var value) || !((Object)(object)value != (Object)null))
		{
			return null;
		}
		return value;
	}

	private IEnumerator Load(string key, Entry e)
	{
		loading.Add(key);
		string url = new Uri(Path.GetFullPath(Path.Combine(WzAssets.DataRoot, e.path))).AbsoluteUri;
		UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(url, (AudioType)13);
		try
		{
			req.timeout = 15;
			yield return req.SendWebRequest();
			if ((int)req.result != 1)
			{
				failedLoads.Add(key);
				Debug.LogWarning((object)("[Audio] 加载失败 " + key + ": " + req.error + " (" + url + ")"));
			}
			else
			{
				clips[key] = DownloadHandlerAudioClip.GetContent(req);
				if ((Object)(object)clips[key] == (Object)null)
				{
					failedLoads.Add(key);
				}
				else
				{
					failedLoads.Remove(key);
				}
			}
		}
		finally
		{
			((IDisposable)req)?.Dispose();
		}
		loading.Remove(key);
	}

	public static void Preload(params string[] keys)
	{
		if ((Object)(object)I == (Object)null)
		{
			return;
		}
		foreach (string key in keys)
		{
			if (Index.TryGetValue(key, out var value))
			{
				I.LoadOrFetch(key, value);
			}
		}
	}
}

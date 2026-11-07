using System.Collections.Generic;
using UnityEngine;

public sealed class NativeWeatherSound : MonoBehaviour
{
	private sealed class Voice
	{
		public AudioSource Source;

		public double Release;
	}

	private readonly List<Voice> fading = new List<Voice>();

	private readonly HashSet<int> failed = new HashSet<int>();

	private AudioSource current;

	private int playing;

	public void Tick(int wanted, float rain)
	{
		double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
		for (int num = fading.Count - 1; num >= 0; num--)
		{
			Voice voice = fading[num];
			if (realtimeSinceStartupAsDouble >= voice.Release)
			{
				voice.Source.Stop();
				Object.Destroy((Object)(object)voice.Source);
				fading.RemoveAt(num);
			}
			else
			{
				voice.Source.volume = 0.7f * AudioManager.SfxVolume * (float)((voice.Release - realtimeSinceStartupAsDouble) / 1.5);
			}
		}
		if (wanted == 1 || playing == 1)
		{
			float num2 = ((playing == 1) ? 0.1f : 0.35f);
			if (playing == 1 && rain >= num2)
			{
				wanted = 1;
			}
			else if (wanted == 1 && rain < num2)
			{
				wanted = 0;
			}
		}
		if ((Object)(object)current != (Object)null)
		{
			current.volume = 0.7f * AudioManager.SfxVolume;
		}
		if (wanted == playing)
		{
			return;
		}
		if ((Object)(object)current != (Object)null)
		{
			fading.Add(new Voice
			{
				Source = current,
				Release = realtimeSinceStartupAsDouble + 1.5
			});
			current = null;
		}
		playing = 0;
		if (wanted == 0 || failed.Contains(wanted))
		{
			return;
		}
		string key = ((wanted == 1) ? "Weather/rain" : "Weather/wind");
		if (!AudioManager.Has(key) || AudioManager.EffectFailed(key))
		{
			failed.Add(wanted);
			return;
		}
		AudioClip val = AudioManager.EffectClip(key);
		if (!((Object)(object)val == (Object)null))
		{
			current = ((Component)this).gameObject.AddComponent<AudioSource>();
			current.playOnAwake = false;
			current.clip = val;
			current.loop = true;
			current.pitch = 1f;
			current.volume = 0.7f * AudioManager.SfxVolume;
			current.Play();
			playing = wanted;
		}
	}

	private void OnDisable()
	{
		if ((Object)(object)current != (Object)null)
		{
			current.Stop();
			Object.Destroy((Object)(object)current);
			current = null;
		}
		foreach (Voice item in fading)
		{
			if ((Object)(object)item.Source != (Object)null)
			{
				item.Source.Stop();
				Object.Destroy((Object)(object)item.Source);
			}
		}
		fading.Clear();
		playing = 0;
	}
}

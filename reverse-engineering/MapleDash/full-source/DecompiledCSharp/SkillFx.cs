using System.Collections.Generic;
using UnityEngine;

public class SkillFx : MonoBehaviour
{
	private NativeVfxClip clip;

	private SpriteRenderer sr;

	private float speed = 1f;

	private float frameElapsed;

	private int index;

	private int baseOrder;

	private bool loop;

	private bool hasFollow;

	private EnemyUnit enemy;

	private BossUnit boss;

	public Transform follow;

	public float followOffsetX;

	private Vector3 followOffset;

	private float fadeAlpha = 1f;

	private float fadeDuration;

	private float fadeElapsed;

	private bool fading;

	public string SkillKey { get; private set; }

	public string Part => clip.path;

	public Sprite CurrentSprite => sr.sprite;

	public static SkillFx Play(string key, Vector3 anchor, int facing, float speed = 1f, int order = 360)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		return PlayPart(key, "effect", anchor, facing, speed, order);
	}

	public static SkillFx PlayPart(string key, string part, Vector3 anchor, int facing, float speed = 1f, int order = 360, bool loop = false)
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected Obj, but got Unknown
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		part = NativeSkillVfx.Alias(part);
		NativeVfxClip nativeVfxClip = NativeSkillVfx.Get(key, part);
		if (nativeVfxClip == null)
		{
			List<NativeVfxClip> list = NativeSkillVfx.Parts(key, part);
			if (list.Count == 0)
			{
				return null;
			}
			nativeVfxClip = list[0];
		}
		GameObject val = new GameObject("SkillFx_" + key + "_" + nativeVfxClip.path.Replace('/', '_'));
		val.transform.position = anchor;
		SkillFx skillFx = val.AddComponent<SkillFx>();
		skillFx.clip = nativeVfxClip;
		skillFx.SkillKey = key;
		skillFx.speed = Mathf.Max(0.1f, speed);
		skillFx.loop = loop;
		skillFx.baseOrder = order;
		skillFx.sr = val.AddComponent<SpriteRenderer>();
		skillFx.sr.flipX = facing > 0;
		skillFx.ShowFrame();
		if ((Object)(object)skillFx.sr.sprite == (Object)null)
		{
			Object.Destroy((Object)(object)val);
			return null;
		}
		return skillFx;
	}

	public void Follow(Transform target)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		follow = target;
		hasFollow = true;
		enemy = ((Component)target).GetComponent<EnemyUnit>();
		boss = ((Component)target).GetComponent<BossUnit>();
		followOffset = ((Component)this).transform.position - target.position;
	}

	public void Stop()
	{
		((Component)this).gameObject.SetActive(false);
		Object.Destroy((Object)(object)((Component)this).gameObject);
	}

	public void FadeOut(float duration)
	{
		if (!fading && !(duration <= 0f))
		{
			fading = true;
			fadeDuration = Mathf.Max(0.01f, duration);
			fadeElapsed = 0f;
		}
	}

	private void ShowFrame()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		NativeVfxFrame nativeVfxFrame = clip.frames[index];
		sr.sprite = Art.FrameAt(nativeVfxFrame.file, nativeVfxFrame.origin);
		((Renderer)sr).sortingOrder = baseOrder + nativeVfxFrame.z;
		sr.color = new Color(1f, 1f, 1f, nativeVfxFrame.alpha0 * fadeAlpha);
	}

	private void Update()
	{
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		if (clip == null)
		{
			return;
		}
		if (hasFollow && ((Object)(object)follow == (Object)null || !((Component)follow).gameObject.activeInHierarchy || ((Object)(object)enemy != (Object)null && enemy.dead) || ((Object)(object)boss != (Object)null && boss.dead)))
		{
			if (loop)
			{
				Stop();
				return;
			}
			follow = null;
			hasFollow = false;
		}
		if ((Object)(object)follow != (Object)null)
		{
			((Component)this).transform.position = follow.position + followOffset + Vector3.right * followOffsetX;
		}
		float num = Time.deltaTime * speed;
		frameElapsed += num;
		while (frameElapsed >= clip.frames[index].delay)
		{
			frameElapsed -= clip.frames[index].delay;
			index++;
			if (index >= clip.frames.Length)
			{
				if (!loop)
				{
					Stop();
					return;
				}
				index = 0;
			}
			ShowFrame();
		}
		if (fading)
		{
			fadeElapsed += Time.deltaTime;
			fadeAlpha = Mathf.Clamp01(1f - fadeElapsed / fadeDuration);
			if (fadeAlpha <= 0f)
			{
				Stop();
				return;
			}
		}
		NativeVfxFrame nativeVfxFrame = clip.frames[index];
		sr.color = new Color(1f, 1f, 1f, Mathf.Lerp(nativeVfxFrame.alpha0, nativeVfxFrame.alpha1, frameElapsed / nativeVfxFrame.delay) * fadeAlpha);
	}
}

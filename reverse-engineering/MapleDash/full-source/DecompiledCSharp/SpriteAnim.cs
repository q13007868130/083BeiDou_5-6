using Server.Bot;
using UnityEngine;

public class SpriteAnim : MonoBehaviour
{
	public string kind = "chars";

	public string key = "knight";

	public float speed = 1f;

	private SpriteRenderer sr;

	private Art.Clip cur;

	private string curName = "";

	private string baseAnim = "stand";

	private bool oneShot;

	private float t;

	private int idx;

	private string expression;

	private float expressionStarted;

	private float lastExpression = -10f;

	private float expressionDuration = 5f;

	public static readonly string[] EmoteNames = new string[23]
	{
		"blink", "hit", "smile", "troubled", "cry", "angry", "bewildered", "stunned", "vomit", "oops",
		"cheers", "chu", "wink", "pain", "glitter", "blaze", "shine", "love", "despair", "hum",
		"bowing", "hot", "dam"
	};

	public const int MaxEmote = 22;

	public const float EmoteCooldown = 2f;

	public const float EmoteDuration = 5f;

	public bool throttleWhenBusy;

	public string Current => curName;

	public bool IsOneShot => oneShot;

	public SpriteRenderer Renderer => sr;

	public float Duration
	{
		get
		{
			if (cur == null)
			{
				return 0f;
			}
			return cur.Duration;
		}
	}

	public int FrameIndex => idx;

	public string FrameUri
	{
		get
		{
			if (cur == null || idx >= cur.frames.Length)
			{
				return null;
			}
			return cur.frames[idx];
		}
	}

	public float FrameRemaining
	{
		get
		{
			if (cur == null)
			{
				return 0f;
			}
			return Mathf.Max(0f, cur.delays[idx] - t);
		}
	}

	public bool Express(int emote, bool checkCooldown = true)
	{
		if (kind != "chars" || emote < 1 || emote > 22)
		{
			return false;
		}
		if (checkCooldown && Time.unscaledTime - lastExpression < 2f)
		{
			return false;
		}
		expression = EmoteNames[emote];
		expressionDuration = 5f;
		expressionStarted = (lastExpression = Time.unscaledTime);
		Apply(idx);
		return true;
	}

	public void ShowHitExpression()
	{
		if (!(kind != "chars"))
		{
			expression = "hit";
			expressionStarted = Time.unscaledTime;
			expressionDuration = 1.5f;
			Apply(idx);
		}
	}

	public void ClearExpression()
	{
		if (expression != null)
		{
			expression = null;
			Apply(idx);
		}
	}

	public void Init(string kind, string key, string idle, SpriteRenderer sr)
	{
		this.kind = kind;
		this.key = key;
		this.sr = sr;
		baseAnim = idle;
		Play(idle, once: false, force: true);
	}

	public void SetBase(string anim)
	{
		if (!(baseAnim == anim))
		{
			baseAnim = anim;
			if (!oneShot)
			{
				Play(anim, once: false);
			}
		}
	}

	public void PlayOnce(string anim, float speedMul = 1f, bool force = true)
	{
		if (Play(anim, once: true, force))
		{
			speed = speedMul;
		}
	}

	public void PlayNativeAttack(string anim, int attackSpeed)
	{
		if (Play(anim, once: true, force: true))
		{
			float[] array = new float[cur.delays.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = WzCharacter.AttackFrameDelay(cur.delays[i], attackSpeed);
			}
			cur = new Art.Clip
			{
				frames = cur.frames,
				delays = array
			};
			speed = 1f;
		}
	}

	public bool Play(string anim, bool once, bool force = false)
	{
		Art.Clip clip = Art.GetClip(kind, key, Art.Resolve(kind, key, anim, baseAnim));
		if (clip == null || clip.frames.Length == 0)
		{
			return false;
		}
		if (!force && curName == anim && oneShot == once)
		{
			return true;
		}
		cur = clip;
		curName = anim;
		oneShot = once;
		t = 0f;
		idx = 0;
		if (!once)
		{
			speed = 1f;
		}
		Apply(idx);
		return true;
	}

	public void SetFacing(int dir)
	{
		if (!((Object)(object)sr == (Object)null))
		{
			sr.flipX = dir > 0;
		}
	}

	private void Apply(int i)
	{
		if (cur != null && !((Object)(object)sr == (Object)null) && i >= 0 && i < cur.frames.Length)
		{
			Sprite val = ((expression != null && kind == "chars") ? WzCharacter.ExpressionFrame(cur.frames[i], expression, Time.unscaledTime - expressionStarted) : Art.Frame(cur.frames[i]));
			if ((Object)(object)val != (Object)null)
			{
				sr.sprite = val;
			}
		}
	}

	private void LateUpdate()
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		if (kind != "chars" || (Object)(object)sr == (Object)null || (Object)(object)sr.sprite == (Object)null)
		{
			return;
		}
		Camera main = Camera.main;
		if (!((Object)(object)main == (Object)null) && main.orthographic)
		{
			float num = (float)main.pixelHeight / (2f * main.orthographicSize);
			Vector3 lossyScale = ((Component)this).transform.lossyScale;
			float num2 = num * Mathf.Abs(lossyScale.x) / sr.sprite.pixelsPerUnit;
			float num3 = num * Mathf.Abs(lossyScale.y) / sr.sprite.pixelsPerUnit;
			FilterMode val = (FilterMode)((!(Mathf.Abs(num2 - 1f) < 0.0001f) || !(Mathf.Abs(num3 - 1f) < 0.0001f)) ? 1 : 0);
			if (((Texture)sr.sprite.texture).filterMode != val)
			{
				((Texture)sr.sprite.texture).filterMode = val;
			}
		}
	}

	private void Update()
	{
		if (cur == null || (Object)(object)sr == (Object)null)
		{
			return;
		}
		if (expression != null)
		{
			bool flag = Time.unscaledTime - expressionStarted >= expressionDuration;
			if (flag)
			{
				expression = null;
			}
			if (flag || !throttleWhenBusy || BotWorkPump.Has(2f))
			{
				Apply(idx);
			}
		}
		float num = Time.deltaTime * Mathf.Max(0f, speed);
		if (num <= 0f)
		{
			return;
		}
		t += num;
		while (t >= cur.delays[idx])
		{
			t -= cur.delays[idx];
			idx++;
			if (idx >= cur.frames.Length)
			{
				if (oneShot)
				{
					oneShot = false;
					speed = 1f;
					Play(baseAnim, once: false);
					break;
				}
				idx = 0;
			}
			if (!throttleWhenBusy || BotWorkPump.Has(0.4f))
			{
				Apply(idx);
			}
		}
	}
}

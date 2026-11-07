using UnityEngine;

public class FxFlash : MonoBehaviour
{
	private SpriteRenderer sr;

	private Color baseColor;

	private Color flashColor;

	private float dur;

	private float t = -1f;

	private void Awake()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		sr = ((Component)this).GetComponent<SpriteRenderer>();
		if ((Object)(object)sr != (Object)null)
		{
			baseColor = sr.color;
		}
	}

	public void Play(Color c, float d)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)sr == (Object)null))
		{
			if (t < 0f)
			{
				baseColor = sr.color;
			}
			flashColor = c;
			dur = Mathf.Max(0.01f, d);
			t = 0f;
		}
	}

	private void Update()
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		if (!(t < 0f))
		{
			t += Time.deltaTime;
			if (t >= dur)
			{
				sr.color = baseColor;
				t = -1f;
			}
			else
			{
				sr.color = Color.Lerp(flashColor, baseColor, t / dur);
			}
		}
	}
}

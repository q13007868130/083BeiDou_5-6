using UnityEngine;

public sealed class AfterimageGhost : MonoBehaviour
{
	public float life = 0.2f;

	public float startAlpha = 0.5f;

	private float t;

	private SpriteRenderer sr;

	private void Awake()
	{
		sr = ((Component)this).GetComponent<SpriteRenderer>();
	}

	private void Update()
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		t += Time.deltaTime;
		float num = ((life <= 0f) ? 1f : Mathf.Clamp01(t / life));
		if ((Object)(object)sr != (Object)null)
		{
			Color color = sr.color;
			color.a = startAlpha * (1f - num);
			sr.color = color;
		}
		if (num >= 1f)
		{
			Object.Destroy((Object)(object)((Component)this).gameObject);
		}
	}

	private void OnDisable()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)sr != (Object)null)
		{
			Color color = sr.color;
			color.a = 0f;
			sr.color = color;
		}
	}
}

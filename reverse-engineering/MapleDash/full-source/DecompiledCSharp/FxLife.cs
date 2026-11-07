using UnityEngine;

public class FxLife : MonoBehaviour
{
	public float life = 0.2f;

	public Vector3 grow = Vector3.one;

	public Vector3 drift;

	private float t;

	private Vector3 startScale;

	private void Start()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		startScale = ((Component)this).transform.localScale;
	}

	private void Update()
	{
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		t += Time.deltaTime;
		float num = Mathf.Clamp01(t / life);
		((Component)this).transform.localScale = new Vector3(startScale.x * Mathf.Lerp(1f, grow.x, num), startScale.y * Mathf.Lerp(1f, grow.y, num), 1f);
		Transform transform = ((Component)this).transform;
		transform.position += drift * Time.deltaTime;
		SpriteRenderer component = ((Component)this).GetComponent<SpriteRenderer>();
		if ((Object)(object)component != (Object)null)
		{
			Color color = component.color;
			color.a = 1f - num;
			component.color = color;
		}
		if (t >= life)
		{
			Object.Destroy((Object)(object)((Component)this).gameObject);
		}
	}

	public FxLife()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
	}
}

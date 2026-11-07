using UnityEngine;

public sealed class NativeRingParticle : MonoBehaviour
{
	private Transform follow;

	private Vector3 last;

	private Vector3 velocity;

	public void Init(Transform target, Vector3 speed)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		follow = target;
		velocity = speed;
		last = (((Object)(object)target != (Object)null) ? target.position : ((Component)this).transform.position);
	}

	private void Update()
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)follow != (Object)null)
		{
			Transform transform = ((Component)this).transform;
			transform.position += follow.position - last;
			last = follow.position;
		}
		Transform transform2 = ((Component)this).transform;
		transform2.position += velocity * Time.deltaTime;
	}
}

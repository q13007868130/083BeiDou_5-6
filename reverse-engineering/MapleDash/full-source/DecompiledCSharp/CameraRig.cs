using UnityEngine;

public class CameraRig : MonoBehaviour
{
	public Transform target;

	public float smooth = 7f;

	public float baseY = -0.14999998f;

	public float baseSize = 9.6f;

	public float freeMinX = -11f;

	public float freeMaxX = 11f;

	private Camera cam;

	private float x;

	private float y;

	private bool useStage;

	private float stageMinX;

	private float stageMaxX;

	private float stageMinY;

	private float stageMaxY;

	private void Awake()
	{
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		cam = ((Component)this).GetComponent<Camera>();
		if ((Object)(object)cam == (Object)null)
		{
			cam = ((Component)this).gameObject.AddComponent<Camera>();
		}
		cam.orthographic = true;
		cam.orthographicSize = baseSize;
		cam.backgroundColor = new Color(0.07f, 0.08f, 0.11f);
		cam.clearFlags = (CameraClearFlags)2;
		cam.farClipPlane = 100f;
		UpdateViewport();
		y = baseY;
	}

	private void UpdateViewport()
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		cam.rect = new Rect(0f, 0f, 1f, 1f);
		cam.aspect = (((Object)(object)cam.targetTexture != (Object)null) ? ((float)((Texture)cam.targetTexture).width / (float)((Texture)cam.targetTexture).height) : ((float)Screen.width / (float)Mathf.Max(1, Screen.height)));
		cam.orthographicSize = baseSize;
	}

	public void SetStage(Art.StageInfo stage)
	{
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		useStage = true;
		stageMinX = (stage.hasView ? stage.WorldX(stage.view.x) : stage.LeftX);
		stageMaxX = (stage.hasView ? stage.WorldX(stage.view.z) : stage.RightX);
		stageMinY = (stage.hasView ? stage.WorldY(stage.view.w) : stage.BottomY);
		stageMaxY = (stage.hasView ? stage.WorldY(stage.view.y) : stage.TopY);
		SnapTo(((Object)(object)target != (Object)null) ? target.position.x : x);
	}

	public void SetBounds(float minX, float maxX, float minY, float maxY)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		useStage = true;
		stageMinX = Mathf.Min(minX, maxX);
		stageMaxX = Mathf.Max(minX, maxX);
		stageMinY = Mathf.Min(minY, maxY);
		stageMaxY = Mathf.Max(minY, maxY);
		SnapTo(((Object)(object)target != (Object)null) ? target.position.x : x);
	}

	public void ClearStage()
	{
		useStage = false;
	}

	private void LateUpdate()
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		UpdateViewport();
		if ((Object)(object)target != (Object)null)
		{
			float num = target.position.x;
			x = Mathf.Lerp(x, num, 1f - Mathf.Exp((0f - smooth) * Time.unscaledDeltaTime));
			x = ClampX(x);
			float num2 = target.position.y + 1.25f;
			y = Mathf.Lerp(y, num2, 1f - Mathf.Exp((0f - smooth) * Time.unscaledDeltaTime));
			y = ClampY(y);
		}
		else
		{
			x = ClampX(x);
			y = ClampY(baseY);
		}
		Vector2 cameraOffset = HitFeel.CameraOffset;
		float num3 = Mathf.Round((x + cameraOffset.x) * 40f) / 40f;
		float num4 = Mathf.Round((y + cameraOffset.y) * 40f) / 40f;
		((Component)this).transform.position = new Vector3(num3, num4, -10f);
	}

	private float ClampX(float v)
	{
		if (!useStage)
		{
			return Mathf.Clamp(v, freeMinX, freeMaxX);
		}
		float num = Mathf.Floor(cam.orthographicSize * Mathf.Max(0.1f, cam.aspect) * 40f) / 40f;
		float num2 = stageMinX + num;
		float num3 = stageMaxX - num;
		if (num2 >= num3)
		{
			return (stageMinX + stageMaxX) * 0.5f;
		}
		return Mathf.Clamp(v, num2, num3);
	}

	public void SnapTo(float targetX)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		UpdateViewport();
		x = ClampX(targetX);
		y = ClampY(((Object)(object)target != (Object)null) ? (target.position.y + 1.25f) : baseY);
		((Component)this).transform.position = new Vector3(x, y, -10f);
	}

	private float ClampY(float value)
	{
		if (!useStage)
		{
			return value;
		}
		float num = Mathf.Floor(cam.orthographicSize * 40f) / 40f;
		float num2 = stageMinY + num;
		float num3 = stageMaxY - num;
		if (!(num2 >= num3))
		{
			return Mathf.Clamp(value, num2, num3);
		}
		return (stageMinY + stageMaxY) * 0.5f;
	}
}

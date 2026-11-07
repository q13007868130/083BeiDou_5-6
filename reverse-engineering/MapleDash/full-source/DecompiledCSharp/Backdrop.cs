using UnityEngine;

[DefaultExecutionOrder(100)]
public class Backdrop : MonoBehaviour
{
	private Transform artRoot;

	private SpriteRenderer sky;

	private string curTheme = "";

	public bool HasStage { get; private set; }

	public float WalkMin { get; private set; } = -13f;

	public float WalkMax { get; private set; } = 13f;

	public void Build()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected Obj, but got Unknown
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("Sky");
		val.transform.SetParent(((Component)this).transform, false);
		sky = val.AddComponent<SpriteRenderer>();
		sky.sprite = SpriteFactory.Center();
		sky.color = new Color(0.07f, 0.08f, 0.11f);
		((Renderer)sky).sortingOrder = -950;
		((Component)sky).transform.position = new Vector3(0f, 1.5f, 0f);
		((Component)sky).transform.localScale = new Vector3(140f, 52f, 1f);
	}

	public void Apply(string theme, StageTheme t)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (t != null && (Object)(object)sky != (Object)null && !Art.Ready)
		{
			sky.color = t.bgTop;
		}
		if (!(theme == curTheme))
		{
			curTheme = theme;
			Rebuild(theme, t);
		}
	}

	private void Rebuild(string theme, StageTheme t)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected Obj, but got Unknown
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)artRoot != (Object)null)
		{
			Object.Destroy((Object)(object)((Component)artRoot).gameObject);
		}
		GameObject val = new GameObject("Stage_" + theme);
		val.transform.SetParent(((Component)this).transform, false);
		artRoot = val.transform;
		Art.StageInfo stageInfo = Art.Stage(theme);
		if (stageInfo == null)
		{
			HasStage = false;
			WalkMin = -13f;
			WalkMax = 13f;
			Fallback(t);
			return;
		}
		HasStage = true;
		WalkMin = stageInfo.WalkMin;
		WalkMax = stageInfo.WalkMax;
		if ((Object)(object)sky != (Object)null)
		{
			sky.color = stageInfo.sky;
		}
		artRoot.position = new Vector3((0f - stageInfo.ground.x) / 40f, -1.4f + stageInfo.ground.y / 40f, 0f);
		((Component)artRoot).gameObject.AddComponent<NativeMapView>().Show(int.Parse(stageInfo.map));
		if ((Object)(object)sky != (Object)null)
		{
			((Component)sky).transform.position = new Vector3((stageInfo.LeftX + stageInfo.RightX) * 0.5f, (stageInfo.TopY + stageInfo.BottomY) * 0.5f, 0f);
		}
	}

	private void Fallback(StageTheme t)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		Color val = t?.groundDeep ?? new Color(0.17f, 0.19f, 0.23f);
		Color val2 = t?.mid ?? new Color(0.2f, 0.24f, 0.3f);
		for (int i = 0; i < 3; i++)
		{
			GameObject val3 = new GameObject("FB" + i);
			val3.transform.SetParent(artRoot, false);
			SpriteRenderer val4 = val3.AddComponent<SpriteRenderer>();
			val4.sprite = SpriteFactory.Bottom();
			val4.color = Color.Lerp(val, val2, (float)i / 2f);
			((Renderer)val4).sortingOrder = -390 + i * 40;
			val3.transform.position = new Vector3(0f, -1.4f, 0f);
			val3.transform.localScale = new Vector3(70f, 4f - (float)i * 0.8f, 1f);
		}
		GameObject val5 = new GameObject("FBGround");
		val5.transform.SetParent(artRoot, false);
		SpriteRenderer val6 = val5.AddComponent<SpriteRenderer>();
		val6.sprite = SpriteFactory.Bottom();
		val6.color = val;
		((Renderer)val6).sortingOrder = -60;
		val5.transform.position = new Vector3(0f, -2.9f, 0f);
		val5.transform.localScale = new Vector3(70f, 1.5f, 1f);
	}
}

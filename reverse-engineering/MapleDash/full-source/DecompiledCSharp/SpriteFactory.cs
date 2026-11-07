using UnityEngine;

public static class SpriteFactory
{
	private static Sprite _bottom;

	private static Sprite _center;

	private static Texture2D MakeTex(Color c)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Expected Obj, but got Unknown
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = new Texture2D(4, 4, (TextureFormat)4, false);
		Color[] array = new Color[16];
		for (int i = 0; i < 16; i++)
		{
			array[i] = c;
		}
		val.SetPixels(array);
		((Texture)val).filterMode = (FilterMode)0;
		((Texture)val).wrapMode = (TextureWrapMode)1;
		val.Apply();
		return val;
	}

	public static Sprite Bottom()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)_bottom == (Object)null)
		{
			_bottom = Sprite.Create(MakeTex(Color.white), new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0f), 4f);
		}
		return _bottom;
	}

	public static Sprite Center()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)_center == (Object)null)
		{
			_center = Sprite.Create(MakeTex(Color.white), new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
		}
		return _center;
	}

	public static SpriteRenderer Make(string name, Transform parent, Vector2 size, Color color, int order = 0, bool bottomPivot = true)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected Obj, but got Unknown
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name);
		if ((Object)(object)parent != (Object)null)
		{
			val.transform.SetParent(parent, false);
		}
		SpriteRenderer val2 = val.AddComponent<SpriteRenderer>();
		val2.sprite = (bottomPivot ? Bottom() : Center());
		val2.color = color;
		((Renderer)val2).sortingOrder = order;
		val.transform.localScale = new Vector3(size.x, size.y, 1f);
		return val2;
	}
}

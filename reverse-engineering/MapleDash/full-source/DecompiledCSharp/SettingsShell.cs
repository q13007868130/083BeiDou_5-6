using UnityEngine;

internal static class SettingsShell
{
	public static bool Begin(string background, ref Vector2 pos, int w, int h, float s, ref bool dragging, ref Vector2 grab, out Rect rect)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Invalid comparison between Unknown and I4
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		pos = NativeUi.ClampWindowPosition(pos, new Vector2((float)w, (float)h), s);
		rect = new Rect(pos.x * s, pos.y * s, (float)w * s, (float)h * s);
		if ((int)Event.current.type == 7 && Pointer.Pressed)
		{
			Rect val = new Rect(rect.x, rect.y, rect.width - 24f * s, 22f * s);
			if (val.Contains(Pointer.Pos))
			{
				dragging = true;
				grab = Pointer.Pos / s - pos;
			}
		}
		if (dragging && Pointer.Down)
		{
			pos = NativeUi.ClampWindowPosition(Pointer.Pos / s - grab, new Vector2((float)w, (float)h), s);
			rect.position = pos * s;
		}
		if (!Pointer.Down)
		{
			dragging = false;
		}
		NativeUi.RegisterWindow(background, rect, () => (!background.Contains("GameOpt/")) ? SysOptWindow.Visible : GameOptWindow.Visible);
		Texture2D val2 = Art.Tex(background);
		if ((Object)(object)val2 != (Object)null)
		{
			GUI.DrawTexture(rect, (Texture)(object)val2);
		}
		return !NativeUi.Button(new Rect(rect.x + (float)(w - 19) * s, rect.y + 5f * s, 12f * s, 12f * s), "UI/Basic.img/BtClose");
	}
}

using System;
using System.Collections.Generic;
using UnityEngine;

public static class NativeUi
{
	private static readonly Dictionary<string, (Rect rect, Func<bool> visible)> windows = new Dictionary<string, (Rect, Func<bool>)>();

	private static int scrollDrag;

	private static float scrollGrab;

	private static Texture2D dialogPanel;

	public static bool WindowAtPointer
	{
		get
		{
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_005b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
			//IL_0063: Unknown result type (might be due to invalid IL or missing references)
			if (NativeVMatrixWindow.AtPointer || NativeHexaMatrixWindow.AtPointer)
			{
				return true;
			}
			foreach (var value in windows.Values)
			{
				if (!value.visible())
				{
					continue;
				}
				var (val, _) = value;
				if (!val.Contains(Pointer.Pos))
				{
					if (!Pointer.Down && !Pointer.Released)
					{
						continue;
					}
					(val, _) = value;
					if (!val.Contains(Pointer.PressPosition))
					{
						continue;
					}
				}
				return true;
			}
			return false;
		}
	}

	public static int TabsStyle2(string labels, int count, int selected, float x, float y, int width, Vector2 pointer, int fixedWidth = 0)
	{
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Invalid comparison between Unknown and I4
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		int num = ((fixedWidth > 0) ? (fixedWidth - 8) : ((width - (count - 1) * 12 - 12) / count));
		int num2 = ((fixedWidth <= 0) ? (width - (num + 12) * count) : 0);
		Image("left" + ((selected == 0) ? 1 : 0), x);
		float num3 = x + 6f;
		for (int i = 0; i < count; i++)
		{
			int num4 = num + ((i < num2) ? 1 : 0);
			Texture2D val = Art.Tex("wz://UI/Basic.img/Tab3/fill" + ((selected == i) ? 1 : 0));
			if ((Object)(object)val != (Object)null)
			{
				GUI.DrawTexture(new Rect(num3, y, (float)num4, (float)((Texture)val).height), (Texture)(object)val);
			}
			float num5 = num3 + (float)num4;
			Image((i == count - 1) ? ("right" + ((selected == i) ? 1 : 0)) : ("middle" + ((selected == i) ? 1 : ((selected == i + 1) ? 2 : 0))), num5);
			Texture2D val2 = Art.Tex("wz://" + labels + "/" + ((selected == i) ? "disabled/" : "enabled/") + i);
			if ((Object)(object)val2 != (Object)null)
			{
				GUI.DrawTexture(new Rect(num3 + (float)(num4 / 2) - (float)(((Texture)val2).width / 2), y + 10f - (float)(((Texture)val2).height / 2) + 2f, (float)((Texture)val2).width, (float)((Texture)val2).height), (Texture)(object)val2);
			}
			if (GUI.enabled && (int)Event.current.type == 7 && Pointer.Released)
			{
				Rect val3 = new Rect(num3 - 6f, y, (float)(num4 + 12), 21f);
				if (val3.Contains(pointer))
				{
					Pointer.ConsumeClick();
					selected = i;
				}
			}
			num3 = num5 + 12f;
		}
		return selected;
		void Image(string name, float dx)
		{
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			Texture2D val4 = Art.Tex("wz://UI/Basic.img/Tab3/" + name);
			if ((Object)(object)val4 != (Object)null)
			{
				GUI.DrawTexture(new Rect(dx, y, (float)((Texture)val4).width, (float)((Texture)val4).height), (Texture)(object)val4);
			}
		}
	}

	public static void RegisterWindow(string key, Rect rect, Func<bool> visible)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		windows[key] = (rect, visible);
	}

	public static bool TryWindowRect(string key, out Rect rect)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		if (windows.TryGetValue(key, out (Rect, Func<bool>) value) && value.Item2())
		{
			(rect, _) = value;
			return true;
		}
		rect = default;
		return false;
	}

	public static Vector2 ClampWindowPosition(Vector2 position, Vector2 size, float scale)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		return new Vector2(Mathf.Clamp(position.x, 0f, Mathf.Max(0f, UiKit.W / scale - size.x)), Mathf.Clamp(position.y, 0f, Mathf.Max(0f, UiKit.H / scale - size.y)));
	}

	public static bool Button(Rect rect, string node)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		return Button(rect, node, Pointer.Pos);
	}

	public static bool Button(Rect rect, string node, Vector2 pointer, bool forceNormal = false)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Invalid comparison between Unknown and I4
		bool flag = rect.Contains(pointer);
		bool flag2 = GUI.enabled && (!NativeUtilPrompt.Visible || NativeUtilPrompt.Drawing);
		bool flag3 = flag2 | forceNormal;
		Texture2D val = Art.Tex("wz://" + node + "/" + ((!flag3) ? "disabled" : ((!flag) ? "normal" : (Pointer.Down ? "pressed" : "mouseOver"))) + "/0");
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(rect, (Texture)(object)val);
		}
		bool num = (flag2 && (int)Event.current.type == 7 && Pointer.Clicked) & flag;
		if (num)
		{
			Pointer.ConsumeClick();
			AudioManager.Play("UI/BtMouseClick", 0.7f, pitchVary: false);
		}
		return num;
	}

	public static float Scroll4(Rect bar, Rect view, float value, float maximum, float step, Vector2 pointer)
	{
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0368: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Invalid comparison between Unknown and I4
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0253: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_030c: Unknown result type (might be due to invalid IL or missing references)
		//IL_028a: Unknown result type (might be due to invalid IL or missing references)
		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0293: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b5: Unknown result type (might be due to invalid IL or missing references)
		value = Mathf.Clamp(value, 0f, maximum);
		bool flag = maximum > 0f;
		string text = "wz://UI/Basic.img/VScr4/" + (flag ? "enabled/" : "disabled/");
		Rect val = new Rect(bar.x, bar.y + 13f, 15f, bar.height - 26f);
		Texture2D val2 = Art.Tex(text + "base");
		for (float num = val.y; num < val.yMax; num += 13f)
		{
			float num2 = Mathf.Min(13f, val.yMax - num);
			if ((Object)(object)val2 != (Object)null)
			{
				GUI.DrawTextureWithTexCoords(new Rect(bar.x, num, 15f, num2), (Texture)(object)val2, new Rect(0f, 1f - num2 / 13f, 1f, num2 / 13f));
			}
		}
		GUI.DrawTexture(new Rect(bar.x, bar.y, 15f, 13f), (Texture)(object)Art.Tex(text + (flag ? "prev0" : "prev")));
		GUI.DrawTexture(new Rect(bar.x, bar.yMax - 13f, 15f, 13f), (Texture)(object)Art.Tex(text + (flag ? "next0" : "next")));
		if (!flag)
		{
			return 0f;
		}
		int controlID = GUIUtility.GetControlID((FocusType)2);
		float num3 = Mathf.Max(1f, val.height - 25f);
		Rect val3 = new Rect(bar.x, val.y + num3 * value / maximum, 15f, 25f);
		if (GUI.enabled && (int)Event.current.type == 7)
		{
			if (view.Contains(pointer))
			{
				value -= Input.mouseScrollDelta.y * step * 2f;
			}
			if (Pointer.Pressed)
			{
				if (val3.Contains(pointer))
				{
					scrollDrag = controlID;
					scrollGrab = pointer.y - val3.y;
					Pointer.ConsumeClick();
				}
				else
				{
					Rect val4 = new Rect(bar.x, bar.y, 15f, 13f);
					if (val4.Contains(pointer))
					{
						value -= step;
						Pointer.ConsumeClick();
					}
					else
					{
						val4 = new Rect(bar.x, bar.yMax - 13f, 15f, 13f);
						if (val4.Contains(pointer))
						{
							value += step;
							Pointer.ConsumeClick();
						}
						else if (val.Contains(pointer))
						{
							value += ((pointer.y < val3.y) ? (-1f) : 1f) * Mathf.Max(16f, Mathf.Floor((maximum / step + 1f) / 16f)) * step;
							Pointer.ConsumeClick();
						}
					}
				}
			}
			if (scrollDrag == controlID)
			{
				if (Pointer.Down)
				{
					value = (pointer.y - scrollGrab - val.y) / num3 * maximum;
				}
				else
				{
					scrollDrag = 0;
				}
			}
		}
		value = Mathf.Clamp(Mathf.Round(value / step) * step, 0f, maximum);
		GUI.DrawTexture(new Rect(bar.x, val.y + num3 * value / maximum, 15f, 25f), (Texture)(object)Art.Tex(text + "thumb0"));
		return value;
	}

	public static void CharacterPortrait(float x, float y)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		FullCharacter character = GameSession.Character;
		if (character != null)
		{
			string[] array = WzCharacter.CharacterIds(character.record, character.inventory);
			Sprite val = WzCharacter.Appearance(string.Join("_", array), array, WzCharacter.AppearanceFrame(array, Time.unscaledTime));
			if ((Object)(object)val != (Object)null)
			{
				float num = x - val.pivot.x;
				Rect rect = val.rect;
				float num2 = y - rect.height + val.pivot.y;
				rect = val.rect;
				float width = rect.width;
				rect = val.rect;
				GUI.DrawTexture(new Rect(num, num2, width, rect.height), (Texture)(object)val.texture);
			}
		}
	}

	public static void DialogPanel(Rect rect)
	{
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Expected Obj, but got Unknown
		int num = Mathf.CeilToInt(rect.height);
		if (num < 86)
		{
			return;
		}
		if ((Object)(object)dialogPanel == (Object)null || ((Texture)dialogPanel).height != num)
		{
			Texture2D val = Art.Tex("wz://UI/UIWindow.img/UtilDlgEx/t");
			Texture2D val2 = Art.Tex("wz://UI/UIWindow.img/UtilDlgEx/c");
			Texture2D val3 = Art.Tex("wz://UI/UIWindow.img/UtilDlgEx/s");
			if ((Object)(object)val == (Object)null || (Object)(object)val2 == (Object)null || (Object)(object)val3 == (Object)null)
			{
				return;
			}
			Color32[] array = new Color32[((Texture)val).width * num];
			Color32[] pixels = val.GetPixels32();
			Color32[] pixels2 = val2.GetPixels32();
			Color32[] pixels3 = val3.GetPixels32();
			for (int i = 0; i < num; i++)
			{
				int num2 = num - 1 - i;
				Color32[] sourceArray;
				if (num2 < 28)
				{
					sourceArray = pixels;
				}
				else
				{
					sourceArray = ((i < 58) ? pixels3 : pixels2);
				}
				int num3;
				if (num2 < 28)
				{
					num3 = 27 - num2;
				}
				else
				{
					num3 = ((i < 58) ? i : (((Texture)val2).height - 1 - (num2 - 28) % ((Texture)val2).height));
				}
				Array.Copy(sourceArray, num3 * ((Texture)val).width, array, i * ((Texture)val).width, ((Texture)val).width);
			}
			if ((Object)(object)dialogPanel != (Object)null)
			{
				Object.Destroy((Object)(object)dialogPanel);
			}
			dialogPanel = new Texture2D(((Texture)val).width, num, (TextureFormat)4, false)
			{
				name = "UtilDlgEx assembled",
				filterMode = (FilterMode)0,
				wrapMode = (TextureWrapMode)1
			};
			dialogPanel.SetPixels32(array);
			dialogPanel.Apply(false, true);
		}
		if (rect.width == (float)((Texture)dialogPanel).width)
		{
			GUI.DrawTexture(rect, (Texture)(object)dialogPanel);
		}
		else
		{
			Strip(rect, dialogPanel);
		}
	}

	private static void Strip(Rect rect, Texture2D texture)
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)texture == (Object)null) && !(rect.width <= 0f) && !(rect.height <= 0f))
		{
			float num = Mathf.Min(12f, rect.width / 2f);
			float num2 = 12f / (float)((Texture)texture).width;
			GUI.DrawTextureWithTexCoords(new Rect(rect.x, rect.y, num, rect.height), (Texture)(object)texture, new Rect(0f, 0f, num2, 1f));
			GUI.DrawTextureWithTexCoords(new Rect(rect.x + num, rect.y, rect.width - num * 2f, rect.height), (Texture)(object)texture, new Rect(num2, 0f, 1f - num2 * 2f, 1f));
			GUI.DrawTextureWithTexCoords(new Rect(rect.xMax - num, rect.y, num, rect.height), (Texture)(object)texture, new Rect(1f - num2, 0f, num2, 1f));
		}
	}

	public static void Panel(Rect rect)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		if (rect.height < 87f)
		{
			Strip(rect, Art.Tex("wz://UI/Basic.img/Notice2/c"));
			return;
		}
		Strip(new Rect(rect.x, rect.y, rect.width, 40f), Art.Tex("wz://UI/Basic.img/Notice2/t"));
		Strip(new Rect(rect.x, rect.y + 40f, rect.width, rect.height - 87f), Art.Tex("wz://UI/Basic.img/Notice2/c"));
		Strip(new Rect(rect.x, rect.yMax - 47f, rect.width, 47f), Art.Tex("wz://UI/Basic.img/Notice2/s"));
	}
}

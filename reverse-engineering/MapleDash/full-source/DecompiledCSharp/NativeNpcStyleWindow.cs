using System;
using System.Collections.Generic;
using UnityEngine;

public static class NativeNpcStyleWindow
{
	private const string Root = "UI/UIWindow.img/UtilDlgEx_Avatar/";

	private static readonly Dictionary<string, Sprite> frames = new Dictionary<string, Sprite>();

	private static GUIStyle nameStyle;

	public static void Move(NpcDialog dialog, int direction)
	{
		int num = dialog.Styles.Length;
		if (num > 0)
		{
			dialog.StyleIndex = (dialog.StyleIndex + direction % num + num) % num;
		}
	}

	public static string[] PreviewIds(NpcDialog dialog)
	{
		FullCharacter character = GameSession.Character;
		if (character == null || dialog.Styles.Length == 0)
		{
			return Array.Empty<string>();
		}
		string[] array = WzCharacter.CharacterIds(character.record, dialog.StyleClothed ? character.inventory : new List<InventoryEntry>());
		int num = dialog.Styles[dialog.StyleIndex];
		int num2 = dialog.Styles[0] / 10000;
		if (num2 == 2)
		{
			array[2] = num.ToString("D8");
		}
		else if (num2 >= 3)
		{
			array[3] = num.ToString("D8");
		}
		else
		{
			array[0] = (2000 + num).ToString("D8");
			array[1] = (12000 + num).ToString("D8");
		}
		return array;
	}

	private static void Image(Rect r, string path, float x, float y)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Art.Tex("wz://UI/UIWindow.img/UtilDlgEx_Avatar/" + path);
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(new Rect(r.x + x, r.y + y, (float)((Texture)val).width, (float)((Texture)val).height), (Texture)(object)val);
		}
	}

	private static bool Button(Rect r, string path, float x, float y)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Art.Tex("wz://" + path + "/normal/0");
		if ((Object)(object)val != (Object)null)
		{
			return NativeUi.Button(new Rect(r.x + x, r.y + y, (float)((Texture)val).width, (float)((Texture)val).height), path);
		}
		return false;
	}

	public static void Draw(NpcSession session)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_041e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Expected Obj, but got Unknown
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0435: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0317: Unknown result type (might be due to invalid IL or missing references)
		//IL_0354: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0301: Unknown result type (might be due to invalid IL or missing references)
		//IL_030d: Expected Obj, but got Unknown
		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
		//IL_0280: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_0290: Unknown result type (might be due to invalid IL or missing references)
		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_037a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0398: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f1: Unknown result type (might be due to invalid IL or missing references)
		NpcDialog dialog = session.Dialog;
		Rect r = new Rect((UiKit.W / NpcDialogUi.WindowScale - 419f) / 2f, (UiKit.H / NpcDialogUi.WindowScale - 306f) / 2f, 419f, 306f);
		Image(r, "backgrnd", 0f, 0f);
		Image(r, "shadow", 174f, 239f);
		DialogFlow.Layout layout = DialogFlow.Measure(dialog.Content, 280f);
		float num = 28 + (int)((100f - layout.Height - 6f) / 2f);
		GUI.BeginClip(new Rect(r.x + 111f, r.y + num + 1f, 278f, 98f));
		foreach (DialogFlow.Span span in layout.Spans)
		{
			Rect val = new Rect(span.Rect.x - 1f, span.Rect.y - 1f, span.Rect.width, span.Rect.height);
			if ((Object)(object)span.Tex != (Object)null)
			{
				GUI.DrawTexture(val, (Texture)(object)span.Tex);
				continue;
			}
			GUIStyle val2 = new GUIStyle
			{
				font = UiKit.Font,
				fontSize = 12,
				fontStyle = (FontStyle)(span.Run.Bold ? 1 : 0)
			};
			val2.normal.textColor = span.Run.Color;
			GUI.Label(val, span.Text, val2);
		}
		GUI.EndClip();
		if (dialog.Styles.Length != 0)
		{
			string[] array = PreviewIds(dialog);
			if (array.Length != 0)
			{
				int frame = WzCharacter.AppearanceFrame(array, Time.unscaledTime);
				string key = string.Join("_", array) + "/" + frame;
				if (!frames.TryGetValue(key, out var value))
				{
					value = WzCharacter.Appearance(string.Join("_", array), array, frame);
					frames[key] = value;
				}
				if ((Object)(object)value != (Object)null)
				{
					float num2 = r.x + 207f - value.pivot.x;
					float num3 = r.y + 245f;
					Rect rect = value.rect;
					float num4 = num3 - (rect.height - value.pivot.y);
					rect = value.rect;
					float width = rect.width;
					rect = value.rect;
					GUI.DrawTexture(new Rect(num2, num4, width, rect.height), (Texture)(object)value.texture);
				}
			}
			Image(r, "nameTag", 123f, 246f);
			if (nameStyle == null)
			{
				nameStyle = new GUIStyle
				{
					font = UiKit.Font,
					fontSize = 12,
					alignment = (TextAnchor)1
				};
			}
			nameStyle.normal.textColor = Color.black;
			int id = dialog.Styles[dialog.StyleIndex];
			GUI.Label(new Rect(r.x + 123f, r.y + 246f, 173f, 19f), WzNames.HasItem(id) ? WzNames.Item(id) : "", nameStyle);
			if (Button(r, "UI/UIWindow.img/UtilDlgEx_Avatar/BtPrev", 15f, 195f))
			{
				Move(dialog, -1);
			}
			if (Button(r, "UI/UIWindow.img/UtilDlgEx_Avatar/BtNext", 341f, 195f))
			{
				Move(dialog, 1);
			}
			if (Button(r, "UI/Basic.img/BtOK2", 314f, 281f))
			{
				session.Reply(1, dialog.StyleIndex);
				return;
			}
			string text = (dialog.StyleClothed ? "BtOff" : "BtOn");
			if (Button(r, "UI/UIWindow.img/UtilDlgEx_Avatar/" + text, 172f, 281f))
			{
				dialog.StyleClothed = !dialog.StyleClothed;
			}
		}
		if (Button(r, "UI/Basic.img/BtCancel2", 363f, 281f) || Button(r, "UI/UIWindow.img/UtilDlgEx_Avatar/BtExit", 9f, 281f))
		{
			session.Cancel();
		}
	}
}

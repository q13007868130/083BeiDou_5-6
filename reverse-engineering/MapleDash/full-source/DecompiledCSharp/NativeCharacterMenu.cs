using Server.Bot;
using UnityEngine;

public static class NativeCharacterMenu
{
	private static BotCharacter target;

	private static Vector2 position;

	private static bool party;

	public static bool Visible => target != null;

	public static void Close()
	{
		target = null;
	}

	public static void Open(BotCharacter actor)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		target = actor;
		SocialResponse socialResponse = NativeSocialWindow.Request(SocialOperation.Read);
		party = !socialResponse.party || socialResponse.partyLeader == GameSession.CharacterId;
		position = NativeUi.ClampWindowPosition(Pointer.Pos / BottomBar.S, new Vector2(100f, (float)(party ? 120 : 105)), BottomBar.S);
		Pointer.ConsumeClick();
	}

	public static void Draw()
	{
		//IL_03b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Expected Obj, but got Unknown
		//IL_01c2: Expected Obj, but got Unknown
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_020c: Expected Obj, but got Unknown
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_0386: Unknown result type (might be due to invalid IL or missing references)
		//IL_038c: Invalid comparison between Unknown and I4
		//IL_0398: Unknown result type (might be due to invalid IL or missing references)
		if (target == null)
		{
			return;
		}
		if (!target.online || target.mapId != Profile.LastMapId || target.world != GameSession.World || target.channel != GameSession.Channel)
		{
			Close();
			return;
		}
		float s = BottomBar.S;
		Vector2 pointer = Pointer.Pos / s;
		int num = (party ? 6 : 5);
		Rect r = new Rect(position.x, position.y, 100f, (float)((num + 2) * 15));
		NativeUi.RegisterWindow("NativeCharacterMenu", new Rect(r.x * s, r.y * s, r.width * s, r.height * s), () => Visible);
		Matrix4x4 matrix = GUI.matrix;
		bool enabled = GUI.enabled;
		GUI.matrix = matrix * Matrix4x4.Scale(new Vector3(s, s, 1f));
		try
		{
			UiKit.Fill(r, Color.white);
			Image("t", 0f, 0f, 100f, 23f);
			for (int num2 = 0; num2 < num; num2++)
			{
				Image("c", 3f, 23 + num2 * 15, 97f, 15f);
			}
			Image("s", 3f, 23 + num * 15, 97f, 7f);
			GUIStyle val = new GUIStyle
			{
				font = UiKit.Font,
				fontSize = 12,
				clipping = (TextClipping)1,
				padding = new RectOffset()
			};
			val.normal.textColor = Color.black;
			string text = target.name;
			while (text.Length > 0 && val.CalcSize(new GUIContent(text)).x > 78f)
			{
				text = text.Substring(0, text.Length - 1);
			}
			GUI.Label(new Rect(position.x + 11f, position.y + 6f, 78f, 16f), text, val);
			if (Button("BtInfo", 24))
			{
				NativeCharacterInfo.Open(target);
				Close();
			}
			else if (Button("BtSave", 39))
			{
				NativeSocialWindow.Request(SocialOperation.AddBuddy, target.name);
				Close();
			}
			else if (Button("BtTrade", 54))
			{
				NativeTradeWindow.Invite(target.id);
				Close();
			}
			else if (Button("BtWhisper", 69))
			{
				BottomBar.FocusChat(NativeChatChannel.Whisper, target.name);
				Close();
			}
			else if (party && Button("BtParty", 84))
			{
				SocialResponse socialResponse = NativeSocialWindow.Request(SocialOperation.InviteParty, target.name);
				if (!socialResponse.ok)
				{
					NativeSystemMessage.Drop(5, socialResponse.error);
				}
				Close();
			}
			else if (Button("BtClaim", party ? 99 : 84))
			{
				NativeClaimWindow.Open(target.name);
				Close();
			}
			else if ((int)Event.current.type == 7 && Pointer.Clicked && !r.Contains(pointer))
			{
				Close();
			}
		}
		finally
		{
			GUI.enabled = enabled;
			GUI.matrix = matrix;
		}
		bool Button(string name, int y, bool active = true)
		{
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			GUI.enabled = (enabled & active) && !NativeUtilPrompt.Visible;
			return NativeUi.Button(new Rect(position.x + 9f, position.y + (float)y, 85f, 15f), "UI/UIWindow.img/ContextMenu/" + name, pointer);
		}
		static void Image(string name, float x, float y, float w, float h)
		{
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			Texture2D val2 = Art.Tex("wz://UI/UIWindow.img/ContextMenu/" + name);
			if ((Object)(object)val2 != (Object)null)
			{
				GUI.DrawTexture(new Rect(position.x + x, position.y + y, w, h), (Texture)(object)val2);
			}
		}
	}
}

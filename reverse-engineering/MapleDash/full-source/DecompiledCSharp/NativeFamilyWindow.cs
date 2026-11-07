using System.Collections.Generic;
using UnityEngine;

public static class NativeFamilyWindow
{
	private static Vector2 position = new Vector2(80f, 40f);

	private static Vector2 grab;

	private static bool dragging;

	private static bool tree;

	private static List<CommunityMember> treeMembers;

	private static int promptedInvite;

	private static int promptedSummon;

	private static int privilege;

	private static int focusId;

	private static int selectedId;

	private static List<object> entitlements;

	private const string Root = "UI/UIWindow.img/Family/";

	private const string TreeRoot = "UI/UIWindow.img/FamilyTree/";

	private static readonly Vector2[] places = new Vector2[11]
	{
		new Vector2(222f, 40f),
		new Vector2(222f, 94f),
		new Vector2(222f, 146f),
		new Vector2(222f, 198f),
		new Vector2(365f, 198f),
		new Vector2(80f, 250f),
		new Vector2(365f, 250f),
		new Vector2(13f, 302f),
		new Vector2(148f, 302f),
		new Vector2(297f, 302f),
		new Vector2(432f, 302f)
	};

	public static void Open(string name = null)
	{
		tree = false;
		privilege = 0;
		focusId = GameSession.CharacterId;
		selectedId = 0;
		treeMembers = null;
		promptedInvite = (promptedSummon = 0);
		if (!string.IsNullOrEmpty(name) && name != GameSession.Character.record.name)
		{
			NativeCommunityWindow.InputName = name;
			CommunityResponse communityResponse = Request(CommunityOperation.ReadFamilyTree);
			if (communityResponse.ok)
			{
				tree = true;
				focusId = communityResponse.treeTarget;
				treeMembers = communityResponse.familyTree;
			}
			else
			{
				NativeCommunityWindow.Visible = false;
			}
		}
	}

	private static CommunityResponse Request(CommunityOperation op, int target = 0, int value = 0)
	{
		return NativeCommunityWindow.Request(op, target, value);
	}

	private static void Invite()
	{
		NativeUtilPrompt.Text("请输入要登记为直属同学的角色名。必须位于相同频道和地图。", NativeCommunityWindow.InputName, 2, 13, (string name) =>
		{
			NativeCommunityWindow.InputName = name;
			Request(CommunityOperation.InviteFamily);
		});
	}

	private static void Use()
	{
		int index = privilege;
		if (index <= 1)
		{
			NativeUtilPrompt.Text("请输入学院成员的角色名。", "", 2, 13, (string name) =>
			{
				NativeCommunityWindow.InputName = name;
				Submit();
			});
			return;
		}
		NativeUtilPrompt.Confirm("是否使用此名门世家特权？", (bool yes) =>
		{
			if (yes)
			{
				Submit();
			}
		});
		void Submit()
		{
			CommunityResponse communityResponse = Request(CommunityOperation.UseFamily, 0, index);
			if (communityResponse.ok && communityResponse.map != Profile.LastMapId)
			{
				NativeCommunityWindow.Visible = false;
				GameLoop.I?.field?.WarpTo(communityResponse.map);
			}
		}
	}

	private static void Separate(List<CommunityMember> family)
	{
		CommunityMember self = family.Find((CommunityMember m) => m.id == GameSession.CharacterId);
		CommunityMember communityMember = family.Find((CommunityMember m) => m.id == selectedId);
		if (communityMember != null)
		{
			Confirm(communityMember);
			return;
		}
		NativeUtilPrompt.Text("请输入要解除关系的角色名。", "", 4, 12, (string name) =>
		{
			Confirm(family.Find((CommunityMember m) => m.name == name));
		});
		void Confirm(CommunityMember relation)
		{
			if (self != null && relation != null)
			{
				bool senior = relation.id == self.senior;
				if (senior || relation.senior == self.id)
				{
					CommunityMember communityMember2 = (senior ? self : relation);
					int num = (communityMember2.level / 20 + 10) * communityMember2.level * 2;
					string text = (senior ? ("学长将被扣除 " + num + " 点声望。") : ("你将被扣除 " + num + " 点声望。"));
					if ((senior ? relation : self).senior != 0)
					{
						text = text + "上一级学长将被扣除 " + num / 2 + " 点声望。";
					}
					text += "所有下属学院成员也会受到影响。是否与所选角色解除关系？";
					NativeUtilPrompt.Confirm(text, (bool yes) =>
					{
						if (yes && Request(CommunityOperation.SeparateFamily, (!senior) ? relation.id : 0).ok)
						{
							treeMembers = null;
							focusId = GameSession.CharacterId;
							selectedId = 0;
						}
					});
				}
			}
		}
	}

	public static void Draw(CommunityResponse state)
	{
		//IL_0bf5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Invalid comparison between Unknown and I4
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_0253: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0280: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_0290: Unknown result type (might be due to invalid IL or missing references)
		//IL_0298: Unknown result type (might be due to invalid IL or missing references)
		//IL_029f: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ab: Expected Obj, but got Unknown
		//IL_02b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0361: Unknown result type (might be due to invalid IL or missing references)
		//IL_036a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0652: Unknown result type (might be due to invalid IL or missing references)
		//IL_0657: Unknown result type (might be due to invalid IL or missing references)
		//IL_065d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bae: Unknown result type (might be due to invalid IL or missing references)
		//IL_06f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_06fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0726: Unknown result type (might be due to invalid IL or missing references)
		//IL_072e: Unknown result type (might be due to invalid IL or missing references)
		//IL_077a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0781: Unknown result type (might be due to invalid IL or missing references)
		//IL_0794: Unknown result type (might be due to invalid IL or missing references)
		//IL_079c: Unknown result type (might be due to invalid IL or missing references)
		//IL_07d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_07e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_080e: Unknown result type (might be due to invalid IL or missing references)
		//IL_081b: Unknown result type (might be due to invalid IL or missing references)
		if (!NativeUtilPrompt.Visible && state.familyInvite > 0 && promptedInvite != state.familyInvite)
		{
			promptedInvite = state.familyInvite;
			NativeUtilPrompt.Confirm("是否接受学长的直属同学登记邀请？", (bool yes) =>
			{
				if (yes)
				{
					Request(CommunityOperation.AcceptFamily);
				}
			});
		}
		if (!NativeUtilPrompt.Visible && state.summonFrom > 0 && promptedSummon != state.summonFrom)
		{
			promptedSummon = state.summonFrom;
			NativeUtilPrompt.Confirm("是否接受学院同学的召集？", (bool yes) =>
			{
				CommunityResponse communityResponse = Request(CommunityOperation.AnswerSummon, 0, yes ? 1 : 0);
				if (yes && communityResponse.ok && communityResponse.map != Profile.LastMapId)
				{
					NativeCommunityWindow.Visible = false;
					GameLoop.I?.field?.WarpTo(communityResponse.map);
				}
			});
		}
		if (entitlements == null)
		{
			entitlements = (List<object>)MiniJsonDb.Parse(Resources.Load<TextAsset>("FamilyEntitlements").text);
		}
		float s = BottomBar.S;
		int num = (tree ? 578 : 224);
		int num2 = (tree ? 386 : 392);
		position = NativeUi.ClampWindowPosition(position, new Vector2((float)num, (float)num2), s);
		Vector2 val = Pointer.Pos / s;
		Rect val2;
		if ((int)Event.current.type == 7 && Pointer.Pressed)
		{
			val2 = new Rect(position.x, position.y, (float)(num - 24), 20f);
			if (val2.Contains(val))
			{
				dragging = true;
				grab = val - position;
			}
		}
		if (dragging && Pointer.Down)
		{
			position = NativeUi.ClampWindowPosition(val - grab, new Vector2((float)num, (float)num2), s);
		}
		if (!Pointer.Down)
		{
			dragging = false;
		}
		NativeUi.RegisterWindow("NativeFamilyWindow", new Rect(position.x * s, position.y * s, (float)num * s, (float)num2 * s), () => NativeCommunityWindow.SchoolVisible);
		Matrix4x4 matrix = GUI.matrix;
		bool enabled = GUI.enabled;
		GUI.matrix = matrix * Matrix4x4.Scale(new Vector3(s, s, 1f));
		GUI.BeginGroup(new Rect(position.x, position.y, (float)num, (float)num2));
		try
		{
			Vector2 p = val - position;
			string root = (tree ? "UI/UIWindow.img/FamilyTree/" : "UI/UIWindow.img/Family/");
			GUIStyle style = new GUIStyle
			{
				font = UiKit.Font,
				fontSize = 12,
				wordWrap = false,
				clipping = (TextClipping)1
			};
			style.normal.textColor = Color.black;
			Image(root + "backgrnd", 0f, 0f);
			GUI.enabled = enabled && !NativeUtilPrompt.Visible;
			string text = (tree ? "UI/UIWindow.img/FamilyTree/BtClose" : "UI/Basic.img/BtClose");
			Texture2D val3 = Art.Tex("wz://" + text + "/normal/0");
			if ((Object)(object)val3 != (Object)null && NativeUi.Button(new Rect((float)(tree ? 558 : 206), (float)(tree ? 10 : 6), (float)((Texture)val3).width, (float)((Texture)val3).height), text, p))
			{
				NativeCommunityWindow.Visible = false;
				return;
			}
			List<CommunityMember> family = ((tree && treeMembers != null) ? treeMembers : state.family);
			CommunityMember communityMember = Find(GameSession.CharacterId);
			CommunityMember communityMember2 = family.Find((CommunityMember m) => m.senior == 0);
			if (tree)
			{
				bool flag = focusId == GameSession.CharacterId;
				Text((communityMember2 == null) ? "尚未加入学院" : (communityMember2.name + "学院"), 205, 10, 167);
				CommunityMember center = Find(focusId) ?? communityMember;
				CommunityMember[] members = new CommunityMember[11];
				members[0] = communityMember2;
				members[3] = center;
				members[2] = Find(center?.senior ?? 0);
				members[1] = Find(members[2]?.senior ?? 0);
				if (members[2] != null)
				{
					members[4] = family.Find((CommunityMember m) => m.senior == members[2].id && m.id != center?.id);
				}
				List<CommunityMember> juniors = family.FindAll((CommunityMember m) => m.senior == center?.id);
				int i;
				for (i = 0; i < 2 && i < juniors.Count; i++)
				{
					members[5 + i] = juniors[i];
					List<CommunityMember> list = family.FindAll((CommunityMember m) => m.senior == juniors[i].id);
					for (int num3 = 0; num3 < 2 && num3 < list.Count; num3++)
					{
						members[7 + i * 2 + num3] = list[num3];
					}
				}
				if (!NativeUtilPrompt.Visible)
				{
					selectedId = 0;
					if (flag)
					{
						int[] array = new int[3] { 2, 5, 6 };
						foreach (int num5 in array)
						{
							if (members[num5] != null)
							{
								val2 = new Rect(places[num5].x, places[num5].y, 133f, 34f);
								if (val2.Contains(p))
								{
									selectedId = members[num5].id;
								}
							}
						}
					}
				}
				if (Button("BtJuniorEntry", 21, 57, flag && juniors.Count < 2))
				{
					Invite();
				}
				if (Button("BtBye", 71, 57, flag))
				{
					Separate(family);
				}
				for (int num6 = 0; num6 < members.Length; num6++)
				{
					CommunityMember communityMember3 = members[num6];
					Vector2 val4 = places[num6];
					if (communityMember3 == null)
					{
						if (num6 != 3)
						{
							string text2;
							if (num6 != 0)
							{
								text2 = ((num6 < 3) ? "尚未有学长" : "尚未有同学");
							}
							else
							{
								text2 = "尚未有学院长";
							}
							Text(text2, (int)val4.x, (int)val4.y + 11, 133);
						}
						continue;
					}
					Image("UI/UIWindow.img/FamilyTree/" + ((num6 == 0) ? "PlateLeader/" : "PlateOthers/") + (communityMember3.online ? 1 : 0), val4.x, val4.y);
					Text(communityMember3.name, (int)val4.x, (int)val4.y + 5, 133);
					Text("Lv." + communityMember3.level + " " + JobDefs.NameOf(communityMember3.job), (int)val4.x - 10, (int)val4.y + 20, 153);
					if (selectedId == communityMember3.id)
					{
						Image("UI/UIWindow.img/FamilyTree/selected", val4.x - 3f, val4.y - 3f);
					}
				}
				return;
			}
			Text((communityMember2 == null) ? "尚未加入学院" : (communityMember2.name + "学院"), 7, 33, 209);
			Text(state.family.FindAll((CommunityMember m) => m.senior == GameSession.CharacterId).Count + " / 2", 52, 81, 37);
			Text(state.reputation.ToString("N0") + " / " + state.totalRep.ToString("N0"), 54, 111, 159);
			Text(state.todayRep.ToString("N0"), 106, 128, 107);
			if (Button("BtJuniorEntry", 95, 77, state.family.FindAll((CommunityMember m) => m.senior == GameSession.CharacterId).Count < 2))
			{
				Invite();
			}
			if (Button("BtTree", 164, 77))
			{
				tree = true;
				treeMembers = null;
				focusId = GameSession.CharacterId;
			}
			if (Button("BtLeft", 15, 177))
			{
				privilege = (privilege + 10) % 11;
			}
			if (Button("BtRight", 196, 177))
			{
				privilege = (privilege + 1) % 11;
			}
			if (Button("BtFamilyPrecept", 209, 55, state.familyId > 0 && communityMember != null && communityMember.senior == 0))
			{
				NativeUtilPrompt.Text("请输入学院家训。", state.precepts, 1, 200, (string inputText) =>
				{
					NativeCommunityWindow.InputText = inputText;
					Request(CommunityOperation.FamilyPrecepts);
				});
			}
			if (Button("BtSpecial", 152, 322, state.familyId > 0 && !state.used.Contains(privilege) && state.reputation >= NativeCommunityService.FamilyCosts[privilege]))
			{
				Use();
			}
			if (Button("BtOK", 93, 368))
			{
				NativeCommunityWindow.Visible = false;
			}
			Dictionary<string, object> dictionary = (Dictionary<string, object>)entitlements[privilege];
			Text(dictionary["name"].ToString(), 15, 178, 193);
			Text(NativeCommunityService.FamilyCosts[privilege].ToString("N0"), 74, 198, 43);
			Text((state.used.Contains(privilege) ? 1 : 0) + " / 1", 175, 198, 32);
			Text(privilege + 1 + " / 11", 158, 158, 40);
			style.wordWrap = true;
			GUI.Label(new Rect(16f, 214f, 195f, 104f), dictionary["description"].ToString().Replace("#c", "").Replace("#", ""), style);
			bool Button(string node, int x, int y, bool active = true)
			{
				//IL_0055: Unknown result type (might be due to invalid IL or missing references)
				//IL_0069: Unknown result type (might be due to invalid IL or missing references)
				Texture2D val5 = Art.Tex("wz://" + root + node + "/normal/0");
				if ((Object)(object)val5 == (Object)null)
				{
					return false;
				}
				GUI.enabled = (enabled & active) && !NativeUtilPrompt.Visible;
				return NativeUi.Button(new Rect((float)x, (float)y, (float)((Texture)val5).width, (float)((Texture)val5).height), root + node, p);
			}
			CommunityMember Find(int id)
			{
				return family.Find((CommunityMember m) => m.id == id);
			}
			void Text(string text3, int x, int y, int w)
			{
				//IL_000b: Unknown result type (might be due to invalid IL or missing references)
				GUI.Label(new Rect((float)x, (float)y, (float)w, 17f), text3, style);
			}
		}
		finally
		{
			GUI.EndGroup();
			GUI.matrix = matrix;
			GUI.enabled = enabled;
		}
		static void Image(string path, float x, float y)
		{
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			Texture2D val5 = Art.Tex("wz://" + path);
			if ((Object)(object)val5 != (Object)null)
			{
				GUI.DrawTexture(new Rect(x, y, (float)((Texture)val5).width, (float)((Texture)val5).height), (Texture)(object)val5);
			}
		}
	}

	static NativeFamilyWindow()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
	}
}

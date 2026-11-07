using System.Collections.Generic;
using MapleDash.Wz;
using Server.Bot;
using UnityEngine;

public static class NativeCharacterInfo
{
	private static BotCharacter target;

	private static Vector2 position = new Vector2(240f, 100f);

	private static Vector2 grab;

	private static bool dragging;

	private static string[] appearance;

	private static string guild = "-";

	private static NativeCharacterInfoData details;

	private static int mode;

	private static int petIndex;

	private static int detailPane;

	private static float detailScroll;

	private static float medalScroll;

	private static float equipmentScroll;

	private static NativeCashCatalog cashCatalog;

	private const string Root = "UI/UIWindow.img/UserInfo/";

	public static bool Visible { get; private set; }

	public static void Close()
	{
		Visible = (dragging = false);
		target = null;
		appearance = null;
		NativePetExclusionWindow.Close();
	}

	public static void RefreshSelf()
	{
		if (!Visible || target != null || !GameSession.InGame)
		{
			return;
		}
		SocialResponse socialResponse = GameSession.Net.Social(new SocialRequest
		{
			characterId = GameSession.CharacterId,
			operation = SocialOperation.CharacterInfo,
			memberId = GameSession.CharacterId
		});
		if (socialResponse.ok)
		{
			details = socialResponse.characterInfo;
			guild = details.guild;
			appearance = WzCharacter.CharacterIds(GameSession.Character.record, GameSession.Character.inventory);
			petIndex = Mathf.Clamp(petIndex, 0, Mathf.Max(0, details.pets.Count - 1));
			if ((mode == 1 && details.pets.Count == 0) || (mode == 2 && details.mount == 0))
			{
				mode = 0;
			}
		}
	}

	public static void Open(BotCharacter actor = null)
	{
		if (GameSession.InGame && (actor == null || (actor.online && actor.mapId == Profile.LastMapId && actor.world == GameSession.World && actor.channel == GameSession.Channel)))
		{
			target = actor;
			appearance = ((actor == null) ? WzCharacter.CharacterIds(GameSession.Character.record, GameSession.Character.inventory) : BotAppearance.CharacterIds(actor));
			GameSession.SaveToDb();
			SocialResponse socialResponse = GameSession.Net.Social(new SocialRequest
			{
				characterId = GameSession.CharacterId,
				operation = SocialOperation.CharacterInfo,
				memberId = (actor?.id ?? GameSession.CharacterId)
			});
			if (!socialResponse.ok)
			{
				BottomBar.Toast(socialResponse.error);
				return;
			}
			details = socialResponse.characterInfo;
			guild = details.guild;
			mode = (petIndex = (detailPane = 0));
			detailScroll = (medalScroll = (equipmentScroll = 0f));
			NativePetExclusionWindow.Close();
			Visible = true;
			dragging = false;
			Pointer.ConsumeClick();
		}
	}

	public static void Draw()
	{
		//IL_11b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Invalid comparison between Unknown and I4
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_020c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
		//IL_023a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0246: Expected Obj, but got Unknown
		//IL_0252: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0435: Unknown result type (might be due to invalid IL or missing references)
		//IL_043f: Expected Obj, but got Unknown
		//IL_043a: Unknown result type (might be due to invalid IL or missing references)
		//IL_045a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0481: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0505: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03da: Unknown result type (might be due to invalid IL or missing references)
		//IL_03df: Unknown result type (might be due to invalid IL or missing references)
		//IL_03eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0408: Unknown result type (might be due to invalid IL or missing references)
		//IL_040d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0416: Unknown result type (might be due to invalid IL or missing references)
		//IL_11ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f38: Unknown result type (might be due to invalid IL or missing references)
		//IL_08dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_08e7: Expected Obj, but got Unknown
		//IL_08e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_10c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_10e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_1108: Unknown result type (might be due to invalid IL or missing references)
		//IL_1083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aa1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ab3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ab8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ac4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ad1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ad6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ae1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ae6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dde: Unknown result type (might be due to invalid IL or missing references)
		//IL_0df7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dfc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e07: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e18: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e1d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e2b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e30: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e3c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bbc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bd3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bdf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bf1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bfc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c01: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c0a: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible)
		{
			return;
		}
		if (!GameSession.InGame)
		{
			Close();
			return;
		}
		float s = BottomBar.S;
		Vector2 pointer = Pointer.Pos / s;
		int num;
		if (mode == 1)
		{
			num = 400;
		}
		else if (mode == 2)
		{
			num = 362;
		}
		else
		{
			num = ((mode == 3) ? 404 : 199);
		}
		Vector2 size = new Vector2((float)((detailPane != 0) ? 510 : (NativePetExclusionWindow.Visible ? 429 : 275)), (float)Mathf.Max(num, NativePetExclusionWindow.Visible ? ((mode == 0) ? 181 : 343) : 0));
		position = NativeUi.ClampWindowPosition(position, size, s);
		Rect val = new Rect(position.x, position.y, 275f, (float)num);
		NativeUi.RegisterWindow("NativeCharacterInfo", new Rect(val.x * s, val.y * s, val.width * s, val.height * s), () => Visible);
		Rect val2;
		if ((int)Event.current.type == 7 && Pointer.Pressed)
		{
			val2 = new Rect(val.x, val.y, 251f, 20f);
			if (val2.Contains(pointer))
			{
				dragging = true;
				grab = pointer - position;
			}
		}
		if (dragging && Pointer.Down)
		{
			position = NativeUi.ClampWindowPosition(pointer - grab, size, s);
			val.position = position;
		}
		if (!Pointer.Down)
		{
			dragging = false;
		}
		Matrix4x4 matrix = GUI.matrix;
		bool enabled = GUI.enabled;
		GUI.matrix = matrix * Matrix4x4.Scale(new Vector3(s, s, 1f));
		GUI.BeginGroup(val);
		try
		{
			pointer -= position;
			GUIStyle style = new GUIStyle
			{
				font = UiKit.Font,
				fontSize = 12,
				clipping = (TextClipping)1
			};
			style.normal.textColor = Color.black;
			string path;
			if (mode == 1)
			{
				path = "backgrnd2";
			}
			else if (mode == 2)
			{
				path = "backgrnd5";
			}
			else
			{
				path = ((mode == 3) ? "backgrnd7" : "backgrnd");
			}
			Image(path, 0f, 0f);
			GUI.enabled = enabled && !NativeUtilPrompt.Visible;
			if (NativeUi.Button(new Rect(256f, 5f, 12f, 12f), "UI/Basic.img/BtClose", pointer))
			{
				Close();
				return;
			}
			CharRecord charRecord = target?.character?.record ?? ((target == null) ? GameSession.Character.record : null);
			string text = target?.name ?? charRecord.name;
			int num2 = target?.level ?? charRecord.level;
			int jobId = target?.job ?? charRecord.job;
			int num3 = target?.fame ?? charRecord.fame;
			Sprite val3 = WzCharacter.Appearance(string.Join("_", appearance), appearance, WzCharacter.AppearanceFrame(appearance, Time.unscaledTime));
			if ((Object)(object)val3 != (Object)null)
			{
				float num4 = 55f - val3.pivot.x;
				val2 = val3.rect;
				float num5 = 130f - val2.height + val3.pivot.y;
				val2 = val3.rect;
				float width = val2.width;
				val2 = val3.rect;
				GUI.DrawTexture(new Rect(num4, num5, width, val2.height), (Texture)(object)val3.texture);
			}
			GUI.Label(new Rect(52f - style.CalcSize(new GUIContent(text)).x / 2f, 144f, 104f, 17f), text, style);
			GUI.Label(new Rect(160f, 77f, 105f, 17f), num2.ToString(), style);
			GUI.Label(new Rect(160f, 96f, 105f, 17f), JobDefs.NameOf(jobId), style);
			GUI.Label(new Rect(160f, 113f, 75f, 17f), num3.ToString(), style);
			GUI.Label(new Rect(160f, 131f, 105f, 17f), guild, style);
			Image("Marry/" + ((charRecord != null && charRecord.partnerId > 0) ? "married" : "unmarried") + "/0", 10f, 30f);
			bool active = target != null;
			if (Button("BtItem", 208f, 22f, active: true))
			{
				detailPane = ((detailPane != 1) ? 1 : 0);
				detailScroll = 0f;
			}
			if (Button("BtWish", 208f, 41f, active: true))
			{
				detailPane = ((detailPane != 2) ? 2 : 0);
				detailScroll = 0f;
			}
			if (Button("BtFamily", 105f, 23f, active: true))
			{
				NativeCommunityWindow.Toggle(GameAction.School, text);
			}
			if (Button("BtParty", 149f, 23f, active))
			{
				NativeSocialWindow.Request(SocialOperation.InviteParty, text);
			}
			if (Button("BtTrade", 149f, 42f, active))
			{
				NativeTradeWindow.Invite(target.id);
			}
			int num6;
			if (mode == 1)
			{
				num6 = 378;
			}
			else if (mode == 2)
			{
				num6 = 340;
			}
			else
			{
				num6 = ((mode == 3) ? 382 : 177);
			}
			if (Button((mode == 1) ? "BtPetHide" : "BtPetShow", 93f, num6, details.pets.Count > 0))
			{
				mode = ((mode != 1) ? 1 : 0);
				equipmentScroll = 0f;
			}
			if (Button((mode == 2) ? "BtTamingHide" : "BtTamingShow", 5f, num6, details.mount > 0))
			{
				mode = ((mode != 2) ? 2 : 0);
				equipmentScroll = 0f;
			}
			if (Button((mode == 3) ? "BtBookHide" : "BtBookShow", 145f, num6, active: true))
			{
				mode = ((mode != 3) ? 3 : 0);
				equipmentScroll = 0f;
			}
			bool active2 = target == null && details.pets.Count > 0 && details.petItems[0].Exists((int id) => (NativeItemSource.Node(id)?.Int("info/ignorePickup") ?? 0) != 0);
			if (Button(NativePetExclusionWindow.Visible ? "BtExceptionHide" : "BtExceptionShow", 199f, num6, active2))
			{
				NativePetExclusionWindow.Toggle(details.pets[Mathf.Clamp(petIndex, 0, details.pets.Count - 1)]);
			}
			if (mode == 1 && details.pets.Count > 0)
			{
				petIndex = Mathf.Clamp(petIndex, 0, details.pets.Count - 1);
				for (int num7 = 0; num7 < 3; num7++)
				{
					if (Button("BtPet" + (num7 + 1), 168 + num7 * 34, 177f, num7 < details.pets.Count))
					{
						petIndex = num7;
						equipmentScroll = 0f;
					}
				}
				InventoryEntry inventoryEntry = details.pets[petIndex];
				Dictionary<string, object> petData = inventoryEntry.petData;
				string text2 = petData["name"].ToString();
				Text(text2, (int)(52f - style.CalcSize(new GUIContent(text2)).x / 2f), 306, 104);
				Text(WzNames.Item(inventoryEntry.itemId), 58, 331, 75);
				Text(petData["level"], 58, 348);
				Text(petData["closeness"], 189, 330);
				Text(petData["fullness"], 189, 348);
				List<ImgNode> list = WzAssets.Frames(WzAssets.Node("Item/Pet/" + inventoryEntry.itemId.ToString("D7") + ".img/stand0"));
				if (list.Count > 0)
				{
					float num8 = 0f;
					foreach (ImgNode item in list)
					{
						num8 += (float)Mathf.Max(1, Mathf.Abs(item.Int("delay", 100))) / 1000f;
					}
					float num9 = Time.unscaledTime % num8;
					int num10;
					for (num10 = 0; num10 < list.Count - 1 && num9 >= (float)Mathf.Max(1, Mathf.Abs(list[num10].Int("delay", 100))) / 1000f; num10++)
					{
						num9 -= (float)Mathf.Max(1, Mathf.Abs(list[num10].Int("delay", 100))) / 1000f;
					}
					Sprite val4 = WzAssets.Sprite(list[num10]);
					if ((Object)(object)val4 != (Object)null)
					{
						float num11 = 53f - val4.pivot.x;
						val2 = val4.rect;
						float num12 = 299f - val2.height + val4.pivot.y;
						val2 = val4.rect;
						float width2 = val2.width;
						val2 = val4.rect;
						GUI.DrawTexture(new Rect(num11, num12, width2, val2.height), (Texture)(object)val4.texture);
					}
					int num13 = details.petItems[petIndex].Find((int id) => id / 10000 == 180);
					if (num13 != 0)
					{
						ImgNode imgNode = WzAssets.Node("Character/PetEquip/" + num13.ToString("D8") + ".img/" + inventoryEntry.itemId + "/stand0/" + list[num10].Name);
						Sprite val5 = ((imgNode == null) ? null : WzAssets.Sprite(imgNode));
						if ((Object)(object)val5 != (Object)null)
						{
							float num14 = 53f - val5.pivot.x;
							val2 = val5.rect;
							float num15 = 299f - val2.height + val5.pivot.y;
							val2 = val5.rect;
							float width3 = val2.width;
							val2 = val5.rect;
							GUI.DrawTexture(new Rect(num14, num15, width3, val2.height), (Texture)(object)val5.texture);
						}
					}
				}
				Equipment(details.petItems[petIndex], pet: true);
			}
			if (mode == 2)
			{
				Art.Clip clip = WzCharacter.Clip(WzCharacter.MountedAppearance(string.Join("_", appearance), details.mount), "stand1");
				if (clip != null && clip.frames.Length != 0)
				{
					float num16 = 0f;
					float[] delays = clip.delays;
					foreach (float num18 in delays)
					{
						num16 += num18;
					}
					float num19 = Mathf.Repeat(Time.unscaledTime, num16);
					int num20;
					for (num20 = 0; num20 < clip.frames.Length - 1 && num19 >= clip.delays[num20]; num20++)
					{
						num19 -= clip.delays[num20];
					}
					Sprite val6 = WzCharacter.Frame(clip.frames[num20]);
					ImgNode imgNode2 = NativeItemSource.Node(details.mount);
					int num21 = imgNode2?.Int("info/userInfoScale", 100) ?? 100;
					int num22 = imgNode2?.Int("info/userInfoDiffX") ?? 0;
					int num23 = imgNode2?.Int("info/userInfoDiffY") ?? 0;
					switch (details.mount)
					{
					case 1902002:
						num21 = 70;
						num22 = (num23 = -25);
						break;
					case 1902015:
						num21 = 80;
						num22 = 0;
						num23 = -12;
						break;
					case 1902016:
					case 1902017:
						num21 = 80;
						num22 = -5;
						num23 = -15;
						break;
					case 1902018:
						num21 = 75;
						num22 = -5;
						num23 = -20;
						break;
					}
					float num24 = (float)num21 / 100f;
					if ((Object)(object)val6 != (Object)null)
					{
						float num25 = (float)(53 + num22) - val6.pivot.x * num24;
						float num26 = 261 + num23;
						val2 = val6.rect;
						float num27 = num26 - (val2.height - val6.pivot.y) * num24;
						val2 = val6.rect;
						float num28 = val2.width * num24;
						val2 = val6.rect;
						GUI.DrawTexture(new Rect(num25, num27, num28, val2.height * num24), (Texture)(object)val6.texture);
					}
				}
				Text(WzNames.Item(details.mount), 58, 293);
				Text(details.mountLevel, 58, 311);
				Text(details.mountExp, 189, 293);
				Text(details.mountTiredness, 189, 311);
				Equipment(details.mountItems, pet: false);
			}
			if (mode == 3)
			{
				Texture2D val7 = Art.Tex("wz://UI/UIWindow.img/MonsterBook/icon/" + (details.bookLevel - 1));
				if ((Object)(object)val7 != (Object)null)
				{
					GUI.DrawTexture(new Rect(14f, 182f, (float)((Texture)val7).width, (float)((Texture)val7).height), (Texture)(object)val7);
				}
				Text(details.bookLevel, 101, 181);
				Text(details.normalCards + details.specialCards, 101, 217, 35);
				if (details.bookCover > 0)
				{
					Text(WzNames.Mob(NativeItemSource.Node(details.bookCover)?.Int("info/mob") ?? 0), 101, 199, 155);
				}
				Text(details.normalCards, 168, 217, 30);
				Text(details.specialCards, 236, 217, 30);
				if (details.medal != 0)
				{
					Text(WzNames.Item(details.medal), 101, 247, 155);
					Texture2D val8 = NativeItemSource.Icon(details.medal);
					if ((Object)(object)val8 != (Object)null)
					{
						GUI.DrawTexture(new Rect(8f, (float)(278 - ((Texture)val8).height), (float)((Texture)val8).width, (float)((Texture)val8).height), (Texture)(object)val8);
					}
				}
				Text(details.medalQuests.Count, 101, 265);
				medalScroll = NativeUi.Scroll4(new Rect(252f, 298f, 15f, 73f), new Rect(47f, 299f, 204f, 72f), medalScroll, Mathf.Max(0, details.medalQuests.Count - 4), 1f, pointer);
				for (int num29 = 0; num29 < 4; num29++)
				{
					int num30 = (int)medalScroll + num29;
					if (num30 >= details.medalQuests.Count)
					{
						break;
					}
					QuestDef questDef = QuestCatalog.Get(details.medalQuests[num30]);
					if (questDef != null && questDef.medalItem > 0)
					{
						Text(WzNames.Item(questDef.medalItem).Replace("'s Medal", ""), 52, 301 + num29 * 18, 198);
					}
				}
			}
			void Equipment(List<int> items, bool pet)
			{
				//IL_003f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0055: Unknown result type (might be due to invalid IL or missing references)
				//IL_0074: Unknown result type (might be due to invalid IL or missing references)
				//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
				int num31 = (pet ? 217 : 179);
				int num32 = (pet ? 248 : 212);
				equipmentScroll = NativeUi.Scroll4(new Rect(252f, (float)(pet ? 215 : 177), 15f, 112f), new Rect(104f, (float)num31, 147f, 112f), equipmentScroll, Mathf.Max(0, items.Count - 3), 1f, pointer);
				for (int i = 0; i < 3; i++)
				{
					int num33 = (int)equipmentScroll + i;
					if (num33 >= items.Count)
					{
						break;
					}
					int id = items[num33];
					int num34 = i * 38;
					Texture2D val9 = NativeItemSource.Icon(id);
					if ((Object)(object)val9 != (Object)null)
					{
						GUI.DrawTexture(new Rect(106f, (float)(num32 + num34 - ((Texture)val9).height), (float)((Texture)val9).width, (float)((Texture)val9).height), (Texture)(object)val9);
					}
					Text(WzNames.Item(id), 147, num31 + num34, 100);
					Image("ReqLv", 147f, num31 + 19 + num34);
					Text(NativeItemSource.Node(id)?.Int("info/reqLevel") ?? 0, 183, num31 + 18 + num34, 60);
				}
			}
			void Text(object value, int x, int y, int num31 = 85)
			{
				//IL_000b: Unknown result type (might be due to invalid IL or missing references)
				GUI.Label(new Rect((float)x, (float)y, (float)num31, 17f), value?.ToString() ?? "", style);
			}
		}
		finally
		{
			GUI.EndGroup();
			GUI.matrix = matrix;
			GUI.enabled = enabled;
		}
		DrawDetail(s);
		NativePetExclusionWindow.Draw(position, mode, s);
		bool Button(string text3, float x, float y, bool flag)
		{
			//IL_004c: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			Texture2D val9 = Art.Tex("wz://UI/UIWindow.img/UserInfo/" + text3 + "/normal/0");
			if ((Object)(object)val9 == (Object)null)
			{
				return false;
			}
			GUI.enabled = (enabled & flag) && !NativeUtilPrompt.Visible;
			return NativeUi.Button(new Rect(x, y, (float)((Texture)val9).width, (float)((Texture)val9).height), "UI/UIWindow.img/UserInfo/" + text3, pointer);
		}
		static void Image(string text3, float x, float y)
		{
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			Texture2D val9 = Art.Tex("wz://UI/UIWindow.img/UserInfo/" + text3);
			if ((Object)(object)val9 != (Object)null)
			{
				GUI.DrawTexture(new Rect(x, y, (float)((Texture)val9).width, (float)((Texture)val9).height), (Texture)(object)val9);
			}
		}
	}

	private static void DrawDetail(float scale)
	{
		//IL_046a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Expected Obj, but got Unknown
		//IL_022c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0312: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_0406: Unknown result type (might be due to invalid IL or missing references)
		//IL_0410: Unknown result type (might be due to invalid IL or missing references)
		//IL_0382: Unknown result type (might be due to invalid IL or missing references)
		//IL_0361: Unknown result type (might be due to invalid IL or missing references)
		if (detailPane == 0 || !Visible)
		{
			return;
		}
		Vector2 val = position + new Vector2(270f, (float)((mode == 0) ? 34 : 196));
		Rect rect = new Rect(val.x * scale, val.y * scale, 240f * scale, 162f * scale);
		NativeUi.RegisterWindow("NativeCharacterInfo.Detail", rect, () => Visible && detailPane != 0);
		Matrix4x4 matrix = GUI.matrix;
		bool enabled = GUI.enabled;
		GUI.matrix = matrix * Matrix4x4.Scale(new Vector3(scale, scale, 1f));
		GUI.BeginGroup(new Rect(val.x, val.y, 240f, 162f));
		try
		{
			Vector2 pointer = Pointer.Pos / scale - val;
			Texture2D val2 = Art.Tex("wz://UI/UIWindow.img/UserInfo/" + ((detailPane == 1) ? "backgrnd4" : "backgrnd3"));
			if ((Object)(object)val2 != (Object)null)
			{
				GUI.DrawTexture(new Rect(0f, 0f, (float)((Texture)val2).width, (float)((Texture)val2).height), (Texture)(object)val2);
			}
			GUI.enabled = enabled && !NativeUtilPrompt.Visible && !NativePetExclusionWindow.CoversPointer(position, mode, scale);
			if (NativeUi.Button(new Rect(220f, 6f, 12f, 12f), "UI/Basic.img/BtHide", pointer))
			{
				detailPane = 0;
				return;
			}
			List<int> list = ((detailPane == 1) ? details.items : details.wishes);
			detailScroll = NativeUi.Scroll4(new Rect(218f, 33f, 15f, 112f), new Rect(9f, 29f, 207f, 120f), detailScroll, Mathf.Max(0, list.Count - 3), 1f, pointer);
			GUIStyle val3 = new GUIStyle
			{
				font = UiKit.Font,
				fontSize = 12,
				clipping = (TextClipping)1
			};
			val3.normal.textColor = Color.black;
			for (int num = 0; num < 3; num++)
			{
				int num2 = (int)detailScroll + num;
				if (num2 >= list.Count)
				{
					break;
				}
				CashCommodity cashCommodity = null;
				if (detailPane == 2)
				{
					if (cashCatalog == null)
					{
						cashCatalog = new NativeCashCatalog();
					}
					cashCommodity = cashCatalog.Find(list[num2]);
				}
				int num3 = ((detailPane == 1) ? list[num2] : (cashCommodity?.itemId ?? 0));
				if (num3 == 0)
				{
					continue;
				}
				int num4 = num * 40;
				Texture2D val4 = NativeItemSource.Icon(num3);
				if ((Object)(object)val4 != (Object)null)
				{
					GUI.DrawTexture(new Rect(17f, (float)(64 + num4 - ((Texture)val4).height), (float)((Texture)val4).width, (float)((Texture)val4).height), (Texture)(object)val4);
				}
				GUI.Label(new Rect(57f, (float)(32 + num4), 153f, 17f), WzNames.Item(num3), val3);
				if (detailPane == 1)
				{
					Texture2D val5 = Art.Tex("wz://UI/UIWindow.img/UserInfo/ReqLv");
					if ((Object)(object)val5 != (Object)null)
					{
						GUI.DrawTexture(new Rect(57f, (float)(50 + num4), (float)((Texture)val5).width, (float)((Texture)val5).height), (Texture)(object)val5);
					}
					GUI.Label(new Rect(91f, (float)(48 + num4), 80f, 17f), (NativeItemSource.Node(num3)?.Int("info/reqLevel") ?? 0).ToString(), val3);
					continue;
				}
				GUI.Label(new Rect(57f, (float)(49 + num4), 105f, 17f), cashCommodity.price.ToString("N0") + " NX", val3);
				if (NativeUi.Button(new Rect(165f, (float)(50 + num4), 47f, 19f), "UI/UIWindow.img/UserInfo/BtPresent", pointer))
				{
					string recipient = target?.name ?? GameSession.Character.record.name;
					Close();
					NativeCashWindow.GiftWish(cashCommodity.sn, recipient);
					break;
				}
			}
		}
		finally
		{
			GUI.EndGroup();
			GUI.matrix = matrix;
			GUI.enabled = enabled;
		}
	}

	static NativeCharacterInfo()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
	}
}

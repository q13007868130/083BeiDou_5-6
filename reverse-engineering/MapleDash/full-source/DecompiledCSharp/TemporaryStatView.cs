using System.Collections.Generic;
using UnityEngine;

public static class TemporaryStatView
{
	private static readonly List<NativeShields.Entry> shields = new List<NativeShields.Entry>();

	public static Rect Slot(int index, int count, float width)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(width - 3f - (float)(count * 32) + (float)(index * 32), 23f, 32f, 32f);
	}

	public static void Draw()
	{
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_022c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_0282: Unknown result type (might be due to invalid IL or missing references)
		//IL_0269: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02df: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0301: Unknown result type (might be due to invalid IL or missing references)
		//IL_0382: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_0333: Unknown result type (might be due to invalid IL or missing references)
		//IL_033a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0345: Unknown result type (might be due to invalid IL or missing references)
		//IL_032a: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0406: Unknown result type (might be due to invalid IL or missing references)
		//IL_041c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0422: Invalid comparison between Unknown and I4
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
		NativeBuffs nativeBuffs = (((Object)(object)playerCtrl != (Object)null) ? ((Component)playerCtrl).GetComponent<NativeBuffs>() : null);
		if ((Object)(object)nativeBuffs == (Object)null)
		{
			return;
		}
		IReadOnlyList<NativeBuffs.Effect> visible = nativeBuffs.Visible;
		NativeShields.Snapshot(playerCtrl, shields);
		bool active = NativeArrowMode.Active;
		int count = visible.Count + (active ? 1 : 0) + shields.Count;
		NativeBuffs.Effect effect = null;
		bool flag = false;
		int num = -1;
		for (int i = 0; i < visible.Count; i++)
		{
			NativeBuffs.Effect effect2 = visible[i];
			Rect slot = Slot(i, count, UiKit.W);
			SkillData skillData = ((effect2.Id > 0) ? SkillBook.Get(effect2.Id) : null);
			if (skillData != null || effect2.Id <= 0)
			{
				Color color = GUI.color;
				int num2 = effect2.RemainingMs(NativeBuffs.Now);
				float num3 = ((num2 <= 3000) ? Mathf.Lerp(210f, 64f, Mathf.Repeat((float)(3000 - num2) / 500f, 1f)) : 210f);
				GUI.color = new Color(color.r, color.g, color.b, color.a * (210f / 255f) * (num3 / 255f));
				Texture2D val = ((effect2.Id < 0) ? NativeItemSource.Icon(-effect2.Id) : Art.Tex(skillData.icon));
				if ((Object)(object)val != (Object)null)
				{
					DrawCentered(slot, val);
				}
				Texture2D val2 = (effect2.NoShadow ? null : Art.Tex("wz://UI/UIWindow.img/Skill/CoolTime/" + effect2.Shadow(NativeBuffs.Now)));
				if ((Object)(object)val2 != (Object)null)
				{
					DrawCentered(slot, val2);
				}
				GUI.color = color;
				if (slot.Contains(Pointer.Pos))
				{
					effect = effect2;
				}
			}
		}
		if (active)
		{
			Rect slot2 = Slot(visible.Count, count, UiKit.W);
			SkillData skillData2 = SkillBook.Get(NativeArrowMode.ActiveFx);
			Color color2 = GUI.color;
			GUI.color = new Color(color2.r, color2.g, color2.b, color2.a * (210f / 255f));
			Texture2D val3 = ((skillData2 != null) ? Art.Tex(skillData2.icon) : null);
			if ((Object)(object)val3 != (Object)null)
			{
				DrawCentered(slot2, val3);
			}
			GUI.color = color2;
			DrawQuiverCharge(slot2);
			if (slot2.Contains(Pointer.Pos))
			{
				flag = true;
			}
		}
		int num4 = visible.Count + (active ? 1 : 0);
		for (int j = 0; j < shields.Count; j++)
		{
			NativeShields.Entry shield = shields[j];
			Rect slot3 = Slot(num4 + j, count, UiKit.W);
			SkillData skillData3 = SkillBook.Get(shield.Id);
			Color color3 = GUI.color;
			GUI.color = new Color(color3.r, color3.g, color3.b, color3.a * (210f / 255f));
			Texture2D val4 = ((skillData3 != null) ? Art.Tex(skillData3.icon) : null);
			if ((Object)(object)val4 != (Object)null)
			{
				DrawCentered(slot3, val4);
			}
			GUI.color = color3;
			DrawShieldBar(slot3, shield);
			if (slot3.Contains(Pointer.Pos))
			{
				num = j;
			}
		}
		if (flag)
		{
			SkillData skillData4 = SkillBook.Get(3141005);
			if (skillData4 != null)
			{
				SkillWindow.DrawSkillData(skillData4, Pointer.Pos, Mathf.Max(1, SkillProgress.LevelOf(3141005)));
			}
		}
		else if (num >= 0)
		{
			SkillData skillData5 = SkillBook.Get(shields[num].Id);
			if (skillData5 != null)
			{
				SkillWindow.DrawSkillData(skillData5, Pointer.Pos, shields[num].Level);
			}
		}
		else if (effect != null)
		{
			if (effect.Id < 0)
			{
				ItemTooltip.DrawForStatus(-effect.Id);
			}
			else
			{
				SkillWindow.DrawSkillData(SkillBook.Get(effect.Id), Pointer.Pos, effect.Level);
			}
			if ((int)Event.current.type == 7 && Pointer.RightPressed)
			{
				nativeBuffs.CancelByUser(effect.Id);
				Pointer.RightPressed = false;
				Pointer.ConsumeClick();
			}
		}
	}

	private static void DrawShieldBar(Rect slot, NativeShields.Entry shield)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		if (!(shield.Max <= 0f))
		{
			float num = Mathf.Clamp01(shield.Value / shield.Max);
			if (!(num <= 0f))
			{
				Color color = GUI.color;
				Rect val = new Rect(slot.x + 3f, slot.yMax - 4f, (slot.width - 6f) * num, 2f);
				GUI.color = ((shield.Mode == "stack") ? new Color(1f, 0.82f, 0.25f, 0.95f) : ((shield.Mode == "elements") ? new Color(0.45f, 0.95f, 0.9f, 0.95f) : new Color(0.62f, 0.78f, 1f, 0.95f)));
				GUI.DrawTexture(val, (Texture)(object)Texture2D.whiteTexture);
				GUI.color = color;
			}
		}
	}

	private static void DrawQuiverCharge(Rect slot)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		int capacity = NativeQuiverBurst.Capacity;
		if (capacity > 0)
		{
			float num = Mathf.Clamp01((float)NativeQuiverBurst.Loads / (float)capacity);
			if (!(num <= 0f))
			{
				Color color = GUI.color;
				Rect val = new Rect(slot.x + 3f, slot.yMax - 4f, (slot.width - 6f) * num, 2f);
				GUI.color = (NativeQuiverBurst.Active ? new Color(1f, 0.82f, 0.25f, 0.95f) : new Color(0.62f, 0.78f, 1f, 0.9f));
				GUI.DrawTexture(val, (Texture)(object)Texture2D.whiteTexture);
				GUI.color = color;
			}
		}
	}

	private static void DrawCentered(Rect slot, Texture2D texture)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		GUI.DrawTexture(new Rect(slot.x + (float)(32 - ((Texture)texture).width) / 2f, slot.y + (float)(32 - ((Texture)texture).height) / 2f, (float)((Texture)texture).width, (float)((Texture)texture).height), (Texture)(object)texture);
	}
}

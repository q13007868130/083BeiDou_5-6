using UnityEngine;

public static class SkillCast
{
	public static bool TryCast(int skillId, out string fail)
	{
		fail = null;
		PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
		SkillSystem skillSystem = (((Object)(object)playerCtrl != (Object)null) ? ((Component)playerCtrl).GetComponent<SkillSystem>() : null);
		if ((Object)(object)skillSystem == (Object)null)
		{
			fail = "当前无法施放技能";
			return false;
		}
		SkillData skillData = SkillBook.Get(skillId);
		if (skillData == null)
		{
			fail = "缺少技能数据";
			return false;
		}
		if (skillData.IsPassive && skillId != 1016 && !NativeArrowMode.IsToggle(skillId))
		{
			fail = "被动技能无需施放";
			skillSystem.Report(fail);
			return false;
		}
		if (SkillProgress.LevelOf(skillId) <= 0)
		{
			fail = "技能尚未学习（技能窗里加点）";
			skillSystem.Report(fail);
			return false;
		}
		if (skillSystem.TryCastSkill(skillId))
		{
			return true;
		}
		fail = skillSystem.LastFailure;
		if (!string.IsNullOrEmpty(fail))
		{
			skillSystem.Report(fail);
		}
		return false;
	}
}

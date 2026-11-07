using System.Collections.Generic;
using MapleDash.Wz;

public static class SkillBookItems
{
	private static readonly Dictionary<int, List<KeyValuePair<int, int>>> bySkill = new Dictionary<int, List<KeyValuePair<int, int>>>();

	private static bool tried;

	public static void Ensure()
	{
		if (tried)
		{
			return;
		}
		tried = true;
		ImgNode imgNode = WzAssets.FileRoot("Item/Consume/0228.img");
		if (imgNode == null)
		{
			return;
		}
		foreach (ImgNode child in imgNode.Children)
		{
			if (!int.TryParse(child.Name, out var result))
			{
				continue;
			}
			ImgNode imgNode2 = child.Get("info");
			if (imgNode2 == null)
			{
				continue;
			}
			int num = imgNode2.Int("masterLevel");
			ImgNode imgNode3 = imgNode2.Get("skill");
			if (num <= 0 || imgNode3 == null)
			{
				continue;
			}
			foreach (ImgNode child2 in imgNode3.Children)
			{
				if (int.TryParse((child2.Value == null) ? child2.Name : child2.Value.ToString(), out var result2))
				{
					if (!bySkill.TryGetValue(result2, out var value))
					{
						value = (bySkill[result2] = new List<KeyValuePair<int, int>>());
					}
					value.Add(new KeyValuePair<int, int>(result, num));
				}
			}
		}
	}

	public static int MasterLevelFor(int skillId)
	{
		return (GameSession.Character?.skills.Find((SkillEntry s) => s.skillId == skillId)?.masterLevel).GetValueOrDefault();
	}
}

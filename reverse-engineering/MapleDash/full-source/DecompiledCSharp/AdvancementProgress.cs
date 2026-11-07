using UnityEngine;

public static class AdvancementProgress
{
	private static readonly int[] GoddessStoneQuests = new int[3] { 38603, 38605, 38607 };

	private const int FifthAwakeningQuest = 1465;

	private const int SolErdaEntryQuest = 38608;

	private const int NewPowerQuest = 38624;

	private const int HexaMatrixQuest = 38625;

	private const int MissionVarQuest = 38616;

	public static void OnQuestCompleted(int questId)
	{
		if (questId == 1465)
		{
			JobSystem.PromoteToAdvanced();
			NativeVMatrixWindow.Open();
			return;
		}
		for (int i = 0; i < GoddessStoneQuests.Length; i++)
		{
			if (GoddessStoneQuests[i] == questId)
			{
				SetMission(i + 1);
				return;
			}
		}
		switch (questId)
		{
		case 38608:
			SetMission(4);
			break;
		case 38624:
			CompleteHexaMatrix();
			NativeHexaMatrixWindow.Open();
			break;
		}
	}

	private static void SetMission(int n)
	{
		string key = "info" + (n - 1);
		if (!(Profile.QuestProgress(38616, key, "") == "1"))
		{
			Profile.SetQuestProgress(38616, key, "1");
			Debug.Log((object)("[Advancement] QR38616 mission" + n + " = 1（写入点自实现，任务 " + n + " 阶段完成）"));
		}
	}

	private static void CompleteHexaMatrix()
	{
		if (!Profile.IsQuestCompleted(38625))
		{
			Profile.CompleteQuest(38625);
			Debug.Log((object)"[Advancement] 38624 完成 → 38625 HEXA矩陣 标记完成（写入点③，HEXA 解锁口径）");
		}
	}
}

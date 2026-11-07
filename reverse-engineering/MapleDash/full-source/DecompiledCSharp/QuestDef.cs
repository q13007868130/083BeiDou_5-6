using System;
using System.Collections.Generic;

public sealed class QuestDef
{
	public int id;

	public string name = "";

	public string parent = "";

	public int area;

	public int medalCategory;

	public int medalItem;

	public string[] info = Array.Empty<string>();

	public readonly Dictionary<int, string> stateInfo = new Dictionary<int, string>();

	public string summary = "";

	public string demandSummary = "";

	public string rewardSummary = "";

	public string startScript = "";

	public string endScript = "";

	public bool autoStart;

	public bool autoComplete;

	public bool autoPreComplete;

	public bool autoAccept;

	public QuestRequirement startReq = new QuestRequirement();

	public QuestRequirement completeReq = new QuestRequirement();

	public QuestActions startAct = new QuestActions();

	public QuestActions completeAct = new QuestActions();

	public bool HasStartScript => !string.IsNullOrEmpty(startScript);

	public bool HasEndScript => !string.IsNullOrEmpty(endScript);

	public string InfoText
	{
		get
		{
			if (info == null || info.Length == 0)
			{
				return "";
			}
			List<string> list = new List<string>(info.Length);
			for (int i = 0; i < info.Length; i++)
			{
				if (!string.IsNullOrEmpty(info[i]))
				{
					list.Add(info[i]);
				}
			}
			return string.Join("\n", list);
		}
	}
}

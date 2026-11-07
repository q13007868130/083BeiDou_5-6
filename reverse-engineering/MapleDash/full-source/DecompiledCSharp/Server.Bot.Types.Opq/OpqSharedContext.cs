using System.Collections.Generic;

namespace Server.Bot.Types.Opq;

public sealed class OpqSharedContext
{
	private readonly Dictionary<int, int> cloudAssignments = new Dictionary<int, int>();

	private readonly Dictionary<int, int> boxAssignments = new Dictionary<int, int>();

	private readonly Dictionary<int, string> platformAssignments = new Dictionary<int, string>();

	private readonly HashSet<int> taskComplete = new HashSet<int>();

	public OpqPhase CurrentPhase { get; private set; }

	public bool IsStage1Complete { get; private set; }

	public bool IsStage2Complete { get; private set; }

	public bool IsPqActive { get; private set; }

	public ICollection<int> CloudAssignmentValues => cloudAssignments.Values;

	public ICollection<int> BoxAssignmentValues => boxAssignments.Values;

	public int? MyCloudAssignment(int botId)
	{
		if (!cloudAssignments.TryGetValue(botId, out var value))
		{
			return null;
		}
		return value;
	}

	public int? MyBoxAssignment(int botId)
	{
		if (!boxAssignments.TryGetValue(botId, out var value))
		{
			return null;
		}
		return value;
	}

	public string MyPlatformAssignment(int botId)
	{
		if (!platformAssignments.TryGetValue(botId, out var value))
		{
			return null;
		}
		return value;
	}

	public bool IsMyTaskComplete(int botId)
	{
		return taskComplete.Contains(botId);
	}

	public void MarkTaskComplete(int botId)
	{
		taskComplete.Add(botId);
	}

	internal void SetCurrentPhase(OpqPhase phase)
	{
		CurrentPhase = phase;
	}

	internal void SetStage1Complete(bool value)
	{
		IsStage1Complete = value;
	}

	internal void SetStage2Complete(bool value)
	{
		IsStage2Complete = value;
	}

	internal void SetPqActive(bool value)
	{
		IsPqActive = value;
	}

	internal void PutCloudAssignment(int botId, int? objectId)
	{
		if (objectId.HasValue)
		{
			cloudAssignments[botId] = objectId.Value;
		}
		else
		{
			cloudAssignments.Remove(botId);
		}
	}

	internal void PutBoxAssignment(int botId, int? objectId)
	{
		if (objectId.HasValue)
		{
			boxAssignments[botId] = objectId.Value;
		}
		else
		{
			boxAssignments.Remove(botId);
		}
	}

	internal void PutPlatformAssignment(int botId, string platformId)
	{
		if (string.IsNullOrEmpty(platformId))
		{
			platformAssignments.Remove(botId);
		}
		else
		{
			platformAssignments[botId] = platformId;
		}
	}

	public void ClearPlatformAssignment(int botId)
	{
		platformAssignments.Remove(botId);
	}

	internal void ClearTaskComplete(int botId)
	{
		taskComplete.Remove(botId);
	}

	internal void Reset()
	{
		CurrentPhase = OpqPhase.Inactive;
		IsStage1Complete = false;
		IsStage2Complete = false;
		IsPqActive = false;
		cloudAssignments.Clear();
		boxAssignments.Clear();
		platformAssignments.Clear();
		taskComplete.Clear();
	}
}

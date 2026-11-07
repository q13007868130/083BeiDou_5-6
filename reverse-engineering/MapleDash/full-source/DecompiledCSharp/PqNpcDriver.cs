using System;
using System.Collections.Generic;
using UnityEngine;

public static class PqNpcDriver
{
	private const double StepSeconds = 0.08;

	private const double TimeoutSeconds = 8.0;

	private static NpcSession session;

	private static PqNpcPolicy policy;

	private static string[] prefer;

	private static Action<bool> finished;

	private static int npcId;

	private static int steps;

	private static int maxSteps;

	private static double nextAt;

	private static double deadline;

	private static bool running;

	public static bool Busy => running;

	public static int ActiveNpc
	{
		get
		{
			if (!running)
			{
				return 0;
			}
			return npcId;
		}
	}

	public static void Begin(int npcId, PqNpcPolicy policy, string[] prefer, Action<bool> done, int maxSteps = 24)
	{
		if (!GameSession.InGame)
		{
			done?.Invoke(obj: false);
			return;
		}
		Cancel();
		PqNpcDriver.npcId = npcId;
		PqNpcDriver.policy = policy;
		PqNpcDriver.prefer = prefer;
		finished = done;
		PqNpcDriver.maxSteps = Math.Max(1, maxSteps);
		steps = 0;
		running = true;
		NpcSession.Silent = true;
		nextAt = Time.realtimeSinceStartupAsDouble;
		deadline = nextAt + 8.0;
		session = NpcSession.Talk(npcId);
		if (session == null)
		{
			Finish(ok: false);
		}
		else if (policy == PqNpcPolicy.OpenOnly)
		{
			session.Cancel();
			Finish(ok: true);
		}
	}

	public static void Tick()
	{
		if (!running)
		{
			return;
		}
		double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
		if (realtimeSinceStartupAsDouble < nextAt)
		{
			return;
		}
		nextAt = realtimeSinceStartupAsDouble + 0.08;
		if (realtimeSinceStartupAsDouble > deadline || session == null)
		{
			Finish(ok: false);
			return;
		}
		if (session.Closed)
		{
			Finish(ok: true);
			return;
		}
		NpcDialog dialog = session.Dialog;
		if (dialog == null || !dialog.Visible)
		{
			if (session.Disposed)
			{
				Finish(ok: true);
			}
			return;
		}
		if (++steps > maxSteps)
		{
			Finish(ok: false);
			return;
		}
		try
		{
			switch (dialog.Mode)
			{
			case NpcDialogMode.Simple:
				session.Reply(1, Choose(dialog));
				break;
			case NpcDialogMode.Style:
				session.Reply(1, (dialog.Styles.Length != 0) ? dialog.StyleIndex : 0);
				break;
			case NpcDialogMode.GetNumber:
				dialog.Input = dialog.NumberDef.ToString();
				session.Reply(1);
				break;
			case NpcDialogMode.GetText:
				dialog.Input = "";
				session.Reply(1);
				break;
			default:
				session.Reply(1);
				break;
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning((object)("[PqNpc] npc=" + npcId + " 代点失败：" + ex.Message));
			Finish(ok: false);
		}
	}

	private static int Choose(NpcDialog dialog)
	{
		List<int> list = DialogFlow.Links(dialog.Content);
		if (list.Count == 0)
		{
			return 0;
		}
		string[] array = prefer ?? Array.Empty<string>();
		foreach (string value in array)
		{
			if (string.IsNullOrEmpty(value))
			{
				continue;
			}
			foreach (DialogSelection selection in dialog.Selections)
			{
				if (selection.HeaderArt == null && selection.Index >= 0 && selection.Text != null && selection.Text.Contains(value) && list.Contains(selection.Index))
				{
					return selection.Index;
				}
			}
		}
		if (!list.Contains(dialog.SelectedLink))
		{
			return list[0];
		}
		return dialog.SelectedLink;
	}

	public static void Cancel()
	{
		if (running)
		{
			Finish(ok: false);
		}
	}

	private static void Finish(bool ok)
	{
		Action<bool> action = finished;
		finished = null;
		running = false;
		NpcSession.Silent = false;
		if (session != null && !session.Closed)
		{
			session.Close();
		}
		session = null;
		try
		{
			action?.Invoke(ok);
		}
		catch (Exception ex)
		{
			Debug.LogWarning((object)("[PqNpc] 回调失败：" + ex.Message));
		}
	}
}

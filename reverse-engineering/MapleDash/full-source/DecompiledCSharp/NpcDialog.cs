using System;
using System.Collections.Generic;

public sealed class NpcDialog
{
	public NpcDialogMode Mode;

	public string RawText = "";

	public DialogContent Content = new DialogContent();

	public int NumberDef;

	public int NumberMin;

	public int NumberMax;

	public string Input = "";

	public int[] Styles = Array.Empty<int>();

	public int StyleIndex;

	public bool StyleClothed = true;

	public bool ScrollReset;

	public bool IsMenu;

	public int SelectedLink = -1;

	public bool RevealSelection;

	public bool HasPagePrevious
	{
		get
		{
			if (Mode != NpcDialogMode.Prev)
			{
				return Mode == NpcDialogMode.NextPrev;
			}
			return true;
		}
	}

	public bool HasPageNext
	{
		get
		{
			if (Mode != NpcDialogMode.Next)
			{
				return Mode == NpcDialogMode.NextPrev;
			}
			return true;
		}
	}

	public bool HasFooterOk
	{
		get
		{
			if (Mode != NpcDialogMode.Prev)
			{
				return Mode == NpcDialogMode.Ok;
			}
			return true;
		}
	}

	public bool CanConfirmWithTalkKey
	{
		get
		{
			if (!HasPageNext)
			{
				return HasFooterOk;
			}
			return true;
		}
	}

	public bool Visible => Mode != NpcDialogMode.None;

	public int MessageType
	{
		get
		{
			if (Mode != NpcDialogMode.YesNo)
			{
				if (Mode != NpcDialogMode.GetText)
				{
					if (Mode != NpcDialogMode.GetNumber)
					{
						if (Mode != NpcDialogMode.Simple)
						{
							if (Mode != NpcDialogMode.Style)
							{
								if (Mode != NpcDialogMode.AcceptDecline)
								{
									return 0;
								}
								return 12;
							}
							return 7;
						}
						return 4;
					}
					return 3;
				}
				return 2;
			}
			return 1;
		}
	}

	public IReadOnlyList<DialogSelection> Selections => Content.Selections;

	public void SelectFirst()
	{
		List<int> list = DialogFlow.Links(Content);
		SelectedLink = ((list.Count > 0) ? list[0] : (-1));
	}

	public void Set(NpcDialogMode mode, string text)
	{
		Styles = Array.Empty<int>();
		StyleIndex = 0;
		StyleClothed = true;
		Mode = mode;
		RawText = text ?? "";
		Content = DialogText.Parse(RawText);
		ScrollReset = true;
		IsMenu = false;
		SelectedLink = -1;
		if (mode == NpcDialogMode.Simple)
		{
			SelectFirst();
		}
	}

	public void Hide()
	{
		Mode = NpcDialogMode.None;
		RawText = "";
		Content = new DialogContent();
		Input = "";
		SelectedLink = -1;
	}
}

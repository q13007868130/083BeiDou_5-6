using System;
using System.Collections.Generic;
using System.Text;
using MapleDash.Wz;
using Server.Bot;

public static class NativeRenameWindow
{
	private const int ItemFamily = 517;

	private const int MinBytes = 4;

	private const int MaxBytes = 12;

	private const string IgnoreChars = " _-:\t^.,*/;!\\'\"`+";

	private const string HasName = "The pet already has a name.\nWill you change it?";

	private const string PleaseName = "Please name your pet.";

	private const string NotAvailable = "That name is not available.";

	private const string ConfirmName = "Do you want to name your pet\n'%s'?";

	private const string Cursing = "Cursing is not allowed.";

	private static int itemId;

	private static int itemPosition;

	private static InventoryEntry pet;

	private static string templateName;

	private static string currentName;

	private static readonly List<string> curseWords = new List<string>();

	public static void Open(int id, int inventoryPosition = 0)
	{
		if (id / 10000 != 517)
		{
			return;
		}
		InventoryChangeResponse inventoryChangeResponse = GameSession.Net.ChangeInventory(new InventoryChangeRequest
		{
			characterId = GameSession.CharacterId,
			itemId = id,
			getPet = true,
			petSlot = 0
		});
		if (inventoryChangeResponse.pet == null)
		{
			return;
		}
		pet = inventoryChangeResponse.pet;
		itemId = id;
		itemPosition = inventoryPosition;
		templateName = BotItemCatalog.Shared.Name(pet.itemId) ?? "";
		currentName = ((pet.petData == null || !pet.petData.TryGetValue("name", out var value)) ? "" : (value?.ToString() ?? ""));
		if (currentName != templateName)
		{
			NativeUtilPrompt.Confirm("The pet already has a name.\nWill you change it?", (bool ok) =>
			{
				if (ok)
				{
					ShowInput();
				}
			});
		}
		else
		{
			ShowInput();
		}
	}

	private static void ShowInput()
	{
		ShowInput("");
	}

	private static void ShowInput(string value)
	{
		NativeUtilPrompt.Text("Please name your pet.", value, 1, 12, OnInput);
	}

	private static void OnInput(string raw)
	{
		string text = ((raw != null) ? raw.Trim() : "");
		if (ByteLength(text) < 4 || ByteLength(text) > 12)
		{
			ShowInput(text);
			return;
		}
		if (!Filter(text, out var filtered))
		{
			BottomBar.Toast("Cursing is not allowed.");
			return;
		}
		if (filtered == currentName || filtered == templateName)
		{
			BottomBar.Toast("That name is not available.");
			return;
		}
		NativeUtilPrompt.Confirm("Do you want to name your pet\n'%s'?".Replace("%s", filtered), (bool ok) =>
		{
			if (ok)
			{
				Apply(filtered);
			}
		});
	}

	private static bool Filter(string name, out string result)
	{
		result = name;
		if (name.Length >= 1023)
		{
			return false;
		}
		foreach (char c in name)
		{
			if (c < ' ' && c != '\r' && c != '\n')
			{
				return false;
			}
		}
		LoadCurse();
		StringBuilder stringBuilder = new StringBuilder();
		List<int> list = new List<int>();
		for (int j = 0; j < name.Length; j++)
		{
			if (" _-:\t^.,*/;!\\'\"`+".IndexOf(name[j]) < 0)
			{
				stringBuilder.Append(char.ToLowerInvariant(name[j]));
				list.Add(j);
			}
		}
		string text = stringBuilder.ToString();
		char[] array = name.ToCharArray();
		bool flag = false;
		foreach (string curseWord in curseWords)
		{
			string text2 = curseWord.ToLowerInvariant();
			if (text2.Length == 0)
			{
				continue;
			}
			int startIndex = 0;
			while ((startIndex = text.IndexOf(text2, startIndex, StringComparison.Ordinal)) >= 0)
			{
				for (int k = 0; k < text2.Length && startIndex + k < list.Count; k++)
				{
					array[list[startIndex + k]] = '*';
				}
				flag = true;
				startIndex += text2.Length;
			}
		}
		if (flag)
		{
			result = new string(array);
		}
		return true;
	}

	private static void LoadCurse()
	{
		if (curseWords.Count > 0)
		{
			return;
		}
		ImgNode imgNode = WzAssets.Node("Etc/Curse.img");
		if (imgNode == null)
		{
			return;
		}
		foreach (ImgNode child in imgNode.Children)
		{
			if (child.Value is string { Length: >0 } text)
			{
				curseWords.Add(text);
			}
		}
	}

	private static int ByteLength(string s)
	{
		int num = 0;
		foreach (char c in s)
		{
			num += ((c < '\u0080') ? 1 : 2);
		}
		return num;
	}

	private static void Apply(string newName)
	{
		DbStore db = GameSession.Server.Db;
		DbRow dbRow = db.Table("pets").Find((DbRow x) => DbStore.Int(x, "petid") == pet.petId);
		if (dbRow != null)
		{
			dbRow["name"] = newName;
		}
		db.Save();
		InventoryEntry inventoryEntry = GameSession.Character.inventory.Find((InventoryEntry x) => x.petId == pet.petId);
		if (inventoryEntry != null && inventoryEntry.petData != null)
		{
			inventoryEntry.petData["name"] = newName;
		}
		GameSession.ApplyInventoryToProfile();
		Profile.RemoveItem(itemId, 1, itemPosition);
		Profile.Save();
		itemId = 0;
		itemPosition = 0;
		pet = null;
		templateName = (currentName = null);
	}
}

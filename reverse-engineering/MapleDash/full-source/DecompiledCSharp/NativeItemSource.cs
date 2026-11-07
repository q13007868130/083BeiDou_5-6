using MapleDash.Wz;
using UnityEngine;

public static class NativeItemSource
{
	public static ImgNode Node(int id)
	{
		if (id / 1000000 == 1)
		{
			string text = EquipCatalog.DirOf(id);
			string relative = "Character/" + text + "/" + id.ToString("D8") + ".img";
			if (text != null && WzAssets.HasFile(relative))
			{
				return WzAssets.FileRoot(relative);
			}
			return null;
		}
		if (id / 10000 == 500)
		{
			string relative2 = "Item/Pet/" + id + ".img";
			if (!WzAssets.HasFile(relative2))
			{
				return null;
			}
			return WzAssets.FileRoot(relative2);
		}
		string text2;
		if (id / 1000000 == 2)
		{
			text2 = "Consume";
		}
		else if (id / 1000000 == 3)
		{
			text2 = "Install";
		}
		else if (id / 1000000 == 4)
		{
			text2 = "Etc";
		}
		else
		{
			text2 = ((id / 1000000 == 5) ? "Cash" : "Special");
		}
		string relative3 = "Item/" + text2 + "/" + (id / 10000).ToString("D4") + ".img";
		if (!WzAssets.HasFile(relative3))
		{
			return null;
		}
		ImgNode imgNode = WzAssets.FileRoot(relative3);
		return imgNode.Get(id.ToString("D8")) ?? imgNode.Get(id.ToString());
	}

	public static Texture2D Icon(int id)
	{
		ImgNode imgNode = IconNode(id);
		if (imgNode != null)
		{
			return WzAssets.Texture(imgNode);
		}
		return null;
	}

	public static ImgNode IconNode(int id)
	{
		ImgNode imgNode = Node(id);
		ImgNode imgNode2 = imgNode?.Get("info/icon");
		if (imgNode2 == null)
		{
			if (imgNode == null)
			{
				return null;
			}
			imgNode2 = imgNode.Get("icon");
		}
		return imgNode2;
	}
}

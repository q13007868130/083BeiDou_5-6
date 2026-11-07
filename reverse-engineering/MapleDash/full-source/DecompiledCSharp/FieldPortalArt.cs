using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public static class FieldPortalArt
{
	public sealed class Frame
	{
		public string file;

		public Vector2 origin;

		public float delay;
	}

	public static readonly List<Frame> Visible = new List<Frame>();

	private static bool tried;

	public static void Ensure()
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		if (tried)
		{
			return;
		}
		tried = true;
		foreach (ImgNode item in WzAssets.Frames(WzAssets.Node("Map/MapHelper.img/portal/game/pv")))
		{
			ImgNode imgNode = item.Resolve();
			Visible.Add(new Frame
			{
				file = "wz://Map/MapHelper.img/portal/game/pv/" + item.Name,
				origin = WzAssets.Origin(imgNode),
				delay = Mathf.Max(0.001f, (float)Mathf.Abs(imgNode.Int("delay", 120)) / 1000f)
			});
		}
		Debug.Log((object)("[Field] 传送门动画 " + Visible.Count + " 帧"));
	}
}

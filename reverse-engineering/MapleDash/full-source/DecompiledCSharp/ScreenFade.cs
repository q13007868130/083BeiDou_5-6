using System;
using UnityEngine;

public sealed class ScreenFade : MonoBehaviour
{
	public const float OutSeconds = 0.32f;

	public const float HoldSeconds = 0.06f;

	public const float InSeconds = 0.32f;

	public const float TotalSeconds = 0.7f;

	private static ScreenFade inst;

	private float alpha;

	private Action changeScene;

	private int scenePhase;

	private double sceneAt;

	private bool previousInputLock;

	public static bool Busy
	{
		get
		{
			if ((Object)(object)inst != (Object)null)
			{
				return inst.scenePhase != 0;
			}
			return false;
		}
	}

	public static float Alpha
	{
		get
		{
			if (!((Object)(object)inst != (Object)null))
			{
				return 0f;
			}
			return inst.alpha;
		}
	}

	public static void Transition(MonoBehaviour host, Action change)
	{
		if (!Busy)
		{
			ScreenFade screenFade = Attach(host);
			screenFade.changeScene = change;
			screenFade.previousInputLock = PlayerCtrl.InputLocked;
			PlayerCtrl.InputLocked = true;
			screenFade.scenePhase = 1;
			screenFade.sceneAt = Time.realtimeSinceStartupAsDouble;
		}
	}

	private void Update()
	{
		if (scenePhase == 0)
		{
			return;
		}
		double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
		if (scenePhase == 1)
		{
			alpha = Mathf.Clamp01((float)(realtimeSinceStartupAsDouble - sceneAt) / 0.32f);
			if (!(alpha < 1f))
			{
				scenePhase = 2;
			}
			return;
		}
		if (scenePhase == 2)
		{
			Action action = changeScene;
			changeScene = null;
			try
			{
				action?.Invoke();
				return;
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
				return;
			}
			finally
			{
				sceneAt = Time.realtimeSinceStartupAsDouble;
				scenePhase = 3;
			}
		}
		if (!(realtimeSinceStartupAsDouble - sceneAt < 0.05999999865889549))
		{
			alpha = 1f - Mathf.Clamp01((float)(realtimeSinceStartupAsDouble - sceneAt - 0.05999999865889549) / 0.32f);
			if (!(alpha > 0f))
			{
				scenePhase = 0;
				PlayerCtrl.InputLocked = previousInputLock;
			}
		}
	}

	public static ScreenFade Attach(MonoBehaviour host)
	{
		if ((Object)(object)inst != (Object)null)
		{
			return inst;
		}
		inst = ((Component)host).gameObject.AddComponent<ScreenFade>();
		return inst;
	}

	public static void Set(float a)
	{
		if ((Object)(object)inst != (Object)null)
		{
			inst.alpha = Mathf.Clamp01(a);
		}
	}

	public static void Dismiss()
	{
		if (!((Object)(object)inst == (Object)null))
		{
			inst.alpha = 0f;
			inst.changeScene = null;
			if (inst.scenePhase != 0)
			{
				PlayerCtrl.InputLocked = inst.previousInputLock;
			}
			inst.scenePhase = 0;
		}
	}

	private void OnDestroy()
	{
		if ((Object)(object)inst == (Object)(object)this)
		{
			Dismiss();
		}
	}

	private void OnGUI()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		GUI.depth = -20000;
		if (!(alpha <= 0.0005f))
		{
			Matrix4x4 matrix = GUI.matrix;
			Color color = GUI.color;
			GUI.matrix = Matrix4x4.identity;
			GUI.color = new Color(0f, 0f, 0f, alpha);
			GUI.DrawTexture(new Rect(0f, 0f, (float)Screen.width, (float)Screen.height), (Texture)(object)Texture2D.whiteTexture);
			GUI.matrix = matrix;
			GUI.color = color;
		}
	}
}

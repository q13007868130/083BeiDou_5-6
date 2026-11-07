using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public sealed class WzWindowsLoader : MonoBehaviour
{
	private Action complete;

	private string phase = "正在检查资源";

	private string message;

	private Task loading;

	private volatile int loaded;

	private volatile int total;

	private bool failed;

	private bool starting;

	private float nextProgress;

	private float visibleProgress;

	private string progressText = "";

	private Camera bootCam;

	private GUIStyle titleStyle;

	private GUIStyle detailStyle;

	private GUIStyle buttonStyle;

	private void Awake()
	{
		Application.targetFrameRate = 60;
		EnsureBootCamera();
	}

	private void EnsureBootCamera()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected Obj, but got Unknown
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)bootCam != (Object)null))
		{
			GameObject val = new GameObject("[BootCamera]");
			Object.DontDestroyOnLoad((Object)(object)val);
			bootCam = val.AddComponent<Camera>();
			bootCam.clearFlags = (CameraClearFlags)2;
			bootCam.backgroundColor = new Color(0.055f, 0.07f, 0.09f, 1f);
			bootCam.cullingMask = 0;
			bootCam.orthographic = true;
			bootCam.orthographicSize = 5f;
			bootCam.depth = -1000f;
		}
	}

	public static void Begin(Action complete)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected Obj, but got Unknown
		GameObject val = new GameObject("[ResourceLoader]");
		Object.DontDestroyOnLoad((Object)val);
		val.AddComponent<WzWindowsLoader>().complete = complete;
	}

	private void Start()
	{
		((MonoBehaviour)this).StartCoroutine(Run());
	}

	private IEnumerator Run()
	{
		failed = false;
		message = null;
		WzWindowsLoader wzWindowsLoader = this;
		WzWindowsLoader wzWindowsLoader2 = this;
		int num = 0;
		wzWindowsLoader2.total = 0;
		wzWindowsLoader.loaded = num;
		visibleProgress = 0f;
		progressText = "";
		phase = "正在检查资源";
		string root = GamePlatform.ResourceRoot;
		loading = Task.Run(() =>
		{
			Prepare(root);
		});
		while (!loading.IsCompleted)
		{
			yield return null;
		}
		if (loading.IsFaulted)
		{
			failed = true;
			phase = "资源加载失败";
			message = loading.Exception.GetBaseException().Message;
			Debug.LogException(loading.Exception.GetBaseException());
			loading = null;
			yield break;
		}
		loading = null;
		starting = true;
		phase = "资源就绪，正在启动游戏";
		progressText = "正在启动游戏";
		yield return null;
		try
		{
			complete?.Invoke();
		}
		catch (Exception ex)
		{
			starting = false;
			failed = true;
			phase = "启动失败";
			message = ex.Message;
			Debug.LogException(ex);
			yield break;
		}
		float wait = 0f;
		while ((Object)(object)Camera.main == (Object)null && wait < 3f)
		{
			wait += Time.unscaledDeltaTime;
			yield return null;
		}
		if ((Object)(object)bootCam != (Object)null)
		{
			Object.Destroy((Object)(object)((Component)bootCam).gameObject);
			bootCam = null;
		}
		Object.Destroy((Object)(object)((Component)this).gameObject);
	}

	private void Prepare(string root)
	{
		Directory.CreateDirectory(root);
		if (!Ready(root))
		{
			string text = Path.Combine(root, "MapleDashResources.zip");
			if (!WzResourceParts.Exists(text))
			{
				throw new FileNotFoundException("未找到资源。请把 MapleDashResources.zip（或全部分包 part01、part02…）或已展开的 Data 目录放到 " + root + " 后点「重试」。");
			}
			phase = "正在解压资源";
			WzResourceInstaller.ExtractFull(text, root, (int n, int all) =>
			{
				total = all;
				loaded = n;
			});
			if (!Ready(root))
			{
				throw new InvalidDataException("资源解压后仍然缺少 Data 目录：" + Path.Combine(root, "Data"));
			}
		}
	}

	private static bool Ready(string root)
	{
		return Directory.Exists(Path.Combine(root, "Data"));
	}

	private void Update()
	{
		if (!starting && loading != null && !(Time.unscaledTime < nextProgress))
		{
			nextProgress = Time.unscaledTime + 0.1f;
			int num = loaded;
			int num2 = total;
			if (num2 > 0)
			{
				float num3 = Mathf.Clamp01((float)num / (float)num2);
				visibleProgress = Mathf.Max(visibleProgress, num3);
				progressText = Mathf.FloorToInt(visibleProgress * 100f) + "%   " + num + "/" + num2;
				phase = "正在解压资源";
			}
		}
	}

	private void Retry()
	{
		if (loading == null)
		{
			((MonoBehaviour)this).StopAllCoroutines();
			((MonoBehaviour)this).StartCoroutine(Run());
		}
	}

	private void OpenResourceFolder()
	{
		try
		{
			Process.Start("explorer.exe", "\"" + GamePlatform.ResourceRoot + "\"");
		}
		catch (Exception ex)
		{
			message = "打开资源目录失败：" + ex.Message;
		}
	}

	private void OnGUI()
	{
		//IL_0386: Unknown result type (might be due to invalid IL or missing references)
		//IL_038c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected Obj, but got Unknown
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Expected Obj, but got Unknown
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Expected Obj, but got Unknown
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_029a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0304: Unknown result type (might be due to invalid IL or missing references)
		//IL_0336: Unknown result type (might be due to invalid IL or missing references)
		//IL_0367: Unknown result type (might be due to invalid IL or missing references)
		if (titleStyle == null)
		{
			Font font = Resources.Load<Font>("Fonts/ui");
			titleStyle = new GUIStyle(GUI.skin.label)
			{
				font = font,
				alignment = (TextAnchor)4,
				fontSize = 19,
				wordWrap = true
			};
			titleStyle.normal.textColor = Color.white;
			detailStyle = new GUIStyle(titleStyle)
			{
				fontSize = 14
			};
			detailStyle.normal.textColor = new Color(0.75f, 0.79f, 0.84f);
			buttonStyle = new GUIStyle(GUI.skin.button)
			{
				font = font,
				fontSize = 16
			};
		}
		Matrix4x4 matrix = GUI.matrix;
		Color color = GUI.color;
		int depth = GUI.depth;
		try
		{
			GUI.depth = -1000;
			GUI.color = Color.white;
			GUI.matrix = Matrix4x4.identity;
			GUI.DrawTexture(new Rect(0f, 0f, (float)Screen.width, (float)Screen.height), (Texture)(object)Texture2D.whiteTexture, (ScaleMode)0, true, 0f, new Color(0.055f, 0.07f, 0.09f, 1f), 0f, 0f);
			float num = Mathf.Min((float)Screen.width / 960f, (float)Screen.height / 540f);
			GUI.matrix = Matrix4x4.TRS(new Vector3(((float)Screen.width - 960f * num) / 2f, ((float)Screen.height - 540f * num) / 2f, 0f), Quaternion.identity, Vector3.one * num);
			GUI.Label(new Rect(120f, 200f, 720f, 30f), phase, titleStyle);
			GUI.DrawTexture(new Rect(160f, 250f, 640f, 10f), (Texture)(object)Texture2D.whiteTexture, (ScaleMode)0, true, 0f, new Color(0.18f, 0.2f, 0.23f), 0f, 0f);
			if (visibleProgress > 0f)
			{
				GUI.DrawTexture(new Rect(160f, 250f, 640f * visibleProgress, 10f), (Texture)(object)Texture2D.whiteTexture, (ScaleMode)0, true, 0f, new Color(0.25f, 0.7f, 0.4f), 0f, 0f);
			}
			if (!string.IsNullOrEmpty(progressText))
			{
				GUI.Label(new Rect(160f, 264f, 640f, 22f), progressText, detailStyle);
			}
			GUI.Label(new Rect(80f, 300f, 800f, 40f), "资源目录：" + GamePlatform.ResourceRoot, detailStyle);
			if (!string.IsNullOrEmpty(message))
			{
				GUI.Label(new Rect(80f, 330f, 800f, 120f), message, detailStyle);
			}
			if (failed)
			{
				if (GUI.Button(new Rect(300f, 470f, 170f, 30f), "重试", buttonStyle))
				{
					Retry();
				}
				if (GUI.Button(new Rect(490f, 470f, 170f, 30f), "打开资源目录", buttonStyle))
				{
					OpenResourceFolder();
				}
			}
		}
		finally
		{
			GUI.matrix = matrix;
			GUI.color = color;
			GUI.depth = depth;
		}
	}
}

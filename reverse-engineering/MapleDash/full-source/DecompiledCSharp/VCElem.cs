using UnityEngine;

public class VCElem
{
	public VCElemType type;

	public GameAction action = GameAction.Jump;

	public KeyCode key;

	public bool shiftMod;

	public float cx = 0.5f;

	public float cy = 0.5f;

	public float size = 0.14f;

	public float opacity = 1f;

	public string label = "";

	public float scale = 1f;

	public float rotation;

	public float aspect = 1f;

	public int shape;

	public float borderW = 1.2f;

	public float halo;

	public Color bg = new Color(1f, 1f, 1f, 0.09f);

	public Color line = new Color(1f, 1f, 1f, 0.1f);

	public Color bgDown = new Color(1f, 1f, 1f, 0.22f);

	public Color lineDown = new Color(1f, 1f, 1f, 0.5f);

	public bool stickStd = true;

	public KeyCode upKey;

	public KeyCode downKey;

	public KeyCode leftKey;

	public KeyCode rightKey;

	public int finger = -1;

	public Vector2 axis;

	public bool held;

	public bool HasDirKey
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Invalid comparison between Unknown and I4
			if ((int)upKey == 0 && (int)downKey == 0 && (int)leftKey == 0)
			{
				return (int)rightKey > 0;
			}
			return true;
		}
	}

	public KeyCode Key
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			if ((int)key == 0)
			{
				return KeyMap.Of(action);
			}
			return key;
		}
	}

	public float BaseR => size * 0.5f * UiKit.H;

	public float R => BaseR * Mathf.Max(0.05f, scale);

	public float HalfW => R * Mathf.Max(0.1f, aspect);

	public Rect Rect
	{
		get
		{
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			float r = R;
			float halfW = HalfW;
			Vector2 val = VirtualControls.Center(this);
			return new Rect(val.x - halfW, val.y - r, halfW * 2f, r * 2f);
		}
	}

	public KeyCode DirKey(int dir)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		return dir switch
		{
			2 => leftKey, 
			1 => downKey, 
			0 => upKey, 
			_ => rightKey, 
		};
	}

	public void SetDirKey(int dir, KeyCode k)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		switch (dir)
		{
		case 0:
			upKey = k;
			break;
		case 1:
			downKey = k;
			break;
		case 2:
			leftKey = k;
			break;
		default:
			rightKey = k;
			break;
		}
	}

	public VCElem Clone()
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		return new VCElem
		{
			type = type,
			action = action,
			key = key,
			shiftMod = shiftMod,
			cx = cx,
			cy = cy,
			size = size,
			shape = shape,
			opacity = opacity,
			label = label,
			scale = scale,
			rotation = rotation,
			aspect = aspect,
			borderW = borderW,
			halo = halo,
			bg = bg,
			line = line,
			bgDown = bgDown,
			lineDown = lineDown,
			stickStd = stickStd,
			upKey = upKey,
			downKey = downKey,
			leftKey = leftKey,
			rightKey = rightKey
		};
	}

	public bool Hit(Vector2 p)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		float num = R * 1.35f;
		float num2 = HalfW * 1.35f;
		Vector2 val = VirtualControls.Center(this);
		Vector2 val2 = p - val;
		if (Mathf.Approximately(num2, 0f) || Mathf.Approximately(num, 0f))
		{
			return false;
		}
		return val2.x * val2.x / (num2 * num2) + val2.y * val2.y / (num * num) <= 1f;
	}

	public VCElem()
	{
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
	}
}

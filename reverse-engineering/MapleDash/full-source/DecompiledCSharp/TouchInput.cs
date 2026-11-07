public static class TouchInput
{
	public static float MoveV;

	public static float MoveH;

	private static int _laneStep;

	private static bool _jump;

	public static int TakeLaneStep()
	{
		int laneStep = _laneStep;
		_laneStep = 0;
		return laneStep;
	}

	public static bool TakeJump()
	{
		bool jump = _jump;
		_jump = false;
		return jump;
	}

	public static void Reset()
	{
		MoveH = (MoveV = 0f);
		_laneStep = 0;
		_jump = false;
	}

	public static void PushLaneStep(int d)
	{
		_laneStep = d;
	}

	public static void PushJump()
	{
		_jump = true;
	}
}

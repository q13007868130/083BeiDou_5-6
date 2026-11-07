using System.Collections.Generic;

public sealed class NativeSkillBackdropService
{
	private readonly Dictionary<int, NativeSkillBackdropState> maps = new Dictionary<int, NativeSkillBackdropState>();

	public NativeSkillBackdropState Map(int id)
	{
		if (!maps.TryGetValue(id, out var value))
		{
			value = (maps[id] = new NativeSkillBackdropState());
		}
		return value;
	}
}

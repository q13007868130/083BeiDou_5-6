public sealed class SkillDef
{
	public readonly string name;

	public readonly string icon;

	public readonly string art;

	public readonly string hint;

	public readonly string action;

	private string visualKey;

	public readonly NativeSkillDefinition native;

	public string vfx => visualKey ?? (visualKey = NativeSkillVfx.Key(native.Id, native.Level, art));

	public float cd => native.Cooldown;

	public SkillDef(ClassSkill presentation, NativeSkillDefinition definition, string visualKeyOverride = null)
	{
		name = presentation.name;
		art = presentation.art;
		icon = (string.IsNullOrEmpty(presentation.icon) ? art : presentation.icon);
		native = definition;
		visualKey = visualKeyOverride;
		action = native.Id + "@" + native.Level;
		hint = "Lv." + native.Level;
	}
}

using System.Collections.Generic;

public sealed class FullCharacter
{
	public CharRecord record;

	public long tradeRevision;

	public List<SkillEntry> skills = new List<SkillEntry>();

	public List<InventoryEntry> inventory = new List<InventoryEntry>();

	public List<KeymapEntry> keymap = new List<KeymapEntry>();

	public List<QuestEntry> quests = new List<QuestEntry>();

	public List<MacroEntry> macros = new List<MacroEntry>();

	public long quickslotKeymap;

	public GrowthState growth = new GrowthState();

	public SoulWeaponWz2State soulWeapon = new SoulWeaponWz2State();

	public List<HyperStatEntry> hyperstats = new List<HyperStatEntry>();

	public List<PotentialEntry> potentials = new List<PotentialEntry>();

	public List<HexaStatView> hexaStats = new List<HexaStatView>();
}

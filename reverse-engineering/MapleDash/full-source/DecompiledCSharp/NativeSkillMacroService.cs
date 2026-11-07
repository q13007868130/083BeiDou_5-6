public static class NativeSkillMacroService
{
	public const int MaxMacros = 5;

	public const int MaxNameLength = 12;

	public static MacroResponse Handle(DbStore db, MacroRequest r)
	{
		if (r == null || r.macros == null)
		{
			return new MacroResponse
			{
				ok = false,
				error = "无效请求"
			};
		}
		if (r.macros.Count > 5)
		{
			return new MacroResponse
			{
				ok = false,
				error = "宏数量超限"
			};
		}
		for (int i = 0; i < r.macros.Count; i++)
		{
			MacroEntry macroEntry = r.macros[i];
			if (macroEntry == null)
			{
				return new MacroResponse
				{
					ok = false,
					error = "无效宏"
				};
			}
			if (macroEntry.name != null && macroEntry.name.Length > 12)
			{
				return new MacroResponse
				{
					ok = false,
					error = "宏名过长"
				};
			}
		}
		int characterId = r.characterId;
		DbTable dbTable = db.Table("skillmacros");
		dbTable.Delete(DbStore.Eq("characterid", characterId));
		for (int j = 0; j < r.macros.Count; j++)
		{
			MacroEntry macroEntry2 = r.macros[j];
			if (!macroEntry2.IsEmpty)
			{
				dbTable.Insert(new DbRow
				{
					["characterid"] = characterId,
					["position"] = j,
					["skill1"] = macroEntry2.skill1,
					["skill2"] = macroEntry2.skill2,
					["skill3"] = macroEntry2.skill3,
					["name"] = macroEntry2.name ?? "",
					["shout"] = macroEntry2.shout
				});
			}
		}
		db.Save();
		return new MacroResponse
		{
			ok = true
		};
	}
}

using System.Collections.Generic;

public sealed class NativeWeaponShop
{
	public int id;

	public string name;

	public readonly List<WeaponDef> items = new List<WeaponDef>();
}

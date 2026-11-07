using System.Collections.Generic;
using UnityEngine;

public struct WeaponDef
{
	public string id;

	public string name;

	public int price;

	public WeaponKind kind;

	public Color color;

	public double multiplier;

	public Dictionary<string, object> stats;
}

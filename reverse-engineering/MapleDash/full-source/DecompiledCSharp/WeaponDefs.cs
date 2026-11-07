using System;
using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public static class WeaponDefs
{
	private static readonly List<WeaponDef> all = new List<WeaponDef>();

	private static readonly List<NativeWeaponShop> shops = new List<NativeWeaponShop>();

	private static bool loaded;

	public static IReadOnlyList<NativeWeaponShop> Shops
	{
		get
		{
			Ensure();
			return shops.AsReadOnly();
		}
	}

	public static int Count
	{
		get
		{
			Ensure();
			return all.Count;
		}
	}

	public static string Canonical(string id)
	{
		return id switch
		{
			"w_sword_basic" => "1302000", 
			"w_dagger" => "1332000", 
			"w_bow" => "1452000", 
			"w_mace" => "1322000", 
			"w_staff" => "1382000", 
			"w_sword_flame" => "1302007", 
			"w_greatsword" => "1402000", 
			_ => id, 
		};
	}

	private static void Ensure()
	{
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		if (loaded)
		{
			return;
		}
		loaded = true;
		foreach (Dictionary<string, object> item2 in (List<object>)NativeCombatData.Root["items"])
		{
			all.Add(new WeaponDef
			{
				id = item2["id"].ToString(),
				name = (string)item2["name"],
				kind = (WeaponKind)Enum.Parse(typeof(WeaponKind), (string)item2["kind"]),
				multiplier = Convert.ToDouble(item2["multiplier"]),
				stats = (Dictionary<string, object>)item2["stats"],
				color = new Color(0.6f, 0.8f, 1f),
				price = -1
			});
		}
		foreach (Dictionary<string, object> item3 in (List<object>)NativeCombatData.Root["shops"])
		{
			NativeWeaponShop nativeWeaponShop = new NativeWeaponShop
			{
				id = NativeCombatData.Int(item3, "id"),
				name = (string)item3["name"]
			};
			foreach (Dictionary<string, object> row in (List<object>)item3["rows"])
			{
				WeaponDef item = all.Find((WeaponDef w) => w.id == row["item"].ToString());
				item.price = NativeCombatData.Int(row, "price");
				nativeWeaponShop.items.Add(item);
				int index = all.FindIndex((WeaponDef w) => w.id == item.id);
				if (all[index].price < 0)
				{
					all[index] = item;
				}
			}
			shops.Add(nativeWeaponShop);
		}
	}

	public static bool TryGet(string id, out WeaponDef weapon)
	{
		//IL_031e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0323: Unknown result type (might be due to invalid IL or missing references)
		Ensure();
		id = Canonical(id);
		foreach (WeaponDef item in all)
		{
			if (item.id == id)
			{
				weapon = item;
				return true;
			}
		}
		if (int.TryParse(id, out var result))
		{
			int key = result / 10000;
			if (new Dictionary<int, (WeaponKind, double)>
			{
				{
					130,
					(WeaponKind.sword, 4.0)
				},
				{
					131,
					(WeaponKind.axe, 4.4)
				},
				{
					132,
					(WeaponKind.mace, 4.4)
				},
				{
					133,
					(WeaponKind.dagger, 4.0)
				},
				{
					137,
					(WeaponKind.wand, 3.6)
				},
				{
					138,
					(WeaponKind.staff, 3.6)
				},
				{
					140,
					(WeaponKind.greatsword, 4.6)
				},
				{
					141,
					(WeaponKind.greataxe, 4.8)
				},
				{
					142,
					(WeaponKind.greatmace, 4.8)
				},
				{
					143,
					(WeaponKind.spear, 5.0)
				},
				{
					144,
					(WeaponKind.polearm, 5.0)
				},
				{
					145,
					(WeaponKind.bow, 3.4)
				},
				{
					146,
					(WeaponKind.crossbow, 3.6)
				},
				{
					147,
					(WeaponKind.claw, 3.6)
				},
				{
					148,
					(WeaponKind.knuckle, 4.8)
				},
				{
					149,
					(WeaponKind.gun, 3.6)
				}
			}.TryGetValue(key, out var value))
			{
				ImgNode imgNode = EquipCatalog.Info(result);
				if (imgNode != null)
				{
					Dictionary<string, object> dictionary = new Dictionary<string, object>();
					foreach (ImgNode child in imgNode.Children)
					{
						if (child.Value is int || child.Value is long || child.Value is double || child.Value is float || child.Value is string)
						{
							dictionary[child.Name] = child.Value;
						}
					}
					weapon = new WeaponDef
					{
						id = id,
						name = WzNames.Item(result),
						kind = value.Item1,
						multiplier = value.Item2,
						stats = dictionary,
						color = new Color(0.6f, 0.8f, 1f),
						price = -1
					};
					all.Add(weapon);
					return true;
				}
			}
		}
		weapon = default;
		return false;
	}

	public static WeaponDef Get(string id)
	{
		if (!TryGet(id, out var weapon))
		{
			return Get("1302000");
		}
		return weapon;
	}

	public static WeaponDef At(int i)
	{
		Ensure();
		return all[i];
	}

	public static bool CanEquip(WeaponDef w, CharacterAttributes a, string cls)
	{
		return CanEquip(w, a, JobDefs.JobIdOf(cls));
	}

	public static bool CanEquip(WeaponDef w, CharacterAttributes a, int jobId)
	{
		Dictionary<string, object> stats = w.stats;
		int num = NativeCombatData.Int(stats, "reqJob");
		if (num != 0 && (num & JobDefs.JobBit(jobId)) == 0)
		{
			return false;
		}
		if (a.level >= NativeCombatData.Int(stats, "reqLevel") && a.str >= NativeCombatData.Int(stats, "reqSTR") && a.dex >= NativeCombatData.Int(stats, "reqDEX") && a.intelligence >= NativeCombatData.Int(stats, "reqINT"))
		{
			return a.luk >= NativeCombatData.Int(stats, "reqLUK");
		}
		return false;
	}
}

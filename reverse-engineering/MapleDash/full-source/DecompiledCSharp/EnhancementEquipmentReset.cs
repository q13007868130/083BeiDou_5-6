using System;
using System.Collections.Generic;
using System.IO;

public static class EnhancementEquipmentReset
{
	private const string Key = "equipment-282-reset";

	public static int Apply(DbStore db)
	{
		lock (db)
		{
			DbTable dbTable = db.Table("native_migrations");
			if (dbTable.Find((DbRow x) => DbStore.Str(x, "name") == "equipment-282-reset") != null)
			{
				return 0;
			}
			string text = db.FilePath + ".before-equipment-282-reset.bak";
			if (File.Exists(text))
			{
				throw new InvalidOperationException("装备切换备份已存在但缺少完成标记，请核实存档状态");
			}
			db.Save();
			File.Copy(db.FilePath, text, overwrite: false);
			HashSet<long> ids = new HashSet<long>();
			foreach (DbRow row in db.Table("inventoryitems").rows)
			{
				if (DbStore.Int(row, "itemid") / 1000000 == 1)
				{
					ids.Add(DbStore.Long(row, "inventoryitemid", 0L));
				}
			}
			foreach (DbRow row2 in db.Table("inventoryequipment").rows)
			{
				ids.Add(DbStore.Long(row2, "inventoryitemid", 0L));
			}
			int num = 0;
			db.BeginTransaction();
			try
			{
				num += db.Table("inventoryitems").Delete((DbRow x) => ids.Contains(DbStore.Long(x, "inventoryitemid", 0L)));
				db.Table("inventoryequipment").Delete((DbRow _) => true);
				string[] array = new string[2] { "dueyitems", "merchantitems" };
				foreach (string name in array)
				{
					db.Table(name).Delete((DbRow x) => ids.Contains(DbStore.Long(x, "inventoryitemid", 0L)));
				}
				num += db.Table("cash_items").Delete((DbRow x) => DbStore.Int(x, "itemid") / 1000000 == 1 || ids.Contains(DbStore.Long(x, "inventoryitemid", 0L)));
				num += db.Table("extrastorage").Delete((DbRow x) => DbStore.Int(x, "itemid") / 1000000 == 1);
				dbTable.Insert(new DbRow
				{
					["name"] = "equipment-282-reset",
					["completedAt"] = DbStore.NowText()
				});
				db.Save();
				db.Commit();
			}
			catch
			{
				db.Rollback();
				throw;
			}
			return num;
		}
	}
}

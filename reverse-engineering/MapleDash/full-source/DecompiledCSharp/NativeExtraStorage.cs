public static class NativeExtraStorage
{
	public static InventoryChangeResponse Handle(DbStore db, CharRecord character, InventoryChangeRequest request)
	{
		lock (db)
		{
			DbTable table = db.Table("extrastorage");
			DbRow row = Find();
			int num = ((row != null) ? DbStore.Int(row, "quantity") : 0);
			if (request.extraStorage == ExtraStorageOperation.Read)
			{
				return new InventoryChangeResponse
				{
					extraStorageQuantity = num
				};
			}
			if (request.quantity <= 0 || request.quantity > 32767 || request.itemId / 1000000 < 2 || request.itemId / 1000000 > 5)
			{
				return new InventoryChangeResponse
				{
					ok = false,
					error = "无效物品或存取数量"
				};
			}
			bool flag = request.extraStorage == ExtraStorageOperation.Withdraw;
			if (flag ? (num < request.quantity) : (InventoryDao.CountOf(db, (int)character.id, request.itemId) < request.quantity))
			{
				return new InventoryChangeResponse
				{
					ok = false,
					error = (flag ? "物品仓库中数量不足" : "背包中物品数量不足")
				};
			}
			if (!flag && num > int.MaxValue - request.quantity)
			{
				return new InventoryChangeResponse
				{
					ok = false,
					error = "仓库数量已达上限"
				};
			}
			using (db.DeferSaves())
			{
				db.BeginTransaction();
				try
				{
					InventoryChangeResponse inventoryChangeResponse = NativeInventoryService.Handle(db, new InventoryChangeRequest
					{
						characterId = (int)character.id,
						itemId = request.itemId,
						quantity = (flag ? request.quantity : (-request.quantity))
					});
					if (!inventoryChangeResponse.ok)
					{
						db.Rollback();
						return inventoryChangeResponse;
					}
					int num2 = num + (flag ? (-request.quantity) : request.quantity);
					if (num2 == 0)
					{
						table.Delete((DbRow value) => value == row);
					}
					else if (row != null)
					{
						row["quantity"] = (long)num2;
					}
					else
					{
						table.Insert(new DbRow
						{
							["accountid"] = (long)character.accountid,
							["characterid"] = character.id,
							["itemid"] = (long)request.itemId,
							["type"] = (long)request.extraStorageType,
							["quantity"] = (long)num2
						});
					}
					db.Commit();
					db.Save();
					inventoryChangeResponse.extraStorageQuantity = num2;
					return inventoryChangeResponse;
				}
				catch
				{
					db.Rollback();
					throw;
				}
			}
			DbRow Find()
			{
				return table.Find((DbRow r) => DbStore.Int(r, "accountid") == character.accountid && DbStore.Int(r, "characterid") == character.id && DbStore.Int(r, "itemid") == request.itemId && DbStore.Int(r, "type") == request.extraStorageType);
			}
		}
	}
}

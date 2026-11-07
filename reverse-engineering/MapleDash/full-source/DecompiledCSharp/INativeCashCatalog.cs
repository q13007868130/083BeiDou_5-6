using System.Collections.Generic;

public interface INativeCashCatalog
{
	IEnumerable<CashCommodity> All { get; }

	CashCommodity Find(int sn);

	IList<int> Package(int itemId);

	bool Exists(int itemId);

	bool Complete(int itemId);

	bool IsCash(int itemId);

	DbRow Equipment(int itemId);

	int EquipSlot(int itemId);

	string CanEquip(int itemId, CharRecord character);
}

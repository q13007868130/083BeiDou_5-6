public sealed class CashCommodity
{
	public int sn;

	public int itemId;

	public int count = 1;

	public int price;

	public int period = 1;

	public int onSale;

	public int gender = 2;

	public int priority;

	public int bonus;

	public int maplePoint;

	public int meso;

	public int forPremiumUser;

	public int clz;

	public int limit;

	public int pbCash;

	public int pbPoint;

	public int pbGift;

	public int packageSn;

	public CashCommodity Copy()
	{
		return (CashCommodity)MemberwiseClone();
	}
}

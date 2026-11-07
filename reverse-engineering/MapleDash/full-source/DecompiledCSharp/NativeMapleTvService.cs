using System;

public static class NativeMapleTvService
{
	public static bool Active;

	public static long ActiveUntil;

	public static Func<long> Now = () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

	public static void ResetForTest()
	{
		Active = false;
		ActiveUntil = 0L;
		Now = () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
	}

	private static int SendType(int tvType)
	{
		if (tvType > 2)
		{
			return tvType - 3;
		}
		return tvType;
	}

	public static TvResponse Handle(DbStore db, TvRequest r)
	{
		TvResponse tvResponse = new TvResponse();
		DbRow dbRow = db.Table("characters").Find((DbRow x) => DbStore.Int(x, "id") == r.characterId);
		if (dbRow == null)
		{
			tvResponse.error = "角色不存在";
			return tvResponse;
		}
		int itemId = r.itemId;
		if (itemId < 5075000 || itemId > 5075005)
		{
			tvResponse.error = "这不是 MapleTV 券";
			return tvResponse;
		}
		int num = itemId % 10;
		long num2 = Now();
		if (Active && num2 < ActiveUntil)
		{
			tvResponse.error = "MapleTV 正在播放中";
			return tvResponse;
		}
		long num3 = num switch
		{
			5 => 60000, 
			4 => 30000, 
			_ => 15000, 
		};
		Active = true;
		ActiveUntil = num2 + num3;
		tvResponse.ok = true;
		tvResponse.tvType = num;
		tvResponse.sendType = SendType(num);
		tvResponse.durationMs = num3;
		tvResponse.message = r.message ?? "";
		tvResponse.speaker = DbStore.Str(dbRow, "name");
		return tvResponse;
	}
}

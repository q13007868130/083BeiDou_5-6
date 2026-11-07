using System;
using System.Collections.Generic;

public static class WorldService
{
	public const int DefaultWorldId = 0;

	public static List<WorldEntry> Worlds(DbStore db)
	{
		List<WorldEntry> list = new List<WorldEntry>();
		DbTable dbTable = db.Table("world_prop");
		foreach (DbRow item in dbTable.Select(null))
		{
			if (DbStore.Int(item, "enabled") != 0 || dbTable.rows.Count <= 1)
			{
				list.Add(new WorldEntry
				{
					id = (int)DbStore.Long(item, "id", 0L),
					flag = (int)DbStore.Long(item, "flag", 0L),
					serverMessage = DbStore.Str(item, "server_message"),
					eventMessage = DbStore.Str(item, "event_message"),
					recommendMessage = DbStore.Str(item, "recommend_message"),
					channelSize = Math.Max(1, (int)DbStore.Long(item, "channel_size", 3L)),
					expRate = DbStore.Dbl(item, "exp_rate", 1.0),
					mesoRate = DbStore.Dbl(item, "meso_rate", 1.0),
					dropRate = DbStore.Dbl(item, "drop_rate", 1.0),
					questRate = DbStore.Dbl(item, "quest_rate", 1.0)
				});
			}
		}
		if (list.Count == 0)
		{
			list.Add(new WorldEntry
			{
				id = 0,
				flag = GameConfigDao.GetInt(db, "flag"),
				serverMessage = GameConfigDao.Get(db, "server_message", "Welcome to Scania!"),
				eventMessage = GameConfigDao.Get(db, "event_message", "Scania"),
				recommendMessage = GameConfigDao.Get(db, "recommend_message", "Welcome to Scania!"),
				channelSize = GameConfigDao.GetInt(db, "channel_size", 3),
				expRate = GameConfigDao.GetFloat(db, "exp_rate", 1.0),
				mesoRate = GameConfigDao.GetFloat(db, "meso_rate", 1.0),
				dropRate = GameConfigDao.GetFloat(db, "drop_rate", 1.0),
				questRate = GameConfigDao.GetFloat(db, "quest_rate", 1.0)
			});
		}
		return list;
	}

	public static List<ChannelEntry> Channels(DbStore db, int worldId)
	{
		List<ChannelEntry> list = new List<ChannelEntry>();
		int num = 3;
		DbRow dbRow = db.Table("world_prop").Find(DbStore.Eq("id", (long)worldId));
		num = ((dbRow == null) ? Math.Max(1, GameConfigDao.GetInt(db, "channel_size", 3)) : Math.Max(1, (int)DbStore.Long(dbRow, "channel_size", 3L)));
		for (int i = 1; i <= num; i++)
		{
			list.Add(new ChannelEntry
			{
				worldId = worldId,
				channel = i,
				load = ChannelLoad(i)
			});
		}
		return list;
	}

	public static int ChannelLoad(int channel)
	{
		return 0;
	}
}

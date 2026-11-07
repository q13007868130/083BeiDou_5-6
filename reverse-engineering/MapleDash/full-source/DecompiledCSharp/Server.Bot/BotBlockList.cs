using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Bot;

public sealed class BotBlockList
{
	public sealed class BlockEntry
	{
		public readonly int botId;

		public readonly int traderId;

		public readonly long blockTime;

		public readonly long expirationTime;

		public BlockEntry(int botId, int traderId, long blockTime, long expirationTime)
		{
			this.botId = botId;
			this.traderId = traderId;
			this.blockTime = blockTime;
			this.expirationTime = expirationTime;
		}

		public bool IsExpired(long now)
		{
			return now > expirationTime;
		}
	}

	private readonly List<BlockEntry> entries = new List<BlockEntry>();

	private readonly Func<long> clock;

	public int ActiveBlockCount
	{
		get
		{
			RemoveExpiredEntries();
			return entries.Count;
		}
	}

	public BotBlockList(Func<long> clock = null)
	{
		this.clock = clock ?? new Func<long>(BotScheduler.Now);
	}

	public BlockEntry AddToBlockList(int botId, int traderId, long seconds = 300L)
	{
		long num = clock();
		BlockEntry blockEntry = new BlockEntry(botId, traderId, num, num + seconds * 1000);
		entries.Add(blockEntry);
		return blockEntry;
	}

	public bool IsBlocked(int botId, int traderId)
	{
		return GetBlockEntry(botId, traderId) != null;
	}

	public BlockEntry GetBlockEntry(int botId, int traderId)
	{
		RemoveExpiredEntries();
		return entries.Find((BlockEntry e) => e.botId == botId && e.traderId == traderId);
	}

	public void RemoveExpiredEntries()
	{
		long now = clock();
		entries.RemoveAll((BlockEntry e) => e.IsExpired(now));
	}

	public bool RemoveBlock(int botId, int traderId)
	{
		return entries.RemoveAll((BlockEntry e) => e.botId == botId && e.traderId == traderId) > 0;
	}

	public List<BlockEntry> GetBlockList()
	{
		RemoveExpiredEntries();
		return new List<BlockEntry>(entries);
	}

	public List<BlockEntry> GetBlocksForBot(int botId)
	{
		return (from e in GetBlockList()
			where e.botId == botId
			select e).ToList();
	}

	public List<int> GetBlockedTraders(int botId)
	{
		return (from e in GetBlocksForBot(botId)
			select e.traderId).ToList();
	}

	public void ClearBlockList()
	{
		entries.Clear();
	}
}

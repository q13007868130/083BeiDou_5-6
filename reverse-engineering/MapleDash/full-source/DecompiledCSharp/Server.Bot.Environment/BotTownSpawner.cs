using System;
using System.Collections.Generic;
using Server.Field;

namespace Server.Bot.Environment;

public sealed class BotTownSpawner : IDisposable
{
	private readonly BotTownPresenceConfig config;

	private readonly BotTownPresenceSampler sampler;

	private readonly BotMapLoad tryMap;

	private readonly List<BotTasks.Handle> pending = new List<BotTasks.Handle>();

	private bool disposed;

	private bool running;

	public bool IsRunning => running;

	public BotTownSpawner(BotTownPresenceConfig config, BotMapLoad tryMap, BotTownPresenceSampler sampler = null)
	{
		this.config = config;
		this.tryMap = tryMap;
		this.sampler = sampler ?? new BotTownPresenceSampler();
	}

	public BotTownSpawner(BotTownPresenceConfig config, Func<int, MapRuntime> getMap, BotTownPresenceSampler sampler = null)
		: this(config, delegate(int id, out MapRuntime map, bool allowBlocking)
		{
			map = null;
			if (getMap == null)
			{
				return BotMapState.Missing;
			}
			try
			{
				map = getMap(id);
			}
			catch (ArgumentException)
			{
				return BotMapState.Missing;
			}
			return (map == null) ? BotMapState.Missing : BotMapState.Ready;
		}, sampler)
	{
	}

	public void Spawn(Action completed = null)
	{
		if (disposed)
		{
			throw new ObjectDisposedException("BotTownSpawner");
		}
		if (running)
		{
			throw new InvalidOperationException("Town spawning is already running");
		}
		running = true;
		int townIndex = 0;
		BotEnvironment.Post(NextTown, "Spawn");
		void NextTown()
		{
			if (disposed)
			{
				running = false;
			}
			else if (townIndex >= config.Towns.Count)
			{
				running = false;
				completed?.Invoke();
			}
			else
			{
				BotTownPresenceConfig.TownEntry townEntry = config.Towns[townIndex++];
				int count = Math.Min(2, townEntry.wanderers);
				int mapId = townEntry.MainMapId;
				if (count <= 0 || mapId <= 0)
				{
					BotEnvironment.Post(NextTown, "Spawn");
				}
				else
				{
					WhenMapReady(mapId, (MapRuntime map) =>
					{
						MapPortal mapPortal = map?.portals.Find((MapPortal p) => p.id == 0);
						BotNavigationGraph.Point anchor;
						List<BotNavigationGraph.Point> spots;
						List<int> ids;
						int index;
						if (mapPortal == null)
						{
							BotEnvironment.Post(NextTown, "Spawn");
						}
						else
						{
							anchor = new BotNavigationGraph.Point(mapPortal.x, mapPortal.y);
							List<BotNavigationGraph.Point> list = new List<BotNavigationGraph.Point>();
							foreach (MapNpcPosition npc in map.npcs)
							{
								list.Add(new BotNavigationGraph.Point(npc.x, npc.y));
							}
							spots = sampler.Sample(map, anchor, count, list, config.OverridesFor(mapId));
							ids = new List<int>();
							index = 0;
							NextBot();
						}
						void NextBot()
						{
							BotNavigationGraph.Point spawn;
							int attempt;
							if (!disposed)
							{
								if (index >= count)
								{
									BotTypeManager.SetAndStartBots(ids, BotType.TOWN_WANDERER_BOT);
									BotEnvironment.Post(NextTown, "Spawn");
								}
								else
								{
									spawn = ((index < spots.Count) ? spots[index] : new BotNavigationGraph.Point(anchor.x, (int)MapService.SnapToGround(mapId, anchor.x, anchor.y)));
									index++;
									attempt = 0;
									TryCreate();
								}
							}
							void TryCreate()
							{
								if (!disposed)
								{
									attempt++;
									BotCharacter botCharacter = null;
									try
									{
										botCharacter = BotGeneration.CreateBotPollReadiness(spawn.x, spawn.y, mapId, ignoreEnvironmentLimit: true);
									}
									catch (Exception)
									{
									}
									if (botCharacter != null)
									{
										ids.Add(botCharacter.id);
										BotEnvironment.Post(NextBot, "Spawn");
									}
									else if (attempt < 5)
									{
										pending.Add(BotTasks.Schedule(TryCreate, 200 * attempt, "Spawn", "/Users/lizixian/Documents/mxd/mxd-android/MapleDash/Assets/Scripts/Server/Bot/Environment/BotTownSpawner.cs"));
									}
									else
									{
										BotEnvironment.Post(NextBot, "Spawn");
									}
								}
							}
						}
					});
				}
			}
		}
	}

	private void WhenMapReady(int mapId, Action<MapRuntime> next, int attempt = 0)
	{
		if (disposed)
		{
			return;
		}
		MapRuntime mapRuntime = MapService.Get(mapId);
		BotMapState botMapState = ((mapRuntime == null) ? BotMapState.Missing : BotMapState.Ready);
		if (mapRuntime == null && tryMap != null)
		{
			try
			{
				botMapState = tryMap(mapId, out var map, attempt >= 180);
				if (botMapState == BotMapState.Ready)
				{
					mapRuntime = map;
				}
			}
			catch (ArgumentException)
			{
				botMapState = BotMapState.Missing;
			}
		}
		if (mapRuntime != null)
		{
			next(mapRuntime);
			return;
		}
		if (botMapState == BotMapState.Missing)
		{
			next(null);
			return;
		}
		BotEnvironment.PostNextFrame(() =>
		{
			WhenMapReady(mapId, next, attempt + 1);
		});
	}

	public void Dispose()
	{
		disposed = true;
		running = false;
		foreach (BotTasks.Handle item in pending)
		{
			item.Cancel();
		}
		pending.Clear();
	}
}

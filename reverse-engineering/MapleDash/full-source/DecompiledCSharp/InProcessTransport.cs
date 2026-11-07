using System;
using Server.Bot;
using Server.Bot.Trade;

public sealed class InProcessTransport : ITransport
{
	private readonly GameServer server;

	public InProcessTransport(GameServer s)
	{
		server = s;
	}

	public ScriptValueResponse ScriptValue(ScriptValueRequest r)
	{
		return server.ScriptValues.Handle(r);
	}

	public ScriptResponse Script(ScriptRequest r)
	{
		return server.Scripts.Handle(r);
	}

	public SocialResponse Social(SocialRequest r)
	{
		return server.Social.Handle(r);
	}

	public PartyGrindResponse PartyGrind(PartyGrindRequest r)
	{
		return BotPartyGrindProtocol.Handle(server.Bots, r);
	}

	public BotTradeResponse BotTrade(BotTradeRequest r)
	{
		return server.Bots.tradeProtocol.Handle(r);
	}

	public MarketResponse Market(MarketRequest r)
	{
		return server.Bots.marketProtocol.Handle(r);
	}

	public CommunityResponse Community(CommunityRequest r)
	{
		return server.Community.Handle(r);
	}

	public BossResponse Boss(BossRequest r)
	{
		return server.Bosses.Handle(r);
	}

	public InventoryChangeResponse ChangeInventory(InventoryChangeRequest r)
	{
		return NativeInventoryService.Handle(server.Db, r);
	}

	public EquipPatchResponse UpdateEquip(EquipPatchRequest r)
	{
		return NativeEquipService.Patch(server.Db, r);
	}

	public StarForceResponse StarForce(StarForceRequest r)
	{
		lock (server.Db)
		{
			if (r != null && server.ActiveBotPlayer(r.characterId)?.trade != null)
			{
				return new StarForceResponse
				{
					ok = false,
					error = "交易中不能强化装备"
				};
			}
			StarForceResponse starForceResponse = NativeStarForceService.Handle(server.Db, r);
			if (starForceResponse.ok && r.commit)
			{
				RefreshEnhancementMirror(r.characterId);
			}
			return starForceResponse;
		}
	}

	public PotentialResponse Potential(PotentialRequest r)
	{
		lock (server.Db)
		{
			if (r != null && server.ActiveBotPlayer(r.characterId)?.trade != null)
			{
				return new PotentialResponse
				{
					ok = false,
					error = "交易中不能变更潜能"
				};
			}
			PotentialResponse potentialResponse = NativePotentialService.Handle(server.Db, r);
			if (potentialResponse.ok && r.commit)
			{
				RefreshEnhancementMirror(r.characterId);
			}
			return potentialResponse;
		}
	}

	public WeeklyGemstoneResponse WeeklyGemstone(WeeklyGemstoneRequest r)
	{
		return NativeWeeklyGemstoneService.Handle(server.Db, r);
	}

	public SymbolResponse Symbol(SymbolRequest r)
	{
		lock (server.Db)
		{
			if (r != null && server.ActiveBotPlayer(r.characterId)?.trade != null)
			{
				return new SymbolResponse
				{
					ok = false,
					error = "交易中不能强化徽章"
				};
			}
			SymbolResponse symbolResponse = NativeSymbolService.Handle(server.Db, r);
			if (symbolResponse.ok && r.commit)
			{
				RefreshEnhancementMirror(r.characterId);
			}
			return symbolResponse;
		}
	}

	public HyperStatResponse HyperStat(HyperStatRequest r)
	{
		lock (server.Db)
		{
			HyperStatResponse hyperStatResponse = NativeHyperStatService.Handle(server.Db, r);
			if (hyperStatResponse.ok && r.commit)
			{
				RefreshGrowthMirror(r.characterId);
			}
			return hyperStatResponse;
		}
	}

	public AbilityResponse InnerAbility(AbilityRequest r)
	{
		lock (server.Db)
		{
			AbilityResponse abilityResponse = NativeInnerAbilityService.Handle(server.Db, r);
			if (abilityResponse.ok && r.commit)
			{
				RefreshGrowthMirror(r.characterId);
			}
			return abilityResponse;
		}
	}

	public FlameResponse Flame(FlameRequest r)
	{
		lock (server.Db)
		{
			if (r != null && server.ActiveBotPlayer(r.characterId)?.trade != null)
			{
				return new FlameResponse
				{
					ok = false,
					error = "交易中不能使用星火"
				};
			}
			FlameResponse flameResponse = NativeFlameService.Handle(server.Db, r);
			if (flameResponse.ok && r.commit)
			{
				RefreshEnhancementMirror(r.characterId);
			}
			return flameResponse;
		}
	}

	public SoulResponse Soul(SoulRequest r)
	{
		lock (server.Db)
		{
			if (r != null && server.ActiveBotPlayer(r.characterId)?.trade != null)
			{
				return new SoulResponse
				{
					ok = false,
					error = "交易中不能变更灵魂"
				};
			}
			SoulResponse soulResponse = NativeSoulService.Handle(server.Db, r);
			if (soulResponse.ok && r.commit)
			{
				RefreshEnhancementMirror(r.characterId);
			}
			return soulResponse;
		}
	}

	private void RefreshGrowthMirror(int characterId)
	{
		BotActor botActor = server.ActiveBotPlayer(characterId);
		if (botActor?.character != null)
		{
			FullCharacter fullCharacter = CharService.Load(server.Db, characterId);
			if (fullCharacter != null)
			{
				botActor.character.growth = fullCharacter.growth;
				botActor.character.hyperstats = fullCharacter.hyperstats;
				botActor.character.potentials = fullCharacter.potentials;
				NativeGrowthStats.Invalidate();
			}
		}
	}

	private void RefreshEnhancementMirror(int characterId)
	{
		BotActor botActor = server.ActiveBotPlayer(characterId);
		if (botActor?.character != null)
		{
			FullCharacter fullCharacter = CharService.Load(server.Db, characterId);
			botActor.character.inventory = fullCharacter.inventory;
			botActor.character.soulWeapon = fullCharacter.soulWeapon;
			botActor.character.record.meso = fullCharacter.record.meso;
			NativeGrowthStats.Invalidate();
			botActor.character.tradeRevision++;
		}
	}

	public SlotRemoveResponse RemoveSlot(SlotRemoveRequest r)
	{
		return NativeEquipService.RemoveSlot(server.Db, r);
	}

	public VMatrixResponse VMatrix(VMatrixRequest r)
	{
		lock (server.Db)
		{
			return NativeVMatrixService.Handle(server.Db, r);
		}
	}

	public HexaResponse Hexa(HexaRequest r)
	{
		lock (server.Db)
		{
			return NativeHexaMatrixService.Handle(server.Db, r);
		}
	}

	public HexaConvertResponse HexaConvert(HexaConvertRequest r)
	{
		lock (server.Db)
		{
			if (r != null && server.ActiveBotPlayer(r.characterId)?.trade != null)
			{
				return new HexaConvertResponse
				{
					ok = false,
					error = "交易中不能转换灵魂艾尔达"
				};
			}
			NativeHexaMatrixService.ErdaConversion erdaConversion = NativeHexaMatrixService.Convert(server.Db, r.characterId, r.itemId, r.count);
			if (erdaConversion.ok)
			{
				RefreshEnhancementMirror(r.characterId);
			}
			return new HexaConvertResponse
			{
				ok = erdaConversion.ok,
				error = erdaConversion.error,
				solErda = erdaConversion.solErda
			};
		}
	}

	public bool HasNpcShop(int npcId)
	{
		return server.NpcShops.HasNpc(npcId);
	}

	public NpcShopResponse NpcShop(NpcShopRequest r)
	{
		return server.NpcShops.Handle(r);
	}

	public DueyResponse Duey(DueyRequest r)
	{
		return NativeDueyService.Handle(server.Db, r);
	}

	public MerchantResponse Merchant(MerchantRequest r)
	{
		return NativeMerchantService.Handle(server.Db, r);
	}

	public PlayerShopResponse PlayerShop(PlayerShopRequest r)
	{
		return NativePlayerShopService.Handle(server.Db, r);
	}

	public MakerResponse Maker(MakerRequest r)
	{
		return NativeMakerService.Handle(server.Db, r);
	}

	public RankingResponse Ranking(RankingRequest r)
	{
		return NativeRankingService.Handle(server.Db, r);
	}

	public OwlResponse Owl(OwlRequest r)
	{
		return NativeOwlService.Handle(server.Db, r);
	}

	public StorageResponse Storage(StorageRequest r)
	{
		return NativeStorageService.Handle(server.Db, r);
	}

	public MacroResponse Macro(MacroRequest r)
	{
		return NativeSkillMacroService.Handle(server.Db, r);
	}

	public ReportResponse Report(ReportRequest r)
	{
		return NativeReportService.Handle(server.Db, r);
	}

	public ArenaResponse Arena(ArenaRequest r)
	{
		return NativeEventArenaService.Handle(server.Db, r);
	}

	public RpsResponse Rps(RpsRequest r)
	{
		return NativeRpsService.Handle(server.Db, r);
	}

	public MemoResponse Memo(MemoRequest r)
	{
		return NativeNoteService.Handle(server.Db, r);
	}

	public TvResponse Tv(TvRequest r)
	{
		return NativeMapleTvService.Handle(server.Db, r);
	}

	public CarnivalResponse Carnival(CarnivalRequest r)
	{
		return NativeCarnivalService.Handle(server.Db, r);
	}

	public CashResponse Cash(CashRequest r)
	{
		return server.Cash.Handle(r);
	}

	public void EndSocialSession(int characterId)
	{
		NativePetItems.EndSession(server.Db, characterId);
		NativePlayerShopService.Logout(server.Db, characterId);
		NativeReportService.EndSession(characterId);
		server.EndBotSession(characterId);
		server.Scripts.EndSession(characterId);
		server.Social.EndSession(characterId);
		server.Community.EndSession(characterId);
		server.Cash.EndSession(characterId);
		server.NpcShops.EndSession(characterId);
	}

	public LoginResponse Login(LoginRequest r)
	{
		LoginResult loginResult = LoginService.Login(server.Db, r.name, r.password);
		return new LoginResponse
		{
			code = loginResult.code,
			accountId = loginResult.accountId,
			accountName = loginResult.accountName,
			justRegistered = loginResult.justRegistered,
			banUntil = loginResult.banUntil,
			gender = loginResult.gender
		};
	}

	public SetGenderResponse SetGender(SetGenderRequest r)
	{
		int num = LoginService.SetGender(server.Db, r.accountId, r.gender);
		if (num == r.gender)
		{
			server.Db.Save();
		}
		return new SetGenderResponse
		{
			ok = (num == r.gender),
			gender = num
		};
	}

	public ServerListResponse ServerList(ServerListRequest r)
	{
		return new ServerListResponse
		{
			worlds = WorldService.Worlds(server.Db)
		};
	}

	public CharListResponse CharList(CharListRequest r)
	{
		return new CharListResponse
		{
			characterSlots = AccountsDao.CharacterSlots(server.Db, r.accountId),
			characters = CharactersDao.ByAccount(server.Db, r.accountId, r.world)
		};
	}

	public CheckCharNameResponse CheckCharName(CheckCharNameRequest r)
	{
		if (!CharactersDao.CanCreateChar(r.name))
		{
			return new CheckCharNameResponse
			{
				available = false,
				reason = -1
			};
		}
		if (CharactersDao.NameTaken(server.Db, r.name))
		{
			return new CheckCharNameResponse
			{
				available = false,
				reason = -2
			};
		}
		return new CheckCharNameResponse
		{
			available = true,
			reason = 0
		};
	}

	public CreateCharResponse CreateChar(CreateCharRequest r)
	{
		CharFactory.Result result = CharFactory.CreateCharacter(server.Db, r.accountId, r.world, r.name, r.job, r.face, r.hair, r.hairColor, r.skinColor, r.top, r.bottom, r.shoes, r.weapon, r.gender);
		if (result.code == 0)
		{
			server.Db.Save();
		}
		return new CreateCharResponse
		{
			code = result.code,
			characterId = result.characterId,
			error = result.error
		};
	}

	public DeleteCharResponse DeleteChar(DeleteCharRequest r)
	{
		CharRecord charRecord = CharactersDao.FindById(server.Db, r.characterId);
		if (charRecord == null || charRecord.accountid != r.accountId)
		{
			return new DeleteCharResponse
			{
				ok = false
			};
		}
		CharactersDao.Delete(server.Db, r.characterId);
		server.Db.Save();
		return new DeleteCharResponse
		{
			ok = true
		};
	}

	public CharSelectResponse CharSelect(CharSelectRequest r)
	{
		FullCharacter fullCharacter = CharService.Load(server.Db, r.characterId);
		if (fullCharacter == null)
		{
			return new CharSelectResponse
			{
				ok = false
			};
		}
		server.Community.BeginSession(r.characterId);
		return new CharSelectResponse
		{
			ok = true,
			character = fullCharacter
		};
	}

	public ImportLegacyResponse ImportLegacy(ImportLegacyRequest r)
	{
		return new ImportLegacyResponse
		{
			characterId = LegacyImport.Import(server.Db, r.snapshot)
		};
	}

	public PlayerDeadResponse PlayerDead(PlayerDeadRequest r)
	{
		CharRecord charRecord = CharactersDao.FindById(server.Db, r.characterId);
		if (charRecord == null)
		{
			return new PlayerDeadResponse();
		}
		int safetyCharmCount = InventoryDao.CountOf(server.Db, (int)charRecord.id, 5130000);
		DeathResult deathResult = DeathService.PlayerDead(charRecord, r.mapId, server.MapInfo, safetyCharmCount);
		if (deathResult.expLost > 0)
		{
			charRecord.exp = Math.Max(0L, charRecord.exp - deathResult.expLost);
			CharactersDao.Save(server.Db, charRecord);
		}
		if (deathResult.usedSafetyCharm)
		{
			InventoryDao.ConsumeItem(server.Db, (int)charRecord.id, 5130000, 1);
		}
		server.Db.Save();
		return new PlayerDeadResponse
		{
			expLost = deathResult.expLost,
			usedSafetyCharm = deathResult.usedSafetyCharm,
			noExpLoss = deathResult.noExpLoss
		};
	}

	public ChangeMapResponse ChangeMap(ChangeMapRequest r)
	{
		CharRecord charRecord = CharactersDao.FindById(server.Db, r.characterId);
		if (charRecord == null)
		{
			return new ChangeMapResponse
			{
				ok = false
			};
		}
		int targetMapId = r.targetMapId;
		if (r.fromDying)
		{
			if (charRecord.hp <= 0 && r.wheel && InventoryDao.CountOf(server.Db, r.characterId, 5510000) > 0)
			{
				InventoryDao.ConsumeItem(server.Db, r.characterId, 5510000, 1);
				charRecord.hp = 50;
				charRecord.spawnpoint = (server.MapInfo as IRespawnMapInfo)?.ClosestSpawn(charRecord.map, r.deathX, r.deathY) ?? 0;
				CharactersDao.Save(server.Db, charRecord);
				server.Db.Save();
				return new ChangeMapResponse
				{
					ok = true,
					mapId = charRecord.map,
					spawnPoint = charRecord.spawnpoint,
					hp = charRecord.hp,
					mp = charRecord.mp,
					respawned = true,
					wheelUsed = true,
					inventory = CharService.Load(server.Db, r.characterId).inventory
				};
			}
			targetMapId = DeathService.RespawnTargetMap(charRecord.map, server.MapInfo);
			int maxhp = charRecord.maxhp;
			int maxmp = charRecord.maxmp;
			charRecord.hp = DeathService.RespawnHp(targetMapId, r.usedSafetyCharm, maxhp);
			charRecord.mp = DeathService.RespawnMp(targetMapId, r.usedSafetyCharm, maxmp);
			charRecord.map = targetMapId;
			charRecord.spawnpoint = 0;
			CharactersDao.Save(server.Db, charRecord);
			server.Db.Save();
			return new ChangeMapResponse
			{
				ok = true,
				mapId = targetMapId,
				spawnPoint = charRecord.spawnpoint,
				hp = charRecord.hp,
				mp = charRecord.mp,
				respawned = true
			};
		}
		charRecord.map = targetMapId;
		CharactersDao.Save(server.Db, charRecord);
		server.Db.Save();
		return new ChangeMapResponse
		{
			ok = true,
			mapId = targetMapId,
			spawnPoint = charRecord.spawnpoint,
			hp = charRecord.hp,
			mp = charRecord.mp
		};
	}

	public void SaveCharacter(SaveCharacterRequest r)
	{
		if (r.character != null && r.character.record != null)
		{
			CharRecord charRecord = CharactersDao.FindById(server.Db, r.characterId);
			if (charRecord != null)
			{
				CharRecord record = r.character.record;
				record.guildid = charRecord.guildid;
				record.guildrank = charRecord.guildrank;
				record.familyId = charRecord.familyId;
				record.allianceRank = charRecord.allianceRank;
				record.party = charRecord.party;
				record.monsterbookcover = charRecord.monsterbookcover;
				BotActor botActor = server.ActiveBotPlayer(r.characterId);
				if (botActor?.character != null && (botActor.trade != null || r.character.tradeRevision < botActor.character.tradeRevision))
				{
					record.meso = charRecord.meso;
					r.character.inventory = CharService.Load(server.Db, r.characterId).inventory;
					r.character.tradeRevision = botActor.character.tradeRevision;
				}
				if (record.level > charRecord.level)
				{
					server.Community.GiveReputation(r.characterId, (record.level - charRecord.level) * GameConfigDao.GetInt(server.Db, "family_rep_per_level_up", 200));
				}
			}
			CharService.Save(server.Db, r.character);
			server.Db.Save();
		}
		else
		{
			FullCharacter fullCharacter = CharService.Load(server.Db, r.characterId);
			if (fullCharacter != null)
			{
				CharService.Save(server.Db, fullCharacter);
				server.Db.Save();
			}
		}
	}
}

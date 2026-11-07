using System;
using System.Collections.Generic;
using MapleDash.Js;
using Server.Bot;
using Server.Field;
using UnityEngine;

public static class GameSession
{
	private static readonly Dictionary<long, string[]> appearances = new Dictionary<long, string[]>();

	public static GameServer Server { get; private set; }

	public static ITransport Net { get; private set; }

	public static int AccountId { get; private set; }

	public static string AccountName { get; private set; }

	public static int World { get; private set; }

	public static int Channel { get; private set; }

	public static int CharacterId { get; private set; }

	public static FullCharacter Character { get; private set; }

	public static bool InGame { get; private set; }

	public static int AccountGender { get; private set; } = 10;

	public static void Boot()
	{
		if (Server != null)
		{
			return;
		}
		Server = GameServer.Start();
		Server.Social.Commands.Host = NativeGmCommandHost.Execute;
		Server.MapInfo = new FieldMapInfo();
		Server.Community.CanFamilyTravel = NativeCommunityMaps.CanTravel;
		Server.Cash.CanEnter = NativeCommunityMaps.CanEnterCash;
		Net = Server.Transport;
		Server.TradeInventoryCommitted += ApplyTradeInventory;
		MapReactorService.ScriptSource = ScriptSource.LoadKey;
		MapReactorService.ScriptMessage = (int map, int type, string text) =>
		{
			GameLoop i = GameLoop.I;
			if (i != null && i.field?.StateId == map)
			{
				NativeSystemMessage.Drop(type, text);
			}
		};
		MapReactorService.EventInstance = (int map) =>
		{
			GameLoop i = GameLoop.I;
			return (i == null || i.field?.StateId != map) ? JsNull.Value : NativeEventScripts.Instance();
		};
		MapReactorService.Player = (int owner) => NpcApi.BuildPlayer(NpcSession.MapContext());
		Server.Scripts.EnterMTS = EnterMtsFromMenu;
		Expedition.LocalPlayerId = () => CharacterId;
		Expedition.Notice = (int type, string text) =>
		{
			NativeSystemMessage.Drop(type, text);
		};
		Expedition.DropMessage = (int type, string text) =>
		{
			NativeSystemMessage.Drop(type, text);
		};
		Expedition.ShowClock = (int seconds) =>
		{
			NativeEventClock.Show(seconds);
		};
		Expedition.HideClock = NativeEventClock.Hide;
		Expedition.ChangeMap = (int mapId, int portal) =>
		{
			NpcSession.MapContext().RequestWarp(mapId, null, portal);
		};
		Expedition.ConfigBool = (string name) => Server != null && GameConfigDao.GetBool(Server.Db, name);
		Expedition.Log = (string text) =>
		{
			Debug.Log((object)("[Expedition] " + text));
		};
		MapReactorService.ScriptError = (Exception error) =>
		{
			Debug.LogError((object)("[ReactorScript] " + error.Message));
		};
		MapReactorService.MapHost = (int map) =>
		{
			NativeEventMap nativeEventMap = NativeEventScripts.ResolveMap(map);
			return (nativeEventMap == null) ? ((JsValue)JsNull.Value) : ((JsValue)nativeEventMap.Host);
		};
		MapReactorService.SpawnMonsterAt = (int map, int mob, float x, float y, int qty) =>
		{
			NativeEventMap nativeEventMap = NativeEventScripts.ResolveMap(map);
			if (nativeEventMap == null)
			{
				return false;
			}
			for (int i = 0; i < qty; i++)
			{
				nativeEventMap.SpawnAt(mob, x, y, ground: true);
			}
			return true;
		};
		NativeReactorDrops.NeedsQuestItem = (int owner, int quest, int item) => owner != CharacterId || QuestLogic.NeedQuestItem(quest, item);
		NativeReactorDrops.Rates = (int owner) =>
		{
			CommunityResponse communityResponse = Net.Community(new CommunityRequest
			{
				characterId = owner,
				operation = CommunityOperation.Read
			});
			Dictionary<string, int> bonus = ((owner == CharacterId) ? EquipLoadout.CurrentSetBonus() : null);
			return (drop: AdminSetItem.ApplyRate(bonus, "DropRate", communityResponse.dropRate), meso: AdminSetItem.ApplyRate(bonus, "MesoRate", communityResponse.mesoRate));
		};
		BotTypeManager.GrantTutorialAdmin = GrantTutorialAdmin;
		BotTypeManager.ApplySkillEffect = ApplyBotSkillEffect;
		NativeStorageService.FeeProvider = () =>
		{
			TextAsset val = Resources.Load<TextAsset>("Storage/TrunkFees");
			return (val == null) ? null : val.text;
		};
		MigrateLegacyProfile();
	}

	public static void MigrateLegacyProfile()
	{
		Profile.Load();
		PlayerProfileData data = Profile.Data;
		if (data.level <= 1 && data.gold <= 0 && data.experience <= 0 && (data.questCompleted == null || data.questCompleted.Count <= 0))
		{
			return;
		}
		LegacyProfileSnapshot legacyProfileSnapshot = new LegacyProfileSnapshot
		{
			name = ((data.playerName != "") ? data.playerName : "Migrated"),
			level = data.level,
			exp = data.experience,
			str = data.attributes.str,
			dex = data.attributes.dex,
			int_ = data.attributes.intelligence,
			luk = data.attributes.luk,
			maxHp = Mathf.Max(50, data.baseMaxHp),
			maxMp = Mathf.Max(5, data.baseMaxMp),
			job = data.jobId,
			map = Profile.LastMapId,
			ap = data.ap,
			sp = data.sp,
			meso = data.gold,
			equipSlots = data.equipmentSlots
		};
		foreach (KeyValuePair<int, int> item in SkillProgress.AllLearned())
		{
			legacyProfileSnapshot.skills.Add(new LegacySkillEntry
			{
				skillId = item.Key,
				skillLevel = item.Value
			});
		}
		ImportLegacyResponse importLegacyResponse = Net.ImportLegacy(new ImportLegacyRequest
		{
			snapshot = legacyProfileSnapshot
		});
		if (importLegacyResponse != null && importLegacyResponse.characterId > 0)
		{
			Debug.Log((object)("[GameSession] 旧存档已迁移到 DB：角色 id=" + importLegacyResponse.characterId));
		}
	}

	public static LoginResponse Login(string name, string password)
	{
		LoginResponse loginResponse = Net.Login(new LoginRequest
		{
			name = name,
			password = password
		});
		if (loginResponse != null && loginResponse.code == LoginResultCode.Ok)
		{
			AccountGender = loginResponse.gender;
		}
		return loginResponse;
	}

	public static bool SetGender(int accountId, int gender)
	{
		if (accountId <= 0)
		{
			return false;
		}
		SetGenderResponse setGenderResponse = Net.SetGender(new SetGenderRequest
		{
			accountId = accountId,
			gender = gender
		});
		if (setGenderResponse != null && setGenderResponse.ok)
		{
			AccountGender = setGenderResponse.gender;
		}
		return setGenderResponse?.ok ?? false;
	}

	public static bool SetGender(int gender)
	{
		return SetGender(AccountId, gender);
	}

	public static ServerListResponse ServerList()
	{
		return Net.ServerList(new ServerListRequest
		{
			accountId = AccountId
		});
	}

	public static CharListResponse CharList(int world, int channel)
	{
		CharListResponse charListResponse = Net.CharList(new CharListRequest
		{
			accountId = AccountId,
			world = world,
			channel = channel
		});
		appearances.Clear();
		foreach (CharRecord character in charListResponse.characters)
		{
			CharSelectResponse charSelectResponse = Net.CharSelect(new CharSelectRequest
			{
				accountId = AccountId,
				characterId = (int)character.id
			});
			if (charSelectResponse.ok && charSelectResponse.character != null)
			{
				appearances[character.id] = WzCharacter.CharacterIds(character, charSelectResponse.character.inventory);
			}
		}
		return charListResponse;
	}

	public static string[] PreviewAppearance(CharRecord rec)
	{
		return appearances[rec.id];
	}

	public static CheckCharNameResponse CheckCharName(string name)
	{
		return Net.CheckCharName(new CheckCharNameRequest
		{
			name = name
		});
	}

	public static bool NameFormatOk(string name)
	{
		return CharNameRules.Valid(name);
	}

	public static CreateCharResponse CreateChar(CreateCharRequest req)
	{
		req.accountId = AccountId;
		req.world = World;
		return Net.CreateChar(req);
	}

	public static bool DeleteChar(int characterId)
	{
		return Net.DeleteChar(new DeleteCharRequest
		{
			accountId = AccountId,
			characterId = characterId
		}).ok;
	}

	public static PlayerDeadResponse PlayerDead(int mapId)
	{
		if (!InGame)
		{
			return new PlayerDeadResponse();
		}
		PlayerDeadResponse playerDeadResponse = Net.PlayerDead(new PlayerDeadRequest
		{
			characterId = CharacterId,
			mapId = mapId
		});
		if (playerDeadResponse != null && playerDeadResponse.expLost > 0)
		{
			PlayerProfileData data = Profile.Data;
			data.experience = Math.Max(0L, data.experience - playerDeadResponse.expLost);
			Profile.Save();
		}
		return playerDeadResponse ?? new PlayerDeadResponse();
	}

	public static int PeekRespawnMap(int mapId)
	{
		if (Server == null || Server.MapInfo == null)
		{
			return -1;
		}
		return DeathService.RespawnTargetMap(mapId, Server.MapInfo);
	}

	public static ChangeMapResponse Respawn(bool usedSafetyCharm, bool wheel = false, int deathX = 0, int deathY = 0)
	{
		if (!InGame)
		{
			return new ChangeMapResponse
			{
				ok = false
			};
		}
		ChangeMapResponse changeMapResponse = Net.ChangeMap(new ChangeMapRequest
		{
			characterId = CharacterId,
			fromDying = true,
			usedSafetyCharm = usedSafetyCharm,
			wheel = wheel,
			deathX = deathX,
			deathY = deathY
		});
		if (changeMapResponse != null && changeMapResponse.ok && Character != null)
		{
			if (changeMapResponse.inventory != null)
			{
				Character.inventory = changeMapResponse.inventory;
				MarkStatsDirty();
				ApplyInventoryToProfile();
			}
			Character.record.map = changeMapResponse.mapId;
			Character.record.hp = changeMapResponse.hp;
			Character.record.mp = changeMapResponse.mp;
		}
		return changeMapResponse ?? new ChangeMapResponse
		{
			ok = false
		};
	}

	public static void BeginSession(int accountId, string accountName, int world, int channel)
	{
		AccountId = accountId;
		AccountName = accountName;
		World = world;
		Channel = channel;
	}

	public static bool SelectChar(int characterId)
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		CharSelectResponse charSelectResponse = Net.CharSelect(new CharSelectRequest
		{
			accountId = AccountId,
			characterId = characterId
		});
		if (!charSelectResponse.ok || charSelectResponse.character == null)
		{
			return false;
		}
		CharacterId = characterId;
		Character = charSelectResponse.character;
		BottomBar.ResetChat();
		BottomBar.AddChat("[欢迎] 欢迎光临冒险岛ONLINE世界。", BottomBar.ChatYellow);
		NativeSocialWindow.Reset();
		NativeTradeWindow.Reset();
		NativeCharacterMenu.Close();
		NativeCharacterInfo.Close();
		NativeMarketWindow.Reset();
		NativeUtilPrompt.Clear();
		NativeCommunityWindow.Reset();
		NativeMedalWindow.Visible = false;
		NativeCashWindow.Reset();
		NativeVegaWindow.Close();
		NativeEnhancementWindow.Close();
		NativeNpcShopWindow.Reset();
		NativeDeliveryWindow.Reset();
		NativeMerchantWindow.Reset();
		NativePlayerShopWindow.Reset();
		NativeItemSearchWindow.Reset();
		NativeStorageWindow.Reset();
		NativeSkillMacros.Reset();
		NativeClaimWindow.Reset();
		NativeArenaHud.Reset();
		NativeRpsWindow.Reset();
		NativeRpsService.ClearSessions();
		NativeMemoWindow.Reset();
		NativeNoteService.AvailableNotes = 0;
		NativeMapleTvDisplay.Reset();
		NativeCarnivalHud.Reset();
		NativeCarnivalService.AbandonOnLogout();
		NativeCarnivalService.FestivalPoints = 0;
		NativeCarnivalLobby.Reset();
		NativeRemoteGachaponWindow.Reset();
		NativeMakerWindow.Reset();
		NativeRankingWindow.Reset();
		NativeMapWindows.WorldVisible = false;
		NativeMonsterBook.Visible = false;
		InGame = true;
		RefreshMatrixLevels();
		if (Net != null)
		{
			MemoNote[] array = Net.Memo(new MemoRequest
			{
				characterId = CharacterId,
				action = 2
			})?.notes;
			if (array != null && array.Length > 0)
			{
				NativeMemoWindow.Open();
			}
		}
		ApplyToProfile();
		NativeNameplate.RefreshFromServer();
		KeyMap.LoadCharacter(Character.keymap);
		return true;
	}

	private static void EnterMtsFromMenu()
	{
		if (!(NativeEventScripts.Instance() is JsNull))
		{
			NativeSystemMessage.Drop(5, "Entering Cash Shop or MTS are disabled when registered on an event.");
			return;
		}
		PlayerCtrl playerCtrl = GameLoop.I?.player;
		if (!((Object)(object)playerCtrl == (Object)null) && !(playerCtrl.hp <= 0f))
		{
			FieldMap map = FieldCatalog.GetMap(Profile.LastMapId);
			if (map != null && (map.fieldLimit & 0x10) != 0)
			{
				NativeSystemMessage.Drop(1, "You can't do it here in this map.");
			}
			else if (Profile.level >= 10)
			{
				throw new JsRuntimeException("Unmigrated MTS: ITC window and trading service");
			}
		}
	}

	private static void RefreshMatrixLevels()
	{
		if (Net == null || CharacterId <= 0)
		{
			return;
		}
		try
		{
			VMatrixResponse vMatrixResponse = Net.VMatrix(new VMatrixRequest
			{
				characterId = CharacterId,
				operation = VMatrixOperation.Snapshot
			});
			if (vMatrixResponse != null)
			{
				NativeSpecialCore.Observe(vMatrixResponse);
				VMatrixLevels.Capture(vMatrixResponse, CharacterId);
			}
			HexaResponse hexaResponse = Net.Hexa(new HexaRequest
			{
				characterId = CharacterId,
				operation = HexaOperation.Snapshot
			});
			if (hexaResponse != null)
			{
				VMatrixLevels.CaptureHexa(hexaResponse, CharacterId);
			}
			if (PlayerPrefs.GetInt("debug_hitlog", 0) != 0)
			{
				Debug.Log((object)("[Matrix] 登录预热：V 等级 " + VMatrixLevels.Count(hexa: false) + " 条 / HEXA 等级 " + VMatrixLevels.Count(hexa: true) + " 条"));
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning((object)("[Matrix] 登录预热失败：" + ex.Message));
		}
	}

	public static void DeselectChar()
	{
		if (InGame)
		{
			SaveToDb();
		}
		NativeCashWindow.Reset();
		NativeVegaWindow.Close();
		NativeEnhancementWindow.Close();
		NativeNpcShopWindow.Reset();
		NativeDeliveryWindow.Reset();
		NativeMerchantWindow.Reset();
		NativePlayerShopWindow.Reset();
		NativeItemSearchWindow.Reset();
		NativeStorageWindow.Reset();
		NativeSkillMacros.Reset();
		NativeClaimWindow.Reset();
		NativeArenaHud.Reset();
		NativeRpsWindow.Reset();
		NativeRpsService.ClearSessions();
		NativeMemoWindow.Reset();
		NativeNoteService.AvailableNotes = 0;
		NativeMapleTvDisplay.Reset();
		NativeCarnivalHud.Reset();
		NativeCarnivalService.AbandonOnLogout();
		NativeCarnivalService.FestivalPoints = 0;
		NativeCarnivalLobby.Reset();
		NativeRemoteGachaponWindow.Reset();
		NativeMakerWindow.Reset();
		NativeRankingWindow.Reset();
		VMatrixLevels.Clear();
		if (InGame)
		{
			NativeEventScripts.PlayerDisconnected();
		}
		Net?.EndSocialSession(CharacterId);
		InGame = false;
		Character = null;
		CharacterId = 0;
	}

	public static void Logout()
	{
		if (InGame)
		{
			SaveToDb();
		}
		NativeCashWindow.Reset();
		NativeVegaWindow.Close();
		NativeEnhancementWindow.Close();
		NativeNpcShopWindow.Reset();
		NativeDeliveryWindow.Reset();
		NativeMerchantWindow.Reset();
		NativePlayerShopWindow.Reset();
		NativeItemSearchWindow.Reset();
		NativeStorageWindow.Reset();
		NativeSkillMacros.Reset();
		NativeClaimWindow.Reset();
		NativeArenaHud.Reset();
		NativeRpsWindow.Reset();
		NativeRpsService.ClearSessions();
		NativeMemoWindow.Reset();
		NativeNoteService.AvailableNotes = 0;
		NativeMapleTvDisplay.Reset();
		NativeCarnivalHud.Reset();
		NativeCarnivalService.AbandonOnLogout();
		NativeCarnivalService.FestivalPoints = 0;
		NativeCarnivalLobby.Reset();
		NativeRemoteGachaponWindow.Reset();
		NativeMakerWindow.Reset();
		NativeRankingWindow.Reset();
		VMatrixLevels.Clear();
		if (InGame)
		{
			NativeEventScripts.PlayerDisconnected();
		}
		Net?.EndSocialSession(CharacterId);
		InGame = false;
		Character = null;
		CharacterId = 0;
		if (Server != null && AccountId > 0)
		{
			LoginService.Logout(Server.Db, AccountId);
		}
		AccountId = 0;
		AccountName = "";
	}

	public static void MarkStatsDirty()
	{
		if (Character != null)
		{
			NativeGrowthStats.Invalidate();
		}
	}

	public static void ReloadAfterCommand()
	{
		FullCharacter fullCharacter = CharService.Load(Server.Db, CharacterId);
		if (fullCharacter == null)
		{
			return;
		}
		Character.record = fullCharacter.record;
		Character.inventory = fullCharacter.inventory;
		Character.skills = fullCharacter.skills;
		Character.quests = fullCharacter.quests;
		Character.keymap = fullCharacter.keymap;
		Character.quickslotKeymap = fullCharacter.quickslotKeymap;
		Character.growth = fullCharacter.growth;
		Character.hyperstats = fullCharacter.hyperstats;
		Character.potentials = fullCharacter.potentials;
		Character.hexaStats = fullCharacter.hexaStats;
		NativeArrowMode.Sync();
		MarkStatsDirty();
		ApplyToProfile();
		PlayerCtrl playerCtrl = GameLoop.I?.player;
		if ((Object)(object)playerCtrl != (Object)null)
		{
			playerCtrl.ApplyProgression();
			playerCtrl.hp = Mathf.Clamp((float)fullCharacter.record.hp, 0f, playerCtrl.hpMax);
			playerCtrl.mp = Mathf.Clamp((float)fullCharacter.record.mp, 0f, playerCtrl.mpMax);
			playerCtrl.RefreshFieldAppearance();
			if (playerCtrl.hp <= 0f && !GameLoop.I.gameOver)
			{
				GameLoop.I.OnPlayerDead();
			}
		}
	}

	public static void ApplyToProfile()
	{
		using (Profile.DeferSaves())
		{
			PlayerProfileData data = Profile.Data;
			CharRecord record = Character.record;
			data.playerName = record.name;
			data.level = record.level;
			data.experience = record.exp;
			data.attributes.str = record.str;
			data.attributes.dex = record.dex;
			data.attributes.intelligence = record.int_;
			data.attributes.luk = record.luk;
			data.baseMaxHp = record.maxhp;
			data.baseMaxMp = record.maxmp;
			data.attributes.maxHp = record.maxhp;
			data.attributes.maxMp = record.maxmp;
			data.ap = record.ap;
			data.SetJob(record.job);
			data.sp = record.GetSp(JobDefs.SkillBookOf(record.job));
			data.gold = record.meso;
			data.equipmentSlots = record.equipslots;
			data.hair = HairFromId(record.hair);
			Profile.LastMapId = record.map;
			ApplyInventoryToProfile();
			data.questStarted.Clear();
			data.questCompleted.Clear();
			data.questCompletedAt.Clear();
			foreach (QuestEntry quest in Character.quests)
			{
				if (quest.completed > 0 || quest.status == 2)
				{
					data.SetQuestCompleted(quest.quest);
				}
				else if (quest.status == 1)
				{
					data.SetQuestStarted(quest.quest);
				}
			}
			SkillProgress.ClearAll();
			foreach (SkillEntry skill in Character.skills)
			{
				if (skill.skillLevel > 0)
				{
					SkillProgress.SetLearned(skill.skillId, skill.skillLevel);
				}
			}
			Profile.Save();
		}
	}

	public static void ApplyInventoryToProfile()
	{
		PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
		string text = (((Object)(object)playerCtrl != (Object)null) ? WzCharacter.CurrentAppearanceKey() : null);
		PlayerProfileData data = Profile.Data;
		int[] worn = EquipSlot.Worn;
		foreach (int slot in worn)
		{
			data.SetEquipSlot(slot, 0);
		}
		worn = NativeAndroidService.ApSlots;
		foreach (int slot2 in worn)
		{
			data.SetEquipSlot(slot2, 0);
		}
		data.owned.Clear();
		data.itemIds.Clear();
		data.itemCounts.Clear();
		List<InventoryEntry> list = new List<InventoryEntry>(Character.inventory);
		list.Sort((InventoryEntry a, InventoryEntry b) =>
		{
			int num = a.inventoryType.CompareTo(b.inventoryType);
			return (num == 0) ? a.position.CompareTo(b.position) : num;
		});
		foreach (InventoryEntry item in list)
		{
			if (item.inventoryType == 1 && (item.position < 0 || NativeAndroidService.IsApSlot(item.position)))
			{
				int position = item.position;
				if (position == -11 || IsEquipSlot(position) || NativeAndroidService.IsApSlot(position))
				{
					data.SetEquipSlot(position, item.itemId);
				}
				if (position == -11)
				{
					data.owned.Add(item.itemId.ToString());
				}
			}
			else if (EquipSlot.SlotOf(item.itemId) == -11)
			{
				data.owned.Add(item.itemId.ToString());
			}
			else
			{
				data.AddItem(item.itemId, item.quantity);
			}
		}
		NativeEquipmentEffects.EquipmentChanged();
		NativeCharacterInfo.RefreshSelf();
		if ((Object)(object)playerCtrl != (Object)null && text != WzCharacter.CurrentAppearanceKey())
		{
			playerCtrl.RefreshFieldAppearance();
		}
	}

	public static void ApplyGroundPickup(BotActor actor, long mesoDelta)
	{
		ApplyTradeInventory(actor, mesoDelta);
	}

	private static void ApplyTradeInventory(BotActor actor, long mesoDelta)
	{
		if (!InGame || Character == null || actor.id != CharacterId)
		{
			return;
		}
		Profile.Data.gold = checked(Profile.Data.gold + mesoDelta);
		List<InventoryEntry> list = new List<InventoryEntry>();
		foreach (InventoryEntry item in actor.character.inventory)
		{
			list.Add(item.Copy());
		}
		Character.inventory = list;
		Character.tradeRevision = actor.character.tradeRevision;
		MarkStatsDirty();
		ApplyInventoryToProfile();
		Profile.Save();
	}

	private static bool GrantTutorialAdmin(BotActor actor)
	{
		if (!InGame || actor.id != CharacterId || Character == null)
		{
			return false;
		}
		PlayerProfileData data = Profile.Data;
		data.level = 149;
		LevelSystem.SetLevel(data, 150);
		if (!JobSystem.ChangeJobById(412))
		{
			return false;
		}
		Character.record.gm = 6;
		data.ap += 750;
		data.sp += 450;
		data.baseMaxHp = 10000;
		data.baseMaxMp = 10000;
		data.attributes.maxHp = 10000;
		data.attributes.maxMp = 10000;
		PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
		playerCtrl?.ApplyProgression();
		if ((Object)(object)playerCtrl != (Object)null)
		{
			playerCtrl.hp = 10000f;
			playerCtrl.mp = 10000f;
		}
		actor.level = 150;
		actor.job = 412;
		Profile.Save();
		SaveToDb();
		return true;
	}

	private static void ApplyBotSkillEffect(BotActor actor, int skillId)
	{
		if (InGame && actor.id == CharacterId)
		{
			PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
			if ((Object)(object)playerCtrl != (Object)null)
			{
				NativeBuffs.For(playerCtrl).ApplySkill(skillId, SkillBook.MaxLevelOf(skillId), NativeBuffs.Now);
			}
		}
	}

	public static void SaveToDb()
	{
		if (!InGame || Character == null)
		{
			return;
		}
		PlayerProfileData data = Profile.Data;
		CharRecord record = Character.record;
		record.level = data.level;
		record.exp = data.experience;
		record.str = data.attributes.str;
		record.dex = data.attributes.dex;
		record.int_ = data.attributes.intelligence;
		record.luk = data.attributes.luk;
		record.maxhp = data.baseMaxHp;
		record.maxmp = data.baseMaxMp;
		PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
		record.hp = (((Object)(object)playerCtrl != (Object)null) ? Mathf.Clamp(Mathf.RoundToInt(playerCtrl.hp), 0, data.baseMaxHp) : data.baseMaxHp);
		record.mp = (((Object)(object)playerCtrl != (Object)null) ? Mathf.Clamp(Mathf.RoundToInt(playerCtrl.mp), 0, data.baseMaxMp) : data.baseMaxMp);
		record.ap = data.ap;
		record.job = data.jobId;
		record.SetSp(JobDefs.SkillBookOf(data.jobId), data.sp);
		record.meso = data.gold;
		record.map = Profile.LastMapId;
		record.equipslots = data.equipmentSlots;
		List<SkillEntry> skills = Character.skills;
		Character.skills = new List<SkillEntry>();
		foreach (KeyValuePair<int, int> kv in SkillProgress.AllLearned())
		{
			Character.skills.Add(new SkillEntry
			{
				skillId = kv.Key,
				skillLevel = kv.Value,
				masterLevel = (skills.Find((SkillEntry s) => s.skillId == kv.Key)?.masterLevel ?? SkillBook.Get(kv.Key)?.masterLevel ?? 0),
				expiration = (skills.Find((SkillEntry s) => s.skillId == kv.Key)?.expiration ?? (-1))
			});
		}
		foreach (SkillEntry skill in skills)
		{
			if (skill.masterLevel > 0 && !Character.skills.Exists((SkillEntry s) => s.skillId == skill.skillId))
			{
				Character.skills.Add(new SkillEntry
				{
					skillId = skill.skillId,
					skillLevel = 0,
					masterLevel = skill.masterLevel,
					expiration = skill.expiration
				});
			}
		}
		Character.quests.Clear();
		foreach (int item in data.questCompleted)
		{
			Character.quests.Add(new QuestEntry
			{
				quest = item,
				status = 2,
				completed = 1
			});
		}
		foreach (int item2 in data.questStarted)
		{
			if (!data.questCompleted.Contains(item2))
			{
				Character.quests.Add(new QuestEntry
				{
					quest = item2,
					status = 1,
					completed = 0
				});
			}
		}
		Character.keymap = KeyMap.ExportCharacter();
		Net.SaveCharacter(new SaveCharacterRequest
		{
			characterId = CharacterId,
			character = Character
		});
	}

	private static bool IsEquipSlot(int pos)
	{
		return Array.IndexOf(EquipSlot.Worn, pos) >= 0;
	}

	private static string HairFromId(int id)
	{
		int num = id / 10 * 10;
		_ = id % 10;
		return "h" + num % 100;
	}
}

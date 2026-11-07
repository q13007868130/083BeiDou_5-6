using System;

public static class BotConfig
{
	public static bool AutoEnvironmentEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_auto_environment", def: false);
	}

	public static int EnvironmentBotMax(DbStore db)
	{
		return GetInt(db, "solo_mapling_environment_bot_max", 30);
	}

	public static int MarketBotMax(DbStore db)
	{
		return GetInt(db, "solo_mapling_market_bot_max", 10);
	}

	public static int MarketShopMax(DbStore db)
	{
		return GetInt(db, "solo_mapling_market_shop_max", 10);
	}

	public static int AppearanceTemplateCount(DbStore db)
	{
		return GetInt(db, "solo_mapling_appearance_templates", 32);
	}

	public static int DirectorySize(DbStore db)
	{
		if (int.TryParse((db == null) ? "" : GameConfigDao.Get(db, "solo_mapling_directory_size"), out var result))
		{
			return Math.Max(0, result);
		}
		return 300;
	}

	public static bool DirectoryCoverageEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_directory_coverage", def: true);
	}

	public static int DirectoryMaxLevel(DbStore db)
	{
		return Math.Max(10, Math.Min(200, GetInt(db, "solo_mapling_directory_max_level", 200)));
	}

	public static bool AutoMapBotsEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_auto_map_bots_enabled", def: true);
	}

	public static int AutoMapBotsMin(DbStore db)
	{
		return GetInt(db, "solo_mapling_auto_map_bots_min", 2);
	}

	public static int AutoMapBotsMax(DbStore db)
	{
		return GetInt(db, "solo_mapling_auto_map_bots_max", 4);
	}

	public static int AutoMapBotsRadius(DbStore db)
	{
		return GetInt(db, "solo_mapling_auto_map_bots_radius", 350);
	}

	public static bool AutoMapBotsRandomPositionEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_auto_map_bots_random_position", def: true);
	}

	public static bool AmbientBehaviorEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_auto_map_bots_ambient_enabled", def: true);
	}

	public static bool AmbientMoveEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_auto_map_bots_move_enabled", def: true);
	}

	public static bool AmbientChatEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_auto_map_bots_chat_enabled", def: true);
	}

	public static bool AmbientEmoteEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_auto_map_bots_emote_enabled", def: true);
	}

	public static bool AmbientFacePlayerEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_auto_map_bots_face_player_enabled", def: true);
	}

	public static bool AmbientHasAnyActionEnabled(DbStore db)
	{
		if (!AmbientMoveEnabled(db) && !AmbientChatEnabled(db) && !AmbientEmoteEnabled(db))
		{
			return AmbientFacePlayerEnabled(db);
		}
		return true;
	}

	public static long AmbientActionMinMs(DbStore db)
	{
		return GetInt(db, "solo_mapling_auto_map_bots_action_min_ms", 2000);
	}

	public static long AmbientActionMaxMs(DbStore db)
	{
		long val = AmbientActionMinMs(db);
		long val2 = GetInt(db, "solo_mapling_auto_map_bots_action_max_ms", 5000);
		return Math.Max(val, val2);
	}

	public static bool RandomChineseNameEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_bot_random_chinese_name", def: true);
	}

	public static bool RandomSkillEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_bot_random_skill_enabled", def: true);
	}

	public static bool RandomChairEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_bot_random_chair_enabled", def: true);
	}

	public static bool RandomBodyEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_bot_random_body_enabled", def: true);
	}

	public static bool NormalEquipsEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_bot_normal_equips_enabled", def: true);
	}

	public static bool FullEquipsEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_bot_full_equips_enabled", def: true);
	}

	public static bool NxEquipsEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_bot_nx_equips_enabled", def: true);
	}

	public static bool AnyOutfitFeatureEnabled(DbStore db)
	{
		if (!NormalEquipsEnabled(db))
		{
			return NxEquipsEnabled(db);
		}
		return true;
	}

	public static bool DeferredDecorationEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_bot_deferred_decoration_enabled", def: false);
	}

	public static bool HenesysBotsChangeMapsEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_henesys_bots_change_maps", def: false);
	}

	public static bool FmBotsEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_feature_fm_bots_enabled", def: true);
	}

	public static bool FmMerchantsEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_feature_fm_merchants_enabled", def: true);
	}

	public static bool FmRegionFillEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_feature_fm_region_fill_enabled", def: true);
	}

	public static bool HenesysCrowdEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_feature_henesys_crowd_enabled", def: true);
	}

	public static bool HenesysMarketCrowdEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_feature_henesys_market_crowd_enabled", def: true);
	}

	public static bool HenesysParkCrowdEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_feature_henesys_park_crowd_enabled", def: true);
	}

	public static bool HenesysPotionShopCrowdEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_feature_henesys_potion_shop_crowd_enabled", def: true);
	}

	public static bool HenesysGameZoneCrowdEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_feature_henesys_game_zone_crowd_enabled", def: true);
	}

	public static bool GachaBotsEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_feature_gacha_bots_enabled", def: true);
	}

	public static bool OpqLobbyBotsEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_feature_opq_lobby_bots_enabled", def: true);
	}

	public static bool PqLobbyBotsEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_feature_pq_lobby_bots_enabled", def: true);
	}

	public static int PqLobbyBotCount(DbStore db)
	{
		return Math.Max(3, Math.Min(15, GetInt(db, "solo_mapling_pq_lobby_bot_count", 12)));
	}

	public static bool BlackjackTablesEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_feature_blackjack_tables_enabled", def: true);
	}

	public static bool CasinoNpcEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_feature_casino_npc_enabled", def: true);
	}

	public static bool ConversationEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_feature_conversation_enabled", def: true);
	}

	public static bool HotPotatoEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_feature_hot_potato_enabled", def: true);
	}

	public static bool TutorialBotEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_feature_tutorial_bot_enabled", def: true);
	}

	public static bool PetParkJqBotsEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_feature_pet_park_jq_bots_enabled", def: true);
	}

	public static bool PetParkSocialBotsEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_feature_pet_park_social_bots_enabled", def: true);
	}

	public static bool GameZoneHostBotsEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_feature_game_zone_host_bots_enabled", def: true);
	}

	public static bool PotionShopDropGameEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_feature_potion_shop_drop_game_enabled", def: true);
	}

	public static bool ScrollBotsEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_feature_scroll_bots_enabled", def: true);
	}

	public static bool MarketWanderEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_market_wander_enabled", def: true);
	}

	public static bool MarketHawkEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_market_hawk_enabled", def: true);
	}

	public static bool MarketBrowseShopsEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_market_browse_shops_enabled", def: true);
	}

	public static bool MarketIdleWhenEmpty(DbStore db)
	{
		return GetBool(db, "solo_mapling_market_idle_when_empty", def: true);
	}

	public static long MarketTickMs(DbStore db)
	{
		return Math.Max(150, GetInt(db, "solo_mapling_market_tick_ms", 250));
	}

	public static long MarketSpeechGapMs(DbStore db)
	{
		return Math.Max(250, GetInt(db, "solo_mapling_market_speech_gap_ms", 500));
	}

	public static int MarketPathfindPerTick(DbStore db)
	{
		return GetInt(db, "solo_mapling_market_pathfind_per_tick", 3);
	}

	public static bool MarketAutoStartEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_market_auto_start", def: true);
	}

	public static bool PartyCompanionEnabled(DbStore db)
	{
		return GetBool(db, "solo_mapling_party_companion_enabled", def: true);
	}

	public static bool PartyCompanionRequireParty(DbStore db)
	{
		return GetBool(db, "solo_mapling_party_companion_require_party", def: true);
	}

	public static int PartyCompanionCount(DbStore db)
	{
		return Math.Max(1, Math.Min(5, GetInt(db, "solo_mapling_party_companion_count", 5)));
	}

	public static int PartyCompanionMinutes(DbStore db)
	{
		return Math.Max(1, Math.Min(720, GetInt(db, "solo_mapling_party_companion_minutes", 30)));
	}

	public static int PartyCompanionLevelJitter(DbStore db)
	{
		if (!int.TryParse((db == null) ? "" : GameConfigDao.Get(db, "solo_mapling_party_companion_level_jitter"), out var result))
		{
			return 3;
		}
		return Math.Max(0, Math.Min(10, result));
	}

	public static long PartyCompanionDurationMs(DbStore db)
	{
		return (long)PartyCompanionMinutes(db) * 60000L;
	}

	private static bool GetBool(DbStore db, string key, bool def)
	{
		string text = ((db == null) ? "" : GameConfigDao.Get(db, key));
		if (!string.IsNullOrEmpty(text))
		{
			return text.Equals("true", StringComparison.OrdinalIgnoreCase);
		}
		return def;
	}

	private static int GetInt(DbStore db, string key, int def)
	{
		int num = ((db != null) ? GameConfigDao.GetInt(db, key) : 0);
		if (num <= 0)
		{
			return def;
		}
		return num;
	}
}

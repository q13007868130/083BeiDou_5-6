using Server.Bot;
using Server.Bot.Trade;

public interface ITransport
{
	LoginResponse Login(LoginRequest req);

	SetGenderResponse SetGender(SetGenderRequest req);

	ServerListResponse ServerList(ServerListRequest req);

	CharListResponse CharList(CharListRequest req);

	CheckCharNameResponse CheckCharName(CheckCharNameRequest req);

	CreateCharResponse CreateChar(CreateCharRequest req);

	DeleteCharResponse DeleteChar(DeleteCharRequest req);

	CharSelectResponse CharSelect(CharSelectRequest req);

	ImportLegacyResponse ImportLegacy(ImportLegacyRequest req);

	PlayerDeadResponse PlayerDead(PlayerDeadRequest req);

	ChangeMapResponse ChangeMap(ChangeMapRequest req);

	void SaveCharacter(SaveCharacterRequest req);

	ScriptResponse Script(ScriptRequest req);

	ScriptValueResponse ScriptValue(ScriptValueRequest req);

	SocialResponse Social(SocialRequest req);

	CommunityResponse Community(CommunityRequest req);

	BossResponse Boss(BossRequest req);

	CashResponse Cash(CashRequest req);

	bool HasNpcShop(int npcId);

	NpcShopResponse NpcShop(NpcShopRequest req);

	DueyResponse Duey(DueyRequest req);

	MerchantResponse Merchant(MerchantRequest req);

	PlayerShopResponse PlayerShop(PlayerShopRequest req);

	MakerResponse Maker(MakerRequest req);

	RankingResponse Ranking(RankingRequest req);

	OwlResponse Owl(OwlRequest req);

	StorageResponse Storage(StorageRequest req);

	MacroResponse Macro(MacroRequest req);

	ReportResponse Report(ReportRequest req);

	ArenaResponse Arena(ArenaRequest req);

	RpsResponse Rps(RpsRequest req);

	MemoResponse Memo(MemoRequest req);

	TvResponse Tv(TvRequest req);

	CarnivalResponse Carnival(CarnivalRequest req);

	PartyGrindResponse PartyGrind(PartyGrindRequest req);

	BotTradeResponse BotTrade(BotTradeRequest req);

	MarketResponse Market(MarketRequest req);

	InventoryChangeResponse ChangeInventory(InventoryChangeRequest req);

	EquipPatchResponse UpdateEquip(EquipPatchRequest req);

	StarForceResponse StarForce(StarForceRequest req);

	PotentialResponse Potential(PotentialRequest req);

	SymbolResponse Symbol(SymbolRequest req);

	WeeklyGemstoneResponse WeeklyGemstone(WeeklyGemstoneRequest req);

	HyperStatResponse HyperStat(HyperStatRequest req);

	AbilityResponse InnerAbility(AbilityRequest req);

	FlameResponse Flame(FlameRequest req);

	SoulResponse Soul(SoulRequest req);

	SlotRemoveResponse RemoveSlot(SlotRemoveRequest req);

	VMatrixResponse VMatrix(VMatrixRequest req);

	HexaResponse Hexa(HexaRequest req);

	HexaConvertResponse HexaConvert(HexaConvertRequest req);

	void EndSocialSession(int characterId);
}

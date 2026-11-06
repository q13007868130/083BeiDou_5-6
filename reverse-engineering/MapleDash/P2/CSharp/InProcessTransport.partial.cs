// Reconstructed from actual MapleDash IL; not the original Unity project.
// Only methods whose IL was fully recognized are included.
// Referenced project types and remaining members must be supplied separately.
using System;
using System.Collections.Generic;

public partial class InProcessTransport
{
    private GameServer server;

    // Original method token 0x0600143a; RVA 0xdf44c
    public InProcessTransport(GameServer s)
    {
        this.server = s;
    }

    // Original method token 0x0600143b; RVA 0xdf45b
    public ScriptValueResponse ScriptValue(ScriptValueRequest r)
    {
        return this.server.ScriptValues.Handle(r);
    }

    // Original method token 0x0600143c; RVA 0xdf46e
    public ScriptResponse Script(ScriptRequest r)
    {
        return this.server.Scripts.Handle(r);
    }

    // Original method token 0x0600143d; RVA 0xdf481
    public SocialResponse Social(SocialRequest r)
    {
        return this.server.Social.Handle(r);
    }

    // Original method token 0x0600143e; RVA 0xdf494
    public Server.Bot.PartyGrindResponse PartyGrind(Server.Bot.PartyGrindRequest r)
    {
        return Server.Bot.BotPartyGrindProtocol.Handle(this.server.Bots, r);
    }

    // Original method token 0x0600143f; RVA 0xdf4a7
    public Server.Bot.Trade.BotTradeResponse BotTrade(Server.Bot.Trade.BotTradeRequest r)
    {
        return this.server.Bots.tradeProtocol.Handle(r);
    }

    // Original method token 0x06001440; RVA 0xdf4bf
    public Server.Bot.MarketResponse Market(Server.Bot.MarketRequest r)
    {
        return this.server.Bots.marketProtocol.Handle(r);
    }

    // Original method token 0x06001441; RVA 0xdf4d7
    public CommunityResponse Community(CommunityRequest r)
    {
        return this.server.Community.Handle(r);
    }

    // Original method token 0x06001442; RVA 0xdf4ea
    public BossResponse Boss(BossRequest r)
    {
        return this.server.Bosses.Handle(r);
    }

    // Original method token 0x06001443; RVA 0xdf4fd
    public InventoryChangeResponse ChangeInventory(InventoryChangeRequest r)
    {
        return NativeInventoryService.Handle(this.server.Db, r);
    }

    // Original method token 0x06001444; RVA 0xdf510
    public EquipPatchResponse UpdateEquip(EquipPatchRequest r)
    {
        return NativeEquipService.Patch(this.server.Db, r);
    }

    // Original method token 0x0600144f; RVA 0xdfa99
    public SlotRemoveResponse RemoveSlot(SlotRemoveRequest r)
    {
        return NativeEquipService.RemoveSlot(this.server.Db, r);
    }

    // Original method token 0x06001453; RVA 0xdfc34
    public bool HasNpcShop(int npcId)
    {
        return this.server.NpcShops.HasNpc(npcId);
    }

    // Original method token 0x06001454; RVA 0xdfc47
    public NpcShopResponse NpcShop(NpcShopRequest r)
    {
        return this.server.NpcShops.Handle(r);
    }

    // Original method token 0x06001455; RVA 0xdfc5a
    public DueyResponse Duey(DueyRequest r)
    {
        return NativeDueyService.Handle(this.server.Db, r);
    }

    // Original method token 0x06001456; RVA 0xdfc6d
    public MerchantResponse Merchant(MerchantRequest r)
    {
        return NativeMerchantService.Handle(this.server.Db, r);
    }

    // Original method token 0x06001457; RVA 0xdfc80
    public PlayerShopResponse PlayerShop(PlayerShopRequest r)
    {
        return NativePlayerShopService.Handle(this.server.Db, r);
    }

    // Original method token 0x06001458; RVA 0xdfc93
    public MakerResponse Maker(MakerRequest r)
    {
        return NativeMakerService.Handle(this.server.Db, r);
    }

    // Original method token 0x06001459; RVA 0xdfca6
    public RankingResponse Ranking(RankingRequest r)
    {
        return NativeRankingService.Handle(this.server.Db, r);
    }

    // Original method token 0x0600145a; RVA 0xdfcb9
    public OwlResponse Owl(OwlRequest r)
    {
        return NativeOwlService.Handle(this.server.Db, r);
    }

    // Original method token 0x0600145b; RVA 0xdfccc
    public StorageResponse Storage(StorageRequest r)
    {
        return NativeStorageService.Handle(this.server.Db, r);
    }

    // Original method token 0x0600145c; RVA 0xdfcdf
    public MacroResponse Macro(MacroRequest r)
    {
        return NativeSkillMacroService.Handle(this.server.Db, r);
    }

    // Original method token 0x0600145d; RVA 0xdfcf2
    public ReportResponse Report(ReportRequest r)
    {
        return NativeReportService.Handle(this.server.Db, r);
    }

    // Original method token 0x0600145e; RVA 0xdfd05
    public ArenaResponse Arena(ArenaRequest r)
    {
        return NativeEventArenaService.Handle(this.server.Db, r);
    }

    // Original method token 0x0600145f; RVA 0xdfd18
    public RpsResponse Rps(RpsRequest r)
    {
        return NativeRpsService.Handle(this.server.Db, r);
    }

    // Original method token 0x06001460; RVA 0xdfd2b
    public MemoResponse Memo(MemoRequest r)
    {
        return NativeNoteService.Handle(this.server.Db, r);
    }

    // Original method token 0x06001461; RVA 0xdfd3e
    public TvResponse Tv(TvRequest r)
    {
        return NativeMapleTvService.Handle(this.server.Db, r);
    }

    // Original method token 0x06001462; RVA 0xdfd51
    public CarnivalResponse Carnival(CarnivalRequest r)
    {
        return NativeCarnivalService.Handle(this.server.Db, r);
    }

    // Original method token 0x06001463; RVA 0xdfd64
    public CashResponse Cash(CashRequest r)
    {
        return this.server.Cash.Handle(r);
    }

    // Original method token 0x06001464; RVA 0xdfd78
    public void EndSocialSession(int characterId)
    {
        NativePetItems.EndSession(this.server.Db, characterId);
        NativePlayerShopService.Logout(this.server.Db, characterId);
        NativeReportService.EndSession(characterId);
        this.server.EndBotSession(characterId);
        this.server.Scripts.EndSession(characterId);
        this.server.Social.EndSession(characterId);
        this.server.Community.EndSession(characterId);
        this.server.Cash.EndSession(characterId);
        this.server.NpcShops.EndSession(characterId);
    }

}
// Reconstructed from actual MapleDash IL; not the original Unity project.
// Only methods whose IL was fully recognized are included.
// Referenced project types and remaining members must be supplied separately.
using System;
using System.Collections.Generic;

public partial class GameServer
{
    // Original backing field <Db>k__BackingField; property accessor IL verified
    public DbStore Db { get; private set; }
    // Original backing field <Transport>k__BackingField; property accessor IL verified
    public ITransport Transport { get; private set; }
    // Original backing field <MapInfo>k__BackingField; property accessor IL verified
    public IMapInfo MapInfo { get; set; }
    // Original backing field <ScriptValues>k__BackingField; property accessor IL verified
    public NativeScriptValues ScriptValues { get; private set; }
    // Original backing field <Scripts>k__BackingField; property accessor IL verified
    public NativeScriptService Scripts { get; private set; }
    // Original backing field <Social>k__BackingField; property accessor IL verified
    public NativeSocialService Social { get; private set; }
    // Original backing field <Community>k__BackingField; property accessor IL verified
    public NativeCommunityService Community { get; private set; }
    // Original backing field <Cash>k__BackingField; property accessor IL verified
    public NativeCashService Cash { get; private set; }
    // Original backing field <NpcShops>k__BackingField; property accessor IL verified
    public NativeNpcShopService NpcShops { get; private set; }
    // Original backing field <Bosses>k__BackingField; property accessor IL verified
    public NativeBossService Bosses { get; private set; }
    // Original backing field <Weather>k__BackingField; property accessor IL verified
    public NativeWeatherService Weather { get; set; }
    // Original backing field <Backdrops>k__BackingField; property accessor IL verified
    public NativeSkillBackdropService Backdrops { get; }
    // Original backing field <Local>k__BackingField; property accessor IL verified
    public static GameServer Local { get; private set; }

    private Server.Bot.BotRuntime bots;
    private System.Action<Server.Bot.BotActor, long> TradeInventoryCommitted;

    // Original method token 0x060013df; RVA 0xdee75
    public void Save()
    {
        Db.Save();
    }

}
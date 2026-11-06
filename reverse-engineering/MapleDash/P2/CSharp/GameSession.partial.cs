// Reconstructed from actual MapleDash IL; not the original Unity project.
// Only methods whose IL was fully recognized are included.
// Referenced project types and remaining members must be supplied separately.
using System;
using System.Collections.Generic;

public partial class GameSession
{
    // Original backing field <Server>k__BackingField; property accessor IL verified
    public static GameServer Server { get; private set; }
    // Original backing field <Net>k__BackingField; property accessor IL verified
    public static ITransport Net { get; private set; }
    // Original backing field <AccountId>k__BackingField; property accessor IL verified
    public static int AccountId { get; private set; }
    // Original backing field <AccountName>k__BackingField; property accessor IL verified
    public static string AccountName { get; private set; }
    // Original backing field <World>k__BackingField; property accessor IL verified
    public static int World { get; private set; }
    // Original backing field <Channel>k__BackingField; property accessor IL verified
    public static int Channel { get; private set; }
    // Original backing field <CharacterId>k__BackingField; property accessor IL verified
    public static int CharacterId { get; private set; }
    // Original backing field <Character>k__BackingField; property accessor IL verified
    public static FullCharacter Character { get; private set; }
    // Original backing field <InGame>k__BackingField; property accessor IL verified
    public static bool InGame { get; private set; }
    // Original backing field <AccountGender>k__BackingField; property accessor IL verified
    public static int AccountGender { get; private set; }

    private static System.Collections.Generic.Dictionary<long, string[]> appearances;

    // Original method token 0x060002aa; RVA 0x1c156
    public static bool SetGender(int gender)
    {
        return GameSession.SetGender(GameSession.AccountId, gender);
    }

    // Original method token 0x060002af; RVA 0x1c286
    public static bool NameFormatOk(string name)
    {
        return CharNameRules.Valid(name);
    }

    // Original method token 0x060002b0; RVA 0x1c28e
    public static CreateCharResponse CreateChar(CreateCharRequest req)
    {
        req.accountId = GameSession.AccountId;
        req.world = GameSession.World;
        return GameSession.Net.CreateChar(req);
    }

    // Original method token 0x060002b5; RVA 0x1c45f
    public static void BeginSession(int accountId, string accountName, int world, int channel)
    {
        GameSession.AccountId = accountId;
        GameSession.AccountName = accountName;
        GameSession.World = world;
        GameSession.Channel = channel;
    }

    // Original method token 0x060002bf; RVA 0x1cf3c
    public static void ApplyGroundPickup(Server.Bot.BotActor actor, long mesoDelta)
    {
        GameSession.ApplyTradeInventory(actor, mesoDelta);
    }

}
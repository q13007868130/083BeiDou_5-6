// MapleDash P3: conservative C# recovery from compiled Mono IL.
// Generated only for wholly handled straight-line method bodies.
// This is NOT a complete project, class, or exact author-authored source.
// Missing fields, properties and referenced types remain dependencies.
using System;
using System.Collections.Generic;

public partial class GameSession
{
    // IL token 0x060002AB RVA 0x0001C163
    public static ServerListResponse ServerList()
    {
        var dup10 = new ServerListRequest();
        dup10.accountId = GameSession.AccountId;
        return GameSession.Net.ServerList(dup10);
    }

    // IL token 0x060002AD RVA 0x0001C25C
    public static string[] PreviewAppearance(CharRecord rec)
    {
        return GameSession.appearances[rec.id];
    }

    // IL token 0x060002AE RVA 0x0001C26E
    public static CheckCharNameResponse CheckCharName(string name)
    {
        var dup10 = new CheckCharNameRequest();
        dup10.name = name;
        return GameSession.Net.CheckCharName(dup10);
    }

    // IL token 0x060002B1 RVA 0x0001C2B1
    public static bool DeleteChar(int characterId)
    {
        var dup10 = new DeleteCharRequest();
        dup10.accountId = GameSession.AccountId;
        dup10.characterId = characterId;
        return GameSession.Net.DeleteChar(dup10).ok;
    }

}

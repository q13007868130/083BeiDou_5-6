// MapleDash P3: conservative C# recovery from compiled Mono IL.
// Generated only for wholly handled straight-line method bodies.
// This is NOT a complete project, class, or exact author-authored source.
// Missing fields, properties and referenced types remain dependencies.
using System;
using System.Collections.Generic;

public partial class InProcessTransport
{
    // IL token 0x06001465 RVA 0x000DFE10
    public LoginResponse Login(LoginRequest r)
    {
        LoginResult V0 = default(LoginResult);
        V0 = LoginService.Login(this.server.Db, r.name, r.password);
        var dup34 = new LoginResponse();
        dup34.code = V0.code;
        dup34.accountId = V0.accountId;
        dup34.accountName = V0.accountName;
        dup34.justRegistered = V0.justRegistered;
        dup34.banUntil = V0.banUntil;
        dup34.gender = V0.gender;
        return dup34;
    }

    // IL token 0x06001467 RVA 0x000DFEE6
    public ServerListResponse ServerList(ServerListRequest r)
    {
        var dup5 = new ServerListResponse();
        dup5.worlds = WorldService.Worlds(this.server.Db);
        return dup5;
    }

    // IL token 0x06001468 RVA 0x000DFF04
    public CharListResponse CharList(CharListRequest r)
    {
        var dup5 = new CharListResponse();
        dup5.characterSlots = AccountsDao.CharacterSlots(this.server.Db, r.accountId);
        dup5.characters = CharactersDao.ByAccount(this.server.Db, r.accountId, r.world);
        return dup5;
    }

    // IL token 0x0600146D RVA 0x000E0145
    public ImportLegacyResponse ImportLegacy(ImportLegacyRequest r)
    {
        var dup5 = new ImportLegacyResponse();
        dup5.characterId = LegacyImport.Import(this.server.Db, r.snapshot);
        return dup5;
    }

}

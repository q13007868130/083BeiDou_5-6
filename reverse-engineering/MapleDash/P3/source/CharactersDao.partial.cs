// MapleDash P3: conservative C# recovery from compiled Mono IL.
// Generated only for wholly handled straight-line method bodies.
// This is NOT a complete project, class, or exact author-authored source.
// Missing fields, properties and referenced types remain dependencies.
using System;
using System.Collections.Generic;

public partial class CharactersDao
{
    // IL token 0x060012B5 RVA 0x000D809D
    public static bool NameTaken(DbStore db, string name)
    {
        return (db.Table("characters").Find(DbStore.Eq("name", name)) != null);
    }

    // IL token 0x060012B6 RVA 0x000D80BD
    public static long Insert(DbStore db, CharRecord c)
    {
        return db.Table("characters").Insert(c.ToInsertRow());
    }

    // IL token 0x060012B7 RVA 0x000D80D5
    public static void Save(DbStore db, CharRecord c)
    {
        db.Table("characters").Update(DbStore.Eq("id", ((object)(c.id))), c.ToUpdateRow());
    }

    // IL token 0x060012B9 RVA 0x000D82B2
    public static bool CanCreateChar(string name)
    {
        return CharNameRules.Valid(name);
    }

}

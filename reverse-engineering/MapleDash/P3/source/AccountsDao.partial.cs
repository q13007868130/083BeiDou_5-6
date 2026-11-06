// MapleDash P3: conservative C# recovery from compiled Mono IL.
// Generated only for wholly handled straight-line method bodies.
// This is NOT a complete project, class, or exact author-authored source.
// Missing fields, properties and referenced types remain dependencies.
using System;
using System.Collections.Generic;

public partial class AccountsDao
{
    // IL token 0x060012BE RVA 0x000D8431
    public static DbRow FindByName(DbStore db, string name)
    {
        return db.Table("accounts").Find(DbStore.Eq("name", name));
    }

    // IL token 0x060012BF RVA 0x000D844E
    public static DbRow FindById(DbStore db, int id)
    {
        return db.Table("accounts").Find(DbStore.Eq("id", ((object)(((long)(id))))));
    }

    // IL token 0x060012C0 RVA 0x000D8474
    public static int AutoRegister(DbStore db, string name, string pwd)
    {
        DbRow V0 = default(DbRow);
        var dup16 = new DbRow();
        dup16["name"] = name;
        dup16["password"] = AccountsDao.HashPassword(pwd);
        dup16["createdat"] = DbStore.NowText();
        V0 = dup16;
        return ((int)(db.Table("accounts").Insert(V0)));
    }

    // IL token 0x060012C2 RVA 0x000D84EE
    public static int Count(DbStore db)
    {
        return db.Table("accounts").rows.Count;
    }

    // IL token 0x060012C5 RVA 0x000D8593
    public static int Gender(DbStore db, int accountId)
    {
        return DbStore.Int(AccountsDao.FindById(db, accountId), "gender", 10);
    }

}

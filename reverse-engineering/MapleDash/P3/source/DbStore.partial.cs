// MapleDash P3: conservative C# recovery from compiled Mono IL.
// Generated only for wholly handled straight-line method bodies.
// This is NOT a complete project, class, or exact author-authored source.
// Missing fields, properties and referenced types remain dependencies.
using System;
using System.Collections.Generic;

public partial class DbStore
{
    // IL token 0x060012F3 RVA 0x000D9F4E
    public static DbStore Create(string dbPath)
    {
        var dup5 = new DbStore();
        dup5.path = dbPath;
        dup5.LoadSchema();
        GameConfigDao.EnsureLevelCaps(dup5);
        return dup5;
    }

    // IL token 0x06001300 RVA 0x000DA87F
    public bool HasTable(string name)
    {
        return this.tables.ContainsKey(name);
    }

    // IL token 0x06001305 RVA 0x000DAB24
    public System.IDisposable DeferSaves()
    {
        this.saveBatchDepth = (this.saveBatchDepth + 1);
        return new DbStore.SaveBatch(this);
    }

    // IL token 0x06001309 RVA 0x000DAE29
    private static string Escape(string s)
    {
        return (((s.Replace("\\", "\\\\")).Replace("\"", "\\\"")).Replace("\n", "\\n")).Replace("\r", "\\r");
    }

    // IL token 0x0600130F RVA 0x000DB114
    public static int Int(DbRow r, string col, int fallback)
    {
        return ((int)(DbStore.Long(r, col, ((long)(fallback)))));
    }

    // IL token 0x06001312 RVA 0x000DB19A
    public DbStore()
    {
        this.tables = new System.Collections.Generic.Dictionary<string, DbTable>();
    }

}

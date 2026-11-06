// MapleDash P3: conservative C# recovery from compiled Mono IL.
// Generated only for wholly handled straight-line method bodies.
// This is NOT a complete project, class, or exact author-authored source.
// Missing fields, properties and referenced types remain dependencies.
using System;
using System.Collections.Generic;

public partial class AdminDbUtil
{
    // IL token 0x060000C1 RVA 0x00009FF5
    public static string Text(DbRow row, string col)
    {
        return DbStore.Str(row, col, "");
    }

}

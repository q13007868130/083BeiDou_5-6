// MapleDash P3: conservative C# recovery from compiled Mono IL.
// Generated only for wholly handled straight-line method bodies.
// This is NOT a complete project, class, or exact author-authored source.
// Missing fields, properties and referenced types remain dependencies.
using System;
using System.Collections.Generic;

public partial class LoginService
{
    // IL token 0x060013E5 RVA 0x000DEF2E
    public static bool IsNoPasswordAccount(string name)
    {
        return System.String.Equals(((name ?? "")).Trim(), "admin", (System.StringComparison)5);
    }

}

// MapleDash P3: conservative C# recovery from compiled Mono IL.
// Generated only for wholly handled straight-line method bodies.
// This is NOT a complete project, class, or exact author-authored source.
// Missing fields, properties and referenced types remain dependencies.
using System;
using System.Collections.Generic;

public partial class AdminJson
{
    // IL token 0x06000115 RVA 0x0000CB7C
    public static string Write(object @value)
    {
        var dup10 = new System.Text.StringBuilder(256);
        AdminJson.WriteValue(dup10, @value);
        return dup10.ToString();
    }

    // IL token 0x0600011B RVA 0x0000D080
    public static System.Collections.Generic.Dictionary<string, object> AsDict(object o)
    {
        return (o as System.Collections.Generic.Dictionary<string, object>);
    }

    // IL token 0x0600011C RVA 0x0000D088
    public static System.Collections.Generic.List<object> AsList(object o)
    {
        return (o as System.Collections.Generic.List<object>);
    }

    // IL token 0x0600011E RVA 0x0000D0B7
    public static System.Collections.Generic.Dictionary<string, object> GetDict(object o, string key)
    {
        return AdminJson.AsDict(AdminJson.Get(o, key));
    }

    // IL token 0x0600011F RVA 0x0000D0C5
    public static System.Collections.Generic.List<object> GetList(object o, string key)
    {
        return AdminJson.AsList(AdminJson.Get(o, key));
    }

    // IL token 0x06000122 RVA 0x0000D185
    public static int GetInt(object o, string key, int fallback)
    {
        return ((int)(AdminJson.GetLong(o, key, ((long)(fallback)))));
    }

}

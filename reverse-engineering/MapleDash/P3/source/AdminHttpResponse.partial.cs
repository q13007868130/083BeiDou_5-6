// MapleDash P3: conservative C# recovery from compiled Mono IL.
// Generated only for wholly handled straight-line method bodies.
// This is NOT a complete project, class, or exact author-authored source.
// Missing fields, properties and referenced types remain dependencies.
using System;
using System.Collections.Generic;

public partial class AdminHttpResponse
{
    // IL token 0x06000101 RVA 0x0000C031
    public static AdminHttpResponse Json(object payload, int status)
    {
        var dup5 = new AdminHttpResponse();
        dup5.Status = status;
        dup5.ContentType = "application/json; charset=utf-8";
        dup5.Body = System.Text.Encoding.UTF8.GetBytes(AdminJson.Write(payload));
        return dup5;
    }

    // IL token 0x06000102 RVA 0x0000C060
    public static AdminHttpResponse Text(string text, int status, string type)
    {
        var dup5 = new AdminHttpResponse();
        dup5.Status = status;
        dup5.ContentType = type;
        dup5.Body = System.Text.Encoding.UTF8.GetBytes((text ?? ""));
        return dup5;
    }

}

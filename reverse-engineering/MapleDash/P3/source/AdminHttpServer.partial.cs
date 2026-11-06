// MapleDash P3: conservative C# recovery from compiled Mono IL.
// Generated only for wholly handled straight-line method bodies.
// This is NOT a complete project, class, or exact author-authored source.
// Missing fields, properties and referenced types remain dependencies.
using System;
using System.Collections.Generic;

public partial class AdminHttpServer
{
    // IL token 0x06000108 RVA 0x0000C108
    public AdminHttpServer(int port, System.Func<AdminHttpRequest, AdminHttpResponse> handler, System.Action<string> log)
    {
        this.Port = port;
        this.handler = handler;
        this.log = log;
    }

}

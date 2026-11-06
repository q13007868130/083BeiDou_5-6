// MapleDash P3: conservative C# recovery from compiled Mono IL.
// Generated only for wholly handled straight-line method bodies.
// This is NOT a complete project, class, or exact author-authored source.
// Missing fields, properties and referenced types remain dependencies.
using System;
using System.Collections.Generic;

public partial class AdminApi
{
    // IL token 0x06000010 RVA 0x00002132
    public void Get(string pattern, AdminHandler h, bool auth)
    {
        this.Add("GET", pattern, h, auth);
    }

    // IL token 0x06000011 RVA 0x00002142
    public void Post(string pattern, AdminHandler h, bool auth)
    {
        this.Add("POST", pattern, h, auth);
    }

    // IL token 0x06000012 RVA 0x00002152
    public void Put(string pattern, AdminHandler h, bool auth)
    {
        this.Add("PUT", pattern, h, auth);
    }

    // IL token 0x06000013 RVA 0x00002162
    public void Delete(string pattern, AdminHandler h, bool auth)
    {
        this.Add("DELETE", pattern, h, auth);
    }

    // IL token 0x06000014 RVA 0x00002174
    private void Add(string method, string pattern, AdminHandler handler, bool auth)
    {
        string[] V0 = default(string[]);
        V0 = pattern.Trim((char)47).Split((char)47, (System.StringSplitOptions)0);
        var dup28 = new AdminApi.Route();
        dup28.Method = method;
        dup28.Segments = V0;
        dup28.Handler = handler;
        dup28.RequireAuth = auth;
        this.routes.Add(dup28);
    }

    // IL token 0x0600001B RVA 0x000028E0
    public AdminApi()
    {
        this.routes = new System.Collections.Generic.List<AdminApi.Route>();
        this.sessions = new System.Collections.Generic.Dictionary<string, AdminApi.Session>(System.StringComparer.Ordinal);
        this.sessionGate = new System.Object();
        this.stubLogged = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        this.LogStubs = true;
    }

}

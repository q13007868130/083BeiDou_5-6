// Reconstructed from actual MapleDash IL; not the original Unity project.
// Only methods whose IL was fully recognized are included.
// Referenced project types and remaining members must be supplied separately.
using System;
using System.Collections.Generic;

public partial class AdminCtx
{
    public AdminApi Api;
    public AdminHttpRequest Req;
    public System.Collections.Generic.Dictionary<string, string> Params;
    public object RawBody;
    public object Body;
    public string Token;
    public AdminAccount User;

    // Original method token 0x06000008; RVA 0x2099
    public string Query(string key, string fallback)
    {
        return this.Req.QueryValue(key, fallback);
    }

    // Original method token 0x06000009; RVA 0x20a8
    public int QueryInt(string key, int fallback)
    {
        return this.Req.QueryInt(key, fallback);
    }

    // Original method token 0x0600000b; RVA 0x20de
    public string BodyString(string key, string fallback)
    {
        return AdminJson.GetString(this.Body, key, fallback);
    }

    // Original method token 0x0600000c; RVA 0x20ed
    public long BodyLong(string key, long fallback)
    {
        return AdminJson.GetLong(this.Body, key, fallback);
    }

    // Original method token 0x0600000d; RVA 0x20fc
    public int BodyInt(string key, int fallback)
    {
        return AdminJson.GetInt(this.Body, key, fallback);
    }

    // Original method token 0x0600000e; RVA 0x210b
    public bool BodyBool(string key, bool fallback)
    {
        return AdminJson.GetBool(this.Body, key, fallback);
    }

}
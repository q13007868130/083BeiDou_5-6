// MapleDash P3: conservative C# recovery from compiled Mono IL.
// Generated only for wholly handled straight-line method bodies.
// This is NOT a complete project, class, or exact author-authored source.
// Missing fields, properties and referenced types remain dependencies.
using System;
using System.Collections.Generic;

public partial class AdminHost
{
    // IL token 0x060000F1 RVA 0x0000BB06
    private void OnApplicationFocus(bool hasFocus)
    {
        this.SwitchForeground(hasFocus);
    }

    // IL token 0x060000F2 RVA 0x0000BB0F
    private void OnApplicationPause(bool paused)
    {
        this.SwitchForeground((!paused));
    }

    // IL token 0x060000F6 RVA 0x0000BBDB
    private void OnApplicationQuit()
    {
        AdminWebView.Close();
        this.StopServer();
    }

}

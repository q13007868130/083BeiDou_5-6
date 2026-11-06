// MapleDash P3: conservative C# recovery from compiled Mono IL.
// Generated only for wholly handled straight-line method bodies.
// This is NOT a complete project, class, or exact author-authored source.
// Missing fields, properties and referenced types remain dependencies.
using System;
using System.Collections.Generic;

namespace Server.Bot
{
public partial class BotWorkPump
{
    // IL token 0x06001E0E RVA 0x00149E3F
    public static bool Has(float reserveMs)
    {
        return ((Server.Bot.BotWorkPump.FrameSpentMs + ((double)(reserveMs))) < 4.0d);
    }

}
}

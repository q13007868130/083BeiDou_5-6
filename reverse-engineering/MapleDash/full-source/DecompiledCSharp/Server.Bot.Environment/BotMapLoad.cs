using Server.Field;

namespace Server.Bot.Environment;

public delegate BotMapState BotMapLoad(int mapId, out MapRuntime map, bool allowBlocking);

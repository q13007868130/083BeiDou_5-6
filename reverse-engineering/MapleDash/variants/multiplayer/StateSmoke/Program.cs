using System.Text.Json;
using MapleDash.Multiplayer.Client;

static WorldEvent Event(string type, string raw)
{
    using var j = JsonDocument.Parse(raw);
    return new WorldEvent(type, j.RootElement.Clone());
}
static void Check(bool ok, string what)
{
    if (!ok) throw new Exception("FAILED: " + what);
    Console.WriteLine("PASS " + what);
}

var roster = new RemoteWorldState();
var changes = new List<RemoteAvatarChange>();
roster.Changed += changes.Add;
roster.Apply(Event("welcome", "{\"name\":\"sdk_alice\"}"));
Check(roster.LocalName == "sdk_alice" && roster.RemoteCount == 0, "local identity and roster reset");
roster.Apply(Event("snapshot", "{\"players\":[{\"name\":\"sdk_alice\",\"x\":0,\"y\":0},{\"name\":\"sdk_bob\",\"x\":12,\"y\":8}]}"));
Check(roster.RemoteCount == 1 && roster.TryGet("SDK_BOB", out var bob) && bob?.X == 12 && bob.HasPosition,
      "snapshot creates remote avatar, excludes local account");
roster.Apply(Event("presence", "{\"action\":\"joined\",\"name\":\"sdk_charlie\"}"));
Check(roster.TryGet("sdk_charlie", out var charlie) && charlie != null && !charlie.HasPosition,
      "presence placeholder awaits first position");
roster.Apply(Event("move", "{\"name\":\"sdk_charlie\",\"x\":21,\"y\":19,\"seq\":1}"));
Check(roster.TryGet("SDK_CHARLIE", out charlie) && charlie?.HasPosition == true && charlie.Y == 19,
      "movement creates positioned remote actor");
roster.Apply(Event("move", "{\"name\":\"sdk_alice\",\"x\":30,\"y\":30}"));
Check(roster.RemoteCount == 2 && !roster.TryGet("sdk_alice", out _), "self movement never becomes remote actor");
roster.Apply(Event("presence", "{\"action\":\"left\",\"name\":\"SDK_CHARLIE\"}"));
Check(roster.RemoteCount == 1 && changes.Last().Kind == RemoteAvatarChangeKind.Removed,
      "disconnect removes remote actor");
roster.Apply(Event("snapshot", "{\"players\":[{\"name\":\"sdk_alice\",\"x\":0,\"y\":0}]}"));
Check(roster.RemoteCount == 0 && changes.Last().Kind == RemoteAvatarChangeKind.Reset,
      "new snapshot removes stale avatars");
roster.Apply(Event("move", "{\"name\":\"sdk_invalid\",\"x\":\"bad\",\"y\":10}"));
Check(roster.RemoteCount == 0, "invalid movement payload ignored without exception");
roster.Reset();
Check(roster.LocalName == null && roster.RemoteCount == 0, "local logout clears all state");
Console.WriteLine("ALL REMOTE WORLD ROSTER TESTS PASSED");

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace MapleDash.Multiplayer.Client
{
    /// <summary>
    /// Main-thread event reducer for server-authenticated world events. Unity is not required.
    /// This is a visual/demo player roster, NOT the authoritative game character database.
    /// </summary>
    public sealed class RemoteAvatar
    {
        public string Name { get; }
        public int X { get; }
        public int Y { get; }
        public bool HasPosition { get; }
        public RemoteAvatar(string name, int x, int y, bool hasPosition)
        {
            Name = name; X = x; Y = y; HasPosition = hasPosition;
        }
    }

    public enum RemoteAvatarChangeKind { Reset, Upsert, Removed }

    public sealed class RemoteAvatarChange
    {
        public RemoteAvatarChangeKind Kind { get; }
        public RemoteAvatar? Avatar { get; }
        public RemoteAvatarChange(RemoteAvatarChangeKind kind, RemoteAvatar? avatar)
        {
            Kind = kind; Avatar = avatar;
        }
    }

    public sealed class RemoteWorldState
    {
        private readonly Dictionary<string, RemoteAvatar> avatars =
            new Dictionary<string, RemoteAvatar>(StringComparer.OrdinalIgnoreCase);

        public string? LocalName { get; private set; }
        public int RemoteCount => avatars.Count;
        public event Action<RemoteAvatarChange>? Changed;
        public IReadOnlyCollection<RemoteAvatar> GetRemotePlayers() => avatars.Values.ToArray();
        public bool TryGet(string name, out RemoteAvatar? avatar) => avatars.TryGetValue(name, out avatar);

        public void Reset()
        {
            avatars.Clear();
            LocalName = null;
            Changed?.Invoke(new RemoteAvatarChange(RemoteAvatarChangeKind.Reset, null));
        }

        /// <summary>Call only from the game/UI main thread, never from the socket receive loop.</summary>
        public void Apply(WorldEvent message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            JsonElement payload = message.Payload;
            if (payload.ValueKind != JsonValueKind.Object) return;
            switch (message.Type)
            {
                case "welcome":
                    if (GetName(payload, "name", out string self))
                    {
                        // A reconnect can switch identities. Never carry over stale remote players.
                        avatars.Clear();
                        LocalName = self;
                        Changed?.Invoke(new RemoteAvatarChange(RemoteAvatarChangeKind.Reset, null));
                    }
                    break;
                case "snapshot":
                    if (!payload.TryGetProperty("players", out JsonElement players) ||
                        players.ValueKind != JsonValueKind.Array) return;
                    avatars.Clear();
                    Changed?.Invoke(new RemoteAvatarChange(RemoteAvatarChangeKind.Reset, null));
                    foreach (JsonElement entry in players.EnumerateArray())
                    {
                        if (entry.ValueKind == JsonValueKind.Object &&
                            GetName(entry, "name", out string name) &&
                            GetCoordinates(entry, out int x, out int y))
                            Put(name, x, y, true);
                    }
                    break;
                case "presence":
                    if (!GetName(payload, "name", out string presenceName) ||
                        !GetString(payload, "action", out string action)) return;
                    if (action == "left") Remove(presenceName);
                    else if (action == "joined" && !IsLocal(presenceName) && !avatars.ContainsKey(presenceName))
                        Put(presenceName, 0, 0, false); // Wait for first real position before rendering.
                    break;
                case "move":
                    if (GetName(payload, "name", out string movingName) &&
                        GetCoordinates(payload, out int moveX, out int moveY))
                        Put(movingName, moveX, moveY, true);
                    break;
            }
        }

        private bool IsLocal(string name) =>
            LocalName != null && string.Equals(name, LocalName, StringComparison.OrdinalIgnoreCase);

        private void Put(string name, int x, int y, bool hasPosition)
        {
            if (IsLocal(name)) return;
            var next = new RemoteAvatar(name, x, y, hasPosition);
            avatars[name] = next;
            Changed?.Invoke(new RemoteAvatarChange(RemoteAvatarChangeKind.Upsert, next));
        }

        private void Remove(string name)
        {
            if (avatars.TryGetValue(name, out RemoteAvatar? old))
            {
                avatars.Remove(name);
                Changed?.Invoke(new RemoteAvatarChange(RemoteAvatarChangeKind.Removed, old));
            }
        }

        private static bool GetCoordinates(JsonElement obj, out int x, out int y)
        {
            x = 0; y = 0;
            return obj.TryGetProperty("x", out JsonElement a) && a.ValueKind == JsonValueKind.Number && a.TryGetInt32(out x) &&
                   obj.TryGetProperty("y", out JsonElement b) && b.ValueKind == JsonValueKind.Number && b.TryGetInt32(out y);
        }

        private static bool GetName(JsonElement obj, string key, out string value)
        {
            value = string.Empty;
            return GetString(obj, key, out value) && value.Length >= 3 && value.Length <= 64;
        }

        private static bool GetString(JsonElement obj, string key, out string value)
        {
            value = string.Empty;
            if (!obj.TryGetProperty(key, out JsonElement e) || e.ValueKind != JsonValueKind.String) return false;
            string? candidate = e.GetString();
            if (string.IsNullOrWhiteSpace(candidate)) return false;
            value = candidate;
            return true;
        }
    }
}

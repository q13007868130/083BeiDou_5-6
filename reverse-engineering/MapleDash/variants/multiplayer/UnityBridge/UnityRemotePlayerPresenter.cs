// Debug-only Unity scene visualization for MapleDash's authenticated world-events sidecar.
// It deliberately does not alter GameSession.Net or the original offline GameServer.
// Replace placeholder sprites with the game's avatar renderer once Unity resources are available.
using System;
using System.Collections.Generic;
using MapleDash.Multiplayer.Client;
using UnityEngine;

public sealed class UnityRemotePlayerPresenter : MonoBehaviour
{
    [SerializeField] private UnityRemoteWorldBridge bridge;
    [SerializeField] private Transform avatarRoot;
    [SerializeField] private float worldUnitsPerNetworkUnit = 0.04f;
    [SerializeField] private bool invertNetworkY = true;
    [SerializeField] private float depth = 0f;
    [SerializeField] private int sortingOrder = 200;
    [SerializeField] private Color placeholderTint = new Color(0.30f, 0.75f, 1.0f, 0.9f);

    private readonly RemoteWorldState roster = new RemoteWorldState();
    private readonly Dictionary<string, GameObject> actors =
        new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
    private static Texture2D whitePixelTexture;
    private static Sprite whitePixel;

    private void OnEnable()
    {
        if (bridge == null) bridge = GetComponent<UnityRemoteWorldBridge>();
        roster.Changed += ApplyChange;
        if (bridge != null) bridge.OnWorldEvent += ReceiveWorldEvent;
        else Debug.LogError("[MapleDashRemote] UnityRemoteWorldBridge is missing");
    }

    private void OnDisable()
    {
        if (bridge != null) bridge.OnWorldEvent -= ReceiveWorldEvent;
        roster.Changed -= ApplyChange;
        ClearActors();
        roster.Reset();
    }

    private void ReceiveWorldEvent(WorldEvent message)
    {
        // UnityRemoteWorldBridge dispatches OnWorldEvent only from its Update() method.
        roster.Apply(message);
    }

    private void ApplyChange(RemoteAvatarChange change)
    {
        if (change.Kind == RemoteAvatarChangeKind.Reset)
        {
            ClearActors();
            return;
        }
        if (change.Avatar == null) return;
        if (change.Kind == RemoteAvatarChangeKind.Removed)
        {
            RemoveActor(change.Avatar.Name);
            return;
        }
        if (!change.Avatar.HasPosition) return;
        GameObject actor = GetOrCreateActor(change.Avatar.Name);
        float scale = worldUnitsPerNetworkUnit > 0f ? worldUnitsPerNetworkUnit : 0.04f;
        actor.transform.localPosition = new Vector3(
            change.Avatar.X * scale,
            change.Avatar.Y * scale * (invertNetworkY ? -1f : 1f), depth);
    }

    private GameObject GetOrCreateActor(string name)
    {
        if (actors.TryGetValue(name, out GameObject existing)) return existing;
        if (whitePixel == null)
        {
            whitePixelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            whitePixelTexture.SetPixel(0, 0, Color.white);
            whitePixelTexture.Apply();
            whitePixel = Sprite.Create(whitePixelTexture, new Rect(0, 0, 1, 1),
                                       new Vector2(0.5f, 0.5f), 1f);
        }
        var actor = new GameObject("MapleDashRemote_" + name);
        actor.transform.SetParent(avatarRoot != null ? avatarRoot : transform, false);
        actor.transform.localScale = new Vector3(0.8f, 1.2f, 1f);
        var renderer = actor.AddComponent<SpriteRenderer>();
        renderer.sprite = whitePixel;
        renderer.color = placeholderTint;
        renderer.sortingOrder = sortingOrder;
        var label = new GameObject("NameLabel");
        label.transform.SetParent(actor.transform, false);
        label.transform.localPosition = new Vector3(0, 0.7f, 0);
        var text = label.AddComponent<TextMesh>();
        text.text = name;
        text.anchor = TextAnchor.MiddleCenter;
        text.characterSize = 0.1f;
        text.fontSize = 24;
        actors.Add(name, actor);
        return actor;
    }

    private void RemoveActor(string name)
    {
        if (actors.TryGetValue(name, out GameObject actor))
        {
            actors.Remove(name);
            UnityEngine.Object.Destroy(actor);
        }
    }

    private void ClearActors()
    {
        foreach (GameObject actor in actors.Values) UnityEngine.Object.Destroy(actor);
        actors.Clear();
    }
}

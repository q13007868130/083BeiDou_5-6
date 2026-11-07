// Drop-in Unity scene component candidate. Requires the separate Client DLL and its System.Text.Json deps.
// World-event sidecar ONLY: not an ITransport replacement, does not render remote players automatically.
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MapleDash.Multiplayer.Client;
using UnityEngine;

public sealed class UnityRemoteWorldBridge : MonoBehaviour
{
    [SerializeField] private string serverOrigin = "http://127.0.0.1:5088";
    private readonly ConcurrentQueue<WorldEvent> received = new ConcurrentQueue<WorldEvent>();
    private RemoteWorldClient client;
    private CancellationTokenSource stop;
    private Task reader;

    // Dispatches on Unity's main thread; callers can map snapshot/move/presence/chat to scene entities.
    public event Action<WorldEvent> OnWorldEvent;
    public bool IsConnected => client != null && client.IsConnected;

    public async Task LoginAndConnectAsync(string username, string password)
    {
        if (client != null) throw new InvalidOperationException("Already initialized");
        client = new RemoteWorldClient(new Uri(serverOrigin));
        stop = new CancellationTokenSource();
        try
        {
            await client.LoginAsync(username, password, stop.Token);
            await client.ConnectAsync(stop.Token);
            reader = ReadLoopAsync(stop.Token);
        }
        catch
        {
            client.Dispose();
            client = null;
            stop.Dispose();
            stop = null;
            throw;
        }
    }

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var message = await client.ReceiveAsync(ct);
                if (message == null) break;
                received.Enqueue(message);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception e) { Debug.LogError("[RemoteWorld] " + e.GetType().Name); }
    }

    // Unity main-thread input/scene code calls these; never bake credentials into Unity assets.
    public Task SendMoveAsync(int x, int y) =>
        client == null ? throw new InvalidOperationException("Not authenticated") : client.MoveAsync(x, y);
    public Task SendChatAsync(string text) =>
        client == null ? throw new InvalidOperationException("Not authenticated") : client.ChatAsync(text);

    private void Update()
    {
        // Limit events per frame to avoid stalls on large update bursts.
        for (int i = 0; i < 100 && received.TryDequeue(out var evt); i++)
            OnWorldEvent?.Invoke(evt);
    }

    private void OnDestroy()
    {
        stop?.Cancel();
        client?.Dispose();
        client = null;
        stop?.Dispose();
        stop = null;
    }
}

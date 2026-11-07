using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using MapleDash.Multiplayer;

var builder = WebApplication.CreateBuilder(args);
// Development-only default. Public deployments require HTTPS/WSS, a trusted reverse proxy,
// hardened authentication, and a complete review of gameplay authorization.
builder.WebHost.UseUrls(Environment.GetEnvironmentVariable("MAPLEDASH_BIND_URL") ?? "http://127.0.0.1:5088");
builder.Services.AddSingleton(new AccountStore(
    Path.Combine(Environment.GetEnvironmentVariable("MAPLEDASH_DATA_DIR")
                 ?? Path.Combine(AppContext.BaseDirectory, "data"), "accounts.json")));
builder.Services.AddSingleton<SessionStore>();
builder.Services.AddSingleton<WorldHub>();
var app = builder.Build();
app.UseWebSockets();

app.MapGet("/health", () => Results.Ok(new { ok = true, protocol = 1, mode = "experimental-multiplayer" }));

app.MapPost("/api/register", async (CredentialInput input, HttpContext context, AccountStore accounts) =>
{
    context.Response.Headers.CacheControl = "no-store";
    var invite = Environment.GetEnvironmentVariable("MAPLEDASH_REGISTRATION_INVITE");
    var supplied = context.Request.Headers["X-Registration-Invite"].ToString();
    if (string.IsNullOrEmpty(invite) || invite.Length < 16 || !Secrets.Equal(supplied, invite))
        return Results.StatusCode(403);
    if (!AccountStore.IsValidInput(input)) return Results.BadRequest(new { error = "invalid_credentials" });
    return await accounts.RegisterAsync(input.Name!, input.Password!, context.RequestAborted)
        ? Results.Ok(new { created = true })
        : Results.Conflict(new { error = "account_exists" });
});

app.MapPost("/api/login", async (CredentialInput input, HttpContext context,
                                  AccountStore accounts, SessionStore sessions) =>
{
    context.Response.Headers.CacheControl = "no-store";
    if (!AccountStore.IsValidInput(input)) return Results.Unauthorized();
    string? user = await accounts.VerifyAsync(input.Name!, input.Password!, context.RequestAborted);
    if (user is null) return Results.Unauthorized();
    return Results.Ok(new { token = sessions.Create(user), name = user, expiresInSeconds = 3600 });
});

app.Map("/ws", async (HttpContext context, SessionStore sessions, WorldHub hub) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }
    string authorization = context.Request.Headers.Authorization.ToString();
    string? token = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
        ? authorization[7..].Trim() : null;
    string? user = sessions.GetUser(token);
    if (user is null)
    {
        context.Response.StatusCode = 401;
        return;
    }
    using var ws = await context.WebSockets.AcceptWebSocketAsync();
    await hub.HandleAsync(user, ws, context.RequestAborted);
});

await app.RunAsync();
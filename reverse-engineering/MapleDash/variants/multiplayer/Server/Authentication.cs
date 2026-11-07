using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MapleDash.Multiplayer;

public sealed record CredentialInput(string? Name, string? Password);

public static class Secrets
{
    public static bool Equal(string left, string right)
    {
        byte[] a = Encoding.UTF8.GetBytes(left);
        byte[] b = Encoding.UTF8.GetBytes(right);
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }
}

public sealed class AccountStore
{
    private sealed record Entry(string Name, string Salt, string Hash);
    private readonly string _path;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly Dictionary<string, Entry> _accounts;
    private const int Iterations = 210_000;

    public AccountStore(string path)
    {
        _path = path;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        _accounts = File.Exists(path)
            ? JsonSerializer.Deserialize<Dictionary<string, Entry>>(File.ReadAllText(path))
              ?? throw new InvalidDataException("Invalid account storage")
            : new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsValidInput(CredentialInput? input) =>
        input is { Name: not null, Password: not null } &&
        Regex.IsMatch(input.Name, "^[a-zA-Z0-9_]{3,20}$", RegexOptions.CultureInvariant) &&
        input.Password.Length >= 12 && input.Password.Length <= 256;

    public async Task<bool> RegisterAsync(string name, string password, CancellationToken ct)
    {
        string key = name.ToLowerInvariant();
        await _mutex.WaitAsync(ct);
        try
        {
            if (_accounts.ContainsKey(key)) return false;
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations,
                                                    HashAlgorithmName.SHA256, 32);
            _accounts[key] = new Entry(key, Convert.ToBase64String(salt), Convert.ToBase64String(hash));
            try
            {
                string temp = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(_accounts), ct);
                    File.Move(temp, _path, overwrite: true);
                }
                finally { if (File.Exists(temp)) File.Delete(temp); }
            }
            catch
            {
                _accounts.Remove(key);
                throw;
            }
            return true;
        }
        finally { _mutex.Release(); }
    }

    public async Task<string?> VerifyAsync(string name, string password, CancellationToken ct)
    {
        await _mutex.WaitAsync(ct);
        try
        {
            string key = name.ToLowerInvariant();
            if (!_accounts.TryGetValue(key, out Entry? entry))
            {
                // Equalize work somewhat to make username probing harder.
                _ = Rfc2898DeriveBytes.Pbkdf2(password, new byte[16], Iterations,
                                               HashAlgorithmName.SHA256, 32);
                return null;
            }
            byte[] expected = Convert.FromBase64String(entry.Hash);
            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(entry.Salt),
                                                       Iterations, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(expected, actual) ? entry.Name : null;
        }
        finally { _mutex.Release(); }
    }
}

public sealed class SessionStore
{
    private sealed record Session(string Name, DateTimeOffset Expires);
    private readonly ConcurrentDictionary<string, Session> _sessions = new(StringComparer.Ordinal);

    public string Create(string user)
    {
        string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _sessions[token] = new Session(user, DateTimeOffset.UtcNow.AddHours(1));
        return token;
    }

    public string? GetUser(string? token)
    {
        if (token is null || token.Length != 64 || !_sessions.TryGetValue(token, out Session? session))
            return null;
        if (session.Expires <= DateTimeOffset.UtcNow)
        {
            _sessions.TryRemove(token, out _);
            return null;
        }
        return session.Name;
    }
}
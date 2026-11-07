// This harness compiles the actual decompiled-and-hardened auth classes with
// in-memory stand-ins for their DB data access dependencies.
// It is NOT an end-to-end multiplayer server test.
using System;
using System.Collections.Generic;
using System.Linq;

public enum LoginResultCode { Invalid, NotRegistered, Banned, WrongPassword, Ok }
public sealed class LoginResult
{
    public LoginResultCode code;
    public int accountId;
    public string accountName;
    public bool justRegistered;
    public int gender;
    public string banUntil;
}
public sealed class DbRow : Dictionary<string, object> {}
public sealed class DbStore
{
    private readonly Dictionary<string, DbTable> tables = new();
    public readonly Dictionary<string, bool> Settings = new();
    public DbTable Table(string table)
    {
        if (!tables.TryGetValue(table, out var found)) tables[table] = found = new DbTable();
        return found;
    }
    public static Predicate<DbRow> Eq(string key, object value) =>
        row => row.TryGetValue(key, out var other) && string.Equals(Convert.ToString(other), Convert.ToString(value), StringComparison.Ordinal);
    public static int Int(DbRow row, string key, int fallback = 0) => row != null && row.TryGetValue(key, out var v) ? Convert.ToInt32(v) : fallback;
    public static string Str(DbRow row, string key) => row != null && row.TryGetValue(key, out var v) ? Convert.ToString(v) : null;
    public static string NowText() => DateTime.UtcNow.ToString("O");
}
public sealed class DbTable
{
    public readonly List<DbRow> rows = new();
    public DbRow Find(Predicate<DbRow> match) => rows.Find(match);
    public long Insert(DbRow row)
    {
        long next = rows.Count + 1;
        row["id"] = next;
        if (!row.ContainsKey("gender")) row["gender"] = 10L;
        if (!row.ContainsKey("banned")) row["banned"] = 0L;
        if (!row.ContainsKey("characterslots")) row["characterslots"] = 3L;
        rows.Add(row);
        return next;
    }
    public void Update(Predicate<DbRow> match, DbRow changes)
    {
        foreach (DbRow row in rows.Where(r => match(r)))
            foreach (var pair in changes) row[pair.Key] = pair.Value;
    }
}
public static class GameConfigDao
{
    public static bool GetBool(DbStore db, string key, bool fallback = false) => db.Settings.TryGetValue(key, out var v) ? v : fallback;
}
public static class Program
{
    private static int checks;
    private static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception("FAIL: " + label);
        checks++;
        Console.WriteLine("PASS: " + label);
    }
    private static void Run()
    {
        var db = new DbStore();
        Check(!LoginService.IsNoPasswordAccount("admin"), "admin no longer exempt from password input");
        Check(LoginService.Login(db, "nobody", "password").code == LoginResultCode.NotRegistered, "registration defaults disabled");
        Check(LoginService.Login(db, "nobody", "").code == LoginResultCode.Invalid, "blank password rejected");
        db.Settings["automatic_register"] = true;
        var reg = LoginService.Login(db, "player", "a-strong-password");
        Check(reg.code == LoginResultCode.Ok && reg.justRegistered, "explicit auto-register works");
        Check(LoginService.Login(db, "player", "incorrect").code == LoginResultCode.WrongPassword, "wrong password rejected");
        Check(LoginService.Login(db, "player", "a-strong-password").code == LoginResultCode.Ok, "correct password accepted");
        int adminId = AccountsDao.AutoRegister(db, "admin", "a-secret-12!xy");
        Check(AccountsDao.DefaultGmLevel(db, adminId) == 0, "account name does not confer GM privilege");
        Check(LoginService.Login(db, "admin", "incorrect").code == LoginResultCode.WrongPassword, "admin bad password rejected");
        Check(LoginService.Login(db, "admin", "a-secret-12!xy").code == LoginResultCode.Ok, "admin correct password accepted");
        var account = AccountsDao.FindById(db, adminId);
        account["banned"] = 1L;
        Check(LoginService.Login(db, "admin", "a-secret-12!xy").code == LoginResultCode.Banned, "banned account refused");
        account["banned"] = 0L;
        account["tempban"] = DateTime.Now.AddMinutes(60).ToString("O");
        Check(LoginService.Login(db, "admin", "a-secret-12!xy").code == LoginResultCode.Banned, "temporary ban refused");
        account["tempban"] = "";
        Check(LoginService.Login(db, "admin", "a-secret-12!xy").code == LoginResultCode.Ok, "expired/cleared temporary ban accepted");
        Console.WriteLine("PASS: " + checks + " authentication assertions");
    }
    public static int Main()
    {
        try { Run(); return 0; }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
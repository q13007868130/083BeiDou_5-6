using System;
using System.Security.Cryptography;

public static class AccountsDao
{
	private const int Pbkdf2Iterations = 120000;

	private const int SaltBytes = 16;

	private const int HashBytes = 32;

	public const int GenderUnset = 10;

	public static string HashPassword(string pwd)
	{
		byte[] array = new byte[16];
		using (RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create())
		{
			randomNumberGenerator.GetBytes(array);
		}
		byte[] inArray = Pbkdf2(pwd, array);
		return "pbkdf2$" + 120000 + "$" + Convert.ToBase64String(array) + "$" + Convert.ToBase64String(inArray);
	}

	public static bool VerifyPassword(string pwd, string stored)
	{
		if (string.IsNullOrEmpty(stored))
		{
			return false;
		}
		string[] array = stored.Split('$');
		if (array.Length != 4 || array[0] != "pbkdf2")
		{
			return false;
		}
		if (!int.TryParse(array[1], out var result))
		{
			return false;
		}
		byte[] salt = Convert.FromBase64String(array[2]);
		byte[] array2 = Convert.FromBase64String(array[3]);
		return FixedTimeEquals(Pbkdf2(pwd, salt, result, array2.Length), array2);
	}

	private static byte[] Pbkdf2(string pwd, byte[] salt, int it = 120000, int len = 32)
	{
		using Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(pwd, salt, it, HashAlgorithmName.SHA256);
		return rfc2898DeriveBytes.GetBytes(len);
	}

	private static bool FixedTimeEquals(byte[] a, byte[] b)
	{
		if (a.Length != b.Length)
		{
			return false;
		}
		int num = 0;
		for (int i = 0; i < a.Length; i++)
		{
			num |= a[i] ^ b[i];
		}
		return num == 0;
	}

	public static DbRow FindByName(DbStore db, string name)
	{
		return db.Table("accounts").Find(DbStore.Eq("name", name));
	}

	public static DbRow FindById(DbStore db, int id)
	{
		return db.Table("accounts").Find(DbStore.Eq("id", (long)id));
	}

	public static int AutoRegister(DbStore db, string name, string pwd)
	{
		DbTable dbTable = db.Table("accounts");
		DbRow values = new DbRow
		{
			["name"] = name,
			["password"] = HashPassword(pwd),
			["createdat"] = DbStore.NowText()
		};
		return (int)dbTable.Insert(values);
	}

	public static int DefaultGmLevel(DbStore db, int accountId)
	{
		if (!string.Equals(DbStore.Str(FindById(db, accountId), "name"), "admin", StringComparison.Ordinal))
		{
			return 0;
		}
		return 6;
	}

	public static int Count(DbStore db)
	{
		return db.Table("accounts").rows.Count;
	}

	public static void UpdateLoginState(DbStore db, int accountId, bool loggedIn)
	{
		db.Table("accounts").Update(DbStore.Eq("id", (long)accountId), new DbRow
		{
			["loggedin"] = (long)(loggedIn ? 1 : 0),
			["lastlogin"] = DbStore.NowText()
		});
	}

	public static int CharacterSlots(DbStore db, int accountId)
	{
		DbRow dbRow = FindById(db, accountId);
		if (dbRow != null)
		{
			return (int)Convert.ToInt64(dbRow["characterslots"]);
		}
		return 3;
	}

	public static int Gender(DbStore db, int accountId)
	{
		return DbStore.Int(FindById(db, accountId), "gender", 10);
	}

	public static void SetGender(DbStore db, int accountId, int gender)
	{
		if (gender == 0 || gender == 1)
		{
			db.Table("accounts").Update(DbStore.Eq("id", (long)accountId), new DbRow { ["gender"] = (long)gender });
		}
	}
}

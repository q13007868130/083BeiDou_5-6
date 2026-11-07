using System;

public static class LoginService
{
	public const string NoPasswordAccount = "admin";

	public static bool IsNoPasswordAccount(string name)
	{
		// The original special-cased "admin" for no-password login. Never exempt accounts.
		return false;
	}

	public static LoginResult Login(DbStore db, string name, string pwd)
	{
		if (string.IsNullOrWhiteSpace(name) || string.IsNullOrEmpty(pwd))
		{
			return new LoginResult
			{
				code = LoginResultCode.Invalid
			};
		}
		DbRow dbRow = AccountsDao.FindByName(db, name);
		if (dbRow == null)
		{
			if (!GameConfigDao.GetBool(db, "automatic_register", fallback: false))
			{
				return new LoginResult
				{
					code = LoginResultCode.NotRegistered
				};
			}
			int accountId = AccountsDao.AutoRegister(db, name, pwd);
			AccountsDao.UpdateLoginState(db, accountId, loggedIn: true);
			return new LoginResult
			{
				code = LoginResultCode.Ok,
				accountId = accountId,
				accountName = name,
				justRegistered = true,
				gender = AccountsDao.Gender(db, accountId)
			};
		}
		int num = (int)Convert.ToInt64(dbRow["id"]);
		if (num <= 0)
		{
			return new LoginResult
			{
				code = LoginResultCode.Invalid
			};
		}
		if (DbStore.Int(dbRow, "banned") != 0)
		{
			return new LoginResult
			{
				code = LoginResultCode.Banned,
				accountId = num,
				accountName = name
			};
		}
		string stored = (string)dbRow["password"];
		if (!AccountsDao.VerifyPassword(pwd, stored))
		{
			return new LoginResult
			{
				code = LoginResultCode.WrongPassword,
				accountId = num,
				accountName = name
			};
		}
		if (IsTempBanned(DbStore.Str(dbRow, "tempban"), out var until))
		{
			return new LoginResult
			{
				code = LoginResultCode.Banned,
				accountId = num,
				accountName = name,
				banUntil = until
			};
		}
		AccountsDao.UpdateLoginState(db, num, loggedIn: true);
		return new LoginResult
		{
			code = LoginResultCode.Ok,
			accountId = num,
			accountName = name,
			gender = AccountsDao.Gender(db, num)
		};
	}

	public static int SetGender(DbStore db, int accountId, int gender)
	{
		int num = AccountsDao.Gender(db, accountId);
		if (num != 10)
		{
			return num;
		}
		if (gender != 0 && gender != 1)
		{
			return num;
		}
		AccountsDao.SetGender(db, accountId, gender);
		return AccountsDao.Gender(db, accountId);
	}

	private static bool IsTempBanned(string tempban, out string until)
	{
		until = null;
		if (string.IsNullOrEmpty(tempban))
		{
			return false;
		}
		if (!DateTime.TryParse(tempban, out var result) || result <= DateTime.Now)
		{
			return false;
		}
		until = result.ToString("yyyy-MM-dd HH:mm");
		return true;
	}

	public static void Logout(DbStore db, int accountId)
	{
		AccountsDao.UpdateLoginState(db, accountId, loggedIn: false);
	}
}
// Reconstructed from actual MapleDash IL; not the original Unity project.
// Only methods whose IL was fully recognized are included.
// Referenced project types and remaining members must be supplied separately.
using System;
using System.Collections.Generic;

public partial class LoginService
{
    // Original method token 0x060013e9; RVA 0xdf146
    public static void Logout(DbStore db, int accountId)
    {
        AccountsDao.UpdateLoginState(db, accountId, 0);
    }

}
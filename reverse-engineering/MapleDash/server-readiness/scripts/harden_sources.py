#!/usr/bin/env python3
"""Conservative, auditable security modifications of MapleDash decompiled C#.

Never overwrites forensic decompiled sources. Builds a separate hardening candidate.
"""
from pathlib import Path
import argparse

def patch(content: str, old: str, new: str, context: str) -> str:
    n=content.count(old)
    if n != 1:
        raise RuntimeError(f'{context}: expected exactly one match, found {n}')
    return content.replace(old,new,1)

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument('--source',type=Path,required=True,help='Path containing original ILSpy *.cs')
    ap.add_argument('--output',type=Path,required=True,help='Separate destination for patched copies')
    args=ap.parse_args(); args.output.mkdir(parents=True,exist_ok=True)
    login=(args.source/'LoginService.cs').read_text(encoding='utf-8')
    login=patch(login, '''\t\treturn string.Equals((name ?? "").Trim(), "admin", StringComparison.OrdinalIgnoreCase);''',
         '''\t\t// The original special-cased "admin" for no-password login. Never exempt accounts.\n\t\treturn false;''', 'login/passwordless')
    login=patch(login, '''\t\tif (string.IsNullOrEmpty(name))''', '''\t\tif (string.IsNullOrWhiteSpace(name) || string.IsNullOrEmpty(pwd))''', 'login/empty-credentials')
    login=patch(login, '''GameConfigDao.GetBool(db, "automatic_register", fallback: true)''', '''GameConfigDao.GetBool(db, "automatic_register", fallback: false)''','login/autoreg-fail-closed')
    login=patch(login, '''if (!IsNoPasswordAccount(name) && !AccountsDao.VerifyPassword(pwd, stored))''', '''if (!AccountsDao.VerifyPassword(pwd, stored))''','login/verify-all-accounts')
    (args.output/'LoginService.cs').write_text(login,encoding='utf-8')
    accounts=(args.source/'AccountsDao.cs').read_text(encoding='utf-8')
    accounts=patch(accounts, '''\t\tif (!string.Equals(DbStore.Str(FindById(db, accountId), "name"), "admin", StringComparison.Ordinal))\n\t\t{\n\t\t\treturn 0;\n\t\t}\n\t\treturn 6;''', '''\t\t// Names must never grant GM privileges. Configure permissions explicitly.\n\t\treturn 0;''','accounts/admin-gm')
    (args.output/'AccountsDao.cs').write_text(accounts,encoding='utf-8')
    admin=(args.source/'AdminBackend.cs').read_text(encoding='utf-8')
    # Existing webadmin_seed == 1 is NOT proof that the password is secure: administrators must rotate manually.
    admin=patch(admin, '''\t\t\tDbTable dbTable2 = db.Table("accounts");\n\t\t\tDbRow dbRow = AccountsDao.FindByName(db, "admin");''', '''\t\t\tstring bootstrapPassword = Environment.GetEnvironmentVariable("MAPLEDASH_ADMIN_BOOTSTRAP_PASSWORD");\n\t\t\tif (string.IsNullOrEmpty(bootstrapPassword) || bootstrapPassword.Length < 16)\n\t\t\t{\n\t\t\t\treturn "未设置 MAPLEDASH_ADMIN_BOOTSTRAP_PASSWORD（至少16位），不初始化管理员";\n\t\t\t}\n\t\t\tDbTable dbTable2 = db.Table("accounts");\n\t\t\tDbRow dbRow = AccountsDao.FindByName(db, "admin");''','admin/env-bootstrap')
    admin=patch(admin, '''AccountsDao.AutoRegister(db, "admin", "admin");''', '''AccountsDao.AutoRegister(db, "admin", bootstrapPassword);''','admin/register-bootstrap')
    admin=patch(admin, '''"已创建后台管理员账号 admin / admin"''', '''"已创建后台管理员账号 admin（凭据不写入日志）"''','admin/log-credential')
    admin=patch(admin, '''!AccountsDao.VerifyPassword("admin", DbStore.Str(dbRow, "password"))''', '''!AccountsDao.VerifyPassword(bootstrapPassword, DbStore.Str(dbRow, "password"))''','admin/check-bootstrap')
    admin=patch(admin, '''AccountsDao.HashPassword("admin")''', '''AccountsDao.HashPassword(bootstrapPassword)''','admin/hash-bootstrap')
    admin=patch(admin, '''"内置账号 admin 已存在，密码重置为 admin（首次初始化后台）"''', '''"内置管理员密码已根据首次配置更新（凭据不写入日志）"''','admin/reset-log')
    admin=patch(admin, '''"内置后台账号已就绪：admin / admin"''', '''"内置后台账号已就绪（凭据不写入日志）"''','admin/ready-log')
    (args.output/'AdminBackend.cs').write_text(admin,encoding='utf-8')
    print('Patched independent copies: LoginService.cs, AccountsDao.cs, AdminBackend.cs')

if __name__=='__main__':main()
using System.Collections.Generic;

public static class AdminRoutes
{
	public static void Register(AdminApi api)
	{
		RegisterAuth(api);
		RegisterAccount(api);
		RegisterCharacter(api);
		RegisterInventory(api);
		RegisterConfig(api);
		RegisterServer(api);
		AdminDrops.Register(api);
		AdminShop.Register(api);
		AdminGachapon.Register(api);
		AdminAutoban.Register(api);
		AdminCommands.Register(api);
		AdminGive.Register(api);
		AdminWeather.Register(api);
		AdminBots.Register(api);
	}

	public static void RegisterUnityOnly(AdminApi api)
	{
		AdminCashShop.Register(api);
		AdminFileTree.Register(api);
		AdminCatalog.Register(api);
		AdminQuestBrowse.Register(api);
		AdminSetItem.Register(api);
	}

	private static void RegisterAuth(AdminApi api)
	{
		api.Post("/auth/v1/login", (AdminCtx ctx) =>
		{
			string name = ctx.BodyString("username");
			string password = ctx.BodyString("password");
			AdminAccount adminAccount = AdminBackend.Authenticate(name, password);
			if (adminAccount == null)
			{
				throw new AdminApiException("账号或密码错误，或该账号没有后台权限（webadmin）");
			}
			return AdminJson.Obj("token", ctx.Api.CreateSession(adminAccount));
		}, auth: false);
		api.Delete("/auth/v1/logout", (AdminCtx ctx) =>
		{
			ctx.Api.DropSession(ctx.Token);
			return (object)null;
		}, auth: false);
		api.Get("/auth/v1/refreshToken", (AdminCtx ctx) => AdminJson.Obj("token", ctx.Token));
		api.Get("/account/v1/info", (AdminCtx ctx) => AdminBackend.AccountInfo(ctx.User.Id) ?? throw new AdminApiException("后台账号已被删除"));
		api.Get("/account/v1/menu", (AdminCtx ctx) => new List<object>());
	}

	private static void RegisterAccount(AdminApi api)
	{
		api.Get("/account/v1", (AdminCtx ctx) => AdminBackend.AccountPage(ctx.QueryInt("page", 1), ctx.QueryInt("size", 20), ctx.QueryLong("id", 0L), ctx.Query("name"), ctx.Query("lastLoginStart"), ctx.Query("lastLoginEnd"), ctx.Query("createdAtStart"), ctx.Query("createdAtEnd")));
		api.Get("/account/v1/{id}/characters/maps", (AdminCtx ctx) => AdminBackend.CharacterMaps(ctx.ParamLong("id")));
		api.Post("/account/v1", (AdminCtx ctx) =>
		{
			bool flag = AdminJson.AsDict(ctx.Body)?.ContainsKey("language") ?? false;
			AdminBackend.AddAccount(ctx.BodyString("name"), ctx.BodyString("password"), ctx.BodyString("birthday"), flag ? new long?(ctx.BodyLong("language", 0L)) : ((long?)null));
			return (object)null;
		});
		api.Put("/account/v1/{id}", (AdminCtx ctx) =>
		{
			AdminBackend.UpdateAccount(ctx.ParamLong("id"), AdminJson.AsDict(ctx.Body));
			return (object)null;
		});
		api.Delete("/account/v1/{id}", (AdminCtx ctx) =>
		{
			AdminBackend.DeleteAccount(ctx.ParamLong("id"));
			return (object)null;
		});
		api.Put("/account/v1/{id}/ban", (AdminCtx ctx) =>
		{
			AdminBackend.BanAccount(ctx.ParamLong("id"), ctx.BodyString("reason"));
			return (object)null;
		});
		api.Put("/account/v1/{id}/unban", (AdminCtx ctx) =>
		{
			AdminBackend.UnbanAccount(ctx.ParamLong("id"));
			return (object)null;
		});
		api.Put("/account/v1/{id}/reset/logged", (AdminCtx ctx) =>
		{
			AdminBackend.ResetLoggedIn(ctx.ParamLong("id"));
			return (object)null;
		});
		api.Put("/account/v1/{id}/moveToHenesys", (AdminCtx ctx) =>
		{
			AdminBackend.MoveAccountToHenesys(ctx.ParamLong("id"));
			return (object)null;
		});
	}

	private static void RegisterCharacter(AdminApi api)
	{
		api.Post("/character/v1/online/list", (AdminCtx ctx) => AdminBackend.OnlineCharacterPage(ctx.BodyInt("pageNo", 1), ctx.BodyInt("pageSize", 20), ctx.BodyLong("id", 0L), ctx.BodyString("name"), ctx.BodyLong("map", 0L)));
		api.Put("/character/v1/{id}/moveToHenesys", (AdminCtx ctx) =>
		{
			AdminBackend.MoveCharacterToHenesys(ctx.ParamLong("id"));
			return (object)null;
		});
		api.Put("/character/v1/moveToHenesys", (AdminCtx ctx) => AdminBackend.MoveCharactersToHenesys(ctx.BodyLong("id", 0L), ctx.BodyString("name")));
		api.Put("/character/v1/{id}/changeName", (AdminCtx ctx) =>
		{
			AdminBackend.ChangeCharacterName(ctx.ParamLong("id"), ctx.BodyString("newName"));
			return (object)null;
		});
	}

	private static void RegisterInventory(AdminApi api)
	{
		api.Get("/inventory/v1/getInventoryTypeList", (AdminCtx ctx) => AdminBackend.InventoryTypes());
		api.Post("/inventory/v1/getCharacterList", (AdminCtx ctx) => AdminBackend.InventoryCharacterPage(ctx.BodyLong("characterId", 0L), ctx.BodyString("characterName"), ctx.BodyLong("accountId", 0L), ctx.BodyInt("pageNo", 1), ctx.BodyInt("pageSize", 20)));
		api.Post("/inventory/v1/getInventoryList", (AdminCtx ctx) => AdminBackend.InventoryPage(ctx.BodyLong("characterId", 0L), ctx.BodyLong("accountId", 0L), ctx.BodyInt("inventoryType"), ctx.BodyInt("pageNo", 1), ctx.BodyInt("pageSize", 20)));
		api.Post("/inventory/v1/updateInventory", (AdminCtx ctx) =>
		{
			AdminBackend.UpdateInventory(AdminJson.AsDict(ctx.Body));
			return (object)null;
		});
		api.Post("/inventory/v1/deleteInventory", (AdminCtx ctx) =>
		{
			AdminBackend.DeleteInventory(AdminJson.AsDict(ctx.Body));
			return (object)null;
		});
	}

	private static void RegisterConfig(AdminApi api)
	{
		api.Get("/config/v1/getConfigTypeList", (AdminCtx ctx) => AdminBackend.ConfigTypeList());
		api.Post("/config/v1/getConfigList", (AdminCtx ctx) => AdminBackend.ConfigPage(ctx.BodyString("type"), ctx.BodyString("subType"), ctx.BodyString("filter"), ctx.BodyInt("pageNo", 1), ctx.BodyInt("pageSize", 20)));
		api.Post("/config/v1/addConfig", (AdminCtx ctx) =>
		{
			AdminBackend.AddConfig(AdminJson.AsDict(ctx.Body));
			return (object)null;
		});
		api.Post("/config/v1/updateConfig", (AdminCtx ctx) =>
		{
			AdminBackend.UpdateConfig(AdminJson.AsDict(ctx.Body));
			return (object)null;
		});
		api.Delete("/config/v1/deleteConfig/{id}", (AdminCtx ctx) =>
		{
			AdminBackend.DeleteConfig(ctx.ParamLong("id"));
			return (object)null;
		});
		api.Post("/config/v1/deleteConfigList", (AdminCtx ctx) => AdminBackend.DeleteConfigList(AdminJson.AsList(ctx.Body) ?? throw new AdminApiException("请选择要删除的参数")));
	}

	private static void RegisterServer(AdminApi api)
	{
		api.Get("/server/v1/online", (AdminCtx ctx) => AdminBackend.Ready);
		api.Get("/server/v1/version", (AdminCtx ctx) => AdminBackend.Version);
		api.Get("/server/v1/startServer", (AdminCtx ctx) =>
		{
			throw new AdminApiException("单机版没有独立服务端进程，无需启动");
		});
		api.Get("/server/v1/restartServer", (AdminCtx ctx) =>
		{
			throw new AdminApiException("单机版没有独立服务端进程，无法重启");
		});
		api.Get("/server/v1/shutdown", (AdminCtx ctx) =>
		{
			throw new AdminApiException("单机版请直接退出游戏");
		});
		api.Post("/server/v1/stopServerWithMsgAndInternal", (AdminCtx ctx) =>
		{
			throw new AdminApiException("单机版没有独立服务端进程，无法停服");
		});
		api.Post("/server/v1/broadcast", (AdminCtx ctx) =>
		{
			string text = ctx.BodyString("message");
			if (string.IsNullOrEmpty(text))
			{
				throw new AdminApiException("广播内容不能为空");
			}
			AdminBackend.Broadcast?.Invoke(text);
			return (object)null;
		});
	}
}

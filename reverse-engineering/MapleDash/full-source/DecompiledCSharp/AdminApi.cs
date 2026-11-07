using System;
using System.Collections.Generic;

public sealed class AdminApi
{
	private sealed class Route
	{
		public string Method;

		public string[] Segments;

		public AdminHandler Handler;

		public bool RequireAuth;
	}

	private sealed class Session
	{
		public AdminAccount Account;

		public DateTime Expires;
	}

	private readonly List<Route> routes = new List<Route>();

	private readonly Dictionary<string, Session> sessions = new Dictionary<string, Session>(StringComparer.Ordinal);

	private readonly object sessionGate = new object();

	private readonly HashSet<string> stubLogged = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	public AdminStaticBundle Statics;

	public Action<string> Log;

	public bool LogStubs = true;

	public void Get(string pattern, AdminHandler h, bool auth = true)
	{
		Add("GET", pattern, h, auth);
	}

	public void Post(string pattern, AdminHandler h, bool auth = true)
	{
		Add("POST", pattern, h, auth);
	}

	public void Put(string pattern, AdminHandler h, bool auth = true)
	{
		Add("PUT", pattern, h, auth);
	}

	public void Delete(string pattern, AdminHandler h, bool auth = true)
	{
		Add("DELETE", pattern, h, auth);
	}

	private void Add(string method, string pattern, AdminHandler handler, bool auth)
	{
		string[] segments = pattern.Trim('/').Split('/');
		routes.Add(new Route
		{
			Method = method,
			Segments = segments,
			Handler = handler,
			RequireAuth = auth
		});
	}

	public string CreateSession(AdminAccount acc)
	{
		string text = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N").Substring(0, 8);
		lock (sessionGate)
		{
			sessions[text] = new Session
			{
				Account = acc,
				Expires = DateTime.UtcNow.AddHours(12.0)
			};
			return text;
		}
	}

	public void DropSession(string token)
	{
		if (string.IsNullOrEmpty(token))
		{
			return;
		}
		lock (sessionGate)
		{
			sessions.Remove(token);
		}
	}

	private AdminAccount Resolve(string token)
	{
		if (string.IsNullOrEmpty(token))
		{
			return null;
		}
		lock (sessionGate)
		{
			if (!sessions.TryGetValue(token, out var value))
			{
				return null;
			}
			if (value.Expires < DateTime.UtcNow)
			{
				sessions.Remove(token);
				return null;
			}
			value.Expires = DateTime.UtcNow.AddHours(12.0);
			return value.Account;
		}
	}

	private static string TokenOf(AdminHttpRequest req)
	{
		string text = req.Header("Authorization");
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		if (text.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
		{
			return text.Substring(7).Trim();
		}
		return text.Trim();
	}

	public AdminHttpResponse Handle(AdminHttpRequest req)
	{
		if (req.Method == "OPTIONS")
		{
			return new AdminHttpResponse
			{
				Status = 204,
				Body = Array.Empty<byte>()
			};
		}
		string[] array = req.Path.Trim('/').Split('/');
		bool flag = false;
		foreach (Route route in routes)
		{
			if (route.Method != req.Method || route.Segments.Length != array.Length)
			{
				continue;
			}
			Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			bool flag2 = true;
			for (int i = 0; i < array.Length; i++)
			{
				string text = route.Segments[i];
				if (text.Length > 2 && text[0] == '{' && text[text.Length - 1] == '}')
				{
					dictionary[text.Substring(1, text.Length - 2)] = Uri.UnescapeDataString(array[i]);
				}
				else if (!string.Equals(text, array[i], StringComparison.OrdinalIgnoreCase))
				{
					flag2 = false;
					break;
				}
			}
			if (!flag2)
			{
				continue;
			}
			flag = true;
			AdminCtx adminCtx = new AdminCtx
			{
				Api = this,
				Req = req,
				Token = TokenOf(req)
			};
			foreach (KeyValuePair<string, string> item in dictionary)
			{
				adminCtx.Params[item.Key] = item.Value;
			}
			if (route.RequireAuth)
			{
				adminCtx.User = Resolve(adminCtx.Token);
				if (adminCtx.User == null)
				{
					return AdminHttpResponse.Json(AdminJson.Fail("登录已过期，请重新登录", 401), 401);
				}
			}
			if (req.BodyText.Length > 0)
			{
				try
				{
					adminCtx.RawBody = AdminJson.Parse(req.BodyText);
					adminCtx.Body = AdminJson.UnwrapSubmit(adminCtx.RawBody);
				}
				catch (Exception ex)
				{
					return AdminHttpResponse.Json(AdminJson.Fail("请求体不是合法 JSON: " + ex.Message), 400);
				}
			}
			try
			{
				object obj = route.Handler(adminCtx);
				if (obj is AdminHttpResponse result)
				{
					return result;
				}
				return AdminHttpResponse.Json(AdminJson.Ok(obj));
			}
			catch (AdminApiException ex2)
			{
				return AdminHttpResponse.Json(AdminJson.Fail(ex2.Message));
			}
			catch (Exception ex3)
			{
				Log?.Invoke("接口异常 " + req.Method + " " + req.Path + " : " + ex3);
				return AdminHttpResponse.Json(AdminJson.Fail(ex3.Message), 500);
			}
		}
		if (flag)
		{
			return AdminHttpResponse.Json(AdminJson.Fail("方法不允许: " + req.Method, 405), 405);
		}
		if (Statics != null && Statics.TryGetExact(req.Path, out var file))
		{
			return AdminHttpResponse.Bytes(file.Data, file.ContentType);
		}
		if (LooksLikeApi(req.Path))
		{
			if (LogStubs)
			{
				lock (stubLogged)
				{
					if (stubLogged.Add(req.Method + " " + req.Path))
					{
						Log?.Invoke("后台接口未接入（返回空数据）: " + req.Method + " " + req.Path);
					}
				}
			}
			return AdminHttpResponse.Json(AdminJson.Ok());
		}
		if (Statics != null && Statics.TryGet(req.Path, out var file2))
		{
			return AdminHttpResponse.Bytes(file2.Data, file2.ContentType);
		}
		return AdminHttpResponse.Json(AdminJson.Fail("找不到资源: " + req.Path, 404), 404);
	}

	private static bool LooksLikeApi(string path)
	{
		if (path.StartsWith("/assets/", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (!path.Contains("/v1/") && !path.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
		{
			return path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}
}

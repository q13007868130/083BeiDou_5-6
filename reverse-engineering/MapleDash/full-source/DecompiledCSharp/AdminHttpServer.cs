using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public sealed class AdminHttpServer : IDisposable
{
	public const int MaxBodyBytes = 16777216;

	private readonly Func<AdminHttpRequest, AdminHttpResponse> handler;

	private readonly Action<string> log;

	private TcpListener listener;

	private Thread acceptThread;

	private volatile bool running;

	public int Port { get; private set; }

	public bool Running => running;

	public AdminHttpServer(int port, Func<AdminHttpRequest, AdminHttpResponse> handler, Action<string> log = null)
	{
		Port = port;
		this.handler = handler;
		this.log = log;
	}

	public static AdminHttpServer StartOn(int firstPort, Func<AdminHttpRequest, AdminHttpResponse> handler, Action<string> log = null, int attempts = 10)
	{
		Exception innerException = null;
		for (int i = 0; i < attempts; i++)
		{
			AdminHttpServer adminHttpServer = new AdminHttpServer(firstPort + i, handler, log);
			try
			{
				adminHttpServer.Start();
				return adminHttpServer;
			}
			catch (Exception ex)
			{
				innerException = ex;
				adminHttpServer.Dispose();
			}
		}
		throw new InvalidOperationException("本地端口全部被占用（" + firstPort + ".." + (firstPort + attempts - 1) + "）", innerException);
	}

	public void Start()
	{
		if (!running)
		{
			listener = new TcpListener(IPAddress.Loopback, Port);
			listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, optionValue: true);
			listener.Start(64);
			Port = ((IPEndPoint)listener.LocalEndpoint).Port;
			running = true;
			acceptThread = new Thread(AcceptLoop)
			{
				IsBackground = true,
				Name = "AdminHttpAccept"
			};
			acceptThread.Start();
			log?.Invoke("后台管理服务已启动: http://127.0.0.1:" + Port + "/");
		}
	}

	private void AcceptLoop()
	{
		while (running)
		{
			TcpClient tcpClient = null;
			try
			{
				tcpClient = listener.AcceptTcpClient();
			}
			catch (Exception)
			{
				if (!running)
				{
					break;
				}
				continue;
			}
			TcpClient c = tcpClient;
			ThreadPool.QueueUserWorkItem((object _) =>
			{
				HandleClient(c);
			});
		}
	}

	private void HandleClient(TcpClient client)
	{
		try
		{
			using (client)
			{
				client.ReceiveTimeout = 15000;
				client.SendTimeout = 15000;
				client.NoDelay = true;
				using NetworkStream stream = client.GetStream();
				AdminHttpRequest adminHttpRequest = ReadRequest(stream);
				if (adminHttpRequest != null)
				{
					AdminHttpResponse res;
					try
					{
						res = handler(adminHttpRequest) ?? AdminHttpResponse.Json(AdminJson.Fail("空响应"), 500);
					}
					catch (Exception ex)
					{
						log?.Invoke("处理请求出错 " + adminHttpRequest.Method + " " + adminHttpRequest.RawPath + " : " + ex.Message);
						res = AdminHttpResponse.Json(AdminJson.Fail(ex.Message), 500);
					}
					WriteResponse(stream, adminHttpRequest, res);
				}
			}
		}
		catch (Exception ex2)
		{
			log?.Invoke("连接处理失败: " + ex2.Message);
		}
	}

	private static AdminHttpRequest ReadRequest(NetworkStream stream)
	{
		string text = ReadHead(stream, out var carry);
		if (text == null)
		{
			return null;
		}
		int num = text.IndexOf("\r\n", StringComparison.Ordinal);
		string[] array = ((num < 0) ? text : text.Substring(0, num)).Split(' ');
		if (array.Length < 2)
		{
			return null;
		}
		AdminHttpRequest adminHttpRequest = new AdminHttpRequest
		{
			Method = array[0].ToUpperInvariant()
		};
		string text2 = array[1];
		int num2 = text2.IndexOf('?');
		adminHttpRequest.RawPath = ((num2 < 0) ? text2 : text2.Substring(0, num2));
		adminHttpRequest.RawQuery = ((num2 < 0) ? "" : text2.Substring(num2 + 1));
		adminHttpRequest.Path = DecodePath(adminHttpRequest.RawPath);
		ParseQuery(adminHttpRequest.RawQuery, adminHttpRequest.Query);
		int num3 = ((num < 0) ? text.Length : (num + 2));
		while (num3 < text.Length)
		{
			int num4 = text.IndexOf("\r\n", num3, StringComparison.Ordinal);
			if (num4 < 0)
			{
				num4 = text.Length;
			}
			string text3 = text.Substring(num3, num4 - num3);
			num3 = num4 + 2;
			if (text3.Length == 0)
			{
				break;
			}
			int num5 = text3.IndexOf(':');
			if (num5 > 0)
			{
				string key = text3.Substring(0, num5).Trim();
				string value = text3.Substring(num5 + 1).Trim();
				adminHttpRequest.Headers[key] = value;
			}
		}
		if (int.TryParse(adminHttpRequest.Header("Content-Length"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) && result > 0)
		{
			if (result > 16777216)
			{
				throw new InvalidOperationException("请求体过大");
			}
			byte[] array2 = new byte[result];
			int i = 0;
			if (carry != null && carry.Length != 0)
			{
				i = Math.Min(result, carry.Length);
				Array.Copy(carry, 0, array2, 0, i);
			}
			int num6;
			for (; i < result; i += num6)
			{
				num6 = stream.Read(array2, i, result - i);
				if (num6 <= 0)
				{
					break;
				}
			}
			if (i != result)
			{
				Array.Resize(ref array2, i);
			}
			adminHttpRequest.BodyBytes = array2;
			adminHttpRequest.BodyText = Encoding.UTF8.GetString(array2);
		}
		return adminHttpRequest;
	}

	private static string ReadHead(NetworkStream stream, out byte[] carry)
	{
		carry = null;
		List<byte> list = new List<byte>(1024);
		byte[] array = new byte[4096];
		int num = -1;
		while (list.Count < 65536)
		{
			int num2 = stream.Read(array, 0, array.Length);
			if (num2 <= 0)
			{
				break;
			}
			list.AddRange(new ArraySegment<byte>(array, 0, num2));
			num = FindHeadEnd(list);
			if (num >= 0)
			{
				break;
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		if (num < 0)
		{
			num = list.Count;
		}
		byte[] bytes = list.GetRange(0, num).ToArray();
		int num3 = Math.Min(list.Count, num + 4);
		int num4 = list.Count - num3;
		if (num4 > 0)
		{
			carry = list.GetRange(num3, num4).ToArray();
		}
		return Encoding.UTF8.GetString(bytes);
	}

	private static int FindHeadEnd(List<byte> data)
	{
		for (int i = 0; i + 3 < data.Count; i++)
		{
			if (data[i] == 13 && data[i + 1] == 10 && data[i + 2] == 13 && data[i + 3] == 10)
			{
				return i;
			}
		}
		return -1;
	}

	private static string DecodePath(string raw)
	{
		if (string.IsNullOrEmpty(raw))
		{
			return "/";
		}
		try
		{
			return Uri.UnescapeDataString(raw);
		}
		catch (Exception)
		{
			return raw;
		}
	}

	private static void ParseQuery(string raw, Dictionary<string, string> into)
	{
		if (string.IsNullOrEmpty(raw))
		{
			return;
		}
		string[] array = raw.Split('&');
		foreach (string text in array)
		{
			if (text.Length != 0)
			{
				int num = text.IndexOf('=');
				string text2 = ((num < 0) ? text : text.Substring(0, num));
				string text3 = ((num < 0) ? "" : text.Substring(num + 1));
				try
				{
					text2 = Uri.UnescapeDataString(text2.Replace('+', ' '));
					text3 = Uri.UnescapeDataString(text3.Replace('+', ' '));
				}
				catch (Exception)
				{
				}
				if (text2.Length > 0)
				{
					into[text2] = text3;
				}
			}
		}
	}

	private static void WriteResponse(NetworkStream stream, AdminHttpRequest req, AdminHttpResponse res)
	{
		StringBuilder stringBuilder = new StringBuilder(256);
		stringBuilder.Append("HTTP/1.1 ").Append(res.Status).Append(' ')
			.Append(StatusText(res.Status))
			.Append("\r\n");
		stringBuilder.Append("Content-Type: ").Append(res.ContentType).Append("\r\n");
		stringBuilder.Append("Content-Length: ").Append(res.Body?.Length ?? 0).Append("\r\n");
		stringBuilder.Append("Cache-Control: no-store\r\n");
		stringBuilder.Append("Access-Control-Allow-Origin: *\r\n");
		stringBuilder.Append("Access-Control-Allow-Headers: Authorization, Content-Type\r\n");
		stringBuilder.Append("Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS\r\n");
		foreach (KeyValuePair<string, string> header in res.Headers)
		{
			stringBuilder.Append(header.Key).Append(": ").Append(header.Value)
				.Append("\r\n");
		}
		stringBuilder.Append("Connection: close\r\n\r\n");
		byte[] bytes = Encoding.ASCII.GetBytes(stringBuilder.ToString());
		stream.Write(bytes, 0, bytes.Length);
		if (res.Body != null && res.Body.Length != 0 && req.Method != "HEAD")
		{
			stream.Write(res.Body, 0, res.Body.Length);
		}
		stream.Flush();
	}

	private static string StatusText(int code)
	{
		return code switch
		{
			200 => "OK", 
			204 => "No Content", 
			301 => "Moved Permanently", 
			302 => "Found", 
			304 => "Not Modified", 
			400 => "Bad Request", 
			401 => "Unauthorized", 
			403 => "Forbidden", 
			404 => "Not Found", 
			405 => "Method Not Allowed", 
			411 => "Length Required", 
			413 => "Payload Too Large", 
			500 => "Internal Server Error", 
			501 => "Not Implemented", 
			503 => "Service Unavailable", 
			_ => "OK", 
		};
	}

	public void Dispose()
	{
		running = false;
		try
		{
			listener?.Stop();
		}
		catch (Exception)
		{
		}
		listener = null;
	}
}

using System;
using System.Collections.Generic;
using System.Text;

public sealed class AdminHttpResponse
{
	public int Status = 200;

	public string ContentType = "application/json; charset=utf-8";

	public byte[] Body = Array.Empty<byte>();

	public readonly Dictionary<string, string> Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	public static AdminHttpResponse Json(object payload, int status = 200)
	{
		return new AdminHttpResponse
		{
			Status = status,
			ContentType = "application/json; charset=utf-8",
			Body = Encoding.UTF8.GetBytes(AdminJson.Write(payload))
		};
	}

	public static AdminHttpResponse Text(string text, int status = 200, string type = "text/plain; charset=utf-8")
	{
		return new AdminHttpResponse
		{
			Status = status,
			ContentType = type,
			Body = Encoding.UTF8.GetBytes(text ?? "")
		};
	}

	public static AdminHttpResponse Bytes(byte[] data, string type, int status = 200)
	{
		return new AdminHttpResponse
		{
			Status = status,
			ContentType = type,
			Body = (data ?? Array.Empty<byte>())
		};
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public sealed class AdminStaticBundle
{
	public const string Magic = "MDUIB1";

	private readonly Dictionary<string, AdminStaticFile> files = new Dictionary<string, AdminStaticFile>(StringComparer.OrdinalIgnoreCase);

	public int Count => files.Count;

	public IEnumerable<string> Names => files.Keys;

	public static AdminStaticBundle Load(byte[] raw)
	{
		if (raw == null || raw.Length < 10)
		{
			throw new InvalidDataException("后台前端资源包为空");
		}
		AdminStaticBundle adminStaticBundle = new AdminStaticBundle();
		using MemoryStream input = new MemoryStream(raw, writable: false);
		using BinaryReader binaryReader = new BinaryReader(input, Encoding.UTF8);
		string text = Encoding.ASCII.GetString(binaryReader.ReadBytes("MDUIB1".Length));
		if (text != "MDUIB1")
		{
			throw new InvalidDataException("后台前端资源包标识不匹配: " + text);
		}
		int num = binaryReader.ReadInt32();
		for (int i = 0; i < num; i++)
		{
			int count = binaryReader.ReadUInt16();
			string path = Encoding.UTF8.GetString(binaryReader.ReadBytes(count));
			int count2 = binaryReader.ReadInt32();
			byte[] data = binaryReader.ReadBytes(count2);
			string text2 = Normalize(path);
			adminStaticBundle.files[text2] = new AdminStaticFile
			{
				Name = text2,
				Data = data,
				ContentType = ContentTypeOf(text2)
			};
		}
		return adminStaticBundle;
	}

	public static string Normalize(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return "index.html";
		}
		string text = path.Replace('\\', '/');
		while (text.StartsWith("/", StringComparison.Ordinal))
		{
			text = text.Substring(1);
		}
		if (text.Length == 0)
		{
			return "index.html";
		}
		return text;
	}

	public bool TryGet(string path, out AdminStaticFile file)
	{
		string text = Normalize(path);
		if (files.TryGetValue(text, out file))
		{
			return true;
		}
		if (!text.Contains(".") && files.TryGetValue("index.html", out file))
		{
			return true;
		}
		file = null;
		return false;
	}

	public bool TryGetExact(string path, out AdminStaticFile file)
	{
		return files.TryGetValue(Normalize(path), out file);
	}

	public static string ContentTypeOf(string name)
	{
		int num = name.LastIndexOf('.');
		switch ((num < 0) ? "" : name.Substring(num).ToLowerInvariant())
		{
		case ".html":
			return "text/html; charset=utf-8";
		case ".js":
		case ".mjs":
			return "application/javascript; charset=utf-8";
		case ".css":
			return "text/css; charset=utf-8";
		case ".json":
			return "application/json; charset=utf-8";
		case ".svg":
			return "image/svg+xml";
		case ".png":
			return "image/png";
		case ".jpg":
		case ".jpeg":
			return "image/jpeg";
		case ".gif":
			return "image/gif";
		case ".webp":
			return "image/webp";
		case ".ico":
			return "image/x-icon";
		case ".woff":
			return "font/woff";
		case ".woff2":
			return "font/woff2";
		case ".ttf":
			return "font/ttf";
		case ".eot":
			return "application/vnd.ms-fontobject";
		case ".map":
			return "application/json; charset=utf-8";
		case ".txt":
			return "text/plain; charset=utf-8";
		default:
			return "application/octet-stream";
		}
	}
}

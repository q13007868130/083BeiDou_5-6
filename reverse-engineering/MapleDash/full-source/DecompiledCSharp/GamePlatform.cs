using System;
using System.IO;
using UnityEngine;

public static class GamePlatform
{
	public const string WindowsResourceRoot = "D:\\mapstory";

	private static string resourceRoot;

	public static bool DesktopPlayer => true;

	public static bool TouchControls => !DesktopPlayer;

	public static bool MoviePlayback => false;

	public static string ResourceRoot
	{
		get
		{
			if (resourceRoot == null)
			{
				resourceRoot = ResolveResourceRoot();
			}
			return resourceRoot;
		}
	}

	public static string DataRoot => Path.Combine(ResourceRoot, "Data");

	private static string ResolveResourceRoot()
	{
		string environmentVariable = Environment.GetEnvironmentVariable("MAPLEDASH_RESOURCE_ROOT");
		if (!string.IsNullOrEmpty(environmentVariable))
		{
			return Path.GetFullPath(environmentVariable);
		}
		return ResolveStandaloneRoot();
	}

	private static string ResolveStandaloneRoot()
	{
		if (HasDrive("D"))
		{
			return "D:\\mapstory";
		}
		string directoryName = Path.GetDirectoryName(Application.dataPath);
		string text = Path.Combine(string.IsNullOrEmpty(directoryName) ? "." : directoryName, "mapstory");
		Debug.LogWarning((object)("[资源] 未找到 D 盘，资源目录改用 " + text + "（约定路径为 D:\\mapstory）"));
		return text;
	}

	private static bool HasDrive(string letter)
	{
		try
		{
			DriveInfo[] drives = DriveInfo.GetDrives();
			foreach (DriveInfo driveInfo in drives)
			{
				if (driveInfo.DriveType != DriveType.CDRom && driveInfo.DriveType != DriveType.NoRootDirectory && (string.Equals(driveInfo.Name, letter + ":" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || string.Equals(driveInfo.Name, letter + ":", StringComparison.OrdinalIgnoreCase)))
				{
					return true;
				}
			}
		}
		catch (Exception)
		{
		}
		return false;
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Colossal;
using JetBrains.Annotations;
using Microsoft.Win32;
using UnityEngine;

namespace Game.Modding.Toolchain.Dependencies;

internal static class RiderPathLocator
{
	[Serializable]
	private class SettingsJson
	{
		public string install_location;

		[CanBeNull]
		public static string GetInstallLocationFromJson(string json)
		{
			try
			{
				return JsonUtility.FromJson<SettingsJson>(json).install_location;
			}
			catch (Exception)
			{
				Logger.Warn("Failed to get install_location from json " + json);
			}
			return null;
		}
	}

	[Serializable]
	private class ToolboxHistory
	{
		public List<ItemNode> history;

		[CanBeNull]
		public static string GetLatestBuildFromJson(string json)
		{
			try
			{
				return JsonUtility.FromJson<ToolboxHistory>(json).history.LastOrDefault()?.item.build;
			}
			catch (Exception)
			{
				Logger.Warn("Failed to get latest build from json " + json);
			}
			return null;
		}
	}

	[Serializable]
	private class ItemNode
	{
		public BuildNode item;
	}

	[Serializable]
	private class BuildNode
	{
		public string build;
	}

	[Serializable]
	internal class ProductInfo
	{
		public string version;

		public string versionSuffix;

		[CanBeNull]
		internal static ProductInfo GetProductInfo(string json)
		{
			try
			{
				return JsonUtility.FromJson<ProductInfo>(json);
			}
			catch (Exception)
			{
				Logger.Warn("Failed to get version from json " + json);
			}
			return null;
		}
	}

	[Serializable]
	private class ToolboxInstallData
	{
		public ActiveApplication active_application;

		[CanBeNull]
		public static string GetLatestBuildFromJson(string json)
		{
			try
			{
				List<string> builds = JsonUtility.FromJson<ToolboxInstallData>(json).active_application.builds;
				if (builds != null && builds.Any())
				{
					return builds.First();
				}
			}
			catch (Exception)
			{
				Logger.Warn("Failed to get latest build from json " + json);
			}
			return null;
		}
	}

	[Serializable]
	private class ActiveApplication
	{
		public List<string> builds;
	}

	internal struct RiderInfo
	{
		public bool IsToolbox;

		public string Presentation;

		public System.Version BuildNumber;

		public ProductInfo ProductInfo;

		public string Path;

		public RiderInfo(string path, bool isToolbox)
		{
			BuildNumber = GetBuildNumber(path);
			ProductInfo = GetBuildVersion(path);
			Path = new FileInfo(path).FullName;
			string text = $"Rider {BuildNumber}";
			if (ProductInfo != null && !string.IsNullOrEmpty(ProductInfo.version))
			{
				string text2 = (string.IsNullOrEmpty(ProductInfo.versionSuffix) ? "" : (" " + ProductInfo.versionSuffix));
				text = "Rider " + ProductInfo.version + text2;
			}
			if (isToolbox)
			{
				text += " (JetBrains Toolbox)";
			}
			Presentation = text;
			IsToolbox = isToolbox;
		}
	}

	private static class Logger
	{
		internal static void Warn(string message, Exception e = null)
		{
			UnityEngine.Debug.LogError(message);
			if (e != null)
			{
				UnityEngine.Debug.LogException(e);
			}
		}
	}

	public static RiderInfo[] GetAllRiderPaths()
	{
		try
		{
			switch (PlatformInfo.operatingSystemFamily)
			{
			case OperatingSystemFamily.Windows:
				return CollectRiderInfosWindows();
			case OperatingSystemFamily.MacOSX:
				return CollectRiderInfosMac();
			case OperatingSystemFamily.Linux:
				return CollectAllRiderPathsLinux();
			}
		}
		catch (Exception exception)
		{
			UnityEngine.Debug.LogException(exception);
		}
		return Array.Empty<RiderInfo>();
	}

	private static RiderInfo[] CollectAllRiderPathsLinux()
	{
		List<RiderInfo> list = new List<RiderInfo>();
		string environmentVariable = Environment.GetEnvironmentVariable("HOME");
		if (!string.IsNullOrEmpty(environmentVariable))
		{
			string toolboxBaseDir = GetToolboxBaseDir();
			list.AddRange((from a in CollectPathsFromToolbox(toolboxBaseDir, "bin", "rider.sh", isMac: false)
				select new RiderInfo(a, isToolbox: true)).ToList());
			FileInfo fileInfo = new FileInfo(Path.Combine(environmentVariable, ".local/share/applications/jetbrains-rider.desktop"));
			if (fileInfo.Exists)
			{
				string[] array = File.ReadAllLines(fileInfo.FullName);
				foreach (string text in array)
				{
					if (text.StartsWith("Exec=\""))
					{
						string path = text.Split('"').Where((string item, int index) => index == 1).SingleOrDefault();
						if (!string.IsNullOrEmpty(path) && !list.Any((RiderInfo a) => a.Path == path))
						{
							list.Add(new RiderInfo(path, isToolbox: false));
						}
					}
				}
			}
		}
		string text2 = "/snap/rider/current/bin/rider.sh";
		if (new FileInfo(text2).Exists)
		{
			list.Add(new RiderInfo(text2, isToolbox: false));
		}
		return list.ToArray();
	}

	private static RiderInfo[] CollectRiderInfosMac()
	{
		List<RiderInfo> list = new List<RiderInfo>();
		DirectoryInfo directoryInfo = new DirectoryInfo("/Applications");
		if (directoryInfo.Exists)
		{
			list.AddRange((from a in directoryInfo.GetDirectories("*Rider*.app")
				select new RiderInfo(a.FullName, isToolbox: false)).ToList());
		}
		IEnumerable<RiderInfo> collection = from a in CollectPathsFromToolbox(GetToolboxBaseDir(), "", "Rider*.app", isMac: true)
			select new RiderInfo(a, isToolbox: true);
		list.AddRange(collection);
		return list.ToArray();
	}

	private static RiderInfo[] CollectRiderInfosWindows()
	{
		List<RiderInfo> list = new List<RiderInfo>();
		List<string> source = CollectPathsFromToolbox(GetToolboxBaseDir(), "bin", "rider64.exe", isMac: false).ToList();
		list.AddRange(source.Select((string a) => new RiderInfo(a, isToolbox: true)).ToList());
		List<string> list2 = new List<string>();
		CollectPathsFromRegistry("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall", list2);
		CollectPathsFromRegistry("SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall", list2);
		list.AddRange(list2.Select((string a) => new RiderInfo(a, isToolbox: false)).ToList());
		return list.ToArray();
	}

	private static string GetToolboxBaseDir()
	{
		switch (PlatformInfo.operatingSystemFamily)
		{
		case OperatingSystemFamily.Windows:
			return GetToolboxRiderRootPath(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
		case OperatingSystemFamily.MacOSX:
		{
			string environmentVariable2 = Environment.GetEnvironmentVariable("HOME");
			if (!string.IsNullOrEmpty(environmentVariable2))
			{
				return GetToolboxRiderRootPath(Path.Combine(environmentVariable2, "Library/Application Support"));
			}
			break;
		}
		case OperatingSystemFamily.Linux:
		{
			string environmentVariable = Environment.GetEnvironmentVariable("HOME");
			if (!string.IsNullOrEmpty(environmentVariable))
			{
				return GetToolboxRiderRootPath(Path.Combine(environmentVariable, ".local/share"));
			}
			break;
		}
		}
		return string.Empty;
	}

	private static string GetToolboxRiderRootPath(string localAppData)
	{
		string path = Path.Combine(localAppData, "JetBrains/Toolbox");
		string path2 = Path.Combine(path, ".settings.json");
		if (File.Exists(path2))
		{
			string installLocationFromJson = SettingsJson.GetInstallLocationFromJson(File.ReadAllText(path2));
			if (!string.IsNullOrEmpty(installLocationFromJson))
			{
				path = installLocationFromJson;
			}
		}
		return Path.Combine(path, "apps/Rider");
	}

	internal static ProductInfo GetBuildVersion(string path)
	{
		string directoryName = new FileInfo(Path.Combine(path, GetRelativePathToBuildTxt())).DirectoryName;
		if (!Directory.Exists(directoryName))
		{
			return null;
		}
		FileInfo fileInfo = new FileInfo(Path.Combine(directoryName, "product-info.json"));
		if (!fileInfo.Exists)
		{
			return null;
		}
		return ProductInfo.GetProductInfo(File.ReadAllText(fileInfo.FullName));
	}

	internal static System.Version GetBuildNumber(string path)
	{
		FileInfo fileInfo = new FileInfo(Path.Combine(path, GetRelativePathToBuildTxt()));
		if (!fileInfo.Exists)
		{
			return null;
		}
		string text = File.ReadAllText(fileInfo.FullName);
		int num = text.IndexOf("-", StringComparison.Ordinal) + 1;
		if (num <= 0)
		{
			return null;
		}
		if (!System.Version.TryParse(text.Substring(num), out var result))
		{
			return null;
		}
		return result;
	}

	internal static bool GetIsToolbox(string path)
	{
		return Path.GetFullPath(path).StartsWith(Path.GetFullPath(GetToolboxBaseDir()));
	}

	private static string GetRelativePathToBuildTxt()
	{
		switch (PlatformInfo.operatingSystemFamily)
		{
		case OperatingSystemFamily.Windows:
		case OperatingSystemFamily.Linux:
			return "../../build.txt";
		case OperatingSystemFamily.MacOSX:
			return "Contents/Resources/build.txt";
		default:
			throw new Exception("Unknown OS");
		}
	}

	private static void CollectPathsFromRegistry(string registryKey, List<string> installPaths)
	{
		using (RegistryKey key = Registry.CurrentUser.OpenSubKey(registryKey))
		{
			CollectPathsFromRegistry(installPaths, key);
		}
		using RegistryKey key2 = Registry.LocalMachine.OpenSubKey(registryKey);
		CollectPathsFromRegistry(installPaths, key2);
	}

	private static void CollectPathsFromRegistry(List<string> installPaths, RegistryKey key)
	{
		if (key == null)
		{
			return;
		}
		string[] subKeyNames = key.GetSubKeyNames();
		foreach (string name in subKeyNames)
		{
			using RegistryKey registryKey = key.OpenSubKey(name);
			object obj = registryKey?.GetValue("InstallLocation");
			if (obj == null)
			{
				continue;
			}
			string text = obj.ToString();
			if (text.Length == 0)
			{
				continue;
			}
			object value = registryKey.GetValue("DisplayName");
			if (value == null || !value.ToString().Contains("Rider"))
			{
				continue;
			}
			try
			{
				string text2 = Path.Combine(text, "bin\\rider64.exe");
				if (File.Exists(text2))
				{
					installPaths.Add(text2);
				}
			}
			catch (ArgumentException)
			{
			}
		}
	}

	private static string[] CollectPathsFromToolbox(string toolboxRiderRootPath, string dirName, string searchPattern, bool isMac)
	{
		if (!Directory.Exists(toolboxRiderRootPath))
		{
			return new string[0];
		}
		return (from c in Directory.GetDirectories(toolboxRiderRootPath).SelectMany(delegate(string channelDir)
			{
				try
				{
					string path = Path.Combine(channelDir, ".history.json");
					if (File.Exists(path))
					{
						string latestBuildFromJson = ToolboxHistory.GetLatestBuildFromJson(File.ReadAllText(path));
						if (latestBuildFromJson != null)
						{
							string buildDir = Path.Combine(channelDir, latestBuildFromJson);
							string[] executablePaths = GetExecutablePaths(dirName, searchPattern, isMac, buildDir);
							if (executablePaths.Any())
							{
								return executablePaths;
							}
						}
					}
					string path2 = Path.Combine(channelDir, ".channel.settings.json");
					if (File.Exists(path2))
					{
						string latestBuildFromJson2 = ToolboxInstallData.GetLatestBuildFromJson(File.ReadAllText(path2).Replace("active-application", "active_application"));
						if (latestBuildFromJson2 != null)
						{
							string buildDir2 = Path.Combine(channelDir, latestBuildFromJson2);
							string[] executablePaths2 = GetExecutablePaths(dirName, searchPattern, isMac, buildDir2);
							if (executablePaths2.Any())
							{
								return executablePaths2;
							}
						}
					}
					return Directory.GetDirectories(channelDir).SelectMany((string buildDir3) => GetExecutablePaths(dirName, searchPattern, isMac, buildDir3));
				}
				catch (Exception e)
				{
					Logger.Warn("Failed to get RiderPath from " + channelDir, e);
				}
				return new string[0];
			})
			where !string.IsNullOrEmpty(c)
			select c).ToArray();
	}

	private static string[] GetExecutablePaths(string dirName, string searchPattern, bool isMac, string buildDir)
	{
		DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(buildDir, dirName));
		if (!directoryInfo.Exists)
		{
			return new string[0];
		}
		if (!isMac)
		{
			return new string[1] { Path.Combine(directoryInfo.FullName, searchPattern) }.Where(File.Exists).ToArray();
		}
		return (from f in directoryInfo.GetDirectories(searchPattern)
			select f.FullName).Where(Directory.Exists).ToArray();
	}
}

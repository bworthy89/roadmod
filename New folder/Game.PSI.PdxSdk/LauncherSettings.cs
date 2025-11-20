using System;
using System.IO;
using Colossal.Json;
using Colossal.Localization;
using Colossal.Logging;
using Colossal.PSI.Environment;
using Game.Settings;
using UnityEngine;

namespace Game.PSI.PdxSdk;

public static class LauncherSettings
{
	private struct Settings
	{
		public struct System
		{
			public string language;

			public string display_mode;

			public bool vsync;

			public string fullscreen_resolution;

			public string windowed_resolution;

			public double refreshRate;

			public string display_index;
		}

		private enum LauncherDisplayMode
		{
			fullscreen,
			borderless_fullscreen,
			windowed
		}

		public System system;

		public void Merge(SharedSettings gameSettings)
		{
			if (gameSettings.userInterface.locale != "os")
			{
				system.language = gameSettings.userInterface.locale;
			}
			else
			{
				system.language = null;
			}
			system.display_mode = ((LauncherDisplayMode)gameSettings.graphics.displayMode/*cast due to .constrained prefix*/).ToString();
			system.vsync = gameSettings.graphics.vSync;
			string text = FormatResolutionStr(gameSettings.graphics.resolution);
			if (gameSettings.graphics.displayMode == DisplayMode.Window)
			{
				system.windowed_resolution = text;
			}
			else
			{
				system.fullscreen_resolution = text;
			}
			system.refreshRate = gameSettings.graphics.resolution.refreshRate.value;
			system.display_index = gameSettings.graphics.displayIndex.ToString();
		}

		public void Apply(LocalizationManager localizationManager, SharedSettings settings)
		{
			bool flag = false;
			if (localizationManager.SupportsLocale(system.language))
			{
				settings.userInterface.locale = system.language;
			}
			if (Enum.TryParse<LauncherDisplayMode>(system.display_mode, out var result))
			{
				settings.graphics.displayMode = (DisplayMode)result;
				flag = true;
			}
			settings.graphics.vSync = system.vsync;
			if (TryFormatResolution((DisplayMode)result, out var resolution))
			{
				settings.graphics.resolution = resolution;
				flag = true;
			}
			settings.graphics.displayIndex = (int.TryParse(system.display_index, out var result2) ? result2 : 0);
			if (flag)
			{
				settings.graphics.ApplyResolution();
			}
		}

		private bool TryFormatResolution(DisplayMode displayMode, out ScreenResolution resolution)
		{
			string text = ((displayMode == DisplayMode.Window) ? system.windowed_resolution : system.fullscreen_resolution);
			if (text != null)
			{
				int num = text.IndexOf("x", StringComparison.Ordinal);
				if (num >= 0)
				{
					string s = text.Substring(0, num);
					string s2 = text.Substring(num + 1);
					if (int.TryParse(s, out var result) && int.TryParse(s2, out var result2))
					{
						resolution = new ScreenResolution
						{
							width = result,
							height = result2,
							refreshRate = new RefreshRate
							{
								numerator = (uint)(system.refreshRate * 1000.0),
								denominator = 1000u
							}
						};
						return resolution.isValid;
					}
				}
			}
			resolution = default(ScreenResolution);
			return false;
		}

		private static string FormatResolutionStr(ScreenResolution resolution)
		{
			return resolution.width + "x" + resolution.height;
		}
	}

	private static readonly string kLauncherSettingsFileName = "launcher-settings.json";

	private static readonly string kLauncherSettingsPath = EnvPath.kUserDataPath + "/" + kLauncherSettingsFileName;

	private static ILog log = LogManager.GetLogger("PdxSdk");

	public static void LoadSettings(LocalizationManager localizationManager, SharedSettings gameSettings)
	{
		if (TryGetLauncherSettings(out var launcherSettings))
		{
			launcherSettings.Apply(localizationManager, gameSettings);
		}
	}

	public static void SaveSettings(SharedSettings gameSettings)
	{
		if (TryGetLauncherSettings(out var launcherSettings))
		{
			try
			{
				launcherSettings.Merge(gameSettings);
				string contents = JSON.Dump(launcherSettings);
				File.WriteAllText(kLauncherSettingsPath, contents);
				log.Info("Launcher settings saved successfully");
			}
			catch (Exception p)
			{
				log.InfoFormat("Saving launcher settings failed: {0}", p);
			}
		}
	}

	private static bool TryGetLauncherSettings(out Settings launcherSettings)
	{
		if (File.Exists(kLauncherSettingsPath))
		{
			try
			{
				Variant variant = JSON.Load(File.ReadAllText(kLauncherSettingsPath));
				launcherSettings = variant.Make<Settings>();
				log.Info("Loaded launcher settings successfully");
				return true;
			}
			catch (Exception p)
			{
				log.InfoFormat("Loading launcher settings failed: {0}", p);
			}
		}
		else
		{
			log.Info("Launcher settings not present");
		}
		launcherSettings = default(Settings);
		return false;
	}
}

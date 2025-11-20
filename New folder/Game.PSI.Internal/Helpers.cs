using System.Collections.Generic;
using Colossal.PSI.Common;
using Game.Settings;
using UnityEngine;

namespace Game.PSI.Internal;

public static class Helpers
{
	public enum json_displaymode
	{
		fullscreen,
		windowed,
		borderless_window
	}

	public enum json_gameplay_mode
	{
		sandbox,
		editor
	}

	private static readonly IReadOnlyDictionary<SystemLanguage, string> s_SystemLanguageToISO = new Dictionary<SystemLanguage, string>
	{
		{
			SystemLanguage.Afrikaans,
			"af"
		},
		{
			SystemLanguage.Arabic,
			"ar"
		},
		{
			SystemLanguage.Basque,
			"eu"
		},
		{
			SystemLanguage.Belarusian,
			"be"
		},
		{
			SystemLanguage.Bulgarian,
			"bg"
		},
		{
			SystemLanguage.Catalan,
			"ca"
		},
		{
			SystemLanguage.Chinese,
			"zh"
		},
		{
			SystemLanguage.Czech,
			"cs"
		},
		{
			SystemLanguage.Danish,
			"da"
		},
		{
			SystemLanguage.Dutch,
			"nl"
		},
		{
			SystemLanguage.English,
			"en"
		},
		{
			SystemLanguage.Estonian,
			"et"
		},
		{
			SystemLanguage.Faroese,
			"fo"
		},
		{
			SystemLanguage.Finnish,
			"fi"
		},
		{
			SystemLanguage.French,
			"fr"
		},
		{
			SystemLanguage.German,
			"de"
		},
		{
			SystemLanguage.Greek,
			"el"
		},
		{
			SystemLanguage.Hebrew,
			"he"
		},
		{
			SystemLanguage.Hindi,
			"hi"
		},
		{
			SystemLanguage.Hungarian,
			"hu"
		},
		{
			SystemLanguage.Icelandic,
			"is"
		},
		{
			SystemLanguage.Indonesian,
			"id"
		},
		{
			SystemLanguage.Italian,
			"it"
		},
		{
			SystemLanguage.Japanese,
			"ja"
		},
		{
			SystemLanguage.Korean,
			"ko"
		},
		{
			SystemLanguage.Latvian,
			"lv"
		},
		{
			SystemLanguage.Lithuanian,
			"lt"
		},
		{
			SystemLanguage.Norwegian,
			"no"
		},
		{
			SystemLanguage.Polish,
			"pl"
		},
		{
			SystemLanguage.Portuguese,
			"pt"
		},
		{
			SystemLanguage.Romanian,
			"ro"
		},
		{
			SystemLanguage.Russian,
			"ru"
		},
		{
			SystemLanguage.SerboCroatian,
			"sh"
		},
		{
			SystemLanguage.Slovak,
			"sk"
		},
		{
			SystemLanguage.Slovenian,
			"sl"
		},
		{
			SystemLanguage.Spanish,
			"es"
		},
		{
			SystemLanguage.Swedish,
			"sv"
		},
		{
			SystemLanguage.Thai,
			"th"
		},
		{
			SystemLanguage.Turkish,
			"tr"
		},
		{
			SystemLanguage.Ukrainian,
			"uk"
		},
		{
			SystemLanguage.Vietnamese,
			"vi"
		},
		{
			SystemLanguage.ChineseSimplified,
			"zh-HANS"
		},
		{
			SystemLanguage.ChineseTraditional,
			"zh-HANT"
		}
	};

	public static string GetSystemLanguage()
	{
		if (s_SystemLanguageToISO.TryGetValue(Application.systemLanguage, out var value))
		{
			return value;
		}
		return string.Empty;
	}

	public static json_displaymode ToTelemetry(this DisplayMode mode)
	{
		return mode switch
		{
			DisplayMode.Fullscreen => json_displaymode.fullscreen, 
			DisplayMode.Window => json_displaymode.windowed, 
			DisplayMode.FullscreenWindow => json_displaymode.borderless_window, 
			_ => throw new TelemetryException($"Invalid display mode {mode}"), 
		};
	}

	public static string ToTelemetry(this ScreenResolution resolution)
	{
		return $"{resolution.width}x{resolution.height}";
	}

	public static int AsInt(this bool value)
	{
		if (!value)
		{
			return 0;
		}
		return 1;
	}

	public static json_gameplay_mode ToTelemetry(this GameMode gameMode)
	{
		return gameMode switch
		{
			GameMode.Game => json_gameplay_mode.sandbox, 
			GameMode.Editor => json_gameplay_mode.editor, 
			_ => throw new TelemetryException($"Invalid game mode {gameMode}"), 
		};
	}
}

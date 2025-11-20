using System.Collections.Generic;
using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.Json;
using Colossal.Localization;
using Game.Input;
using Game.SceneFlow;
using Game.UI.Localization;
using Game.UI.Widgets;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Settings;

[FileLocation("Settings")]
[SettingsUIGroupOrder(new string[] { "Language", "Style", "Popup", "Hint", "Unit", "Block" })]
public class InterfaceSettings : Setting
{
	public enum InputHintsType
	{
		AutoDetect,
		Xbox,
		PS
	}

	public enum KeyboardLayout
	{
		AutoDetect,
		International
	}

	public enum TimeFormat
	{
		TwentyFourHours,
		TwelveHours
	}

	public enum TemperatureUnit
	{
		Celsius,
		Fahrenheit,
		Kelvin
	}

	public enum UnitSystem
	{
		Metric,
		Freedom
	}

	public const string kName = "Interface";

	public const string kLanguageGroup = "Language";

	public const string kStyleGroup = "Style";

	public const string kPopupGroup = "Popup";

	public const string kHintGroup = "Hint";

	public const string kUnitGroup = "Unit";

	public const string kBlockGroup = "Block";

	[Exclude]
	[SettingsUISection("Language")]
	[SettingsUIDropdown(typeof(InterfaceSettings), "GetLanguageValues")]
	public string currentLocale
	{
		get
		{
			if (locale == "os")
			{
				return GameManager.instance.localizationManager.activeLocaleId;
			}
			return locale;
		}
		set
		{
			locale = value;
		}
	}

	[SettingsUIHidden]
	public string locale { get; set; }

	[SettingsUISection("Style")]
	[SettingsUIDropdown(typeof(InterfaceSettings), "GetInterfaceStyleValues")]
	public string interfaceStyle { get; set; }

	[SettingsUISection("Style")]
	[SettingsUISlider(min = 0f, max = 100f, step = 1f, unit = "percentage", scalarMultiplier = 100f)]
	public float interfaceTransparency { get; set; }

	[SettingsUIDeveloper]
	[SettingsUISection("Style")]
	public bool interfaceScaling { get; set; }

	[SettingsUISection("Style")]
	[SettingsUISlider(min = 100f, max = 150f, step = 10f, unit = "percentage", scalarMultiplier = 100f)]
	public float textScale { get; set; }

	[SettingsUISection("Popup")]
	public bool unlockHighlightsEnabled { get; set; }

	[SettingsUISection("Popup")]
	public bool chirperPopupsEnabled { get; set; }

	[SettingsUISection("Popup")]
	public bool blockingPopupsEnabled { get; set; }

	[SettingsUISection("Popup")]
	public bool showWhatsNewPanel { get; set; }

	[SettingsUISection("Popup")]
	public bool resetDismissedConfirmations
	{
		set
		{
			dismissedConfirmations.Clear();
			ApplyAndSave();
		}
	}

	[SettingsUIPlatform(Platform.PC, false)]
	[SettingsUISection("Hint")]
	public InputHintsType inputHintsType { get; set; }

	[SettingsUIPlatform(Platform.PC, false)]
	[SettingsUISection("Hint")]
	public KeyboardLayout keyboardLayout { get; set; }

	[SettingsUIPlatform(Platform.PC, false)]
	[SettingsUISection("Hint")]
	public bool shortcutHints { get; set; }

	[SettingsUISection("Unit")]
	public TimeFormat timeFormat { get; set; }

	[SettingsUISection("Unit")]
	public TemperatureUnit temperatureUnit { get; set; }

	[SettingsUISection("Unit")]
	public UnitSystem unitSystem { get; set; }

	[SettingsUIHidden]
	public HashSet<string> dismissedConfirmations { get; set; }

	[SettingsUIHidden]
	public int errorMuteCooldownSeconds { get; set; }

	public void AddDismissedConfirmation(string name)
	{
		dismissedConfirmations.Add(name);
		ApplyAndSave();
	}

	public InterfaceSettings()
	{
		SetDefaults();
	}

	public override void SetDefaults()
	{
		locale = "os";
		interfaceStyle = "default";
		interfaceScaling = true;
		if (Platform.PC.IsPlatformSet(Application.platform))
		{
			textScale = 1f;
			interfaceTransparency = 0.5f;
		}
		else
		{
			textScale = 1.3f;
			interfaceTransparency = 0.2f;
		}
		unlockHighlightsEnabled = true;
		chirperPopupsEnabled = true;
		showWhatsNewPanel = true;
		blockingPopupsEnabled = true;
		inputHintsType = InputHintsType.AutoDetect;
		keyboardLayout = KeyboardLayout.AutoDetect;
		shortcutHints = true;
		timeFormat = TimeFormat.TwentyFourHours;
		temperatureUnit = TemperatureUnit.Celsius;
		unitSystem = UnitSystem.Metric;
		dismissedConfirmations = new HashSet<string>();
		errorMuteCooldownSeconds = 10;
	}

	public override void Apply()
	{
		base.Apply();
		GameManager.instance.localizationManager.SetActiveLocale(locale);
	}

	[Preserve]
	public static DropdownItem<string>[] GetLanguageValues()
	{
		LocalizationManager localizationManager = GameManager.instance.localizationManager;
		string[] supportedLocales = localizationManager.GetSupportedLocales();
		List<DropdownItem<string>> list = new List<DropdownItem<string>>(supportedLocales.Length);
		string[] array = supportedLocales;
		foreach (string text in array)
		{
			list.Add(new DropdownItem<string>
			{
				value = text,
				displayName = LocalizedString.Value(localizationManager.GetLocalizedName(text))
			});
		}
		return list.ToArray();
	}

	[Preserve]
	public static DropdownItem<string>[] GetInterfaceStyleValues()
	{
		return new List<DropdownItem<string>>
		{
			new DropdownItem<string>
			{
				value = "default",
				displayName = "Options.INTERFACE_STYLE[default]"
			},
			new DropdownItem<string>
			{
				value = "bright-blue",
				displayName = "Options.INTERFACE_STYLE[bright-blue]"
			},
			new DropdownItem<string>
			{
				value = "dark-grey-orange",
				displayName = "Options.INTERFACE_STYLE[dark-grey-orange]"
			}
		}.ToArray();
	}

	public InputManager.GamepadType GetFinalInputHintsType()
	{
		return inputHintsType switch
		{
			InputHintsType.AutoDetect => InputManager.instance.GetActiveGamepadType(), 
			InputHintsType.Xbox => InputManager.GamepadType.Xbox, 
			InputHintsType.PS => InputManager.GamepadType.PS, 
			_ => InputManager.GamepadType.Xbox, 
		};
	}
}

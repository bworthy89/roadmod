using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.Json;
using Colossal.PSI.Common;
using Colossal.PSI.PdxSdk;
using Game.SceneFlow;
using Game.Simulation;
using Game.UI;
using Unity.Entities;

namespace Game.Settings;

[FileLocation("Settings")]
public class GeneralSettings : Setting
{
	public enum FPSMode
	{
		Off,
		Simple,
		Advanced,
		Precise
	}

	public enum AutoSaveCount
	{
		One = 1,
		Three = 3,
		Ten = 10,
		Fifty = 50,
		Hundred = 100,
		Unlimited = 0
	}

	public enum AutoSaveInterval
	{
		OneMinute = 60,
		TwoMinutes = 120,
		FiveMinutes = 300,
		TenMinutes = 600,
		ThirtyMinutes = 1800,
		OneHour = 3600
	}

	public const string kName = "General";

	private AssetDatabase.AutoReloadMode m_AssetDatabaseAutoReloadMode;

	private SimulationSystem.PerformancePreference m_PerformancePreference;

	private PdxSdkPlatform m_Manager;

	private bool m_OptionalTelemetryConsentFaulted;

	[SettingsUIPlatform(Platform.PC, false)]
	public AssetDatabase.AutoReloadMode assetDatabaseAutoReloadMode
	{
		get
		{
			return m_AssetDatabaseAutoReloadMode;
		}
		set
		{
			m_AssetDatabaseAutoReloadMode = value;
			AssetDatabase.global.autoReloadMode = m_AssetDatabaseAutoReloadMode;
		}
	}

	public SimulationSystem.PerformancePreference performancePreference
	{
		get
		{
			return m_PerformancePreference;
		}
		set
		{
			if (m_PerformancePreference != value)
			{
				m_PerformancePreference = value;
				SimulationSystem simulationSystem = World.DefaultGameObjectInjectionWorld?.GetExistingSystemManaged<SimulationSystem>();
				if (simulationSystem != null)
				{
					simulationSystem.performancePreference = value;
				}
			}
		}
	}

	[SettingsUIDeveloper]
	public FPSMode fpsMode { get; set; }

	public bool autoSave { get; set; }

	[SettingsUIDisableByCondition(typeof(GeneralSettings), "AutoSaveEnabled")]
	public AutoSaveInterval autoSaveInterval { get; set; }

	[SettingsUIDisableByCondition(typeof(GeneralSettings), "AutoSaveEnabled")]
	public AutoSaveCount autoSaveCount { get; set; }

	[SettingsUIDeveloper]
	[SettingsUIButton]
	[SettingsUIDisableByCondition(typeof(GeneralSettings), "CanSave")]
	public bool autoSaveNow
	{
		get
		{
			return true;
		}
		set
		{
			if (GameManager.instance.gameMode.IsGameOrEditor())
			{
				World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<AutoSaveSystem>()?.PerformAutoSave(this);
			}
		}
	}

	[Exclude]
	[SettingsUIHideByCondition(typeof(GeneralSettings), "HideTelemetryConsentChoice")]
	[SettingsUIWarning(typeof(GeneralSettings), "TelemetryConsentFaulted")]
	public bool allowOptionalTelemetry
	{
		get
		{
			return m_Manager?.GetTelemetryConsentChoice() ?? false;
		}
		set
		{
			if (m_Manager != null)
			{
				SetTelemetryConsentChoice(value);
			}
		}
	}

	[SettingsUIButton]
	[SettingsUIConfirmation(null, null)]
	public bool resetSettings
	{
		set
		{
			GameManager.instance.settings.Reset();
		}
	}

	private async void SetTelemetryConsentChoice(bool allow)
	{
		bool flag = await m_Manager.SetTelemetryConsentChoice(allow);
		m_OptionalTelemetryConsentFaulted = !flag;
		if (!flag)
		{
			GameManager.instance.userInterface.appBindings.ShowMessageDialog(new MessageDialog("Paradox.TELEMETRY_CONSENT_ERROR_TITLE", "Paradox.TELEMETRY_CONSENT_ERROR_DESCRIPTION", "Common.OK"), delegate
			{
			});
		}
	}

	private bool HideTelemetryConsentChoice()
	{
		if (m_Manager != null)
		{
			return !m_Manager.IsTelemetryConsentPresentable();
		}
		return true;
	}

	private bool TelemetryConsentFaulted()
	{
		return m_OptionalTelemetryConsentFaulted;
	}

	private void InitializePlatform()
	{
		m_Manager = PlatformManager.instance.GetPSI<PdxSdkPlatform>("PdxSdk");
		PlatformManager.instance.onPlatformRegistered += delegate(IPlatformServiceIntegration psi)
		{
			if (psi is PdxSdkPlatform manager)
			{
				m_Manager = manager;
			}
		};
	}

	public GeneralSettings()
	{
		SetDefaults();
		InitializePlatform();
	}

	public override void SetDefaults()
	{
		autoSave = false;
		autoSaveInterval = AutoSaveInterval.FiveMinutes;
		autoSaveCount = AutoSaveCount.Three;
		fpsMode = FPSMode.Off;
		assetDatabaseAutoReloadMode = AssetDatabase.AutoReloadMode.None;
		performancePreference = SimulationSystem.PerformancePreference.Balanced;
	}

	public static bool CanSave()
	{
		return !GameManager.instance.gameMode.IsGameOrEditor();
	}

	public static bool AutoSaveEnabled()
	{
		return !SharedSettings.instance.general.autoSave;
	}
}

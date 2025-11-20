using System.Collections.Generic;
using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.Localization;
using Colossal.PSI.Common;
using Game.Input;
using Game.PSI.PdxSdk;
using Game.SceneFlow;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Settings;

public class SharedSettings
{
	private readonly List<Setting> m_Settings = new List<Setting>();

	public static SharedSettings instance => GameManager.instance?.settings;

	public GeneralSettings general { get; private set; }

	public AudioSettings audio { get; private set; }

	public GameplaySettings gameplay { get; private set; }

	public RadioSettings radio { get; private set; }

	public GraphicsSettings graphics { get; private set; }

	public EditorSettings editor { get; private set; }

	public InterfaceSettings userInterface { get; private set; }

	public InputSettings input { get; private set; }

	public KeybindingSettings keybinding { get; private set; }

	public ModdingSettings modding { get; private set; }

	public UserState userState { get; private set; }

	public SharedSettings(LocalizationManager localizationManager)
	{
		m_Settings.Add(general = new GeneralSettings());
		m_Settings.Add(audio = new AudioSettings());
		m_Settings.Add(gameplay = new GameplaySettings());
		m_Settings.Add(radio = new RadioSettings());
		m_Settings.Add(graphics = new GraphicsSettings());
		m_Settings.Add(editor = new EditorSettings());
		m_Settings.Add(userInterface = new InterfaceSettings());
		m_Settings.Add(input = new InputSettings());
		m_Settings.Add(this.userState = new UserState());
		m_Settings.Add(keybinding = new KeybindingSettings());
		m_Settings.Add(modding = new ModdingSettings());
		LoadSettings();
		LauncherSettings.LoadSettings(localizationManager, this);
		localizationManager.SetActiveLocale(userInterface.locale);
	}

	public void RegisterInOptionsUI()
	{
		general.RegisterInOptionsUI("General");
		if (Platform.PC.IsPlatformSet(Application.platform, debugConditional: true))
		{
			graphics.RegisterInOptionsUI("Graphics");
		}
		gameplay.RegisterInOptionsUI("Gameplay");
		userInterface.RegisterInOptionsUI("Interface");
		audio.RegisterInOptionsUI("Audio");
		input.RegisterInOptionsUI("Input");
		modding.RegisterInOptionsUI("Modding");
		if (GameManager.instance.configuration.developerMode)
		{
			new About().RegisterInOptionsUI("About");
			PlatformManager.instance.onStatusChanged += delegate
			{
				new About().RegisterInOptionsUI("About");
			};
		}
		InputSystem.onDeviceChange += OnDeviceChange;
		Game.Input.InputManager.instance.EventControlSchemeChanged += OnControlSchemeChanged;
		void OnControlSchemeChanged(Game.Input.InputManager.ControlScheme controlScheme)
		{
			input.RegisterInOptionsUI("Input");
		}
		void OnDeviceChange(InputDevice changedDevice, InputDeviceChange change)
		{
			if (change == InputDeviceChange.Added || change == InputDeviceChange.Removed)
			{
				input.RegisterInOptionsUI("Input");
			}
		}
	}

	public void LoadSettings()
	{
		AssetDatabase.global.LoadSettings("General Settings", general, new GeneralSettings(), userSetting: true);
		AssetDatabase.global.LoadSettings("Audio Settings", audio, new AudioSettings(), userSetting: true);
		AssetDatabase.global.LoadSettings("Gameplay Settings", gameplay, new GameplaySettings(), userSetting: true);
		AssetDatabase.global.LoadSettings("Radio Settings", radio, new RadioSettings(), userSetting: true);
		AssetDatabase.global.LoadSettings("Graphics Settings", graphics, new GraphicsSettings(), userSetting: true);
		AssetDatabase.global.LoadSettings("Editor Settings", editor, new EditorSettings(), userSetting: true);
		AssetDatabase.global.LoadSettings("Interface Settings", userInterface, new InterfaceSettings(), userSetting: true);
		AssetDatabase.global.LoadSettings("Input Settings", input, new InputSettings(), userSetting: true);
		AssetDatabase.global.LoadSettings("Keybinding Settings", keybinding, null, userSetting: true);
		AssetDatabase.global.LoadSettings("Modding Settings", modding, new ModdingSettings(), userSetting: true);
	}

	public void LoadUserSettings()
	{
		AssetDatabase.global.LoadSettings("User Settings", userState, new UserState(), userSetting: true);
	}

	public void Reset()
	{
		Launcher.DeleteLastSaveMetadata();
		foreach (Setting setting in m_Settings)
		{
			setting.SetDefaults();
			setting.ApplyAndSave();
		}
	}

	public void SetDefaultsWithoutApplying()
	{
		foreach (Setting setting in m_Settings)
		{
			setting.SetDefaults();
		}
		userState.SetDefaults();
	}

	public void Apply()
	{
		foreach (Setting setting in m_Settings)
		{
			setting.Apply();
		}
	}
}

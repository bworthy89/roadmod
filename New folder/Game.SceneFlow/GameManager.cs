using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ATL;
using cohtml.Net;
using Colossal;
using Colossal.AssetPipeline.Importers;
using Colossal.Core;
using Colossal.FileSystem;
using Colossal.IO.AssetDatabase;
using Colossal.IO.AssetDatabase.VirtualTexturing;
using Colossal.Json;
using Colossal.Localization;
using Colossal.Logging;
using Colossal.Logging.Backtrace;
using Colossal.PSI.Common;
using Colossal.PSI.Environment;
using Colossal.PSI.PdxSdk;
using Colossal.Reflection;
using Colossal.Serialization.Entities;
using Colossal.TestFramework;
using Colossal.UI;
using Colossal.UI.Fatal;
using Game.Assets;
using Game.Audio;
using Game.Common;
using Game.Debug;
using Game.Input;
using Game.Modding;
using Game.Prefabs;
using Game.PSI;
using Game.PSI.PdxSdk;
using Game.Rendering;
using Game.Serialization;
using Game.Settings;
using Game.Threading;
using Game.UI;
using Game.UI.Localization;
using Game.UI.Menu;
using Game.UI.Thumbnails;
using Mono.Options;
using PDX.SDK;
using PDX.SDK.Contracts.Service.Mods.Enums;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering.HighDefinition;

namespace Game.SceneFlow;

public class GameManager : MonoBehaviour, ICoroutineHost
{
	public class Configuration
	{
		public enum StdoutCaptureMode
		{
			None,
			Console,
			CaptureOnly,
			Redirect
		}

		public Colossal.Hash128 startGame;

		public bool disablePDXSDK;

		public bool noThumbnails;

		public string profilerTarget;

		public bool saveAllSettings;

		public bool cleanupSettings = true;

		public bool developerMode;

		public bool uiDeveloperMode;

		public bool qaDeveloperMode;

		public bool duplicateLogToDefault;

		public bool disableUserSection;

		public bool disableModding;

		public bool disableCodeModding;

		public StdoutCaptureMode captureStdout;

		public string showHelp;

		public bool continuelastsave;

		public override string ToString()
		{
			return this.ToJSONString();
		}

		public void LogConfiguration()
		{
			log.InfoFormat("Configuration: {0}", this);
		}
	}

	public enum State
	{
		Booting,
		Terminated,
		UIReady,
		WorldDisposed,
		WorldReady,
		Quitting,
		Loading
	}

	public delegate void EventCallback();

	public delegate void EventGamePreload(Purpose purpose, GameMode mode);

	public delegate void EventGameSaveLoad(string saveName, bool start, bool success);

	public delegate void FullscreenOverlayOpened();

	public class LocalTypeCache
	{
		private readonly Dictionary<(Type type, string name, BindingFlags bindingFlags), MethodInfo> m_MethodCache = new Dictionary<(Type, string, BindingFlags), MethodInfo>();

		private readonly Dictionary<(Type type, string name), PropertyInfo> m_PropertyCache = new Dictionary<(Type, string), PropertyInfo>();

		private readonly Dictionary<(Type type, string name), FieldInfo> m_FieldCache = new Dictionary<(Type, string), FieldInfo>();

		public MethodInfo GetMethod(Type type, string methodName, BindingFlags bindingFlags)
		{
			(Type, string, BindingFlags) key = (type, methodName, bindingFlags);
			if (!m_MethodCache.TryGetValue(key, out var value))
			{
				value = type.GetMethod(methodName, bindingFlags);
				m_MethodCache[key] = value;
			}
			return value;
		}

		public PropertyInfo GetProperty(Type type, string propertyName)
		{
			(Type, string) key = (type, propertyName);
			if (!m_PropertyCache.TryGetValue(key, out var value))
			{
				value = type.GetProperty(propertyName);
				m_PropertyCache[key] = value;
			}
			return value;
		}

		public FieldInfo GetField(Type type, string propertyName)
		{
			(Type, string) key = (type, propertyName);
			if (!m_FieldCache.TryGetValue(key, out var value))
			{
				value = type.GetField(propertyName);
				m_FieldCache[key] = value;
			}
			return value;
		}
	}

	private Configuration m_Configuration;

	[SerializeField]
	private string m_AdditionalCommandLineToggles;

	private static ILog log;

	private ModManager m_ModManager;

	private CancellationTokenSource m_Cts;

	private readonly CancellationTokenSource m_QuitRequested = new CancellationTokenSource();

	private readonly TaskCompletionSource<bool> m_WorldReadySource = new TaskCompletionSource<bool>();

	public GameObject[] m_SettingsDependantObjects;

	private int m_MainThreadId;

	private State m_State;

	private OverlayScreen m_InitialEngagementScreen = OverlayScreen.Engagement;

	private bool m_IsEngagementStarted;

	public const string kInMainMenuState = "#StatusInMainMenu";

	public const string kInGameState = "#StatusInGame";

	public const string kInEditorState = "#StatusInEditor";

	private bool m_StartUpTelemetryFired;

	[SerializeField]
	private string m_UILocation;

	private UIManager m_UIManager;

	private UIInputSystem m_UIInputSystem;

	private readonly ConcurrentDictionary<Guid, Func<bool>> m_Updaters = new ConcurrentDictionary<Guid, Func<bool>>();

	private const string kBootTask = "Boot";

	private LayerMask m_DefaultCullingMask;

	private LayerMask m_DefaultVolumeLayerMask;

	private readonly List<(int count, int target, TaskCompletionSource<bool> tcs)> m_PendingWaitFrameCounts = new List<(int, int, TaskCompletionSource<bool>)>();

	private ConsoleWindow m_Console;

	private static string s_ModdingRuntime;

	private World m_World;

	private UpdateSystem m_UpdateSystem;

	private LoadGameSystem m_DeserializationSystem;

	private SaveGameSystem m_SerializationSystem;

	private PrefabSystem m_PrefabSystem;

	public string[] cmdLine { get; private set; }

	public Configuration configuration
	{
		get
		{
			if (m_Configuration == null)
			{
				m_Configuration = new Configuration();
			}
			return m_Configuration;
		}
	}

	public static GameManager instance { get; private set; }

	public bool isMainThread => Thread.CurrentThread.ManagedThreadId == m_MainThreadId;

	public GameMode gameMode { get; private set; } = GameMode.Other;

	public bool isGameLoading
	{
		get
		{
			if (state != State.Booting)
			{
				return state == State.Loading;
			}
			return true;
		}
	}

	public SharedSettings settings { get; private set; }

	public ModManager modManager => m_ModManager;

	public CancellationToken terminationToken
	{
		get
		{
			if (m_Cts == null)
			{
				return CancellationToken.None;
			}
			return m_Cts.Token;
		}
	}

	public State state => m_State;

	public bool shouldUpdateManager => m_State >= State.UIReady;

	public bool shouldUpdateWorld => m_State >= State.WorldReady;

	public static UIInputSystem UIInputSystem => instance?.m_UIInputSystem;

	public LocalizationManager localizationManager { get; private set; }

	public UserInterface userInterface { get; private set; }

	public ThumbnailCache thumbnailCache { get; private set; }

	public event EventGameSaveLoad onGameSaveLoad;

	public event EventGamePreload onGamePreload;

	public event EventGamePreload onGameLoadingComplete;

	public event EventCallback onWorldReady;

	public event FullscreenOverlayOpened onFullscreenOverlayOpened;

	private void OnGUI()
	{
		if (shouldUpdateWorld && !m_Cts.IsCancellationRequested)
		{
			TerrainDebugSystem orCreateSystemManaged = m_World.GetOrCreateSystemManaged<TerrainDebugSystem>();
			if (orCreateSystemManaged.Enabled)
			{
				orCreateSystemManaged.RenderDebugUI();
			}
		}
	}

	private static string[] MergeAdditionalCommandLineArguments(string[] cmdLineArgs, string additionalCmdLine)
	{
		HashSet<string> hashSet = (string.IsNullOrEmpty(additionalCmdLine) ? new HashSet<string>() : additionalCmdLine.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToHashSet());
		if (hashSet.Count > 0)
		{
			string[] array = new string[cmdLineArgs.Length + hashSet.Count];
			cmdLineArgs.CopyTo(array, 0);
			hashSet.CopyTo(array, cmdLineArgs.Length);
			return array;
		}
		return cmdLineArgs;
	}

	private Configuration.StdoutCaptureMode GetStdoutCaptureMode(string option)
	{
		return option switch
		{
			"console" => Configuration.StdoutCaptureMode.Console, 
			"capture" => Configuration.StdoutCaptureMode.CaptureOnly, 
			"redirect" => Configuration.StdoutCaptureMode.Redirect, 
			_ => Configuration.StdoutCaptureMode.None, 
		};
	}

	private void ParseOptions()
	{
		OptionSet optionSet = new OptionSet().Add("cleanupSettings", "Cleanup unchanged settings", delegate(string option)
		{
			configuration.cleanupSettings = option != null;
		}).Add("saveAllSettings", "Dump all settings regardless if they have changed", delegate(string option)
		{
			configuration.saveAllSettings = option != null;
		}).Add("logsEffectiveness=", "Override effectiveness level of all logs", delegate(string option)
		{
			LogManager.SetDefaultEffectiveness(Level.GetLevel(option));
		})
			.Add("duplicateLogToDefault", "Duplicate logs to default log handler", delegate(string option)
			{
				configuration.duplicateLogToDefault = option != null;
			})
			.Add("developerMode", "Enable developer mode", delegate(string option)
			{
				configuration.developerMode = option != null;
			})
			.Add("uiDeveloperMode", "Enable UI debugger and memory tracker", delegate(string option)
			{
				configuration.uiDeveloperMode = option != null;
			})
			.Add("qaDeveloperMode", "Enable tests and automation", delegate(string option)
			{
				configuration.qaDeveloperMode = option != null;
			})
			.Add("help", "Display usage", delegate(string option)
			{
				configuration.showHelp = option;
			})
			.Add("disableThumbnails", "Disable thumbnails", delegate(string option)
			{
				configuration.noThumbnails = option != null;
			})
			.Add("disablePdxSdk", "Disables PDX SDK integration", delegate(string option)
			{
				configuration.disablePDXSDK = option != null;
			})
			.Add("disableModding", "Disable modding", delegate(string option)
			{
				configuration.disableModding = option != null;
				configuration.disableCodeModding = configuration.disableModding;
			})
			.Add("disableCodeModding", "Disable code modding", delegate(string option)
			{
				configuration.disableCodeModding = option != null;
			})
			.Add("disableUserSection", "Disable user section in main menu", delegate(string option)
			{
				configuration.disableUserSection = option != null;
			})
			.Add("startGame=", "Auto start the game with the asset referenced", delegate(string option)
			{
				configuration.startGame = Colossal.Hash128.Parse(option);
			})
			.Add("profile=", "Enable profiling to the specific file", delegate(string option)
			{
				configuration.profilerTarget = option;
			})
			.Add("captureStdout=", "Capture all logs on stdout. Options: \"console\",\"capture\"", delegate(string option)
			{
				configuration.captureStdout = GetStdoutCaptureMode(option);
			})
			.Add("continuelastsave", "Auto start the game with the asset referenced", delegate(string option)
			{
				configuration.continuelastsave = option != null;
			});
		try
		{
			string path = EnvPath.kUserDataPath + "/runOnce.txt";
			if (LongFile.Exists(path))
			{
				m_AdditionalCommandLineToggles = StringUtils.Concatenate(" ", m_AdditionalCommandLineToggles, File.ReadAllText(path));
				LongFile.Delete(path);
			}
			cmdLine = Environment.GetCommandLineArgs();
			cmdLine = MergeAdditionalCommandLineArguments(cmdLine, m_AdditionalCommandLineToggles);
			optionSet.Parse(cmdLine);
			log.InfoFormat("Command line: {0}", string.Join("\n", MaskArguments(cmdLine)));
			if (configuration.showHelp != null)
			{
				using (TextWriter textWriter = new StringWriter())
				{
					optionSet.WriteOptionDescriptions(textWriter);
					configuration.showHelp = textWriter.ToString();
					return;
				}
			}
		}
		catch (OptionException exception)
		{
			UnityEngine.Debug.LogException(exception);
		}
	}

	private static string[] MaskArguments(string[] cmdLine)
	{
		try
		{
			HashSet<string> hashSet = new HashSet<string> { "pdx-launcher-session-token", "paradox-account-userid", "accessToken", "hubSessionId", "licensingIpc" };
			string[] array = (string[])cmdLine.Clone();
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i].TrimStart('-');
				int num = text.IndexOf('=');
				if (num != -1)
				{
					string input = text.Substring(num + 1);
					text = text.Substring(0, num);
					array[i] = text + "=" + input.Sensitive();
				}
				else if (hashSet.Contains(text) && i + 1 < array.Length)
				{
					array[i + 1] = array[i + 1].Sensitive();
				}
			}
			return array;
		}
		catch (Exception exception)
		{
			log.Warn(exception, "An error occured parsing the command line for logging");
			return Array.Empty<string>();
		}
	}

	public async void RegisterCancellationOnQuit(TaskCompletionSource<bool> tcs, bool stateOnCancel)
	{
		await using (m_QuitRequested.Token.Register(delegate
		{
			tcs.TrySetResult(stateOnCancel);
		}))
		{
			await tcs.Task;
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void SetupCustomAssetTypes()
	{
		DefaultAssetFactory.instance.AddSupportedType(".SaveGameMetadata", () => new SaveGameMetadata());
		DefaultAssetFactory.instance.AddSupportedType(".MapMetadata", () => new MapMetadata());
		DefaultAssetFactory.instance.AddSupportedType(".CinematicCamera", () => new CinematicCameraAsset());
	}

	private async void Awake()
	{
		_ = 1;
		try
		{
			CoroutineHost.Register(this);
			PreInitializePlatform();
			if (!CheckValidity())
			{
				return;
			}
			using (Colossal.PerformanceCounter.Start(delegate(TimeSpan t)
			{
				log?.InfoFormat("GameManager created! ({0}ms)", t.TotalMilliseconds);
			}))
			{
				Task checkCapabilities = CheckCapabilities();
				DetectModdingRuntime();
				BacktraceHelper.SetDefaultAttributes(GetDefaultBacktraceAttributes());
				EnableMemoryLeaksDetection();
				Application.wantsToQuit += WantsToQuit;
				m_MainThreadId = Thread.CurrentThread.ManagedThreadId;
				m_State = State.Booting;
				Application.focusChanged += FocusChanged;
				SetNativeStackTrace();
				m_Cts = new CancellationTokenSource();
				CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
				LogManager.SetDefaultEffectiveness(Level.Info);
				BacktraceHelper.SetDefaultAttributes(GetDefaultBacktraceAttributes());
				log = LogManager.GetLogger("SceneFlow");
				TryCatchUnhandledExceptions();
				ParseOptions();
				InitConsole();
				if (!HandleConfiguration())
				{
					QuitGame();
					return;
				}
				DisableCameraRendering();
				await PreparePersistentStorage();
				HandleUserFolderVersion();
				await checkCapabilities;
				instance = this;
				Initialize();
			}
		}
		catch (Exception exception)
		{
			log.Fatal(exception);
			QuitGame();
		}
	}

	private Task CheckCapabilities()
	{
		return Capabilities.CacheCapabilities();
	}

	private void OnMainMenuReached(Purpose purpose, GameMode mode)
	{
		if (mode == GameMode.MainMenu)
		{
			AutomationClientSystem.instance.OnMainMenuReached();
		}
	}

	private async void Initialize()
	{
		_ = 17;
		try
		{
			using (Colossal.PerformanceCounter.Start(delegate(TimeSpan t)
			{
				log?.InfoFormat("GameManager initialized! ({0}ms)", t.TotalMilliseconds);
			}))
			{
				ErrorDialogManager errorDialogManager = new ErrorDialogManager();
				await TestScenarioSystem.Create(cmdLine);
				onGameLoadingComplete += OnMainMenuReached;
				ListHarmonyPatches();
				TaskManager taskManager = TaskManager.instance;
				InputManager.CreateInstance();
				AssetDatabase.global.SetSettingsConfiguration(configuration.saveAllSettings, configuration.cleanupSettings);
				await InitializePlatformManager();
				await taskManager.SharedTask("CacheAssets", () => AssetDatabase.global.CacheAssets(priorityAssets: true, m_Cts.Token));
				Task caching = taskManager.SharedTask("CacheAssets", () => AssetDatabase.global.CacheAssets(priorityAssets: false, m_Cts.Token));
				InitializeLocalization();
				settings = new SharedSettings(localizationManager);
				CreateWorld();
				InputManager.instance.SetDefaultControlScheme();
				await InitializeUI(errorDialogManager);
				TaskManager.instance.onNotifyProgress += NotifyProgress;
				ReportBootProgress(0f);
				Task engagement = SetInitialEngagementScreenActive();
				Task loading = SetScreenActive<LoadingScreen>();
				await SetScreenActive<SplashScreenSequence>();
				Task assetLoading = LoadUnityPrefabs();
				log.Info(GetVersionsInfo());
				log.Info(GetSystemInfoString());
				configuration.LogConfiguration();
				await engagement;
				RegisterDeviceAndUserListeners();
				m_ModManager = new ModManager(configuration.disableCodeModding);
				await caching;
				await RegisterPdxSdk();
				ReportBootProgress(0.3f);
				await WaitXFrames(4);
				settings.LoadUserSettings();
				ReportBootProgress(0.4f);
				await WaitXFrames(4);
				CreateSystems();
				ReportBootProgress(0.5f);
				await WaitXFrames(4);
				InitializeModManager();
				settings.Apply();
				EnableSettingsDependantObjects();
				await assetLoading;
				await LoadPrefabs();
				InitializeThumbnails();
				m_State = State.WorldReady;
				ReportBootProgress(1f);
				await WaitXFrames(4);
				this.onWorldReady?.Invoke();
				await Task.WhenAll(loading, PlatformManager.instance.WaitForAchievements());
				EnableCameraRendering();
				m_WorldReadySource.TrySetResult(result: true);
				log.Info("Boot completed");
				bool flag = true;
				if (configuration.startGame.isValid)
				{
					flag = !(await AutoLoad(configuration.startGame));
				}
				else if (configuration.continuelastsave)
				{
					flag = !(await userInterface.appBindings.LauncherContinueGame());
				}
				if (flag)
				{
					await MainMenu();
				}
			}
		}
		catch (OperationCanceledException)
		{
			UnityEngine.Debug.Log("GameManager termination requested before initialization completed");
		}
		catch (Exception ex2)
		{
			log.Fatal(ex2);
			ShowFallbackUI(ex2);
		}
	}

	private void InitializeModManager(bool ignoreParadox = false)
	{
		if (m_UpdateSystem != null && (ignoreParadox || AssetDatabase<ParadoxMods>.instance.isCached))
		{
			m_ModManager.Initialize(m_UpdateSystem);
		}
	}

	private void EnableSettingsDependantObjects()
	{
		m_Cts.Token.ThrowIfCancellationRequested();
		GameObject[] settingsDependantObjects = m_SettingsDependantObjects;
		foreach (GameObject obj in settingsDependantObjects)
		{
			obj.SetActive(obj);
		}
	}

	public async Task<bool> WaitForReadyState()
	{
		_ = 1;
		try
		{
			await using (m_Cts.Token.Register(delegate
			{
				m_WorldReadySource.TrySetCanceled();
			}))
			{
				await m_WorldReadySource.Task.ConfigureAwait(continueOnCapturedContext: false);
			}
			return m_WorldReadySource.Task.IsCompletedSuccessfully;
		}
		catch (OperationCanceledException)
		{
			return false;
		}
	}

	private void Update()
	{
		UpdateWaitFrameCount();
		if (shouldUpdateManager && !m_Cts.IsCancellationRequested)
		{
			TestScenarioSystem.instance.Update();
			InputManager.instance.Update();
			m_UIInputSystem.DispatchInputEvents(InputManager.instance.activeControlScheme == InputManager.ControlScheme.KeyboardAndMouse);
			UpdateWorld();
			UpdateUI();
			PostUpdateWorld();
		}
		UpdateUpdaters();
		UpdatePlatforms();
	}

	private void LateUpdate()
	{
		if (!m_Cts.IsCancellationRequested)
		{
			LateUpdateWorld();
		}
	}

	public static void QuitGame()
	{
		Application.Quit();
	}

	public void FocusChanged(bool hasFocus)
	{
		InputManager.instance?.OnFocusChanged(hasFocus);
	}

	private bool WantsToQuit()
	{
		if (m_State != State.Quitting && m_State != State.Terminated)
		{
			TerminateGame();
			return false;
		}
		if (m_State == State.Quitting)
		{
			UnityEngine.Debug.LogWarning("TerminateGame is already in progress, please wait.");
			return false;
		}
		return true;
	}

	private void OnDestroy()
	{
		Application.wantsToQuit -= WantsToQuit;
	}

	private async Task TerminateGame()
	{
		if (m_Cts == null)
		{
			m_State = State.Terminated;
			QuitGame();
		}
		else
		{
			if (m_State == State.Quitting || m_State == State.Terminated)
			{
				return;
			}
			try
			{
				using (Colossal.PerformanceCounter.Start(delegate(TimeSpan t)
				{
					log?.InfoFormat("GameManager destroyed ({0}ms)", t.TotalMilliseconds);
				}))
				{
					DisableCameraRendering();
					await UnityTask.WaitForGPUFrame();
					State quittingState = m_State;
					m_State = State.Quitting;
					m_QuitRequested.Cancel();
					await TaskManager.instance.Complete("SaveLoadGame");
					if (quittingState >= State.WorldReady)
					{
						LauncherSettings.SaveSettings(settings);
						await TaskManager.instance.SharedTask("CacheAssets", () => AssetDatabase.global.SaveSettings());
					}
					TaskManager.instance.onNotifyProgress -= NotifyProgress;
					m_Cts.Cancel();
					m_ModManager?.Dispose();
					DestroyWorld();
					DisposeThumbnails();
					bool flag = await DisposePlatforms().AwaitWithTimeout(TimeSpan.FromSeconds(10.0));
					ReleaseUI();
					InputManager.DestroyInstance();
					StopAllCoroutines();
					CoroutineHost.Register(null);
					bool flag2 = flag;
					flag = flag2 & await TaskManager.instance.CompleteAndClear().AwaitWithTimeout(TimeSpan.FromSeconds(10.0));
					VolumeHelper.Dispose();
					AssetDatabase.global.Dispose();
					LogManager.ReleaseResources();
					Colossal.Gizmos.ReleaseResources();
					LogManager.stdOutActive = false;
					ReleaseConsole();
					onGameLoadingComplete -= OnMainMenuReached;
					TestScenarioSystem.Destroy();
					instance = null;
					Application.focusChanged -= FocusChanged;
					if (flag)
					{
						UnityEngine.Debug.Log("Game terminated successfully");
					}
					else
					{
						UnityEngine.Debug.Log("Game terminated due to timeout");
					}
				}
			}
			catch (Exception exception)
			{
				instance = null;
				log.Error(exception);
			}
			finally
			{
				m_State = State.Terminated;
				QuitGame();
			}
		}
	}

	private Task SetInitialEngagementScreenActive()
	{
		m_IsEngagementStarted = true;
		if (!PlatformManager.instance.requiresEngagement)
		{
			return Task.CompletedTask;
		}
		if (AutomationClientSystem.instance.IsConnected)
		{
			return Task.CompletedTask;
		}
		return m_InitialEngagementScreen switch
		{
			OverlayScreen.UserLoggedOut => SetScreenActive<LoggedOutScreen>(), 
			OverlayScreen.ControllerPairingChanged => SetScreenActive<ControllerPairingScreen>(), 
			OverlayScreen.ControllerDisconnected => SetScreenActive<ControllerDisconnectedScreen>(), 
			_ => SetScreenActive<EngagementScreen>(), 
		};
	}

	private void RegisterDeviceAndUserListeners()
	{
		InputManager.instance.EventActiveDeviceAssociationLost += HandleDeviceAssociationLost;
		InputManager.instance.EventActiveDeviceDisconnected += HandleDeviceDisconnected;
		InputManager.instance.EventDevicePaired += HandleDevicePaired;
		PlatformManager.instance.onUserUpdated += HandleUserUpdated;
	}

	private void HandleDeviceAssociationLost()
	{
		if (m_IsEngagementStarted)
		{
			SetScreenActive<ControllerPairingScreen>();
		}
		else if (m_InitialEngagementScreen > OverlayScreen.ControllerPairingChanged)
		{
			m_InitialEngagementScreen = OverlayScreen.ControllerPairingChanged;
		}
	}

	private void HandleDeviceDisconnected()
	{
		if (m_IsEngagementStarted)
		{
			SetScreenActive<ControllerDisconnectedScreen>();
		}
		else if (m_InitialEngagementScreen > OverlayScreen.ControllerDisconnected)
		{
			m_InitialEngagementScreen = OverlayScreen.ControllerDisconnected;
		}
	}

	private void HandleDevicePaired()
	{
		if (!m_IsEngagementStarted)
		{
			m_InitialEngagementScreen = OverlayScreen.Engagement;
		}
	}

	private void HandleUserUpdated(IPlatformServiceIntegration psi, UserChangedFlags flags)
	{
		if (m_IsEngagementStarted)
		{
			if (flags.HasFlag(UserChangedFlags.UserSigningOut) && !flags.HasFlag(UserChangedFlags.ChangingUser))
			{
				SetScreenActive<LoggedOutScreen>();
			}
		}
		else if (flags.HasFlag(UserChangedFlags.UserSigningOut) && m_InitialEngagementScreen > OverlayScreen.UserLoggedOut)
		{
			m_InitialEngagementScreen = OverlayScreen.UserLoggedOut;
		}
		else if (flags.HasFlag(UserChangedFlags.UserSignedInAgain))
		{
			m_InitialEngagementScreen = OverlayScreen.Engagement;
		}
	}

	private void CleanupMemory()
	{
		Resources.UnloadUnusedAssets();
		foreach (Colossal.UI.UISystem uISystem in UIManager.UISystems)
		{
			uISystem.ClearCachedUnusedImages();
		}
		GC.Collect();
	}

	private async Task<string[]> SaveSimulationData(Colossal.Serialization.Entities.Context context, Stream stream)
	{
		CleanupMemory();
		m_SerializationSystem.stream = stream;
		m_SerializationSystem.context = context;
		await m_SerializationSystem.RunOnce();
		string[] array = m_SerializationSystem.referencedContent.Select((Entity x) => m_PrefabSystem.GetPrefabName(x)).ToArray();
		return (array.Length != 0) ? array : null;
	}

	public Task<bool> Save(string saveName, SaveInfo meta, ILocalAssetDatabase database, Texture savePreview)
	{
		return Save(saveName, meta, database, new ScreenCaptureHelper.AsyncRequest(savePreview));
	}

	public async Task<bool> Save(string saveName, SaveInfo meta, ILocalAssetDatabase database, ScreenCaptureHelper.AsyncRequest previewRequest)
	{
		log.Info("Save " + saveName + " to " + database.name);
		this.onGameSaveLoad?.Invoke(saveName, start: true, success: true);
		await UnityTask.WaitForGPUFrame();
		ILocalAssetDatabase saveDatabase = AssetDatabase.GetTransient(0L);
		try
		{
			meta.sessionGuid = Telemetry.GetCurrentSession();
			meta.lastModified = DateTime.Now;
			AssetDataPath saveNameDataPath = saveName;
			SaveGameData saveGameData = saveDatabase.AddAsset<SaveGameData>(saveNameDataPath);
			Colossal.Serialization.Entities.Context context = new Colossal.Serialization.Entities.Context(Purpose.SaveGame, Version.current, saveGameData.id.guid);
			SaveInfo saveInfo = meta;
			saveInfo.contentPrerequisites = await SaveSimulationData(context, saveGameData.GetWriteStream());
			meta.saveGameData = saveGameData;
			if (previewRequest != null)
			{
				await previewRequest.Complete();
				using TextureImporter.Texture texture = TextureImporter.Texture.CreateUncompressed1Mip(saveName, previewRequest.width, previewRequest.height, sRGB: false, previewRequest.result);
				using TextureAsset textureAsset = saveDatabase.AddAsset(texture);
				meta.preview = textureAsset;
				textureAsset.Save();
			}
			SaveGameMetadata saveGameMetaData = saveDatabase.AddAsset<SaveGameMetadata>(saveNameDataPath);
			saveGameMetaData.target = meta;
			saveGameMetaData.Save();
			bool saveSuccess = false;
			PackageAsset arg = null;
			if (HaveEnoughStorageSpace(saveDatabase, database, saveName))
			{
				arg = await Task.Run(delegate
				{
					AssetDataPath assetDataPath = SaveHelpers.GetAssetDataPath<SaveGameMetadata>(database, saveName);
					using (AssetDatabase.global.DisableNotificationsScoped())
					{
						if (database.Exists<PackageAsset>(assetDataPath, out var asset))
						{
							database.DeleteAsset(asset);
						}
						PackageAsset packageAsset = database.AddAsset(assetDataPath, saveDatabase);
						packageAsset.Save();
						if (database.Exists<PackageAsset>(assetDataPath, out var _))
						{
							settings.userState.lastSaveGameMetadata = saveGameMetaData;
							settings.userState.ApplyAndSave();
							Launcher.SaveLastSaveMetadata(meta);
							saveSuccess = true;
						}
						return packageAsset;
					}
				});
			}
			this.onGameSaveLoad?.Invoke(saveName, start: false, saveSuccess);
			log.InfoFormat(string.Format("Saving {0} {1}", saveSuccess ? "completed" : "failed", arg));
			return true;
		}
		finally
		{
			if (saveDatabase != null)
			{
				saveDatabase.Dispose();
			}
		}
	}

	private bool HaveEnoughStorageSpace(ILocalAssetDatabase tempSaveDatabase, ILocalAssetDatabase targetSaveDatabase, string saveName)
	{
		long num = 0L;
		long num2 = 0L;
		foreach (IAssetData asset in tempSaveDatabase.GetAssets(default(SearchFilter<IAssetData>)))
		{
			num += tempSaveDatabase.GetMeta(asset.id).size;
		}
		num = Mathf.CeilToInt((float)num * 1.05f);
		num2 = targetSaveDatabase.dataSource.GetQuota().available;
		log.Verbose("[HaveEnoughStorageSpace] Required: " + FormatUtils.FormatBytes(num) + " | Available: " + FormatUtils.FormatBytes(num2));
		return num < num2;
	}

	private Task LoadSimulationData(Colossal.Serialization.Entities.Context context, AsyncReadDescriptor dataDescriptor)
	{
		this.onGamePreload?.Invoke(context.purpose, gameMode);
		CleanupMemory();
		m_DeserializationSystem.dataDescriptor = dataDescriptor;
		m_DeserializationSystem.context = context;
		return m_DeserializationSystem.RunOnce();
	}

	private async Task<bool> Load(GameMode mode, Purpose purpose, AsyncReadDescriptor descriptor, Colossal.Hash128 instigatorGuid, Guid sessionGuid)
	{
		log.InfoFormat("Loading mode {0} with purpose {1}", mode, purpose);
		if (descriptor == AsyncReadDescriptor.Invalid && purpose != Purpose.NewGame && purpose != Purpose.NewMap && purpose != Purpose.Cleanup)
		{
			log.WarnFormat("Invalid descriptor provided with purpose {0}", purpose);
			return false;
		}
		GameMode oldMode = gameMode;
		gameMode = mode;
		m_State = State.Loading;
		TextureStreamingSystem tss = null;
		if (mode.IsGameOrEditor())
		{
			TaskManager taskManager = TaskManager.instance;
			taskManager.ScheduleGroup(ProgressTracker.Group.Group1, 1);
			taskManager.ScheduleGroup(ProgressTracker.Group.Group2, 1);
			taskManager.ScheduleGroup(ProgressTracker.Group.Group3, 1);
			taskManager.progress.Report(new ProgressTracker("LoadTextures", ProgressTracker.Group.Group1)
			{
				progress = 0f
			});
			taskManager.progress.Report(new ProgressTracker("LoadMeshes", ProgressTracker.Group.Group2)
			{
				progress = 0f
			});
			taskManager.progress.Report(new ProgressTracker("LoadSimulation", ProgressTracker.Group.Group3)
			{
				progress = 0f
			});
			tss = m_World.GetExistingSystemManaged<TextureStreamingSystem>();
			tss.boostInitRates = true;
			RegisterUpdater(delegate
			{
				float num = (tss.VTMaterialAssetsProgression + tss.VTMaterialDuplicatesProgression) * 0.5f;
				taskManager.progress.Report(new ProgressTracker("LoadTextures", ProgressTracker.Group.Group1)
				{
					progress = num
				});
				return num >= 1f;
			});
		}
		Task loading = SetScreenActive<LoadingScreen>();
		await UnityTask.WaitForGPUFrame();
		if (mode != GameMode.MainMenu || oldMode != GameMode.Other)
		{
			Colossal.Serialization.Entities.Context context = new Colossal.Serialization.Entities.Context(purpose, Version.current, instigatorGuid);
			await LoadSimulationData(context, descriptor);
		}
		switch (gameMode)
		{
		case GameMode.Editor:
			Telemetry.OpenSession(sessionGuid);
			userInterface.appBindings.SetEditorActive();
			break;
		case GameMode.Game:
			Telemetry.OpenSession(sessionGuid);
			userInterface.appBindings.SetGameActive();
			break;
		case GameMode.MainMenu:
			Telemetry.CloseSession();
			userInterface.appBindings.SetMainMenuActive();
			Cursor.lockState = CursorLockMode.None;
			break;
		}
		PlatformManager.instance.SetRichPresence(gameMode.ToRichPresence());
		await loading;
		if (tss != null)
		{
			tss.boostInitRates = false;
		}
		CleanupMemory();
		this.onGameLoadingComplete?.Invoke(purpose, mode);
		m_State = State.WorldReady;
		HDCamera.GetOrCreate(Camera.main).Reset();
		log.Info("Loading completed");
		return true;
	}

	private Guid GetSessionGuid(Purpose purpose, Guid existingGuid)
	{
		if (purpose == Purpose.NewMap || purpose == Purpose.NewGame)
		{
			return Guid.NewGuid();
		}
		return existingGuid;
	}

	public Task<bool> Load(GameMode mode, Purpose purpose, IAssetData asset = null)
	{
		log.InfoFormat("Starting game from '{0}'", asset);
		if (asset is MapMetadata mapMetadata)
		{
			using (mapMetadata)
			{
				AsyncReadDescriptor descriptor = AsyncReadDescriptor.Invalid;
				MapInfo target = mapMetadata.target;
				if (target.mapData == null)
				{
					log.WarnFormat("The mapData referenced by '{0}' (meta: '{1}') doesn't not exist'", target.id, asset);
				}
				else
				{
					descriptor = target.mapData.GetAsyncReadDescriptor();
				}
				return Load(mode, purpose, descriptor, mapMetadata.id, GetSessionGuid(purpose, target.sessionGuid));
			}
		}
		if (asset is SaveGameMetadata saveGameMetadata)
		{
			using (saveGameMetadata)
			{
				AsyncReadDescriptor descriptor2 = AsyncReadDescriptor.Invalid;
				SaveInfo target2 = saveGameMetadata.target;
				if (target2.saveGameData == null)
				{
					log.WarnFormat("The saveGameData referenced by '{0}' (meta: '{1}') doesn't not exist'", target2.id, asset);
				}
				else
				{
					descriptor2 = target2.saveGameData.GetAsyncReadDescriptor();
				}
				return Load(mode, purpose, descriptor2, saveGameMetadata.id, GetSessionGuid(purpose, target2.sessionGuid));
			}
		}
		if (asset is MapData mapData)
		{
			log.Warn("Loading with MapData. Session guid will be lost, rather use metadata if available");
			return Load(mode, purpose, mapData.GetAsyncReadDescriptor(), mapData.id, Guid.NewGuid());
		}
		if (asset is SaveGameData saveGameData)
		{
			log.Warn("Loading with SaveGameData. Session guid will be lost, rather use metadata if available");
			return Load(mode, purpose, saveGameData.GetAsyncReadDescriptor(), saveGameData.id, Guid.NewGuid());
		}
		if (asset == null)
		{
			return Load(mode, purpose, AsyncReadDescriptor.Invalid, Colossal.Hash128.Empty, Guid.NewGuid());
		}
		log.WarnFormat("Couldn't start game from '{0}'", asset);
		return Task.FromResult(result: false);
	}

	public Task<bool> Load(GameMode mode, Purpose purpose, Colossal.Hash128 guid)
	{
		if (AssetDatabase.global.TryGetAsset(guid, out var asset))
		{
			return Load(mode, purpose, asset);
		}
		log.WarnFormat("Couldn't load '{0}'. Asset doesn't exist!", guid);
		return Task.FromResult(result: false);
	}

	private Task<bool> AutoLoad(IAssetData asset)
	{
		if (asset is MapData || asset is MapMetadata)
		{
			return Load(GameMode.Game, Purpose.NewGame, asset);
		}
		if (asset is SaveGameData || asset is SaveGameMetadata)
		{
			return Load(GameMode.Game, Purpose.LoadGame, asset);
		}
		log.WarnFormat("Couldn't load '{0}'. Asset doesn't exist!", asset);
		return Task.FromResult(result: false);
	}

	private Task<bool> AutoLoad(Colossal.Hash128 guid)
	{
		if (AssetDatabase.global.TryGetAsset(guid, out var asset))
		{
			return AutoLoad(asset);
		}
		log.WarnFormat("Couldn't load '{0}'. Asset doesn't exist!", guid);
		return Task.FromResult(result: false);
	}

	public async Task<bool> MainMenu()
	{
		_ = 2;
		try
		{
			IAssetData asset = null;
			bool ret = await Load(GameMode.MainMenu, Purpose.Cleanup, asset);
			if (ret)
			{
				await AudioManager.instance.ResetAudioOnMainThread();
				await AudioManager.instance.PlayMenuMusic("Main Menu Theme");
				log.Info("MainMenu reached");
			}
			return ret;
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception exception)
		{
			log.Error(exception);
		}
		return false;
	}

	private Task PreparePersistentStorage()
	{
		EnvPath.RegisterSpecialPath<SaveGameMetadata>(SaveGameMetadata.kPersistentLocation);
		EnvPath.RegisterSpecialPath<SaveGameData>(SaveGameMetadata.kPersistentLocation);
		EnvPath.RegisterSpecialPath<MapMetadata>(MapMetadata.kPersistentLocation);
		EnvPath.RegisterSpecialPath<MapData>(MapMetadata.kPersistentLocation);
		EnvPath.RegisterSpecialPath<CinematicCameraAsset>(CinematicCameraAsset.kPersistentLocation);
		return EnvPath.WipeTempPath();
	}

	private async Task RegisterPdxSdk()
	{
		ProductEnvironment environment = ProductEnvironment.Sandbox;
		string text = "beverly_hills";
		if (1 == 1)
		{
			environment = ProductEnvironment.Live;
			text = "cities_skylines_2";
		}
		log.Debug("productEnvironment " + environment);
		log.Debug("pdxSdkNamespace " + text);
		if (!configuration.disablePDXSDK)
		{
			PdxSdkConfiguration pdxConfiguration = new PdxSdkConfiguration
			{
				language = localizationManager.activeLocaleId,
				gameNamespace = text,
				gameVersion = Version.current.fullVersion,
				environment = environment
			};
			await PlatformManager.instance.RegisterPSI(delegate
			{
				CancellationTokenSource cts = new CancellationTokenSource();
				string value;
				PdxSdkPlatform pdxSdkPlatform = new PdxSdkPlatform(pdxConfiguration)
				{
					translationHandler = (string localeId) => (!localizationManager.activeDictionary.TryGetValue(localeId, out value)) ? localeId : value
				};
				localizationManager.onActiveDictionaryChanged += delegate
				{
					pdxSdkPlatform.ChangeLanguage(localizationManager.activeLocaleId);
				};
				pdxSdkPlatform.onLegalDocumentStatusChanged += delegate(LegalDocument doc, int remaining)
				{
					if (remaining == 0)
					{
						TelemetryReady();
						PlatformManager.instance.EnableSharing();
					}
				};
				pdxSdkPlatform.onNoLogin += async delegate
				{
					try
					{
						if (state != State.Quitting && !configuration.disableModding)
						{
							await RegisterDatabase();
						}
					}
					catch (OperationCanceledException)
					{
					}
					catch (Exception exception)
					{
						InitializeModManager(ignoreParadox: true);
						PdxSdkPlatform.log.Error(exception);
					}
					finally
					{
						cts = new CancellationTokenSource();
					}
				};
				pdxSdkPlatform.onLoggedIn += async delegate
				{
					_ = 1;
					try
					{
						if (state != State.Quitting)
						{
							Task task = Task.CompletedTask;
							if (!configuration.disableModding)
							{
								task = pdxSdkPlatform.SyncMods(SyncDirection.Default, cts.Token);
							}
							if (!configuration.disableModding)
							{
								await task;
								await RegisterDatabase();
							}
						}
					}
					catch (OperationCanceledException)
					{
					}
					catch (Exception exception)
					{
						InitializeModManager(ignoreParadox: true);
						PdxSdkPlatform.log.Error(exception);
					}
					finally
					{
						cts = new CancellationTokenSource();
					}
				};
				pdxSdkPlatform.onLoggedOut += async delegate
				{
					cts.Cancel();
					cts = new CancellationTokenSource();
				};
				pdxSdkPlatform.onContentUnlocked += delegate(List<IDlc> dlcs)
				{
					if (dlcs != null)
					{
						foreach (IDlc dlc in dlcs)
						{
							if (!string.IsNullOrEmpty(dlc.internalName))
							{
								string internalName = dlc.internalName;
								string internalName2 = dlc.internalName;
								string internalName3 = dlc.internalName;
								ProgressState? progressState = ProgressState.Complete;
								NotificationSystem.Pop(internalName, 4f, null, null, internalName2, internalName3, null, progressState);
							}
						}
					}
					LoadUnityPrefabs();
				};
				pdxSdkPlatform.onDataSyncConflict += delegate
				{
					ProgressState? progressState = ProgressState.Warning;
					NotificationSystem.Push("PDXDataSyncConflict", null, null, "ActionRequired", "PDXDataSyncConflict", null, progressState, null, async delegate
					{
						TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
						userInterface.appBindings.ShowConfirmationDialog(new ParadoxCloudConflictResolutionDialog(), delegate(int msg)
						{
							tcs.SetResult(msg);
						});
						await tcs.Task;
						if (tcs.Task.Result != -1)
						{
							NotificationSystem.Pop("PDXDataSyncConflict");
							ProgressState? progressState2 = ProgressState.Indeterminate;
							NotificationSystem.Push("PDXDataSyncConflictResolving", null, null, "PDXDataSyncConflict", "PDXDataSyncConflictResolving", null, progressState2);
							if (await pdxSdkPlatform.SyncModConflict((tcs.Task.Result == 0) ? SyncDirection.Downstream : SyncDirection.Upstream, cts.Token))
							{
								progressState2 = ProgressState.Complete;
								NotificationSystem.Pop("PDXDataSyncConflictResolving", 1f, null, null, "PDXDataSyncConflict", "PDXDataSyncConflictResolved", null, progressState2);
							}
							else
							{
								progressState2 = ProgressState.Failed;
								NotificationSystem.Pop("PDXDataSyncConflictResolving", 1f, null, null, "PDXDataSyncConflict", "PDXDataSyncConflictFailed", null, progressState2);
							}
						}
					});
				};
				pdxSdkPlatform.onModSyncCompleted += delegate
				{
					if (!pdxSdkPlatform.HasLocalChanges())
					{
						NotificationSystem.Pop("PDXDataSyncConflict");
					}
				};
				pdxSdkPlatform.onModSyncCancelled += delegate
				{
					cts.Cancel();
					cts = new CancellationTokenSource();
				};
				return pdxSdkPlatform;
			}, m_Cts.Token).ConfigureAwait(continueOnCapturedContext: false);
		}
		else
		{
			PlatformManager.instance.EnableSharing();
			await Task.CompletedTask;
		}
		void OnActivePlaysetChanged()
		{
			m_World.GetOrCreateSystemManaged<TextureStreamingSystem>()?.RefreshVT(AssetDatabase<ParadoxMods>.instance);
		}
		void OnAssetChanged(AssetChangedEventArgs args)
		{
			if (args.change == Colossal.IO.AssetDatabase.ChangeType.AssetAdded)
			{
				IAssetData asset = args.asset;
				if (!(asset is UIModuleAsset uIModuleAsset))
				{
					if (asset is ExecutableAsset executableAsset)
					{
						executableAsset.onActivePlaysetChanged += delegate(ExecutableAsset executableAsset2, bool isInActivePlayset)
						{
							if (executableAsset2.isILAssembly && executableAsset2.isLoaded != isInActivePlayset)
							{
								m_ModManager?.RequireRestart();
							}
						};
					}
				}
				else
				{
					uIModuleAsset.onActivePlaysetChanged += delegate(UIModuleAsset uiModule, bool isInActivePlayset)
					{
						if (isInActivePlayset)
						{
							m_ModManager?.AddUIModule(uiModule);
						}
						else
						{
							m_ModManager?.RemoveUIModule(uiModule);
						}
					};
				}
			}
		}
		async Task RegisterDatabase()
		{
			m_Cts.Token.ThrowIfCancellationRequested();
			AssetDatabase<ParadoxMods> modsDatabase = AssetDatabase<ParadoxMods>.instance;
			IDataSourceProvider dataSource = modsDatabase.dataSource;
			if (dataSource is ParadoxModsDataSource dataSource2)
			{
				if (AssetDatabase.global.databases.FirstOrDefault((IAssetDatabase db) => typeof(AssetDatabase<ParadoxMods>).IsAssignableFrom(db.GetType())) == null)
				{
					await AssetDatabase.global.RegisterDatabase(modsDatabase);
					modsDatabase.onAssetDatabaseChanged.Subscribe(OnAssetChanged);
					dataSource2.onEntryIsInActivePlaysetChanged += OnEntryIsInActivePlaysetChanged;
					dataSource2.onAfterActivePlaysetOrModStatusChanged += OnActivePlaysetChanged;
				}
				await dataSource2.Populate();
			}
			InitializeModManager(!modsDatabase.isCached);
			void OnEntryIsInActivePlaysetChanged(Colossal.Hash128 guid, bool isInActivePlayset)
			{
				if (modsDatabase.TryGetAsset(guid, out var asset))
				{
					if (!(asset is ExecutableAsset executableAsset))
					{
						if (!(asset is UIModuleAsset uIModuleAsset))
						{
							if (!(asset is SurfaceAsset) && !(asset is MidMipCacheAsset))
							{
								if (asset is PrefabAsset prefabAsset)
								{
									try
									{
										log.DebugFormat("OnEntryIsInActivePlaysetChanged: {0} ({1})", asset.name, isInActivePlayset);
										PrefabBase prefabBase = prefabAsset.Load() as PrefabBase;
										if (isInActivePlayset)
										{
											if (m_PrefabSystem.AddPrefab(prefabBase))
											{
												log.DebugFormat("Loaded {0}", prefabBase.name);
											}
										}
										else if (m_PrefabSystem.RemovePrefab(prefabBase))
										{
											log.DebugFormat("Removed {0}", prefabBase.name);
										}
									}
									catch (Exception exception)
									{
										log.Error(exception);
									}
								}
							}
							else
							{
								m_World.GetOrCreateSystemManaged<TextureStreamingSystem>()?.MarkVTAssetsDirty();
							}
						}
						else
						{
							uIModuleAsset.isInActivePlayset = isInActivePlayset;
						}
					}
					else
					{
						executableAsset.isInActivePlayset = isInActivePlayset;
					}
				}
			}
		}
	}

	private void TelemetryReady()
	{
		if (!m_StartUpTelemetryFired)
		{
			Telemetry.FireSessionStartEvents();
			PlatformManager.instance.onAchievementUpdated += delegate(IAchievementsSupport p, AchievementId a)
			{
				Telemetry.AchievementUnlocked(a);
			};
			m_StartUpTelemetryFired = true;
		}
	}

	private void PreInitializePlatform()
	{
	}

	private async Task InitializePlatformManager()
	{
		PlatformManager.instance.RegisterRichPresenceKey("#StatusInMainMenu", () => "In Main-Menu");
		PlatformManager.instance.RegisterRichPresenceKey("#StatusInGame", () => "In-Game");
		PlatformManager.instance.RegisterRichPresenceKey("#StatusInEditor", () => "In-Editor");
		await PlatformManager.instance.RegisterPSI(PlatformSupport.kCreateSteamPlatform, m_Cts.Token).ConfigureAwait(continueOnCapturedContext: false);
		await PlatformManager.instance.RegisterPSI(PlatformSupport.kCreateDiscordRichPresence, m_Cts.Token).ConfigureAwait(continueOnCapturedContext: false);
		if (!(await PlatformManager.instance.Initialize(m_Cts.Token)))
		{
			log.ErrorFormat("A platform service integration failed to initialize");
			QuitGame();
		}
		EnvPath.UpdateSpecialPathCache();
		await AssetDatabase.global.RegisterDatabase(AssetDatabase<SteamCloud>.instance);
		await Colossal.IO.AssetDatabase.ContentHelper.RegisterContent();
	}

	private void UpdatePlatforms()
	{
		PlatformManager.instance.Update();
	}

	private async Task DisposePlatforms()
	{
		Task task = PlatformManager.instance.Dispose(disposeEvents: true, CancellationToken.None);
		while (!task.IsCompleted)
		{
			Update();
			await Task.Delay(500);
		}
		await task;
	}

	private void ShowFallbackUI(Exception ex)
	{
		if (m_UIManager == null)
		{
			m_UIManager = new UIManager(developerMode: false);
		}
		ErrorPage errorPage = new ErrorPage();
		errorPage.AddAction("quit", QuitGame);
		errorPage.AddAction("visit", delegate
		{
			try
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = "https://pdxint.at/3Do979W",
					UseShellExecute = true
				});
			}
			catch
			{
				QuitGame();
			}
		});
		errorPage.SetStopCode(ex);
		errorPage.SetRoot(EnvPath.kContentPath + "/Game/UI/.fatal", EnvPath.kContentPath + "/Game/.fatal");
		errorPage.SetFonts(EnvPath.kContentPath + "/Game/UI/Fonts", EnvPath.kContentPath + "/Game/Fonts.cok");
		Colossal.UI.UISystem.Settings settings = Colossal.UI.UISystem.Settings.New;
		settings.resourceHandler = new FatalResourceHandler(errorPage);
		settings.enableDebugger = false;
		Colossal.UI.UISystem uISystem = m_UIManager.CreateUISystem(settings);
		UIView.Settings settings2 = UIView.Settings.New;
		settings2.liveReload = true;
		uISystem.CreateView("fatal://error", settings2, GetComponent<Camera>()).enabled = true;
		if (m_UIInputSystem != null)
		{
			m_UIInputSystem.Dispose();
		}
		m_UIInputSystem = new UIInputSystem(uISystem);
		if (!shouldUpdateManager)
		{
			RegisterUpdater(delegate
			{
				m_UIInputSystem.DispatchInputEvents();
				m_UIManager.Update();
				return false;
			});
		}
	}

	private async Task InitializeUI(ErrorDialogManager errorDialogManager)
	{
		m_Cts.Token.ThrowIfCancellationRequested();
		ILog uiLog = UIManager.log;
		try
		{
			uiLog.Info("Bootstrapping cohtmlUISystem");
			m_UIManager = new UIManager(configuration.uiDeveloperMode);
			Colossal.UI.UISystem.Settings settings = Colossal.UI.UISystem.Settings.New;
			settings.localizationManager = new UILocalizationManager(localizationManager);
			settings.resourceHandler = new GameUIResourceHandler(this);
			Colossal.UI.UISystem uISystem = m_UIManager.CreateUISystem(settings);
			foreach (UIHostAsset asset in AssetDatabase.global.GetAssets(default(SearchFilter<UIHostAsset>)))
			{
				if (asset.scheme == "assetdb")
				{
					uISystem.AddDatabaseHostLocation(asset.hostname, asset.uiUri, asset.priority);
				}
				else
				{
					uISystem.AddHostLocation(asset.hostname, asset.uiPath, shouldWatch: true, asset.priority);
				}
			}
			m_UIInputSystem = new UIInputSystem(uISystem);
			userInterface = new UserInterface(m_UILocation, localizationManager, errorDialogManager, uISystem);
			m_World.GetOrCreateSystem<NotificationUISystem>();
			m_World.GetOrCreateSystem<OptionsUISystem>();
			this.settings.RegisterInOptionsUI();
			m_State = State.UIReady;
			InputManager.instance.CheckConflicts();
			log.DebugFormat("Time to UI {0}s", Time.realtimeSinceStartup);
			await userInterface.WaitForBindings();
		}
		catch (Exception exception)
		{
			uiLog.Error(exception);
		}
	}

	private void CreateUISystems()
	{
		foreach (Type item in ReflectionUtils.GetAllTypesDerivedFrom<UISystemBase>())
		{
			if (!item.IsAbstract)
			{
				m_World.GetOrCreateSystem(item);
			}
		}
	}

	private void UpdateUI()
	{
		m_UIManager.Update();
		userInterface.Update();
	}

	private void ReleaseUI()
	{
		userInterface?.Dispose();
		m_UIInputSystem?.Dispose();
		m_UIManager?.Dispose();
	}

	public Task SetScreenActive<T>() where T : IScreenState, new()
	{
		T val = new T();
		this.onFullscreenOverlayOpened?.Invoke();
		return val.Execute(this, m_Cts.Token);
	}

	private void UpdateUpdaters()
	{
		foreach (KeyValuePair<Guid, Func<bool>> updater in m_Updaters)
		{
			if (updater.Value())
			{
				UnregisterUpdater(updater.Key);
			}
		}
	}

	public Guid RegisterUpdater(Action action)
	{
		return RegisterUpdater(delegate
		{
			action();
			return true;
		});
	}

	public Guid RegisterUpdater(Func<bool> func)
	{
		if (func != null)
		{
			Guid guid = Guid.NewGuid();
			m_Updaters.TryAdd(guid, func);
			log.DebugFormat("Updater {0} registered with guid {1}", func.Method.Name, guid.ToLowerNoDashString());
			return guid;
		}
		return Guid.Empty;
	}

	public bool UnregisterUpdater(Guid guid)
	{
		if (m_Updaters.TryRemove(guid, out var value))
		{
			log.DebugFormat("Updater {0} with {1} unregistered", guid.ToLowerNoDashString(), value.Method.Name);
			return true;
		}
		log.DebugFormat("Updater {0} was not found");
		return false;
	}

	public string[] GetAvailablePrerequisitesNames()
	{
		return m_PrefabSystem.GetAvailablePrerequisitesNames();
	}

	public bool ArePrerequisitesMet(string[] contentPrerequisites)
	{
		if (contentPrerequisites == null)
		{
			return true;
		}
		foreach (string text in contentPrerequisites)
		{
			if (!m_PrefabSystem.TryGetPrefab(new PrefabID("ContentPrefab", text), out var prefab) || !((ContentPrefab)prefab).IsAvailable())
			{
				return false;
			}
		}
		return true;
	}

	public bool ArePrerequisitesMet<T>(Metadata<T> meta) where T : IContentPrerequisite
	{
		string[] contentPrerequisites = meta.target.contentPrerequisites;
		return ArePrerequisitesMet(contentPrerequisites);
	}

	private void ReportBootProgress(float progress)
	{
		TaskManager.instance.progress.Report(new ProgressTracker("Boot", ProgressTracker.Group.Group3)
		{
			progress = progress
		});
	}

	private void NotifyProgress(string identifier, int progress)
	{
		string titleId = identifier;
		string textId = identifier;
		ProgressState? progressState = ProgressState.Progressing;
		int? progress2 = progress;
		NotificationSystem.Push(identifier, null, null, titleId, textId, null, progressState, progress2);
		if (progress >= 100)
		{
			textId = identifier;
			titleId = identifier;
			progressState = ProgressState.Complete;
			progress2 = progress;
			NotificationSystem.Pop(identifier, 2f, null, null, textId, titleId, null, progressState, progress2);
		}
	}

	private void EnableMemoryLeaksDetection()
	{
		NativeLeakDetection.Mode = NativeLeakDetectionMode.Disabled;
	}

	public void RunOnMainThread(Action action)
	{
		if (isMainThread)
		{
			action();
		}
		else
		{
			RegisterUpdater(action);
		}
	}

	private void InitializeThumbnails()
	{
		m_Cts.Token.ThrowIfCancellationRequested();
		thumbnailCache = new ThumbnailCache();
		thumbnailCache.Initialize();
	}

	private void DisposeThumbnails()
	{
		thumbnailCache?.Dispose();
	}

	private Task LoadUnityPrefabs()
	{
		return LoadAssetLibraryAsync();
	}

	private Task WaitXFrames(int frames)
	{
		TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
		m_PendingWaitFrameCounts.Add((0, frames, taskCompletionSource));
		return taskCompletionSource.Task;
	}

	private void UpdateWaitFrameCount()
	{
		for (int num = m_PendingWaitFrameCounts.Count - 1; num >= 0; num--)
		{
			(int count, int target, TaskCompletionSource<bool> tcs) tuple = m_PendingWaitFrameCounts[num];
			int item = tuple.count;
			int item2 = tuple.target;
			TaskCompletionSource<bool> item3 = tuple.tcs;
			item++;
			if (item >= item2)
			{
				item3.TrySetResult(result: true);
				m_PendingWaitFrameCounts.RemoveAt(num);
			}
			else
			{
				m_PendingWaitFrameCounts[num] = (item, item2, item3);
			}
		}
	}

	private async Task LoadPrefabs()
	{
		int count = 0;
		using (Colossal.PerformanceCounter.Start(delegate(TimeSpan t)
		{
			log.InfoFormat("Loaded {1} prefabs in {0}s", t.TotalSeconds, count);
		}))
		{
			List<PrefabBase> batch = new List<PrefabBase>(128);
			List<PrefabAsset> list = AssetDatabase.global.GetAssets(default(SearchFilter<PrefabAsset>)).ToList();
			int total = list.Count;
			int batchSize = 42;
			foreach (PrefabAsset item2 in list)
			{
				m_Cts.Token.ThrowIfCancellationRequested();
				count++;
				if (!(item2.Load() is PrefabBase item))
				{
					continue;
				}
				batch.Add(item);
				if (batch.Count < batchSize)
				{
					continue;
				}
				foreach (PrefabBase item3 in batch)
				{
					m_PrefabSystem.AddPrefab(item3);
				}
				batch.Clear();
				ReportBootProgress(0.5f + (float)count / (float)total * 0.49f);
				await WaitXFrames(8);
			}
			if (batch.Count <= 0)
			{
				return;
			}
			foreach (PrefabBase item4 in batch)
			{
				m_PrefabSystem.AddPrefab(item4);
			}
		}
	}

	private async Task<AssetLibrary> LoadAssetLibraryAsync()
	{
		using (Colossal.PerformanceCounter.Start(delegate(TimeSpan t)
		{
			log.InfoFormat("LoadAssetLibraryAsync performed in {0}ms", t.TotalMilliseconds);
		}))
		{
			UnityEngine.ResourceRequest asyncLoad = Resources.LoadAsync<AssetLibrary>("GameAssetLibrary");
			while (!asyncLoad.isDone)
			{
				m_Cts.Token.ThrowIfCancellationRequested();
				await Task.Yield();
			}
			((AssetLibrary)asyncLoad.asset).Load(m_PrefabSystem, m_Cts.Token);
			return asyncLoad.asset as AssetLibrary;
		}
	}

	private void DisableCameraRendering()
	{
		Camera main = Camera.main;
		if (main != null)
		{
			m_DefaultCullingMask = main.cullingMask;
			main.cullingMask = 0;
			HDAdditionalCameraData component = main.GetComponent<HDAdditionalCameraData>();
			if (component != null)
			{
				m_DefaultVolumeLayerMask = component.volumeLayerMask;
				component.volumeLayerMask = 0;
			}
		}
	}

	private void EnableCameraRendering()
	{
		m_Cts.Token.ThrowIfCancellationRequested();
		Camera main = Camera.main;
		if (main != null)
		{
			main.cullingMask = m_DefaultCullingMask;
			HDAdditionalCameraData component = main.GetComponent<HDAdditionalCameraData>();
			if (component != null)
			{
				component.volumeLayerMask = m_DefaultVolumeLayerMask;
			}
		}
	}

	[DllImport("user32.dll")]
	private static extern bool SetWindowText(IntPtr hWnd, string lpString);

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	private static extern IntPtr FindWindow(string strClassName, string strWindowName);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

	[RuntimeInitializeOnLoadMethod]
	private static void SetWindowsTitle()
	{
		IntPtr intPtr = FindWindow(null, Application.productName);
		if (intPtr != IntPtr.Zero)
		{
			SetWindowText(intPtr, "Cities: Skylines II");
		}
	}

	private void InitConsole()
	{
		if (configuration.captureStdout != Configuration.StdoutCaptureMode.None)
		{
			if (configuration.captureStdout != Configuration.StdoutCaptureMode.Redirect)
			{
				m_Console = new ConsoleWindow(Application.productName, configuration.captureStdout == Configuration.StdoutCaptureMode.Console);
			}
			LogManager.stdOutActive = true;
			log.Info("\u001b[1m\u001b[38;2;0;135;215mWelcome to Cities: Skylines II\u001b[0m");
			log.Info("\u001b[1m\u001b[38;2;0;135;215mColossal Order Oy - 2023\u001b[0m");
			Thread.Sleep(1000);
		}
	}

	private void ReleaseConsole()
	{
		m_Console?.Dispose();
	}

	private void TryCatchUnhandledExceptions()
	{
		System.Threading.Tasks.TaskScheduler.UnobservedTaskException += delegate(object sender, UnobservedTaskExceptionEventArgs e)
		{
			e.SetObserved();
			log.Critical(e.Exception, "Unobserved exception triggered");
		};
		AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
		{
			Exception exception = (Exception)e.ExceptionObject;
			log.Critical(exception, "Unhandled domain exception triggered");
		};
	}

	private bool CheckValidity()
	{
		try
		{
			_ = instance.enabled;
		}
		catch (MissingReferenceException)
		{
			base.enabled = false;
			UnityEngine.Object.Destroy(base.gameObject);
			QuitGame();
			return false;
		}
		catch
		{
		}
		if (instance != null && instance != this)
		{
			base.enabled = false;
			UnityEngine.Object.Destroy(base.gameObject);
			return false;
		}
		return true;
	}

	public static string GetVersionsInfo()
	{
		StringBuilder stringBuilder = new StringBuilder();
		string text = null;
		text = "Mono";
		stringBuilder.AppendLine($"Date: {DateTime.UtcNow}");
		stringBuilder.AppendLine($"Game version: {Version.current.fullVersion} {Application.platform.ToPlatform()} {PlatformManager.instance.principalPlatformName}");
		stringBuilder.AppendLine("Game configuration: " + (UnityEngine.Debug.isDebugBuild ? "Development" : "Release") + " (" + text + ")");
		stringBuilder.AppendLine("COre version: " + Colossal.Core.Version.current.fullVersion);
		stringBuilder.AppendLine("Localization version: " + Colossal.Localization.Version.current.fullVersion);
		stringBuilder.AppendLine("UI version: " + Colossal.UI.Version.current.fullVersion);
		stringBuilder.AppendLine("Unity version: " + Application.unityVersion);
		stringBuilder.AppendLine($"Cohtml version: {Versioning.Build}");
		stringBuilder.AppendLine("ATL Version: " + ATL.Version.getVersion());
		PlatformManager.instance.LogVersion(stringBuilder);
		foreach (IDlc item in PlatformManager.instance.EnumerateLocalDLCs())
		{
			stringBuilder.AppendLine(item.internalName.Nicify() + ": " + item.version.fullVersion);
		}
		if (Application.genuineCheckAvailable)
		{
			stringBuilder.AppendLine($"Genuine: {Application.genuine}");
		}
		return stringBuilder.ToString().TrimEnd();
	}

	public static string GetSystemInfoString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("Type: " + SystemInfo.deviceType);
		stringBuilder.AppendLine("OS: " + SystemInfo.operatingSystem);
		stringBuilder.AppendLine("System memory: " + FormatUtils.FormatBytes((long)SystemInfo.systemMemorySize * 1024L * 1024));
		stringBuilder.AppendLine("Graphics device: " + SystemInfo.graphicsDeviceName + " (Version: " + SystemInfo.graphicsDeviceVersion + ")");
		stringBuilder.AppendLine("Graphics memory: " + FormatUtils.FormatBytes((long)SystemInfo.graphicsMemorySize * 1024L * 1024));
		stringBuilder.AppendLine("Max texture size: " + SystemInfo.maxTextureSize);
		stringBuilder.AppendLine("Shader level: " + SystemInfo.graphicsShaderLevel);
		stringBuilder.AppendLine("3D textures: " + SystemInfo.supports3DTextures);
		stringBuilder.AppendLine("Shadows: " + SystemInfo.supportsShadows);
		stringBuilder.AppendLine("Compute: " + SystemInfo.supportsComputeShaders);
		stringBuilder.AppendLine("CPU: " + SystemInfo.processorType);
		stringBuilder.AppendLine("Core count: " + SystemInfo.processorCount);
		stringBuilder.AppendLine("Platform: " + Application.platform);
		stringBuilder.AppendLine("Screen resolution: " + Screen.currentResolution.width + "x" + Screen.currentResolution.height + "x" + (int)Screen.currentResolution.refreshRateRatio.value);
		stringBuilder.AppendLine("Window resolution: " + Screen.width + "x" + Screen.height);
		stringBuilder.AppendLine("DPI: " + Screen.dpi);
		stringBuilder.AppendLine("Rendering Threading Mode: " + SystemInfo.renderingThreadingMode);
		stringBuilder.AppendLine("CLR: " + Environment.Version);
		stringBuilder.AppendLine("Modding runtime: " + s_ModdingRuntime);
		Type type = Type.GetType("Mono.Runtime");
		if (type != null)
		{
			MethodInfo method = type.GetMethod("GetDisplayName", BindingFlags.Static | BindingFlags.NonPublic);
			if (method != null)
			{
				stringBuilder.AppendLine("Scripting runtime: Mono " + method.Invoke(null, null));
			}
		}
		return stringBuilder.ToString().TrimEnd();
	}

	private static Dictionary<string, string> GetDefaultBacktraceAttributes()
	{
		return new Dictionary<string, string>
		{
			["game.version"] = Version.current.fullVersion,
			["cohtml.version"] = Versioning.Build.ToString(),
			["pdxsdk.version"] = SDKVersion.Version,
			["atl.version"] = ATL.Version.getVersion(),
			["game.moddingRuntime"] = s_ModdingRuntime
		};
	}

	private static void ListHarmonyPatches()
	{
		ILog logger = LogManager.GetLogger("Modding");
		logger.InfoFormat("Modding runtime: {0}", s_ModdingRuntime);
		try
		{
			LocalTypeCache localTypeCache = new LocalTypeCache();
			Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault((Assembly a) => a.GetName().Name.Contains("Harmony"));
			if (assembly == null)
			{
				return;
			}
			log.Info("Harmony found.");
			Type type = assembly.GetType("Harmony.HarmonyInstance", throwOnError: false) ?? assembly.GetType("HarmonyLib.Harmony", throwOnError: false);
			if (type == null)
			{
				logger.Info("HarmonyInstance/Harmony class not found.");
				return;
			}
			MethodInfo method = localTypeCache.GetMethod(type, "GetAllPatchedMethods", BindingFlags.Static | BindingFlags.Public);
			if (method == null)
			{
				logger.Info("Method GetAllPatchedMethods not found.");
				return;
			}
			if (!(method.Invoke(null, null) is IEnumerable<MethodBase> enumerable))
			{
				logger.Info("No patched methods found.");
				return;
			}
			MethodInfo method2 = localTypeCache.GetMethod(type, "GetPatchInfo", BindingFlags.Static | BindingFlags.Public);
			if (method2 == null)
			{
				logger.Info("Method GetPatchInfo not found.");
				return;
			}
			Type type2 = assembly.GetType("HarmonyLib.Patches", throwOnError: false);
			if (type2 == null)
			{
				logger.Info("Patches class not found.");
				return;
			}
			foreach (MethodBase item in enumerable)
			{
				logger.InfoFormat("Patched Method: {0}.{1}", item.DeclaringType?.FullName ?? "<Global Type>", item.Name);
				object patchInfo = method2.Invoke(null, new object[1] { item });
				PrintPatchDetails(logger, patchInfo, type2, localTypeCache);
			}
		}
		catch (Exception exception)
		{
			log.Warn(exception, "ListHarmonyPatches failed");
		}
	}

	private static void PrintPatchDetails(ILog moddingLog, object patchInfo, Type patchInfoType, LocalTypeCache typeCache)
	{
		if (patchInfo != null)
		{
			FieldInfo field = typeCache.GetField(patchInfoType, "Prefixes");
			FieldInfo field2 = typeCache.GetField(patchInfoType, "Postfixes");
			FieldInfo field3 = typeCache.GetField(patchInfoType, "Transpilers");
			FieldInfo field4 = typeCache.GetField(patchInfoType, "Finalizers");
			IEnumerable<object> patches = field?.GetValue(patchInfo) as IEnumerable<object>;
			IEnumerable<object> patches2 = field2?.GetValue(patchInfo) as IEnumerable<object>;
			IEnumerable<object> patches3 = field3?.GetValue(patchInfo) as IEnumerable<object>;
			IEnumerable<object> patches4 = field4?.GetValue(patchInfo) as IEnumerable<object>;
			PrintIndividualPatches(moddingLog, "Prefixes", patches, typeCache);
			PrintIndividualPatches(moddingLog, "Postfixes", patches2, typeCache);
			PrintIndividualPatches(moddingLog, "Transpilers", patches3, typeCache);
			PrintIndividualPatches(moddingLog, "Finalizers", patches4, typeCache);
		}
	}

	private static void PrintIndividualPatches(ILog moddingLog, string patchType, IEnumerable<object> patches, LocalTypeCache typeCache)
	{
		if (patches == null || !patches.Any())
		{
			return;
		}
		moddingLog.InfoFormat(" {0}:", patchType);
		using (moddingLog.indent.scoped)
		{
			foreach (object patch in patches)
			{
				MethodBase methodBase = typeCache.GetProperty(patch.GetType(), "PatchMethod").GetValue(patch, null) as MethodBase;
				if (methodBase != null)
				{
					string p = methodBase.DeclaringType?.FullName ?? "<Global Method>";
					moddingLog.InfoFormat("Patch Method: {0}.{1}", p, methodBase.Name);
				}
			}
		}
	}

	private static void DetectModdingRuntime()
	{
		s_ModdingRuntime = DetectModdingRuntimeName();
	}

	private static string DetectModdingRuntimeName()
	{
		try
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			Assembly[] array = assemblies;
			foreach (Assembly assembly in array)
			{
				if (assembly.GetName().Name.Equals("BepInEx", StringComparison.OrdinalIgnoreCase))
				{
					return $"{assembly.GetName().Name} {assembly.GetName().Version}";
				}
			}
			array = assemblies;
			foreach (Assembly assembly2 in array)
			{
				if (assembly2.GetName().Name.Contains("BepInEx", StringComparison.OrdinalIgnoreCase))
				{
					return $"{assembly2.GetName().Name} {assembly2.GetName().Version}";
				}
				if (assembly2.GetTypes().Any((Type t) => t.Namespace != null && t.Namespace.StartsWith("BepInEx")))
				{
					return $"{assembly2.GetName().Name} {assembly2.GetName().Version}";
				}
			}
			return "Builtin";
		}
		catch
		{
			return "Unknown";
		}
	}

	private static void SetNativeStackTrace()
	{
		Application.SetStackTraceLogType(LogType.Assert, StackTraceLogType.Full);
		Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.Full);
		Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.Full);
		Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.Full);
		Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.Full);
		UnityEngine.Debug.Log("Game version: " + Version.current.fullVersion);
		UnityEngine.Debug.Log(GetSystemInfoString());
	}

	private bool HandleConfiguration()
	{
		if (configuration.showHelp != null)
		{
			Console.WriteLine(configuration.showHelp);
			return false;
		}
		if (!string.IsNullOrEmpty(configuration.profilerTarget))
		{
			Profiler.logFile = configuration.profilerTarget;
			Profiler.enableBinaryLog = true;
			Profiler.enabled = true;
		}
		return true;
	}

	private void HandleUserFolderVersion()
	{
		try
		{
			Colossal.Version version = Version.current;
			string path = EnvPath.kUserDataPath + "/version";
			if (LongFile.Exists(path))
			{
				version = new Colossal.Version(LongFile.ReadAllText(path));
			}
			if (version < Version.current)
			{
				UnityEngine.Debug.Log("Persistent folder version is outdated " + version.fullVersion + " (Game: " + Version.current.fullVersion + ")");
				if (version < new Colossal.Version("1.0.6f2"))
				{
					UnityEngine.Debug.Log("User settings deleted due to outdated persistent folder version. Backups were created ending with ~");
					DeleteSettings(EnvPath.kUserDataPath);
					PlayerPrefs.DeleteAll();
					PlayerPrefs.Save();
				}
			}
			LongFile.WriteAllText(path, Version.current.fullVersion);
		}
		catch (Exception exception)
		{
			log.Error(exception);
		}
		static void DeleteSettings(string settingsPath)
		{
			foreach (FileInfo item in new DirectoryInfo(settingsPath).EnumerateFiles("*", SearchOption.AllDirectories))
			{
				if (item.Extension.ToLower() == ".coc")
				{
					string text = Path.ChangeExtension(item.FullName, ".coc~");
					LongFile.Delete(text);
					item.MoveTo(text);
				}
			}
		}
	}

	private void InitializeLocalization()
	{
		m_Cts.Token.ThrowIfCancellationRequested();
		localizationManager = new LocalizationManager("en-US", SystemLanguage.English, "English");
		localizationManager.LoadAvailableLocales();
	}

	private void CreateWorld()
	{
		m_Cts.Token.ThrowIfCancellationRequested();
		log.Info("Creating ECS world");
		CORuntimeApplication.Initialize();
		m_World = new World("Game");
		World.DefaultGameObjectInjectionWorld = m_World;
		m_PrefabSystem = m_World.GetOrCreateSystemManaged<PrefabSystem>();
		m_UpdateSystem = m_World.GetOrCreateSystemManaged<UpdateSystem>();
		m_DeserializationSystem = m_World.GetOrCreateSystemManaged<LoadGameSystem>();
		m_SerializationSystem = m_World.GetOrCreateSystemManaged<SaveGameSystem>();
	}

	private void CreateSystems()
	{
		m_Cts.Token.ThrowIfCancellationRequested();
		log.Info("Creating ECS systems");
		SystemOrder.Initialize(m_UpdateSystem);
		userInterface.view.AudioSource = AudioManager.instance.UIHtmlAudioSource;
		Telemetry.gameplayData = new Telemetry.GameplayData(m_World);
	}

	private void UpdateWorld()
	{
		if (shouldUpdateWorld)
		{
			CORuntimeApplication.ResetUpdateAllocator(m_World);
			m_UpdateSystem.Update(SystemUpdatePhase.MainLoop);
		}
	}

	private void PostUpdateWorld()
	{
		if (shouldUpdateWorld)
		{
			m_UpdateSystem.Update(SystemUpdatePhase.Cleanup);
		}
	}

	private void LateUpdateWorld()
	{
		if (shouldUpdateWorld)
		{
			m_UpdateSystem.Update(SystemUpdatePhase.LateUpdate);
			m_UpdateSystem.Update(SystemUpdatePhase.DebugGizmos);
			CORuntimeApplication.Update();
		}
	}

	private void DestroyWorld()
	{
		Telemetry.gameplayData = null;
		World.DisposeAllWorlds();
		CORuntimeApplication.Shutdown();
		m_State = State.WorldDisposed;
	}

	public void TakeScreenshot()
	{
		StartCoroutine(CaptureScreenshot());
	}

	private IEnumerator CaptureScreenshot()
	{
		yield return new WaitForEndOfFrame();
		ScreenUtility.CaptureScreenshot();
	}

	Coroutine ICoroutineHost.StartCoroutine(IEnumerator routine)
	{
		return StartCoroutine(routine);
	}
}

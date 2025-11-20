using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Colossal;
using Colossal.Entities;
using Colossal.IO.AssetDatabase;
using Colossal.PSI.Common;
using Colossal.Serialization.Entities;
using Colossal.UI;
using Colossal.UI.Binding;
using Game.Areas;
using Game.Assets;
using Game.City;
using Game.Modding;
using Game.Prefabs;
using Game.Prefabs.Modes;
using Game.PSI.PdxSdk;
using Game.SceneFlow;
using Game.Serialization;
using Game.Settings;
using Game.Simulation;
using Game.UI.InGame;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace Game.UI.Menu;

[CompilerGenerated]
public class MenuUISystem : UISystemBase, IPreDeserialize
{
	private enum MapFilter
	{
		None = -1,
		Default,
		Custom
	}

	public enum MenuScreen
	{
		Menu,
		NewGame,
		LoadGame,
		Options,
		Credits
	}

	public class ThemeInfo : IJsonWritable
	{
		public string id { get; set; }

		public string icon { get; set; }

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin("menu.ThemeInfo");
			writer.PropertyName("id");
			writer.Write(id);
			writer.PropertyName("icon");
			writer.Write(icon);
			writer.TypeEnd();
		}
	}

	public struct NewGameArgs : IJsonReadable
	{
		public string mapId;

		public string cityName;

		public string theme;

		public Dictionary<string, bool> options;

		public string gameMode;

		public void Read(IJsonReader reader)
		{
			reader.ReadMapBegin();
			reader.ReadProperty("mapId");
			reader.Read(out mapId);
			reader.ReadProperty("cityName");
			reader.Read(out cityName);
			reader.ReadProperty("theme");
			reader.Read(out theme);
			reader.ReadProperty("options");
			ulong num = reader.ReadMapBegin();
			options = new Dictionary<string, bool>((int)num);
			for (ulong num2 = 0uL; num2 < num; num2++)
			{
				reader.ReadMapKeyValue();
				reader.Read(out string value);
				reader.Read(out bool value2);
				options.Add(value, value2);
			}
			reader.ReadMapEnd();
			reader.ReadProperty("gameMode");
			reader.Read(out gameMode);
			reader.ReadMapEnd();
		}
	}

	public struct LoadGameArgs : IJsonReadable
	{
		public string saveId;

		public string cityName;

		public Dictionary<string, bool> options;

		public string gameMode;

		public void Read(IJsonReader reader)
		{
			reader.ReadMapBegin();
			reader.ReadProperty("saveId");
			reader.Read(out saveId);
			reader.ReadProperty("cityName");
			reader.Read(out cityName);
			reader.ReadProperty("options");
			ulong num = reader.ReadMapBegin();
			options = new Dictionary<string, bool>((int)num);
			for (ulong num2 = 0uL; num2 < num; num2++)
			{
				reader.ReadMapKeyValue();
				reader.Read(out string value);
				reader.Read(out bool value2);
				options.Add(value, value2);
			}
			reader.ReadMapEnd();
			reader.ReadProperty("gameMode");
			reader.Read(out gameMode);
			reader.ReadMapEnd();
		}
	}

	private class GameOptions : IJsonWritable
	{
		public bool unlockAll;

		public bool unlimitedMoney;

		public bool unlockMapTiles;

		public HashSet<string> usedMods;

		public GameOptions(CityConfigurationSystem cityConfigurationSystem)
		{
			unlockAll = cityConfigurationSystem.unlockAll;
			unlimitedMoney = cityConfigurationSystem.unlimitedMoney;
			unlockMapTiles = cityConfigurationSystem.unlockMapTiles;
			usedMods = cityConfigurationSystem.usedMods;
		}

		public void Write(IJsonWriter writer)
		{
			writer.MapBegin(4u);
			writer.Write("unlockAll");
			writer.Write(unlockAll);
			writer.Write("unlimitedMoney");
			writer.Write(unlimitedMoney);
			writer.Write("unlockMapTiles");
			writer.Write(unlockMapTiles);
			writer.Write("usedMods");
			writer.ArrayBegin(usedMods.Count);
			foreach (string item in usedMods)
			{
				writer.Write(item);
			}
			writer.ArrayEnd();
			writer.MapEnd();
		}
	}

	private class DefaultGameOptions : IJsonWritable
	{
		public bool leftHandTraffic => SharedSettings.instance.userState.leftHandTraffic;

		public bool naturalDisasters => SharedSettings.instance.userState.naturalDisasters;

		public bool unlockAll => SharedSettings.instance.userState.unlockAll;

		public bool unlimitedMoney => SharedSettings.instance.userState.unlimitedMoney;

		public bool unlockMapTiles => SharedSettings.instance.userState.unlockMapTiles;

		public void Write(IJsonWriter writer)
		{
			writer.MapBegin(5u);
			writer.Write("leftHandTraffic");
			writer.Write(leftHandTraffic);
			writer.Write("naturalDisasters");
			writer.Write(naturalDisasters);
			writer.Write("unlockAll");
			writer.Write(unlockAll);
			writer.Write("unlimitedMoney");
			writer.Write(unlimitedMoney);
			writer.Write("unlockMapTiles");
			writer.Write(unlockMapTiles);
			writer.MapEnd();
		}

		public Dictionary<string, bool> MergeOptions(Dictionary<string, bool> options)
		{
			Dictionary<string, bool> dictionary = new Dictionary<string, bool>
			{
				["leftHandTraffic"] = leftHandTraffic,
				["naturalDisasters"] = naturalDisasters,
				["unlockAll"] = unlockAll,
				["unlimitedMoney"] = unlimitedMoney,
				["unlockMapTiles"] = unlockMapTiles
			};
			if (options != null)
			{
				foreach (KeyValuePair<string, bool> option in options)
				{
					dictionary[option.Key] = option.Value;
				}
			}
			return dictionary;
		}
	}

	public struct SaveabilityStatus : IJsonWritable
	{
		public bool canSave;

		public string reasonHash;

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin("SaveabilityStatus");
			writer.PropertyName("canSave");
			writer.Write(canSave);
			writer.PropertyName("reasonHash");
			if (canSave)
			{
				writer.WriteNull();
			}
			else
			{
				writer.Write(reasonHash);
			}
			writer.TypeEnd();
		}
	}

	public struct MapResourceMultipliers : IJsonWritable
	{
		public float boost;

		public float fertile;

		public float forest;

		public float ore;

		public float oil;

		public float fish;

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(typeof(MapResourceMultipliers).FullName);
			writer.PropertyName("boost");
			writer.Write(boost);
			writer.PropertyName("fertile");
			writer.Write(fertile);
			writer.PropertyName("forest");
			writer.Write(forest);
			writer.PropertyName("ore");
			writer.Write(ore);
			writer.PropertyName("oil");
			writer.Write(oil);
			writer.PropertyName("fish");
			writer.Write(fish);
			writer.TypeEnd();
		}
	}

	private const string kPreviewName = "SaveGamePanel";

	private const int kPreviewWidth = 680;

	private const int kPreviewHeight = 383;

	private const string kGroup = "menu";

	private PrefabSystem m_PrefabSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private TimeSystem m_TimeSystem;

	private MapMetadataSystem m_MapMetadataSystem;

	private StandaloneAssetUploadPanelUISystem m_AssetUploadPanelUISystem;

	private GameScreenUISystem m_GameScreenUISystem;

	private GameModeSystem m_GameModeSystem;

	private ValueBinding<int> m_ActiveScreenBinding;

	private GetterValueBinding<List<ThemeInfo>> m_ThemesBinding;

	private ValueBinding<List<MapInfo>> m_MapsBinding;

	private ValueBinding<HashSet<int>> m_AvailableMapFilters;

	private ValueBinding<int> m_SelectedMapFilter;

	private GetterValueBinding<List<GameModeInfo>> m_GameModesBinding;

	private ValueBinding<string> m_CurrentGameModeBinding;

	private ValueBinding<List<SaveInfo>> m_SavesBinding;

	private ValueBinding<string> m_SavePreviewBinding;

	private ValueBinding<string> m_LastSaveNameBinding;

	private ValueBinding<int> m_SaveGameSlotsBinding;

	private GetterValueBinding<SaveabilityStatus> m_SaveabilityBinding;

	private ValueBinding<List<string>> m_AvailableCloudTargetsBinding;

	private GetterValueBinding<string> m_SelectedCloudTargetBinding;

	private DefaultGameOptions m_DefaultGameOptions;

	private MenuHelpers.SaveGamePreviewSettings m_PreviewSettings = new MenuHelpers.SaveGamePreviewSettings();

	private EntityQuery m_XPQuery;

	private bool m_IsLoading;

	private string m_LastSelectedCloudTarget;

	private PdxModsUI m_ModsUI;

	private static int s_PreviewId;

	public MenuScreen activeScreen
	{
		get
		{
			return (MenuScreen)m_ActiveScreenBinding.value;
		}
		set
		{
			m_ActiveScreenBinding.Update((int)value);
		}
	}

	private bool IsEditorEnabled()
	{
		if (!GameManager.instance.configuration.disableModding)
		{
			return Platform.PC.IsPlatformSet(Application.platform);
		}
		return false;
	}

	protected override void OnWorldReady()
	{
		GameManager.instance.userInterface.appBindings.UpdateOwnedPrerequisiteBinding();
		m_PrefabSystem.onContentAvailabilityChanged += OnContentAvailabilityChanged;
		AssetDatabase.global.onAssetDatabaseChanged.Subscribe(UpdateClouds, delegate(AssetChangedEventArgs args)
		{
			ChangeType change = args.change;
			return change == ChangeType.DatabaseRegistered || change == ChangeType.DatabaseUnregistered;
		}, AssetChangedEventArgs.Default);
		AssetDatabase.global.onAssetDatabaseChanged.Subscribe<MapMetadata>(UpdateMaps, AssetChangedEventArgs.Default);
		AssetDatabase.global.onAssetDatabaseChanged.Subscribe<SaveGameMetadata>(UpdateSaves, AssetChangedEventArgs.Default);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_TimeSystem = base.World.GetOrCreateSystemManaged<TimeSystem>();
		m_MapMetadataSystem = base.World.GetOrCreateSystemManaged<MapMetadataSystem>();
		m_AssetUploadPanelUISystem = base.World.GetOrCreateSystemManaged<StandaloneAssetUploadPanelUISystem>();
		m_GameScreenUISystem = base.World.GetOrCreateSystemManaged<GameScreenUISystem>();
		m_GameModeSystem = base.World.GetOrCreateSystemManaged<GameModeSystem>();
		m_DefaultGameOptions = new DefaultGameOptions();
		AssetDatabase.global.LoadSettings("Save Preview Settings", m_PreviewSettings);
		AddBinding(m_ActiveScreenBinding = new ValueBinding<int>("menu", "activeScreen", 0));
		AddBinding(new ValueBinding<bool>("menu", "canExitGame", !Application.isConsolePlatform));
		AddBinding(new ValueBinding<string>("menu", "gameVersion", Version.current.fullVersion));
		AddBinding(m_SavePreviewBinding = new ValueBinding<string>("menu", "savePreview", null, ValueWriters.Nullable(new StringWriter())));
		AddUpdateBinding(new GetterValueBinding<string>("menu", "mapName", () => m_MapMetadataSystem.mapName, ValueWriters.Nullable(new StringWriter())));
		AddBinding(m_LastSaveNameBinding = new ValueBinding<string>("menu", "lastSaveName", null, ValueWriters.Nullable(new StringWriter())));
		int initialValue = -1;
		AddBinding(m_SaveGameSlotsBinding = new ValueBinding<int>("menu", "saveGameSlots", initialValue));
		AddBinding(m_AvailableCloudTargetsBinding = new ValueBinding<List<string>>("menu", "availableCloudTargets", MenuHelpers.GetAvailableCloudTargets(), new ListWriter<string>()));
		AddUpdateBinding(m_SelectedCloudTargetBinding = new GetterValueBinding<string>("menu", "selectedCloudTarget", () => MenuHelpers.GetSanitizedCloudTarget(SharedSettings.instance.userState.lastCloudTarget).name, ValueWriters.Nullable(new StringWriter())));
		AddBinding(m_SaveabilityBinding = new GetterValueBinding<SaveabilityStatus>("menu", "saveabilityStatus", GetSaveabilityStatus, new ValueWriter<SaveabilityStatus>()));
		AddBinding(m_ThemesBinding = new GetterValueBinding<List<ThemeInfo>>("menu", "themes", GetThemes, new ListWriter<ThemeInfo>(new ValueWriter<ThemeInfo>())));
		AddBinding(m_MapsBinding = new ValueBinding<List<MapInfo>>("menu", "maps", new List<MapInfo>(), new ListWriter<MapInfo>(new ValueWriter<MapInfo>())));
		AddBinding(m_AvailableMapFilters = new ValueBinding<HashSet<int>>("menu", "availableMapFilters", GetAvailableMapFilters(), new CollectionWriter<int>()));
		AddBinding(m_SelectedMapFilter = new ValueBinding<int>("menu", "selectedMapFilter", 0));
		AddBinding(m_SavesBinding = new ValueBinding<List<SaveInfo>>("menu", "saves", new List<SaveInfo>(), new ListWriter<SaveInfo>(new ValueWriter<SaveInfo>())));
		AddBinding(m_GameModesBinding = new GetterValueBinding<List<GameModeInfo>>("menu", "gameModes", m_GameModeSystem.GetGameModeInfo, new ListWriter<GameModeInfo>(new ValueWriter<GameModeInfo>())));
		AddBinding(m_CurrentGameModeBinding = new ValueBinding<string>("menu", "gameMode", m_GameModeSystem.currentModeName));
		AddBinding(new CallBinding<string, MapResourceMultipliers>("menu", "getMapResourceMultipliers", delegate(string mode)
		{
			MapResourceMultipliers result = new MapResourceMultipliers
			{
				boost = 1f,
				fertile = 1f,
				forest = 1f,
				oil = 1f,
				ore = 1f,
				fish = 1f
			};
			ModeSetting modeSetting = m_GameModeSystem.GetModeSetting(mode);
			if ((object)modeSetting != null)
			{
				result.boost = (modeSetting.m_EnableAdjustNaturalResources ? modeSetting.m_InitialNaturalResourceBoostMultiplier : 1f);
				for (int i = 0; i < modeSetting.m_ModePrefabs.Count; i++)
				{
					if (modeSetting.m_ModePrefabs[i] is IMapResourceMultiplier mapResourceMultiplier)
					{
						if (mapResourceMultiplier.TryGetMultiplier(MapFeature.FertileLand, out var multiplier))
						{
							result.fertile = multiplier;
						}
						if (mapResourceMultiplier.TryGetMultiplier(MapFeature.Forest, out multiplier))
						{
							result.forest = multiplier;
						}
						if (mapResourceMultiplier.TryGetMultiplier(MapFeature.Ore, out multiplier))
						{
							result.ore = multiplier;
						}
						if (mapResourceMultiplier.TryGetMultiplier(MapFeature.Oil, out multiplier))
						{
							result.oil = multiplier;
						}
						if (mapResourceMultiplier.TryGetMultiplier(MapFeature.Fish, out multiplier))
						{
							result.fish = multiplier;
						}
					}
				}
			}
			return result;
		}));
		AddBinding(new GetterValueBinding<List<string>>("menu", "creditFiles", GetCreditFiles, new ListWriter<string>(new StringWriter())));
		AddUpdateBinding(new GetterValueBinding<DefaultGameOptions>("menu", "defaultGameOptions", () => m_DefaultGameOptions, new ValueWriter<DefaultGameOptions>()));
		AddUpdateBinding(new GetterValueBinding<GameOptions>("menu", "gameOptions", GetGameOptions, new ValueWriter<GameOptions>()));
		AddUpdateBinding(new GetterValueBinding<bool>("menu", "modsEnabled", ModManager.AreModsEnabled));
		AddUpdateBinding(new GetterValueBinding<bool>("menu", "pdxModsUIEnabled", IsPdxModsUIEnabled));
		AddBinding(new ValueBinding<bool>("menu", "hideModsUIButton", !IsModdingEnabled()));
		AddBinding(new ValueBinding<bool>("menu", "hideEditorButton", !IsEditorEnabled()));
		AddBinding(new ValueBinding<bool>("menu", "displayModdingBetaBanners", initialValue: true));
		AddUpdateBinding(new GetterValueBinding<bool>("menu", "hasCompletedTutorials", () => SharedSettings.instance.userState.shownTutorials.ContainsValue(value: true)));
		AddUpdateBinding(new GetterValueBinding<bool>("menu", "showTutorials", () => SharedSettings.instance.gameplay.showTutorials));
		AddUpdateBinding(new GetterValueBinding<bool>("menu", "dismissLoadGameConfirmation", () => SharedSettings.instance.userInterface.dismissedConfirmations.Contains("LoadGame")));
		AddUpdateBinding(new GetterValueBinding<bool>("menu", "isModsUIActive", IsModsUIActive));
		AddBinding(new GetterValueBinding<int>("menu", "citySizeWarningThreshold", GetPopulationGoal));
		AddBinding(new TriggerBinding<int>("menu", "setActiveScreen", m_ActiveScreenBinding.Update));
		AddBinding(new TriggerBinding("menu", "continueGame", SafeContinueGame));
		AddBinding(new TriggerBinding<NewGameArgs, bool>("menu", "newGame", SafeNewGame, new ValueReader<NewGameArgs>()));
		AddBinding(new TriggerBinding<LoadGameArgs, bool>("menu", "loadGame", SafeLoadGame, new ValueReader<LoadGameArgs>()));
		AddBinding(new TriggerBinding<string>("menu", "saveGame", SafeSaveGame));
		AddBinding(new TriggerBinding<string>("menu", "deleteSave", DeleteSave));
		AddBinding(new TriggerBinding<string>("menu", "shareSave", ShareSave));
		AddBinding(new TriggerBinding<string>("menu", "shareMap", ShareMap));
		AddBinding(new TriggerBinding("menu", "quicksave", SafeQuickSave));
		AddBinding(new TriggerBinding<bool>("menu", "quickload", SafeQuickLoad));
		AddBinding(new TriggerBinding("menu", "startEditor", StartEditor));
		AddBinding(new TriggerBinding("menu", "showPdxModsUI", ShowModsUI));
		AddBinding(new TriggerBinding("menu", "exitToMainMenu", ExitToMainMenu));
		AddBinding(new TriggerBinding<bool>("menu", "onSaveGameScreenVisibilityChanged", OnSaveGameScreenVisibilityChanged));
		AddBinding(new TriggerBinding<bool, bool>("menu", "applyTutorialSettings", ApplyTutorialSettings));
		AddBinding(new TriggerBinding<string>("menu", "selectCloudTarget", SelectCloudTarget));
		AddBinding(new TriggerBinding<int>("menu", "selectMapFilter", OnSelectMapFilter));
		m_XPQuery = GetEntityQuery(ComponentType.ReadOnly<XP>());
		m_LastSelectedCloudTarget = SharedSettings.instance.userState.lastCloudTarget;
		m_ModsUI = new PdxModsUI();
	}

	private void OnContentAvailabilityChanged(ContentPrefab contentPrefab)
	{
		GameManager.instance.userInterface.appBindings.UpdateOwnedPrerequisiteBinding();
		UpdateMaps();
		UpdateSaves();
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_ModsUI.Dispose();
		AssetDatabase.global.onAssetDatabaseChanged.Unsubscribe(UpdateClouds);
		AssetDatabase.global.onAssetDatabaseChanged.Unsubscribe(UpdateMaps);
		AssetDatabase.global.onAssetDatabaseChanged.Unsubscribe(UpdateSaves);
		m_PrefabSystem.onContentAvailabilityChanged -= OnContentAvailabilityChanged;
		base.OnDestroy();
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		string currentModeName = m_GameModeSystem.currentModeName;
		m_CurrentGameModeBinding.Update((currentModeName == string.Empty) ? "NormalMode" : currentModeName);
	}

	private bool IsModsUIActive()
	{
		return m_ModsUI.isActive;
	}

	private int GetPopulationGoal()
	{
		if (Platform.Consoles.IsPlatformSet(Application.platform))
		{
			return base.EntityManager.GetComponentData<PopulationVictoryConfigurationData>(GetEntityQuery(ComponentType.ReadOnly<PopulationVictoryConfigurationData>()).GetSingletonEntity()).m_populationGoal;
		}
		return -1;
	}

	private void UpdateClouds(AssetChangedEventArgs args)
	{
		GameManager.instance.RunOnMainThread(delegate
		{
			m_AvailableCloudTargetsBinding.Update(MenuHelpers.GetAvailableCloudTargets());
			m_SelectedCloudTargetBinding.Update();
		});
	}

	private void UpdateMaps(AssetChangedEventArgs args)
	{
		GameManager.instance.RunOnMainThread(UpdateMaps);
	}

	private void UpdateMaps()
	{
		m_AvailableMapFilters.Update(GetAvailableMapFilters());
		if (!m_AvailableMapFilters.value.Contains(m_SelectedMapFilter.value))
		{
			m_SelectedMapFilter.Update((m_AvailableMapFilters.value.Count > 0) ? m_AvailableMapFilters.value.First() : (-1));
		}
		MenuHelpers.UpdateMeta(m_MapsBinding, FilterMaps);
	}

	private void UpdateSaves()
	{
		MenuHelpers.UpdateMeta(m_SavesBinding);
		GameManager.instance.userInterface.appBindings.UpdateCanContinueBinding();
	}

	private void UpdateSaves(AssetChangedEventArgs args)
	{
		GameManager.instance.RunOnMainThread(UpdateSaves);
	}

	private void ApplyTutorialSettings(bool showTutorials, bool resetTutorials)
	{
		SharedSettings.instance.gameplay.showTutorials = showTutorials;
		if (resetTutorials)
		{
			SharedSettings.instance.userState.ResetTutorials();
		}
	}

	public void PreDeserialize(Context context)
	{
		if (context.purpose == Purpose.Cleanup)
		{
			m_ActiveScreenBinding.Update(0);
		}
	}

	private void OnSaveGameScreenVisibilityChanged(bool visible)
	{
		if (visible)
		{
			m_SavePreviewBinding.Update(string.Format("{0}{1}/{2}?width={3}&height={4}&op={5}&{6}#{7}", "screencapture://", Camera.main.tag.ToLowerInvariant(), "SaveGamePanel", 680, 383, "Screenshot", m_PreviewSettings.ToUri(), s_PreviewId++));
		}
		else
		{
			m_SavePreviewBinding.Update(null);
		}
	}

	private bool IsModdingEnabled()
	{
		return !GameManager.instance.configuration.disableModding;
	}

	private bool IsPdxModsUIEnabled()
	{
		if (!GameManager.instance.configuration.disablePDXSDK && !GameManager.instance.configuration.disableModding)
		{
			return !PlatformManager.instance.isOfflineOnly;
		}
		return false;
	}

	private async void ShowModsUI()
	{
		if (PlatformManager.instance.hasUgcPrivilege)
		{
			if (Platform.PlayStation.IsPlatformSet(Application.platform))
			{
				GameManager.instance.userInterface.paradoxBindings.OnPSModsUIOpened(m_ModsUI.Show);
				m_ModsUI.platform.onModsUIClosed -= OnPSModsUIClosed;
				m_ModsUI.platform.onModsUIClosed += OnPSModsUIClosed;
			}
			else
			{
				m_ModsUI.Show();
			}
		}
		else
		{
			await PlatformManager.instance.TryResolveUgcPrivilege();
			if (PlatformManager.instance.hasUgcPrivilege)
			{
				m_ModsUI.Show();
			}
		}
	}

	private async void OnPSModsUIClosed()
	{
		HashSet<Mod> hashSet = await m_ModsUI.platform.GetModsInActivePlayset();
		if (hashSet != null && hashSet.Count > 0)
		{
			GameManager.instance.userInterface.paradoxBindings.OnPSModsUIClosed(null, m_ModsUI.platform.DeactivateActivePlayset, m_ModsUI.Show);
		}
	}

	private List<string> GetCreditFiles()
	{
		return new List<string> { "Media/Menu/Credits.md", "Media/Menu/Licenses.md" };
	}

	private List<ThemeInfo> GetThemes()
	{
		return new List<ThemeInfo>
		{
			new ThemeInfo
			{
				id = "European",
				icon = "Media/Game/Themes/European.svg"
			},
			new ThemeInfo
			{
				id = "North American",
				icon = "Media/Game/Themes/North American.svg"
			}
		};
	}

	private void SafeContinueGame()
	{
		TaskManager.instance.EnqueueTask("SaveLoadGame", ContinueGame, 1);
	}

	private async Task ContinueGame()
	{
		try
		{
			SaveGameMetadata lastSave = GameManager.instance.settings.userState.lastSaveGameMetadata;
			if (lastSave != null && lastSave.isValidSaveGame)
			{
				m_MapMetadataSystem.mapName = lastSave.target.mapName;
				PlatformManager.instance.achievementsEnabled = !lastSave.target.isReadonly;
				await GameManager.instance.Load(GameMode.Game, Purpose.LoadGame, lastSave);
				SaveInfo target = lastSave.target;
				m_LastSaveNameBinding.Update(target.autoSave ? null : target.displayName);
				if (!target.autoSave)
				{
					m_LastSelectedCloudTarget = target.metaData.database.dataSource.remoteStorageSourceName;
				}
			}
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception exception)
		{
			COSystemBase.baseLog.Error(exception);
		}
	}

	private void SafeNewGame(NewGameArgs args, bool dismiss)
	{
		TaskManager.instance.EnqueueTask("SaveLoadGame", () => NewGame(args, dismiss), 1);
	}

	private async Task NewGame(NewGameArgs args, bool dismiss)
	{
		try
		{
			if (dismiss)
			{
				SharedSettings.instance.userInterface.AddDismissedConfirmation("LoadGame");
			}
			MapInfo mapInfo = m_MapsBinding.value.Find((MapInfo x) => x.id == args.mapId);
			m_MapMetadataSystem.mapName = mapInfo.displayName;
			m_CityConfigurationSystem.overrideLoadedOptions = true;
			m_CityConfigurationSystem.overrideThemeName = args.theme;
			m_GameModeSystem.overrideMode = args.gameMode;
			ApplyOptions(args.cityName, args.options);
			PlatformManager.instance.achievementsEnabled = true;
			m_TimeSystem.startingYear = ((mapInfo.startingYear != -1) ? mapInfo.startingYear : DateTime.Now.Year);
			await GameManager.instance.Load(GameMode.Game, Purpose.NewGame, mapInfo.metaData);
			m_LastSaveNameBinding.Update(null);
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception exception)
		{
			COSystemBase.baseLog.Error(exception);
		}
	}

	private void SafeLoadGame(LoadGameArgs args, bool dismiss)
	{
		TaskManager.instance.EnqueueTask("SaveLoadGame", () => LoadGame(args, dismiss), 1);
	}

	private async Task LoadGame(LoadGameArgs args, bool dismiss)
	{
		try
		{
			if (dismiss)
			{
				SharedSettings.instance.userInterface.AddDismissedConfirmation("LoadGame");
			}
			SaveInfo saveInfo = m_SavesBinding.value.Find((SaveInfo x) => x.id == args.saveId);
			m_MapMetadataSystem.mapName = saveInfo.mapName;
			m_CityConfigurationSystem.overrideLoadedOptions = true;
			m_CityConfigurationSystem.overrideThemeName = null;
			m_GameModeSystem.overrideMode = args.gameMode;
			ApplyOptions(args.cityName, args.options);
			PlatformManager.instance.achievementsEnabled = !saveInfo.isReadonly;
			await GameManager.instance.Load(GameMode.Game, Purpose.LoadGame, saveInfo.metaData);
			m_LastSaveNameBinding.Update(saveInfo.autoSave ? null : saveInfo.displayName);
			if (!saveInfo.autoSave)
			{
				m_LastSelectedCloudTarget = saveInfo.metaData.database.dataSource.remoteStorageSourceName;
			}
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception exception)
		{
			COSystemBase.baseLog.Error(exception);
		}
	}

	private void ApplyOptions(string cityName, Dictionary<string, bool> options)
	{
		m_CityConfigurationSystem.overrideCityName = cityName;
		if (options != null)
		{
			UserState userState = SharedSettings.instance.userState;
			if (options.TryGetValue("leftHandTraffic", out var value))
			{
				bool leftHandTraffic = (m_CityConfigurationSystem.overrideLeftHandTraffic = value);
				userState.leftHandTraffic = leftHandTraffic;
			}
			if (options.TryGetValue("naturalDisasters", out var value2))
			{
				bool leftHandTraffic = (m_CityConfigurationSystem.overrideNaturalDisasters = value2);
				userState.naturalDisasters = leftHandTraffic;
			}
			if (options.TryGetValue("unlockAll", out var value3))
			{
				bool leftHandTraffic = (m_CityConfigurationSystem.overrideUnlockAll = value3);
				userState.unlockAll = leftHandTraffic;
			}
			if (options.TryGetValue("unlimitedMoney", out var value4))
			{
				bool leftHandTraffic = (m_CityConfigurationSystem.overrideUnlimitedMoney = value4);
				userState.unlimitedMoney = leftHandTraffic;
			}
			if (options.TryGetValue("unlockMapTiles", out var value5))
			{
				bool leftHandTraffic = (m_CityConfigurationSystem.overrideUnlockMapTiles = value5);
				userState.unlockMapTiles = leftHandTraffic;
			}
			userState.ApplyAndSave();
		}
	}

	public SaveInfo GetSaveInfo(bool autoSave)
	{
		CitySystem existingSystemManaged = base.World.GetExistingSystemManaged<CitySystem>();
		DateTime currentDateTime = base.World.GetExistingSystemManaged<TimeSystem>().GetCurrentDateTime();
		Population componentData = base.EntityManager.GetComponentData<Population>(existingSystemManaged.City);
		m_MapMetadataSystem.Update();
		return new SaveInfo
		{
			theme = m_MapMetadataSystem.theme,
			cityName = m_CityConfigurationSystem.cityName,
			population = componentData.m_Population,
			money = existingSystemManaged.moneyAmount,
			xp = existingSystemManaged.XP,
			simulationDate = new SimulationDateTime(currentDateTime.Year, currentDateTime.DayOfYear - 1, currentDateTime.Hour, currentDateTime.Minute),
			options = new Dictionary<string, bool>
			{
				{ "leftHandTraffic", m_CityConfigurationSystem.leftHandTraffic },
				{ "naturalDisasters", m_CityConfigurationSystem.naturalDisasters },
				{ "unlockAll", m_CityConfigurationSystem.unlockAll },
				{ "unlimitedMoney", m_CityConfigurationSystem.unlimitedMoney },
				{ "unlockMapTiles", m_CityConfigurationSystem.unlockMapTiles }
			},
			mapName = m_MapMetadataSystem.mapName,
			autoSave = autoSave,
			modsEnabled = m_CityConfigurationSystem.usedMods.ToArray(),
			gameMode = m_GameModeSystem.currentModeName
		};
	}

	private void SafeSaveGame(string saveName)
	{
		TaskManager.instance.EnqueueTask("SaveLoadGame", () => SaveGame(saveName), 1);
	}

	private async Task SaveGame(string saveName)
	{
		_ = 1;
		try
		{
			Texture savePreview = UIManager.defaultUISystem.userImagesManager.GetUserImageTarget("SaveGamePanel", 680, 383);
			ILocalAssetDatabase targetDatabase = MenuHelpers.GetSanitizedCloudTarget(SharedSettings.instance.userState.lastCloudTarget).db;
			SaveInfo saveInfo = GetSaveInfo(autoSave: false);
			if (await HandlesOverwrite(targetDatabase, saveName))
			{
				m_GameScreenUISystem.SetScreen(GameScreenUISystem.GameScreen.PauseMenu);
				await GameManager.instance.Save(saveName, saveInfo, targetDatabase, savePreview);
				m_LastSaveNameBinding.Update(saveName);
				m_LastSelectedCloudTarget = saveInfo.metaData.database.dataSource.remoteStorageSourceName;
			}
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception exception)
		{
			COSystemBase.baseLog.Error(exception);
		}
	}

	private bool SaveExists(ILocalAssetDatabase database, string name, out PackageAsset asset)
	{
		return database.Exists<PackageAsset>(SaveHelpers.GetAssetDataPath<SaveGameMetadata>(database, name), out asset);
	}

	private Task<bool> HandlesOverwrite(ILocalAssetDatabase database, string saveName)
	{
		if (SaveExists(database, saveName, out var _) && !SharedSettings.instance.userInterface.dismissedConfirmations.Contains("SaveGame"))
		{
			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
			GameManager.instance.RegisterCancellationOnQuit(tcs, stateOnCancel: false);
			GameManager.instance.userInterface.appBindings.ShowConfirmationDialog(new DismissibleConfirmationDialog("Common.DIALOG_TITLE[Warning]", "Common.DIALOG_MESSAGE[Overwrite]", "Common.DIALOG_ACTION[Yes]", "Common.DIALOG_ACTION[No]"), delegate(int msg, bool dismiss)
			{
				if (msg == 0 && dismiss)
				{
					SharedSettings.instance.userInterface.AddDismissedConfirmation("SaveGame");
				}
				tcs.SetResult(msg == 0);
			});
			return tcs.Task;
		}
		return Task.FromResult(result: true);
	}

	private void SafeQuickSave()
	{
		TaskManager.instance.EnqueueTask("SaveLoadGame", QuickSave, 1);
	}

	private async Task QuickSave()
	{
		_ = 1;
		try
		{
			string saveName = m_LastSaveNameBinding.value;
			if (string.IsNullOrEmpty(saveName))
			{
				saveName = m_CityConfigurationSystem.cityName;
			}
			if (string.IsNullOrEmpty(saveName))
			{
				saveName = "SaveGame";
			}
			ILocalAssetDatabase targetDatabase = MenuHelpers.GetSanitizedCloudTarget(m_LastSelectedCloudTarget).db;
			if (targetDatabase.name != null)
			{
				RenderTexture savePreview = ScreenCaptureHelper.CreateRenderTarget("SaveGamePanel", 680, 383);
				ScreenCaptureHelper.CaptureScreenshot(Camera.main, savePreview, m_PreviewSettings);
				SaveInfo saveInfo = GetSaveInfo(autoSave: false);
				if (await HandlesOverwrite(targetDatabase, saveName))
				{
					await GameManager.instance.Save(saveName, saveInfo, targetDatabase, savePreview);
				}
				CoreUtils.Destroy(savePreview);
			}
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception exception)
		{
			COSystemBase.baseLog.Error(exception);
		}
	}

	private void SafeQuickLoad(bool dismiss)
	{
		TaskManager.instance.EnqueueTask("SaveLoadGame", () => QuickLoad(dismiss), 1);
	}

	private async Task QuickLoad(bool dismiss)
	{
		try
		{
			if (dismiss)
			{
				SharedSettings.instance.userInterface.AddDismissedConfirmation("LoadGame");
			}
			if (MenuHelpers.hasPreviouslySavedGame)
			{
				await ContinueGame();
			}
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception exception)
		{
			COSystemBase.baseLog.Error(exception);
		}
	}

	public void DeleteSave(string guid)
	{
		try
		{
			SaveHelpers.DeleteSaveGame(m_SavesBinding.value.Find((SaveInfo x) => x.id == guid).metaData);
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception exception)
		{
			COSystemBase.baseLog.Error(exception);
		}
	}

	public void ShareSave(string id)
	{
		foreach (SaveInfo item in m_SavesBinding.value)
		{
			if (item.id == id)
			{
				m_AssetUploadPanelUISystem.Show(item.metaData);
				break;
			}
		}
	}

	public void ShareMap(string id)
	{
		foreach (MapInfo item in m_MapsBinding.value)
		{
			if (item.id == id)
			{
				m_AssetUploadPanelUISystem.Show(item.metaData);
				break;
			}
		}
	}

	private async void StartEditor()
	{
		try
		{
			m_CityConfigurationSystem.overrideLoadedOptions = false;
			m_CityConfigurationSystem.overrideThemeName = null;
			await GameManager.instance.Load(GameMode.Editor, Purpose.NewMap).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception exception)
		{
			COSystemBase.baseLog.Error(exception);
		}
	}

	private async void ExitToMainMenu()
	{
		try
		{
			m_CityConfigurationSystem.overrideLoadedOptions = false;
			m_CityConfigurationSystem.overrideThemeName = null;
			await GameManager.instance.MainMenu();
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception exception)
		{
			COSystemBase.baseLog.Error(exception);
		}
	}

	private void SelectCloudTarget(string cloudTarget)
	{
		SharedSettings.instance.userState.lastCloudTarget = cloudTarget;
		_ = MenuHelpers.GetSanitizedCloudTarget(cloudTarget).db.dataSource.maxSupportedFileLength;
	}

	private SaveabilityStatus GetSaveabilityStatus()
	{
		int count = MenuHelpers.GetAvailableCloudTargets().Count;
		return new SaveabilityStatus
		{
			canSave = (count > 0),
			reasonHash = ((count > 0) ? null : "NoLocations")
		};
	}

	private GameOptions GetGameOptions()
	{
		return new GameOptions(m_CityConfigurationSystem);
	}

	private static bool IsDefaultAsset(IAssetData asset)
	{
		return asset.database is AssetDatabase<Colossal.IO.AssetDatabase.Game>;
	}

	private HashSet<int> GetAvailableMapFilters()
	{
		HashSet<int> hashSet = new HashSet<int>(2);
		foreach (Metadata<MapInfo> asset in AssetDatabase.global.GetAssets(default(SearchFilter<Metadata<MapInfo>>)))
		{
			hashSet.Add((!IsDefaultAsset(asset)) ? 1 : 0);
		}
		return hashSet;
	}

	private void OnSelectMapFilter(int tab)
	{
		m_SelectedMapFilter.Update(tab);
		UpdateMaps();
	}

	private bool FilterMaps(Metadata<MapInfo> meta)
	{
		if (m_AvailableMapFilters.value.Count > 1)
		{
			if (m_SelectedMapFilter.value < 0)
			{
				return true;
			}
			bool flag = IsDefaultAsset(meta);
			if (m_SelectedMapFilter.value == 0 && flag)
			{
				return GameManager.instance.ArePrerequisitesMet(meta);
			}
			if (m_SelectedMapFilter.value == 1 && !flag)
			{
				return true;
			}
			return false;
		}
		return true;
	}

	[Preserve]
	public MenuUISystem()
	{
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.PSI.Common;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Economy;
using Game.Input;
using Game.Policies;
using Game.Prefabs;
using Game.Prefabs.Modes;
using Game.PSI.Internal;
using Game.SceneFlow;
using Game.Settings;
using Game.Simulation;
using Game.Tools;
using Game.Tutorials;
using Game.UI;
using Game.UI.InGame;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.PSI;

[Telemetry]
public static class Telemetry
{
	private struct HardwarePayload
	{
		public string os_version;

		public int ram;

		public int gfx_memory;

		public int cpucount;

		public string cpu_model;

		public string gpu_model;
	}

	private struct LanguagePayload
	{
		public string os_language;

		public string game_language;
	}

	private struct GraphicsSettingsPayload
	{
		public Helpers.json_displaymode display_mode;

		public string resolution;

		public string graphics_quality;
	}

	private struct AchievementPayload
	{
		public Guid playthrough_id;

		public string achievement_name;

		public int achievement_number;
	}

	private struct TutorialEventPayload
	{
		public Guid playthrough_id;

		public string advice_followed;
	}

	private struct MilestoneUnlockedPayload
	{
		public Guid playthrough_id;

		public int milestone_index;

		public int ingame_days;
	}

	private struct DevNodePurchasedPayload
	{
		public Guid playthrough_id;

		public string dev_node_name;

		public string node_type;

		public int tier_id;
	}

	private struct ControlInputPayload
	{
		public Guid playthrough_id;

		public string control_scheme;
	}

	private struct PanelClosedPayload
	{
		public Guid playthrough_id;

		public string panel_name;

		public double time_spent;
	}

	private struct CityStatsPayload
	{
		public Guid playthrough_id;

		public string map_id;

		public int n_buildings;

		public int population;

		public int happiness;

		public int ingame_days;

		public int map_tiles;

		public int resource_output;

		public int tagged_citizens;

		public int cash_balance;

		public int cash_income;
	}

	private struct ChirperPayload
	{
		public Guid playthrough_id;

		public string message_type;

		public uint likes;
	}

	private struct BuildingPlacedPayload
	{
		public Guid playthrough_id;

		public string map_id;

		public string building_id;

		public string type;

		public int building_level;

		public string coordinates;

		public string origin;
	}

	private struct PolicyPayload
	{
		public Guid playthrough_id;

		public string policy_id;

		public PolicyCategory policy_category;

		public ModifiedSystem.PolicyRange policy_range;
	}

	private struct InputIdleEndPayload
	{
		public Guid playthrough_id;

		public float simulation_speed_start;

		public float simulation_speed_end;

		public double duration;
	}

	public class GameplayData
	{
		private EntityQuery m_PopulationQuery;

		private EntityQuery m_BuildingsQuery;

		private EntityQuery m_OwnedTileQuery;

		private EntityQuery m_FollowedQuery;

		private readonly PrefabSystem m_PrefabSystem;

		private readonly TimeUISystem m_TimeSystem;

		private readonly SimulationSystem m_SimulationSystem;

		private readonly MapMetadataSystem m_MapMetadataSystem;

		private readonly CityStatisticsSystem m_CityStatisticsSystem;

		private readonly CityConfigurationSystem m_CityConfigurationSystem;

		private readonly TutorialSystem m_TutorialSystem;

		private readonly CitySystem m_CitySystem;

		private readonly CityServiceBudgetSystem m_CityServiceBudgetSystem;

		private readonly GameModeSystem m_GameModeSystem;

		public int buildingCount => m_BuildingsQuery.CalculateEntityCount();

		public Population population
		{
			get
			{
				Population result = new Population
				{
					m_Population = 0,
					m_AverageHappiness = 50
				};
				if (!m_PopulationQuery.IsEmpty)
				{
					m_PopulationQuery.CompleteDependency();
					result = m_PopulationQuery.GetSingleton<Population>();
				}
				return result;
			}
		}

		public int moneyAmount => m_CitySystem.moneyAmount;

		public int moneyDelta => m_CityServiceBudgetSystem.GetMoneyDelta();

		public int followedCitizens => m_FollowedQuery.CalculateEntityCount();

		public int ownedMapTiles => m_OwnedTileQuery.CalculateEntityCount();

		public string mapName => m_MapMetadataSystem.mapName;

		public float simulationSpeed
		{
			get
			{
				if (GameManager.instance.gameMode != GameMode.Game)
				{
					return 0f;
				}
				return m_SimulationSystem.selectedSpeed;
			}
		}

		public bool unlimitedMoney => m_CityConfigurationSystem.unlimitedMoney;

		public bool unlockAll => m_CityConfigurationSystem.unlockAll;

		public bool naturalDisasters => m_CityConfigurationSystem.naturalDisasters;

		public bool tutorialEnabled => m_TutorialSystem.tutorialEnabled;

		public string currentGameMode => m_GameModeSystem.currentModeName;

		private void InitializeStats(World world)
		{
			m_PopulationQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<Game.City.City>(), ComponentType.ReadOnly<Population>());
			m_BuildingsQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
			m_OwnedTileQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<MapTile>(), ComponentType.Exclude<Native>());
			m_FollowedQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<Citizen>(), ComponentType.ReadOnly<Followed>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		}

		public int GetResourcesOutputCount()
		{
			BufferLookup<CityStatistic> bufferLookup = m_PrefabSystem.GetBufferLookup<CityStatistic>(isReadOnly: true);
			int num = 0;
			ResourceIterator iterator = ResourceIterator.GetIterator();
			while (iterator.Next())
			{
				num += m_CityStatisticsSystem.GetStatisticValue(bufferLookup, StatisticType.ProcessingCount, EconomyUtils.GetResourceIndex(iterator.resource));
			}
			return num;
		}

		public GameplayData(World world)
		{
			m_PrefabSystem = world.GetExistingSystemManaged<PrefabSystem>();
			m_TimeSystem = world.GetExistingSystemManaged<TimeUISystem>();
			m_SimulationSystem = world.GetExistingSystemManaged<SimulationSystem>();
			m_MapMetadataSystem = world.GetExistingSystemManaged<MapMetadataSystem>();
			m_CityStatisticsSystem = world.GetExistingSystemManaged<CityStatisticsSystem>();
			m_CityConfigurationSystem = world.GetExistingSystemManaged<CityConfigurationSystem>();
			m_TutorialSystem = world.GetExistingSystemManaged<TutorialSystem>();
			m_CitySystem = world.GetExistingSystemManaged<CitySystem>();
			m_CityServiceBudgetSystem = world.GetExistingSystemManaged<CityServiceBudgetSystem>();
			m_GameModeSystem = world.GetExistingSystemManaged<GameModeSystem>();
			InitializeStats(world);
		}

		public IEnumerable<IDlc> GetDLCsFromContent()
		{
			return m_PrefabSystem.GetAvailableContentPrefabs().Select(CreateDummyDLC);
			static IDlc CreateDummyDLC(ContentPrefab contentPrefab)
			{
				if (contentPrefab.TryGet<DlcRequirement>(out var component))
				{
					return new CommonDlc(component.m_Dlc, contentPrefab.name, default(Colossal.Version));
				}
				if (contentPrefab.TryGet<PdxLoginRequirement>(out var _))
				{
					return new CommonDlc(DlcId.Virtual, contentPrefab.name, default(Colossal.Version));
				}
				return new CommonDlc(DlcId.Invalid, contentPrefab.name, default(Colossal.Version));
			}
		}

		public int GetDay()
		{
			return m_TimeSystem.GetDay();
		}

		public T GetPrefab<T>(Entity entity) where T : PrefabBase
		{
			return m_PrefabSystem.GetPrefab<T>(entity);
		}
	}

	private struct Session
	{
		public Guid guid;

		private DateTime m_InputIdleStart;

		private float m_StartSimulationSpeed;

		private DateTime m_TimeStarted;

		private Dictionary<string, DateTime> timeSpentInPanel;

		public bool active { get; private set; }

		public TimeSpan duration => DateTime.UtcNow - m_TimeStarted;

		public TimeSpan idleTime => DateTime.UtcNow - m_InputIdleStart;

		public float startSimulationSpeed => m_StartSimulationSpeed;

		public void Open(Guid guid)
		{
			if (guid == Guid.Empty)
			{
				guid = Guid.NewGuid();
			}
			this.guid = guid;
			timeSpentInPanel = new Dictionary<string, DateTime>();
			active = true;
			m_TimeStarted = DateTime.UtcNow;
			log.DebugFormat("Telemetry session {0} opened", guid);
		}

		public void PanelOpened(string name)
		{
			timeSpentInPanel[name] = DateTime.UtcNow;
		}

		public bool PanelClosed(string name, out TimeSpan timeSpent)
		{
			timeSpent = TimeSpan.MinValue;
			if (timeSpentInPanel.TryGetValue(name, out var value))
			{
				timeSpent = DateTime.UtcNow - value;
			}
			return timeSpentInPanel.Remove(name);
		}

		public void Close()
		{
			log.DebugFormat("Telemetry session {0} closed", guid);
			guid = default(Guid);
			active = false;
		}

		public void ReportInputIdle()
		{
			m_InputIdleStart = DateTime.UtcNow;
			m_StartSimulationSpeed = gameplayData.simulationSpeed;
		}
	}

	private struct OpenSessionPayload
	{
		public Guid playthrough_id;

		public string map_id;

		public Helpers.json_gameplay_mode gameplay_mode;

		public bool tutorial_messages;

		public bool unlock_all;

		public bool unlimited_money;

		public bool disasters;

		public string mode_settings;
	}

	private struct CloseSessionPayload
	{
		public Guid playthrough_id;

		public string map_id;

		public int ingame_days;

		public double time_passed;
	}

	private struct ModUsedPayload
	{
		public struct Mod
		{
			public string mod_name;

			public string mod_id;

			public string[] mod_tags;
		}

		public Mod[] mods;
	}

	private struct DlcPayload
	{
		public struct Dlc
		{
			public string dlc_name;

			public string dlc_platform_id;
		}

		public Dlc[] dlcs;
	}

	private static ILog log = LogManager.GetLogger("SceneFlow");

	private const string kHardwareEvent = "hardware";

	private const string kLanguageEvent = "language";

	private const string kGraphicsSettings = "graphics_settings";

	private const string kAchievementUnlocked = "achievement";

	private const string kTutorialEvent = "tutorial_event";

	private const string kMilestoneUnlocked = "milestone_unlocked";

	private const string kDevNodePurchased = "dev_node";

	private const string kControlInput = "control_input";

	private const string kPanelClosed = "panel_closed";

	private const string kCityStats = "city_stats";

	private const string kChirper = "chirper";

	private const string kBuildingPlaced = "building_placed";

	private const string kPolicy = "policy";

	private const string kInputIdleEnd = "idle_time_end";

	private const string kSessionOpen = "playsession_start";

	private const string kSessionClose = "playsession_close";

	private static Session s_Session = default(Session);

	private const string kModsUsed = "mod_used";

	private const string kDlc = "dlc";

	public static GameplayData gameplayData { get; set; }

	[TelemetryEvent("hardware", typeof(HardwarePayload))]
	private static void Hardware()
	{
		try
		{
			HardwarePayload payload = new HardwarePayload
			{
				os_version = SystemInfo.operatingSystem,
				ram = Mathf.RoundToInt((float)SystemInfo.systemMemorySize / 1024f),
				gfx_memory = Mathf.RoundToInt((float)SystemInfo.graphicsMemorySize / 1024f),
				cpucount = SystemInfo.processorCount,
				cpu_model = SystemInfo.processorType,
				gpu_model = SystemInfo.graphicsDeviceName
			};
			PlatformManager.instance.SendTelemetry("hardware", payload);
		}
		catch (Exception exception)
		{
			log.WarnFormat(exception, "{0} telemetry event payload generation failed", "hardware");
		}
	}

	[TelemetryEvent("language", typeof(LanguagePayload))]
	private static void Language()
	{
		try
		{
			LanguagePayload payload = new LanguagePayload
			{
				os_language = Helpers.GetSystemLanguage(),
				game_language = GameManager.instance.localizationManager.activeLocaleId
			};
			PlatformManager.instance.SendTelemetry("language", payload);
		}
		catch (Exception exception)
		{
			log.WarnFormat(exception, "{0} telemetry event payload generation failed", "language");
		}
	}

	[TelemetryEvent("graphics_settings", typeof(GraphicsSettingsPayload))]
	public static void GraphicsSettings()
	{
		try
		{
			GraphicsSettingsPayload payload = new GraphicsSettingsPayload
			{
				display_mode = SharedSettings.instance.graphics.displayMode.ToTelemetry(),
				resolution = SharedSettings.instance.graphics.resolution.ToTelemetry(),
				graphics_quality = SharedSettings.instance.graphics.GetLevel().ToString().ToLowerInvariant()
			};
			PlatformManager.instance.SendTelemetry("graphics_settings", payload);
		}
		catch (Exception exception)
		{
			log.WarnFormat(exception, "{0} telemetry event payload generation failed", "graphics_settings");
		}
	}

	[TelemetryEvent("achievement", typeof(AchievementPayload))]
	public static void AchievementUnlocked(AchievementId id)
	{
		try
		{
			if (s_Session.active && PlatformManager.instance.GetAchievement(id, out var achievement) && achievement.achieved)
			{
				AchievementPayload payload = new AchievementPayload
				{
					playthrough_id = s_Session.guid,
					achievement_name = achievement.internalName,
					achievement_number = PlatformManager.instance.CountAchievements(onlyAchieved: true)
				};
				PlatformManager.instance.SendTelemetry("achievement", payload);
			}
		}
		catch (Exception exception)
		{
			log.WarnFormat(exception, "{0} telemetry event payload generation failed", "achievement");
		}
	}

	[TelemetryEvent("tutorial_event", typeof(TutorialEventPayload))]
	public static void TutorialEvent(Entity tutorial)
	{
		try
		{
			if (gameplayData != null && s_Session.active)
			{
				PrefabBase prefab = gameplayData.GetPrefab<PrefabBase>(tutorial);
				TutorialEventPayload payload = new TutorialEventPayload
				{
					playthrough_id = s_Session.guid,
					advice_followed = ((prefab != null) ? prefab.name : null)
				};
				PlatformManager.instance.SendTelemetry("tutorial_event", payload);
			}
		}
		catch (Exception exception)
		{
			log.WarnFormat(exception, "{0} telemetry event payload generation failed", "tutorial_event");
		}
	}

	[TelemetryEvent("milestone_unlocked", typeof(MilestoneUnlockedPayload))]
	public static void MilestoneUnlocked(int milestoneIndex)
	{
		try
		{
			if (gameplayData != null && s_Session.active)
			{
				MilestoneUnlockedPayload payload = new MilestoneUnlockedPayload
				{
					playthrough_id = s_Session.guid,
					milestone_index = milestoneIndex,
					ingame_days = gameplayData.GetDay()
				};
				PlatformManager.instance.SendTelemetry("milestone_unlocked", payload);
			}
		}
		catch (Exception exception)
		{
			log.WarnFormat(exception, "{0} telemetry event payload generation failed", "milestone_unlocked");
		}
	}

	[TelemetryEvent("dev_node", typeof(DevNodePurchasedPayload))]
	public static void DevNodePurchased(DevTreeNodePrefab nodePrefab)
	{
		try
		{
			if (s_Session.active)
			{
				DevNodePurchasedPayload payload = new DevNodePurchasedPayload
				{
					playthrough_id = s_Session.guid,
					dev_node_name = nodePrefab.name,
					node_type = nodePrefab.m_Service.name,
					tier_id = nodePrefab.m_HorizontalPosition
				};
				PlatformManager.instance.SendTelemetry("dev_node", payload);
			}
		}
		catch (Exception exception)
		{
			log.WarnFormat(exception, "{0} telemetry event payload generation failed", "dev_node");
		}
	}

	[TelemetryEvent("control_input", typeof(ControlInputPayload))]
	public static void ControlSchemeChanged(InputManager.ControlScheme controlScheme)
	{
		try
		{
			if (s_Session.active)
			{
				ControlInputPayload payload = new ControlInputPayload
				{
					playthrough_id = s_Session.guid,
					control_scheme = controlScheme.ToString()
				};
				PlatformManager.instance.SendTelemetry("control_input", payload);
			}
		}
		catch (Exception exception)
		{
			log.WarnFormat(exception, "{0} telemetry event payload generation failed", "control_input");
		}
	}

	public static void PanelOpened(GamePanel panel)
	{
		if (s_Session.active)
		{
			s_Session.PanelOpened(panel.GetType().Name);
		}
	}

	[TelemetryEvent("panel_closed", typeof(PanelClosedPayload))]
	public static void PanelClosed(GamePanel panel)
	{
		try
		{
			if (s_Session.active && s_Session.PanelClosed(panel.GetType().Name, out var timeSpent))
			{
				string name = panel.GetType().Name;
				PanelClosedPayload payload = new PanelClosedPayload
				{
					playthrough_id = s_Session.guid,
					panel_name = name,
					time_spent = Math.Round(timeSpent.TotalSeconds, 2)
				};
				PlatformManager.instance.SendTelemetry("panel_closed", payload);
			}
		}
		catch (Exception exception)
		{
			log.WarnFormat(exception, "{0} telemetry event payload generation failed", "panel_closed");
		}
	}

	[TelemetryEvent("city_stats", typeof(CityStatsPayload))]
	public static void CityStats()
	{
		try
		{
			if (gameplayData != null && s_Session.active)
			{
				Population population = gameplayData.population;
				CityStatsPayload payload = new CityStatsPayload
				{
					playthrough_id = s_Session.guid,
					map_id = gameplayData.mapName,
					n_buildings = gameplayData.buildingCount,
					population = population.m_Population,
					happiness = population.m_AverageHappiness,
					ingame_days = gameplayData.GetDay(),
					map_tiles = gameplayData.ownedMapTiles,
					resource_output = gameplayData.GetResourcesOutputCount(),
					cash_balance = gameplayData.moneyAmount,
					cash_income = gameplayData.moneyDelta,
					tagged_citizens = gameplayData.followedCitizens
				};
				PlatformManager.instance.SendTelemetry("city_stats", payload);
			}
		}
		catch (Exception exception)
		{
			log.WarnFormat(exception, "{0} telemetry event payload generation failed", "city_stats");
		}
	}

	[TelemetryEvent("chirper", typeof(ChirperPayload))]
	public static void Chirp(Entity chirpPrefab, uint likes)
	{
		try
		{
			if (gameplayData != null && s_Session.active)
			{
				PrefabBase prefab = gameplayData.GetPrefab<PrefabBase>(chirpPrefab);
				ChirperPayload payload = new ChirperPayload
				{
					playthrough_id = s_Session.guid,
					message_type = prefab.name,
					likes = likes
				};
				PlatformManager.instance.SendTelemetry("chirper", payload);
			}
		}
		catch (Exception exception)
		{
			log.WarnFormat(exception, "{0} telemetry event payload generation failed", "chirper");
		}
	}

	[TelemetryEvent("building_placed", typeof(BuildingPlacedPayload))]
	public static void PlaceBuilding(Entity entity, PrefabBase building, float3 position)
	{
		try
		{
			if (gameplayData != null && s_Session.active && (building.Has<BuildingPrefab>() || building.Has<BuildingExtensionPrefab>()))
			{
				string type = null;
				int building_level = 0;
				if (building.TryGet<UIObject>(out var component) && component.m_Group is UIAssetCategoryPrefab uIAssetCategoryPrefab)
				{
					type = uIAssetCategoryPrefab.name;
				}
				string origin = "base_game";
				if (building.TryGet<ContentPrerequisite>(out var component2))
				{
					origin = component2.m_ContentPrerequisite.name;
				}
				BuildingPlacedPayload payload = new BuildingPlacedPayload
				{
					playthrough_id = s_Session.guid,
					map_id = gameplayData.mapName,
					building_id = building.name,
					type = type,
					building_level = building_level,
					coordinates = $"{position.x}|{position.z}",
					origin = origin
				};
				PlatformManager.instance.SendTelemetry("building_placed", payload);
			}
		}
		catch (Exception exception)
		{
			log.WarnFormat(exception, "{0} telemetry event payload generation failed", "building_placed");
		}
	}

	[TelemetryEvent("policy", typeof(PolicyPayload))]
	public static void Policy(ModifiedSystem.PolicyEventInfo eventInfo)
	{
		try
		{
			if (gameplayData != null && s_Session.active)
			{
				PolicyPrefab prefab = gameplayData.GetPrefab<PolicyPrefab>(eventInfo.m_Entity);
				if (prefab.m_Visibility != PolicyVisibility.HideFromPolicyList)
				{
					PolicyPayload payload = new PolicyPayload
					{
						playthrough_id = s_Session.guid,
						policy_id = prefab.name,
						policy_category = prefab.m_Category,
						policy_range = eventInfo.m_PolicyRange
					};
					PlatformManager.instance.SendTelemetry("policy", payload);
				}
			}
		}
		catch (Exception exception)
		{
			log.WarnFormat(exception, "{0} telemetry event payload generation failed", "policy");
		}
	}

	public static void InputIdleStart()
	{
		try
		{
			if (gameplayData != null && s_Session.active)
			{
				s_Session.ReportInputIdle();
			}
		}
		catch (Exception exception)
		{
			log.Warn(exception);
		}
	}

	[TelemetryEvent("idle_time_end", typeof(InputIdleEndPayload))]
	public static void InputIdleEnd()
	{
		try
		{
			if (gameplayData != null && s_Session.active && !GameManager.instance.isGameLoading)
			{
				InputIdleEndPayload payload = new InputIdleEndPayload
				{
					playthrough_id = s_Session.guid,
					simulation_speed_start = s_Session.startSimulationSpeed,
					simulation_speed_end = gameplayData.simulationSpeed,
					duration = Math.Round(s_Session.idleTime.TotalSeconds, 2)
				};
				PlatformManager.instance.SendTelemetry("idle_time_end", payload);
			}
		}
		catch (Exception exception)
		{
			log.WarnFormat(exception, "{0} telemetry event payload generation failed", "idle_time_end");
		}
	}

	public static void FireSessionStartEvents()
	{
		Hardware();
		Language();
		GraphicsSettings();
	}

	public static Guid GetCurrentSession()
	{
		return s_Session.guid;
	}

	[TelemetryEvent("playsession_start", typeof(OpenSessionPayload))]
	public static void OpenSession(Guid guid)
	{
		try
		{
			CloseSession();
			if (gameplayData != null && !s_Session.active)
			{
				s_Session.Open(guid);
				OpenSessionPayload payload = new OpenSessionPayload
				{
					playthrough_id = s_Session.guid,
					map_id = gameplayData.mapName,
					gameplay_mode = GameManager.instance.gameMode.ToTelemetry(),
					tutorial_messages = gameplayData.tutorialEnabled,
					unlimited_money = gameplayData.unlimitedMoney,
					unlock_all = gameplayData.unlockAll,
					disasters = gameplayData.naturalDisasters,
					mode_settings = gameplayData.currentGameMode
				};
				PlatformManager.instance.SendTelemetry("playsession_start", payload);
				ModsUsed();
				DlcsInstalled(gameplayData);
			}
		}
		catch (Exception exception)
		{
			log.WarnFormat(exception, "{0} telemetry event payload generation failed", "playsession_start");
		}
	}

	[TelemetryEvent("mod_used", typeof(ModUsedPayload))]
	private static void ModsUsed()
	{
		try
		{
			if (AssetDatabase<ParadoxMods>.instance?.dataSource is ParadoxModsDataSource paradoxModsDataSource)
			{
				ModUsedPayload payload = new ModUsedPayload
				{
					mods = (from mod in paradoxModsDataSource.GetActiveMods()
						select new ModUsedPayload.Mod
						{
							mod_name = mod.displayName,
							mod_id = mod.id.ToString(),
							mod_tags = mod.tags
						}).ToArray()
				};
				if (payload.mods.Any())
				{
					PlatformManager.instance.SendTelemetry("mod_used", payload);
				}
			}
		}
		catch (Exception exception)
		{
			log.WarnFormat(exception, "{0} telemetry event payload generation failed", "mod_used");
		}
	}

	[TelemetryEvent("dlc", typeof(DlcPayload))]
	private static void DlcsInstalled(GameplayData data)
	{
		try
		{
			DlcPayload payload = new DlcPayload
			{
				dlcs = (from dlc in PlatformManager.instance.EnumerateDLCs()
					where dlc.hasStoreBackend
					select new DlcPayload.Dlc
					{
						dlc_name = dlc.internalName,
						dlc_platform_id = dlc.backendId
					}).ToArray()
			};
			if (payload.dlcs.Any())
			{
				PlatformManager.instance.SendTelemetry("dlc", payload);
			}
		}
		catch (Exception exception)
		{
			log.WarnFormat(exception, "{0} telemetry event payload generation failed", "dlc");
		}
	}

	[TelemetryEvent("playsession_close", typeof(CloseSessionPayload))]
	public static void CloseSession()
	{
		try
		{
			if (gameplayData != null && s_Session.active)
			{
				CloseSessionPayload payload = new CloseSessionPayload
				{
					playthrough_id = s_Session.guid,
					map_id = gameplayData.mapName,
					ingame_days = gameplayData.GetDay(),
					time_passed = Math.Round(s_Session.duration.TotalHours, 2)
				};
				PlatformManager.instance.SendTelemetry("playsession_close", payload);
				s_Session.Close();
			}
		}
		catch (Exception exception)
		{
			log.WarnFormat(exception, "{0} telemetry event payload generation failed", "playsession_close");
		}
	}
}

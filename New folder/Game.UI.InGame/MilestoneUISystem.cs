using System;
using System.Runtime.CompilerServices;
using Colossal;
using Colossal.Entities;
using Colossal.Logging;
using Colossal.PSI.Common;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Achievements;
using Game.City;
using Game.Common;
using Game.Prefabs;
using Game.PSI;
using Game.SceneFlow;
using Game.Settings;
using Game.Simulation;
using Game.Tools;
using Game.Tutorials;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class MilestoneUISystem : UISystemBase, IXPMessageHandler, IDefaultSerializable, ISerializable
{
	private struct ComparableMilestone : IComparable<ComparableMilestone>
	{
		public Entity m_Entity;

		public MilestoneData m_Data;

		public int CompareTo(ComparableMilestone other)
		{
			return m_Data.m_Index.CompareTo(other.m_Data.m_Index);
		}
	}

	private struct ServiceInfo : IComparable<ServiceInfo>
	{
		public Entity m_Entity;

		public PrefabData m_PrefabData;

		public int m_UIPriority;

		public bool m_DevTreeUnlocked;

		public int CompareTo(ServiceInfo other)
		{
			return m_UIPriority.CompareTo(other.m_UIPriority);
		}
	}

	private struct AssetInfo : IComparable<AssetInfo>
	{
		public Entity m_Entity;

		public PrefabData m_PrefabData;

		public int m_UIPriority1;

		public int m_UIPriority2;

		public int CompareTo(AssetInfo other)
		{
			int num = m_UIPriority1.CompareTo(other.m_UIPriority1);
			if (num != 0)
			{
				return num;
			}
			return m_UIPriority2.CompareTo(other.m_UIPriority2);
		}
	}

	private const string kGroup = "milestone";

	private PrefabSystem m_PrefabSystem;

	private IXPSystem m_XPSystem;

	private CitySystem m_CitySystem;

	private IMilestoneSystem m_XpMilestoneSystem;

	private ImageSystem m_ImageSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private TutorialSystem m_TutorialSystem;

	private EntityQuery m_MilestoneLevelQuery;

	private EntityQuery m_MilestoneQuery;

	private EntityQuery m_LockedMilestoneQuery;

	private EntityQuery m_ModifiedMilestoneQuery;

	private EntityQuery m_MilestoneReachedEventQuery;

	private EntityQuery m_UnlockableAssetQuery;

	private EntityQuery m_UnlockableZoneQuery;

	private EntityQuery m_DevTreeNodeQuery;

	private EntityQuery m_UnlockableFeatureQuery;

	private EntityQuery m_UnlockablePolicyQuery;

	private EntityArchetype m_UnlockEventArchetype;

	private EntityQuery m_PopulationVictoryConfigQuery;

	private GetterValueBinding<int> m_AchievedMilestoneBinding;

	private GetterValueBinding<bool> m_MaxMilestoneReachedBinding;

	private GetterValueBinding<int> m_AchievedMilestoneXPBinding;

	private GetterValueBinding<int> m_NextMilestoneXPBinding;

	private GetterValueBinding<int> m_TotalXPBinding;

	private RawEventBinding m_XpMessageAddedBinding;

	private RawValueBinding m_MilestonesBinding;

	private ValueBinding<Entity> m_UnlockedMilestoneBinding;

	private RawMapBinding<Entity> m_MilestoneDetailsBinding;

	private RawMapBinding<Entity> m_MilestoneUnlocksBinding;

	private RawMapBinding<Entity> m_UnlockDetailsBinding;

	private GetterValueBinding<bool> m_UnlockAllBinding;

	private GetterValueBinding<bool> m_reachedPopulationGoalBinding;

	private GetterValueBinding<bool> m_victoryPopupShownBinding;

	private bool m_reachedPopulationGoal;

	private bool m_setVictoryPopupShown;

	private bool m_victoryPopupShown;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_XPSystem = base.World.GetOrCreateSystemManaged<XPSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_XpMilestoneSystem = base.World.GetOrCreateSystemManaged<MilestoneSystem>();
		m_ImageSystem = base.World.GetOrCreateSystemManaged<ImageSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_TutorialSystem = base.World.GetOrCreateSystemManaged<TutorialSystem>();
		m_MilestoneLevelQuery = GetEntityQuery(ComponentType.ReadOnly<MilestoneLevel>());
		m_MilestoneQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<PrefabData>(),
				ComponentType.ReadOnly<MilestoneData>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_LockedMilestoneQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<PrefabData>(),
				ComponentType.ReadOnly<MilestoneData>(),
				ComponentType.ReadOnly<Locked>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_ModifiedMilestoneQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<PrefabData>(),
				ComponentType.ReadOnly<MilestoneData>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Updated>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_MilestoneReachedEventQuery = GetEntityQuery(ComponentType.ReadOnly<MilestoneReachedEvent>());
		m_UnlockableAssetQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[4]
			{
				ComponentType.ReadOnly<PrefabData>(),
				ComponentType.ReadOnly<UIObjectData>(),
				ComponentType.ReadOnly<ServiceObjectData>(),
				ComponentType.ReadOnly<UnlockRequirement>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_UnlockableZoneQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[4]
			{
				ComponentType.ReadOnly<PrefabData>(),
				ComponentType.ReadOnly<UIObjectData>(),
				ComponentType.ReadOnly<ZoneData>(),
				ComponentType.ReadOnly<UnlockRequirement>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[5]
			{
				ComponentType.ReadOnly<PrefabData>(),
				ComponentType.ReadOnly<UIObjectData>(),
				ComponentType.ReadOnly<PlaceholderBuildingData>(),
				ComponentType.ReadOnly<PlaceableObjectData>(),
				ComponentType.ReadOnly<UnlockRequirement>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_DevTreeNodeQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<PrefabData>(),
				ComponentType.ReadOnly<DevTreeNodeData>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_UnlockableFeatureQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[4]
			{
				ComponentType.ReadOnly<PrefabData>(),
				ComponentType.ReadOnly<FeatureData>(),
				ComponentType.ReadOnly<UIObjectData>(),
				ComponentType.ReadOnly<UnlockRequirement>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_UnlockablePolicyQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabData>(), ComponentType.ReadOnly<PolicyData>(), ComponentType.ReadOnly<UIObjectData>(), ComponentType.ReadOnly<UnlockRequirement>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_UnlockEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<Unlock>());
		m_PopulationVictoryConfigQuery = GetEntityQuery(ComponentType.ReadOnly<PopulationVictoryConfigurationData>());
		AddBinding(m_AchievedMilestoneBinding = new GetterValueBinding<int>("milestone", "achievedMilestone", GetAchievedMilestone));
		AddBinding(m_MaxMilestoneReachedBinding = new GetterValueBinding<bool>("milestone", "maxMilestoneReached", IsMaxMilestoneReached));
		AddBinding(m_AchievedMilestoneXPBinding = new GetterValueBinding<int>("milestone", "achievedMilestoneXP", GetAchievedMilestoneXP));
		AddBinding(m_NextMilestoneXPBinding = new GetterValueBinding<int>("milestone", "nextMilestoneXP", GetNextMilestoneXP));
		AddBinding(m_TotalXPBinding = new GetterValueBinding<int>("milestone", "totalXP", GetTotalXP));
		AddBinding(m_XpMessageAddedBinding = new RawEventBinding("milestone", "xpMessageAdded"));
		AddBinding(m_MilestonesBinding = new RawValueBinding("milestone", "milestones", BindMilestones));
		AddBinding(m_UnlockedMilestoneBinding = new ValueBinding<Entity>("milestone", "unlockedMilestone", Entity.Null));
		AddBinding(new TriggerBinding("milestone", "clearUnlockedMilestone", ClearUnlockedMilestone));
		AddBinding(m_MilestoneDetailsBinding = new RawMapBinding<Entity>("milestone", "milestoneDetails", BindMilestoneDetails));
		AddBinding(m_MilestoneUnlocksBinding = new RawMapBinding<Entity>("milestone", "milestoneUnlocks", BindMilestoneUnlocks));
		AddBinding(m_UnlockDetailsBinding = new RawMapBinding<Entity>("milestone", "unlockDetails", BindUnlockDetails));
		AddBinding(m_UnlockAllBinding = new GetterValueBinding<bool>("milestone", "unlockAll", () => m_CityConfigurationSystem.unlockAll));
		AddBinding(m_reachedPopulationGoalBinding = new GetterValueBinding<bool>("milestone", "reachedPopulationGoal", () => m_reachedPopulationGoal));
		AddBinding(m_victoryPopupShownBinding = new GetterValueBinding<bool>("milestone", "victoryPopupShown", () => m_victoryPopupShown));
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		m_UnlockedMilestoneBinding.Update(Entity.Null);
		m_reachedPopulationGoalBinding.Update();
		m_victoryPopupShownBinding.Update();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_AchievedMilestoneBinding.Update();
		m_MaxMilestoneReachedBinding.Update();
		m_TotalXPBinding.Update();
		m_AchievedMilestoneXPBinding.Update();
		m_NextMilestoneXPBinding.Update();
		if (!m_MilestoneReachedEventQuery.IsEmptyIgnoreFilter)
		{
			PublishReachedMilestones();
		}
		if (!m_MilestoneReachedEventQuery.IsEmptyIgnoreFilter || !m_ModifiedMilestoneQuery.IsEmptyIgnoreFilter)
		{
			m_MilestonesBinding.Update();
			m_MilestoneDetailsBinding.Update();
		}
		m_UnlockAllBinding.Update();
		if (Platform.Consoles.IsPlatformSet(Application.platform))
		{
			CheckPopulationVictory();
		}
		m_XPSystem.TransferMessages(this);
	}

	private void CheckPopulationVictory()
	{
		if (GameManager.instance.state != GameManager.State.WorldReady || !GameManager.instance.gameMode.IsGame() || m_victoryPopupShown || m_setVictoryPopupShown)
		{
			return;
		}
		CitySystem existingSystemManaged = base.World.GetExistingSystemManaged<CitySystem>();
		Population componentData = base.EntityManager.GetComponentData<Population>(existingSystemManaged.City);
		int populationGoal = base.EntityManager.GetComponentData<PopulationVictoryConfigurationData>(m_PopulationVictoryConfigQuery.GetSingletonEntity()).m_populationGoal;
		if (componentData.m_Population >= populationGoal)
		{
			Entity victoryMilestone = GetVictoryMilestone();
			if (victoryMilestone != Entity.Null)
			{
				m_reachedPopulationGoal = true;
				m_reachedPopulationGoalBinding.Update();
				m_UnlockedMilestoneBinding.Update(victoryMilestone);
				m_setVictoryPopupShown = true;
			}
			else
			{
				LogManager.GetLogger("Platforms").Error("Could not find victory milestone.");
			}
		}
	}

	public void AddMessage(XPMessage message)
	{
		if (m_XpMessageAddedBinding.active)
		{
			IJsonWriter jsonWriter = m_XpMessageAddedBinding.EventBegin();
			jsonWriter.TypeBegin("milestone.XPMessage");
			jsonWriter.PropertyName("amount");
			jsonWriter.Write(message.amount);
			jsonWriter.PropertyName("reason");
			jsonWriter.Write(Enum.GetName(typeof(XPReason), message.reason));
			jsonWriter.TypeEnd();
			m_XpMessageAddedBinding.EventEnd();
		}
	}

	private int GetAchievedMilestone()
	{
		if (m_MilestoneLevelQuery.IsEmptyIgnoreFilter)
		{
			return 0;
		}
		return m_MilestoneLevelQuery.GetSingleton<MilestoneLevel>().m_AchievedMilestone;
	}

	private bool IsMaxMilestoneReached()
	{
		return m_LockedMilestoneQuery.IsEmpty;
	}

	private int GetAchievedMilestoneXP()
	{
		return m_XpMilestoneSystem.lastRequiredXP;
	}

	private int GetNextMilestoneXP()
	{
		return m_XpMilestoneSystem.nextRequiredXP;
	}

	private int GetTotalXP()
	{
		return m_CitySystem.XP;
	}

	private void PublishReachedMilestones()
	{
		Entity entity = m_UnlockedMilestoneBinding.value;
		int num = GetMilestoneIndex(m_UnlockedMilestoneBinding.value);
		NativeArray<MilestoneReachedEvent> nativeArray = m_MilestoneReachedEventQuery.ToComponentDataArray<MilestoneReachedEvent>(Allocator.TempJob);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			if (nativeArray[i].m_Index > num)
			{
				entity = nativeArray[i].m_Milestone;
				num = nativeArray[i].m_Index;
			}
		}
		nativeArray.Dispose();
		Telemetry.MilestoneUnlocked(num);
		PlatformManager.instance.IndicateAchievementProgress(Game.Achievements.Achievements.TheLastMileMarker, num);
		if (SharedSettings.instance.userInterface.blockingPopupsEnabled && !m_CityConfigurationSystem.unlockAll)
		{
			m_UnlockedMilestoneBinding.Update(entity);
			if (Platform.Consoles.IsPlatformSet(Application.platform) && GetVictoryMilestone() == entity)
			{
				m_setVictoryPopupShown = true;
			}
		}
	}

	private int GetMilestoneIndex(Entity milestoneEntity)
	{
		if (!(milestoneEntity != Entity.Null) || !base.EntityManager.TryGetComponent<MilestoneData>(milestoneEntity, out var component))
		{
			return -1;
		}
		return component.m_Index;
	}

	private void BindMilestones(IJsonWriter writer)
	{
		NativeArray<ComparableMilestone> sortedMilestones = GetSortedMilestones(Allocator.TempJob);
		writer.ArrayBegin(sortedMilestones.Length);
		for (int i = 0; i < sortedMilestones.Length; i++)
		{
			Entity entity = sortedMilestones[i].m_Entity;
			MilestoneData milestoneData = sortedMilestones[i].m_Data;
			writer.TypeBegin("milestone.Milestone");
			writer.PropertyName("entity");
			writer.Write(entity);
			writer.PropertyName("index");
			writer.Write(milestoneData.m_Index);
			writer.PropertyName("major");
			writer.Write(milestoneData.m_Major);
			writer.PropertyName("locked");
			writer.Write(base.EntityManager.HasEnabledComponent<Locked>(entity));
			bool value = Platform.Consoles.IsPlatformSet(Application.platform) && milestoneData.m_IsVictory;
			writer.PropertyName("isVictory");
			writer.Write(value);
			writer.TypeEnd();
		}
		writer.ArrayEnd();
		sortedMilestones.Dispose();
	}

	private Entity GetVictoryMilestone()
	{
		Entity result = Entity.Null;
		NativeArray<Entity> nativeArray = m_MilestoneQuery.ToEntityArray(Allocator.TempJob);
		NativeArray<MilestoneData> nativeArray2 = m_MilestoneQuery.ToComponentDataArray<MilestoneData>(Allocator.TempJob);
		for (int i = 0; i < nativeArray2.Length; i++)
		{
			if (nativeArray2[i].m_IsVictory)
			{
				result = nativeArray[i];
				break;
			}
		}
		nativeArray.Dispose();
		nativeArray2.Dispose();
		return result;
	}

	private NativeArray<ComparableMilestone> GetSortedMilestones(Allocator allocator)
	{
		NativeArray<Entity> nativeArray = m_MilestoneQuery.ToEntityArray(Allocator.TempJob);
		NativeArray<MilestoneData> nativeArray2 = m_MilestoneQuery.ToComponentDataArray<MilestoneData>(Allocator.TempJob);
		NativeArray<ComparableMilestone> nativeArray3 = new NativeArray<ComparableMilestone>(nativeArray2.Length, allocator);
		for (int i = 0; i < nativeArray2.Length; i++)
		{
			nativeArray3[i] = new ComparableMilestone
			{
				m_Entity = nativeArray[i],
				m_Data = nativeArray2[i]
			};
		}
		nativeArray3.Sort();
		nativeArray.Dispose();
		nativeArray2.Dispose();
		return nativeArray3;
	}

	private void ClearUnlockedMilestone()
	{
		if (m_setVictoryPopupShown)
		{
			m_victoryPopupShown = true;
		}
		m_reachedPopulationGoal = false;
		m_UnlockedMilestoneBinding.Update(Entity.Null);
		m_reachedPopulationGoalBinding.Update();
		m_victoryPopupShownBinding.Update();
	}

	private void BindMilestoneDetails(IJsonWriter writer, Entity milestone)
	{
		if (milestone != Entity.Null && base.EntityManager.TryGetComponent<MilestoneData>(milestone, out var component))
		{
			bool value = base.EntityManager.HasEnabledComponent<Locked>(milestone);
			writer.TypeBegin("milestone.MilestoneDetails");
			writer.PropertyName("entity");
			writer.Write(milestone);
			writer.PropertyName("index");
			writer.Write(component.m_Index);
			writer.PropertyName("xpRequirement");
			writer.Write(component.m_XpRequried);
			writer.PropertyName("reward");
			writer.Write(component.m_Reward);
			writer.PropertyName("devTreePoints");
			writer.Write(component.m_DevTreePoints);
			writer.PropertyName("mapTiles");
			writer.Write((!m_CityConfigurationSystem.unlockMapTiles) ? component.m_MapTiles : 0);
			writer.PropertyName("loanLimit");
			writer.Write(component.m_LoanLimit);
			bool value2 = Platform.Consoles.IsPlatformSet(Application.platform) && component.m_IsVictory;
			writer.PropertyName("isVictory");
			writer.Write(value2);
			MilestonePrefab prefab = m_PrefabSystem.GetPrefab<MilestonePrefab>(milestone);
			writer.PropertyName("image");
			writer.Write(prefab.m_Image);
			writer.PropertyName("backgroundColor");
			writer.Write(prefab.m_BackgroundColor);
			writer.PropertyName("accentColor");
			writer.Write(prefab.m_AccentColor);
			writer.PropertyName("textColor");
			writer.Write(prefab.m_TextColor);
			writer.PropertyName("locked");
			writer.Write(value);
			writer.TypeEnd();
		}
		else
		{
			writer.WriteNull();
		}
	}

	private void BindMilestoneUnlocks(IJsonWriter writer, Entity milestoneEntity)
	{
		NativeList<Entity> unlockedDevTreeServices = GetUnlockedDevTreeServices(milestoneEntity, Allocator.TempJob);
		NativeList<Entity> unlockedZones = GetUnlockedZones(milestoneEntity, Allocator.TempJob);
		NativeList<Entity> unlockedAssets = GetUnlockedAssets(milestoneEntity, Allocator.TempJob);
		NativeList<UIObjectInfo> sortedUnlockedFeatures = GetSortedUnlockedFeatures(milestoneEntity, Allocator.TempJob);
		NativeList<ServiceInfo> sortedServices = GetSortedServices(unlockedDevTreeServices, unlockedAssets, Allocator.TempJob);
		NativeList<AssetInfo> sortedZones = GetSortedZones(unlockedZones, Allocator.TempJob);
		NativeList<UIObjectInfo> sortedPolicies = GetSortedPolicies(milestoneEntity, Allocator.TempJob);
		NativeList<AssetInfo> result = new NativeList<AssetInfo>(20, Allocator.TempJob);
		NativeList<Entity> assetThemes = new NativeList<Entity>(10, Allocator.TempJob);
		writer.ArrayBegin(sortedUnlockedFeatures.Length + sortedZones.Length + sortedServices.Length + sortedPolicies.Length);
		for (int i = 0; i < sortedUnlockedFeatures.Length; i++)
		{
			UIObjectInfo uIObjectInfo = sortedUnlockedFeatures[i];
			FeaturePrefab prefab = m_PrefabSystem.GetPrefab<FeaturePrefab>(uIObjectInfo.prefabData);
			UIObject component = prefab.GetComponent<UIObject>();
			writer.TypeBegin("milestone.Feature");
			writer.PropertyName("entity");
			writer.Write(uIObjectInfo.entity);
			writer.PropertyName("name");
			writer.Write(prefab.name);
			writer.PropertyName("icon");
			writer.Write(component.m_Icon);
			writer.TypeEnd();
		}
		for (int j = 0; j < sortedZones.Length; j++)
		{
			AssetInfo asset = sortedZones[j];
			PrefabBase prefab2 = m_PrefabSystem.GetPrefab<PrefabBase>(asset.m_PrefabData);
			BindAsset(writer, asset, prefab2, assetThemes);
		}
		for (int k = 0; k < sortedServices.Length; k++)
		{
			ServiceInfo serviceInfo = sortedServices[k];
			ServicePrefab prefab3 = m_PrefabSystem.GetPrefab<ServicePrefab>(serviceInfo.m_PrefabData);
			UIObject component2 = prefab3.GetComponent<UIObject>();
			FilterAndSortAssets(result, serviceInfo.m_Entity, unlockedAssets);
			writer.TypeBegin("milestone.Service");
			writer.PropertyName("entity");
			writer.Write(serviceInfo.m_Entity);
			writer.PropertyName("name");
			writer.Write(prefab3.name);
			writer.PropertyName("icon");
			writer.Write(component2.m_Icon);
			writer.PropertyName("devTreeUnlocked");
			writer.Write(serviceInfo.m_DevTreeUnlocked);
			writer.PropertyName("assets");
			writer.ArrayBegin(result.Length);
			for (int l = 0; l < result.Length; l++)
			{
				AssetInfo asset2 = result[l];
				PrefabBase prefab4 = m_PrefabSystem.GetPrefab<PrefabBase>(asset2.m_PrefabData);
				BindAsset(writer, asset2, prefab4, assetThemes);
			}
			writer.ArrayEnd();
			writer.TypeEnd();
		}
		for (int m = 0; m < sortedPolicies.Length; m++)
		{
			UIObjectInfo uIObjectInfo2 = sortedPolicies[m];
			PolicyPrefab prefab5 = m_PrefabSystem.GetPrefab<PolicyPrefab>(uIObjectInfo2.prefabData);
			UIObject component3 = prefab5.GetComponent<UIObject>();
			writer.TypeBegin("milestone.Policy");
			writer.PropertyName("entity");
			writer.Write(uIObjectInfo2.entity);
			writer.PropertyName("name");
			writer.Write(prefab5.name);
			writer.PropertyName("icon");
			writer.Write(component3.m_Icon);
			writer.TypeEnd();
		}
		writer.ArrayEnd();
		unlockedDevTreeServices.Dispose();
		unlockedAssets.Dispose();
		unlockedZones.Dispose();
		sortedUnlockedFeatures.Dispose();
		sortedServices.Dispose();
		result.Dispose();
		sortedZones.Dispose();
		sortedPolicies.Dispose();
		assetThemes.Dispose();
	}

	private NativeList<Entity> GetUnlockedZones(Entity milestoneEntity, Allocator allocator)
	{
		NativeArray<Entity> prefabs = m_UnlockableZoneQuery.ToEntityArray(Allocator.TempJob);
		NativeList<Entity> result = FilterUnlockedPrefabs(prefabs, milestoneEntity, allocator);
		prefabs.Dispose();
		return result;
	}

	private NativeList<AssetInfo> GetSortedZones(NativeList<Entity> unlockedZones, Allocator allocator)
	{
		NativeList<AssetInfo> nativeList = new NativeList<AssetInfo>(10, allocator);
		for (int i = 0; i < unlockedZones.Length; i++)
		{
			Entity entity = unlockedZones[i];
			PrefabData componentData = base.EntityManager.GetComponentData<PrefabData>(entity);
			UIObjectData componentData2 = base.EntityManager.GetComponentData<UIObjectData>(entity);
			int uIPriority = int.MinValue;
			if (componentData2.m_Group != Entity.Null && base.EntityManager.TryGetComponent<UIObjectData>(componentData2.m_Group, out var component))
			{
				uIPriority = component.m_Priority;
			}
			nativeList.Add(new AssetInfo
			{
				m_Entity = entity,
				m_PrefabData = componentData,
				m_UIPriority1 = uIPriority,
				m_UIPriority2 = componentData2.m_Priority
			});
		}
		nativeList.Sort();
		return nativeList;
	}

	private void BindAsset(IJsonWriter writer, AssetInfo asset, PrefabBase assetPrefab, NativeList<Entity> assetThemes)
	{
		writer.TypeBegin("milestone.Asset");
		writer.PropertyName("entity");
		writer.Write(asset.m_Entity);
		writer.PropertyName("name");
		writer.Write(assetPrefab.name);
		writer.PropertyName("icon");
		writer.Write(ImageSystem.GetThumbnail(assetPrefab) ?? m_ImageSystem.placeholderIcon);
		writer.PropertyName("themes");
		GetThemes(assetThemes, asset.m_Entity);
		writer.ArrayBegin(assetThemes.Length);
		for (int i = 0; i < assetThemes.Length; i++)
		{
			writer.Write(assetThemes[i]);
		}
		writer.ArrayEnd();
		writer.TypeEnd();
	}

	private NativeList<Entity> GetUnlockedDevTreeServices(Entity milestoneEntity, Allocator allocator)
	{
		NativeArray<DevTreeNodeData> nativeArray = m_DevTreeNodeQuery.ToComponentDataArray<DevTreeNodeData>(Allocator.TempJob);
		NativeParallelHashSet<Entity> nativeParallelHashSet = new NativeParallelHashSet<Entity>(10, Allocator.TempJob);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			nativeParallelHashSet.Add(nativeArray[i].m_Service);
		}
		NativeArray<Entity> prefabs = nativeParallelHashSet.ToNativeArray(Allocator.TempJob);
		NativeList<Entity> result = FilterUnlockedPrefabs(prefabs, milestoneEntity, allocator);
		nativeArray.Dispose();
		nativeParallelHashSet.Dispose();
		prefabs.Dispose();
		return result;
	}

	private NativeList<Entity> GetUnlockedAssets(Entity milestoneEntity, Allocator allocator)
	{
		NativeArray<Entity> prefabs = m_UnlockableAssetQuery.ToEntityArray(Allocator.TempJob);
		NativeList<Entity> result = FilterUnlockedPrefabs(prefabs, milestoneEntity, allocator);
		prefabs.Dispose();
		return result;
	}

	private NativeList<UIObjectInfo> GetSortedPolicies(Entity milestoneEntity, Allocator allocator)
	{
		NativeArray<Entity> prefabs = m_UnlockablePolicyQuery.ToEntityArray(Allocator.TempJob);
		NativeList<Entity> entities = FilterUnlockedPrefabs(prefabs, milestoneEntity, Allocator.TempJob);
		NativeList<UIObjectInfo> sortedObjects = UIObjectInfo.GetSortedObjects(base.EntityManager, entities, allocator);
		prefabs.Dispose();
		entities.Dispose();
		return sortedObjects;
	}

	private NativeList<Entity> FilterUnlockedPrefabs(NativeArray<Entity> prefabs, Entity milestoneEntity, Allocator allocator)
	{
		NativeParallelHashMap<Entity, UnlockFlags> requiredPrefabs = new NativeParallelHashMap<Entity, UnlockFlags>(10, Allocator.TempJob);
		NativeList<Entity> result = new NativeList<Entity>(20, allocator);
		for (int i = 0; i < prefabs.Length; i++)
		{
			Entity value = prefabs[i];
			requiredPrefabs.Clear();
			ProgressionUtils.CollectSubRequirements(base.EntityManager, value, requiredPrefabs);
			Entity entity = Entity.Null;
			int num = -1;
			foreach (KeyValue<Entity, UnlockFlags> item in requiredPrefabs)
			{
				if ((item.Value & UnlockFlags.RequireAll) != 0)
				{
					if (base.EntityManager.HasComponent<DevTreeNodeData>(item.Key) || base.EntityManager.HasComponent<UnlockRequirementData>(item.Key))
					{
						entity = Entity.Null;
						break;
					}
					if (base.EntityManager.TryGetComponent<MilestoneData>(item.Key, out var component) && component.m_Index > num)
					{
						entity = item.Key;
						num = component.m_Index;
					}
				}
			}
			if (entity == milestoneEntity && entity != Entity.Null)
			{
				result.Add(in value);
			}
		}
		requiredPrefabs.Dispose();
		return result;
	}

	private NativeList<UIObjectInfo> GetSortedUnlockedFeatures(Entity milestoneEntity, Allocator allocator)
	{
		NativeArray<Entity> prefabs = m_UnlockableFeatureQuery.ToEntityArray(Allocator.TempJob);
		NativeList<Entity> entities = FilterUnlockedPrefabs(prefabs, milestoneEntity, Allocator.TempJob);
		NativeList<UIObjectInfo> sortedObjects = UIObjectInfo.GetSortedObjects(base.EntityManager, entities, allocator);
		prefabs.Dispose();
		entities.Dispose();
		if (m_CityConfigurationSystem.unlockMapTiles)
		{
			int num = -1;
			for (int i = 0; i < sortedObjects.Length; i++)
			{
				UIObjectInfo uIObjectInfo = sortedObjects[i];
				if (m_PrefabSystem.GetPrefab<PrefabBase>(uIObjectInfo.prefabData).name == "Map Tiles")
				{
					num = i;
					break;
				}
			}
			if (num != -1)
			{
				sortedObjects.RemoveAt(num);
			}
		}
		return sortedObjects;
	}

	private NativeList<ServiceInfo> GetSortedServices(NativeList<Entity> unlockedDevTreeServices, NativeList<Entity> unlockedAssets, Allocator allocator)
	{
		NativeParallelHashSet<Entity> nativeParallelHashSet = new NativeParallelHashSet<Entity>(10, Allocator.TempJob);
		NativeList<ServiceInfo> nativeList = new NativeList<ServiceInfo>(10, allocator);
		for (int i = 0; i < unlockedDevTreeServices.Length; i++)
		{
			Entity entity = unlockedDevTreeServices[i];
			if (nativeParallelHashSet.Add(entity) && base.EntityManager.TryGetComponent<UIObjectData>(entity, out var component))
			{
				PrefabData componentData = base.EntityManager.GetComponentData<PrefabData>(entity);
				ServiceInfo value = new ServiceInfo
				{
					m_Entity = entity,
					m_PrefabData = componentData,
					m_UIPriority = component.m_Priority,
					m_DevTreeUnlocked = true
				};
				nativeList.Add(in value);
			}
		}
		for (int j = 0; j < unlockedAssets.Length; j++)
		{
			Entity service = base.EntityManager.GetComponentData<ServiceObjectData>(unlockedAssets[j]).m_Service;
			if (nativeParallelHashSet.Add(service) && base.EntityManager.TryGetComponent<UIObjectData>(service, out var component2))
			{
				PrefabData componentData2 = base.EntityManager.GetComponentData<PrefabData>(service);
				ServiceInfo value = new ServiceInfo
				{
					m_Entity = service,
					m_PrefabData = componentData2,
					m_UIPriority = component2.m_Priority,
					m_DevTreeUnlocked = false
				};
				nativeList.Add(in value);
			}
		}
		nativeList.Sort();
		nativeParallelHashSet.Dispose();
		return nativeList;
	}

	private void FilterAndSortAssets(NativeList<AssetInfo> result, Entity serviceEntity, NativeList<Entity> unlockedAssets)
	{
		result.Clear();
		for (int i = 0; i < unlockedAssets.Length; i++)
		{
			Entity entity = unlockedAssets[i];
			if (base.EntityManager.GetComponentData<ServiceObjectData>(entity).m_Service == serviceEntity)
			{
				PrefabData componentData = base.EntityManager.GetComponentData<PrefabData>(entity);
				UIObjectData componentData2 = base.EntityManager.GetComponentData<UIObjectData>(entity);
				int uIPriority = int.MinValue;
				if (componentData2.m_Group != Entity.Null && base.EntityManager.TryGetComponent<UIObjectData>(componentData2.m_Group, out var component))
				{
					uIPriority = component.m_Priority;
				}
				result.Add(new AssetInfo
				{
					m_Entity = entity,
					m_PrefabData = componentData,
					m_UIPriority1 = uIPriority,
					m_UIPriority2 = componentData2.m_Priority
				});
			}
		}
		result.Sort();
	}

	private void GetThemes(NativeList<Entity> result, Entity assetEntity)
	{
		result.Clear();
		if (!base.EntityManager.TryGetBuffer(assetEntity, isReadOnly: true, out DynamicBuffer<ObjectRequirementElement> buffer))
		{
			return;
		}
		foreach (ObjectRequirementElement item in buffer)
		{
			ObjectRequirementElement current = item;
			if (base.EntityManager.HasComponent<ThemeData>(current.m_Requirement))
			{
				result.Add(in current.m_Requirement);
			}
		}
	}

	private void BindUnlockDetails(IJsonWriter writer, Entity unlockEntity)
	{
		if (unlockEntity != Entity.Null && base.EntityManager.HasComponent<PrefabData>(unlockEntity) && base.EntityManager.HasComponent<UIObjectData>(unlockEntity))
		{
			bool locked = base.EntityManager.HasEnabledComponent<Locked>(unlockEntity);
			if (base.EntityManager.HasComponent<FeatureData>(unlockEntity))
			{
				BindFeatureUnlock(writer, unlockEntity, locked);
			}
			else if (base.EntityManager.HasComponent<ServiceData>(unlockEntity))
			{
				BindServiceUnlock(writer, unlockEntity, locked);
			}
			else if (base.EntityManager.HasComponent<ServiceObjectData>(unlockEntity) || base.EntityManager.HasComponent<PlaceableObjectData>(unlockEntity) || base.EntityManager.HasComponent<ZoneData>(unlockEntity))
			{
				BindAssetUnlock(writer, unlockEntity, locked);
			}
			else if (base.EntityManager.HasComponent<PolicyData>(unlockEntity))
			{
				BindPolicyUnlock(writer, unlockEntity, locked);
			}
			else
			{
				writer.WriteNull();
			}
		}
		else
		{
			writer.WriteNull();
		}
	}

	private void BindFeatureUnlock(IJsonWriter writer, Entity featureEntity, bool locked)
	{
		FeaturePrefab prefab = m_PrefabSystem.GetPrefab<FeaturePrefab>(featureEntity);
		UIObject component = prefab.GetComponent<UIObject>();
		writer.TypeBegin("milestone.UnlockDetails");
		writer.PropertyName("entity");
		writer.Write(featureEntity);
		writer.PropertyName("icon");
		writer.Write(component.m_Icon);
		writer.PropertyName("titleId");
		writer.Write("Assets.NAME[" + prefab.name + "]");
		writer.PropertyName("descriptionId");
		writer.Write("Assets.DESCRIPTION[" + prefab.name + "]");
		writer.PropertyName("locked");
		writer.Write(locked);
		writer.PropertyName("hasDevTree");
		writer.Write(value: false);
		writer.TypeEnd();
	}

	private void BindServiceUnlock(IJsonWriter writer, Entity serviceEntity, bool locked)
	{
		ServicePrefab prefab = m_PrefabSystem.GetPrefab<ServicePrefab>(serviceEntity);
		UIObject component = prefab.GetComponent<UIObject>();
		writer.TypeBegin("milestone.UnlockDetails");
		writer.PropertyName("entity");
		writer.Write(serviceEntity);
		writer.PropertyName("icon");
		writer.Write(component.m_Icon);
		writer.PropertyName("titleId");
		writer.Write("Services.NAME[" + prefab.name + "]");
		writer.PropertyName("descriptionId");
		writer.Write("Services.DESCRIPTION[" + prefab.name + "]");
		writer.PropertyName("locked");
		writer.Write(locked);
		writer.PropertyName("hasDevTree");
		writer.Write(HasDevTree(serviceEntity));
		writer.TypeEnd();
	}

	private bool HasDevTree(Entity serviceEntity)
	{
		using NativeArray<DevTreeNodeData> nativeArray = m_DevTreeNodeQuery.ToComponentDataArray<DevTreeNodeData>(Allocator.TempJob);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			if (nativeArray[i].m_Service == serviceEntity)
			{
				return true;
			}
		}
		return false;
	}

	private void BindAssetUnlock(IJsonWriter writer, Entity assetEntity, bool locked)
	{
		PrefabBase prefab = m_PrefabSystem.GetPrefab<PrefabBase>(assetEntity);
		writer.TypeBegin("milestone.UnlockDetails");
		writer.PropertyName("entity");
		writer.Write(assetEntity);
		writer.PropertyName("icon");
		writer.Write(ImageSystem.GetThumbnail(prefab) ?? m_ImageSystem.placeholderIcon);
		writer.PropertyName("titleId");
		writer.Write("Assets.NAME[" + prefab.name + "]");
		writer.PropertyName("descriptionId");
		writer.Write("Assets.DESCRIPTION[" + prefab.name + "]");
		writer.PropertyName("locked");
		writer.Write(locked);
		writer.PropertyName("hasDevTree");
		writer.Write(value: false);
		writer.TypeEnd();
	}

	private void BindPolicyUnlock(IJsonWriter writer, Entity policyEntity, bool locked)
	{
		PrefabBase prefab = m_PrefabSystem.GetPrefab<PrefabBase>(policyEntity);
		UIObject component = prefab.GetComponent<UIObject>();
		writer.TypeBegin("milestone.UnlockDetails");
		writer.PropertyName("entity");
		writer.Write(policyEntity);
		writer.PropertyName("icon");
		writer.Write(component.m_Icon);
		writer.PropertyName("titleId");
		writer.Write("Policy.TITLE[" + prefab.name + "]");
		writer.PropertyName("descriptionId");
		writer.Write("Policy.DESCRIPTION[" + prefab.name + "]");
		writer.PropertyName("locked");
		writer.Write(locked);
		writer.PropertyName("hasDevTree");
		writer.Write(value: false);
		writer.TypeEnd();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_victoryPopupShown);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_victoryPopupShown);
	}

	public void SetDefaults(Context context)
	{
		m_reachedPopulationGoal = false;
		m_setVictoryPopupShown = false;
		m_victoryPopupShown = false;
	}

	[Preserve]
	public MilestoneUISystem()
	{
	}
}

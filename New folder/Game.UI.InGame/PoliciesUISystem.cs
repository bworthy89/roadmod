using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.PSI.Common;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Achievements;
using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Policies;
using Game.Prefabs;
using Game.Routes;
using Game.SceneFlow;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class PoliciesUISystem : UISystemBase
{
	internal static class BindingNames
	{
		internal const string kCityPolicies = "cityPolicies";

		internal const string kSetPolicy = "setPolicy";

		internal const string kSetCityPolicy = "setCityPolicy";
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PolicySliderData> __Game_Prefabs_PolicySliderData_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PolicySliderData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PolicySliderData>(isReadOnly: true);
		}
	}

	public const string kGroup = "policies";

	public Action EventPolicyUnlocked;

	private CitySystem m_CitySystem;

	private PrefabSystem m_PrefabSystem;

	private PrefabUISystem m_PrefabUISystem;

	private SelectedInfoUISystem m_InfoSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private ImageSystem m_ImageSystem;

	private EntityQuery m_CityPoliciesQuery;

	private EntityQuery m_CityPoliciesUpdatedQuery;

	private EntityQuery m_DistrictPoliciesQuery;

	private EntityQuery m_BuildingPoliciesQuery;

	private EntityQuery m_RoutePoliciesQuery;

	private EntityQuery m_PolicyUnlockedQuery;

	private EntityArchetype m_PolicyEventArchetype;

	private List<UIPolicy> m_CityPolicies;

	private List<UIPolicy> m_SelectedInfoPolicies;

	private GetterValueBinding<List<UIPolicy>> m_CityPoliciesBinding;

	private TypeHandle __TypeHandle;

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		m_CityPolicies.Clear();
		m_SelectedInfoPolicies.Clear();
		m_CityPoliciesBinding.Update();
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_PrefabUISystem = base.World.GetOrCreateSystemManaged<PrefabUISystem>();
		m_InfoSystem = base.World.GetOrCreateSystemManaged<SelectedInfoUISystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_ImageSystem = base.World.GetOrCreateSystemManaged<ImageSystem>();
		m_CityPoliciesQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<PolicyData>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<CityOptionData>(),
				ComponentType.ReadOnly<CityModifierData>()
			}
		});
		m_CityPoliciesUpdatedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<Game.City.City>(),
				ComponentType.ReadOnly<Policy>(),
				ComponentType.ReadOnly<Updated>()
			}
		});
		m_DistrictPoliciesQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<PolicyData>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<DistrictOptionData>(),
				ComponentType.ReadOnly<DistrictModifierData>()
			}
		});
		m_BuildingPoliciesQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<PolicyData>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<BuildingOptionData>(),
				ComponentType.ReadOnly<BuildingModifierData>()
			}
		});
		m_RoutePoliciesQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<PolicyData>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<RouteOptionData>(),
				ComponentType.ReadOnly<RouteModifierData>()
			}
		});
		m_PolicyUnlockedQuery = GetEntityQuery(ComponentType.ReadOnly<Unlock>());
		m_PolicyEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<Modify>());
		m_CityPolicies = new List<UIPolicy>();
		m_SelectedInfoPolicies = new List<UIPolicy>();
		AddBinding(m_CityPoliciesBinding = new GetterValueBinding<List<UIPolicy>>("policies", "cityPolicies", BindCityPolicies, new DelegateWriter<List<UIPolicy>>(WriteCityPolicies)));
		AddBinding(new TriggerBinding<Entity, bool, float>("policies", "setPolicy", SetSelectedInfoPolicy));
		AddBinding(new TriggerBinding<Entity, bool, float>("policies", "setCityPolicy", SetCityPolicy));
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		EventPolicyUnlocked = null;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (PrefabUtils.HasUnlockedPrefab<PolicyData>(base.EntityManager, m_PolicyUnlockedQuery))
		{
			EventPolicyUnlocked?.Invoke();
			m_CityPoliciesBinding.Update();
		}
		if (!m_CityPoliciesUpdatedQuery.IsEmptyIgnoreFilter)
		{
			m_CityPoliciesBinding.Update();
		}
	}

	public void SetPolicy(Entity target, Entity policy, bool active, float adjustment = 0f)
	{
		ModifyPolicy(target, policy, active, adjustment);
	}

	public void SetCityPolicy(Entity policy, bool active, float adjustment)
	{
		ModifyPolicy(m_CitySystem.City, policy, active, adjustment);
		RefreshCityPolicyAchievement(policy, active);
	}

	private void RefreshCityPolicyAchievement(Entity policy, bool active)
	{
		if (!World.DefaultGameObjectInjectionWorld.EntityManager.TryGetBuffer(m_CitySystem.City, isReadOnly: true, out DynamicBuffer<Policy> buffer))
		{
			return;
		}
		bool flag = false;
		int num = 0;
		for (int i = 0; i < buffer.Length; i++)
		{
			if ((buffer[i].m_Flags & PolicyFlags.Active) != 0)
			{
				num++;
				if (buffer[i].m_Policy == policy)
				{
					flag = true;
				}
			}
		}
		if (active && !flag)
		{
			num++;
		}
		if (!active && flag)
		{
			num--;
		}
		PlatformManager.instance.IndicateAchievementProgress(Game.Achievements.Achievements.CallingtheShots, num);
	}

	public void SetSelectedInfoPolicy(Entity policy, bool active, float adjustment = 0f)
	{
		ModifyPolicy(m_InfoSystem.selectedEntity, policy, active, adjustment);
	}

	public void SetSelectedInfoPolicy(Entity target, Entity policy, bool active, float adjustment = 0f)
	{
		ModifyPolicy(target, policy, active, adjustment);
	}

	private void ModifyPolicy(Entity target, Entity policy, bool active, float adjustment)
	{
		EntityCommandBuffer entityCommandBuffer = m_EndFrameBarrier.CreateCommandBuffer();
		Entity e = entityCommandBuffer.CreateEntity(m_PolicyEventArchetype);
		entityCommandBuffer.SetComponent(e, new Modify(target, policy, active, adjustment));
	}

	private void FindAndSortPolicies(Entity entity, EntityQuery policyQuery, List<UIPolicy> list)
	{
		DynamicBuffer<Policy> buffer = base.EntityManager.GetBuffer<Policy>(entity, isReadOnly: true);
		EntityTypeHandle entityTypeHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<PolicySliderData> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PolicySliderData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		NativeArray<ArchetypeChunk> nativeArray = policyQuery.ToArchetypeChunkArray(Allocator.TempJob);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			NativeArray<Entity> nativeArray2 = nativeArray[i].GetNativeArray(entityTypeHandle);
			bool flag = nativeArray[i].Has(ref typeHandle);
			NativeArray<PolicySliderData> nativeArray3 = nativeArray[i].GetNativeArray(ref typeHandle);
			for (int j = 0; j < nativeArray2.Length; j++)
			{
				PolicyPrefab prefab = m_PrefabSystem.GetPrefab<PolicyPrefab>(nativeArray2[j]);
				int priority = 0;
				if (base.EntityManager.TryGetComponent<UIObjectData>(nativeArray2[j], out var component))
				{
					priority = component.m_Priority;
				}
				if (FilterPolicy(nativeArray2[j], prefab, entity))
				{
					UIPolicy item = ExtractInfo(nativeArray2[j], prefab, buffer, flag, flag ? nativeArray3[j] : default(PolicySliderData), priority);
					list.Add(item);
				}
			}
		}
		list.Sort();
		nativeArray.Dispose();
	}

	private bool FilterPolicy(Entity policy, PolicyPrefab prefab, Entity target)
	{
		if (prefab.m_Visibility == PolicyVisibility.HideFromPolicyList)
		{
			return false;
		}
		if (base.EntityManager.HasComponent<District>(target) || base.EntityManager.HasComponent<Game.City.City>(target) || base.EntityManager.HasComponent<Route>(target))
		{
			return true;
		}
		if (!base.EntityManager.HasComponent<Building>(target))
		{
			return false;
		}
		if (!base.EntityManager.TryGetComponent<BuildingOptionData>(policy, out var component))
		{
			return false;
		}
		if (BuildingUtils.HasOption(component, BuildingOption.PaidParking) && base.EntityManager.TryGetComponent<PrefabRef>(target, out var component2) && base.EntityManager.TryGetComponent<BuildingData>(component2.m_Prefab, out var component3) && (component3.m_Flags & (Game.Prefabs.BuildingFlags.RestrictedPedestrian | Game.Prefabs.BuildingFlags.RestrictedCar)) == 0 && HasParkingLanes(target))
		{
			return true;
		}
		if (BuildingUtils.HasOption(component, BuildingOption.Empty) && base.EntityManager.TryGetComponent<PrefabRef>(target, out var component4) && base.EntityManager.TryGetComponent<GarbageFacilityData>(component4.m_Prefab, out var component5) && component5.m_LongTermStorage)
		{
			return true;
		}
		if (!BuildingUtils.HasOption(component, BuildingOption.Inactive))
		{
			return false;
		}
		if (base.EntityManager.HasComponent<CityServiceUpkeep>(target))
		{
			return base.EntityManager.HasComponent<Efficiency>(target);
		}
		return false;
	}

	private UIPolicy ExtractInfo(Entity entity, PolicyPrefab prefab, DynamicBuffer<Policy> activePolicies, bool slider, PolicySliderData sliderData, int priority)
	{
		string name = prefab.name;
		string icon = ImageSystem.GetIcon(prefab) ?? m_ImageSystem.placeholderIcon;
		bool active = false;
		bool flag = base.EntityManager.HasEnabledComponent<Locked>(entity);
		float value = sliderData.m_Default;
		for (int i = 0; i < activePolicies.Length; i++)
		{
			if (activePolicies[i].m_Policy == entity)
			{
				active = (activePolicies[i].m_Flags & PolicyFlags.Active) != 0;
				value = activePolicies[i].m_Adjustment;
				break;
			}
		}
		if (!GameManager.instance.localizationManager.activeDictionary.TryGetValue($"Policy.TITLE[{name}]", out var value2))
		{
			value2 = string.Empty;
		}
		int milestone = (flag ? ProgressionUtils.GetRequiredMilestone(base.EntityManager, entity) : 0);
		return new UIPolicy(data: new UIPolicySlider(value, sliderData), id: name, localizedName: value2, priority: priority, icon: icon, entity: entity, active: active, locked: flag, uiTag: prefab.uiTag, milestone: milestone, slider: slider);
	}

	private bool HasParkingLanes(Entity building)
	{
		if (base.EntityManager.TryGetBuffer(building, isReadOnly: true, out DynamicBuffer<Game.Net.SubLane> buffer) && HasParkingLanes(buffer))
		{
			return true;
		}
		if (base.EntityManager.TryGetBuffer(building, isReadOnly: true, out DynamicBuffer<Game.Net.SubNet> buffer2) && HasParkingLanes(buffer2))
		{
			return true;
		}
		if (base.EntityManager.TryGetBuffer(building, isReadOnly: true, out DynamicBuffer<Game.Objects.SubObject> buffer3) && HasParkingLanes(buffer3))
		{
			return true;
		}
		return false;
	}

	private bool HasParkingLanes(DynamicBuffer<Game.Objects.SubObject> subObjects)
	{
		for (int i = 0; i < subObjects.Length; i++)
		{
			Entity subObject = subObjects[i].m_SubObject;
			if (base.EntityManager.TryGetBuffer(subObject, isReadOnly: true, out DynamicBuffer<Game.Net.SubLane> buffer) && HasParkingLanes(buffer))
			{
				return true;
			}
			if (base.EntityManager.TryGetBuffer(subObject, isReadOnly: true, out DynamicBuffer<Game.Objects.SubObject> buffer2) && HasParkingLanes(buffer2))
			{
				return true;
			}
		}
		return false;
	}

	private bool HasParkingLanes(DynamicBuffer<Game.Net.SubNet> subNets)
	{
		for (int i = 0; i < subNets.Length; i++)
		{
			Entity subNet = subNets[i].m_SubNet;
			if (base.EntityManager.TryGetBuffer(subNet, isReadOnly: true, out DynamicBuffer<Game.Net.SubLane> buffer) && HasParkingLanes(buffer))
			{
				return true;
			}
		}
		return false;
	}

	private bool HasParkingLanes(DynamicBuffer<Game.Net.SubLane> subLanes)
	{
		for (int i = 0; i < subLanes.Length; i++)
		{
			Entity subLane = subLanes[i].m_SubLane;
			Game.Net.ConnectionLane component2;
			if (base.EntityManager.TryGetComponent<Game.Net.ParkingLane>(subLane, out var component))
			{
				if ((component.m_Flags & ParkingLaneFlags.VirtualLane) == 0)
				{
					return true;
				}
			}
			else if (base.EntityManager.TryGetComponent<Game.Net.ConnectionLane>(subLane, out component2) && (component2.m_Flags & ConnectionLaneFlags.Parking) != 0)
			{
				return true;
			}
		}
		return false;
	}

	private List<UIPolicy> BindCityPolicies()
	{
		m_CityPolicies.Clear();
		FindAndSortPolicies(m_CitySystem.City, m_CityPoliciesQuery, m_CityPolicies);
		return m_CityPolicies;
	}

	private void WriteCityPolicies(IJsonWriter writer, List<UIPolicy> policies)
	{
		writer.ArrayBegin(policies.Count);
		foreach (UIPolicy policy in policies)
		{
			policy.Write(m_PrefabUISystem, writer);
		}
		writer.ArrayEnd();
	}

	public bool GatherSelectedInfoPolicies(Entity target)
	{
		m_SelectedInfoPolicies.Clear();
		if (base.EntityManager.HasComponent<Building>(target))
		{
			FindAndSortPolicies(target, m_BuildingPoliciesQuery, m_SelectedInfoPolicies);
		}
		else if (base.EntityManager.HasComponent<District>(target))
		{
			FindAndSortPolicies(target, m_DistrictPoliciesQuery, m_SelectedInfoPolicies);
		}
		else if (base.EntityManager.HasComponent<Route>(target))
		{
			FindAndSortPolicies(target, m_RoutePoliciesQuery, m_SelectedInfoPolicies);
		}
		return m_SelectedInfoPolicies.Count > 0;
	}

	public void BindDistrictPolicies(IJsonWriter binder)
	{
		BindPolicies(binder, m_SelectedInfoPolicies);
	}

	public void BindBuildingPolicies(IJsonWriter binder)
	{
		BindPolicies(binder, m_SelectedInfoPolicies);
	}

	public void BindRoutePolicies(IJsonWriter binder)
	{
		BindPolicies(binder, m_SelectedInfoPolicies);
	}

	private void BindPolicies(IJsonWriter binder, List<UIPolicy> list)
	{
		binder.ArrayBegin(list.Count);
		for (int i = 0; i < list.Count; i++)
		{
			list[i].Write(m_PrefabUISystem, binder);
		}
		binder.ArrayEnd();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		new EntityQueryBuilder(Allocator.Temp).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public PoliciesUISystem()
	{
	}
}

using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Game.Buildings;
using Game.Citizens;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Game.UI.InGame;
using Game.UI.Localization;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.UI.Tooltip;

[CompilerGenerated]
public class ResidentialTooltipSystem : TooltipSystemBase
{
	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Park> __Game_Buildings_Park_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Abandoned> __Game_Buildings_Abandoned_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HomelessHousehold> __Game_Citizens_HomelessHousehold_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TravelPurpose> __Game_Citizens_TravelPurpose_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<HouseholdAnimal> __Game_Citizens_HouseholdAnimal_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Buildings_Park_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.Park>(isReadOnly: true);
			__Game_Buildings_Abandoned_RO_ComponentLookup = state.GetComponentLookup<Abandoned>(isReadOnly: true);
			__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(isReadOnly: true);
			__Game_Citizens_HomelessHousehold_RO_ComponentLookup = state.GetComponentLookup<HomelessHousehold>(isReadOnly: true);
			__Game_Citizens_HealthProblem_RO_ComponentLookup = state.GetComponentLookup<HealthProblem>(isReadOnly: true);
			__Game_Citizens_TravelPurpose_RO_ComponentLookup = state.GetComponentLookup<TravelPurpose>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Citizens_HouseholdAnimal_RO_BufferLookup = state.GetBufferLookup<HouseholdAnimal>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(isReadOnly: true);
		}
	}

	private PrefabSystem m_PrefabSystem;

	private ToolSystem m_ToolSystem;

	private DefaultToolSystem m_DefaultTool;

	private ToolRaycastSystem m_ToolRaycastSystem;

	private IntTooltip m_Residents;

	private IntTooltip m_Level;

	private NativeArray<int> m_Results;

	private NativeValue<Entity> m_ResidenceResult;

	private NativeList<Entity> m_HouseholdsResult;

	private Entity m_SelectedEntity;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_DefaultTool = base.World.GetOrCreateSystemManaged<DefaultToolSystem>();
		m_ToolRaycastSystem = base.World.GetOrCreateSystemManaged<ToolRaycastSystem>();
		m_Residents = new IntTooltip
		{
			path = "residents",
			icon = "Media/Game/Icons/Citizen.svg",
			label = LocalizedString.Id("DefaultTool.INFOMODE_TOOLTIP[Residents]"),
			unit = "integer"
		};
		m_Level = new IntTooltip
		{
			path = "level",
			icon = "Media/Game/Icons/BuildingLevel.svg",
			label = LocalizedString.Id("DefaultTool.INFOMODE_TOOLTIP[Level]"),
			unit = "integer"
		};
		m_HouseholdsResult = new NativeList<Entity>(Allocator.Persistent);
		m_ResidenceResult = new NativeValue<Entity>(Allocator.Persistent);
		m_Results = new NativeArray<int>(5, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_HouseholdsResult.Dispose();
		m_ResidenceResult.Dispose();
		m_Results.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_ToolSystem.activeTool != m_DefaultTool || !m_ToolRaycastSystem.GetRaycastResult(out var result) || !base.EntityManager.HasComponent<Building>(result.m_Owner) || base.EntityManager.HasComponent<UnderConstruction>(result.m_Owner) || base.EntityManager.HasComponent<Abandoned>(result.m_Owner) || base.EntityManager.HasComponent<Game.Buildings.Park>(result.m_Owner) || !base.EntityManager.TryGetComponent<PrefabRef>(result.m_Owner, out var component))
		{
			m_SelectedEntity = Entity.Null;
			return;
		}
		if (base.EntityManager.TryGetComponent<SpawnableBuildingData>(component.m_Prefab, out var component2))
		{
			m_Level.value = component2.m_Level;
			AddMouseTooltip(m_Level);
		}
		if (base.EntityManager.HasComponent<ResidentialProperty>(result.m_Owner))
		{
			if (result.m_Owner != m_SelectedEntity)
			{
				m_SelectedEntity = result.m_Owner;
				IJobExtensions.Schedule(new ResidentsSection.CountHouseholdsJob
				{
					m_SelectedEntity = m_SelectedEntity,
					m_SelectedPrefab = component.m_Prefab,
					m_BuildingLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
					m_ParkLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Park_RO_ComponentLookup, ref base.CheckedStateRef),
					m_AbandonedLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup, ref base.CheckedStateRef),
					m_HouseholdLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref base.CheckedStateRef),
					m_HomelessHouseholdLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HomelessHousehold_RO_ComponentLookup, ref base.CheckedStateRef),
					m_HealthProblemLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup, ref base.CheckedStateRef),
					m_TravelPurposeLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PropertyRenterLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PropertyDataLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_HouseholdCitizenLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
					m_HouseholdAnimalLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdAnimal_RO_BufferLookup, ref base.CheckedStateRef),
					m_RenterLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
					m_Results = m_Results,
					m_HouseholdsResult = m_HouseholdsResult,
					m_ResidenceResult = m_ResidenceResult
				}, base.Dependency).Complete();
			}
			m_Residents.value = m_Results[1];
			AddMouseTooltip(m_Residents);
		}
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
	public ResidentialTooltipSystem()
	{
	}
}

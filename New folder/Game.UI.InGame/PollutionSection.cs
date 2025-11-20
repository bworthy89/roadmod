using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class PollutionSection : InfoSectionBase
{
	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PollutionData> __Game_Prefabs_PollutionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PollutionModifierData> __Game_Prefabs_PollutionModifierData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Employee> __Game_Companies_Employee_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PollutionEmitModifier> __Game_Buildings_PollutionEmitModifier_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Prefabs_PollutionData_RO_ComponentLookup = state.GetComponentLookup<PollutionData>(isReadOnly: true);
			__Game_Prefabs_PollutionModifierData_RO_ComponentLookup = state.GetComponentLookup<PollutionModifierData>(isReadOnly: true);
			__Game_Prefabs_ZoneData_RO_ComponentLookup = state.GetComponentLookup<ZoneData>(isReadOnly: true);
			__Game_Companies_Employee_RO_BufferLookup = state.GetBufferLookup<Employee>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Buildings_PollutionEmitModifier_RO_ComponentLookup = state.GetComponentLookup<PollutionEmitModifier>(isReadOnly: true);
		}
	}

	private EntityQuery m_UIConfigQuery;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_1774369403_0;

	private EntityQuery __query_1774369403_1;

	protected override string group => "PollutionSection";

	protected override bool displayForDestroyedObjects => true;

	private PollutionThreshold groundPollutionKey { get; set; }

	private PollutionThreshold airPollutionKey { get; set; }

	private PollutionThreshold noisePollutionKey { get; set; }

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_UIConfigQuery = GetEntityQuery(ComponentType.ReadOnly<UIPollutionConfigurationData>());
	}

	protected override void Reset()
	{
		groundPollutionKey = PollutionThreshold.None;
		airPollutionKey = PollutionThreshold.None;
		noisePollutionKey = PollutionThreshold.None;
	}

	private bool Visible()
	{
		if (base.EntityManager.HasComponent<Building>(selectedEntity))
		{
			PollutionData pollution = GetPollution();
			if (!(pollution.m_GroundPollution > 0f) && !(pollution.m_AirPollution > 0f))
			{
				return pollution.m_NoisePollution > 0f;
			}
			return true;
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	protected override void OnProcess()
	{
		PollutionData pollution = GetPollution();
		UIPollutionConfigurationPrefab singletonPrefab = m_PrefabSystem.GetSingletonPrefab<UIPollutionConfigurationPrefab>(m_UIConfigQuery);
		groundPollutionKey = PollutionUIUtils.GetPollutionKey(singletonPrefab.m_GroundPollution, pollution.m_GroundPollution);
		airPollutionKey = PollutionUIUtils.GetPollutionKey(singletonPrefab.m_AirPollution, pollution.m_AirPollution);
		noisePollutionKey = PollutionUIUtils.GetPollutionKey(singletonPrefab.m_NoisePollution, pollution.m_NoisePollution);
	}

	private PollutionData GetPollution()
	{
		CompleteDependency();
		bool destroyed = base.EntityManager.HasComponent<Destroyed>(selectedEntity);
		bool abandoned = base.EntityManager.HasComponent<Abandoned>(selectedEntity);
		bool isPark = base.EntityManager.HasComponent<Game.Buildings.Park>(selectedEntity);
		DynamicBuffer<Efficiency> buffer;
		float efficiency = (base.EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out buffer) ? BuildingUtils.GetEfficiency(buffer) : 1f);
		base.EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<Renter> buffer2);
		base.EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<InstalledUpgrade> buffer3);
		PollutionParameterData singleton = __query_1774369403_0.GetSingleton<PollutionParameterData>();
		DynamicBuffer<CityModifier> singletonBuffer = __query_1774369403_1.GetSingletonBuffer<CityModifier>(isReadOnly: true);
		ComponentLookup<PrefabRef> prefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<BuildingData> buildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<SpawnableBuildingData> spawnableDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<PollutionData> pollutionDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PollutionData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<PollutionModifierData> pollutionModifierDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PollutionModifierData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<ZoneData> zoneDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup, ref base.CheckedStateRef);
		BufferLookup<Employee> employees = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_Employee_RO_BufferLookup, ref base.CheckedStateRef);
		BufferLookup<HouseholdCitizen> householdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef);
		ComponentLookup<Citizen> citizens = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<PollutionEmitModifier> pollutionEmitModifiers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PollutionEmitModifier_RO_ComponentLookup, ref base.CheckedStateRef);
		return BuildingPollutionAddSystem.GetBuildingPollution(selectedPrefab, destroyed, abandoned, isPark, efficiency, buffer2, buffer3, singleton, singletonBuffer, ref prefabRefs, ref buildingDatas, ref spawnableDatas, ref pollutionDatas, ref pollutionModifierDatas, ref zoneDatas, ref employees, ref householdCitizens, ref citizens, ref pollutionEmitModifiers);
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("groundPollutionKey");
		writer.Write((int)groundPollutionKey);
		writer.PropertyName("airPollutionKey");
		writer.Write((int)airPollutionKey);
		writer.PropertyName("noisePollutionKey");
		writer.Write((int)noisePollutionKey);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<PollutionParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_1774369403_0 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder2 = entityQueryBuilder.WithAllRW<CityModifier>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_1774369403_1 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder.Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public PollutionSection()
	{
	}
}

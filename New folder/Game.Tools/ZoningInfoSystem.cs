using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Simulation;
using Game.Zones;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class ZoningInfoSystem : GameSystemBase, IZoningInfoSystem
{
	private struct TypeHandle
	{
		[ReadOnly]
		public BufferLookup<ResourceAvailability> __Game_Net_ResourceAvailability_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<LandValue> __Game_Net_LandValue_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Net_ResourceAvailability_RO_BufferLookup = state.GetBufferLookup<ResourceAvailability>(isReadOnly: true);
			__Game_Net_LandValue_RO_ComponentLookup = state.GetComponentLookup<LandValue>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
		}
	}

	private EntityQuery m_ZoningPreferenceGroup;

	private EntityQuery m_ProcessQuery;

	private NativeList<ZoneEvaluationUtils.ZoningEvaluationResult> m_EvaluationResults;

	private ZoneToolSystem m_ZoneToolSystem;

	private IndustrialDemandSystem m_IndustrialDemandSystem;

	private GroundPollutionSystem m_GroundPollutionSystem;

	private AirPollutionSystem m_AirPollutionSystem;

	private NoisePollutionSystem m_NoisePollutionSystem;

	private PrefabSystem m_PrefabSystem;

	private ResourceSystem m_ResourceSystem;

	private ToolRaycastSystem m_ToolRaycastSystem;

	private TypeHandle __TypeHandle;

	public NativeList<ZoneEvaluationUtils.ZoningEvaluationResult> evaluationResults => m_EvaluationResults;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ZoneToolSystem = base.World.GetOrCreateSystemManaged<ZoneToolSystem>();
		m_IndustrialDemandSystem = base.World.GetOrCreateSystemManaged<IndustrialDemandSystem>();
		m_GroundPollutionSystem = base.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
		m_AirPollutionSystem = base.World.GetOrCreateSystemManaged<AirPollutionSystem>();
		m_NoisePollutionSystem = base.World.GetOrCreateSystemManaged<NoisePollutionSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_ToolRaycastSystem = base.World.GetOrCreateSystemManaged<ToolRaycastSystem>();
		m_ProcessQuery = GetEntityQuery(ComponentType.ReadOnly<IndustrialProcessData>());
		m_ZoningPreferenceGroup = GetEntityQuery(ComponentType.ReadOnly<ZonePreferenceData>());
		m_EvaluationResults = new NativeList<ZoneEvaluationUtils.ZoningEvaluationResult>(Allocator.Persistent);
		RequireForUpdate(m_ProcessQuery);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_EvaluationResults.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_EvaluationResults.Clear();
		if (m_ToolRaycastSystem.GetRaycastResult(out var result) && base.EntityManager.TryGetComponent<Block>(result.m_Owner, out var component) && base.EntityManager.TryGetComponent<Owner>(result.m_Owner, out var component2))
		{
			base.Dependency.Complete();
			BufferLookup<ResourceAvailability> bufferLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ResourceAvailability_RO_BufferLookup, ref base.CheckedStateRef);
			ComponentLookup<LandValue> componentLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LandValue_RO_ComponentLookup, ref base.CheckedStateRef);
			NativeArray<ZonePreferenceData> nativeArray = m_ZoningPreferenceGroup.ToComponentDataArray<ZonePreferenceData>(Allocator.TempJob);
			JobHandle deps;
			NativeArray<int> industrialResourceDemands = m_IndustrialDemandSystem.GetIndustrialResourceDemands(out deps);
			ResourcePrefabs prefabs = m_ResourceSystem.GetPrefabs();
			ComponentLookup<ResourceData> componentLookup2 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef);
			ZonePreferenceData preferences = nativeArray[0];
			Entity owner = component2.m_Owner;
			AreaType areaType = m_ZoneToolSystem.prefab.m_AreaType;
			JobHandle dependencies;
			NativeArray<GroundPollution> map = m_GroundPollutionSystem.GetMap(readOnly: true, out dependencies);
			JobHandle dependencies2;
			NativeArray<AirPollution> map2 = m_AirPollutionSystem.GetMap(readOnly: true, out dependencies2);
			JobHandle dependencies3;
			NativeArray<NoisePollution> map3 = m_NoisePollutionSystem.GetMap(readOnly: true, out dependencies3);
			deps.Complete();
			dependencies.Complete();
			dependencies2.Complete();
			dependencies3.Complete();
			float num = GroundPollutionSystem.GetPollution(component.m_Position, map).m_Pollution;
			num += (float)AirPollutionSystem.GetPollution(component.m_Position, map2).m_Pollution;
			num += (float)NoisePollutionSystem.GetPollution(component.m_Position, map3).m_Pollution;
			float num2 = componentLookup[owner].m_LandValue;
			Entity entity = m_PrefabSystem.GetEntity(m_ZoneToolSystem.prefab);
			DynamicBuffer<ProcessEstimate> buffer = base.World.EntityManager.GetBuffer<ProcessEstimate>(entity, isReadOnly: true);
			if (base.World.EntityManager.HasComponent<ZonePropertiesData>(entity))
			{
				ZonePropertiesData componentData = base.World.EntityManager.GetComponentData<ZonePropertiesData>(entity);
				float num3 = ((areaType != AreaType.Residential) ? componentData.m_SpaceMultiplier : (componentData.m_ScaleResidentials ? componentData.m_ResidentialProperties : (componentData.m_ResidentialProperties / 8f)));
				num2 /= num3;
			}
			JobHandle outJobHandle;
			NativeList<IndustrialProcessData> processes = m_ProcessQuery.ToComponentDataListAsync<IndustrialProcessData>(Allocator.TempJob, out outJobHandle);
			outJobHandle.Complete();
			ZoneEvaluationUtils.GetFactors(areaType, m_ZoneToolSystem.prefab.m_Office, bufferLookup[owner], result.m_Hit.m_CurvePosition, ref preferences, m_EvaluationResults, industrialResourceDemands, num, num2, buffer, processes, prefabs, componentLookup2);
			processes.Dispose();
			nativeArray.Dispose();
			m_EvaluationResults.Sort();
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
	public ZoningInfoSystem()
	{
	}
}

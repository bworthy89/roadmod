using System.Runtime.CompilerServices;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Zones;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Prefabs;

[CompilerGenerated]
public class ZonePrefabInitializeSystem : GameSystemBase
{
	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentTypeHandle;

		public ComponentTypeHandle<ZoneData> __Game_Prefabs_ZoneData_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle;

		public BufferTypeHandle<ProcessEstimate> __Game_Zones_ProcessEstimate_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabData>(isReadOnly: true);
			__Game_Prefabs_ZoneData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ZoneData>();
			__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingPropertyData>(isReadOnly: true);
			__Game_Zones_ProcessEstimate_RW_BufferTypeHandle = state.GetBufferTypeHandle<ProcessEstimate>();
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
		}
	}

	private EntityQuery m_PrefabGroup;

	private EntityQuery m_ProcessGroup;

	private EntityQuery m_EconomyParameterGroup;

	private PrefabSystem m_PrefabSystem;

	private ResourceSystem m_ResourceSystem;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_PrefabGroup = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<PrefabData>(),
				ComponentType.ReadOnly<ZoneData>()
			},
			Any = new ComponentType[0]
		});
		m_ProcessGroup = GetEntityQuery(ComponentType.ReadOnly<IndustrialProcessData>(), ComponentType.ReadOnly<IndustrialCompanyData>(), ComponentType.ReadOnly<WorkplaceData>(), ComponentType.Exclude<StorageCompanyData>());
		m_EconomyParameterGroup = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		RequireForUpdate(m_ProcessGroup);
		RequireForUpdate(m_EconomyParameterGroup);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_PrefabGroup.IsEmptyIgnoreFilter)
		{
			return;
		}
		NativeArray<ArchetypeChunk> nativeArray = m_PrefabGroup.ToArchetypeChunkArray(Allocator.TempJob);
		EntityTypeHandle entityTypeHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef);
		InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<ZoneData> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ZoneData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<BuildingPropertyData> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		BufferTypeHandle<ProcessEstimate> bufferTypeHandle = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Zones_ProcessEstimate_RW_BufferTypeHandle, ref base.CheckedStateRef);
		EconomyParameterData economyParameters = m_EconomyParameterGroup.GetSingleton<EconomyParameterData>();
		ResourcePrefabs prefabs = m_ResourceSystem.GetPrefabs();
		NativeArray<Entity> nativeArray2 = m_ProcessGroup.ToEntityArray(Allocator.TempJob);
		NativeArray<IndustrialProcessData> nativeArray3 = m_ProcessGroup.ToComponentDataArray<IndustrialProcessData>(Allocator.TempJob);
		NativeArray<WorkplaceData> nativeArray4 = m_ProcessGroup.ToComponentDataArray<WorkplaceData>(Allocator.TempJob);
		ComponentLookup<ResourceData> resourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			ArchetypeChunk archetypeChunk = nativeArray[i];
			NativeArray<Entity> nativeArray5 = archetypeChunk.GetNativeArray(entityTypeHandle);
			NativeArray<ZoneData> nativeArray6 = archetypeChunk.GetNativeArray(ref typeHandle);
			NativeArray<BuildingPropertyData> nativeArray7 = archetypeChunk.GetNativeArray(ref typeHandle2);
			BufferAccessor<ProcessEstimate> bufferAccessor = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle);
			if (nativeArray6.Length <= 0)
			{
				continue;
			}
			for (int j = 0; j < archetypeChunk.Count; j++)
			{
				DynamicBuffer<ProcessEstimate> dynamicBuffer = bufferAccessor[j];
				bool office = m_PrefabSystem.GetPrefab<ZonePrefab>(nativeArray5[j]).m_Office;
				if (office)
				{
					ZoneData value = nativeArray6[j];
					value.m_ZoneFlags |= ZoneFlags.Office;
					nativeArray6[j] = value;
				}
				if (nativeArray6[j].m_AreaType != AreaType.Industrial || office)
				{
					continue;
				}
				float num = 1f;
				if (nativeArray7.Length > 0)
				{
					num = nativeArray7[j].m_SpaceMultiplier;
				}
				for (int k = 0; k < EconomyUtils.ResourceCount; k++)
				{
					dynamicBuffer.Add(default(ProcessEstimate));
				}
				for (int l = 0; l < nativeArray3.Length; l++)
				{
					IndustrialProcessData industrialProcessData = nativeArray3[l];
					int num2 = Mathf.RoundToInt(num * industrialProcessData.m_MaxWorkersPerCell * 100f);
					WorkplaceData workplaceData = nativeArray4[l];
					Workplaces workplaces = EconomyUtils.CalculateNumberOfWorkplaces(num2, workplaceData.m_Complexity, 1);
					float num3 = 0f;
					float num4 = 1f;
					for (int m = 0; m < 5; m++)
					{
						float num5 = (float)workplaces[m] * EconomyUtils.GetWorkerWorkforce(50, m);
						if (m < 2)
						{
							num3 += num5;
						}
						else
						{
							num4 += num5;
						}
					}
					int resourceIndex = EconomyUtils.GetResourceIndex(industrialProcessData.m_Output.m_Resource);
					BuildingData buildingData = new BuildingData
					{
						m_LotSize = new int2(10, 10)
					};
					EconomyUtils.BuildPseudoTradeCost(5000f, industrialProcessData, ref resourceDatas, prefabs);
					float num6 = 1f * (float)EconomyUtils.GetCompanyProductionPerDay(1f, num2, new SpawnableBuildingData
					{
						m_Level = 1
					}.m_Level, isIndustrial: true, workplaceData, industrialProcessData, prefabs, ref resourceDatas, ref economyParameters) / (float)EconomyUtils.kCompanyUpdatesPerDay;
					ProcessEstimate value2 = new ProcessEstimate
					{
						m_ProductionPerCell = 0.01f * num6,
						m_WorkerProductionPerCell = 0.01f * num6 / (num * industrialProcessData.m_MaxWorkersPerCell),
						m_LowEducationWeight = num3 / (num3 + num4),
						m_ProcessEntity = nativeArray2[l]
					};
					dynamicBuffer[resourceIndex] = value2;
				}
			}
		}
		nativeArray3.Dispose();
		nativeArray4.Dispose();
		nativeArray.Dispose();
		nativeArray2.Dispose();
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
	public ZonePrefabInitializeSystem()
	{
	}
}

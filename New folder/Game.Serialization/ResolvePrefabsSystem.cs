using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Prefabs;
using Game.Simulation;
using Game.Zones;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Serialization;

[CompilerGenerated]
public class ResolvePrefabsSystem : GameSystemBase
{
	private struct ComponentModification
	{
		public Entity m_Entity;

		public PrefabComponents m_Add;

		public PrefabComponents m_Remove;

		public ComponentModification(Entity entity, PrefabComponents add, PrefabComponents remove)
		{
			m_Entity = entity;
			m_Add = add;
			m_Remove = remove;
		}
	}

	[BurstCompile]
	private struct FillLoadedPrefabsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabData> m_PrefabDataType;

		[ReadOnly]
		public ComponentTypeHandle<Locked> m_LockedType;

		[ReadOnly]
		public ComponentTypeHandle<PlacedSignatureBuildingData> m_PlacedSignatureType;

		[NativeDisableParallelForRestriction]
		public NativeArray<Entity> m_PrefabArray;

		[NativeDisableParallelForRestriction]
		public NativeArray<PrefabComponents> m_PrefabComponents;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabData> nativeArray2 = chunk.GetNativeArray(ref m_PrefabDataType);
			PrefabComponents prefabComponents = (PrefabComponents)0u;
			EnabledMask enabledMask = chunk.GetEnabledMask(ref m_LockedType);
			if (chunk.Has(ref m_PlacedSignatureType))
			{
				prefabComponents |= PrefabComponents.PlacedSignatureBuilding;
			}
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				int index = nativeArray2[i].m_Index;
				index = math.select(index, m_PrefabArray.Length + index, index < 0);
				m_PrefabArray[index] = nativeArray[i];
				PrefabComponents prefabComponents2 = prefabComponents;
				if (enabledMask.EnableBit.IsValid && enabledMask[i])
				{
					prefabComponents2 |= PrefabComponents.Locked;
				}
				m_PrefabComponents[index] = prefabComponents2;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct CheckActualPrefabsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabData> m_PrefabDataType;

		[ReadOnly]
		public ComponentTypeHandle<SignatureBuildingData> m_SignatureBuildingType;

		[ReadOnly]
		public ComponentTypeHandle<PlacedSignatureBuildingData> m_PlacedSignatureBuildingType;

		[ReadOnly]
		public BufferTypeHandle<LoadedIndex> m_LoadedIndexType;

		[ReadOnly]
		public Context m_Context;

		[ReadOnly]
		public NativeArray<PrefabComponents> m_PrefabComponents;

		public ComponentTypeHandle<Locked> m_LockedType;

		[NativeDisableParallelForRestriction]
		public NativeArray<Entity> m_PrefabArray;

		public NativeQueue<ComponentModification>.ParallelWriter m_ComponentModifications;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabData> nativeArray2 = chunk.GetNativeArray(ref m_PrefabDataType);
			BufferAccessor<LoadedIndex> bufferAccessor = chunk.GetBufferAccessor(ref m_LoadedIndexType);
			PrefabComponents prefabComponents = (PrefabComponents)0u;
			if (chunk.Has(ref m_SignatureBuildingType))
			{
				prefabComponents |= PrefabComponents.PlacedSignatureBuilding;
			}
			PrefabComponents prefabComponents2 = (PrefabComponents)0u;
			EnabledMask enabledMask = chunk.GetEnabledMask(ref m_LockedType);
			if (chunk.Has(ref m_PlacedSignatureBuildingType))
			{
				prefabComponents2 |= PrefabComponents.PlacedSignatureBuilding;
			}
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity entity = nativeArray[i];
				DynamicBuffer<LoadedIndex> dynamicBuffer = bufferAccessor[i];
				PrefabComponents prefabComponents3 = PrefabComponents.Locked;
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					int index = dynamicBuffer[j].m_Index;
					index = math.select(index, m_PrefabArray.Length + index, index < 0);
					m_PrefabArray[index] = entity;
					if (m_Context.purpose == Purpose.LoadGame)
					{
						prefabComponents3 = m_PrefabComponents[index];
					}
				}
				if (enabledMask.EnableBit.IsValid)
				{
					enabledMask[i] = (prefabComponents3 & PrefabComponents.Locked) != 0;
					prefabComponents3 = (PrefabComponents)((uint)prefabComponents3 & 0xFFFFFFFEu);
				}
				prefabComponents3 &= prefabComponents;
				if (prefabComponents3 != prefabComponents2)
				{
					m_ComponentModifications.Enqueue(new ComponentModification(entity, prefabComponents3 & ~prefabComponents2, prefabComponents2 & ~prefabComponents3));
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct CopyBudgetDataJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabData> m_PrefabDataType;

		[ReadOnly]
		public BufferTypeHandle<LoadedIndex> m_LoadedIndexType;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<CollectedCityServiceBudgetData> m_Budgets;

		[NativeDisableParallelForRestriction]
		public BufferLookup<CollectedCityServiceFeeData> m_Fees;

		[NativeDisableParallelForRestriction]
		public BufferLookup<CollectedCityServiceUpkeepData> m_Upkeeps;

		[ReadOnly]
		public NativeArray<Entity> m_PrefabArray;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabData> nativeArray2 = chunk.GetNativeArray(ref m_PrefabDataType);
			BufferAccessor<LoadedIndex> bufferAccessor = chunk.GetBufferAccessor(ref m_LoadedIndexType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity entity = nativeArray[i];
				DynamicBuffer<LoadedIndex> dynamicBuffer = bufferAccessor[i];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					int index = dynamicBuffer[j].m_Index;
					index = math.select(index, m_PrefabArray.Length + index, index < 0);
					Entity entity2 = m_PrefabArray[index];
					if (m_Budgets.HasComponent(entity) && m_Budgets.HasComponent(entity2))
					{
						m_Budgets[entity] = m_Budgets[entity2];
					}
					if (m_Fees.HasBuffer(entity) && m_Fees.HasBuffer(entity2))
					{
						DynamicBuffer<CollectedCityServiceFeeData> dynamicBuffer2 = m_Fees[entity];
						DynamicBuffer<CollectedCityServiceFeeData> dynamicBuffer3 = m_Fees[entity2];
						for (int k = 0; k < dynamicBuffer3.Length; k++)
						{
							for (int l = 0; l < dynamicBuffer2.Length; l++)
							{
								if (dynamicBuffer2[l].m_PlayerResource == dynamicBuffer3[k].m_PlayerResource)
								{
									dynamicBuffer2[l] = dynamicBuffer3[k];
								}
							}
						}
					}
					if (!m_Upkeeps.HasBuffer(entity) || !m_Upkeeps.HasBuffer(entity2))
					{
						continue;
					}
					DynamicBuffer<CollectedCityServiceUpkeepData> dynamicBuffer4 = m_Upkeeps[entity];
					DynamicBuffer<CollectedCityServiceUpkeepData> dynamicBuffer5 = m_Upkeeps[entity2];
					for (int m = 0; m < dynamicBuffer5.Length; m++)
					{
						for (int n = 0; n < dynamicBuffer4.Length; n++)
						{
							if (dynamicBuffer4[n].m_Resource == dynamicBuffer5[m].m_Resource)
							{
								dynamicBuffer4[n] = dynamicBuffer5[m];
							}
						}
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FillZoneTypeArrayJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabData> m_PrefabDataType;

		[ReadOnly]
		public ComponentTypeHandle<ZoneData> m_ZoneDataType;

		[ReadOnly]
		public ComponentLookup<ZoneData> m_ZoneData;

		[ReadOnly]
		public NativeArray<Entity> m_PrefabArray;

		[NativeDisableParallelForRestriction]
		public NativeArray<ZoneType> m_ZoneTypeArray;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabData> nativeArray2 = chunk.GetNativeArray(ref m_PrefabDataType);
			NativeArray<ZoneData> nativeArray3 = chunk.GetNativeArray(ref m_ZoneDataType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity entity = nativeArray[i];
				int index = nativeArray2[i].m_Index;
				index = math.select(index, m_PrefabArray.Length + index, index < 0);
				Entity entity2 = m_PrefabArray[index];
				ZoneType zoneType = nativeArray3[i].m_ZoneType;
				ZoneType value = m_ZoneData[entity2].m_ZoneType;
				if (entity == entity2)
				{
					value = ZoneType.None;
				}
				m_ZoneTypeArray[zoneType.m_Index] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FixZoneTypeJob : IJobChunk
	{
		[ReadOnly]
		public NativeArray<ZoneType> m_ZoneTypeArray;

		public BufferTypeHandle<Cell> m_CellType;

		public BufferTypeHandle<VacantLot> m_VacantLotType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<Cell> bufferAccessor = chunk.GetBufferAccessor(ref m_CellType);
			BufferAccessor<VacantLot> bufferAccessor2 = chunk.GetBufferAccessor(ref m_VacantLotType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				DynamicBuffer<Cell> dynamicBuffer = bufferAccessor[i];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					Cell value = dynamicBuffer[j];
					value.m_Zone = m_ZoneTypeArray[value.m_Zone.m_Index];
					dynamicBuffer[j] = value;
				}
			}
			for (int k = 0; k < bufferAccessor2.Length; k++)
			{
				DynamicBuffer<VacantLot> dynamicBuffer2 = bufferAccessor2[k];
				for (int l = 0; l < dynamicBuffer2.Length; l++)
				{
					VacantLot value2 = dynamicBuffer2[l];
					value2.m_Type = m_ZoneTypeArray[value2.m_Type.m_Index];
					dynamicBuffer2[l] = value2;
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Locked> __Game_Prefabs_Locked_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PlacedSignatureBuildingData> __Game_Prefabs_PlacedSignatureBuildingData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<SignatureBuildingData> __Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<LoadedIndex> __Game_Prefabs_LoadedIndex_RO_BufferTypeHandle;

		public ComponentTypeHandle<Locked> __Game_Prefabs_Locked_RW_ComponentTypeHandle;

		public ComponentLookup<CollectedCityServiceBudgetData> __Game_Simulation_CollectedCityServiceBudgetData_RW_ComponentLookup;

		public BufferLookup<CollectedCityServiceFeeData> __Game_Simulation_CollectedCityServiceFeeData_RW_BufferLookup;

		public BufferLookup<CollectedCityServiceUpkeepData> __Game_Simulation_CollectedCityServiceUpkeepData_RW_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentLookup;

		public BufferTypeHandle<Cell> __Game_Zones_Cell_RW_BufferTypeHandle;

		public BufferTypeHandle<VacantLot> __Game_Zones_VacantLot_RW_BufferTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabData>(isReadOnly: true);
			__Game_Prefabs_Locked_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Locked>(isReadOnly: true);
			__Game_Prefabs_PlacedSignatureBuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PlacedSignatureBuildingData>(isReadOnly: true);
			__Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SignatureBuildingData>(isReadOnly: true);
			__Game_Prefabs_LoadedIndex_RO_BufferTypeHandle = state.GetBufferTypeHandle<LoadedIndex>(isReadOnly: true);
			__Game_Prefabs_Locked_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Locked>();
			__Game_Simulation_CollectedCityServiceBudgetData_RW_ComponentLookup = state.GetComponentLookup<CollectedCityServiceBudgetData>();
			__Game_Simulation_CollectedCityServiceFeeData_RW_BufferLookup = state.GetBufferLookup<CollectedCityServiceFeeData>();
			__Game_Simulation_CollectedCityServiceUpkeepData_RW_BufferLookup = state.GetBufferLookup<CollectedCityServiceUpkeepData>();
			__Game_Prefabs_ZoneData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ZoneData>(isReadOnly: true);
			__Game_Prefabs_ZoneData_RO_ComponentLookup = state.GetComponentLookup<ZoneData>(isReadOnly: true);
			__Game_Zones_Cell_RW_BufferTypeHandle = state.GetBufferTypeHandle<Cell>();
			__Game_Zones_VacantLot_RW_BufferTypeHandle = state.GetBufferTypeHandle<VacantLot>();
		}
	}

	private LoadGameSystem m_LoadGameSystem;

	private PrefabSystem m_PrefabSystem;

	private UpdateSystem m_UpdateSystem;

	private CheckPrefabReferencesSystem m_CheckPrefabReferencesSystem;

	private EntityQuery m_ActualPrefabQuery;

	private EntityQuery m_EnabledLoadedPrefabQuery;

	private EntityQuery m_AllLoadedPrefabQuery;

	private EntityQuery m_LoadedZonePrefabQuery;

	private EntityQuery m_LoadedZoneCellQuery;

	private EntityQuery m_ActualBudgetQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_LoadGameSystem = base.World.GetOrCreateSystemManaged<LoadGameSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_UpdateSystem = base.World.GetOrCreateSystemManaged<UpdateSystem>();
		m_CheckPrefabReferencesSystem = base.World.GetOrCreateSystemManaged<CheckPrefabReferencesSystem>();
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		entityQueryBuilder = entityQueryBuilder.WithAll<PrefabData, LoadedIndex>();
		entityQueryBuilder = entityQueryBuilder.WithOptions(EntityQueryOptions.IgnoreComponentEnabledState);
		m_ActualPrefabQuery = entityQueryBuilder.Build(this);
		m_EnabledLoadedPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabData>(), ComponentType.Exclude<LoadedIndex>());
		entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		entityQueryBuilder = entityQueryBuilder.WithAll<PrefabData>();
		entityQueryBuilder = entityQueryBuilder.WithNone<LoadedIndex>();
		entityQueryBuilder = entityQueryBuilder.WithOptions(EntityQueryOptions.IgnoreComponentEnabledState);
		m_AllLoadedPrefabQuery = entityQueryBuilder.Build(this);
		m_LoadedZonePrefabQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabData>(), ComponentType.ReadOnly<ZoneData>(), ComponentType.Exclude<LoadedIndex>());
		m_LoadedZoneCellQuery = GetEntityQuery(ComponentType.ReadOnly<Cell>());
		m_ActualBudgetQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<PrefabData>(),
				ComponentType.ReadOnly<LoadedIndex>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<CollectedCityServiceBudgetData>(),
				ComponentType.ReadOnly<CollectedCityServiceFeeData>(),
				ComponentType.ReadOnly<CollectedCityServiceUpkeepData>()
			}
		});
	}

	[Preserve]
	protected override void OnUpdate()
	{
		int length = m_EnabledLoadedPrefabQuery.CalculateEntityCount();
		NativeArray<Entity> nativeArray = new NativeArray<Entity>(length, Allocator.TempJob);
		NativeArray<PrefabComponents> prefabComponents = new NativeArray<PrefabComponents>(length, Allocator.TempJob);
		NativeArray<ZoneType> zoneTypeArray = new NativeArray<ZoneType>(340, Allocator.TempJob);
		NativeQueue<ComponentModification> nativeQueue = new NativeQueue<ComponentModification>(Allocator.TempJob);
		FillLoadedPrefabsJob jobData = new FillLoadedPrefabsJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LockedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_Locked_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PlacedSignatureType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PlacedSignatureBuildingData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabArray = nativeArray,
			m_PrefabComponents = prefabComponents
		};
		CheckActualPrefabsJob jobData2 = new CheckActualPrefabsJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SignatureBuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PlacedSignatureBuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PlacedSignatureBuildingData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LoadedIndexType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_LoadedIndex_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_Context = m_LoadGameSystem.context,
			m_PrefabComponents = prefabComponents,
			m_LockedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_Locked_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabArray = nativeArray,
			m_ComponentModifications = nativeQueue.AsParallelWriter()
		};
		CopyBudgetDataJob jobData3 = new CopyBudgetDataJob
		{
			m_PrefabDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LoadedIndexType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_LoadedIndex_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PrefabArray = nativeArray,
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_Budgets = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_CollectedCityServiceBudgetData_RW_ComponentLookup, ref base.CheckedStateRef),
			m_Fees = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_CollectedCityServiceFeeData_RW_BufferLookup, ref base.CheckedStateRef),
			m_Upkeeps = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_CollectedCityServiceUpkeepData_RW_BufferLookup, ref base.CheckedStateRef)
		};
		FillZoneTypeArrayJob jobData4 = new FillZoneTypeArrayJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ZoneDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ZoneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabArray = nativeArray,
			m_ZoneTypeArray = zoneTypeArray
		};
		FixZoneTypeJob jobData5 = new FixZoneTypeJob
		{
			m_ZoneTypeArray = zoneTypeArray,
			m_CellType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Zones_Cell_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_VacantLotType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Zones_VacantLot_RW_BufferTypeHandle, ref base.CheckedStateRef)
		};
		JobHandle dependsOn = JobChunkExtensions.ScheduleParallel(jobData, m_EnabledLoadedPrefabQuery, base.Dependency);
		JobHandle.ScheduleBatchedJobs();
		m_PrefabSystem.UpdateLoadedIndices();
		JobHandle dependsOn2 = JobChunkExtensions.ScheduleParallel(dependsOn: JobChunkExtensions.ScheduleParallel(dependsOn: JobChunkExtensions.ScheduleParallel(jobData3, m_ActualBudgetQuery, dependsOn), jobData: jobData2, query: m_ActualPrefabQuery), jobData: jobData4, query: m_LoadedZonePrefabQuery);
		JobHandle dependencies = JobChunkExtensions.ScheduleParallel(jobData5, m_LoadedZoneCellQuery, dependsOn2);
		dependsOn2.Complete();
		base.EntityManager.SetComponentEnabled<PrefabData>(m_ActualPrefabQuery, value: false);
		base.EntityManager.SetComponentEnabled<PrefabData>(m_EnabledLoadedPrefabQuery, value: false);
		m_CheckPrefabReferencesSystem.BeginPrefabCheck(nativeArray, isLoading: true, dependencies);
		m_UpdateSystem.Update(SystemUpdatePhase.PrefabReferences);
		m_CheckPrefabReferencesSystem.EndPrefabCheck(out var dependencies2);
		dependencies2.Complete();
		base.EntityManager.SetComponentEnabled<PrefabData>(m_ActualPrefabQuery, value: true);
		NativeArray<Entity> entities = m_EnabledLoadedPrefabQuery.ToEntityArray(Allocator.TempJob);
		ComponentType type = ComponentType.ReadWrite<PlacedSignatureBuildingData>();
		ComponentModification item;
		while (nativeQueue.TryDequeue(out item))
		{
			AddOrRemoveComponent(item, PrefabComponents.PlacedSignatureBuilding, type);
		}
		if (entities.Length != 0)
		{
			base.EntityManager.AddComponent<LoadedIndex>(entities);
			for (int i = 0; i < entities.Length; i++)
			{
				Entity entity = entities[i];
				PrefabData componentData = base.EntityManager.GetComponentData<PrefabData>(entity);
				PrefabID loadedObsoleteID = m_PrefabSystem.GetLoadedObsoleteID(componentData.m_Index);
				componentData.m_Index = -1 - i;
				base.EntityManager.SetComponentData(entity, componentData);
				base.EntityManager.SetComponentEnabled<PrefabData>(entity, value: false);
				m_PrefabSystem.AddObsoleteID(entity, loadedObsoleteID);
			}
		}
		base.EntityManager.DestroyEntity(m_AllLoadedPrefabQuery);
		nativeArray.Dispose();
		prefabComponents.Dispose();
		zoneTypeArray.Dispose();
		nativeQueue.Dispose();
		entities.Dispose();
	}

	private void AddOrRemoveComponent(ComponentModification componentModification, PrefabComponents mask, ComponentType type)
	{
		if ((componentModification.m_Remove & mask) != 0)
		{
			base.EntityManager.RemoveComponent(componentModification.m_Entity, type);
		}
		else if ((componentModification.m_Add & mask) != 0)
		{
			base.EntityManager.AddComponent(componentModification.m_Entity, type);
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
	public ResolvePrefabsSystem()
	{
	}
}

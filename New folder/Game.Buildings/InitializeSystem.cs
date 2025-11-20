using System.Runtime.CompilerServices;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Buildings;

[CompilerGenerated]
public class InitializeSystem : GameSystemBase
{
	[BurstCompile]
	private struct InitializeCoverageTypeJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntitiesType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<CoverageData> m_PrefabCoverageData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntitiesType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			for (int i = 0; i < chunk.Count; i++)
			{
				PrefabRef prefabRef = nativeArray2[i];
				Entity e = nativeArray[i];
				CoverageData coverageData = m_PrefabCoverageData[prefabRef.m_Prefab];
				m_CommandBuffer.SetSharedComponent(unfilteredChunkIndex, e, new CoverageServiceType(coverageData.m_Service));
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct InitializeBuildingsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntitiesType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<Building> m_BuildingType;

		[ReadOnly]
		public ComponentTypeHandle<PoliceStation> m_PoliceStationType;

		[ReadOnly]
		public ComponentTypeHandle<PostFacility> m_PostFacilityType;

		[ReadOnly]
		public ComponentTypeHandle<Destroyed> m_DestroyedType;

		[ReadOnly]
		public ComponentTypeHandle<Created> m_CreatedType;

		[ReadOnly]
		public ComponentTypeHandle<Abandoned> m_AbandonedType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> m_ElectricityConsumerData;

		[ReadOnly]
		public ComponentLookup<WaterConsumer> m_WaterConsumerData;

		[ReadOnly]
		public ComponentLookup<GarbageProducer> m_GarbageProducerData;

		[ReadOnly]
		public ComponentLookup<MailProducer> m_MailProducerData;

		[ReadOnly]
		public ComponentLookup<ConsumptionData> m_ConsumptionData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<GateData> m_GateData;

		[ReadOnly]
		public ComponentLookup<ObjectData> m_ObjectData;

		[ReadOnly]
		public ComponentLookup<RouteData> m_RouteData;

		public ComponentTypeSet m_DestroyedBuildingComponents;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<Entity>.ParallelWriter m_UpdatedElectricityRoadEdges;

		public NativeQueue<Entity>.ParallelWriter m_UpdatedWaterPipeRoadEdges;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntitiesType);
			NativeArray<Building> nativeArray2 = chunk.GetNativeArray(ref m_BuildingType);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
			bool flag = chunk.Has(ref m_PoliceStationType);
			bool flag2 = chunk.Has(ref m_PostFacilityType);
			bool flag3 = chunk.Has(ref m_DestroyedType);
			bool flag4 = chunk.Has(ref m_CreatedType);
			bool flag5 = chunk.Has(ref m_AbandonedType);
			bool flag6 = chunk.Has(ref m_TempType);
			if (!flag4 && flag3)
			{
				return;
			}
			TypeIndex typeIndex = TypeManager.GetTypeIndex<ElectricityConsumer>();
			TypeIndex typeIndex2 = TypeManager.GetTypeIndex<WaterConsumer>();
			TypeIndex typeIndex3 = TypeManager.GetTypeIndex<GarbageProducer>();
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity prefab = nativeArray3[i].m_Prefab;
				Entity entity = nativeArray[i];
				if (m_ConsumptionData.HasComponent(prefab) && (m_BuildingData[prefab].m_Flags & Game.Prefabs.BuildingFlags.RequireRoad) != 0)
				{
					if (flag4 && !flag)
					{
						m_CommandBuffer.AddComponent<CrimeProducer>(unfilteredChunkIndex, entity);
					}
					if (!flag2 && !flag3 && !flag5 && (flag4 || !m_MailProducerData.HasComponent(entity)))
					{
						m_CommandBuffer.AddComponent<MailProducer>(unfilteredChunkIndex, entity);
					}
				}
				if (flag4 && !flag6 && m_GateData.TryGetComponent(prefab, out var componentData) && m_RouteData.TryGetComponent(componentData.m_BypassPathPrefab, out var componentData2) && componentData2.m_RouteArchetype.Valid)
				{
					Entity entity2 = m_CommandBuffer.CreateEntity(unfilteredChunkIndex, componentData2.m_RouteArchetype);
					m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity2, new PrefabRef(componentData.m_BypassPathPrefab));
					m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity2, new Game.Routes.Color(componentData2.m_Color));
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity2, new Owner(entity));
					for (int j = 0; j < 2; j++)
					{
						Entity entity3 = m_CommandBuffer.CreateEntity(unfilteredChunkIndex, componentData2.m_SegmentArchetype);
						m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity3, new PrefabRef(componentData.m_BypassPathPrefab));
						m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity3, new Game.Routes.Segment(j));
						m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity3, new Owner(entity2));
						m_CommandBuffer.AppendToBuffer(unfilteredChunkIndex, entity2, new RouteSegment(entity3));
					}
				}
				if (flag3)
				{
					m_CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity, in m_DestroyedBuildingComponents);
				}
				if (flag4 || flag5 || !m_ObjectData.TryGetComponent(prefab, out var componentData3))
				{
					continue;
				}
				Building building = nativeArray2[i];
				NativeArray<ComponentType> componentTypes = componentData3.m_Archetype.GetComponentTypes();
				for (int k = 0; k < componentTypes.Length; k++)
				{
					ComponentType componentType = componentTypes[k];
					if (componentType.TypeIndex == typeIndex)
					{
						if (!m_ElectricityConsumerData.HasComponent(entity))
						{
							m_CommandBuffer.AddComponent<ElectricityConsumer>(unfilteredChunkIndex, entity);
							if (building.m_RoadEdge != Entity.Null)
							{
								m_UpdatedElectricityRoadEdges.Enqueue(building.m_RoadEdge);
							}
						}
					}
					else if (componentType.TypeIndex == typeIndex2)
					{
						if (!m_WaterConsumerData.HasComponent(entity))
						{
							m_CommandBuffer.AddComponent<WaterConsumer>(unfilteredChunkIndex, entity);
							if (building.m_RoadEdge != Entity.Null)
							{
								m_UpdatedWaterPipeRoadEdges.Enqueue(building.m_RoadEdge);
							}
						}
					}
					else if (componentType.TypeIndex == typeIndex3 && !m_GarbageProducerData.HasComponent(entity))
					{
						m_CommandBuffer.AddComponent<GarbageProducer>(unfilteredChunkIndex, entity);
					}
				}
				componentTypes.Dispose();
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
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<CoverageData> __Game_Prefabs_CoverageData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<Building> __Game_Buildings_Building_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PoliceStation> __Game_Buildings_PoliceStation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PostFacility> __Game_Buildings_PostFacility_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Destroyed> __Game_Common_Destroyed_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Created> __Game_Common_Created_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Abandoned> __Game_Buildings_Abandoned_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> __Game_Buildings_ElectricityConsumer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaterConsumer> __Game_Buildings_WaterConsumer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GarbageProducer> __Game_Buildings_GarbageProducer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MailProducer> __Game_Buildings_MailProducer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ConsumptionData> __Game_Prefabs_ConsumptionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GateData> __Game_Prefabs_GateData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectData> __Game_Prefabs_ObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RouteData> __Game_Prefabs_RouteData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_CoverageData_RO_ComponentLookup = state.GetComponentLookup<CoverageData>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Building>(isReadOnly: true);
			__Game_Buildings_PoliceStation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PoliceStation>(isReadOnly: true);
			__Game_Buildings_PostFacility_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PostFacility>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Destroyed>(isReadOnly: true);
			__Game_Common_Created_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Created>(isReadOnly: true);
			__Game_Buildings_Abandoned_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Abandoned>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Buildings_ElectricityConsumer_RO_ComponentLookup = state.GetComponentLookup<ElectricityConsumer>(isReadOnly: true);
			__Game_Buildings_WaterConsumer_RO_ComponentLookup = state.GetComponentLookup<WaterConsumer>(isReadOnly: true);
			__Game_Buildings_GarbageProducer_RO_ComponentLookup = state.GetComponentLookup<GarbageProducer>(isReadOnly: true);
			__Game_Buildings_MailProducer_RO_ComponentLookup = state.GetComponentLookup<MailProducer>(isReadOnly: true);
			__Game_Prefabs_ConsumptionData_RO_ComponentLookup = state.GetComponentLookup<ConsumptionData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_GateData_RO_ComponentLookup = state.GetComponentLookup<GateData>(isReadOnly: true);
			__Game_Prefabs_ObjectData_RO_ComponentLookup = state.GetComponentLookup<ObjectData>(isReadOnly: true);
			__Game_Prefabs_RouteData_RO_ComponentLookup = state.GetComponentLookup<RouteData>(isReadOnly: true);
		}
	}

	private ModificationBarrier2 m_ModificationBarrier;

	private ElectricityRoadConnectionGraphSystem m_ElectricityRoadConnectionGraphSystem;

	private WaterPipeRoadConnectionGraphSystem m_WaterPipeRoadConnectionGraphSystem;

	private EntityQuery m_CoverageQuery;

	private EntityQuery m_BuildingQuery;

	private ComponentTypeSet m_DestroyedBuildingComponents;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier2>();
		m_ElectricityRoadConnectionGraphSystem = base.World.GetOrCreateSystemManaged<ElectricityRoadConnectionGraphSystem>();
		m_WaterPipeRoadConnectionGraphSystem = base.World.GetOrCreateSystemManaged<WaterPipeRoadConnectionGraphSystem>();
		m_CoverageQuery = GetEntityQuery(ComponentType.ReadOnly<CoverageServiceType>(), ComponentType.ReadOnly<CoverageElement>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Created>(), ComponentType.Exclude<Placeholder>());
		m_BuildingQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Updated>(), ComponentType.Exclude<ServiceUpgrade>(), ComponentType.Exclude<Placeholder>());
		m_DestroyedBuildingComponents = new ComponentTypeSet(ComponentType.ReadOnly<ElectricityConsumer>(), ComponentType.ReadOnly<WaterConsumer>(), ComponentType.ReadOnly<GarbageProducer>(), ComponentType.ReadOnly<MailProducer>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_CoverageQuery.IsEmptyIgnoreFilter)
		{
			JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new InitializeCoverageTypeJob
			{
				m_EntitiesType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabCoverageData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CoverageData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
			}, m_CoverageQuery, base.Dependency);
			m_ModificationBarrier.AddJobHandleForProducer(jobHandle);
			base.Dependency = jobHandle;
		}
		if (!m_BuildingQuery.IsEmptyIgnoreFilter)
		{
			JobHandle deps;
			JobHandle deps2;
			JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(new InitializeBuildingsJob
			{
				m_EntitiesType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_BuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PoliceStationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_PoliceStation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PostFacilityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_PostFacility_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_DestroyedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CreatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Created_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_AbandonedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Abandoned_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ElectricityConsumerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
				m_WaterConsumerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
				m_GarbageProducerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_GarbageProducer_RO_ComponentLookup, ref base.CheckedStateRef),
				m_MailProducerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_MailProducer_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ConsumptionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ConsumptionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_GateData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_GateData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_RouteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RouteData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_DestroyedBuildingComponents = m_DestroyedBuildingComponents,
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter(),
				m_UpdatedElectricityRoadEdges = m_ElectricityRoadConnectionGraphSystem.GetEdgeUpdateQueue(out deps).AsParallelWriter(),
				m_UpdatedWaterPipeRoadEdges = m_WaterPipeRoadConnectionGraphSystem.GetEdgeUpdateQueue(out deps2).AsParallelWriter()
			}, m_BuildingQuery, JobHandle.CombineDependencies(base.Dependency, deps, deps2));
			m_ModificationBarrier.AddJobHandleForProducer(jobHandle2);
			m_ElectricityRoadConnectionGraphSystem.AddQueueWriter(jobHandle2);
			m_WaterPipeRoadConnectionGraphSystem.AddQueueWriter(jobHandle2);
			base.Dependency = jobHandle2;
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
	public InitializeSystem()
	{
	}
}

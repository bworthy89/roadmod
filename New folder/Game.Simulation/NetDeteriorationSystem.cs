#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class NetDeteriorationSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateLaneConditionJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<LaneDeteriorationData> m_LaneDeteriorationData;

		public ComponentTypeHandle<LaneCondition> m_LaneConditionType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			float num = 1f / (float)kUpdatesPerDay;
			NativeArray<LaneCondition> nativeArray = chunk.GetNativeArray(ref m_LaneConditionType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				LaneCondition value = nativeArray[i];
				PrefabRef prefabRef = nativeArray2[i];
				if (m_LaneDeteriorationData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
				{
					value.m_Wear = math.min(value.m_Wear + num * componentData.m_TimeFactor, 10f);
					nativeArray[i] = value;
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct UpdateNetConditionJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Native> m_NativeType;

		[ReadOnly]
		public BufferTypeHandle<Game.Net.SubLane> m_SubLaneType;

		[ReadOnly]
		public BufferTypeHandle<ConnectedEdge> m_ConnectedEdgeType;

		public ComponentTypeHandle<MaintenanceConsumer> m_MaintenanceConsumerType;

		public ComponentTypeHandle<NetCondition> m_NetConditionType;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Native> m_NativeData;

		[ReadOnly]
		public ComponentLookup<EdgeLane> m_EdgeLaneData;

		[ReadOnly]
		public ComponentLookup<MaintenanceRequest> m_MaintenanceRequestData;

		[NativeDisableContainerSafetyRestriction]
		[ReadOnly]
		public ComponentLookup<MaintenanceConsumer> m_MaintenanceConsumerData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<LaneCondition> m_LaneConditionData;

		[ReadOnly]
		public EntityArchetype m_MaintenanceRequestArchetype;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<NetCondition> nativeArray = chunk.GetNativeArray(ref m_NetConditionType);
			BufferAccessor<Game.Net.SubLane> bufferAccessor = chunk.GetBufferAccessor(ref m_SubLaneType);
			if (chunk.Has(ref m_NativeType))
			{
				for (int i = 0; i < nativeArray.Length; i++)
				{
					ref NetCondition reference = ref nativeArray.ElementAt(i);
					DynamicBuffer<Game.Net.SubLane> dynamicBuffer = bufferAccessor[i];
					reference.m_Wear = 0f;
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						Entity subLane = dynamicBuffer[j].m_SubLane;
						if (m_LaneConditionData.TryGetComponent(subLane, out var componentData))
						{
							componentData.m_Wear = 0f;
							m_LaneConditionData[subLane] = componentData;
						}
					}
				}
				return;
			}
			NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
			NativeArray<MaintenanceConsumer> nativeArray3 = chunk.GetNativeArray(ref m_MaintenanceConsumerType);
			BufferAccessor<ConnectedEdge> bufferAccessor2 = chunk.GetBufferAccessor(ref m_ConnectedEdgeType);
			for (int k = 0; k < nativeArray.Length; k++)
			{
				Entity entity = nativeArray2[k];
				ref NetCondition reference2 = ref nativeArray.ElementAt(k);
				DynamicBuffer<Game.Net.SubLane> dynamicBuffer2 = bufferAccessor[k];
				reference2.m_Wear = 0f;
				bool flag = false;
				if (CollectionUtils.TryGet(bufferAccessor2, k, out var value))
				{
					flag = true;
					for (int l = 0; l < value.Length; l++)
					{
						ConnectedEdge connectedEdge = value[l];
						if (m_EdgeData.TryGetComponent(connectedEdge.m_Edge, out var componentData2) && m_MaintenanceConsumerData.HasComponent(connectedEdge.m_Edge) && !m_NativeData.HasComponent(connectedEdge.m_Edge) && (componentData2.m_Start == entity || componentData2.m_End == entity))
						{
							flag = false;
							break;
						}
					}
				}
				if (flag)
				{
					for (int m = 0; m < dynamicBuffer2.Length; m++)
					{
						Entity subLane2 = dynamicBuffer2[m].m_SubLane;
						if (m_LaneConditionData.TryGetComponent(subLane2, out var componentData3))
						{
							componentData3.m_Wear = 0f;
							m_LaneConditionData[subLane2] = componentData3;
						}
					}
					continue;
				}
				for (int n = 0; n < dynamicBuffer2.Length; n++)
				{
					Entity subLane3 = dynamicBuffer2[n].m_SubLane;
					if (m_LaneConditionData.TryGetComponent(subLane3, out var componentData4))
					{
						if (m_EdgeLaneData.TryGetComponent(subLane3, out var componentData5))
						{
							reference2.m_Wear = math.select(reference2.m_Wear, componentData4.m_Wear, new bool2(math.any(componentData5.m_EdgeDelta == 0f), math.any(componentData5.m_EdgeDelta == 1f)) & (componentData4.m_Wear > reference2.m_Wear));
						}
						else
						{
							reference2.m_Wear = math.max(reference2.m_Wear, componentData4.m_Wear);
						}
					}
				}
				if (nativeArray3.Length != 0)
				{
					MaintenanceConsumer maintenanceConsumer = nativeArray3[k];
					RequestMaintenanceIfNeeded(unfilteredChunkIndex, entity, reference2, ref maintenanceConsumer);
					nativeArray3[k] = maintenanceConsumer;
				}
			}
		}

		private void RequestMaintenanceIfNeeded(int jobIndex, Entity entity, NetCondition condition, ref MaintenanceConsumer maintenanceConsumer)
		{
			int maintenancePriority = GetMaintenancePriority(condition);
			if (maintenancePriority > 0 && (!m_MaintenanceRequestData.TryGetComponent(maintenanceConsumer.m_Request, out var componentData) || (!(componentData.m_Target == entity) && componentData.m_DispatchIndex != maintenanceConsumer.m_DispatchIndex)))
			{
				maintenanceConsumer.m_Request = Entity.Null;
				maintenanceConsumer.m_DispatchIndex = 0;
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_MaintenanceRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new MaintenanceRequest(entity, maintenancePriority));
				m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(32u));
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
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<LaneDeteriorationData> __Game_Prefabs_LaneDeteriorationData_RO_ComponentLookup;

		public ComponentTypeHandle<LaneCondition> __Game_Net_LaneCondition_RW_ComponentTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Native> __Game_Common_Native_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferTypeHandle;

		public ComponentTypeHandle<MaintenanceConsumer> __Game_Simulation_MaintenanceConsumer_RW_ComponentTypeHandle;

		public ComponentTypeHandle<NetCondition> __Game_Net_NetCondition_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Native> __Game_Common_Native_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeLane> __Game_Net_EdgeLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MaintenanceRequest> __Game_Simulation_MaintenanceRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MaintenanceConsumer> __Game_Simulation_MaintenanceConsumer_RO_ComponentLookup;

		public ComponentLookup<LaneCondition> __Game_Net_LaneCondition_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_LaneDeteriorationData_RO_ComponentLookup = state.GetComponentLookup<LaneDeteriorationData>(isReadOnly: true);
			__Game_Net_LaneCondition_RW_ComponentTypeHandle = state.GetComponentTypeHandle<LaneCondition>();
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Native_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Native>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Net.SubLane>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferTypeHandle = state.GetBufferTypeHandle<ConnectedEdge>(isReadOnly: true);
			__Game_Simulation_MaintenanceConsumer_RW_ComponentTypeHandle = state.GetComponentTypeHandle<MaintenanceConsumer>();
			__Game_Net_NetCondition_RW_ComponentTypeHandle = state.GetComponentTypeHandle<NetCondition>();
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Common_Native_RO_ComponentLookup = state.GetComponentLookup<Native>(isReadOnly: true);
			__Game_Net_EdgeLane_RO_ComponentLookup = state.GetComponentLookup<EdgeLane>(isReadOnly: true);
			__Game_Simulation_MaintenanceRequest_RO_ComponentLookup = state.GetComponentLookup<MaintenanceRequest>(isReadOnly: true);
			__Game_Simulation_MaintenanceConsumer_RO_ComponentLookup = state.GetComponentLookup<MaintenanceConsumer>(isReadOnly: true);
			__Game_Net_LaneCondition_RW_ComponentLookup = state.GetComponentLookup<LaneCondition>();
		}
	}

	public static readonly int kUpdatesPerDay = 16;

	private SimulationSystem m_SimulationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_LaneQuery;

	private EntityQuery m_EdgeQuery;

	private EntityArchetype m_MaintenanceRequestArchetype;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / (kUpdatesPerDay * 16);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_LaneQuery = GetEntityQuery(ComponentType.ReadWrite<LaneCondition>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_EdgeQuery = GetEntityQuery(ComponentType.ReadWrite<NetCondition>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_MaintenanceRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<MaintenanceRequest>(), ComponentType.ReadWrite<RequestGroup>());
		RequireAnyForUpdate(m_LaneQuery, m_EdgeQuery);
		Assert.IsTrue((long)(262144 / kUpdatesPerDay) >= 512L);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_LaneQuery.ResetFilter();
		m_LaneQuery.SetSharedComponentFilter(new UpdateFrame(SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16)));
		m_EdgeQuery.ResetFilter();
		m_EdgeQuery.SetSharedComponentFilter(new UpdateFrame(SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16)));
		UpdateLaneConditionJob jobData = new UpdateLaneConditionJob
		{
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LaneDeteriorationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_LaneDeteriorationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneConditionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_LaneCondition_RW_ComponentTypeHandle, ref base.CheckedStateRef)
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new UpdateNetConditionJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_NativeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Native_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SubLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_SubLane_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_ConnectedEdgeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_MaintenanceConsumerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_MaintenanceConsumer_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NetConditionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_NetCondition_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NativeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Native_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MaintenanceRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_MaintenanceRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MaintenanceConsumerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_MaintenanceConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneConditionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LaneCondition_RW_ComponentLookup, ref base.CheckedStateRef),
			m_MaintenanceRequestArchetype = m_MaintenanceRequestArchetype,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, dependsOn: JobChunkExtensions.ScheduleParallel(jobData, m_LaneQuery, base.Dependency), query: m_EdgeQuery);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
		base.Dependency = jobHandle;
	}

	public static int GetMaintenancePriority(NetCondition condition)
	{
		return (int)(math.cmax(condition.m_Wear) / 10f * 100f) - 10;
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
	public NetDeteriorationSystem()
	{
	}
}

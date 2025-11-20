using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class ElectricityRoadConnectionGraphSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateRoadConnectionsJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<RoadConnectionUpdated> m_RoadConnectionUpdatedType;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> m_ElectricityConsumers;

		[ReadOnly]
		public ComponentLookup<Deleted> m_Deleted;

		public NativeQueue<Entity>.ParallelWriter m_UpdatedEdges;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<RoadConnectionUpdated> nativeArray = chunk.GetNativeArray(ref m_RoadConnectionUpdatedType);
			for (int i = 0; i < chunk.Count; i++)
			{
				RoadConnectionUpdated roadConnectionUpdated = nativeArray[i];
				if (m_ElectricityConsumers.HasComponent(roadConnectionUpdated.m_Building))
				{
					if (roadConnectionUpdated.m_Old != Entity.Null && !m_Deleted.HasComponent(roadConnectionUpdated.m_Old))
					{
						m_UpdatedEdges.Enqueue(roadConnectionUpdated.m_Old);
					}
					if (roadConnectionUpdated.m_New != Entity.Null)
					{
						m_UpdatedEdges.Enqueue(roadConnectionUpdated.m_New);
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
	private struct UpdateRoadEdgesJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> m_ElectricityConsumers;

		[ReadOnly]
		public ComponentLookup<ElectricityNodeConnection> m_NodeConnections;

		[ReadOnly]
		public ComponentLookup<ElectricityBuildingConnection> m_BuildingConnections;

		[ReadOnly]
		public BufferLookup<ConnectedBuilding> m_ConnectedBuildings;

		[ReadOnly]
		public BufferLookup<ConnectedFlowEdge> m_FlowConnections;

		public ComponentLookup<ElectricityFlowEdge> m_FlowEdges;

		public EntityCommandBuffer m_CommandBuffer;

		public NativeQueue<Entity> m_UpdatedEdges;

		public Entity m_SinkNode;

		public EntityArchetype m_EdgeArchetype;

		public void Execute()
		{
			NativeParallelHashSet<Entity> nativeParallelHashSet = new NativeParallelHashSet<Entity>(m_UpdatedEdges.Count, Allocator.Temp);
			Entity item;
			while (m_UpdatedEdges.TryDequeue(out item))
			{
				if (nativeParallelHashSet.Add(item) && m_NodeConnections.TryGetComponent(item, out var componentData))
				{
					if (HasConsumersWithoutBuildingSinkConnection(item, out var consumption))
					{
						CreateOrUpdateRoadEdgeSinkConnection(componentData.m_ElectricityNode, consumption);
					}
					else
					{
						ClearRoadEdgeSinkConnection(componentData.m_ElectricityNode);
					}
				}
			}
		}

		private bool HasConsumersWithoutBuildingSinkConnection(Entity roadEdge, out int consumption)
		{
			bool result = false;
			consumption = 0;
			if (m_ConnectedBuildings.TryGetBuffer(roadEdge, out var bufferData))
			{
				foreach (ConnectedBuilding item in bufferData)
				{
					if (m_ElectricityConsumers.TryGetComponent(item.m_Building, out var componentData) && !m_BuildingConnections.HasComponent(item.m_Building))
					{
						result = true;
						consumption += componentData.m_WantedConsumption;
					}
				}
			}
			return result;
		}

		private void CreateOrUpdateRoadEdgeSinkConnection(Entity roadEdgeFlowNode, int capacity)
		{
			if (!ElectricityGraphUtils.TrySetFlowEdge(roadEdgeFlowNode, m_SinkNode, FlowDirection.Forward, capacity, ref m_FlowConnections, ref m_FlowEdges))
			{
				ElectricityGraphUtils.CreateFlowEdge(m_CommandBuffer, m_EdgeArchetype, roadEdgeFlowNode, m_SinkNode, FlowDirection.Forward, capacity);
			}
		}

		private void ClearRoadEdgeSinkConnection(Entity roadEdgeFlowNode)
		{
			if (ElectricityGraphUtils.TryGetFlowEdge(roadEdgeFlowNode, m_SinkNode, ref m_FlowConnections, ref m_FlowEdges, out Entity entity))
			{
				m_CommandBuffer.AddComponent<Deleted>(entity);
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<RoadConnectionUpdated> __Game_Buildings_RoadConnectionUpdated_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> __Game_Buildings_ElectricityConsumer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityNodeConnection> __Game_Simulation_ElectricityNodeConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityBuildingConnection> __Game_Simulation_ElectricityBuildingConnection_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedBuilding> __Game_Buildings_ConnectedBuilding_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedFlowEdge> __Game_Simulation_ConnectedFlowEdge_RO_BufferLookup;

		public ComponentLookup<ElectricityFlowEdge> __Game_Simulation_ElectricityFlowEdge_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Buildings_RoadConnectionUpdated_RO_ComponentTypeHandle = state.GetComponentTypeHandle<RoadConnectionUpdated>(isReadOnly: true);
			__Game_Buildings_ElectricityConsumer_RO_ComponentLookup = state.GetComponentLookup<ElectricityConsumer>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Simulation_ElectricityNodeConnection_RO_ComponentLookup = state.GetComponentLookup<ElectricityNodeConnection>(isReadOnly: true);
			__Game_Simulation_ElectricityBuildingConnection_RO_ComponentLookup = state.GetComponentLookup<ElectricityBuildingConnection>(isReadOnly: true);
			__Game_Buildings_ConnectedBuilding_RO_BufferLookup = state.GetBufferLookup<ConnectedBuilding>(isReadOnly: true);
			__Game_Simulation_ConnectedFlowEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedFlowEdge>(isReadOnly: true);
			__Game_Simulation_ElectricityFlowEdge_RW_ComponentLookup = state.GetComponentLookup<ElectricityFlowEdge>();
		}
	}

	private ElectricityFlowSystem m_ElectricityFlowSystem;

	private ModificationBarrier5 m_ModificationBarrier;

	private EntityQuery m_EventQuery;

	private NativeQueue<Entity> m_UpdatedEdges;

	private JobHandle m_WriteDependencies;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ElectricityFlowSystem = base.World.GetOrCreateSystemManaged<ElectricityFlowSystem>();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier5>();
		m_EventQuery = GetEntityQuery(ComponentType.ReadOnly<RoadConnectionUpdated>());
		m_UpdatedEdges = new NativeQueue<Entity>(Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_UpdatedEdges.Dispose();
		base.OnDestroy();
	}

	public NativeQueue<Entity> GetEdgeUpdateQueue(out JobHandle deps)
	{
		deps = m_WriteDependencies;
		return m_UpdatedEdges;
	}

	public void AddQueueWriter(JobHandle handle)
	{
		m_WriteDependencies = JobHandle.CombineDependencies(m_WriteDependencies, handle);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.Dependency = JobHandle.CombineDependencies(base.Dependency, m_WriteDependencies);
		if (!m_EventQuery.IsEmptyIgnoreFilter)
		{
			UpdateRoadConnectionsJob jobData = new UpdateRoadConnectionsJob
			{
				m_RoadConnectionUpdatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_RoadConnectionUpdated_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ElectricityConsumers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Deleted = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
				m_UpdatedEdges = m_UpdatedEdges.AsParallelWriter()
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_EventQuery, base.Dependency);
		}
		UpdateRoadEdgesJob jobData2 = new UpdateRoadEdgesJob
		{
			m_ElectricityConsumers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NodeConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ElectricityNodeConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ElectricityBuildingConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectedBuildings = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_ConnectedBuilding_RO_BufferLookup, ref base.CheckedStateRef),
			m_FlowConnections = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ConnectedFlowEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_FlowEdges = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ElectricityFlowEdge_RW_ComponentLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer(),
			m_UpdatedEdges = m_UpdatedEdges,
			m_SinkNode = m_ElectricityFlowSystem.sinkNode,
			m_EdgeArchetype = m_ElectricityFlowSystem.edgeArchetype
		};
		base.Dependency = IJobExtensions.Schedule(jobData2, base.Dependency);
		m_ModificationBarrier.AddJobHandleForProducer(base.Dependency);
		m_WriteDependencies = base.Dependency;
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
	public ElectricityRoadConnectionGraphSystem()
	{
	}
}

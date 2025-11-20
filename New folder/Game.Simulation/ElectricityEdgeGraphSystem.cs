using System.Runtime.CompilerServices;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class ElectricityEdgeGraphSystem : GameSystemBase
{
	[BurstCompile]
	private struct CreateEdgeConnectionsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Edge> m_NetEdgeType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferLookup<ConnectedNode> m_ConnectedNetNodes;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedNetEdges;

		[ReadOnly]
		public BufferLookup<ConnectedFlowEdge> m_FlowConnections;

		[ReadOnly]
		public ComponentLookup<ElectricityFlowEdge> m_FlowEdges;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public ComponentLookup<ElectricityConnectionData> m_ElectricityConnectionDatas;

		[ReadOnly]
		public ComponentLookup<ElectricityNodeConnection> m_ElectricityNodeConnections;

		public EntityCommandBuffer m_CommandBuffer;

		public NativeParallelHashMap<Entity, Entity> m_NodeMap;

		public EntityArchetype m_NodeArchetype;

		public EntityArchetype m_EdgeArchetype;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Edge> nativeArray2 = chunk.GetNativeArray(ref m_NetEdgeType);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray[i];
				Entity start = nativeArray2[i].m_Start;
				Entity end = nativeArray2[i].m_End;
				BufferedEntity orCreateNetNodeConnection = GetOrCreateNetNodeConnection(start);
				BufferedEntity orCreateNetNodeConnection2 = GetOrCreateNetNodeConnection(end);
				Entity entity2 = m_CommandBuffer.CreateEntity(m_NodeArchetype);
				m_CommandBuffer.AddComponent(entity, new ElectricityNodeConnection
				{
					m_ElectricityNode = entity2
				});
				PrefabRef prefabRef = nativeArray3[i];
				ElectricityConnectionData connectionData = m_ElectricityConnectionDatas[prefabRef.m_Prefab];
				CreateFlowEdge(orCreateNetNodeConnection.m_Value, entity2, connectionData);
				CreateFlowEdge(entity2, orCreateNetNodeConnection2.m_Value, connectionData);
				CreateEdgeMiddleNodeConnections(entity, entity2);
				EnsureNodeEdgeConnections(start, orCreateNetNodeConnection, connectionData);
				EnsureNodeEdgeConnections(end, orCreateNetNodeConnection2, connectionData);
			}
		}

		private void CreateEdgeMiddleNodeConnections(Entity netEdge, Entity flowMiddleNode)
		{
			if (!m_ConnectedNetNodes.TryGetBuffer(netEdge, out var bufferData))
			{
				return;
			}
			foreach (ConnectedNode item in bufferData)
			{
				if (m_PrefabRefs.TryGetComponent(item.m_Node, out var componentData) && m_ElectricityConnectionDatas.TryGetComponent(componentData.m_Prefab, out var componentData2))
				{
					CreateFlowEdge(GetOrCreateNetNodeConnection(item.m_Node).m_Value, flowMiddleNode, componentData2);
				}
			}
		}

		private void EnsureNodeEdgeConnections(Entity netNode, BufferedEntity flowNode, ElectricityConnectionData connectionData)
		{
			foreach (ConnectedEdge item in m_ConnectedNetEdges[netNode])
			{
				if (m_ElectricityNodeConnections.TryGetComponent(item.m_Edge, out var componentData))
				{
					Entity electricityNode = componentData.m_ElectricityNode;
					if (!flowNode.m_Stored || !ElectricityGraphUtils.HasAnyFlowEdge(flowNode.m_Value, electricityNode, ref m_FlowConnections, ref m_FlowEdges))
					{
						CreateFlowEdge(flowNode.m_Value, componentData.m_ElectricityNode, connectionData);
					}
				}
			}
		}

		private BufferedEntity GetOrCreateNetNodeConnection(Entity netNode)
		{
			if (m_ElectricityNodeConnections.TryGetComponent(netNode, out var componentData))
			{
				return new BufferedEntity(componentData.m_ElectricityNode, stored: true);
			}
			if (m_NodeMap.TryGetValue(netNode, out var item))
			{
				return new BufferedEntity(item, stored: false);
			}
			item = m_CommandBuffer.CreateEntity(m_NodeArchetype);
			m_CommandBuffer.AddComponent(netNode, new ElectricityNodeConnection
			{
				m_ElectricityNode = item
			});
			m_NodeMap.Add(netNode, item);
			return new BufferedEntity(item, stored: false);
		}

		private void CreateFlowEdge(Entity startNode, Entity endNode, ElectricityConnectionData connectionData)
		{
			ElectricityGraphUtils.CreateFlowEdge(m_CommandBuffer, m_EdgeArchetype, startNode, endNode, connectionData.m_Direction, connectionData.m_Capacity);
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
		public ComponentTypeHandle<Edge> __Game_Net_Edge_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferLookup<ConnectedNode> __Game_Net_ConnectedNode_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedFlowEdge> __Game_Simulation_ConnectedFlowEdge_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityFlowEdge> __Game_Simulation_ElectricityFlowEdge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityConnectionData> __Game_Prefabs_ElectricityConnectionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityNodeConnection> __Game_Simulation_ElectricityNodeConnection_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Net_Edge_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Edge>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Net_ConnectedNode_RO_BufferLookup = state.GetBufferLookup<ConnectedNode>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Simulation_ConnectedFlowEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedFlowEdge>(isReadOnly: true);
			__Game_Simulation_ElectricityFlowEdge_RO_ComponentLookup = state.GetComponentLookup<ElectricityFlowEdge>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ElectricityConnectionData_RO_ComponentLookup = state.GetComponentLookup<ElectricityConnectionData>(isReadOnly: true);
			__Game_Simulation_ElectricityNodeConnection_RO_ComponentLookup = state.GetComponentLookup<ElectricityNodeConnection>(isReadOnly: true);
		}
	}

	private ElectricityFlowSystem m_ElectricityFlowSystem;

	private ModificationBarrier2B m_ModificationBarrier;

	private EntityQuery m_CreatedEdgeQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ElectricityFlowSystem = base.World.GetOrCreateSystemManaged<ElectricityFlowSystem>();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier2B>();
		m_CreatedEdgeQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Net.ElectricityConnection>(), ComponentType.ReadOnly<Edge>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Created>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_CreatedEdgeQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeParallelHashMap<Entity, Entity> nodeMap = new NativeParallelHashMap<Entity, Entity>(32, Allocator.TempJob);
		CreateEdgeConnectionsJob jobData = new CreateEdgeConnectionsJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_NetEdgeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ConnectedNetNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedNode_RO_BufferLookup, ref base.CheckedStateRef),
			m_ConnectedNetEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_FlowConnections = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ConnectedFlowEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_FlowEdges = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ElectricityFlowEdge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ElectricityConnectionDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ElectricityConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ElectricityNodeConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ElectricityNodeConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer(),
			m_NodeMap = nodeMap,
			m_NodeArchetype = m_ElectricityFlowSystem.nodeArchetype,
			m_EdgeArchetype = m_ElectricityFlowSystem.edgeArchetype
		};
		base.Dependency = JobChunkExtensions.Schedule(jobData, m_CreatedEdgeQuery, base.Dependency);
		nodeMap.Dispose(base.Dependency);
		m_ModificationBarrier.AddJobHandleForProducer(base.Dependency);
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
	public ElectricityEdgeGraphSystem()
	{
	}
}

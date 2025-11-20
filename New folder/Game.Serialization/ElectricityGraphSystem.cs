using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Net;
using Game.Prefabs;
using Game.Simulation;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Serialization;

[CompilerGenerated]
public class ElectricityGraphSystem : GameSystemBase
{
	[BurstCompile]
	private struct EdgeJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<Edge> m_NetEdgeType;

		[ReadOnly]
		public BufferTypeHandle<ConnectedNode> m_ConnectedNetNodeType;

		[ReadOnly]
		public ComponentTypeHandle<ElectricityNodeConnection> m_ElectricityNodeConnectionType;

		[ReadOnly]
		public BufferLookup<ConnectedFlowEdge> m_FlowConnections;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public ComponentLookup<ElectricityConnectionData> m_ElectricityConnectionDatas;

		[ReadOnly]
		public ComponentLookup<ElectricityNodeConnection> m_ElectricityNodeConnections;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<ElectricityFlowEdge> m_FlowEdges;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Edge> nativeArray3 = chunk.GetNativeArray(ref m_NetEdgeType);
			BufferAccessor<ConnectedNode> bufferAccessor = chunk.GetBufferAccessor(ref m_ConnectedNetNodeType);
			NativeArray<ElectricityNodeConnection> nativeArray4 = chunk.GetNativeArray(ref m_ElectricityNodeConnectionType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray[i];
				Entity start = nativeArray3[i].m_Start;
				Entity end = nativeArray3[i].m_End;
				Entity electricityNode = m_ElectricityNodeConnections[start].m_ElectricityNode;
				Entity electricityNode2 = m_ElectricityNodeConnections[end].m_ElectricityNode;
				Entity electricityNode3 = nativeArray4[i].m_ElectricityNode;
				PrefabRef prefabRef = nativeArray2[i];
				if (!m_ElectricityConnectionDatas.TryGetComponent(prefabRef.m_Prefab, out var componentData))
				{
					componentData.m_Capacity = 400000;
					componentData.m_Direction = FlowDirection.Both;
				}
				if ((1u & (UpdateFlowEdge(electricityNode, electricityNode3, componentData) ? 1u : 0u) & (UpdateFlowEdge(electricityNode3, electricityNode2, componentData) ? 1u : 0u)) == 0)
				{
					UnityEngine.Debug.LogWarning($"ElectricityFlowEdge for net edge {entity.Index} not found!");
				}
				if (bufferAccessor.Length != 0)
				{
					UpdateEdgeMiddleNodeConnections(bufferAccessor[i], electricityNode3);
				}
			}
		}

		private void UpdateEdgeMiddleNodeConnections(DynamicBuffer<ConnectedNode> connectedNodes, Entity flowMiddleNode)
		{
			foreach (ConnectedNode item in connectedNodes)
			{
				if (m_PrefabRefs.TryGetComponent(item.m_Node, out var componentData) && m_ElectricityConnectionDatas.TryGetComponent(componentData.m_Prefab, out var componentData2))
				{
					Entity electricityNode = m_ElectricityNodeConnections[item.m_Node].m_ElectricityNode;
					if (!UpdateFlowEdge(electricityNode, flowMiddleNode, componentData2))
					{
						UnityEngine.Debug.LogWarning($"ElectricityFlowEdge for connected node {item.m_Node.Index} not found!");
					}
				}
			}
		}

		private bool UpdateFlowEdge(Entity startNode, Entity endNode, ElectricityConnectionData connectionData)
		{
			if (!ElectricityGraphUtils.TrySetFlowEdge(startNode, endNode, connectionData.m_Direction, connectionData.m_Capacity, ref m_FlowConnections, ref m_FlowEdges))
			{
				if (ElectricityGraphUtils.TryGetFlowEdge(endNode, startNode, ref m_FlowConnections, ref m_FlowEdges, out var entity, out var edge))
				{
					ref Entity start = ref edge.m_Start;
					ref Entity end = ref edge.m_End;
					Entity end2 = edge.m_End;
					Entity start2 = edge.m_Start;
					start = end2;
					end = start2;
					edge.direction = connectionData.m_Direction;
					edge.m_Capacity = connectionData.m_Capacity;
					edge.m_Flow = -edge.m_Flow;
					m_FlowEdges[entity] = edge;
					return true;
				}
				return false;
			}
			return true;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct BuildingJob : IJobChunk
	{
		private struct MarkerNodeData
		{
			public Entity m_NetNode;

			public int m_Capacity;

			public FlowDirection m_Direction;
		}

		[ReadOnly]
		public BufferTypeHandle<Game.Net.SubNet> m_SubNetType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		[ReadOnly]
		public ComponentTypeHandle<ElectricityBuildingConnection> m_BuildingConnectionType;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> m_SubNets;

		[ReadOnly]
		public ComponentLookup<Node> m_NetNodes;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public ComponentLookup<ElectricityConnectionData> m_ElectricityConnectionDatas;

		[ReadOnly]
		public ComponentLookup<ElectricityNodeConnection> m_ElectricityNodeConnections;

		[ReadOnly]
		public ComponentLookup<ElectricityValveConnection> m_ElectricityValveConnections;

		[ReadOnly]
		public BufferLookup<ConnectedFlowEdge> m_FlowConnections;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<ElectricityFlowEdge> m_FlowEdges;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<Game.Net.SubNet> bufferAccessor = chunk.GetBufferAccessor(ref m_SubNetType);
			BufferAccessor<InstalledUpgrade> bufferAccessor2 = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			NativeArray<ElectricityBuildingConnection> nativeArray = chunk.GetNativeArray(ref m_BuildingConnectionType);
			NativeList<MarkerNodeData> result = new NativeList<MarkerNodeData>(Allocator.Temp);
			for (int i = 0; i < chunk.Count; i++)
			{
				result.Clear();
				if (bufferAccessor.Length != 0)
				{
					FindMarkerNodes(bufferAccessor[i], result);
				}
				if (bufferAccessor2.Length != 0)
				{
					FindMarkerNodes(bufferAccessor2[i], result);
				}
				foreach (MarkerNodeData item in result)
				{
					UpdateMarkerNode(item, nativeArray[i]);
				}
			}
		}

		private void FindMarkerNodes(DynamicBuffer<InstalledUpgrade> upgrades, NativeList<MarkerNodeData> result)
		{
			for (int i = 0; i < upgrades.Length; i++)
			{
				InstalledUpgrade installedUpgrade = upgrades[i];
				if (!BuildingUtils.CheckOption(installedUpgrade, BuildingOption.Inactive) && m_SubNets.TryGetBuffer(installedUpgrade.m_Upgrade, out var bufferData))
				{
					FindMarkerNodes(bufferData, result);
				}
			}
		}

		private void FindMarkerNodes(DynamicBuffer<Game.Net.SubNet> subNets, NativeList<MarkerNodeData> result)
		{
			for (int i = 0; i < subNets.Length; i++)
			{
				Entity subNet = subNets[i].m_SubNet;
				if (m_NetNodes.HasComponent(subNet) && m_ElectricityValveConnections.HasComponent(subNet) && m_PrefabRefs.TryGetComponent(subNet, out var componentData) && m_ElectricityConnectionDatas.TryGetComponent(componentData.m_Prefab, out var componentData2))
				{
					MarkerNodeData value = new MarkerNodeData
					{
						m_NetNode = subNet,
						m_Capacity = componentData2.m_Capacity,
						m_Direction = componentData2.m_Direction
					};
					result.Add(in value);
				}
			}
		}

		private void UpdateMarkerNode(MarkerNodeData markerNodeData, ElectricityBuildingConnection buildingNodes)
		{
			Entity electricityNode = m_ElectricityNodeConnections[markerNodeData.m_NetNode].m_ElectricityNode;
			Entity valveNode = m_ElectricityValveConnections[markerNodeData.m_NetNode].m_ValveNode;
			UpdateFlowEdge(valveNode, electricityNode, markerNodeData.m_Direction, markerNodeData.m_Capacity);
			if (buildingNodes.m_TransformerNode != Entity.Null)
			{
				UpdateFlowEdge(buildingNodes.m_TransformerNode, valveNode, markerNodeData.m_Direction, markerNodeData.m_Capacity);
			}
			if (buildingNodes.m_ProducerEdge != Entity.Null)
			{
				UpdateFlowEdge(buildingNodes.GetProducerNode(ref m_FlowEdges), valveNode, FlowDirection.Forward, markerNodeData.m_Capacity);
			}
			if (buildingNodes.m_ConsumerEdge != Entity.Null)
			{
				UpdateFlowEdge(valveNode, buildingNodes.GetConsumerNode(ref m_FlowEdges), FlowDirection.Forward, markerNodeData.m_Capacity);
			}
			if (buildingNodes.m_ChargeEdge != Entity.Null)
			{
				UpdateFlowEdge(valveNode, buildingNodes.GetChargeNode(ref m_FlowEdges), FlowDirection.None, markerNodeData.m_Capacity);
			}
			if (buildingNodes.m_DischargeEdge != Entity.Null)
			{
				UpdateFlowEdge(buildingNodes.GetDischargeNode(ref m_FlowEdges), valveNode, FlowDirection.None, markerNodeData.m_Capacity);
			}
		}

		private void UpdateFlowEdge(Entity startNode, Entity endNode, FlowDirection direction, int capacity)
		{
			if (!ElectricityGraphUtils.TrySetFlowEdge(startNode, endNode, direction, capacity, ref m_FlowConnections, ref m_FlowEdges))
			{
				UnityEngine.Debug.LogWarning($"ElectricityFlowEdge from {startNode} to {endNode} not found!");
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
		public ComponentTypeHandle<Edge> __Game_Net_Edge_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<ConnectedNode> __Game_Net_ConnectedNode_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ElectricityNodeConnection> __Game_Simulation_ElectricityNodeConnection_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferLookup<ConnectedFlowEdge> __Game_Simulation_ConnectedFlowEdge_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityConnectionData> __Game_Prefabs_ElectricityConnectionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityNodeConnection> __Game_Simulation_ElectricityNodeConnection_RO_ComponentLookup;

		public ComponentLookup<ElectricityFlowEdge> __Game_Simulation_ElectricityFlowEdge_RW_ComponentLookup;

		[ReadOnly]
		public BufferTypeHandle<Game.Net.SubNet> __Game_Net_SubNet_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ElectricityBuildingConnection> __Game_Simulation_ElectricityBuildingConnection_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> __Game_Net_SubNet_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Node> __Game_Net_Node_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityValveConnection> __Game_Simulation_ElectricityValveConnection_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Edge>(isReadOnly: true);
			__Game_Net_ConnectedNode_RO_BufferTypeHandle = state.GetBufferTypeHandle<ConnectedNode>(isReadOnly: true);
			__Game_Simulation_ElectricityNodeConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ElectricityNodeConnection>(isReadOnly: true);
			__Game_Simulation_ConnectedFlowEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedFlowEdge>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ElectricityConnectionData_RO_ComponentLookup = state.GetComponentLookup<ElectricityConnectionData>(isReadOnly: true);
			__Game_Simulation_ElectricityNodeConnection_RO_ComponentLookup = state.GetComponentLookup<ElectricityNodeConnection>(isReadOnly: true);
			__Game_Simulation_ElectricityFlowEdge_RW_ComponentLookup = state.GetComponentLookup<ElectricityFlowEdge>();
			__Game_Net_SubNet_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Net.SubNet>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Simulation_ElectricityBuildingConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ElectricityBuildingConnection>(isReadOnly: true);
			__Game_Net_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubNet>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Node>(isReadOnly: true);
			__Game_Simulation_ElectricityValveConnection_RO_ComponentLookup = state.GetComponentLookup<ElectricityValveConnection>(isReadOnly: true);
		}
	}

	private EntityQuery m_NetEdgeQuery;

	private EntityQuery m_BuildingQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_NetEdgeQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Net.ElectricityConnection>(), ComponentType.ReadOnly<ElectricityNodeConnection>(), ComponentType.ReadOnly<Edge>(), ComponentType.ReadOnly<PrefabRef>());
		m_BuildingQuery = GetEntityQuery(ComponentType.ReadOnly<ElectricityBuildingConnection>(), ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<PrefabRef>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		EdgeJob jobData = new EdgeJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NetEdgeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ConnectedNetNodeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_ConnectedNode_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_ElectricityNodeConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_ElectricityNodeConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_FlowConnections = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ConnectedFlowEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ElectricityConnectionDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ElectricityConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ElectricityNodeConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ElectricityNodeConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_FlowEdges = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ElectricityFlowEdge_RW_ComponentLookup, ref base.CheckedStateRef)
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_NetEdgeQuery, base.Dependency);
		BuildingJob jobData2 = new BuildingJob
		{
			m_SubNetType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_SubNet_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_BuildingConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_ElectricityBuildingConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
			m_NetNodes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ElectricityConnectionDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ElectricityConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ElectricityNodeConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ElectricityNodeConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ElectricityValveConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ElectricityValveConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_FlowConnections = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ConnectedFlowEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_FlowEdges = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ElectricityFlowEdge_RW_ComponentLookup, ref base.CheckedStateRef)
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData2, m_BuildingQuery, base.Dependency);
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
	public ElectricityGraphSystem()
	{
	}
}

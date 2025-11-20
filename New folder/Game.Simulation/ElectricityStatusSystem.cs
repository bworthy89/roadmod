using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Notifications;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class ElectricityStatusSystem : GameSystemBase
{
	[BurstCompile]
	private struct NetEdgeNotificationJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<ElectricityNodeConnection> m_ElectricityNodeConnectionType;

		[ReadOnly]
		public BufferLookup<ConnectedFlowEdge> m_ConnectedFlowEdges;

		[ReadOnly]
		public ComponentLookup<ElectricityFlowEdge> m_FlowEdges;

		public Entity m_SourceNode;

		public Entity m_SinkNode;

		public ElectricityParameterData m_ElectricityParameters;

		public IconCommandBuffer m_IconCommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<ElectricityNodeConnection> nativeArray2 = chunk.GetNativeArray(ref m_ElectricityNodeConnectionType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity owner = nativeArray[i];
				Entity electricityNode = nativeArray2[i].m_ElectricityNode;
				bool flag = false;
				DynamicBuffer<ConnectedFlowEdge> dynamicBuffer = m_ConnectedFlowEdges[electricityNode];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					ElectricityFlowEdge electricityFlowEdge = m_FlowEdges[dynamicBuffer[j].m_Edge];
					if (electricityFlowEdge.isBottleneck && electricityFlowEdge.m_Start != m_SourceNode && electricityFlowEdge.m_End != m_SinkNode)
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					m_IconCommandBuffer.Add(owner, m_ElectricityParameters.m_BottleneckNotificationPrefab, IconPriority.Problem);
				}
				else
				{
					m_IconCommandBuffer.Remove(owner, m_ElectricityParameters.m_BottleneckNotificationPrefab);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct BuildingNotificationJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<ElectricityBuildingConnection> m_ElectricityBuildingConnectionType;

		[ReadOnly]
		public BufferLookup<ConnectedFlowEdge> m_ConnectedFlowEdges;

		[ReadOnly]
		public ComponentLookup<ElectricityFlowEdge> m_FlowEdges;

		public Entity m_SourceNode;

		public ElectricityParameterData m_ElectricityParameters;

		public IconCommandBuffer m_IconCommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<ElectricityBuildingConnection> nativeArray2 = chunk.GetNativeArray(ref m_ElectricityBuildingConnectionType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity netEntity = nativeArray[i];
				ElectricityBuildingConnection electricityBuildingConnection = nativeArray2[i];
				bool flag = false;
				if (electricityBuildingConnection.m_TransformerNode != Entity.Null && m_ConnectedFlowEdges.TryGetBuffer(electricityBuildingConnection.m_TransformerNode, out var bufferData))
				{
					for (int j = 0; j < bufferData.Length; j++)
					{
						if (m_FlowEdges[bufferData[j].m_Edge].isBottleneck)
						{
							flag = true;
							break;
						}
					}
				}
				bool flag2 = false;
				if (electricityBuildingConnection.m_ProducerEdge != Entity.Null && m_FlowEdges.TryGetComponent(electricityBuildingConnection.m_ProducerEdge, out var componentData))
				{
					flag2 = componentData.isBottleneck;
				}
				bool enabled = false;
				if (electricityBuildingConnection.m_ConsumerEdge != Entity.Null && m_FlowEdges.TryGetComponent(electricityBuildingConnection.m_ConsumerEdge, out var componentData2))
				{
					enabled = componentData2.isBeyondBottleneck;
				}
				SetProblemNotification(netEntity, m_ElectricityParameters.m_TransformerNotificationPrefab, !flag2 && flag);
				SetProblemNotification(netEntity, m_ElectricityParameters.m_NotEnoughProductionNotificationPrefab, flag2);
				SetProblemNotification(netEntity, m_ElectricityParameters.m_BottleneckNotificationPrefab, enabled);
			}
		}

		private void SetProblemNotification(Entity netEntity, Entity prefab, bool enabled)
		{
			if (enabled)
			{
				m_IconCommandBuffer.Add(netEntity, prefab, IconPriority.Problem);
			}
			else
			{
				m_IconCommandBuffer.Remove(netEntity, prefab);
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
		public ComponentTypeHandle<ElectricityNodeConnection> __Game_Simulation_ElectricityNodeConnection_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferLookup<ConnectedFlowEdge> __Game_Simulation_ConnectedFlowEdge_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityFlowEdge> __Game_Simulation_ElectricityFlowEdge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<ElectricityBuildingConnection> __Game_Simulation_ElectricityBuildingConnection_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Simulation_ElectricityNodeConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ElectricityNodeConnection>(isReadOnly: true);
			__Game_Simulation_ConnectedFlowEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedFlowEdge>(isReadOnly: true);
			__Game_Simulation_ElectricityFlowEdge_RO_ComponentLookup = state.GetComponentLookup<ElectricityFlowEdge>(isReadOnly: true);
			__Game_Simulation_ElectricityBuildingConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ElectricityBuildingConnection>(isReadOnly: true);
		}
	}

	private ElectricityFlowSystem m_ElectricityFlowSystem;

	private IconCommandSystem m_IconCommandSystem;

	private EntityQuery m_EdgeQuery;

	private EntityQuery m_BuildingQuery;

	private EntityQuery m_ElectricityParameterQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 128;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 127;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ElectricityFlowSystem = base.World.GetOrCreateSystemManaged<ElectricityFlowSystem>();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_EdgeQuery = GetEntityQuery(ComponentType.ReadOnly<Edge>(), ComponentType.ReadOnly<ElectricityNodeConnection>(), ComponentType.Exclude<Temp>());
		m_BuildingQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<ElectricityBuildingConnection>(), ComponentType.ReadOnly<Transform>(), ComponentType.ReadOnly<Game.Net.SubNet>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_ElectricityParameterQuery = GetEntityQuery(ComponentType.ReadOnly<ElectricityParameterData>());
		RequireAnyForUpdate(m_EdgeQuery, m_BuildingQuery);
		RequireForUpdate(m_ElectricityParameterQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		IconCommandBuffer iconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer();
		ElectricityParameterData singleton = m_ElectricityParameterQuery.GetSingleton<ElectricityParameterData>();
		JobHandle dependsOn = JobChunkExtensions.ScheduleParallel(new NetEdgeNotificationJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_ElectricityNodeConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_ElectricityNodeConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ConnectedFlowEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ConnectedFlowEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_FlowEdges = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ElectricityFlowEdge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SourceNode = m_ElectricityFlowSystem.sourceNode,
			m_SinkNode = m_ElectricityFlowSystem.sinkNode,
			m_ElectricityParameters = singleton,
			m_IconCommandBuffer = iconCommandBuffer
		}, m_EdgeQuery, base.Dependency);
		JobHandle dependency = JobChunkExtensions.ScheduleParallel(new BuildingNotificationJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_ElectricityBuildingConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_ElectricityBuildingConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ConnectedFlowEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ConnectedFlowEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_FlowEdges = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ElectricityFlowEdge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SourceNode = m_ElectricityFlowSystem.sourceNode,
			m_ElectricityParameters = singleton,
			m_IconCommandBuffer = iconCommandBuffer
		}, m_BuildingQuery, dependsOn);
		base.Dependency = dependency;
		m_IconCommandSystem.AddCommandBufferWriter(base.Dependency);
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
	public ElectricityStatusSystem()
	{
	}
}

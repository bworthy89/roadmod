using System.Runtime.CompilerServices;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class ElectricityOutsideConnectionGraphSystem : GameSystemBase
{
	[BurstCompile]
	private struct CreateOutsideConnectionsJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentLookup<ElectricityNodeConnection> m_ElectricityNodeConnections;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public EntityArchetype m_EdgeArchetype;

		public Entity m_SourceNode;

		public Entity m_SinkNode;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Owner> nativeArray = chunk.GetNativeArray(ref m_OwnerType);
			for (int i = 0; i < chunk.Count; i++)
			{
				if (m_ElectricityNodeConnections.TryGetComponent(nativeArray[i].m_Owner, out var componentData))
				{
					m_CommandBuffer.AddComponent<TradeNode>(unfilteredChunkIndex, componentData.m_ElectricityNode);
					CreateOutsideFlowEdge(unfilteredChunkIndex, m_SourceNode, componentData.m_ElectricityNode);
					CreateOutsideFlowEdge(unfilteredChunkIndex, componentData.m_ElectricityNode, m_SinkNode);
				}
			}
		}

		private void CreateOutsideFlowEdge(int jobIndex, Entity startNode, Entity endNode)
		{
			ElectricityGraphUtils.CreateFlowEdge(m_CommandBuffer, jobIndex, m_EdgeArchetype, startNode, endNode, FlowDirection.None, 1073741823);
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<ElectricityNodeConnection> __Game_Simulation_ElectricityNodeConnection_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Simulation_ElectricityNodeConnection_RO_ComponentLookup = state.GetComponentLookup<ElectricityNodeConnection>(isReadOnly: true);
		}
	}

	private ElectricityFlowSystem m_ElectricityFlowSystem;

	private ModificationBarrier3 m_ModificationBarrier;

	private EntityQuery m_CreatedConnectionQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ElectricityFlowSystem = base.World.GetOrCreateSystemManaged<ElectricityFlowSystem>();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier3>();
		m_CreatedConnectionQuery = GetEntityQuery(ComponentType.ReadOnly<ElectricityOutsideConnection>(), ComponentType.ReadOnly<Owner>(), ComponentType.ReadOnly<Created>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_CreatedConnectionQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		CreateOutsideConnectionsJob jobData = new CreateOutsideConnectionsJob
		{
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ElectricityNodeConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ElectricityNodeConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_EdgeArchetype = m_ElectricityFlowSystem.edgeArchetype,
			m_SourceNode = m_ElectricityFlowSystem.sourceNode,
			m_SinkNode = m_ElectricityFlowSystem.sinkNode
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_CreatedConnectionQuery, base.Dependency);
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
	public ElectricityOutsideConnectionGraphSystem()
	{
	}
}

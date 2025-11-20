using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Simulation;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Serialization;

[CompilerGenerated]
public class ConnectedFlowEdgeSystem : GameSystemBase
{
	[BurstCompile]
	public struct ConnectedFlowEdgeJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<ElectricityFlowEdge> m_ElectricityFlowEdgeType;

		[ReadOnly]
		public ComponentTypeHandle<WaterPipeEdge> m_WaterPipeEdgeType;

		public BufferLookup<ConnectedFlowEdge> m_ConnectedFlowEdges;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<ElectricityFlowEdge> nativeArray2 = chunk.GetNativeArray(ref m_ElectricityFlowEdgeType);
			NativeArray<WaterPipeEdge> nativeArray3 = chunk.GetNativeArray(ref m_WaterPipeEdgeType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity edge = nativeArray[i];
				ElectricityFlowEdge electricityFlowEdge = nativeArray2[i];
				DynamicBuffer<ConnectedFlowEdge> buffer = m_ConnectedFlowEdges[electricityFlowEdge.m_Start];
				DynamicBuffer<ConnectedFlowEdge> buffer2 = m_ConnectedFlowEdges[electricityFlowEdge.m_End];
				CollectionUtils.TryAddUniqueValue(buffer, new ConnectedFlowEdge(edge));
				CollectionUtils.TryAddUniqueValue(buffer2, new ConnectedFlowEdge(edge));
			}
			for (int j = 0; j < nativeArray3.Length; j++)
			{
				Entity edge2 = nativeArray[j];
				WaterPipeEdge waterPipeEdge = nativeArray3[j];
				DynamicBuffer<ConnectedFlowEdge> buffer3 = m_ConnectedFlowEdges[waterPipeEdge.m_Start];
				DynamicBuffer<ConnectedFlowEdge> buffer4 = m_ConnectedFlowEdges[waterPipeEdge.m_End];
				CollectionUtils.TryAddUniqueValue(buffer3, new ConnectedFlowEdge(edge2));
				CollectionUtils.TryAddUniqueValue(buffer4, new ConnectedFlowEdge(edge2));
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
		public ComponentTypeHandle<ElectricityFlowEdge> __Game_Simulation_ElectricityFlowEdge_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WaterPipeEdge> __Game_Simulation_WaterPipeEdge_RO_ComponentTypeHandle;

		public BufferLookup<ConnectedFlowEdge> __Game_Simulation_ConnectedFlowEdge_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Simulation_ElectricityFlowEdge_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ElectricityFlowEdge>(isReadOnly: true);
			__Game_Simulation_WaterPipeEdge_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WaterPipeEdge>(isReadOnly: true);
			__Game_Simulation_ConnectedFlowEdge_RW_BufferLookup = state.GetBufferLookup<ConnectedFlowEdge>();
		}
	}

	private EntityQuery m_Query;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_Query = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<ElectricityFlowEdge>(),
				ComponentType.ReadOnly<WaterPipeEdge>()
			}
		});
		RequireForUpdate(m_Query);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		ConnectedFlowEdgeJob jobData = new ConnectedFlowEdgeJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_ElectricityFlowEdgeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_ElectricityFlowEdge_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WaterPipeEdgeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_WaterPipeEdge_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ConnectedFlowEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ConnectedFlowEdge_RW_BufferLookup, ref base.CheckedStateRef)
		};
		base.Dependency = JobChunkExtensions.Schedule(jobData, m_Query, base.Dependency);
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
	public ConnectedFlowEdgeSystem()
	{
	}
}

using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Common;
using Game.Net;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class NetLaneReservationSystem : GameSystemBase
{
	[BurstCompile]
	private struct ResetLaneReservationsJob : IJobChunk
	{
		public ComponentTypeHandle<LaneReservation> m_LaneReservationType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<LaneReservation> nativeArray = chunk.GetNativeArray(ref m_LaneReservationType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				ref LaneReservation reference = ref nativeArray.ElementAt(i);
				if (reference.m_Next.m_Priority < reference.m_Prev.m_Priority)
				{
					reference.m_Blocker = Entity.Null;
				}
				reference.m_Prev = reference.m_Next;
				reference.m_Next = default(ReservationData);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		public ComponentTypeHandle<LaneReservation> __Game_Net_LaneReservation_RW_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Net_LaneReservation_RW_ComponentTypeHandle = state.GetComponentTypeHandle<LaneReservation>();
		}
	}

	private SimulationSystem m_SimulationSystem;

	private EntityQuery m_LaneQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_LaneQuery = GetEntityQuery(ComponentType.ReadWrite<LaneReservation>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_LaneQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint index = m_SimulationSystem.frameIndex % 16;
		m_LaneQuery.ResetFilter();
		m_LaneQuery.SetSharedComponentFilter(new UpdateFrame(index));
		ResetLaneReservationsJob jobData = new ResetLaneReservationsJob
		{
			m_LaneReservationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_LaneReservation_RW_ComponentTypeHandle, ref base.CheckedStateRef)
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_LaneQuery, base.Dependency);
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
	public NetLaneReservationSystem()
	{
	}
}

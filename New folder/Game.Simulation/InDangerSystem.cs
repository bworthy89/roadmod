#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Game.Common;
using Game.Events;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Assertions;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class InDangerSystem : GameSystemBase
{
	[BurstCompile]
	private struct InDangerJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public ComponentTypeHandle<InDanger> m_InDangerType;

		[ReadOnly]
		public ComponentLookup<Duration> m_DurationData;

		[ReadOnly]
		public ComponentLookup<EvacuationRequest> m_EvacuationRequestData;

		[ReadOnly]
		public uint m_SimulationFrame;

		[ReadOnly]
		public EntityArchetype m_EvacuationRequestArchetype;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<InDanger> nativeArray2 = chunk.GetNativeArray(ref m_InDangerType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity entity = nativeArray[i];
				InDanger inDanger = nativeArray2[i];
				if (!IsStillInDanger(ref inDanger))
				{
					inDanger.m_Flags = (DangerFlags)0u;
					m_CommandBuffer.RemoveComponent<InDanger>(unfilteredChunkIndex, entity);
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, default(EffectsUpdated));
				}
				if ((inDanger.m_Flags & (DangerFlags.Evacuate | DangerFlags.UseTransport | DangerFlags.WaitingCitizens)) == (DangerFlags.Evacuate | DangerFlags.UseTransport | DangerFlags.WaitingCitizens))
				{
					RequestEvacuationIfNeeded(unfilteredChunkIndex, entity, ref inDanger);
				}
				nativeArray2[i] = inDanger;
			}
		}

		private bool IsStillInDanger(ref InDanger inDanger)
		{
			if (m_SimulationFrame >= inDanger.m_EndFrame)
			{
				return false;
			}
			if (!m_DurationData.HasComponent(inDanger.m_Event))
			{
				return false;
			}
			Duration duration = m_DurationData[inDanger.m_Event];
			return m_SimulationFrame < duration.m_EndFrame;
		}

		private void RequestEvacuationIfNeeded(int jobIndex, Entity entity, ref InDanger inDanger)
		{
			if (!m_EvacuationRequestData.HasComponent(inDanger.m_EvacuationRequest))
			{
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_EvacuationRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new EvacuationRequest(entity, 1f));
				m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(4u));
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

		public ComponentTypeHandle<InDanger> __Game_Events_InDanger_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Duration> __Game_Events_Duration_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EvacuationRequest> __Game_Simulation_EvacuationRequest_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Events_InDanger_RW_ComponentTypeHandle = state.GetComponentTypeHandle<InDanger>();
			__Game_Events_Duration_RO_ComponentLookup = state.GetComponentLookup<Duration>(isReadOnly: true);
			__Game_Simulation_EvacuationRequest_RO_ComponentLookup = state.GetComponentLookup<EvacuationRequest>(isReadOnly: true);
		}
	}

	public const uint UPDATE_INTERVAL = 64u;

	private SimulationSystem m_SimulationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_InDangerQuery;

	private EntityArchetype m_EvacuationRequestArchetype;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 64;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_InDangerQuery = GetEntityQuery(ComponentType.ReadWrite<InDanger>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_EvacuationRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<EvacuationRequest>(), ComponentType.ReadWrite<RequestGroup>());
		RequireForUpdate(m_InDangerQuery);
		Assert.IsTrue(condition: true);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new InDangerJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_InDangerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_InDanger_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DurationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_Duration_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EvacuationRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_EvacuationRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_EvacuationRequestArchetype = m_EvacuationRequestArchetype,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_InDangerQuery, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
		base.Dependency = jobHandle;
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
	public InDangerSystem()
	{
	}
}

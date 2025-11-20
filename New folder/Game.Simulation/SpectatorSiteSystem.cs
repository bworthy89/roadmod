using System.Runtime.CompilerServices;
using Game.Common;
using Game.Events;
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
public class SpectatorSiteSystem : GameSystemBase
{
	[BurstCompile]
	private struct SpectatorSiteJob : IJobChunk
	{
		[ReadOnly]
		public uint m_SimulationFrame;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public ComponentTypeHandle<SpectatorSite> m_SpectatorSiteType;

		[ReadOnly]
		public ComponentLookup<Duration> m_DurationData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<SpectatorEventData> m_SpectatorEventData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<SpectatorSite> nativeArray2 = chunk.GetNativeArray(ref m_SpectatorSiteType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity e = nativeArray[i];
				SpectatorSite value = nativeArray2[i];
				uint num = m_SimulationFrame;
				if (m_DurationData.HasComponent(value.m_Event))
				{
					Duration duration = m_DurationData[value.m_Event];
					PrefabRef prefabRef = m_PrefabRefData[value.m_Event];
					SpectatorEventData spectatorEventData = m_SpectatorEventData[prefabRef.m_Prefab];
					num = duration.m_EndFrame + (uint)(262144f * spectatorEventData.m_TerminationDuration);
				}
				if (m_SimulationFrame >= num)
				{
					m_CommandBuffer.RemoveComponent<SpectatorSite>(unfilteredChunkIndex, e);
				}
				nativeArray2[i] = value;
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

		public ComponentTypeHandle<SpectatorSite> __Game_Events_SpectatorSite_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Duration> __Game_Events_Duration_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpectatorEventData> __Game_Prefabs_SpectatorEventData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Events_SpectatorSite_RW_ComponentTypeHandle = state.GetComponentTypeHandle<SpectatorSite>();
			__Game_Events_Duration_RO_ComponentLookup = state.GetComponentLookup<Duration>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_SpectatorEventData_RO_ComponentLookup = state.GetComponentLookup<SpectatorEventData>(isReadOnly: true);
		}
	}

	private const uint UPDATE_INTERVAL = 64u;

	private SimulationSystem m_SimulationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_SiteQuery;

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
		m_SiteQuery = GetEntityQuery(ComponentType.ReadWrite<SpectatorSite>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_SiteQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		SpectatorSiteJob jobData = new SpectatorSiteJob
		{
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_SpectatorSiteType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_SpectatorSite_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DurationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_Duration_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpectatorEventData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpectatorEventData_RO_ComponentLookup, ref base.CheckedStateRef)
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_SiteQuery, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
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
	public SpectatorSiteSystem()
	{
	}
}

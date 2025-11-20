using System.Runtime.CompilerServices;
using Game.Common;
using Game.Economy;
using Game.Simulation;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Prefabs;

[CompilerGenerated]
public class ProcessingRequirementSystem : GameSystemBase
{
	[BurstCompile]
	private struct ProcessingRequirementJob : IJobChunk
	{
		[ReadOnly]
		public NativeArray<long> m_ProducedResources;

		[ReadOnly]
		public EntityArchetype m_UnlockEventArchetype;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<ProcessingRequirementData> m_ProcessingRequirementType;

		public ComponentTypeHandle<UnlockRequirementData> m_UnlockRequirementType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<ProcessingRequirementData> nativeArray2 = chunk.GetNativeArray(ref m_ProcessingRequirementType);
			NativeArray<UnlockRequirementData> nativeArray3 = chunk.GetNativeArray(ref m_UnlockRequirementType);
			ChunkEntityEnumerator chunkEntityEnumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
			int nextIndex;
			while (chunkEntityEnumerator.NextEntityIndex(out nextIndex))
			{
				ProcessingRequirementData processingRequirement = nativeArray2[nextIndex];
				UnlockRequirementData unlockRequirement = nativeArray3[nextIndex];
				if (ShouldUnlock(processingRequirement, ref unlockRequirement))
				{
					Entity e = m_CommandBuffer.CreateEntity(unfilteredChunkIndex, m_UnlockEventArchetype);
					m_CommandBuffer.SetComponent(unfilteredChunkIndex, e, new Unlock(nativeArray[nextIndex]));
				}
				nativeArray3[nextIndex] = unlockRequirement;
			}
		}

		private bool ShouldUnlock(ProcessingRequirementData processingRequirement, ref UnlockRequirementData unlockRequirement)
		{
			long num = ((processingRequirement.m_ResourceType == Resource.NoResource) ? 0 : m_ProducedResources[EconomyUtils.GetResourceIndex(processingRequirement.m_ResourceType)]);
			if (num >= processingRequirement.m_MinimumProducedAmount)
			{
				unlockRequirement.m_Progress = processingRequirement.m_MinimumProducedAmount;
				return true;
			}
			unlockRequirement.m_Progress = (int)num;
			return false;
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
		public ComponentTypeHandle<ProcessingRequirementData> __Game_Prefabs_ProcessingRequirementData_RO_ComponentTypeHandle;

		public ComponentTypeHandle<UnlockRequirementData> __Game_Prefabs_UnlockRequirementData_RW_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_ProcessingRequirementData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ProcessingRequirementData>(isReadOnly: true);
			__Game_Prefabs_UnlockRequirementData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<UnlockRequirementData>();
		}
	}

	private EndFrameBarrier m_EndFrameBarrier;

	private ProcessingCompanySystem m_ProcessingCompanySystem;

	private EntityQuery m_RequirementQuery;

	private EntityArchetype m_UnlockEventArchetype;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 128;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_ProcessingCompanySystem = base.World.GetOrCreateSystemManaged<ProcessingCompanySystem>();
		m_RequirementQuery = GetEntityQuery(ComponentType.ReadOnly<ProcessingRequirementData>(), ComponentType.ReadWrite<UnlockRequirementData>(), ComponentType.ReadOnly<Locked>());
		m_UnlockEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<Unlock>());
		RequireForUpdate(m_RequirementQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependencies;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new ProcessingRequirementJob
		{
			m_ProducedResources = m_ProcessingCompanySystem.GetProducedResourcesArray(out dependencies),
			m_UnlockEventArchetype = m_UnlockEventArchetype,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_ProcessingRequirementType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ProcessingRequirementData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UnlockRequirementType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_UnlockRequirementData_RW_ComponentTypeHandle, ref base.CheckedStateRef)
		}, m_RequirementQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
		m_ProcessingCompanySystem.AddProducedResourcesReader(jobHandle);
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
	public ProcessingRequirementSystem()
	{
	}
}

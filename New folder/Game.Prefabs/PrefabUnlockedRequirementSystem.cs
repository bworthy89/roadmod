using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Common;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Prefabs;

[CompilerGenerated]
public class PrefabUnlockedRequirementSystem : GameSystemBase
{
	[BurstCompile]
	private struct UnlockJob : IJobChunk
	{
		public EntityArchetype m_UnlockEventArchetype;

		public EntityCommandBuffer.ParallelWriter m_Buffer;

		[ReadOnly]
		public NativeList<Entity> m_PrefabUnlockedRequirementEntities;

		[ReadOnly]
		public ComponentTypeHandle<Unlock> m_UnlockTypeHandle;

		[ReadOnly]
		public ComponentLookup<Locked> m_LockedDataFromEntity;

		[ReadOnly]
		public BufferLookup<PrefabUnlockedRequirement> m_PrefabUnlockedRequirementFromEntity;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Unlock> nativeArray = chunk.GetNativeArray(ref m_UnlockTypeHandle);
			for (int i = 0; i < m_PrefabUnlockedRequirementEntities.Length; i++)
			{
				Entity entity = m_PrefabUnlockedRequirementEntities[i];
				if (!m_LockedDataFromEntity.HasEnabledComponent(entity) || !m_PrefabUnlockedRequirementFromEntity.TryGetBuffer(entity, out var bufferData))
				{
					continue;
				}
				for (int j = 0; j < bufferData.Length; j++)
				{
					Entity requirement = bufferData[j].m_Requirement;
					for (int k = 0; k < nativeArray.Length; k++)
					{
						if (requirement == nativeArray[k].m_Prefab)
						{
							Entity e = m_Buffer.CreateEntity(i, m_UnlockEventArchetype);
							m_Buffer.SetComponent(i, e, new Unlock(entity));
						}
					}
				}
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
		public BufferLookup<PrefabUnlockedRequirement> __Game_Prefabs_PrefabUnlockedRequirement_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Locked> __Game_Prefabs_Locked_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<Unlock> __Game_Prefabs_Unlock_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabUnlockedRequirement_RO_BufferLookup = state.GetBufferLookup<PrefabUnlockedRequirement>(isReadOnly: true);
			__Game_Prefabs_Locked_RO_ComponentLookup = state.GetComponentLookup<Locked>(isReadOnly: true);
			__Game_Prefabs_Unlock_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Unlock>(isReadOnly: true);
		}
	}

	private ModificationEndBarrier m_ModificationEndBarrier;

	private EntityQuery m_UnlockQuery;

	private EntityQuery m_PrefabUnlockedQuery;

	private EntityArchetype m_UnlockEventArchetype;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationEndBarrier = base.World.GetOrCreateSystemManaged<ModificationEndBarrier>();
		m_UnlockQuery = GetEntityQuery(ComponentType.ReadOnly<Unlock>());
		m_PrefabUnlockedQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabUnlockedRequirement>());
		m_UnlockEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<Unlock>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_UnlockQuery.IsEmptyIgnoreFilter)
		{
			JobHandle outJobHandle;
			NativeList<Entity> prefabUnlockedRequirementEntities = m_PrefabUnlockedQuery.ToEntityListAsync(Allocator.TempJob, out outJobHandle);
			JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new UnlockJob
			{
				m_UnlockEventArchetype = m_UnlockEventArchetype,
				m_Buffer = m_ModificationEndBarrier.CreateCommandBuffer().AsParallelWriter(),
				m_PrefabUnlockedRequirementEntities = prefabUnlockedRequirementEntities,
				m_PrefabUnlockedRequirementFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_PrefabUnlockedRequirement_RO_BufferLookup, ref base.CheckedStateRef),
				m_LockedDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_Locked_RO_ComponentLookup, ref base.CheckedStateRef),
				m_UnlockTypeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_Unlock_RO_ComponentTypeHandle, ref base.CheckedStateRef)
			}, m_UnlockQuery, JobHandle.CombineDependencies(outJobHandle, base.Dependency));
			prefabUnlockedRequirementEntities.Dispose(jobHandle);
			m_ModificationEndBarrier.AddJobHandleForProducer(jobHandle);
			base.Dependency = jobHandle;
		}
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
	public PrefabUnlockedRequirementSystem()
	{
	}
}

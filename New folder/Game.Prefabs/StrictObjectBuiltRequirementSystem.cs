using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Prefabs;

[CompilerGenerated]
public class StrictObjectBuiltRequirementSystem : GameSystemBase
{
	[BurstCompile]
	private struct TrackObjectsJob : IJobChunk
	{
		public EntityCommandBuffer.ParallelWriter m_Buffer;

		[ReadOnly]
		public NativeParallelHashMap<Entity, int> m_InstanceCounts;

		[ReadOnly]
		public ComponentTypeHandle<StrictObjectBuiltRequirementData> m_ObjectBuiltRequirementDataHandle;

		public ComponentTypeHandle<UnlockRequirementData> m_RequirementDataHandle;

		[ReadOnly]
		public EntityTypeHandle m_EntityTypeHandle;

		public EntityArchetype m_UnlockEventArchetype;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<StrictObjectBuiltRequirementData> nativeArray = chunk.GetNativeArray(ref m_ObjectBuiltRequirementDataHandle);
			NativeArray<UnlockRequirementData> nativeArray2 = chunk.GetNativeArray(ref m_RequirementDataHandle);
			NativeArray<Entity> nativeArray3 = chunk.GetNativeArray(m_EntityTypeHandle);
			ChunkEntityEnumerator chunkEntityEnumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
			int nextIndex;
			while (chunkEntityEnumerator.NextEntityIndex(out nextIndex))
			{
				if (m_InstanceCounts.TryGetValue(nativeArray[nextIndex].m_Requirement, out var item))
				{
					UnlockRequirementData value = nativeArray2[nextIndex];
					value.m_Progress = math.min(nativeArray[nextIndex].m_MinimumCount, item);
					nativeArray2[nextIndex] = value;
					if (nativeArray[nextIndex].m_MinimumCount <= item)
					{
						Entity e = m_Buffer.CreateEntity(unfilteredChunkIndex, m_UnlockEventArchetype);
						m_Buffer.SetComponent(unfilteredChunkIndex, e, new Unlock(nativeArray3[nextIndex]));
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
		public ComponentTypeHandle<StrictObjectBuiltRequirementData> __Game_Prefabs_StrictObjectBuiltRequirementData_RO_ComponentTypeHandle;

		public ComponentTypeHandle<UnlockRequirementData> __Game_Prefabs_UnlockRequirementData_RW_ComponentTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_StrictObjectBuiltRequirementData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<StrictObjectBuiltRequirementData>(isReadOnly: true);
			__Game_Prefabs_UnlockRequirementData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<UnlockRequirementData>();
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
		}
	}

	private InstanceCountSystem m_InstanceCountSystem;

	private ModificationEndBarrier m_ModificationEndBarrier;

	private EntityQuery m_ChangedQuery;

	private EntityQuery m_RequirementQuery;

	private EntityArchetype m_UnlockEventArchetype;

	private bool m_Loaded;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_InstanceCountSystem = base.World.GetOrCreateSystemManaged<InstanceCountSystem>();
		m_ModificationEndBarrier = base.World.GetOrCreateSystemManaged<ModificationEndBarrier>();
		m_ChangedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<PrefabRef>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_RequirementQuery = GetEntityQuery(ComponentType.ReadOnly<StrictObjectBuiltRequirementData>(), ComponentType.ReadWrite<UnlockRequirementData>(), ComponentType.ReadOnly<Locked>());
		m_UnlockEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<Unlock>());
		RequireForUpdate(m_RequirementQuery);
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		m_Loaded = true;
	}

	private bool GetLoaded()
	{
		if (m_Loaded)
		{
			m_Loaded = false;
			return true;
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (GetLoaded() || !m_ChangedQuery.IsEmptyIgnoreFilter)
		{
			JobHandle dependencies;
			TrackObjectsJob jobData = new TrackObjectsJob
			{
				m_Buffer = m_ModificationEndBarrier.CreateCommandBuffer().AsParallelWriter(),
				m_InstanceCounts = m_InstanceCountSystem.GetInstanceCounts(readOnly: true, out dependencies),
				m_ObjectBuiltRequirementDataHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_StrictObjectBuiltRequirementData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_RequirementDataHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_UnlockRequirementData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_EntityTypeHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_UnlockEventArchetype = m_UnlockEventArchetype
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_RequirementQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
			m_InstanceCountSystem.AddCountReader(base.Dependency);
			m_ModificationEndBarrier.AddJobHandleForProducer(base.Dependency);
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
	public StrictObjectBuiltRequirementSystem()
	{
	}
}

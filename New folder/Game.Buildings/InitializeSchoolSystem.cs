using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Citizens;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Buildings;

[CompilerGenerated]
public class InitializeSchoolSystem : GameSystemBase
{
	[BurstCompile]
	private struct InitializeSchoolsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Citizens.Student> m_StudentType;

		[ReadOnly]
		public ComponentLookup<School> m_Schools;

		[ReadOnly]
		public ComponentLookup<Building> m_Buildings;

		[DeallocateOnJobCompletion]
		[ReadOnly]
		public NativeArray<int> m_NewLevels;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Game.Citizens.Student> nativeArray2 = chunk.GetNativeArray(ref m_StudentType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Game.Citizens.Student student = nativeArray2[i];
				if (m_Schools.HasComponent(student.m_School) && !m_Buildings.HasComponent(student.m_School) && student.m_Level >= 1 && student.m_Level <= 4 && m_NewLevels[student.m_Level - 1] != 0)
				{
					m_CommandBuffer.AddComponent<StudentsRemoved>(unfilteredChunkIndex, student.m_School);
					m_CommandBuffer.RemoveComponent<Game.Citizens.Student>(unfilteredChunkIndex, nativeArray[i]);
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
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Citizens.Student> __Game_Citizens_Student_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<School> __Game_Buildings_School_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Citizens_Student_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Citizens.Student>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Buildings_School_RO_ComponentLookup = state.GetComponentLookup<School>(isReadOnly: true);
		}
	}

	private ModificationBarrier5 m_ModificationBarrier;

	private EntityQuery m_CreatedSchoolQuery;

	private EntityQuery m_StudentQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier5>();
		m_CreatedSchoolQuery = GetEntityQuery(ComponentType.ReadOnly<School>(), ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<Updated>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_StudentQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Citizens.Student>(), ComponentType.Exclude<Deleted>());
		RequireForUpdate(m_CreatedSchoolQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeArray<PrefabRef> nativeArray = m_CreatedSchoolQuery.ToComponentDataArray<PrefabRef>(Allocator.TempJob);
		NativeArray<int> newLevels = new NativeArray<int>(4, Allocator.TempJob);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			if (base.EntityManager.TryGetComponent<SchoolData>(nativeArray[i].m_Prefab, out var component) && component.m_EducationLevel >= 1 && component.m_EducationLevel <= 4)
			{
				newLevels[component.m_EducationLevel - 1] = 1;
			}
		}
		nativeArray.Dispose();
		InitializeSchoolsJob jobData = new InitializeSchoolsJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_StudentType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Student_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_Buildings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Schools = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_School_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NewLevels = newLevels,
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_StudentQuery, base.Dependency);
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
	public InitializeSchoolSystem()
	{
	}
}

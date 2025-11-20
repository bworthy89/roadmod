using System.Runtime.CompilerServices;
using Game.Citizens;
using Game.Common;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Buildings;

[CompilerGenerated]
public class SchoolUpdatedSystem : GameSystemBase
{
	[BurstCompile]
	private struct SchoolUpdatedJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public BufferTypeHandle<Student> m_StudentType;

		[ReadOnly]
		public ComponentLookup<Game.Citizens.Student> m_Students;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<Student> bufferAccessor = chunk.GetBufferAccessor(ref m_StudentType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				DynamicBuffer<Student> dynamicBuffer = bufferAccessor[i];
				Entity entity = nativeArray[i];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					Entity student = dynamicBuffer[j].m_Student;
					if (!m_Students.HasComponent(student) || m_Students[student].m_School != entity)
					{
						dynamicBuffer.RemoveAt(j);
						j--;
					}
				}
				m_CommandBuffer.RemoveComponent<StudentsRemoved>(unfilteredChunkIndex, entity);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct SchoolDeletedJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<Student> m_StudentType;

		[ReadOnly]
		public ComponentLookup<Game.Citizens.Student> m_Students;

		[ReadOnly]
		public ComponentLookup<TravelPurpose> m_Purposes;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<Student> bufferAccessor = chunk.GetBufferAccessor(ref m_StudentType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				DynamicBuffer<Student> dynamicBuffer = bufferAccessor[i];
				Entity entity = nativeArray[i];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					Entity student = dynamicBuffer[j].m_Student;
					if (m_Students.HasComponent(student) && m_Students[student].m_School == entity)
					{
						if (m_Purposes.HasComponent(student) && (m_Purposes[student].m_Purpose == Purpose.GoingToSchool || m_Purposes[student].m_Purpose == Purpose.Studying))
						{
							m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, student);
						}
						m_CommandBuffer.RemoveComponent<Game.Citizens.Student>(unfilteredChunkIndex, student);
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
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		public BufferTypeHandle<Student> __Game_Buildings_Student_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Game.Citizens.Student> __Game_Citizens_Student_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TravelPurpose> __Game_Citizens_TravelPurpose_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Buildings_Student_RW_BufferTypeHandle = state.GetBufferTypeHandle<Student>();
			__Game_Citizens_Student_RO_ComponentLookup = state.GetComponentLookup<Game.Citizens.Student>(isReadOnly: true);
			__Game_Citizens_TravelPurpose_RO_ComponentLookup = state.GetComponentLookup<TravelPurpose>(isReadOnly: true);
		}
	}

	private ModificationEndBarrier m_ModificationBarrier;

	private EntityQuery m_UpdatedSchoolQuery;

	private EntityQuery m_DeletedSchoolQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationEndBarrier>();
		m_UpdatedSchoolQuery = GetEntityQuery(ComponentType.ReadOnly<School>(), ComponentType.ReadOnly<Student>(), ComponentType.ReadOnly<StudentsRemoved>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_DeletedSchoolQuery = GetEntityQuery(ComponentType.ReadOnly<School>(), ComponentType.ReadOnly<Student>(), ComponentType.ReadOnly<Deleted>(), ComponentType.Exclude<Temp>());
		RequireAnyForUpdate(m_UpdatedSchoolQuery, m_DeletedSchoolQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_UpdatedSchoolQuery.IsEmptyIgnoreFilter)
		{
			SchoolUpdatedJob jobData = new SchoolUpdatedJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_StudentType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Student_RW_BufferTypeHandle, ref base.CheckedStateRef),
				m_Students = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Student_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_UpdatedSchoolQuery, base.Dependency);
			m_ModificationBarrier.AddJobHandleForProducer(base.Dependency);
		}
		if (!m_DeletedSchoolQuery.IsEmptyIgnoreFilter)
		{
			SchoolDeletedJob jobData2 = new SchoolDeletedJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_StudentType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Student_RW_BufferTypeHandle, ref base.CheckedStateRef),
				m_Purposes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Students = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Student_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData2, m_DeletedSchoolQuery, base.Dependency);
			m_ModificationBarrier.AddJobHandleForProducer(base.Dependency);
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
	public SchoolUpdatedSystem()
	{
	}
}

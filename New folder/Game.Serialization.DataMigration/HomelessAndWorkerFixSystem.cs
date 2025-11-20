using System.Runtime.CompilerServices;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Serialization.DataMigration;

[CompilerGenerated]
public class HomelessAndWorkerFixSystem : GameSystemBase
{
	[BurstCompile]
	private struct WorkerFixJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Worker> m_WorkerType;

		[ReadOnly]
		public BufferLookup<Employee> m_EmployeeBufs;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Worker> nativeArray2 = chunk.GetNativeArray(ref m_WorkerType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				if (!m_EmployeeBufs.HasBuffer(nativeArray2[i].m_Workplace))
				{
					continue;
				}
				DynamicBuffer<Employee> dynamicBuffer = m_EmployeeBufs[nativeArray2[i].m_Workplace];
				bool flag = false;
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					if (dynamicBuffer[j].m_Worker == nativeArray[i])
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					m_CommandBuffer.RemoveComponent<Worker>(unfilteredChunkIndex, nativeArray[i]);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct AddPropertySeekerJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				m_CommandBuffer.AddComponent<PropertySeeker>(unfilteredChunkIndex, nativeArray[i]);
				m_CommandBuffer.SetComponentEnabled<PropertySeeker>(unfilteredChunkIndex, nativeArray[i], value: false);
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
		public ComponentTypeHandle<Worker> __Game_Citizens_Worker_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferLookup<Employee> __Game_Companies_Employee_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Citizens_Worker_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Worker>(isReadOnly: true);
			__Game_Companies_Employee_RO_BufferLookup = state.GetBufferLookup<Employee>(isReadOnly: true);
		}
	}

	private LoadGameSystem m_LoadGameSystem;

	private DeserializationBarrier m_DeserializationBarrier;

	private EntityQuery m_WorkerQuery;

	private EntityQuery m_HomelessQuery;

	private EntityQuery m_NeedAddPropertySeekerQuery;

	private EntityQuery m_AbandonedPropertyQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_LoadGameSystem = base.World.GetOrCreateSystemManaged<LoadGameSystem>();
		m_DeserializationBarrier = base.World.GetOrCreateSystemManaged<DeserializationBarrier>();
		m_WorkerQuery = GetEntityQuery(ComponentType.ReadOnly<Worker>());
		m_HomelessQuery = GetEntityQuery(ComponentType.ReadOnly<HomelessHousehold>());
		m_AbandonedPropertyQuery = GetEntityQuery(ComponentType.ReadOnly<Abandoned>());
		m_NeedAddPropertySeekerQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Household>(),
				ComponentType.ReadOnly<CompanyData>()
			},
			None = new ComponentType[1] { ComponentType.Exclude<PropertySeeker>() }
		});
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_LoadGameSystem.context.format.Has(FormatTags.HomelessAndWorkerFix))
		{
			if (!m_WorkerQuery.IsEmptyIgnoreFilter)
			{
				JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new WorkerFixJob
				{
					m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
					m_WorkerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Worker_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_EmployeeBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_Employee_RO_BufferLookup, ref base.CheckedStateRef),
					m_CommandBuffer = m_DeserializationBarrier.CreateCommandBuffer().AsParallelWriter()
				}, m_WorkerQuery, base.Dependency);
				m_DeserializationBarrier.AddJobHandleForProducer(jobHandle);
				base.Dependency = jobHandle;
			}
			if (!m_HomelessQuery.IsEmptyIgnoreFilter)
			{
				base.EntityManager.AddComponent<Deleted>(m_HomelessQuery);
			}
			if (!m_NeedAddPropertySeekerQuery.IsEmptyIgnoreFilter)
			{
				JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(new AddPropertySeekerJob
				{
					m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
					m_CommandBuffer = m_DeserializationBarrier.CreateCommandBuffer().AsParallelWriter()
				}, m_NeedAddPropertySeekerQuery, base.Dependency);
				m_DeserializationBarrier.AddJobHandleForProducer(jobHandle2);
				base.Dependency = jobHandle2;
			}
			if (!m_AbandonedPropertyQuery.IsEmptyIgnoreFilter)
			{
				base.EntityManager.RemoveComponent<PropertyOnMarket>(m_AbandonedPropertyQuery);
			}
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
	public HomelessAndWorkerFixSystem()
	{
	}
}

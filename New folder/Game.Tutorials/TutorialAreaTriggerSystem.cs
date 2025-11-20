using System.Runtime.CompilerServices;
using Game.Areas;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Tutorials;

[CompilerGenerated]
public class TutorialAreaTriggerSystem : TutorialTriggerSystemBase
{
	[BurstCompile]
	private struct CheckModifiedAreasJob : IJobChunk
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_AreaModificationChunks;

		[ReadOnly]
		public BufferTypeHandle<AreaTriggerData> m_TriggerType;

		[ReadOnly]
		public BufferLookup<UnlockRequirement> m_UnlockRequirementFromEntity;

		[ReadOnly]
		public BufferLookup<ForceUIGroupUnlockData> m_ForcedUnlockDataFromEntity;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<Created> m_CreatedType;

		[ReadOnly]
		public ComponentTypeHandle<Updated> m_UpdatedType;

		[ReadOnly]
		public EntityArchetype m_UnlockEventArchetype;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public bool m_FirstTimeCheck;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<AreaTriggerData> bufferAccessor = chunk.GetBufferAccessor(ref m_TriggerType);
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				if (Check(bufferAccessor[i]))
				{
					if (m_FirstTimeCheck)
					{
						m_CommandBuffer.AddComponent<TriggerPreCompleted>(unfilteredChunkIndex, nativeArray[i]);
					}
					else
					{
						m_CommandBuffer.AddComponent<TriggerCompleted>(unfilteredChunkIndex, nativeArray[i]);
					}
					TutorialSystem.ManualUnlock(nativeArray[i], m_UnlockEventArchetype, ref m_ForcedUnlockDataFromEntity, ref m_UnlockRequirementFromEntity, m_CommandBuffer, unfilteredChunkIndex);
				}
			}
		}

		private bool Check(DynamicBuffer<AreaTriggerData> triggerDatas)
		{
			for (int i = 0; i < m_AreaModificationChunks.Length; i++)
			{
				AreaTriggerFlags areaTriggerFlags = (AreaTriggerFlags)0;
				if (m_AreaModificationChunks[i].Has(ref m_CreatedType))
				{
					areaTriggerFlags |= AreaTriggerFlags.Created;
				}
				if (m_AreaModificationChunks[i].Has(ref m_UpdatedType))
				{
					areaTriggerFlags |= AreaTriggerFlags.Modified;
				}
				for (int j = 0; j < triggerDatas.Length; j++)
				{
					AreaTriggerData areaTriggerData = triggerDatas[j];
					if (areaTriggerData.m_Flags != 0 && (areaTriggerData.m_Flags & areaTriggerFlags) == 0)
					{
						continue;
					}
					NativeArray<PrefabRef> nativeArray = m_AreaModificationChunks[i].GetNativeArray(ref m_PrefabRefType);
					for (int k = 0; k < nativeArray.Length; k++)
					{
						if (areaTriggerData.m_Prefab == nativeArray[k].m_Prefab)
						{
							return true;
						}
					}
				}
			}
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
		public BufferTypeHandle<AreaTriggerData> __Game_Tutorials_AreaTriggerData_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferLookup<UnlockRequirement> __Game_Prefabs_UnlockRequirement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ForceUIGroupUnlockData> __Game_Prefabs_ForceUIGroupUnlockData_RO_BufferLookup;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Created> __Game_Common_Created_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Updated> __Game_Common_Updated_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Tutorials_AreaTriggerData_RO_BufferTypeHandle = state.GetBufferTypeHandle<AreaTriggerData>(isReadOnly: true);
			__Game_Prefabs_UnlockRequirement_RO_BufferLookup = state.GetBufferLookup<UnlockRequirement>(isReadOnly: true);
			__Game_Prefabs_ForceUIGroupUnlockData_RO_BufferLookup = state.GetBufferLookup<ForceUIGroupUnlockData>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Common_Created_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Created>(isReadOnly: true);
			__Game_Common_Updated_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Updated>(isReadOnly: true);
		}
	}

	private EntityQuery m_AreaModificationQuery;

	private EntityQuery m_AreaQuery;

	private EntityArchetype m_UnlockEventArchetype;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_AreaModificationQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Area>(), ComponentType.ReadOnly<Updated>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Native>());
		m_AreaQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Area>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Native>());
		m_ActiveTriggerQuery = GetEntityQuery(ComponentType.ReadOnly<AreaTriggerData>(), ComponentType.ReadOnly<TriggerActive>(), ComponentType.Exclude<TriggerCompleted>());
		m_UnlockEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<Unlock>());
		RequireForUpdate(m_ActiveTriggerQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (base.triggersChanged)
		{
			JobHandle outJobHandle;
			CheckModifiedAreasJob jobData = new CheckModifiedAreasJob
			{
				m_AreaModificationChunks = m_AreaQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle),
				m_TriggerType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Tutorials_AreaTriggerData_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_UnlockRequirementFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_UnlockRequirement_RO_BufferLookup, ref base.CheckedStateRef),
				m_ForcedUnlockDataFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ForceUIGroupUnlockData_RO_BufferLookup, ref base.CheckedStateRef),
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CreatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Created_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_UpdatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Updated_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_UnlockEventArchetype = m_UnlockEventArchetype,
				m_CommandBuffer = m_BarrierSystem.CreateCommandBuffer().AsParallelWriter(),
				m_FirstTimeCheck = true
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_ActiveTriggerQuery, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
			jobData.m_AreaModificationChunks.Dispose(base.Dependency);
			m_BarrierSystem.AddJobHandleForProducer(base.Dependency);
		}
		else if (!m_AreaModificationQuery.IsEmptyIgnoreFilter)
		{
			JobHandle outJobHandle2;
			CheckModifiedAreasJob jobData2 = new CheckModifiedAreasJob
			{
				m_AreaModificationChunks = m_AreaModificationQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle2),
				m_TriggerType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Tutorials_AreaTriggerData_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_UnlockRequirementFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_UnlockRequirement_RO_BufferLookup, ref base.CheckedStateRef),
				m_ForcedUnlockDataFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ForceUIGroupUnlockData_RO_BufferLookup, ref base.CheckedStateRef),
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CreatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Created_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_UpdatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Updated_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_UnlockEventArchetype = m_UnlockEventArchetype,
				m_CommandBuffer = m_BarrierSystem.CreateCommandBuffer().AsParallelWriter(),
				m_FirstTimeCheck = false
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData2, m_ActiveTriggerQuery, JobHandle.CombineDependencies(base.Dependency, outJobHandle2));
			jobData2.m_AreaModificationChunks.Dispose(base.Dependency);
			m_BarrierSystem.AddJobHandleForProducer(base.Dependency);
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
	public TutorialAreaTriggerSystem()
	{
	}
}

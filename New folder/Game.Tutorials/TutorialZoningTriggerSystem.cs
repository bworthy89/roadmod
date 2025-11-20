using System.Runtime.CompilerServices;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Game.Zones;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Tutorials;

[CompilerGenerated]
public class TutorialZoningTriggerSystem : TutorialTriggerSystemBase
{
	[BurstCompile]
	private struct CheckZonesJob : IJobChunk
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_ZoneChunks;

		[ReadOnly]
		public BufferTypeHandle<Cell> m_CellType;

		[ReadOnly]
		public BufferTypeHandle<ZoningTriggerData> m_ZoningTriggerType;

		[ReadOnly]
		public BufferLookup<UnlockRequirement> m_UnlockRequirementFromEntity;

		[ReadOnly]
		public BufferLookup<ForceUIGroupUnlockData> m_ForcedUnlockDataFromEntity;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ZonePrefabs m_ZonePrefabs;

		[ReadOnly]
		public EntityArchetype m_UnlockEventArchetype;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public bool m_FirstTimeCheck;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<ZoningTriggerData> bufferAccessor = chunk.GetBufferAccessor(ref m_ZoningTriggerType);
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

		private bool Check(DynamicBuffer<ZoningTriggerData> triggerDatas)
		{
			for (int i = 0; i < m_ZoneChunks.Length; i++)
			{
				if (Check(triggerDatas, m_ZoneChunks[i].GetBufferAccessor(ref m_CellType)))
				{
					return true;
				}
			}
			return false;
		}

		private bool Check(DynamicBuffer<ZoningTriggerData> triggerDatas, BufferAccessor<Cell> cellAccessor)
		{
			for (int i = 0; i < cellAccessor.Length; i++)
			{
				if (Check(triggerDatas, cellAccessor[i]))
				{
					return true;
				}
			}
			return false;
		}

		private bool Check(DynamicBuffer<ZoningTriggerData> triggerDatas, DynamicBuffer<Cell> cells)
		{
			for (int i = 0; i < cells.Length; i++)
			{
				if (Check(triggerDatas, m_ZonePrefabs[cells[i].m_Zone]))
				{
					return true;
				}
			}
			return false;
		}

		private bool Check(DynamicBuffer<ZoningTriggerData> triggerDatas, Entity zone)
		{
			for (int i = 0; i < triggerDatas.Length; i++)
			{
				if (triggerDatas[i].m_Zone == zone)
				{
					return true;
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
		public BufferTypeHandle<Cell> __Game_Zones_Cell_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<ZoningTriggerData> __Game_Tutorials_ZoningTriggerData_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferLookup<UnlockRequirement> __Game_Prefabs_UnlockRequirement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ForceUIGroupUnlockData> __Game_Prefabs_ForceUIGroupUnlockData_RO_BufferLookup;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Zones_Cell_RO_BufferTypeHandle = state.GetBufferTypeHandle<Cell>(isReadOnly: true);
			__Game_Tutorials_ZoningTriggerData_RO_BufferTypeHandle = state.GetBufferTypeHandle<ZoningTriggerData>(isReadOnly: true);
			__Game_Prefabs_UnlockRequirement_RO_BufferLookup = state.GetBufferLookup<UnlockRequirement>(isReadOnly: true);
			__Game_Prefabs_ForceUIGroupUnlockData_RO_BufferLookup = state.GetBufferLookup<ForceUIGroupUnlockData>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
		}
	}

	private ZoneSystem m_ZoneSystem;

	private EntityQuery m_CreatedZonesQuery;

	private EntityQuery m_ZonesQuery;

	private EntityArchetype m_UnlockEventArchetype;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ActiveTriggerQuery = GetEntityQuery(ComponentType.ReadOnly<ZoningTriggerData>(), ComponentType.ReadOnly<TriggerActive>(), ComponentType.Exclude<TriggerCompleted>());
		m_CreatedZonesQuery = GetEntityQuery(ComponentType.ReadOnly<Cell>(), ComponentType.ReadOnly<Updated>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Created>(), ComponentType.Exclude<Native>());
		m_ZonesQuery = GetEntityQuery(ComponentType.ReadOnly<Cell>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Created>(), ComponentType.Exclude<Native>());
		m_UnlockEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<Unlock>());
		m_ZoneSystem = base.World.GetOrCreateSystemManaged<ZoneSystem>();
		RequireForUpdate(m_ActiveTriggerQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (base.triggersChanged && !m_ZonesQuery.IsEmptyIgnoreFilter)
		{
			JobHandle outJobHandle;
			CheckZonesJob jobData = new CheckZonesJob
			{
				m_ZoneChunks = m_ZonesQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle),
				m_CellType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Zones_Cell_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_ZoningTriggerType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Tutorials_ZoningTriggerData_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_UnlockRequirementFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_UnlockRequirement_RO_BufferLookup, ref base.CheckedStateRef),
				m_ForcedUnlockDataFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ForceUIGroupUnlockData_RO_BufferLookup, ref base.CheckedStateRef),
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_ZonePrefabs = m_ZoneSystem.GetPrefabs(),
				m_UnlockEventArchetype = m_UnlockEventArchetype,
				m_CommandBuffer = m_BarrierSystem.CreateCommandBuffer().AsParallelWriter(),
				m_FirstTimeCheck = true
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_ActiveTriggerQuery, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
			jobData.m_ZoneChunks.Dispose(base.Dependency);
			m_ZoneSystem.AddPrefabsReader(base.Dependency);
			m_BarrierSystem.AddJobHandleForProducer(base.Dependency);
		}
		else if (!m_CreatedZonesQuery.IsEmptyIgnoreFilter)
		{
			JobHandle outJobHandle2;
			CheckZonesJob jobData2 = new CheckZonesJob
			{
				m_ZoneChunks = m_CreatedZonesQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle2),
				m_CellType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Zones_Cell_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_ZoningTriggerType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Tutorials_ZoningTriggerData_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_UnlockRequirementFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_UnlockRequirement_RO_BufferLookup, ref base.CheckedStateRef),
				m_ForcedUnlockDataFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ForceUIGroupUnlockData_RO_BufferLookup, ref base.CheckedStateRef),
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_ZonePrefabs = m_ZoneSystem.GetPrefabs(),
				m_UnlockEventArchetype = m_UnlockEventArchetype,
				m_CommandBuffer = m_BarrierSystem.CreateCommandBuffer().AsParallelWriter(),
				m_FirstTimeCheck = false
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData2, m_ActiveTriggerQuery, JobHandle.CombineDependencies(base.Dependency, outJobHandle2));
			jobData2.m_ZoneChunks.Dispose(base.Dependency);
			m_ZoneSystem.AddPrefabsReader(base.Dependency);
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
	public TutorialZoningTriggerSystem()
	{
	}
}

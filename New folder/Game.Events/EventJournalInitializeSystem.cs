using System.Runtime.CompilerServices;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Events;

[CompilerGenerated]
public class EventJournalInitializeSystem : GameSystemBase
{
	[BurstCompile]
	private struct InitEventJournalEntriesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<Duration> m_DurationType;

		[ReadOnly]
		public ComponentLookup<JournalEventPrefabData> m_JournalEventPrefabDatas;

		public EntityArchetype m_JournalArchetype;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
			if (chunk.Has(ref m_DurationType))
			{
				NativeArray<Duration> nativeArray3 = chunk.GetNativeArray(ref m_DurationType);
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					Entity entity = CreateJournalEntry(nativeArray2[i], nativeArray[i].m_Prefab, unfilteredChunkIndex);
					m_CommandBuffer.SetComponent(unfilteredChunkIndex, nativeArray2[i], new JournalEvent
					{
						m_JournalEntity = entity
					});
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, new EventJournalPending
					{
						m_StartFrame = nativeArray3[i].m_StartFrame
					});
				}
			}
			else
			{
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					Entity journalEntity = CreateJournalEntry(nativeArray2[j], nativeArray[j].m_Prefab, unfilteredChunkIndex);
					m_CommandBuffer.SetComponent(unfilteredChunkIndex, nativeArray2[j], new JournalEvent
					{
						m_JournalEntity = journalEntity
					});
				}
			}
		}

		private Entity CreateJournalEntry(Entity eventEntity, Entity eventPrefab, int chunkIndex)
		{
			Entity entity = m_CommandBuffer.CreateEntity(chunkIndex, m_JournalArchetype);
			m_CommandBuffer.SetComponent(chunkIndex, entity, new EventJournalEntry
			{
				m_Event = eventEntity
			});
			m_CommandBuffer.SetComponent(chunkIndex, entity, new PrefabRef
			{
				m_Prefab = eventPrefab
			});
			JournalEventPrefabData journalEventPrefabData = m_JournalEventPrefabDatas[eventPrefab];
			if (journalEventPrefabData.m_DataFlags != 0)
			{
				DynamicBuffer<EventJournalData> datas = m_CommandBuffer.AddBuffer<EventJournalData>(chunkIndex, entity);
				InitDataBuffer(datas, journalEventPrefabData.m_DataFlags);
			}
			if (journalEventPrefabData.m_EffectFlags != 0)
			{
				DynamicBuffer<EventJournalCityEffect> effects = m_CommandBuffer.AddBuffer<EventJournalCityEffect>(chunkIndex, entity);
				InitEffectBuffer(effects, journalEventPrefabData.m_EffectFlags);
			}
			return entity;
		}

		private void InitDataBuffer(DynamicBuffer<EventJournalData> datas, int dataFlags)
		{
			for (int i = 0; i < 3; i++)
			{
				if (((1 << i) & dataFlags) != 0)
				{
					datas.Add(new EventJournalData
					{
						m_Type = (EventDataTrackingType)i,
						m_Value = 0
					});
				}
			}
		}

		private void InitEffectBuffer(DynamicBuffer<EventJournalCityEffect> effects, int effectFlags)
		{
			for (int i = 0; i < 5; i++)
			{
				if (((1 << i) & effectFlags) != 0)
				{
					effects.Add(new EventJournalCityEffect
					{
						m_Type = (EventCityEffectTrackingType)i,
						m_StartValue = 0,
						m_Value = 0
					});
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
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Duration> __Game_Events_Duration_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<JournalEventPrefabData> __Game_Prefabs_JournalEventPrefabData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Events_Duration_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Duration>(isReadOnly: true);
			__Game_Prefabs_JournalEventPrefabData_RO_ComponentLookup = state.GetComponentLookup<JournalEventPrefabData>(isReadOnly: true);
		}
	}

	private EntityQuery m_CreatedEventQuery;

	private EntityArchetype m_EventJournalArchetype;

	private ModificationBarrier4 m_ModificationBarrier;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CreatedEventQuery = GetEntityQuery(ComponentType.ReadOnly<Event>(), ComponentType.ReadOnly<Created>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<JournalEvent>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_EventJournalArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<EventJournalEntry>(), ComponentType.ReadWrite<Created>(), ComponentType.ReadWrite<PrefabRef>());
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier4>();
		RequireForUpdate(m_CreatedEventQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		InitEventJournalEntriesJob jobData = new InitEventJournalEntriesJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DurationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_Duration_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_JournalEventPrefabDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_JournalEventPrefabData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_JournalArchetype = m_EventJournalArchetype,
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_CreatedEventQuery, base.Dependency);
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
	public EventJournalInitializeSystem()
	{
	}
}

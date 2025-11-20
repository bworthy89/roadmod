using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.City;
using Game.Common;
using Game.Prefabs;
using Game.Simulation;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Events;

[CompilerGenerated]
public class EventJournalSystem : GameSystemBase, IEventJournalSystem
{
	[BurstCompile]
	private struct StartedEventsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<EventJournalPending> m_PendingType;

		public BufferTypeHandle<EventJournalCityEffect> m_CityEffectType;

		public ComponentTypeHandle<EventJournalEntry> m_EntryType;

		public uint m_SimulationFrame;

		[ReadOnly]
		public NativeArray<int> m_CityEffects;

		public NativeQueue<Entity>.ParallelWriter m_Started;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<EventJournalEntry> nativeArray2 = chunk.GetNativeArray(ref m_EntryType);
			if (chunk.Has(ref m_CityEffectType))
			{
				BufferAccessor<EventJournalCityEffect> bufferAccessor = chunk.GetBufferAccessor(ref m_CityEffectType);
				if (chunk.Has(ref m_PendingType))
				{
					NativeArray<EventJournalPending> nativeArray3 = chunk.GetNativeArray(ref m_PendingType);
					for (int i = 0; i < nativeArray.Length; i++)
					{
						if (nativeArray3[i].m_StartFrame <= m_SimulationFrame)
						{
							Init(bufferAccessor[i]);
							m_CommandBuffer.RemoveComponent<EventJournalPending>(unfilteredChunkIndex, nativeArray[i]);
							EventJournalEntry value = nativeArray2[i];
							value.m_StartFrame = m_SimulationFrame;
							nativeArray2[i] = value;
							m_Started.Enqueue(nativeArray[i]);
						}
					}
				}
				else
				{
					for (int j = 0; j < nativeArray.Length; j++)
					{
						Init(bufferAccessor[j]);
						EventJournalEntry value2 = nativeArray2[j];
						value2.m_StartFrame = m_SimulationFrame;
						nativeArray2[j] = value2;
						m_Started.Enqueue(nativeArray[j]);
					}
				}
			}
			else if (chunk.Has(ref m_PendingType))
			{
				NativeArray<EventJournalPending> nativeArray4 = chunk.GetNativeArray(ref m_PendingType);
				for (int k = 0; k < nativeArray.Length; k++)
				{
					if (nativeArray4[k].m_StartFrame <= m_SimulationFrame)
					{
						m_CommandBuffer.RemoveComponent<EventJournalPending>(unfilteredChunkIndex, nativeArray[k]);
						EventJournalEntry value3 = nativeArray2[k];
						value3.m_StartFrame = m_SimulationFrame;
						nativeArray2[k] = value3;
						m_Started.Enqueue(nativeArray[k]);
					}
				}
			}
			else
			{
				for (int l = 0; l < nativeArray.Length; l++)
				{
					EventJournalEntry value4 = nativeArray2[l];
					value4.m_StartFrame = m_SimulationFrame;
					nativeArray2[l] = value4;
					m_Started.Enqueue(nativeArray[l]);
				}
			}
		}

		private void Init(DynamicBuffer<EventJournalCityEffect> effects)
		{
			for (int i = 0; i < effects.Length; i++)
			{
				EventJournalCityEffect value = effects[i];
				value.m_StartValue = m_CityEffects[(int)value.m_Type];
				effects[i] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct DeletedEventsJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<JournalEvent> m_JournalEventType;

		[ReadOnly]
		public ComponentLookup<EventJournalCompleted> m_CompletedData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<EventJournalEntry> m_EntryData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<JournalEvent> nativeArray = chunk.GetNativeArray(ref m_JournalEventType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity journalEntity = nativeArray[i].m_JournalEntity;
				if (m_EntryData.HasComponent(journalEntity))
				{
					EventJournalEntry value = m_EntryData[journalEntity];
					value.m_Event = Entity.Null;
					m_EntryData[journalEntity] = value;
					if (!m_CompletedData.HasComponent(journalEntity))
					{
						m_CommandBuffer.AddComponent<EventJournalCompleted>(unfilteredChunkIndex, nativeArray[i].m_JournalEntity);
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct TrackCityEffectsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public BufferTypeHandle<EventJournalCityEffect> m_CityEffectType;

		[ReadOnly]
		public NativeArray<int> m_CityEffects;

		public NativeQueue<Entity>.ParallelWriter m_Changes;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<EventJournalCityEffect> bufferAccessor = chunk.GetBufferAccessor(ref m_CityEffectType);
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				if (Update(bufferAccessor[i]))
				{
					m_Changes.Enqueue(nativeArray[i]);
				}
			}
		}

		private bool Update(DynamicBuffer<EventJournalCityEffect> effects)
		{
			bool result = false;
			for (int i = 0; i < effects.Length; i++)
			{
				EventJournalCityEffect value = effects[i];
				int num = m_CityEffects[(int)value.m_Type];
				if (value.m_Value != num)
				{
					result = true;
					value.m_Value = num;
					effects[i] = value;
				}
			}
			return result;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct TrackDataJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<AddEventJournalData> m_AddDataType;

		[ReadOnly]
		public ComponentLookup<JournalEvent> m_JournalEvents;

		public BufferLookup<EventJournalData> m_EventJournalDatas;

		public NativeQueue<Entity> m_Changes;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<AddEventJournalData> nativeArray = chunk.GetNativeArray(ref m_AddDataType);
			Execute(nativeArray);
		}

		private void Execute(NativeArray<AddEventJournalData> addedDatas)
		{
			for (int i = 0; i < addedDatas.Length; i++)
			{
				AddEventJournalData addEventJournalData = addedDatas[i];
				if (!m_JournalEvents.HasComponent(addEventJournalData.m_Event))
				{
					continue;
				}
				Entity journalEntity = m_JournalEvents[addedDatas[i].m_Event].m_JournalEntity;
				if (m_EventJournalDatas.HasBuffer(journalEntity))
				{
					DynamicBuffer<EventJournalData> eventJournalDatas = m_EventJournalDatas[journalEntity];
					if (TryAddData(eventJournalDatas, addEventJournalData.m_Type, addEventJournalData.m_Count))
					{
						m_Changes.Enqueue(journalEntity);
					}
				}
			}
		}

		private bool TryAddData(DynamicBuffer<EventJournalData> eventJournalDatas, EventDataTrackingType type, int count)
		{
			for (int i = 0; i < eventJournalDatas.Length; i++)
			{
				EventJournalData value = eventJournalDatas[i];
				if (value.m_Type == type)
				{
					value.m_Value += count;
					eventJournalDatas[i] = value;
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

	[BurstCompile]
	private struct CheckJournalTrackingEndJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<EventJournalEntry> m_EntryType;

		[ReadOnly]
		public ComponentLookup<Fire> m_FireData;

		[ReadOnly]
		public BufferLookup<TargetElement> m_TargetElementData;

		[ReadOnly]
		public ComponentLookup<OnFire> m_OnFireData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<EventJournalEntry> nativeArray = chunk.GetNativeArray(ref m_EntryType);
			NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i].m_Event;
				if (m_FireData.HasComponent(entity) && m_TargetElementData.HasBuffer(entity) && CheckFireEnded(m_TargetElementData[entity]))
				{
					m_CommandBuffer.AddComponent<EventJournalCompleted>(unfilteredChunkIndex, nativeArray2[i]);
				}
			}
		}

		private bool CheckFireEnded(DynamicBuffer<TargetElement> targetElements)
		{
			for (int i = 0; i < targetElements.Length; i++)
			{
				if (m_OnFireData.HasComponent(targetElements[i].m_Entity))
				{
					return false;
				}
			}
			return true;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct JournalSortingInfo : IComparable<JournalSortingInfo>
	{
		public Entity m_Entity;

		public uint m_StartFrame;

		public int CompareTo(JournalSortingInfo other)
		{
			return (int)(m_StartFrame - other.m_StartFrame);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<EventJournalPending> __Game_Events_EventJournalPending_RO_ComponentTypeHandle;

		public BufferTypeHandle<EventJournalCityEffect> __Game_Events_EventJournalCityEffect_RW_BufferTypeHandle;

		public ComponentTypeHandle<EventJournalEntry> __Game_Events_EventJournalEntry_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<JournalEvent> __Game_Events_JournalEvent_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<EventJournalCompleted> __Game_Events_EventJournalCompleted_RO_ComponentLookup;

		public ComponentLookup<EventJournalEntry> __Game_Events_EventJournalEntry_RW_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<EventJournalEntry> __Game_Events_EventJournalEntry_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Fire> __Game_Events_Fire_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<TargetElement> __Game_Events_TargetElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<OnFire> __Game_Events_OnFire_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<AddEventJournalData> __Game_Events_AddEventJournalData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<JournalEvent> __Game_Events_JournalEvent_RO_ComponentLookup;

		public BufferLookup<EventJournalData> __Game_Events_EventJournalData_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Events_EventJournalPending_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EventJournalPending>(isReadOnly: true);
			__Game_Events_EventJournalCityEffect_RW_BufferTypeHandle = state.GetBufferTypeHandle<EventJournalCityEffect>();
			__Game_Events_EventJournalEntry_RW_ComponentTypeHandle = state.GetComponentTypeHandle<EventJournalEntry>();
			__Game_Events_JournalEvent_RO_ComponentTypeHandle = state.GetComponentTypeHandle<JournalEvent>(isReadOnly: true);
			__Game_Events_EventJournalCompleted_RO_ComponentLookup = state.GetComponentLookup<EventJournalCompleted>(isReadOnly: true);
			__Game_Events_EventJournalEntry_RW_ComponentLookup = state.GetComponentLookup<EventJournalEntry>();
			__Game_Events_EventJournalEntry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EventJournalEntry>(isReadOnly: true);
			__Game_Events_Fire_RO_ComponentLookup = state.GetComponentLookup<Fire>(isReadOnly: true);
			__Game_Events_TargetElement_RO_BufferLookup = state.GetBufferLookup<TargetElement>(isReadOnly: true);
			__Game_Events_OnFire_RO_ComponentLookup = state.GetComponentLookup<OnFire>(isReadOnly: true);
			__Game_Events_AddEventJournalData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AddEventJournalData>(isReadOnly: true);
			__Game_Events_JournalEvent_RO_ComponentLookup = state.GetComponentLookup<JournalEvent>(isReadOnly: true);
			__Game_Events_EventJournalData_RW_BufferLookup = state.GetBufferLookup<EventJournalData>();
		}
	}

	private EntityQuery m_StartedJournalQuery;

	private EntityQuery m_DeletedEventQuery;

	private EntityQuery m_JournalDataEventQuery;

	private EntityQuery m_ActiveJournalEffectQuery;

	private EntityQuery m_JournalEventPrefabQuery;

	private EntityQuery m_LoadedJournalQuery;

	private ISimulationSystem m_SimulationSystem;

	private IBudgetSystem m_BudgetSystem;

	private ICityServiceBudgetSystem m_CityServiceBudgetSystem;

	private CitySystem m_CitySystem;

	private ModificationBarrier5 m_ModificationBarrier;

	private NativeQueue<Entity> m_Started;

	private NativeQueue<Entity> m_Changed;

	private NativeArray<int> m_CityEffects;

	private TypeHandle __TypeHandle;

	public NativeList<Entity> eventJournal { get; private set; }

	public Action<Entity> eventEventDataChanged { get; set; }

	public Action eventEntryAdded { get; set; }

	public IEnumerable<JournalEventComponent> eventPrefabs
	{
		get
		{
			PrefabSystem prefabSystem = base.World.GetExistingSystemManaged<PrefabSystem>();
			NativeArray<PrefabData> prefabs = m_JournalEventPrefabQuery.ToComponentDataArray<PrefabData>(Allocator.TempJob);
			try
			{
				int i = 0;
				while (i < prefabs.Length)
				{
					EventPrefab prefab = prefabSystem.GetPrefab<EventPrefab>(prefabs[i]);
					yield return prefab.GetComponent<JournalEventComponent>();
					int num = i + 1;
					i = num;
				}
			}
			finally
			{
				prefabs.Dispose();
			}
		}
	}

	public EventJournalEntry GetInfo(Entity journalEntity)
	{
		return base.EntityManager.GetComponentData<EventJournalEntry>(journalEntity);
	}

	public Entity GetPrefab(Entity journalEntity)
	{
		return base.EntityManager.GetComponentData<PrefabRef>(journalEntity).m_Prefab;
	}

	public bool TryGetData(Entity journalEntity, out DynamicBuffer<EventJournalData> data)
	{
		if (base.EntityManager.HasComponent<EventJournalData>(journalEntity))
		{
			data = base.EntityManager.GetBuffer<EventJournalData>(journalEntity, isReadOnly: true);
			return true;
		}
		data = default(DynamicBuffer<EventJournalData>);
		return false;
	}

	public bool TryGetCityEffects(Entity journalEntity, out DynamicBuffer<EventJournalCityEffect> data)
	{
		if (base.EntityManager.HasComponent<EventJournalCityEffect>(journalEntity))
		{
			data = base.EntityManager.GetBuffer<EventJournalCityEffect>(journalEntity, isReadOnly: true);
			return true;
		}
		data = default(DynamicBuffer<EventJournalCityEffect>);
		return false;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_StartedJournalQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadWrite<EventJournalEntry>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<EventJournalPending>()
			}
		});
		m_DeletedEventQuery = GetEntityQuery(ComponentType.ReadOnly<JournalEvent>(), ComponentType.ReadOnly<Deleted>());
		m_ActiveJournalEffectQuery = GetEntityQuery(ComponentType.ReadWrite<EventJournalCityEffect>(), ComponentType.Exclude<EventJournalPending>(), ComponentType.Exclude<EventJournalCompleted>());
		m_JournalDataEventQuery = GetEntityQuery(ComponentType.ReadOnly<AddEventJournalData>(), ComponentType.ReadOnly<Game.Common.Event>());
		m_JournalEventPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<EventPrefab>(), ComponentType.ReadOnly<PrefabData>());
		m_LoadedJournalQuery = GetEntityQuery(ComponentType.ReadOnly<EventJournalEntry>(), ComponentType.Exclude<EventJournalPending>());
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_BudgetSystem = base.World.GetOrCreateSystemManaged<BudgetSystem>();
		m_CityServiceBudgetSystem = base.World.GetOrCreateSystemManaged<CityServiceBudgetSystem>();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier5>();
		eventJournal = new NativeList<Entity>(Allocator.Persistent);
		m_Started = new NativeQueue<Entity>(Allocator.Persistent);
		m_Changed = new NativeQueue<Entity>(Allocator.Persistent);
		m_CityEffects = new NativeArray<int>(5, Allocator.Persistent);
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		eventJournal.Clear();
		m_Changed.Clear();
		m_Started.Clear();
		if (!m_LoadedJournalQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray = m_LoadedJournalQuery.ToEntityArray(Allocator.TempJob);
			NativeArray<EventJournalEntry> nativeArray2 = m_LoadedJournalQuery.ToComponentDataArray<EventJournalEntry>(Allocator.TempJob);
			NativeArray<JournalSortingInfo> array = new NativeArray<JournalSortingInfo>(nativeArray.Length, Allocator.TempJob);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				array[i] = new JournalSortingInfo
				{
					m_Entity = nativeArray[i],
					m_StartFrame = nativeArray2[i].m_StartFrame
				};
			}
			nativeArray.Dispose();
			nativeArray2.Dispose();
			array.Sort();
			for (int j = 0; j < array.Length; j++)
			{
				NativeList<Entity> nativeList = eventJournal;
				JournalSortingInfo journalSortingInfo = array[j];
				nativeList.Add(in journalSortingInfo.m_Entity);
			}
			array.Dispose();
		}
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		eventJournal.Dispose();
		m_Changed.Dispose();
		m_Started.Dispose();
		m_CityEffects.Dispose();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		bool flag = false;
		Entity item;
		while (m_Started.TryDequeue(out item))
		{
			eventJournal.Add(in item);
			flag = true;
		}
		if (flag)
		{
			eventEntryAdded?.Invoke();
		}
		Entity item2;
		while (m_Changed.TryDequeue(out item2))
		{
			eventEventDataChanged?.Invoke(item2);
		}
		if ((!m_StartedJournalQuery.IsEmptyIgnoreFilter || !m_ActiveJournalEffectQuery.IsEmptyIgnoreFilter) && base.EntityManager.TryGetComponent<Population>(m_CitySystem.City, out var component) && base.EntityManager.TryGetComponent<Tourism>(m_CitySystem.City, out var component2))
		{
			m_CityEffects[0] = 0;
			m_CityEffects[1] = component.m_AverageHappiness;
			m_CityEffects[2] = m_CityServiceBudgetSystem.GetTotalTaxIncome();
			m_CityEffects[3] = m_BudgetSystem.GetTotalTradeWorth();
			m_CityEffects[4] = component2.m_CurrentTourists;
		}
		if (!m_StartedJournalQuery.IsEmptyIgnoreFilter)
		{
			StartedEventsJob jobData = new StartedEventsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_PendingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_EventJournalPending_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CityEffectType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Events_EventJournalCityEffect_RW_BufferTypeHandle, ref base.CheckedStateRef),
				m_EntryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_EventJournalEntry_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_SimulationFrame = m_SimulationSystem.frameIndex,
				m_CityEffects = m_CityEffects,
				m_Started = m_Started.AsParallelWriter(),
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_StartedJournalQuery, base.Dependency);
			m_ModificationBarrier.AddJobHandleForProducer(base.Dependency);
		}
		if (!m_DeletedEventQuery.IsEmptyIgnoreFilter)
		{
			DeletedEventsJob jobData2 = new DeletedEventsJob
			{
				m_JournalEventType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_JournalEvent_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CompletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_EventJournalCompleted_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EntryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_EventJournalEntry_RW_ComponentLookup, ref base.CheckedStateRef),
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData2, m_DeletedEventQuery, base.Dependency);
			m_ModificationBarrier.AddJobHandleForProducer(base.Dependency);
		}
		if (!m_ActiveJournalEffectQuery.IsEmptyIgnoreFilter)
		{
			CheckJournalTrackingEndJob jobData3 = new CheckJournalTrackingEndJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_EntryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_EventJournalEntry_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_FireData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_Fire_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TargetElementData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Events_TargetElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_OnFireData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_OnFire_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData3, m_ActiveJournalEffectQuery, base.Dependency);
			m_ModificationBarrier.AddJobHandleForProducer(base.Dependency);
			TrackCityEffectsJob jobData4 = new TrackCityEffectsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_CityEffectType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Events_EventJournalCityEffect_RW_BufferTypeHandle, ref base.CheckedStateRef),
				m_CityEffects = m_CityEffects,
				m_Changes = m_Changed.AsParallelWriter()
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData4, m_ActiveJournalEffectQuery, base.Dependency);
		}
		if (!m_JournalDataEventQuery.IsEmptyIgnoreFilter)
		{
			TrackDataJob jobData5 = new TrackDataJob
			{
				m_AddDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_AddEventJournalData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_JournalEvents = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_JournalEvent_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EventJournalDatas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Events_EventJournalData_RW_BufferLookup, ref base.CheckedStateRef),
				m_Changes = m_Changed
			};
			base.Dependency = JobChunkExtensions.Schedule(jobData5, m_JournalDataEventQuery, base.Dependency);
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
	public EventJournalSystem()
	{
	}
}

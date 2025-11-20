using System.Runtime.CompilerServices;
using Game.Citizens;
using Game.Common;
using Game.Events;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class FindEventAttendantsSystem : GameSystemBase
{
	private struct Attend
	{
		public Entity m_Event;

		public Entity m_Participant;
	}

	[BurstCompile]
	private struct AttendJob : IJob
	{
		public NativeQueue<Attend> m_AttendQueue;

		public BufferLookup<TargetElement> m_TargetElements;

		[ReadOnly]
		public NativeList<Entity> m_EventEntities;

		[ReadOnly]
		public ComponentLookup<Duration> m_Durations;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<CalendarEventData> m_CalendarEventDatas;

		[ReadOnly]
		public BufferLookup<HaveCoordinatedMeetingData> m_HaveCoordinatedMeetings;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_Citizens;

		public EntityArchetype m_MeetingArchetype;

		public EntityArchetype m_JournalDataArchetype;

		[ReadOnly]
		public uint m_SimulationFrame;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			Attend item;
			while (m_AttendQueue.TryDequeue(out item))
			{
				if (m_Prefabs.HasComponent(item.m_Event))
				{
					Entity prefab = m_Prefabs[item.m_Event].m_Prefab;
					CalendarEventData calendarEventData = m_CalendarEventDatas[prefab];
					if (!m_HaveCoordinatedMeetings.HasBuffer(prefab))
					{
						continue;
					}
					Entity e = m_CommandBuffer.CreateEntity(m_MeetingArchetype);
					m_CommandBuffer.SetComponent(e, new PrefabRef
					{
						m_Prefab = prefab
					});
					m_CommandBuffer.SetComponent(e, new CoordinatedMeeting
					{
						m_Phase = 0,
						m_Status = MeetingStatus.Waiting,
						m_Target = Entity.Null
					});
					m_CommandBuffer.SetComponent(e, new PrefabRef
					{
						m_Prefab = prefab
					});
					if (calendarEventData.m_RandomTargetType == EventTargetType.Couple && m_Citizens.HasBuffer(item.m_Participant))
					{
						DynamicBuffer<TargetElement> dynamicBuffer = m_TargetElements[item.m_Event];
						DynamicBuffer<HouseholdCitizen> dynamicBuffer2 = m_Citizens[item.m_Participant];
						DynamicBuffer<CoordinatedMeetingAttendee> dynamicBuffer3 = m_CommandBuffer.AddBuffer<CoordinatedMeetingAttendee>(e);
						for (int i = 0; i < dynamicBuffer2.Length; i++)
						{
							dynamicBuffer.Add(new TargetElement
							{
								m_Entity = dynamicBuffer2[i].m_Citizen
							});
							dynamicBuffer3.Add(new CoordinatedMeetingAttendee
							{
								m_Attendee = dynamicBuffer2[i].m_Citizen
							});
						}
						AddJournalData(item.m_Event, dynamicBuffer2.Length);
					}
				}
				else
				{
					UnityEngine.Debug.LogWarning($"Event {item.m_Event} does not have a prefab");
				}
			}
			for (int j = 0; j < m_EventEntities.Length; j++)
			{
				Entity entity = m_EventEntities[j];
				Duration duration = m_Durations[entity];
				if (m_SimulationFrame > duration.m_StartFrame + 240)
				{
					m_CommandBuffer.RemoveComponent<FindingEventParticipants>(entity);
				}
			}
		}

		private void AddJournalData(Entity eventEntity, int count)
		{
			if (eventEntity != Entity.Null && count > 0)
			{
				Entity e = m_CommandBuffer.CreateEntity(m_JournalDataArchetype);
				m_CommandBuffer.SetComponent(e, new AddEventJournalData(eventEntity, EventDataTrackingType.Attendants, count));
			}
		}
	}

	[BurstCompile]
	private struct ConsiderAttendanceJob : IJobChunk
	{
		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public NativeList<Entity> m_EventEntities;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public BufferTypeHandle<HouseholdCitizen> m_HouseholdCitizenType;

		[ReadOnly]
		public ComponentTypeHandle<CommuterHousehold> m_CommuterHouseholdType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<CalendarEventData> m_Events;

		[ReadOnly]
		public ComponentLookup<Citizen> m_CitizenDatas;

		public NativeQueue<Attend>.ParallelWriter m_AttendQueue;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public uint m_UpdateFrameIndex;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeList<Entity> nativeList = new NativeList<Entity>(0, Allocator.Temp);
			for (int i = 0; i < m_EventEntities.Length; i++)
			{
				Entity value = m_EventEntities[i];
				Entity prefab = m_Prefabs[value].m_Prefab;
				if (m_Events[prefab].m_RandomTargetType == EventTargetType.Couple && !chunk.Has(ref m_CommuterHouseholdType))
				{
					nativeList.Add(in value);
				}
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<HouseholdCitizen> bufferAccessor = chunk.GetBufferAccessor(ref m_HouseholdCitizenType);
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int j = 0; j < nativeArray.Length; j++)
			{
				Entity entity = nativeArray[j];
				DynamicBuffer<HouseholdCitizen> dynamicBuffer = bufferAccessor[j];
				int num = 0;
				int num2 = 0;
				for (int k = 0; k < dynamicBuffer.Length; k++)
				{
					Entity citizen = dynamicBuffer[k].m_Citizen;
					switch (m_CitizenDatas[citizen].GetAge())
					{
					case CitizenAge.Child:
						num2++;
						break;
					default:
						num++;
						break;
					case CitizenAge.Teen:
						break;
					}
				}
				if (num < 2 || num2 != 0)
				{
					continue;
				}
				for (int l = 0; l < nativeList.Length; l++)
				{
					Entity entity2 = nativeList[l];
					Entity prefab2 = m_Prefabs[entity2].m_Prefab;
					CalendarEventData calendarEventData = m_Events[prefab2];
					if ((float)random.NextInt(100) < calendarEventData.m_AffectedProbability.min)
					{
						m_AttendQueue.Enqueue(new Attend
						{
							m_Event = nativeList[l],
							m_Participant = entity
						});
					}
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, new AttendingEvent
					{
						m_Event = nativeList[l]
					});
				}
			}
			nativeList.Dispose();
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

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CommuterHousehold> __Game_Citizens_CommuterHousehold_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CalendarEventData> __Game_Prefabs_CalendarEventData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		public BufferLookup<TargetElement> __Game_Events_TargetElement_RW_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Duration> __Game_Events_Duration_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<HaveCoordinatedMeetingData> __Game_Prefabs_HaveCoordinatedMeetingData_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle = state.GetBufferTypeHandle<HouseholdCitizen>(isReadOnly: true);
			__Game_Citizens_CommuterHousehold_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CommuterHousehold>(isReadOnly: true);
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Prefabs_CalendarEventData_RO_ComponentLookup = state.GetComponentLookup<CalendarEventData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Events_TargetElement_RW_BufferLookup = state.GetBufferLookup<TargetElement>();
			__Game_Events_Duration_RO_ComponentLookup = state.GetComponentLookup<Duration>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Prefabs_HaveCoordinatedMeetingData_RO_BufferLookup = state.GetBufferLookup<HaveCoordinatedMeetingData>(isReadOnly: true);
		}
	}

	private const uint UPDATE_INTERVAL = 16u;

	private SimulationSystem m_SimulationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_EventQuery;

	private EntityQuery m_HouseholdQuery;

	private NativeQueue<Attend> m_AttendQueue;

	private EntityArchetype m_MeetingArchetype;

	private EntityArchetype m_JournalDataArchetype;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_EventQuery = GetEntityQuery(ComponentType.ReadWrite<Game.Events.CalendarEvent>(), ComponentType.ReadWrite<FindingEventParticipants>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_HouseholdQuery = GetEntityQuery(ComponentType.ReadWrite<Household>(), ComponentType.ReadWrite<UpdateFrame>(), ComponentType.ReadWrite<HouseholdCitizen>(), ComponentType.Exclude<AttendingEvent>(), ComponentType.Exclude<Deleted>());
		m_AttendQueue = new NativeQueue<Attend>(Allocator.Persistent);
		m_MeetingArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<CoordinatedMeeting>(), ComponentType.ReadWrite<CoordinatedMeetingAttendee>(), ComponentType.ReadWrite<PrefabRef>(), ComponentType.ReadWrite<Created>());
		m_JournalDataArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<AddEventJournalData>(), ComponentType.ReadWrite<Game.Common.Event>());
		RequireForUpdate(m_EventQuery);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_AttendQueue.Dispose();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrameWithInterval = SimulationUtils.GetUpdateFrameWithInterval(m_SimulationSystem.frameIndex, (uint)GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16);
		JobHandle outJobHandle;
		ConsiderAttendanceJob jobData = new ConsiderAttendanceJob
		{
			m_EventEntities = m_EventQuery.ToEntityListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_HouseholdCitizenType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_CommuterHouseholdType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_CommuterHousehold_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameIndex = updateFrameWithInterval,
			m_CitizenDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Events = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CalendarEventData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AttendQueue = m_AttendQueue.AsParallelWriter(),
			m_RandomSeed = RandomSeed.Next(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_HouseholdQuery, JobHandle.CombineDependencies(outJobHandle, base.Dependency));
		AttendJob jobData2 = new AttendJob
		{
			m_EventEntities = m_EventQuery.ToEntityListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
			m_AttendQueue = m_AttendQueue,
			m_TargetElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Events_TargetElement_RW_BufferLookup, ref base.CheckedStateRef),
			m_Durations = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_Duration_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Citizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
			m_HaveCoordinatedMeetings = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_HaveCoordinatedMeetingData_RO_BufferLookup, ref base.CheckedStateRef),
			m_CalendarEventDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CalendarEventData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MeetingArchetype = m_MeetingArchetype,
			m_JournalDataArchetype = m_JournalDataArchetype,
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer()
		};
		base.Dependency = IJobExtensions.Schedule(jobData2, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
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
	public FindEventAttendantsSystem()
	{
	}
}

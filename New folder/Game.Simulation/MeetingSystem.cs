using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Notifications;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class MeetingSystem : GameSystemBase
{
	[BurstCompile]
	private struct MeetingJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public ComponentTypeHandle<CoordinatedMeeting> m_MeetingType;

		[ReadOnly]
		public BufferTypeHandle<CoordinatedMeetingAttendee> m_AttendeeType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public ComponentLookup<HealthProblem> m_HealthProblems;

		[ReadOnly]
		public ComponentLookup<Citizen> m_Citizens;

		[ReadOnly]
		public ComponentLookup<CurrentBuilding> m_CurrentBuildings;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		[ReadOnly]
		public BufferLookup<HaveCoordinatedMeetingData> m_MeetingDatas;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> m_HouseholdMembers;

		[ReadOnly]
		public ComponentLookup<AttendingMeeting> m_AttendingMeetings;

		[ReadOnly]
		public ComponentLookup<CalendarEventData> m_CalendarEvents;

		public RandomSeed m_RandomSeed;

		public uint m_SimulationFrame;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public IconCommandBuffer m_IconCommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<CoordinatedMeeting> nativeArray2 = chunk.GetNativeArray(ref m_MeetingType);
			BufferAccessor<CoordinatedMeetingAttendee> bufferAccessor = chunk.GetBufferAccessor(ref m_AttendeeType);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				CoordinatedMeeting value = nativeArray2[i];
				Entity prefab = nativeArray3[i].m_Prefab;
				DynamicBuffer<HaveCoordinatedMeetingData> dynamicBuffer = m_MeetingDatas[prefab];
				CalendarEventData calendarEventData = default(CalendarEventData);
				if (m_CalendarEvents.HasComponent(prefab))
				{
					calendarEventData = m_CalendarEvents[prefab];
				}
				bool flag = calendarEventData.m_RandomTargetType == EventTargetType.Couple;
				HaveCoordinatedMeetingData haveCoordinatedMeetingData = default(HaveCoordinatedMeetingData);
				if (value.m_Status != MeetingStatus.Done)
				{
					haveCoordinatedMeetingData = dynamicBuffer[value.m_Phase];
				}
				DynamicBuffer<CoordinatedMeetingAttendee> dynamicBuffer2 = bufferAccessor[i];
				Entity entity = value.m_Target;
				if (m_PropertyRenters.HasComponent(entity))
				{
					entity = m_PropertyRenters[entity].m_Property;
				}
				Entity entity2 = default(Entity);
				for (int j = 0; j < dynamicBuffer2.Length; j++)
				{
					Entity attendee = dynamicBuffer2[j].m_Attendee;
					if (!m_Citizens.HasComponent(attendee) || (m_Citizens[attendee].m_State & CitizenFlags.MovingAwayReachOC) != CitizenFlags.None || m_HealthProblems.HasComponent(attendee))
					{
						value.m_Status = MeetingStatus.Done;
						nativeArray2[i] = value;
						break;
					}
					if (flag)
					{
						if (j == 0)
						{
							entity2 = m_HouseholdMembers[attendee].m_Household;
						}
						else if (entity2 != m_HouseholdMembers[attendee].m_Household)
						{
							value.m_Status = MeetingStatus.Done;
							nativeArray2[i] = value;
							break;
						}
					}
				}
				if (value.m_Status == MeetingStatus.Waiting)
				{
					if (value.m_Target != Entity.Null)
					{
						value.m_Status = MeetingStatus.Traveling;
						nativeArray2[i] = value;
					}
				}
				else if (value.m_Status == MeetingStatus.Traveling)
				{
					bool flag2 = false;
					for (int k = 0; k < dynamicBuffer2.Length; k++)
					{
						Entity attendee2 = dynamicBuffer2[k].m_Attendee;
						if (!m_CurrentBuildings.HasComponent(attendee2) || m_CurrentBuildings[attendee2].m_CurrentBuilding != entity)
						{
							flag2 = true;
							break;
						}
					}
					if (!flag2)
					{
						if (haveCoordinatedMeetingData.m_Notification != Entity.Null)
						{
							m_IconCommandBuffer.Add(m_CurrentBuildings[dynamicBuffer2[0].m_Attendee].m_CurrentBuilding, haveCoordinatedMeetingData.m_Notification, IconPriority.Info, IconClusterLayer.Transaction);
						}
						value.m_Status = MeetingStatus.Attending;
						value.m_PhaseEndTime = m_SimulationFrame + m_RandomSeed.GetRandom((int)m_SimulationFrame).NextUInt(haveCoordinatedMeetingData.m_Delay.x, haveCoordinatedMeetingData.m_Delay.y);
						nativeArray2[i] = value;
					}
				}
				else if (value.m_Status == MeetingStatus.Attending)
				{
					bool flag3 = m_SimulationFrame <= value.m_PhaseEndTime;
					if (flag3)
					{
						for (int l = 0; l < dynamicBuffer2.Length; l++)
						{
							Entity attendee3 = dynamicBuffer2[l].m_Attendee;
							if (m_Citizens.HasComponent(attendee3) && (m_Citizens[attendee3].m_State & CitizenFlags.MovingAwayReachOC) == 0 && !m_HealthProblems.HasComponent(attendee3) && (!m_CurrentBuildings.HasComponent(attendee3) || m_CurrentBuildings[attendee3].m_CurrentBuilding != entity))
							{
								flag3 = false;
								break;
							}
						}
					}
					if (!flag3)
					{
						value.m_Phase++;
						if (value.m_Phase >= dynamicBuffer.Length)
						{
							value.m_Status = MeetingStatus.Done;
						}
						else
						{
							value.m_Target = default(Entity);
							value.m_Status = MeetingStatus.Waiting;
						}
						nativeArray2[i] = value;
					}
				}
				else
				{
					if (value.m_Status != MeetingStatus.Done)
					{
						continue;
					}
					m_CommandBuffer.AddComponent<Deleted>(unfilteredChunkIndex, nativeArray[i]);
					for (int m = 0; m < dynamicBuffer2.Length; m++)
					{
						Entity attendee4 = dynamicBuffer2[m].m_Attendee;
						if (m_HouseholdMembers.HasComponent(attendee4))
						{
							entity2 = m_HouseholdMembers[attendee4].m_Household;
							if (m_AttendingMeetings.HasComponent(entity2))
							{
								m_CommandBuffer.RemoveComponent<AttendingMeeting>(unfilteredChunkIndex, entity2);
							}
						}
						if (attendee4 != Entity.Null)
						{
							m_CommandBuffer.RemoveComponent<AttendingMeeting>(unfilteredChunkIndex, attendee4);
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
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public BufferTypeHandle<CoordinatedMeetingAttendee> __Game_Citizens_CoordinatedMeetingAttendee_RO_BufferTypeHandle;

		public ComponentTypeHandle<CoordinatedMeeting> __Game_Citizens_CoordinatedMeeting_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<HaveCoordinatedMeetingData> __Game_Prefabs_HaveCoordinatedMeetingData_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<AttendingMeeting> __Game_Citizens_AttendingMeeting_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CalendarEventData> __Game_Prefabs_CalendarEventData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Citizens_CoordinatedMeetingAttendee_RO_BufferTypeHandle = state.GetBufferTypeHandle<CoordinatedMeetingAttendee>(isReadOnly: true);
			__Game_Citizens_CoordinatedMeeting_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CoordinatedMeeting>();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Citizens_CurrentBuilding_RO_ComponentLookup = state.GetComponentLookup<CurrentBuilding>(isReadOnly: true);
			__Game_Citizens_HealthProblem_RO_ComponentLookup = state.GetComponentLookup<HealthProblem>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Prefabs_HaveCoordinatedMeetingData_RO_BufferLookup = state.GetBufferLookup<HaveCoordinatedMeetingData>(isReadOnly: true);
			__Game_Citizens_AttendingMeeting_RO_ComponentLookup = state.GetComponentLookup<AttendingMeeting>(isReadOnly: true);
			__Game_Citizens_HouseholdMember_RO_ComponentLookup = state.GetComponentLookup<HouseholdMember>(isReadOnly: true);
			__Game_Prefabs_CalendarEventData_RO_ComponentLookup = state.GetComponentLookup<CalendarEventData>(isReadOnly: true);
		}
	}

	private EntityQuery m_MeetingGroup;

	private SimulationSystem m_SimulationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private IconCommandSystem m_IconCommandSystem;

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
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_MeetingGroup = GetEntityQuery(ComponentType.ReadWrite<CoordinatedMeeting>(), ComponentType.ReadWrite<CoordinatedMeetingAttendee>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_MeetingGroup);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		MeetingJob jobData = new MeetingJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_AttendeeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Citizens_CoordinatedMeetingAttendee_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_MeetingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_CoordinatedMeeting_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_Citizens = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentBuildings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HealthProblems = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertyRenters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MeetingDatas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_HaveCoordinatedMeetingData_RO_BufferLookup, ref base.CheckedStateRef),
			m_AttendingMeetings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_AttendingMeeting_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdMembers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CalendarEvents = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CalendarEventData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_MeetingGroup, base.Dependency);
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
	public MeetingSystem()
	{
	}
}

using System.Runtime.CompilerServices;
using Game.Agents;
using Game.Citizens;
using Game.Common;
using Game.Prefabs;
using Game.Simulation;
using Game.Triggers;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Events;

[CompilerGenerated]
public class AddMeetingSystem : GameSystemBase
{
	public struct AddMeeting
	{
		public Entity m_Household;

		public LeisureType m_Type;
	}

	[BurstCompile]
	private struct TravelJob : IJob
	{
		public LeisureParametersData m_LeisureParameters;

		[ReadOnly]
		public BufferLookup<HaveCoordinatedMeetingData> m_HaveCoordinatedMeetings;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		[ReadOnly]
		public ComponentLookup<AttendingEvent> m_AttendingEvents;

		[ReadOnly]
		public ComponentLookup<EventData> m_EventDatas;

		[ReadOnly]
		public ComponentLookup<TouristHousehold> m_TouristHouseholds;

		[ReadOnly]
		public ComponentLookup<Target> m_Targets;

		public NativeQueue<AddMeeting> m_MeetingQueue;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			NativeParallelHashSet<Entity> nativeParallelHashSet = new NativeParallelHashSet<Entity>(4, Allocator.Temp);
			AddMeeting item;
			while (m_MeetingQueue.TryDequeue(out item))
			{
				Entity prefab = m_LeisureParameters.GetPrefab(item.m_Type);
				if (!m_HaveCoordinatedMeetings.HasBuffer(prefab))
				{
					continue;
				}
				Entity entity = item.m_Household;
				if (!m_AttendingEvents.HasComponent(entity) && !nativeParallelHashSet.Contains(entity))
				{
					m_CommandBuffer.AddComponent<AttendingEvent>(entity);
					nativeParallelHashSet.Add(entity);
					Entity entity2 = m_CommandBuffer.CreateEntity(m_EventDatas[prefab].m_Archetype);
					m_CommandBuffer.SetComponent(entity2, new PrefabRef
					{
						m_Prefab = prefab
					});
					DynamicBuffer<TargetElement> dynamicBuffer = m_CommandBuffer.SetBuffer<TargetElement>(entity2);
					DynamicBuffer<HouseholdCitizen> dynamicBuffer2 = m_HouseholdCitizens[entity];
					for (int i = 0; i < dynamicBuffer2.Length; i++)
					{
						Entity citizen = dynamicBuffer2[i].m_Citizen;
						dynamicBuffer.Add(new TargetElement
						{
							m_Entity = citizen
						});
					}
					if (m_TouristHouseholds.HasComponent(entity) && m_TouristHouseholds[entity].m_Hotel == Entity.Null && m_Targets.HasComponent(entity) && m_Targets[entity].m_Target != Entity.Null)
					{
						m_CommandBuffer.SetComponent(entity2, new CoordinatedMeeting
						{
							m_Target = m_Targets[entity].m_Target
						});
					}
				}
			}
			nativeParallelHashSet.Dispose();
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<AttendingEvent> __Game_Events_AttendingEvent_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EventData> __Game_Prefabs_EventData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<HaveCoordinatedMeetingData> __Game_Prefabs_HaveCoordinatedMeetingData_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<TouristHousehold> __Game_Citizens_TouristHousehold_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Target> __Game_Common_Target_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Events_AttendingEvent_RO_ComponentLookup = state.GetComponentLookup<AttendingEvent>(isReadOnly: true);
			__Game_Prefabs_EventData_RO_ComponentLookup = state.GetComponentLookup<EventData>(isReadOnly: true);
			__Game_Prefabs_HaveCoordinatedMeetingData_RO_BufferLookup = state.GetBufferLookup<HaveCoordinatedMeetingData>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Citizens_TouristHousehold_RO_ComponentLookup = state.GetComponentLookup<TouristHousehold>(isReadOnly: true);
			__Game_Common_Target_RO_ComponentLookup = state.GetComponentLookup<Target>(isReadOnly: true);
		}
	}

	private ModificationBarrier1 m_ModificationBarrier;

	private NativeQueue<AddMeeting> m_MeetingQueue;

	private EntityQuery m_LeisureSettingsQuery;

	private EntityArchetype m_JournalDataArchetype;

	private TriggerSystem m_TriggerSystem;

	private CityStatisticsSystem m_CityStatisticsSystem;

	private JobHandle m_Deps;

	private TypeHandle __TypeHandle;

	public NativeQueue<AddMeeting> GetMeetingQueue(out JobHandle deps)
	{
		deps = m_Deps;
		return m_MeetingQueue;
	}

	public void AddWriter(JobHandle reader)
	{
		m_Deps = JobHandle.CombineDependencies(m_Deps, reader);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier1>();
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_LeisureSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<LeisureParametersData>());
		m_JournalDataArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<AddEventJournalData>(), ComponentType.ReadWrite<Game.Common.Event>());
		m_MeetingQueue = new NativeQueue<AddMeeting>(Allocator.Persistent);
		RequireForUpdate(m_LeisureSettingsQuery);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_MeetingQueue.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		TravelJob jobData = new TravelJob
		{
			m_MeetingQueue = m_MeetingQueue,
			m_AttendingEvents = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_AttendingEvent_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EventDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_EventData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HaveCoordinatedMeetings = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_HaveCoordinatedMeetingData_RO_BufferLookup, ref base.CheckedStateRef),
			m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
			m_LeisureParameters = m_LeisureSettingsQuery.GetSingleton<LeisureParametersData>(),
			m_TouristHouseholds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_TouristHousehold_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Targets = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Target_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer()
		};
		base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(m_Deps, base.Dependency));
		AddWriter(base.Dependency);
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
	public AddMeetingSystem()
	{
	}
}

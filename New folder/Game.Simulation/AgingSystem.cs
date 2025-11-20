using System.Runtime.CompilerServices;
using Colossal;
using Colossal.Collections;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Debug;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class AgingSystem : GameSystemBase
{
	[BurstCompile]
	private struct AgingJob : IJobChunk
	{
		public NativeCounter.Concurrent m_BecomeTeenCounter;

		public NativeCounter.Concurrent m_BecomeAdultCounter;

		public NativeCounter.Concurrent m_BecomeElderCounter;

		[ReadOnly]
		public BufferTypeHandle<HouseholdCitizen> m_HouseholdCitizenType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Citizen> m_Citizens;

		[ReadOnly]
		public ComponentLookup<TravelPurpose> m_TravelPurposes;

		[ReadOnly]
		public ComponentLookup<Game.Citizens.Student> m_Students;

		[ReadOnly]
		public uint m_SimulationFrame;

		[ReadOnly]
		public uint m_UpdateFrameIndex;

		[ReadOnly]
		public TimeData m_TimeData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		[ReadOnly]
		public bool m_DebugAgeAllCitizens;

		private void LeaveSchool(int chunkIndex, Entity citizenEntity, ComponentLookup<Game.Citizens.Student> students)
		{
			Entity school = students[citizenEntity].m_School;
			m_CommandBuffer.RemoveComponent<Game.Citizens.Student>(chunkIndex, citizenEntity);
			m_CommandBuffer.AddComponent<StudentsRemoved>(chunkIndex, school);
		}

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (!m_DebugAgeAllCitizens && chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			BufferAccessor<HouseholdCitizen> bufferAccessor = chunk.GetBufferAccessor(ref m_HouseholdCitizenType);
			int day = TimeSystem.GetDay(m_SimulationFrame, m_TimeData);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				DynamicBuffer<HouseholdCitizen> dynamicBuffer = bufferAccessor[i];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					Entity citizen = dynamicBuffer[j].m_Citizen;
					Citizen value = m_Citizens[citizen];
					CitizenAge age = value.GetAge();
					int num = day - value.m_BirthDay;
					int num2;
					if (age == CitizenAge.Child)
					{
						num2 = GetTeenAgeLimitInDays();
					}
					else if (age == CitizenAge.Teen)
					{
						num2 = GetAdultAgeLimitInDays();
					}
					else
					{
						if (age != CitizenAge.Adult)
						{
							continue;
						}
						num2 = GetElderAgeLimitInDays();
					}
					if (num < num2)
					{
						continue;
					}
					switch (age)
					{
					case CitizenAge.Child:
						if (m_Students.HasComponent(citizen))
						{
							LeaveSchool(unfilteredChunkIndex, citizen, m_Students);
						}
						m_BecomeTeenCounter.Increment();
						value.SetAge(CitizenAge.Teen);
						m_Citizens[citizen] = value;
						m_CommandBuffer.SetComponentEnabled<BicycleOwner>(unfilteredChunkIndex, citizen, value: true);
						break;
					case CitizenAge.Teen:
						if (m_Students.HasComponent(citizen))
						{
							LeaveSchool(unfilteredChunkIndex, citizen, m_Students);
						}
						m_CommandBuffer.AddComponent<LeaveHouseholdTag>(unfilteredChunkIndex, citizen);
						m_BecomeAdultCounter.Increment();
						value.SetAge(CitizenAge.Adult);
						m_Citizens[citizen] = value;
						break;
					case CitizenAge.Adult:
						if (m_TravelPurposes.HasComponent(citizen) && (m_TravelPurposes[citizen].m_Purpose == Purpose.GoingToWork || m_TravelPurposes[citizen].m_Purpose == Purpose.Working))
						{
							m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, citizen);
						}
						m_CommandBuffer.RemoveComponent<Worker>(unfilteredChunkIndex, citizen);
						m_BecomeElderCounter.Increment();
						value.SetAge(CitizenAge.Elderly);
						m_Citizens[citizen] = value;
						break;
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
		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<TravelPurpose> __Game_Citizens_TravelPurpose_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Citizens.Student> __Game_Citizens_Student_RO_ComponentLookup;

		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle = state.GetBufferTypeHandle<HouseholdCitizen>(isReadOnly: true);
			__Game_Citizens_TravelPurpose_RO_ComponentLookup = state.GetComponentLookup<TravelPurpose>(isReadOnly: true);
			__Game_Citizens_Student_RO_ComponentLookup = state.GetComponentLookup<Game.Citizens.Student>(isReadOnly: true);
			__Game_Citizens_Citizen_RW_ComponentLookup = state.GetComponentLookup<Citizen>();
		}
	}

	public static readonly int kUpdatesPerDay = 1;

	private EntityQuery m_HouseholdQuery;

	private EntityQuery m_TimeDataQuery;

	private SimulationSystem m_SimulationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	public static bool s_DebugAgeAllCitizens = false;

	[DebugWatchValue]
	public NativeValue<int> m_BecomeTeen;

	[DebugWatchValue]
	public NativeValue<int> m_BecomeAdult;

	[DebugWatchValue]
	public NativeValue<int> m_BecomeElder;

	public NativeCounter m_BecomeTeenCounter;

	public NativeCounter m_BecomeAdultCounter;

	public NativeCounter m_BecomeElderCounter;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / (kUpdatesPerDay * 16);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_HouseholdQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Household>() },
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_TimeDataQuery = GetEntityQuery(ComponentType.ReadOnly<TimeData>());
		m_BecomeTeen = new NativeValue<int>(Allocator.Persistent);
		m_BecomeAdult = new NativeValue<int>(Allocator.Persistent);
		m_BecomeElder = new NativeValue<int>(Allocator.Persistent);
		m_BecomeTeenCounter = new NativeCounter(Allocator.Persistent);
		m_BecomeAdultCounter = new NativeCounter(Allocator.Persistent);
		m_BecomeElderCounter = new NativeCounter(Allocator.Persistent);
		RequireForUpdate(m_HouseholdQuery);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_BecomeTeen.Dispose();
		m_BecomeAdult.Dispose();
		m_BecomeElder.Dispose();
		m_BecomeTeenCounter.Dispose();
		m_BecomeAdultCounter.Dispose();
		m_BecomeElderCounter.Dispose();
	}

	public static int GetTeenAgeLimitInDays()
	{
		return 21;
	}

	public static int GetAdultAgeLimitInDays()
	{
		return 36;
	}

	public static int GetElderAgeLimitInDays()
	{
		return 84;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16);
		AgingJob jobData = new AgingJob
		{
			m_BecomeTeenCounter = m_BecomeTeenCounter.ToConcurrent(),
			m_BecomeAdultCounter = m_BecomeAdultCounter.ToConcurrent(),
			m_BecomeElderCounter = m_BecomeElderCounter.ToConcurrent(),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_HouseholdCitizenType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_TravelPurposes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Students = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Student_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Citizens = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RW_ComponentLookup, ref base.CheckedStateRef),
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_TimeData = m_TimeDataQuery.GetSingleton<TimeData>(),
			m_UpdateFrameIndex = updateFrame,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_DebugAgeAllCitizens = s_DebugAgeAllCitizens
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_HouseholdQuery, base.Dependency);
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
	public AgingSystem()
	{
	}
}

using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Game.Triggers;
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
public class WorkerSystem : GameSystemBase
{
	[BurstCompile]
	private struct GoToWorkJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public ComponentTypeHandle<Citizen> m_CitizenType;

		[ReadOnly]
		public ComponentTypeHandle<Worker> m_WorkerType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentBuilding> m_CurrentBuildingType;

		public BufferTypeHandle<TripNeeded> m_TripType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public ComponentLookup<TravelPurpose> m_Purposes;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_Properties;

		[ReadOnly]
		public ComponentLookup<Building> m_Buildings;

		[ReadOnly]
		public ComponentLookup<CarKeeper> m_CarKeepers;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;

		[ReadOnly]
		public ComponentLookup<AttendingMeeting> m_Attendings;

		[ReadOnly]
		public ComponentLookup<Population> m_PopulationData;

		public NativeQueue<TriggerAction>.ParallelWriter m_TriggerBuffer;

		public uint m_Frame;

		public TimeData m_TimeData;

		public uint m_UpdateFrameIndex;

		public float m_TimeOfDay;

		public Entity m_PopulationEntity;

		public EconomyParameterData m_EconomyParameters;

		public NativeQueue<Entity>.ParallelWriter m_CarReserverQueue;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Citizen> nativeArray2 = chunk.GetNativeArray(ref m_CitizenType);
			NativeArray<Worker> nativeArray3 = chunk.GetNativeArray(ref m_WorkerType);
			NativeArray<CurrentBuilding> nativeArray4 = chunk.GetNativeArray(ref m_CurrentBuildingType);
			BufferAccessor<TripNeeded> bufferAccessor = chunk.GetBufferAccessor(ref m_TripType);
			int population = m_PopulationData[m_PopulationEntity].m_Population;
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Citizen citizen = nativeArray2[i];
				if (IsTodayOffDay(citizen, ref m_EconomyParameters, m_Frame, m_TimeData, population) || !IsTimeToWork(citizen, nativeArray3[i], ref m_EconomyParameters, m_TimeOfDay))
				{
					continue;
				}
				DynamicBuffer<TripNeeded> dynamicBuffer = bufferAccessor[i];
				if (m_Attendings.HasComponent(entity) || (citizen.m_State & CitizenFlags.MovingAwayReachOC) != CitizenFlags.None)
				{
					continue;
				}
				Entity workplace = nativeArray3[i].m_Workplace;
				Entity entity2 = Entity.Null;
				if (m_Properties.HasComponent(workplace))
				{
					entity2 = m_Properties[workplace].m_Property;
				}
				else if (m_Buildings.HasComponent(workplace))
				{
					entity2 = workplace;
				}
				else if (m_OutsideConnections.HasComponent(workplace))
				{
					entity2 = workplace;
				}
				if (entity2 != Entity.Null)
				{
					if (nativeArray4[i].m_CurrentBuilding != entity2)
					{
						if (!m_CarKeepers.IsComponentEnabled(entity))
						{
							m_CarReserverQueue.Enqueue(entity);
						}
						dynamicBuffer.Add(new TripNeeded
						{
							m_TargetAgent = workplace,
							m_Purpose = Purpose.GoingToWork
						});
					}
				}
				else
				{
					citizen.SetFailedEducationCount(0);
					nativeArray2[i] = citizen;
					if (m_Purposes.HasComponent(entity) && (m_Purposes[entity].m_Purpose == Purpose.GoingToWork || m_Purposes[entity].m_Purpose == Purpose.Working))
					{
						m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
					}
					m_CommandBuffer.RemoveComponent<Worker>(unfilteredChunkIndex, entity);
					m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizenBecameUnemployed, Entity.Null, entity, workplace));
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct WorkJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Worker> m_WorkerType;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<TravelPurpose> m_PurposeType;

		[ReadOnly]
		public ComponentTypeHandle<HealthProblem> m_HealthProblemType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		public ComponentTypeHandle<Citizen> m_CitizenType;

		[ReadOnly]
		public ComponentLookup<WorkProvider> m_Workplaces;

		[ReadOnly]
		public ComponentLookup<AttendingMeeting> m_Attendings;

		public EconomyParameterData m_EconomyParameters;

		public NativeQueue<TriggerAction>.ParallelWriter m_TriggerBuffer;

		public float m_TimeOfDay;

		public uint m_UpdateFrameIndex;

		public uint m_Frame;

		public TimeData m_TimeData;

		public int m_Population;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Worker> nativeArray2 = chunk.GetNativeArray(ref m_WorkerType);
			NativeArray<TravelPurpose> nativeArray3 = chunk.GetNativeArray(ref m_PurposeType);
			NativeArray<Citizen> nativeArray4 = chunk.GetNativeArray(ref m_CitizenType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Entity workplace = nativeArray2[i].m_Workplace;
				Worker worker = nativeArray2[i];
				Citizen citizen = nativeArray4[i];
				if (chunk.Has(ref m_HealthProblemType))
				{
					if (nativeArray3[i].m_Purpose == Purpose.Working)
					{
						m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
					}
				}
				else if (!m_Workplaces.HasComponent(workplace))
				{
					citizen.SetFailedEducationCount(0);
					nativeArray4[i] = citizen;
					TravelPurpose travelPurpose = nativeArray3[i];
					if (travelPurpose.m_Purpose == Purpose.GoingToWork || travelPurpose.m_Purpose == Purpose.Working)
					{
						m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
					}
					m_CommandBuffer.RemoveComponent<Worker>(unfilteredChunkIndex, entity);
					m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizenBecameUnemployed, Entity.Null, entity, workplace));
				}
				else if ((!IsTimeToWork(citizen, worker, ref m_EconomyParameters, m_TimeOfDay) || m_Attendings.HasComponent(entity)) && nativeArray3[i].m_Purpose == Purpose.Working)
				{
					m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
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

		public ComponentTypeHandle<Citizen> __Game_Citizens_Citizen_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Worker> __Game_Citizens_Worker_RO_ComponentTypeHandle;

		public BufferTypeHandle<TripNeeded> __Game_Citizens_TripNeeded_RW_BufferTypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarKeeper> __Game_Citizens_CarKeeper_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TravelPurpose> __Game_Citizens_TravelPurpose_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AttendingMeeting> __Game_Citizens_AttendingMeeting_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Population> __Game_City_Population_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<TravelPurpose> __Game_Citizens_TravelPurpose_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<WorkProvider> __Game_Companies_WorkProvider_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Citizens_Citizen_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Citizen>();
			__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentBuilding>(isReadOnly: true);
			__Game_Citizens_Worker_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Worker>(isReadOnly: true);
			__Game_Citizens_TripNeeded_RW_BufferTypeHandle = state.GetBufferTypeHandle<TripNeeded>();
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Citizens_CarKeeper_RO_ComponentLookup = state.GetComponentLookup<CarKeeper>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Objects_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.OutsideConnection>(isReadOnly: true);
			__Game_Citizens_TravelPurpose_RO_ComponentLookup = state.GetComponentLookup<TravelPurpose>(isReadOnly: true);
			__Game_Citizens_AttendingMeeting_RO_ComponentLookup = state.GetComponentLookup<AttendingMeeting>(isReadOnly: true);
			__Game_City_Population_RO_ComponentLookup = state.GetComponentLookup<Population>(isReadOnly: true);
			__Game_Citizens_TravelPurpose_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TravelPurpose>(isReadOnly: true);
			__Game_Citizens_HealthProblem_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HealthProblem>(isReadOnly: true);
			__Game_Companies_WorkProvider_RO_ComponentLookup = state.GetComponentLookup<WorkProvider>(isReadOnly: true);
		}
	}

	private EndFrameBarrier m_EndFrameBarrier;

	private TimeSystem m_TimeSystem;

	private CitizenBehaviorSystem m_CitizenBehaviorSystem;

	private EntityQuery m_EconomyParameterQuery;

	private EntityQuery m_GotoWorkQuery;

	private EntityQuery m_WorkerQuery;

	private EntityQuery m_TimeDataQuery;

	private EntityQuery m_PopulationQuery;

	private SimulationSystem m_SimulationSystem;

	private TriggerSystem m_TriggerSystem;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	public static float GetWorkOffset(Citizen citizen)
	{
		return (float)(-10922 + citizen.GetPseudoRandom(CitizenPseudoRandom.WorkOffset).NextInt(21845)) / 262144f;
	}

	public static bool IsTodayOffDay(Citizen citizen, ref EconomyParameterData economyParameters, uint frame, TimeData timeData, int population)
	{
		int num = math.min(40, Mathf.RoundToInt(100f / math.max(1f, math.sqrt(economyParameters.m_TrafficReduction * (float)population))));
		int day = TimeSystem.GetDay(frame, timeData);
		if (Unity.Mathematics.Random.CreateFromIndex((uint)(citizen.m_PseudoRandom + day)).NextInt(100) > num)
		{
			return true;
		}
		return false;
	}

	public static bool IsTimeToWork(Citizen citizen, Worker worker, ref EconomyParameterData economyParameters, float timeOfDay)
	{
		float2 timeToWork = GetTimeToWork(citizen, worker, ref economyParameters, includeCommute: true);
		if (!(timeToWork.x < timeToWork.y))
		{
			if (!(timeOfDay >= timeToWork.x))
			{
				return timeOfDay <= timeToWork.y;
			}
			return true;
		}
		if (timeOfDay >= timeToWork.x)
		{
			return timeOfDay <= timeToWork.y;
		}
		return false;
	}

	public static float2 GetTimeToWork(Citizen citizen, Worker worker, ref EconomyParameterData economyParameters, bool includeCommute)
	{
		float num = GetWorkOffset(citizen);
		if (worker.m_Shift == Workshift.Evening)
		{
			num += 0.33f;
		}
		else if (worker.m_Shift == Workshift.Night)
		{
			num += 0.67f;
		}
		float num2 = math.frac((float)Mathf.RoundToInt(24f * (economyParameters.m_WorkDayStart + num)) / 24f);
		float y = math.frac((float)Mathf.RoundToInt(24f * (economyParameters.m_WorkDayEnd + num)) / 24f);
		float num3 = 0f;
		if (includeCommute)
		{
			num3 = 60f * worker.m_LastCommuteTime;
			if (num3 < 60f)
			{
				num3 = 40000f;
			}
			num3 /= 262144f;
		}
		return new float2(math.frac(num2 - num3), y);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CitizenBehaviorSystem = base.World.GetOrCreateSystemManaged<CitizenBehaviorSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_TimeSystem = base.World.GetOrCreateSystemManaged<TimeSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
		m_WorkerQuery = GetEntityQuery(ComponentType.ReadOnly<Worker>(), ComponentType.ReadOnly<Citizen>(), ComponentType.ReadOnly<TravelPurpose>(), ComponentType.ReadOnly<CurrentBuilding>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_GotoWorkQuery = GetEntityQuery(ComponentType.ReadOnly<Worker>(), ComponentType.ReadOnly<Citizen>(), ComponentType.ReadOnly<CurrentBuilding>(), ComponentType.Exclude<TravelPurpose>(), ComponentType.Exclude<HealthProblem>(), ComponentType.Exclude<ResourceBuyer>(), ComponentType.ReadWrite<TripNeeded>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		m_TimeDataQuery = GetEntityQuery(ComponentType.ReadOnly<TimeData>());
		m_PopulationQuery = GetEntityQuery(ComponentType.ReadOnly<Population>());
		RequireAnyForUpdate(m_GotoWorkQuery, m_WorkerQuery);
		RequireForUpdate(m_EconomyParameterQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrameWithInterval = SimulationUtils.GetUpdateFrameWithInterval(m_SimulationSystem.frameIndex, (uint)GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16);
		JobHandle deps;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new GoToWorkJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CitizenType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentBuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WorkerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Worker_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TripType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Citizens_TripNeeded_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_Buildings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarKeepers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CarKeeper_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Properties = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OutsideConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Purposes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Attendings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_AttendingMeeting_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PopulationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_Population_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TriggerBuffer = m_TriggerSystem.CreateActionBuffer().AsParallelWriter(),
			m_EconomyParameters = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
			m_TimeOfDay = m_TimeSystem.normalizedTime,
			m_UpdateFrameIndex = updateFrameWithInterval,
			m_Frame = m_SimulationSystem.frameIndex,
			m_TimeData = m_TimeDataQuery.GetSingleton<TimeData>(),
			m_PopulationEntity = m_PopulationQuery.GetSingletonEntity(),
			m_CarReserverQueue = m_CitizenBehaviorSystem.GetCarReserveQueue(out deps),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_GotoWorkQuery, JobHandle.CombineDependencies(base.Dependency, deps));
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
		m_CitizenBehaviorSystem.AddCarReserveWriter(jobHandle);
		m_TriggerSystem.AddActionBufferWriter(jobHandle);
		JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(new WorkJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_WorkerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Worker_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PurposeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_CitizenType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_Attendings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_AttendingMeeting_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HealthProblemType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_Workplaces = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_WorkProvider_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TriggerBuffer = m_TriggerSystem.CreateActionBuffer().AsParallelWriter(),
			m_EconomyParameters = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
			m_UpdateFrameIndex = updateFrameWithInterval,
			m_TimeOfDay = m_TimeSystem.normalizedTime,
			m_Frame = m_SimulationSystem.frameIndex,
			m_TimeData = m_TimeDataQuery.GetSingleton<TimeData>(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_WorkerQuery, JobHandle.CombineDependencies(base.Dependency, jobHandle));
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);
		m_TriggerSystem.AddActionBufferWriter(jobHandle2);
		base.Dependency = jobHandle2;
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
	public WorkerSystem()
	{
	}
}

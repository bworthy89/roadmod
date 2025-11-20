using System.Runtime.CompilerServices;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Economy;
using Game.Prefabs;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class ApplyToSchoolSystem : GameSystemBase
{
	[BurstCompile]
	public struct ApplyToSchoolJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		public ComponentTypeHandle<Citizen> m_CitizenType;

		[ReadOnly]
		public ComponentTypeHandle<Worker> m_WorkerType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<SchoolData> m_SchoolDatas;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> m_HouseholdMembers;

		[ReadOnly]
		public ComponentLookup<Household> m_HouseholdDatas;

		[ReadOnly]
		public BufferLookup<Resources> m_Resources;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		[ReadOnly]
		public BufferLookup<ServiceFee> m_Fees;

		[ReadOnly]
		public ComponentLookup<TouristHousehold> m_TouristHouseholds;

		[ReadOnly]
		public ComponentLookup<MovingAway> m_MovingAways;

		[ReadOnly]
		public ComponentLookup<SchoolSeekerCooldown> m_SchoolSeekerCooldowns;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		public uint m_UpdateFrameIndex;

		public Entity m_City;

		public uint m_SimulationFrame;

		public EconomyParameterData m_EconomyParameters;

		public EducationParameterData m_EducationParameters;

		public TimeData m_TimeData;

		public bool m_DebugFastApplySchool;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (!m_DebugFastApplySchool && chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Citizen> nativeArray2 = chunk.GetNativeArray(ref m_CitizenType);
			NativeArray<Worker> nativeArray3 = chunk.GetNativeArray(ref m_WorkerType);
			DynamicBuffer<CityModifier> cityModifiers = m_CityModifiers[m_City];
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < chunk.Count; i++)
			{
				Citizen value = nativeArray2[i];
				CitizenAge age = value.GetAge();
				if (age == CitizenAge.Elderly || (!m_DebugFastApplySchool && m_SchoolSeekerCooldowns.HasComponent(nativeArray[i]) && m_SimulationFrame < m_SchoolSeekerCooldowns[nativeArray[i]].m_SimulationFrame + kCoolDown))
				{
					continue;
				}
				SchoolLevel schoolLevel = ((age == CitizenAge.Child && !m_DebugFastApplySchool) ? SchoolLevel.Elementary : ((SchoolLevel)(value.GetEducationLevel() + 1)));
				int failedEducationCount = value.GetFailedEducationCount();
				if (failedEducationCount == 0 && age > CitizenAge.Teen && schoolLevel == SchoolLevel.College)
				{
					schoolLevel = SchoolLevel.University;
				}
				bool flag = age == CitizenAge.Child || (age == CitizenAge.Teen && schoolLevel >= SchoolLevel.HighSchool && schoolLevel < SchoolLevel.University) || (age == CitizenAge.Adult && schoolLevel >= SchoolLevel.HighSchool);
				Entity household = m_HouseholdMembers[nativeArray[i]].m_Household;
				if (!m_DebugFastApplySchool && (!flag || !CitizenUtils.HasMovedIn(household, m_HouseholdDatas)))
				{
					continue;
				}
				float willingness = value.GetPseudoRandom(CitizenPseudoRandom.StudyWillingness).NextFloat();
				float enteringProbability = GetEnteringProbability(age, nativeArray3.IsCreated, (int)schoolLevel, value.m_WellBeing, willingness, cityModifiers, ref m_EducationParameters);
				if (m_DebugFastApplySchool || random.NextFloat(1f) < enteringProbability)
				{
					if (m_PropertyRenters.HasComponent(household) && !m_TouristHouseholds.HasComponent(household) && !m_MovingAways.HasComponent(household))
					{
						Entity property = m_PropertyRenters[household].m_Property;
						Entity entity = m_CommandBuffer.CreateEntity(unfilteredChunkIndex);
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, new Owner
						{
							m_Owner = nativeArray[i]
						});
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, new SchoolSeeker
						{
							m_Level = (int)schoolLevel
						});
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, new CurrentBuilding
						{
							m_CurrentBuilding = property
						});
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, nativeArray[i], new HasSchoolSeeker
						{
							m_Seeker = entity
						});
					}
				}
				else if (schoolLevel > SchoolLevel.HighSchool)
				{
					value.SetFailedEducationCount(math.min(3, failedEducationCount + 1));
					nativeArray2[i] = value;
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, nativeArray[i], new SchoolSeekerCooldown
					{
						m_SimulationFrame = m_SimulationFrame
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
		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		public ComponentTypeHandle<Citizen> __Game_Citizens_Citizen_RW_ComponentTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Worker> __Game_Citizens_Worker_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SchoolData> __Game_Prefabs_SchoolData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Resources> __Game_Economy_Resources_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ServiceFee> __Game_City_ServiceFee_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<TouristHousehold> __Game_Citizens_TouristHousehold_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MovingAway> __Game_Agents_MovingAway_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SchoolSeekerCooldown> __Game_Citizens_SchoolSeekerCooldown_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Citizens_Citizen_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Citizen>();
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Citizens_Worker_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Worker>(isReadOnly: true);
			__Game_Citizens_HouseholdMember_RO_ComponentLookup = state.GetComponentLookup<HouseholdMember>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_SchoolData_RO_ComponentLookup = state.GetComponentLookup<SchoolData>(isReadOnly: true);
			__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(isReadOnly: true);
			__Game_Economy_Resources_RO_BufferLookup = state.GetBufferLookup<Resources>(isReadOnly: true);
			__Game_City_ServiceFee_RO_BufferLookup = state.GetBufferLookup<ServiceFee>(isReadOnly: true);
			__Game_Citizens_TouristHousehold_RO_ComponentLookup = state.GetComponentLookup<TouristHousehold>(isReadOnly: true);
			__Game_Agents_MovingAway_RO_ComponentLookup = state.GetComponentLookup<MovingAway>(isReadOnly: true);
			__Game_Citizens_SchoolSeekerCooldown_RO_ComponentLookup = state.GetComponentLookup<SchoolSeekerCooldown>(isReadOnly: true);
		}
	}

	public static readonly int kCoolDown = 20000;

	public const uint UPDATE_INTERVAL = 8192u;

	public bool debugFastApplySchool;

	private EntityQuery m_CitizenGroup;

	private SimulationSystem m_SimulationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private CitySystem m_CitySystem;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_2069025490_0;

	private EntityQuery __query_2069025490_1;

	private EntityQuery __query_2069025490_2;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 512;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_CitizenGroup = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadWrite<Citizen>(),
				ComponentType.ReadOnly<UpdateFrame>()
			},
			None = new ComponentType[5]
			{
				ComponentType.ReadOnly<HealthProblem>(),
				ComponentType.ReadOnly<HasJobSeeker>(),
				ComponentType.ReadOnly<HasSchoolSeeker>(),
				ComponentType.ReadOnly<Game.Citizens.Student>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		RequireForUpdate(m_CitizenGroup);
		RequireForUpdate<EconomyParameterData>();
		RequireForUpdate<TimeData>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrameWithInterval = SimulationUtils.GetUpdateFrameWithInterval(m_SimulationSystem.frameIndex, (uint)GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16);
		ApplyToSchoolJob jobData = new ApplyToSchoolJob
		{
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_CitizenType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_WorkerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Worker_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HouseholdMembers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertyRenters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SchoolDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SchoolData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Resources = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RO_BufferLookup, ref base.CheckedStateRef),
			m_Fees = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_ServiceFee_RO_BufferLookup, ref base.CheckedStateRef),
			m_TouristHouseholds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_TouristHousehold_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MovingAways = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Agents_MovingAway_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SchoolSeekerCooldowns = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_SchoolSeekerCooldown_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_EconomyParameters = __query_2069025490_0.GetSingleton<EconomyParameterData>(),
			m_EducationParameters = __query_2069025490_1.GetSingleton<EducationParameterData>(),
			m_TimeData = __query_2069025490_2.GetSingleton<TimeData>(),
			m_City = m_CitySystem.City,
			m_UpdateFrameIndex = updateFrameWithInterval,
			m_DebugFastApplySchool = debugFastApplySchool,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_CitizenGroup, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
	}

	public static float GetEnteringProbability(CitizenAge age, bool worker, int level, int wellbeing, float willingness, DynamicBuffer<CityModifier> cityModifiers, ref EducationParameterData educationParameterData)
	{
		if (level == 1)
		{
			if (age != CitizenAge.Child)
			{
				return 0f;
			}
			return 1f;
		}
		if (age == CitizenAge.Child || age == CitizenAge.Elderly)
		{
			return 0f;
		}
		if (level == 2)
		{
			if (!(age == CitizenAge.Adult || worker))
			{
				return educationParameterData.m_EnterHighSchoolProbability;
			}
			return educationParameterData.m_AdultEnterHighSchoolProbability;
		}
		float num = (float)wellbeing / 60f * (0.5f + willingness);
		switch (level)
		{
		case 3:
			return 0.5f * (worker ? educationParameterData.m_WorkerContinueEducationProbability : 1f) * math.log(1.6f * num + 1f);
		case 4:
		{
			float value = 0.3f * (worker ? educationParameterData.m_WorkerContinueEducationProbability : 1f) * num;
			CityUtils.ApplyModifier(ref value, cityModifiers, CityModifierType.UniversityInterest);
			return value;
		}
		default:
			return 0f;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<EconomyParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_2069025490_0 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder2 = entityQueryBuilder.WithAll<EducationParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_2069025490_1 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder2 = entityQueryBuilder.WithAll<TimeData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_2069025490_2 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder.Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public ApplyToSchoolSystem()
	{
	}
}

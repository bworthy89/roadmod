using System.Runtime.CompilerServices;
using Colossal;
using Colossal.Collections;
using Colossal.Entities;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Debug;
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
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class BirthSystem : GameSystemBase
{
	[BurstCompile]
	private struct CheckBirthJob : IJobChunk
	{
		public NativeCounter.Concurrent m_DebugBirthCounter;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Citizen> m_CitizenType;

		[ReadOnly]
		public ComponentTypeHandle<HouseholdMember> m_MemberType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		[ReadOnly]
		public ComponentLookup<Citizen> m_Citizens;

		[ReadOnly]
		public ComponentLookup<Game.Citizens.Student> m_Students;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		public uint m_UpdateFrameIndex;

		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public CitizenParametersData m_CitizenParametersData;

		[ReadOnly]
		public NativeList<Entity> m_CitizenPrefabs;

		[ReadOnly]
		public NativeList<ArchetypeData> m_CitizenPrefabArchetypes;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<StatisticsEvent>.ParallelWriter m_StatisticsEventQueue;

		private Entity SpawnBaby(int index, Entity household, ref Random random, Entity building)
		{
			m_DebugBirthCounter.Increment();
			int index2 = random.NextInt(m_CitizenPrefabs.Length);
			Entity prefab = m_CitizenPrefabs[index2];
			ArchetypeData archetypeData = m_CitizenPrefabArchetypes[index2];
			Entity entity = m_CommandBuffer.CreateEntity(index, archetypeData.m_Archetype);
			PrefabRef component = new PrefabRef
			{
				m_Prefab = prefab
			};
			m_CommandBuffer.SetComponent(index, entity, component);
			HouseholdMember component2 = new HouseholdMember
			{
				m_Household = household
			};
			m_CommandBuffer.AddComponent(index, entity, component2);
			Citizen component3 = new Citizen
			{
				m_BirthDay = 0,
				m_State = CitizenFlags.None
			};
			m_CommandBuffer.SetComponent(index, entity, component3);
			m_CommandBuffer.AddComponent(index, entity, new CurrentBuilding
			{
				m_CurrentBuilding = building
			});
			return entity;
		}

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Citizen> nativeArray2 = chunk.GetNativeArray(ref m_CitizenType);
			NativeArray<HouseholdMember> nativeArray3 = chunk.GetNativeArray(ref m_MemberType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Citizen citizen = nativeArray2[i];
				if (citizen.GetAge() != CitizenAge.Adult || (citizen.m_State & (CitizenFlags.Male | CitizenFlags.Tourist | CitizenFlags.Commuter)) != CitizenFlags.None)
				{
					continue;
				}
				Entity household = nativeArray3[i].m_Household;
				Entity entity2 = Entity.Null;
				if (m_PropertyRenters.HasComponent(household))
				{
					entity2 = m_PropertyRenters[household].m_Property;
				}
				if (entity2 == Entity.Null)
				{
					continue;
				}
				DynamicBuffer<HouseholdCitizen> dynamicBuffer = m_HouseholdCitizens[household];
				Entity entity3 = Entity.Null;
				float num = m_CitizenParametersData.m_BaseBirthRate;
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					entity3 = dynamicBuffer[j].m_Citizen;
					if (m_Citizens.HasComponent(entity3))
					{
						Citizen citizen2 = m_Citizens[entity3];
						if ((citizen2.m_State & CitizenFlags.Male) != CitizenFlags.None && citizen2.GetAge() == CitizenAge.Adult)
						{
							num += m_CitizenParametersData.m_AdultFemaleBirthRateBonus;
							break;
						}
					}
				}
				if (m_Students.HasComponent(entity))
				{
					num *= m_CitizenParametersData.m_StudentBirthRateAdjust;
				}
				if (random.NextFloat(1f) < num / (float)kUpdatesPerDay)
				{
					SpawnBaby(unfilteredChunkIndex, household, ref random, entity2);
					m_StatisticsEventQueue.Enqueue(new StatisticsEvent
					{
						m_Statistic = StatisticType.BirthRate,
						m_Change = 1f
					});
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct SumBirthJob : IJob
	{
		public NativeCounter m_DebugBirthCount;

		public NativeValue<int> m_DebugBirth;

		public void Execute()
		{
			m_DebugBirth.value = m_DebugBirthCount.Count;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Citizen> __Game_Citizens_Citizen_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentTypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Game.Citizens.Student> __Game_Citizens_Student_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Citizens_Citizen_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Citizen>(isReadOnly: true);
			__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HouseholdMember>(isReadOnly: true);
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Citizens_Student_RO_ComponentLookup = state.GetComponentLookup<Game.Citizens.Student>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
		}
	}

	public static readonly int kUpdatesPerDay = 16;

	private EndFrameBarrier m_EndFrameBarrier;

	private SimulationSystem m_SimulationSystem;

	private CityStatisticsSystem m_CityStatisticsSystem;

	private TriggerSystem m_TriggerSystem;

	[DebugWatchValue]
	private NativeValue<int> m_DebugBirth;

	private NativeCounter m_DebugBirthCounter;

	private EntityQuery m_CitizenQuery;

	private EntityQuery m_CitizenPrefabQuery;

	private EntityQuery m_CitizenParametersQuery;

	public int m_BirthChance = 20;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / (kUpdatesPerDay * 16);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_DebugBirthCounter = new NativeCounter(Allocator.Persistent);
		m_DebugBirth = new NativeValue<int>(Allocator.Persistent);
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
		m_CitizenQuery = GetEntityQuery(ComponentType.ReadOnly<Citizen>(), ComponentType.ReadOnly<HouseholdMember>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadOnly<CurrentBuilding>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_CitizenPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<CitizenData>(), ComponentType.ReadOnly<ArchetypeData>());
		m_CitizenParametersQuery = GetEntityQuery(ComponentType.ReadOnly<CitizenParametersData>());
		RequireForUpdate(m_CitizenPrefabQuery);
		RequireForUpdate(m_CitizenParametersQuery);
		RequireForUpdate(m_CitizenQuery);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_DebugBirthCounter.Dispose();
		m_DebugBirth.Dispose();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16);
		JobHandle outJobHandle;
		JobHandle outJobHandle2;
		JobHandle deps;
		CheckBirthJob jobData = new CheckBirthJob
		{
			m_DebugBirthCounter = m_DebugBirthCounter.ToConcurrent(),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CitizenType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MemberType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_Citizens = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
			m_Students = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Student_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertyRenters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CitizenPrefabArchetypes = m_CitizenPrefabQuery.ToComponentDataListAsync<ArchetypeData>(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
			m_CitizenPrefabs = m_CitizenPrefabQuery.ToEntityListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle2),
			m_CitizenParametersData = m_CitizenParametersQuery.GetSingleton<CitizenParametersData>(),
			m_RandomSeed = RandomSeed.Next(),
			m_UpdateFrameIndex = updateFrame,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_StatisticsEventQueue = m_CityStatisticsSystem.GetStatisticsEventQueue(out deps).AsParallelWriter()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_CitizenQuery, JobUtils.CombineDependencies(base.Dependency, deps, outJobHandle2, outJobHandle));
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
		m_TriggerSystem.AddActionBufferWriter(base.Dependency);
		m_CityStatisticsSystem.AddWriter(base.Dependency);
		SumBirthJob jobData2 = new SumBirthJob
		{
			m_DebugBirth = m_DebugBirth,
			m_DebugBirthCount = m_DebugBirthCounter
		};
		base.Dependency = IJobExtensions.Schedule(jobData2, base.Dependency);
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
	public BirthSystem()
	{
	}
}

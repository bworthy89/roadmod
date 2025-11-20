using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Events;
using Game.Net;
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
public class CrimeCheckSystem : GameSystemBase
{
	[BurstCompile]
	private struct CrimeCheckJob : IJobChunk
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_EventPrefabChunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Citizen> m_CitizenType;

		[ReadOnly]
		public ComponentTypeHandle<Criminal> m_CriminalType;

		[ReadOnly]
		public ComponentTypeHandle<HouseholdMember> m_HouseholdMemberType;

		[ReadOnly]
		public ComponentTypeHandle<EventData> m_PrefabEventType;

		[ReadOnly]
		public ComponentTypeHandle<CrimeData> m_CrimeDataType;

		[ReadOnly]
		public ComponentTypeHandle<Locked> m_LockedType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenterData;

		[ReadOnly]
		public BufferLookup<Game.Net.ServiceCoverage> m_ServiceCoverages;

		[ReadOnly]
		public ComponentLookup<Population> m_Populations;

		public NativeQueue<TriggerAction>.ParallelWriter m_TriggerBuffer;

		public NativeQueue<StatisticsEvent>.ParallelWriter m_StatisticsEventQueue;

		[ReadOnly]
		public uint m_UpdateFrameIndex;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public PoliceConfigurationData m_PoliceConfigurationData;

		[ReadOnly]
		public Entity m_City;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		[ReadOnly]
		public bool m_DebugFullCrimeMode;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			DynamicBuffer<CityModifier> cityModifiers = m_CityModifiers[m_City];
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Citizen> nativeArray2 = chunk.GetNativeArray(ref m_CitizenType);
			NativeArray<Criminal> nativeArray3 = chunk.GetNativeArray(ref m_CriminalType);
			NativeArray<HouseholdMember> nativeArray4 = chunk.GetNativeArray(ref m_HouseholdMemberType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray[i];
				Citizen citizen = nativeArray2[i];
				CitizenAge age = citizen.GetAge();
				if (age == CitizenAge.Child || age == CitizenAge.Elderly)
				{
					continue;
				}
				bool flag = nativeArray3.Length != 0;
				if (!flag || !(nativeArray3[i].m_Event != Entity.Null))
				{
					Entity entity2 = Entity.Null;
					if (nativeArray4.Length != 0)
					{
						entity2 = nativeArray4[i].m_Household;
					}
					Entity property = Entity.Null;
					if (m_PropertyRenterData.HasComponent(entity2))
					{
						property = m_PropertyRenterData[entity2].m_Property;
					}
					TryAddCrime(unfilteredChunkIndex, ref random, entity, citizen, flag, entity2, property, cityModifiers);
				}
			}
		}

		private void TryAddCrime(int jobIndex, ref Random random, Entity entity, Citizen citizen, bool isCriminal, Entity household, Entity property, DynamicBuffer<CityModifier> cityModifiers)
		{
			float t;
			if (citizen.m_WellBeing <= 25)
			{
				t = (float)(int)citizen.m_WellBeing / 25f;
			}
			else
			{
				t = (float)(100 - citizen.m_WellBeing) / 75f;
				t *= t;
			}
			for (int i = 0; i < m_EventPrefabChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_EventPrefabChunks[i];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<EventData> nativeArray2 = archetypeChunk.GetNativeArray(ref m_PrefabEventType);
				NativeArray<CrimeData> nativeArray3 = archetypeChunk.GetNativeArray(ref m_CrimeDataType);
				EnabledMask enabledMask = archetypeChunk.GetEnabledMask(ref m_LockedType);
				for (int j = 0; j < nativeArray3.Length; j++)
				{
					CrimeData crimeData = nativeArray3[j];
					if (crimeData.m_RandomTargetType != EventTargetType.Citizen || (enabledMask.EnableBit.IsValid && enabledMask[j]))
					{
						continue;
					}
					float num = 0f;
					num = ((!isCriminal) ? math.lerp(crimeData.m_OccurenceProbability.min, crimeData.m_OccurenceProbability.max, t) : math.lerp(crimeData.m_RecurrenceProbability.min, crimeData.m_RecurrenceProbability.max, t));
					CityUtils.ApplyModifier(ref num, cityModifiers, CityModifierType.CrimeProbability);
					float max = math.max((float)m_Populations[m_City].m_Population / m_PoliceConfigurationData.m_CrimePopulationReduction * 100f, 100f);
					if (!m_DebugFullCrimeMode && !(random.NextFloat(max) < num))
					{
						continue;
					}
					if (isCriminal && property != Entity.Null && m_BuildingData.HasComponent(property))
					{
						Building building = m_BuildingData[property];
						if (m_ServiceCoverages.HasBuffer(building.m_RoadEdge))
						{
							float serviceCoverage = NetUtils.GetServiceCoverage(m_ServiceCoverages[building.m_RoadEdge], CoverageService.Welfare, building.m_CurvePosition);
							if (random.NextFloat(max) < serviceCoverage * m_PoliceConfigurationData.m_WelfareCrimeRecurrenceFactor)
							{
								continue;
							}
						}
					}
					m_StatisticsEventQueue.Enqueue(new StatisticsEvent
					{
						m_Statistic = StatisticType.CrimeCount,
						m_Change = 1f
					});
					CreateCrimeEvent(jobIndex, entity, nativeArray[j], nativeArray2[j]);
					return;
				}
			}
		}

		private void CreateCrimeEvent(int jobIndex, Entity targetEntity, Entity eventPrefab, EventData eventData)
		{
			Entity e = m_CommandBuffer.CreateEntity(jobIndex, eventData.m_Archetype);
			m_CommandBuffer.SetComponent(jobIndex, e, new PrefabRef(eventPrefab));
			m_CommandBuffer.SetBuffer<TargetElement>(jobIndex, e).Add(new TargetElement(targetEntity));
			m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizenCommitedCrime, eventPrefab, targetEntity, Entity.Null));
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Citizen> __Game_Citizens_Citizen_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Criminal> __Game_Citizens_Criminal_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<EventData> __Game_Prefabs_EventData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CrimeData> __Game_Prefabs_CrimeData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Locked> __Game_Prefabs_Locked_RO_ComponentTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.ServiceCoverage> __Game_Net_ServiceCoverage_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Population> __Game_City_Population_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Citizens_Citizen_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Citizen>(isReadOnly: true);
			__Game_Citizens_Criminal_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Criminal>(isReadOnly: true);
			__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HouseholdMember>(isReadOnly: true);
			__Game_Prefabs_EventData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EventData>(isReadOnly: true);
			__Game_Prefabs_CrimeData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CrimeData>(isReadOnly: true);
			__Game_Prefabs_Locked_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Locked>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Net_ServiceCoverage_RO_BufferLookup = state.GetBufferLookup<Game.Net.ServiceCoverage>(isReadOnly: true);
			__Game_City_Population_RO_ComponentLookup = state.GetComponentLookup<Population>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
		}
	}

	public readonly int kUpdatesPerDay = 1;

	public bool debugFullCrimeMode;

	private EndFrameBarrier m_EndFrameBarrier;

	private SimulationSystem m_SimulationSystem;

	private CitySystem m_CitySystem;

	private CityStatisticsSystem m_CityStatisticsSystem;

	private EntityQuery m_CitizenQuery;

	private EntityQuery m_EventQuery;

	private EntityQuery m_PoliceConfigurationQuery;

	private TriggerSystem m_TriggerSystem;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / (kUpdatesPerDay * 16);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_CitizenQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Citizen>(),
				ComponentType.ReadOnly<UpdateFrame>()
			},
			None = new ComponentType[5]
			{
				ComponentType.ReadOnly<HealthProblem>(),
				ComponentType.ReadOnly<Worker>(),
				ComponentType.ReadOnly<Game.Citizens.Student>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_EventQuery = GetEntityQuery(ComponentType.ReadWrite<CrimeData>(), ComponentType.Exclude<Locked>());
		m_PoliceConfigurationQuery = GetEntityQuery(ComponentType.ReadOnly<PoliceConfigurationData>(), ComponentType.Exclude<Locked>());
		RequireForUpdate(m_CitizenQuery);
		RequireForUpdate(m_EventQuery);
		RequireForUpdate(m_PoliceConfigurationQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16);
		JobHandle outJobHandle;
		JobHandle deps;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new CrimeCheckJob
		{
			m_EventPrefabChunks = m_EventQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
			m_CitizenType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CriminalType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Criminal_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HouseholdMemberType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabEventType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_EventData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CrimeDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_CrimeData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LockedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_Locked_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertyRenterData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceCoverages = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ServiceCoverage_RO_BufferLookup, ref base.CheckedStateRef),
			m_Populations = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_Population_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TriggerBuffer = m_TriggerSystem.CreateActionBuffer().AsParallelWriter(),
			m_StatisticsEventQueue = m_CityStatisticsSystem.GetStatisticsEventQueue(out deps).AsParallelWriter(),
			m_UpdateFrameIndex = updateFrame,
			m_RandomSeed = RandomSeed.Next(),
			m_PoliceConfigurationData = m_PoliceConfigurationQuery.GetSingleton<PoliceConfigurationData>(),
			m_City = m_CitySystem.City,
			m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
			m_DebugFullCrimeMode = debugFullCrimeMode,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_CitizenQuery, JobHandle.CombineDependencies(base.Dependency, outJobHandle, deps));
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
		m_TriggerSystem.AddActionBufferWriter(jobHandle);
		m_CityStatisticsSystem.AddWriter(jobHandle);
		base.Dependency = jobHandle;
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
	public CrimeCheckSystem()
	{
	}
}

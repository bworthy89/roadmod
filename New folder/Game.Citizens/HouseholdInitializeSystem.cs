using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Agents;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Citizens;

[CompilerGenerated]
public class HouseholdInitializeSystem : GameSystemBase
{
	[BurstCompile]
	private struct InitializeHouseholdJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		public BufferTypeHandle<Resources> m_ResourceType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentBuilding> m_CurrentBuildingType;

		[ReadOnly]
		public ComponentTypeHandle<TouristHousehold> m_TouristHouseholdType;

		[ReadOnly]
		public ComponentTypeHandle<CommuterHousehold> m_CommuterHouseholdType;

		public ComponentTypeHandle<Household> m_HouseholdType;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<HouseholdData> m_HouseholdDatas;

		[ReadOnly]
		public ComponentLookup<DynamicHousehold> m_DynamicHouseholds;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public ComponentLookup<OutsideConnectionData> m_OutsideConnectionDatas;

		[ReadOnly]
		public NativeList<Entity> m_CitizenPrefabs;

		[ReadOnly]
		public NativeList<Entity> m_HouseholdPetPrefabs;

		[ReadOnly]
		public NativeList<ArchetypeData> m_CitizenPrefabArchetypes;

		[ReadOnly]
		public NativeList<ArchetypeData> m_HouseholdPetArchetypes;

		[ReadOnly]
		public PersonalCarSelectData m_PersonalCarSelectData;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public EconomyParameterData m_EconomyParameterData;

		public CityStatisticsSystem.SafeStatisticQueue m_StatisticsQueue;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		private void SpawnCitizen(int index, Entity household, ref Random random, CurrentBuilding building, int age, bool tourist, bool commuter)
		{
			int index2 = random.NextInt(m_CitizenPrefabs.Length);
			Entity prefab = m_CitizenPrefabs[index2];
			ArchetypeData archetypeData = m_CitizenPrefabArchetypes[index2];
			Entity e = m_CommandBuffer.CreateEntity(index, archetypeData.m_Archetype);
			PrefabRef component = new PrefabRef
			{
				m_Prefab = prefab
			};
			m_CommandBuffer.SetComponent(index, e, component);
			HouseholdMember component2 = new HouseholdMember
			{
				m_Household = household
			};
			m_CommandBuffer.AddComponent(index, e, component2);
			CitizenFlags citizenFlags = CitizenFlags.None;
			if (tourist)
			{
				citizenFlags |= CitizenFlags.Tourist;
			}
			if (commuter)
			{
				citizenFlags |= CitizenFlags.Commuter;
			}
			Citizen component3 = new Citizen
			{
				m_BirthDay = (short)age,
				m_State = citizenFlags
			};
			m_CommandBuffer.SetComponent(index, e, component3);
			m_CommandBuffer.AddComponent(index, e, building);
		}

		private void SpawnHouseholdPet(int index, Entity household, ref Random random, CurrentBuilding building)
		{
			int index2 = random.NextInt(m_HouseholdPetPrefabs.Length);
			Entity prefab = m_HouseholdPetPrefabs[index2];
			ArchetypeData archetypeData = m_HouseholdPetArchetypes[index2];
			Entity e = m_CommandBuffer.CreateEntity(index, archetypeData.m_Archetype);
			PrefabRef component = new PrefabRef
			{
				m_Prefab = prefab
			};
			m_CommandBuffer.SetComponent(index, e, component);
			HouseholdPet component2 = new HouseholdPet
			{
				m_Household = household
			};
			m_CommandBuffer.SetComponent(index, e, component2);
			m_CommandBuffer.AddComponent(index, e, building);
		}

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabType);
			BufferAccessor<Resources> bufferAccessor = chunk.GetBufferAccessor(ref m_ResourceType);
			NativeArray<CurrentBuilding> nativeArray3 = chunk.GetNativeArray(ref m_CurrentBuildingType);
			NativeArray<Household> nativeArray4 = chunk.GetNativeArray(ref m_HouseholdType);
			bool tourist = chunk.Has(ref m_TouristHouseholdType);
			bool flag = chunk.Has(ref m_CommuterHouseholdType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Entity prefab = nativeArray2[i].m_Prefab;
				if (m_DynamicHouseholds.HasComponent(prefab))
				{
					continue;
				}
				HouseholdData householdData = m_HouseholdDatas[prefab];
				DynamicBuffer<Resources> resources = bufferAccessor[i];
				int num = ((!chunk.Has(ref m_TouristHouseholdType)) ? (random.NextInt(householdData.m_InitialWealthRange) - householdData.m_InitialWealthRange / 2 + householdData.m_InitialWealthOffset) : (random.NextInt(m_EconomyParameterData.m_TouristInitialWealthRange) - m_EconomyParameterData.m_TouristInitialWealthRange / 2 + m_EconomyParameterData.m_TouristInitialWealthOffset));
				EconomyUtils.AddResources(Resource.Money, num, resources);
				CurrentBuilding building = nativeArray3[i];
				for (int j = 0; j < householdData.m_StudentCount; j++)
				{
					SpawnCitizen(unfilteredChunkIndex, entity, ref random, building, 4, tourist, flag);
				}
				for (int k = 0; k < householdData.m_AdultCount; k++)
				{
					SpawnCitizen(unfilteredChunkIndex, entity, ref random, building, 1, tourist, flag);
				}
				int num2 = random.NextInt(householdData.m_ChildCount);
				for (int l = 0; l < num2; l++)
				{
					SpawnCitizen(unfilteredChunkIndex, entity, ref random, building, 2, tourist, flag);
				}
				for (int m = 0; m < householdData.m_ElderCount; m++)
				{
					SpawnCitizen(unfilteredChunkIndex, entity, ref random, building, 3, tourist, flag);
				}
				int num3 = 0;
				if (!flag && random.NextInt(100) < householdData.m_FirstPetProbability)
				{
					do
					{
						SpawnHouseholdPet(unfilteredChunkIndex, entity, ref random, building);
					}
					while (++num3 < 4 && random.NextInt(100) < householdData.m_NextPetProbability);
					m_CommandBuffer.AddBuffer<HouseholdAnimal>(unfilteredChunkIndex, entity);
				}
				int num4 = householdData.m_AdultCount + num2 + householdData.m_ElderCount;
				bool flag2 = false;
				if ((BuildingUtils.GetOutsideConnectionType(building.m_CurrentBuilding, ref m_PrefabRefs, ref m_OutsideConnectionDatas) & OutsideConnectionTransferType.Road) != OutsideConnectionTransferType.None)
				{
					flag2 = true;
				}
				if (flag2)
				{
					Entity entity2 = entity;
					Entity currentBuilding = nativeArray3[i].m_CurrentBuilding;
					if (m_TransformData.HasComponent(currentBuilding))
					{
						Transform transform = m_TransformData[currentBuilding];
						int num5 = num4;
						int num6 = 1 + num3;
						if (random.NextInt(20) == 0)
						{
							num5 += 5;
							num6 += 5;
						}
						else if (random.NextInt(10) == 0)
						{
							num6 += 5;
							if (random.NextInt(10) == 0)
							{
								num6 += 5;
							}
						}
						Entity entity3 = m_PersonalCarSelectData.CreateVehicle(m_CommandBuffer, unfilteredChunkIndex, ref random, num5, num6, avoidTrailers: false, noSlowVehicles: true, bicycle: false, transform, currentBuilding, Entity.Null, (PersonalCarFlags)0u, stopped: true);
						if (entity3 != Entity.Null)
						{
							m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity3, new Owner(entity2));
							m_CommandBuffer.AddBuffer<OwnedVehicle>(unfilteredChunkIndex, entity2);
						}
					}
				}
				Household value = nativeArray4[i];
				value.m_Resources = random.NextInt(1000 * num4);
				nativeArray4[i] = value;
				if (chunk.Has(ref m_TouristHouseholdType))
				{
					TouristHousehold component = new TouristHousehold
					{
						m_LeavingTime = 0u,
						m_Hotel = Entity.Null
					};
					m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity, component);
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, default(LodgingSeeker));
					m_StatisticsQueue.Enqueue(new StatisticsEvent
					{
						m_Statistic = StatisticType.TouristIncome,
						m_Change = num
					});
				}
				else if (!chunk.Has(ref m_CommuterHouseholdType))
				{
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, default(PropertySeeker));
				}
				m_CommandBuffer.RemoveComponent<CurrentBuilding>(unfilteredChunkIndex, entity);
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
		public ComponentTypeHandle<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public BufferTypeHandle<Resources> __Game_Economy_Resources_RW_BufferTypeHandle;

		public ComponentTypeHandle<TouristHousehold> __Game_Citizens_TouristHousehold_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CommuterHousehold> __Game_Citizens_CommuterHousehold_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Household> __Game_Citizens_Household_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HouseholdData> __Game_Prefabs_HouseholdData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<DynamicHousehold> __Game_Prefabs_DynamicHousehold_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<OutsideConnectionData> __Game_Prefabs_OutsideConnectionData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentBuilding>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Economy_Resources_RW_BufferTypeHandle = state.GetBufferTypeHandle<Resources>();
			__Game_Citizens_TouristHousehold_RW_ComponentTypeHandle = state.GetComponentTypeHandle<TouristHousehold>();
			__Game_Citizens_CommuterHousehold_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CommuterHousehold>(isReadOnly: true);
			__Game_Citizens_Household_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Household>();
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Prefabs_HouseholdData_RO_ComponentLookup = state.GetComponentLookup<HouseholdData>(isReadOnly: true);
			__Game_Prefabs_DynamicHousehold_RO_ComponentLookup = state.GetComponentLookup<DynamicHousehold>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup = state.GetComponentLookup<OutsideConnectionData>(isReadOnly: true);
		}
	}

	private EntityQuery m_CarPrefabGroup;

	private EntityQuery m_CitizenPrefabGroup;

	private EntityQuery m_HouseholdPetPrefabGroup;

	private EntityQuery m_Additions;

	private ModificationBarrier4 m_EndFrameBarrier;

	private CityStatisticsSystem m_CityStatisticsSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private PersonalCarSelectData m_PersonalCarSelectData;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_1887319088_0;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier4>();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_PersonalCarSelectData = new PersonalCarSelectData(this);
		m_CarPrefabGroup = GetEntityQuery(PersonalCarSelectData.GetEntityQueryDesc());
		m_CitizenPrefabGroup = GetEntityQuery(ComponentType.ReadOnly<CitizenData>(), ComponentType.ReadOnly<ArchetypeData>());
		m_HouseholdPetPrefabGroup = GetEntityQuery(ComponentType.ReadOnly<HouseholdPetData>(), ComponentType.ReadOnly<ArchetypeData>());
		m_Additions = GetEntityQuery(ComponentType.ReadWrite<Household>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadWrite<HouseholdCitizen>(), ComponentType.ReadOnly<CurrentBuilding>(), ComponentType.ReadWrite<Resources>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_Additions);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_PersonalCarSelectData.PreUpdate(this, m_CityConfigurationSystem, m_CarPrefabGroup, Allocator.TempJob, out var jobHandle);
		JobHandle outJobHandle;
		JobHandle outJobHandle2;
		JobHandle outJobHandle3;
		JobHandle outJobHandle4;
		JobHandle deps;
		JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(new InitializeHouseholdJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CurrentBuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ResourceType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Economy_Resources_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_TouristHouseholdType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_TouristHousehold_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CommuterHouseholdType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_CommuterHousehold_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HouseholdType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Household_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_HouseholdData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DynamicHouseholds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_DynamicHousehold_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OutsideConnectionDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CitizenPrefabs = m_CitizenPrefabGroup.ToEntityListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
			m_HouseholdPetPrefabs = m_HouseholdPetPrefabGroup.ToEntityListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle2),
			m_CitizenPrefabArchetypes = m_CitizenPrefabGroup.ToComponentDataListAsync<ArchetypeData>(base.World.UpdateAllocator.ToAllocator, out outJobHandle3),
			m_HouseholdPetArchetypes = m_HouseholdPetPrefabGroup.ToComponentDataListAsync<ArchetypeData>(base.World.UpdateAllocator.ToAllocator, out outJobHandle4),
			m_StatisticsQueue = m_CityStatisticsSystem.GetSafeStatisticsQueue(out deps),
			m_EconomyParameterData = __query_1887319088_0.GetSingleton<EconomyParameterData>(),
			m_RandomSeed = RandomSeed.Next(),
			m_PersonalCarSelectData = m_PersonalCarSelectData
		}, m_Additions, JobUtils.CombineDependencies(base.Dependency, outJobHandle, outJobHandle2, outJobHandle3, outJobHandle4, deps, jobHandle));
		m_PersonalCarSelectData.PostUpdate(jobHandle2);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);
		m_CityStatisticsSystem.AddWriter(jobHandle2);
		base.Dependency = jobHandle2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<EconomyParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_1887319088_0 = entityQueryBuilder2.Build(ref state);
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
	public HouseholdInitializeSystem()
	{
	}
}

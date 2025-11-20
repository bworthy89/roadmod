using System.Runtime.CompilerServices;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Game.Triggers;
using Game.Vehicles;
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
public class HouseholdMoveAwaySystem : GameSystemBase
{
	[BurstCompile]
	private struct MoveAwayJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<HouseholdCitizen> m_HouseholdCitizenType;

		[ReadOnly]
		public BufferTypeHandle<Resources> m_ResourceType;

		public ComponentTypeHandle<HomelessHousehold> m_HomelessHouseholdType;

		public ComponentTypeHandle<MovingAway> m_MovingAwayType;

		[ReadOnly]
		public ComponentLookup<Citizen> m_Citizens;

		[ReadOnly]
		public ComponentLookup<Worker> m_Workers;

		[ReadOnly]
		public ComponentLookup<Household> m_Households;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		[ReadOnly]
		public ComponentLookup<HealthProblem> m_HealthProblems;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public ComponentLookup<OutsideConnectionData> m_OutsideConnectionDatas;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> m_OwnedVehicles;

		[ReadOnly]
		public BufferLookup<Renter> m_RenterBufs;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public NativeList<Entity> m_OutsideConnectionEntities;

		public NativeQueue<StatisticsEvent>.ParallelWriter m_StatisticsQueue;

		public NativeQueue<TriggerAction>.ParallelWriter m_TriggerBuffer;

		[ReadOnly]
		public EntityArchetype m_RentEventArchetype;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<MovingAway> nativeArray2 = chunk.GetNativeArray(ref m_MovingAwayType);
			NativeArray<HomelessHousehold> nativeArray3 = chunk.GetNativeArray(ref m_HomelessHouseholdType);
			BufferAccessor<HouseholdCitizen> bufferAccessor = chunk.GetBufferAccessor(ref m_HouseholdCitizenType);
			BufferAccessor<Resources> bufferAccessor2 = chunk.GetBufferAccessor(ref m_ResourceType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				MovingAway value = nativeArray2[i];
				DynamicBuffer<HouseholdCitizen> dynamicBuffer = bufferAccessor[i];
				if (!m_PrefabRefs.HasComponent(value.m_Target))
				{
					value.m_Target = Entity.Null;
					OutsideConnectionTransferType ocTransferType = OutsideConnectionTransferType.Train | OutsideConnectionTransferType.Air | OutsideConnectionTransferType.Ship;
					if (m_OwnedVehicles.TryGetBuffer(entity, out var bufferData) && bufferData.Length > 0)
					{
						ocTransferType = OutsideConnectionTransferType.Road;
					}
					if (!BuildingUtils.GetRandomOutsideConnectionByTransferType(ref m_OutsideConnectionEntities, ref m_OutsideConnectionDatas, ref m_PrefabRefs, random, ocTransferType, out value.m_Target) && m_OutsideConnectionEntities.Length != 0)
					{
						int index = random.NextInt(m_OutsideConnectionEntities.Length);
						value.m_Target = m_OutsideConnectionEntities[index];
					}
					nativeArray2[i] = value;
				}
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					Entity citizen = dynamicBuffer[j].m_Citizen;
					if (m_Workers.HasComponent(citizen))
					{
						m_CommandBuffer.RemoveComponent<Worker>(unfilteredChunkIndex, citizen);
					}
				}
				if (nativeArray3.Length > 0 && m_RenterBufs.HasBuffer(nativeArray3[i].m_TempHome))
				{
					Entity e = m_CommandBuffer.CreateEntity(unfilteredChunkIndex, m_RentEventArchetype);
					m_CommandBuffer.SetComponent(unfilteredChunkIndex, e, new RentersUpdated(nativeArray3[i].m_TempHome));
					nativeArray3[i] = new HomelessHousehold
					{
						m_TempHome = Entity.Null
					};
				}
				bool flag = chunk.Has<TouristHousehold>();
				bool flag2 = true;
				if (dynamicBuffer.Length > 0)
				{
					if (flag)
					{
						flag2 = false;
						for (int k = 0; k < dynamicBuffer.Length; k++)
						{
							Entity citizen2 = dynamicBuffer[k].m_Citizen;
							if (m_Citizens.HasComponent(citizen2) && (m_Citizens[citizen2].m_State & CitizenFlags.MovingAwayReachOC) != CitizenFlags.None)
							{
								flag2 = true;
								break;
							}
						}
					}
					else
					{
						for (int l = 0; l < dynamicBuffer.Length; l++)
						{
							Entity citizen3 = dynamicBuffer[l].m_Citizen;
							if (m_Citizens.HasComponent(citizen3))
							{
								Citizen citizen4 = m_Citizens[citizen3];
								if (!CitizenUtils.IsDead(dynamicBuffer[l].m_Citizen, ref m_HealthProblems) && (citizen4.m_State & CitizenFlags.MovingAwayReachOC) == 0)
								{
									flag2 = false;
								}
							}
						}
					}
				}
				if (!flag2)
				{
					continue;
				}
				if (flag)
				{
					DynamicBuffer<Resources> resources = bufferAccessor2[i];
					int resources2 = EconomyUtils.GetResources(Resource.Money, resources);
					m_StatisticsQueue.Enqueue(new StatisticsEvent
					{
						m_Statistic = StatisticType.TouristIncome,
						m_Change = -resources2
					});
				}
				if ((m_Households[entity].m_Flags & HouseholdFlags.MovedIn) != HouseholdFlags.None)
				{
					m_StatisticsQueue.Enqueue(new StatisticsEvent
					{
						m_Statistic = StatisticType.CitizensMovedAway,
						m_Change = dynamicBuffer.Length
					});
					m_StatisticsQueue.Enqueue(new StatisticsEvent
					{
						m_Statistic = StatisticType.MovedAwayReason,
						m_Change = dynamicBuffer.Length,
						m_Parameter = (int)value.m_Reason
					});
				}
				if (m_PropertyRenters.HasComponent(entity) && m_PropertyRenters[entity].m_Property != Entity.Null)
				{
					foreach (HouseholdCitizen item in dynamicBuffer)
					{
						m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizenMovedOutOfCity, Entity.Null, item.m_Citizen, m_PropertyRenters[entity].m_Property));
					}
				}
				m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, default(Deleted));
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
		public BufferTypeHandle<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Resources> __Game_Economy_Resources_RO_BufferTypeHandle;

		public ComponentTypeHandle<HomelessHousehold> __Game_Citizens_HomelessHousehold_RW_ComponentTypeHandle;

		public ComponentTypeHandle<MovingAway> __Game_Agents_MovingAway_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Worker> __Game_Citizens_Worker_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<OutsideConnectionData> __Game_Prefabs_OutsideConnectionData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle = state.GetBufferTypeHandle<HouseholdCitizen>(isReadOnly: true);
			__Game_Economy_Resources_RO_BufferTypeHandle = state.GetBufferTypeHandle<Resources>(isReadOnly: true);
			__Game_Citizens_HomelessHousehold_RW_ComponentTypeHandle = state.GetComponentTypeHandle<HomelessHousehold>();
			__Game_Agents_MovingAway_RW_ComponentTypeHandle = state.GetComponentTypeHandle<MovingAway>();
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Citizens_Worker_RO_ComponentLookup = state.GetComponentLookup<Worker>(isReadOnly: true);
			__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Citizens_HealthProblem_RO_ComponentLookup = state.GetComponentLookup<HealthProblem>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup = state.GetComponentLookup<OutsideConnectionData>(isReadOnly: true);
			__Game_Vehicles_OwnedVehicle_RO_BufferLookup = state.GetBufferLookup<OwnedVehicle>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(isReadOnly: true);
		}
	}

	private EntityQuery m_MoveAwayGroup;

	private EntityQuery m_OutsideConnectionQuery;

	private EntityArchetype m_RentEventArchetype;

	private EndFrameBarrier m_EndFrameBarrier;

	private CityStatisticsSystem m_CityStatisticsSystem;

	private TriggerSystem m_TriggerSystem;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
		m_MoveAwayGroup = GetEntityQuery(ComponentType.ReadOnly<Household>(), ComponentType.ReadOnly<HouseholdCitizen>(), ComponentType.ReadWrite<MovingAway>(), ComponentType.ReadOnly<Resources>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_OutsideConnectionQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Objects.OutsideConnection>(), ComponentType.Exclude<Game.Objects.ElectricityOutsideConnection>(), ComponentType.Exclude<Game.Objects.WaterPipeOutsideConnection>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_RentEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<RentersUpdated>());
		RequireForUpdate(m_MoveAwayGroup);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		JobHandle deps;
		MoveAwayJob jobData = new MoveAwayJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_HouseholdCitizenType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_ResourceType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Economy_Resources_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_HomelessHouseholdType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_HomelessHousehold_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MovingAwayType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Agents_MovingAway_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_Citizens = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Workers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Households = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertyRenters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HealthProblems = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OutsideConnectionDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnedVehicles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup, ref base.CheckedStateRef),
			m_RenterBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_RentEventArchetype = m_RentEventArchetype,
			m_OutsideConnectionEntities = m_OutsideConnectionQuery.ToEntityListAsync(Allocator.TempJob, out outJobHandle),
			m_StatisticsQueue = m_CityStatisticsSystem.GetStatisticsEventQueue(out deps).AsParallelWriter(),
			m_TriggerBuffer = m_TriggerSystem.CreateActionBuffer().AsParallelWriter(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_MoveAwayGroup, JobHandle.CombineDependencies(base.Dependency, outJobHandle, deps));
		jobData.m_OutsideConnectionEntities.Dispose(base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
		m_CityStatisticsSystem.AddWriter(base.Dependency);
		m_TriggerSystem.AddActionBufferWriter(base.Dependency);
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
	public HouseholdMoveAwaySystem()
	{
	}
}

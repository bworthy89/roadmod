using System.Runtime.CompilerServices;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
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
public class LeaveHouseholdSystem : GameSystemBase
{
	[BurstCompile]
	private struct LeaveHouseholdJob : IJobChunk
	{
		public EntityTypeHandle m_EntityType;

		public ComponentTypeHandle<Citizen> m_CitizenType;

		public ComponentLookup<HouseholdMember> m_HouseholdMembers;

		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		public BufferLookup<Resources> m_ResourcesBufs;

		public CountResidentialPropertySystem.ResidentialPropertyData m_ResidentialPropertyData;

		[ReadOnly]
		public ComponentLookup<Worker> m_Workers;

		[ReadOnly]
		public ComponentLookup<MovingAway> m_MovingAways;

		[ReadOnly]
		public ComponentLookup<ArchetypeData> m_ArchetypeDatas;

		[ReadOnly]
		public DemandParameterData m_DemandParameterData;

		[ReadOnly]
		public ComponentLookup<OutsideConnectionData> m_OutsideConnectionDatas;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public NativeList<Entity> m_HouseholdPrefabs;

		[ReadOnly]
		public NativeList<Entity> m_OutsideConnectionEntities;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Random random = m_RandomSeed.GetRandom(62347);
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Citizen> nativeArray2 = chunk.GetNativeArray(ref m_CitizenType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Citizen component = nativeArray2[i];
				HouseholdMember component2 = m_HouseholdMembers[entity];
				Entity household = component2.m_Household;
				DynamicBuffer<Resources> resources = m_ResourcesBufs[household];
				int resources2 = EconomyUtils.GetResources(Resource.Money, resources);
				if (m_MovingAways.HasComponent(household))
				{
					m_CommandBuffer.RemoveComponent<LeaveHouseholdTag>(unfilteredChunkIndex, entity);
				}
				else
				{
					if (!m_HouseholdCitizens.HasBuffer(household))
					{
						continue;
					}
					DynamicBuffer<HouseholdCitizen> dynamicBuffer = m_HouseholdCitizens[household];
					if (dynamicBuffer.Length <= 0 || resources2 <= kNewHouseholdStartMoney * 2 || !m_Workers.HasComponent(entity))
					{
						continue;
					}
					Entity entity2 = m_HouseholdPrefabs[random.NextInt(m_HouseholdPrefabs.Length)];
					ArchetypeData archetypeData = m_ArchetypeDatas[entity2];
					Entity entity3 = m_CommandBuffer.CreateEntity(unfilteredChunkIndex, archetypeData.m_Archetype);
					m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity3, new PrefabRef
					{
						m_Prefab = entity2
					});
					EconomyUtils.AddResources(Resource.Money, resources2 - kNewHouseholdStartMoney, resources);
					DynamicBuffer<Resources> resources3 = m_CommandBuffer.AddBuffer<Resources>(unfilteredChunkIndex, entity3);
					EconomyUtils.AddResources(Resource.Money, kNewHouseholdStartMoney, resources3);
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						if (dynamicBuffer[j].m_Citizen == entity)
						{
							dynamicBuffer.RemoveAt(j);
							break;
						}
					}
					m_CommandBuffer.SetBuffer<HouseholdCitizen>(unfilteredChunkIndex, entity3).Add(new HouseholdCitizen
					{
						m_Citizen = entity
					});
					Entity result;
					if (math.csum(m_ResidentialPropertyData.m_FreeProperties) > 10)
					{
						m_CommandBuffer.SetComponentEnabled<PropertySeeker>(unfilteredChunkIndex, entity3, value: true);
					}
					else if (m_OutsideConnectionEntities.Length > 0 && BuildingUtils.GetRandomOutsideConnectionByParameters(ref m_OutsideConnectionEntities, ref m_OutsideConnectionDatas, ref m_PrefabRefs, random, m_DemandParameterData.m_CommuterOCSpawnParameters, out result))
					{
						component.m_State |= CitizenFlags.Commuter;
						m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity, component);
						m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity3, new Household
						{
							m_Flags = HouseholdFlags.Commuter
						});
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity3, new CommuterHousehold
						{
							m_OriginalFrom = result
						});
					}
					component2.m_Household = entity3;
					m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity, component2);
					m_CommandBuffer.RemoveComponent<LeaveHouseholdTag>(unfilteredChunkIndex, entity);
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

		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RW_BufferLookup;

		public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RW_ComponentLookup;

		public BufferLookup<Resources> __Game_Economy_Resources_RW_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Worker> __Game_Citizens_Worker_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MovingAway> __Game_Agents_MovingAway_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<OutsideConnectionData> __Game_Prefabs_OutsideConnectionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ArchetypeData> __Game_Prefabs_ArchetypeData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Citizens_Citizen_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Citizen>();
			__Game_Citizens_HouseholdCitizen_RW_BufferLookup = state.GetBufferLookup<HouseholdCitizen>();
			__Game_Citizens_HouseholdMember_RW_ComponentLookup = state.GetComponentLookup<HouseholdMember>();
			__Game_Economy_Resources_RW_BufferLookup = state.GetBufferLookup<Resources>();
			__Game_Citizens_Worker_RO_ComponentLookup = state.GetComponentLookup<Worker>(isReadOnly: true);
			__Game_Agents_MovingAway_RO_ComponentLookup = state.GetComponentLookup<MovingAway>(isReadOnly: true);
			__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup = state.GetComponentLookup<OutsideConnectionData>(isReadOnly: true);
			__Game_Prefabs_ArchetypeData_RO_ComponentLookup = state.GetComponentLookup<ArchetypeData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
		}
	}

	public static readonly int kUpdatesPerDay = 2;

	public static readonly int kNewHouseholdStartMoney = 2000;

	private CountResidentialPropertySystem m_CountResidentialPropertySystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_LeaveHouseholdQuery;

	private EntityQuery m_HouseholdPrefabQuery;

	private EntityQuery m_OutsideConnectionQuery;

	private EntityQuery m_DemandParameterQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / (kUpdatesPerDay * 16);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CountResidentialPropertySystem = base.World.GetOrCreateSystemManaged<CountResidentialPropertySystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_LeaveHouseholdQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Citizen>(),
				ComponentType.ReadOnly<LeaveHouseholdTag>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_OutsideConnectionQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Objects.OutsideConnection>(), ComponentType.Exclude<Game.Objects.ElectricityOutsideConnection>(), ComponentType.Exclude<Game.Objects.WaterPipeOutsideConnection>(), ComponentType.Exclude<Building>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_HouseholdPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<ArchetypeData>(), ComponentType.ReadOnly<HouseholdData>(), ComponentType.ReadOnly<DynamicHousehold>());
		m_DemandParameterQuery = GetEntityQuery(ComponentType.ReadOnly<DemandParameterData>());
		RequireForUpdate(m_LeaveHouseholdQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		JobHandle outJobHandle2;
		LeaveHouseholdJob jobData = new LeaveHouseholdJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CitizenType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RW_BufferLookup, ref base.CheckedStateRef),
			m_HouseholdMembers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HouseholdMember_RW_ComponentLookup, ref base.CheckedStateRef),
			m_ResourcesBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RW_BufferLookup, ref base.CheckedStateRef),
			m_ResidentialPropertyData = m_CountResidentialPropertySystem.GetResidentialPropertyData(),
			m_Workers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MovingAways = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Agents_MovingAway_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OutsideConnectionDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ArchetypeDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ArchetypeData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdPrefabs = m_HouseholdPrefabQuery.ToEntityListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
			m_OutsideConnectionEntities = m_OutsideConnectionQuery.ToEntityListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle2),
			m_PrefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_DemandParameterData = m_DemandParameterQuery.GetSingleton<DemandParameterData>(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		};
		base.Dependency = JobChunkExtensions.Schedule(jobData, m_LeaveHouseholdQuery, JobHandle.CombineDependencies(base.Dependency, outJobHandle, outJobHandle2));
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
	public LeaveHouseholdSystem()
	{
	}
}

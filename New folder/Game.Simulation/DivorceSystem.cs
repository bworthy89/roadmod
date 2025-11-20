using System.Runtime.CompilerServices;
using Colossal;
using Colossal.Collections;
using Game.Agents;
using Game.Citizens;
using Game.Common;
using Game.Debug;
using Game.Economy;
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
public class DivorceSystem : GameSystemBase
{
	[BurstCompile]
	private struct CheckDivorceJob : IJobChunk
	{
		public NativeCounter.Concurrent m_DebugDivorceCount;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public ComponentTypeHandle<Household> m_HouseholdType;

		public BufferTypeHandle<HouseholdCitizen> m_HouseholdCitizenType;

		public BufferTypeHandle<Resources> m_ResourceType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public ComponentLookup<Citizen> m_Citizens;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> m_HouseholdMembers;

		[ReadOnly]
		public ComponentLookup<ArchetypeData> m_ArchetypeDatas;

		[ReadOnly]
		public NativeList<Entity> m_HouseholdPrefabs;

		public uint m_UpdateFrameIndex;

		[ReadOnly]
		public CitizenParametersData m_CitizenParametersData;

		public RandomSeed m_RandomSeed;

		public NativeQueue<TriggerAction>.ParallelWriter m_TriggerBuffer;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		private void Divorce(int index, Entity household, Entity leavingCitizen, Entity stayingCitizen, ref Household oldHouseholdData, ref Random random, DynamicBuffer<HouseholdCitizen> oldCitizenBuffer, DynamicBuffer<Resources> oldResourceBuffer)
		{
			m_DebugDivorceCount.Increment();
			Entity entity = m_HouseholdPrefabs[random.NextInt(m_HouseholdPrefabs.Length)];
			ArchetypeData archetypeData = m_ArchetypeDatas[entity];
			Entity entity2 = m_CommandBuffer.CreateEntity(index, archetypeData.m_Archetype);
			m_CommandBuffer.SetComponent(index, entity2, new Household
			{
				m_Flags = oldHouseholdData.m_Flags,
				m_Resources = oldHouseholdData.m_Resources / 2
			});
			m_CommandBuffer.SetComponentEnabled<PropertySeeker>(index, entity2, value: true);
			m_CommandBuffer.SetComponent(index, entity2, new PrefabRef
			{
				m_Prefab = entity
			});
			oldHouseholdData.m_Resources /= 2;
			HouseholdMember component = m_HouseholdMembers[leavingCitizen];
			component.m_Household = entity2;
			m_CommandBuffer.SetComponent(index, leavingCitizen, component);
			m_CommandBuffer.SetBuffer<HouseholdCitizen>(index, entity2).Add(new HouseholdCitizen
			{
				m_Citizen = leavingCitizen
			});
			int amount = EconomyUtils.GetResources(Resource.Money, oldResourceBuffer) / 2;
			EconomyUtils.SetResources(Resource.Money, oldResourceBuffer, amount);
			DynamicBuffer<Resources> resources = m_CommandBuffer.SetBuffer<Resources>(index, entity2);
			EconomyUtils.SetResources(Resource.Money, resources, amount);
			m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizenDivorced, Entity.Null, leavingCitizen, stayingCitizen));
			m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizenDivorced, Entity.Null, stayingCitizen, leavingCitizen));
			for (int i = 0; i < oldCitizenBuffer.Length; i++)
			{
				if (oldCitizenBuffer[i].m_Citizen == leavingCitizen)
				{
					oldCitizenBuffer.RemoveAt(i);
					break;
				}
			}
		}

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Household> nativeArray2 = chunk.GetNativeArray(ref m_HouseholdType);
			BufferAccessor<HouseholdCitizen> bufferAccessor = chunk.GetBufferAccessor(ref m_HouseholdCitizenType);
			BufferAccessor<Resources> bufferAccessor2 = chunk.GetBufferAccessor(ref m_ResourceType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity household = nativeArray[i];
				DynamicBuffer<HouseholdCitizen> oldCitizenBuffer = bufferAccessor[i];
				int num = 0;
				for (int j = 0; j < oldCitizenBuffer.Length; j++)
				{
					Entity citizen = oldCitizenBuffer[j].m_Citizen;
					CitizenAge age = m_Citizens[citizen].GetAge();
					if (age == CitizenAge.Adult || age == CitizenAge.Elderly)
					{
						num++;
					}
				}
				if (num < 2 || !(random.NextFloat(1f) < m_CitizenParametersData.m_DivorceRate / (float)kUpdatesPerDay))
				{
					continue;
				}
				int num2 = random.NextInt(num);
				Entity entity = Entity.Null;
				for (int k = 0; k < oldCitizenBuffer.Length; k++)
				{
					Entity citizen2 = oldCitizenBuffer[k].m_Citizen;
					CitizenAge age2 = m_Citizens[citizen2].GetAge();
					if (age2 == CitizenAge.Adult || age2 == CitizenAge.Elderly)
					{
						if (num2 == 0)
						{
							entity = citizen2;
							break;
						}
						num2--;
					}
				}
				if (!(entity != Entity.Null))
				{
					continue;
				}
				Household oldHouseholdData = nativeArray2[i];
				Entity stayingCitizen = Entity.Null;
				for (int l = 0; l < oldCitizenBuffer.Length; l++)
				{
					Entity citizen3 = oldCitizenBuffer[l].m_Citizen;
					CitizenAge age3 = m_Citizens[citizen3].GetAge();
					if ((age3 == CitizenAge.Adult || age3 == CitizenAge.Elderly) && citizen3 != entity)
					{
						stayingCitizen = citizen3;
					}
				}
				Divorce(unfilteredChunkIndex, household, entity, stayingCitizen, ref oldHouseholdData, ref random, oldCitizenBuffer, bufferAccessor2[i]);
				nativeArray2[i] = oldHouseholdData;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct SumDivorceJob : IJob
	{
		public NativeCounter m_DebugDivorceCount;

		public NativeValue<int> m_DebugDivorce;

		public void Execute()
		{
			m_DebugDivorce.value = m_DebugDivorceCount.Count;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		public BufferTypeHandle<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RW_BufferTypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		public ComponentTypeHandle<Household> __Game_Citizens_Household_RW_ComponentTypeHandle;

		public BufferTypeHandle<Resources> __Game_Economy_Resources_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ArchetypeData> __Game_Prefabs_ArchetypeData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Citizens_HouseholdCitizen_RW_BufferTypeHandle = state.GetBufferTypeHandle<HouseholdCitizen>();
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Citizens_Household_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Household>();
			__Game_Economy_Resources_RW_BufferTypeHandle = state.GetBufferTypeHandle<Resources>();
			__Game_Citizens_HouseholdMember_RO_ComponentLookup = state.GetComponentLookup<HouseholdMember>(isReadOnly: true);
			__Game_Prefabs_ArchetypeData_RO_ComponentLookup = state.GetComponentLookup<ArchetypeData>(isReadOnly: true);
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
		}
	}

	public static readonly int kUpdatesPerDay = 4;

	private EndFrameBarrier m_EndFrameBarrier;

	private SimulationSystem m_SimulationSystem;

	[DebugWatchValue]
	private NativeValue<int> m_DebugDivorce;

	private NativeCounter m_DebugDivorceCount;

	private EntityQuery m_HouseholdQuery;

	private EntityQuery m_HouseholdPrefabQuery;

	private EntityQuery m_CitizenParametersQuery;

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
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
		m_DebugDivorce = new NativeValue<int>(Allocator.Persistent);
		m_DebugDivorceCount = new NativeCounter(Allocator.Persistent);
		m_HouseholdQuery = GetEntityQuery(ComponentType.ReadOnly<Household>(), ComponentType.ReadOnly<HouseholdCitizen>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<TouristHousehold>(), ComponentType.Exclude<CommuterHousehold>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_HouseholdPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<ArchetypeData>(), ComponentType.ReadOnly<HouseholdData>(), ComponentType.ReadOnly<DynamicHousehold>());
		m_CitizenParametersQuery = GetEntityQuery(ComponentType.ReadOnly<CitizenParametersData>());
		RequireForUpdate(m_HouseholdPrefabQuery);
		RequireForUpdate(m_CitizenParametersQuery);
		RequireForUpdate(m_HouseholdQuery);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_DebugDivorce.Dispose();
		m_DebugDivorceCount.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16);
		JobHandle outJobHandle;
		CheckDivorceJob jobData = new CheckDivorceJob
		{
			m_DebugDivorceCount = m_DebugDivorceCount.ToConcurrent(),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_HouseholdCitizenType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_HouseholdType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Household_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ResourceType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Economy_Resources_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_HouseholdMembers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ArchetypeDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ArchetypeData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Citizens = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdPrefabs = m_HouseholdPrefabQuery.ToEntityListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
			m_RandomSeed = RandomSeed.Next(),
			m_UpdateFrameIndex = updateFrame,
			m_CitizenParametersData = m_CitizenParametersQuery.GetSingleton<CitizenParametersData>(),
			m_TriggerBuffer = m_TriggerSystem.CreateActionBuffer().AsParallelWriter(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_HouseholdQuery, JobHandle.CombineDependencies(outJobHandle, base.Dependency));
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
		m_TriggerSystem.AddActionBufferWriter(base.Dependency);
		SumDivorceJob jobData2 = new SumDivorceJob
		{
			m_DebugDivorce = m_DebugDivorce,
			m_DebugDivorceCount = m_DebugDivorceCount
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
	public DivorceSystem()
	{
	}
}

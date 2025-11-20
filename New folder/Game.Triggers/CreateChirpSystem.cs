#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Triggers;

[CompilerGenerated]
public class CreateChirpSystem : GameSystemBase
{
	[BurstCompile]
	public struct CollectRecentChirpsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public ComponentTypeHandle<Chirp> m_ChirpType;

		[ReadOnly]
		public ComponentLookup<ChirpData> m_ChirpDatas;

		public NativeParallelHashMap<Entity, Entity>.ParallelWriter m_RecentChirps;

		public uint m_SimulationFrame;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabType);
			NativeArray<Chirp> nativeArray3 = chunk.GetNativeArray(ref m_ChirpType);
			for (int i = 0; i < chunk.Count; i++)
			{
				if (nativeArray3[i].m_CreationFrame + 18000 >= m_SimulationFrame)
				{
					Entity prefab = nativeArray2[i].m_Prefab;
					if (m_ChirpDatas.HasComponent(prefab))
					{
						m_RecentChirps.TryAdd(prefab, nativeArray[i]);
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct CreateChirpJob : IJob
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferLookup<TriggerChirpData> m_TriggerChirpData;

		[ReadOnly]
		public ComponentLookup<ChirpData> m_ChirpData;

		[ReadOnly]
		public ComponentLookup<LifePathEventData> m_LifepathEventData;

		[ReadOnly]
		public ComponentLookup<BrandChirpData> m_BrandChirpData;

		[ReadOnly]
		public ComponentLookup<RandomLikeCountData> m_RandomLikeCountData;

		[ReadOnly]
		public ComponentLookup<ServiceChirpData> m_ServiceChirpDatas;

		[ReadOnly]
		public ComponentLookup<Citizen> m_Citizens;

		[ReadOnly]
		public ComponentLookup<HealthProblem> m_HealthProblems;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> m_HouseholdMembers;

		[ReadOnly]
		public ComponentLookup<Household> m_Households;

		[ReadOnly]
		public ComponentLookup<HomelessHousehold> m_HomelessHouseholds;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		[ReadOnly]
		public BufferLookup<Employee> m_Employees;

		[ReadOnly]
		public BufferLookup<LifePathEntry> m_LifepathEntries;

		public EntityCommandBuffer m_CommandBuffer;

		public NativeQueue<ChirpCreationData> m_Queue;

		public NativeParallelHashMap<Entity, Entity> m_RecentChirps;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_RandomCitizenChunks;

		public RandomSeed m_RandomSeed;

		public int m_UneducatedPopulation;

		public int m_EducatedPopulation;

		public uint m_SimulationFrame;

		public void Execute()
		{
			if (m_Queue.IsEmpty())
			{
				return;
			}
			Random random = m_RandomSeed.GetRandom(0);
			ChirpCreationData item;
			while (m_Queue.TryDequeue(out item))
			{
				bool isChirperChirp;
				Entity chirpPrefab = GetChirpPrefab(item.m_TriggerPrefab, ref random, out isChirperChirp);
				if (chirpPrefab == Entity.Null || (isChirperChirp && m_RecentChirps.ContainsKey(chirpPrefab)))
				{
					continue;
				}
				EntityArchetype archetype = GetArchetype(chirpPrefab);
				if (!archetype.Valid)
				{
					continue;
				}
				Entity entity = (isChirperChirp ? FindSender(item.m_Sender, item.m_Target, chirpPrefab, ref random) : item.m_Sender);
				if (entity == Entity.Null)
				{
					continue;
				}
				float num = random.NextFloat(0.2f, 1f);
				float num2 = random.NextFloat(0.001f, 0.03f);
				int viralFactor = random.NextInt(5, 100);
				int num3 = m_EducatedPopulation + m_UneducatedPopulation;
				float continuousFactor = 0.2f;
				if (m_RandomLikeCountData.TryGetComponent(chirpPrefab, out var componentData))
				{
					num2 = random.NextFloat(componentData.m_RandomAmountFactor.x, componentData.m_RandomAmountFactor.y);
					num3 = (int)((float)m_EducatedPopulation * componentData.m_EducatedPercentage + (float)m_UneducatedPopulation * componentData.m_UneducatedPercentage);
					viralFactor = random.NextInt(componentData.m_GoViralFactor.x, componentData.m_GoViralFactor.y + 1);
					num = random.NextFloat(m_RandomLikeCountData[chirpPrefab].m_ActiveDays.x, m_RandomLikeCountData[chirpPrefab].m_ActiveDays.y);
					continuousFactor = componentData.m_ContinuousFactor;
				}
				int targetLikes = (int)((float)num3 * num2);
				Entity entity2 = m_CommandBuffer.CreateEntity(archetype);
				m_CommandBuffer.SetComponent(entity2, new Chirp(entity, m_SimulationFrame)
				{
					m_TargetLikes = (uint)targetLikes,
					m_InactiveFrame = (uint)((float)m_SimulationFrame + num * 262144f),
					m_ViralFactor = viralFactor,
					m_ContinuousFactor = continuousFactor,
					m_Likes = (uint)math.min(num3, random.NextInt(5))
				});
				m_CommandBuffer.SetComponent(entity2, new PrefabRef(chirpPrefab));
				DynamicBuffer<ChirpEntity> dynamicBuffer = m_CommandBuffer.AddBuffer<ChirpEntity>(entity2);
				if (entity != Entity.Null)
				{
					dynamicBuffer.Add(new ChirpEntity(entity));
				}
				if (m_LifepathEventData.TryGetComponent(chirpPrefab, out var componentData2) && componentData2.m_EventType == LifePathEventType.CitizenCoupleMadeBaby && m_HouseholdMembers.TryGetComponent(item.m_Sender, out var componentData3) && m_HouseholdCitizens.TryGetBuffer(componentData3.m_Household, out var bufferData))
				{
					for (int i = 0; i < bufferData.Length; i++)
					{
						Entity citizen = bufferData[i].m_Citizen;
						if (m_Citizens.HasComponent(citizen) && m_Citizens[citizen].GetAge() == CitizenAge.Adult && bufferData[i].m_Citizen != entity)
						{
							dynamicBuffer.Add(new ChirpEntity(bufferData[i].m_Citizen));
							break;
						}
					}
				}
				if (item.m_Target != Entity.Null)
				{
					dynamicBuffer.Add(new ChirpEntity(item.m_Target));
				}
				if (isChirperChirp)
				{
					m_RecentChirps.Add(chirpPrefab, entity2);
				}
				else if (m_LifepathEntries.HasBuffer(entity))
				{
					m_CommandBuffer.AppendToBuffer(entity, new LifePathEntry(entity2));
				}
			}
		}

		private Entity GetChirpPrefab(Entity triggerPrefab, ref Random random, out bool isChirperChirp)
		{
			if (m_TriggerChirpData.TryGetBuffer(triggerPrefab, out var bufferData))
			{
				isChirperChirp = true;
				if (bufferData.Length <= 0)
				{
					return Entity.Null;
				}
				return bufferData[random.NextInt(bufferData.Length)].m_Chirp;
			}
			isChirperChirp = false;
			return triggerPrefab;
		}

		private EntityArchetype GetArchetype(Entity prefab)
		{
			if (m_ChirpData.TryGetComponent(prefab, out var componentData))
			{
				return componentData.m_Archetype;
			}
			if (m_LifepathEventData.TryGetComponent(prefab, out var componentData2))
			{
				return componentData2.m_ChirpArchetype;
			}
			return default(EntityArchetype);
		}

		private Entity FindSender(Entity sender, Entity target, Entity prefab, ref Random random)
		{
			if (m_ServiceChirpDatas.TryGetComponent(prefab, out var componentData))
			{
				return componentData.m_Account;
			}
			if (m_BrandChirpData.HasComponent(prefab))
			{
				return sender;
			}
			if (m_Employees.TryGetBuffer(sender, out var bufferData) && bufferData.Length > 0)
			{
				return SelectRandomSender(bufferData, ref random);
			}
			if (m_HouseholdCitizens.TryGetBuffer(sender, out var bufferData2) && bufferData2.Length > 0)
			{
				return SelectRandomSender(bufferData2, ref random);
			}
			return SelectRandomSender(ref random);
		}

		private Entity SelectRandomSender(DynamicBuffer<Employee> employees, ref Random random)
		{
			Entity result = Entity.Null;
			int num = 0;
			for (int i = 0; i < employees.Length; i++)
			{
				Entity worker = employees[i].m_Worker;
				if (m_Citizens.HasComponent(worker) && !CitizenUtils.IsDead(worker, ref m_HealthProblems) && random.NextInt(++num) == 0)
				{
					result = worker;
				}
			}
			return result;
		}

		private Entity SelectRandomSender(DynamicBuffer<HouseholdCitizen> citizens, ref Random random)
		{
			Entity result = Entity.Null;
			int num = 0;
			for (int i = 0; i < citizens.Length; i++)
			{
				Entity citizen = citizens[i].m_Citizen;
				if (m_Citizens.HasComponent(citizen) && !CitizenUtils.IsDead(citizen, ref m_HealthProblems) && random.NextInt(++num) == 0)
				{
					result = citizen;
				}
			}
			return result;
		}

		private Entity SelectRandomSender(ref Random random)
		{
			NativeArray<ArchetypeChunk> nativeArray = m_RandomCitizenChunks.AsArray();
			int length = nativeArray.Length;
			if (length != 0)
			{
				for (int i = 0; i < 100; i++)
				{
					NativeArray<Entity> nativeArray2 = nativeArray[random.NextInt(length)].GetNativeArray(m_EntityType);
					Entity entity = nativeArray2[random.NextInt(nativeArray2.Length)];
					if (!CitizenUtils.IsDead(entity, ref m_HealthProblems) && CitizenUtils.HasMovedIn(entity, ref m_HouseholdMembers, ref m_Households, ref m_HomelessHouseholds) && m_Citizens.TryGetComponent(entity, out var componentData) && componentData.GetAge() == CitizenAge.Adult)
					{
						return entity;
					}
				}
				int num = random.NextInt(length);
				for (int j = 0; j < length; j++)
				{
					ArchetypeChunk archetypeChunk = nativeArray[num++];
					num = math.select(num, 0, num == length);
					NativeArray<Entity> nativeArray3 = archetypeChunk.GetNativeArray(m_EntityType);
					int length2 = nativeArray3.Length;
					int num2 = random.NextInt(length2);
					for (int k = 0; k < length2; k++)
					{
						Entity entity2 = nativeArray3[num2++];
						num2 = math.select(num2, 0, num2 == length2);
						if (!CitizenUtils.IsDead(entity2, ref m_HealthProblems) && CitizenUtils.HasMovedIn(entity2, ref m_HouseholdMembers, ref m_Households, ref m_HomelessHouseholds) && m_Citizens.TryGetComponent(entity2, out var componentData2) && componentData2.GetAge() == CitizenAge.Adult)
						{
							return entity2;
						}
					}
				}
			}
			return Entity.Null;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Chirp> __Game_Triggers_Chirp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<ChirpData> __Game_Prefabs_ChirpData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<TriggerChirpData> __Game_Prefabs_TriggerChirpData_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<LifePathEventData> __Game_Prefabs_LifePathEventData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BrandChirpData> __Game_Prefabs_BrandChirpData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RandomLikeCountData> __Game_Prefabs_RandomLikeCountData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceChirpData> __Game_Prefabs_ServiceChirpData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HomelessHousehold> __Game_Citizens_HomelessHousehold_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Employee> __Game_Companies_Employee_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LifePathEntry> __Game_Triggers_LifePathEntry_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Triggers_Chirp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Chirp>(isReadOnly: true);
			__Game_Prefabs_ChirpData_RO_ComponentLookup = state.GetComponentLookup<ChirpData>(isReadOnly: true);
			__Game_Prefabs_TriggerChirpData_RO_BufferLookup = state.GetBufferLookup<TriggerChirpData>(isReadOnly: true);
			__Game_Prefabs_LifePathEventData_RO_ComponentLookup = state.GetComponentLookup<LifePathEventData>(isReadOnly: true);
			__Game_Prefabs_BrandChirpData_RO_ComponentLookup = state.GetComponentLookup<BrandChirpData>(isReadOnly: true);
			__Game_Prefabs_RandomLikeCountData_RO_ComponentLookup = state.GetComponentLookup<RandomLikeCountData>(isReadOnly: true);
			__Game_Prefabs_ServiceChirpData_RO_ComponentLookup = state.GetComponentLookup<ServiceChirpData>(isReadOnly: true);
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Citizens_HealthProblem_RO_ComponentLookup = state.GetComponentLookup<HealthProblem>(isReadOnly: true);
			__Game_Citizens_HouseholdMember_RO_ComponentLookup = state.GetComponentLookup<HouseholdMember>(isReadOnly: true);
			__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(isReadOnly: true);
			__Game_Citizens_HomelessHousehold_RO_ComponentLookup = state.GetComponentLookup<HomelessHousehold>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Companies_Employee_RO_BufferLookup = state.GetBufferLookup<Employee>(isReadOnly: true);
			__Game_Triggers_LifePathEntry_RO_BufferLookup = state.GetBufferLookup<LifePathEntry>(isReadOnly: true);
		}
	}

	private SimulationSystem m_SimulationSystem;

	private CityStatisticsSystem m_CityStatisticsSystem;

	private ModificationEndBarrier m_ModificationBarrier;

	private JobHandle m_WriteDependencies;

	private EntityQuery m_PrefabQuery;

	private EntityQuery m_ChirpQuery;

	private EntityQuery m_CitizenQuery;

	private NativeQueue<ChirpCreationData> m_Queue;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationEndBarrier>();
		m_PrefabQuery = GetEntityQuery(ComponentType.ReadOnly<ChirpData>());
		m_ChirpQuery = GetEntityQuery(ComponentType.ReadOnly<Chirp>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_CitizenQuery = GetEntityQuery(ComponentType.ReadOnly<Citizen>(), ComponentType.ReadOnly<HouseholdMember>(), ComponentType.Exclude<Deleted>());
		m_Queue = new NativeQueue<ChirpCreationData>(Allocator.Persistent);
		base.Enabled = false;
	}

	protected override void OnGamePreload(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
	{
		base.OnGamePreload(purpose, mode);
		base.Enabled = mode.IsGame();
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_WriteDependencies.Complete();
		m_Queue.Dispose();
		base.OnDestroy();
	}

	public NativeQueue<ChirpCreationData> GetQueue(out JobHandle deps)
	{
		Assert.IsTrue(base.Enabled, "Can not write to queue when system isn't running");
		deps = m_WriteDependencies;
		return m_Queue;
	}

	public void AddQueueWriter(JobHandle handle)
	{
		m_WriteDependencies = JobHandle.CombineDependencies(m_WriteDependencies, handle);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		int capacity = m_PrefabQuery.CalculateEntityCount();
		NativeParallelHashMap<Entity, Entity> recentChirps = new NativeParallelHashMap<Entity, Entity>(capacity, Allocator.TempJob);
		JobHandle job = default(JobHandle);
		if (!m_ChirpQuery.IsEmptyIgnoreFilter)
		{
			job = JobChunkExtensions.ScheduleParallel(new CollectRecentChirpsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ChirpType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Triggers_Chirp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ChirpDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ChirpData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_RecentChirps = recentChirps.AsParallelWriter(),
				m_SimulationFrame = m_SimulationSystem.frameIndex
			}, m_ChirpQuery, base.Dependency);
		}
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> randomCitizenChunks = m_CitizenQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		CreateChirpJob jobData = new CreateChirpJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TriggerChirpData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_TriggerChirpData_RO_BufferLookup, ref base.CheckedStateRef),
			m_ChirpData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ChirpData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LifepathEventData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_LifePathEventData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BrandChirpData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BrandChirpData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RandomLikeCountData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RandomLikeCountData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceChirpDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ServiceChirpData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Citizens = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HealthProblems = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdMembers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Households = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HomelessHouseholds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HomelessHousehold_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
			m_Employees = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_Employee_RO_BufferLookup, ref base.CheckedStateRef),
			m_LifepathEntries = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Triggers_LifePathEntry_RO_BufferLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer(),
			m_Queue = m_Queue,
			m_RecentChirps = recentChirps,
			m_RandomCitizenChunks = randomCitizenChunks,
			m_RandomSeed = RandomSeed.Next(),
			m_UneducatedPopulation = m_CityStatisticsSystem.GetStatisticValue(StatisticType.EducationCount) + m_CityStatisticsSystem.GetStatisticValue(StatisticType.EducationCount, 1),
			m_EducatedPopulation = m_CityStatisticsSystem.GetStatisticValue(StatisticType.EducationCount, 2) + m_CityStatisticsSystem.GetStatisticValue(StatisticType.EducationCount, 3) + m_CityStatisticsSystem.GetStatisticValue(StatisticType.EducationCount, 4),
			m_SimulationFrame = m_SimulationSystem.frameIndex
		};
		base.Dependency = IJobExtensions.Schedule(jobData, JobUtils.CombineDependencies(base.Dependency, m_WriteDependencies, outJobHandle, job));
		recentChirps.Dispose(base.Dependency);
		randomCitizenChunks.Dispose(base.Dependency);
		m_WriteDependencies = base.Dependency;
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
	public CreateChirpSystem()
	{
	}
}

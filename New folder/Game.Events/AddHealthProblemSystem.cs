using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Notifications;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Simulation;
using Game.Triggers;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Events;

[CompilerGenerated]
public class AddHealthProblemSystem : GameSystemBase
{
	[BurstCompile]
	private struct FindCitizensInBuildingJob : IJobChunk
	{
		[ReadOnly]
		public Entity m_Event;

		[ReadOnly]
		public Entity m_Building;

		[ReadOnly]
		public HealthProblemFlags m_Flags;

		[ReadOnly]
		public float m_DeathProbability;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentBuilding> m_CurrentBuildingType;

		[ReadOnly]
		public ComponentTypeHandle<HouseholdMember> m_HouseholdMemberType;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		public NativeQueue<AddHealthProblem>.ParallelWriter m_AddQueue;

		public NativeQueue<Game.City.StatisticsEvent>.ParallelWriter m_StatisticsEventQueue;

		public NativeQueue<TriggerAction>.ParallelWriter m_TriggerBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<CurrentBuilding> nativeArray2 = chunk.GetNativeArray(ref m_CurrentBuildingType);
			NativeArray<HouseholdMember> nativeArray3 = chunk.GetNativeArray(ref m_HouseholdMemberType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				if (nativeArray2[i].m_CurrentBuilding == m_Building)
				{
					AddHealthProblem value = new AddHealthProblem
					{
						m_Event = m_Event,
						m_Target = nativeArray[i],
						m_Flags = m_Flags
					};
					if (m_DeathProbability > 0f && random.NextFloat(1f) < m_DeathProbability)
					{
						value.m_Flags |= HealthProblemFlags.Dead | HealthProblemFlags.RequireTransport;
						Entity household = ((nativeArray3.Length != 0) ? nativeArray3[i].m_Household : Entity.Null);
						DeathCheckSystem.PerformAfterDeathActions(nativeArray[i], household, m_TriggerBuffer, m_StatisticsEventQueue, ref m_HouseholdCitizens);
					}
					m_AddQueue.Enqueue(value);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct AddHealthProblemJob : IJob
	{
		[DeallocateOnJobCompletion]
		[ReadOnly]
		public NativeArray<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public ComponentTypeHandle<AddHealthProblem> m_AddHealthProblemType;

		[ReadOnly]
		public ComponentLookup<CurrentTransport> m_CurrentTransportData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public HealthcareParameterData m_HealthcareParameterData;

		public IconCommandBuffer m_IconCommandBuffer;

		public ComponentLookup<HealthProblem> m_HealthProblemData;

		public ComponentLookup<PathOwner> m_PathOwnerData;

		public ComponentLookup<Target> m_TargetData;

		public BufferLookup<TargetElement> m_TargetElements;

		public EntityArchetype m_JournalDataArchetype;

		public NativeQueue<AddHealthProblem> m_AddQueue;

		public EntityCommandBuffer m_CommandBuffer;

		public NativeQueue<TriggerAction> m_TriggerBuffer;

		public void Execute()
		{
			int count = m_AddQueue.Count;
			int num = count;
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				num += m_Chunks[i].Count;
			}
			NativeParallelHashMap<Entity, HealthProblem> nativeParallelHashMap = new NativeParallelHashMap<Entity, HealthProblem>(num, Allocator.Temp);
			for (int j = 0; j < m_Chunks.Length; j++)
			{
				NativeArray<AddHealthProblem> nativeArray = m_Chunks[j].GetNativeArray(ref m_AddHealthProblemType);
				for (int k = 0; k < nativeArray.Length; k++)
				{
					AddHealthProblem addHealthProblem = nativeArray[k];
					if (m_PrefabRefData.HasComponent(addHealthProblem.m_Target))
					{
						HealthProblem healthProblem = new HealthProblem(addHealthProblem.m_Event, addHealthProblem.m_Flags);
						if (nativeParallelHashMap.TryGetValue(addHealthProblem.m_Target, out var item))
						{
							nativeParallelHashMap[addHealthProblem.m_Target] = MergeProblems(item, healthProblem);
						}
						else if (m_HealthProblemData.HasComponent(addHealthProblem.m_Target))
						{
							item = m_HealthProblemData[addHealthProblem.m_Target];
							nativeParallelHashMap.TryAdd(addHealthProblem.m_Target, MergeProblems(item, healthProblem));
						}
						else
						{
							nativeParallelHashMap.TryAdd(addHealthProblem.m_Target, healthProblem);
						}
					}
				}
			}
			for (int l = 0; l < count; l++)
			{
				AddHealthProblem addHealthProblem2 = m_AddQueue.Dequeue();
				if (m_PrefabRefData.HasComponent(addHealthProblem2.m_Target))
				{
					HealthProblem healthProblem2 = new HealthProblem(addHealthProblem2.m_Event, addHealthProblem2.m_Flags);
					if (nativeParallelHashMap.TryGetValue(addHealthProblem2.m_Target, out var item2))
					{
						nativeParallelHashMap[addHealthProblem2.m_Target] = MergeProblems(item2, healthProblem2);
					}
					else if (m_HealthProblemData.HasComponent(addHealthProblem2.m_Target))
					{
						item2 = m_HealthProblemData[addHealthProblem2.m_Target];
						nativeParallelHashMap.TryAdd(addHealthProblem2.m_Target, MergeProblems(item2, healthProblem2));
					}
					else
					{
						nativeParallelHashMap.TryAdd(addHealthProblem2.m_Target, healthProblem2);
					}
				}
			}
			if (nativeParallelHashMap.Count() != 0)
			{
				NativeArray<Entity> keyArray = nativeParallelHashMap.GetKeyArray(Allocator.Temp);
				for (int m = 0; m < keyArray.Length; m++)
				{
					Entity entity = keyArray[m];
					HealthProblem healthProblem3 = nativeParallelHashMap[entity];
					if (m_HealthProblemData.HasComponent(entity))
					{
						HealthProblem oldProblem = m_HealthProblemData[entity];
						if (oldProblem.m_Event != healthProblem3.m_Event && m_TargetElements.HasBuffer(healthProblem3.m_Event))
						{
							CollectionUtils.TryAddUniqueValue(m_TargetElements[healthProblem3.m_Event], new TargetElement(entity));
						}
						if ((oldProblem.m_Flags & (HealthProblemFlags.Dead | HealthProblemFlags.RequireTransport)) == HealthProblemFlags.RequireTransport && (healthProblem3.m_Flags & HealthProblemFlags.Dead) != HealthProblemFlags.None)
						{
							m_IconCommandBuffer.Remove(entity, m_HealthcareParameterData.m_AmbulanceNotificationPrefab);
							healthProblem3.m_Timer = 0;
						}
						if ((healthProblem3.m_Flags & (HealthProblemFlags.Dead | HealthProblemFlags.Injured)) != HealthProblemFlags.None && (healthProblem3.m_Flags & HealthProblemFlags.RequireTransport) != HealthProblemFlags.None && ((oldProblem.m_Flags & (HealthProblemFlags.Dead | HealthProblemFlags.Injured)) == 0 || (oldProblem.m_Flags & HealthProblemFlags.RequireTransport) == 0))
						{
							StopMoving(entity);
						}
						AddJournalData(oldProblem, healthProblem3);
						m_HealthProblemData[entity] = healthProblem3;
					}
					else
					{
						if (m_TargetElements.HasBuffer(healthProblem3.m_Event))
						{
							CollectionUtils.TryAddUniqueValue(m_TargetElements[healthProblem3.m_Event], new TargetElement(entity));
						}
						if ((healthProblem3.m_Flags & (HealthProblemFlags.Dead | HealthProblemFlags.Injured)) != HealthProblemFlags.None && (healthProblem3.m_Flags & HealthProblemFlags.RequireTransport) != HealthProblemFlags.None)
						{
							StopMoving(entity);
						}
						m_CommandBuffer.AddComponent(entity, healthProblem3);
						AddJournalData(healthProblem3);
					}
					Entity triggerPrefab = Entity.Null;
					if (m_PrefabRefData.HasComponent(healthProblem3.m_Event))
					{
						triggerPrefab = m_PrefabRefData[healthProblem3.m_Event].m_Prefab;
					}
					if ((healthProblem3.m_Flags & HealthProblemFlags.Sick) != HealthProblemFlags.None)
					{
						m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizenGotSick, triggerPrefab, entity, healthProblem3.m_Event));
					}
					else if ((healthProblem3.m_Flags & HealthProblemFlags.Injured) != HealthProblemFlags.None)
					{
						m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizenGotInjured, triggerPrefab, entity, healthProblem3.m_Event));
					}
					else if ((healthProblem3.m_Flags & HealthProblemFlags.Trapped) != HealthProblemFlags.None)
					{
						m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizenGotTrapped, triggerPrefab, entity, healthProblem3.m_Event));
					}
					else if ((healthProblem3.m_Flags & HealthProblemFlags.InDanger) != HealthProblemFlags.None)
					{
						m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizenGotInDanger, triggerPrefab, entity, healthProblem3.m_Event));
					}
				}
			}
			nativeParallelHashMap.Dispose();
		}

		private void StopMoving(Entity citizen)
		{
			if (m_CurrentTransportData.HasComponent(citizen))
			{
				CurrentTransport currentTransport = m_CurrentTransportData[citizen];
				if (m_PathOwnerData.HasComponent(currentTransport.m_CurrentTransport))
				{
					PathOwner value = m_PathOwnerData[currentTransport.m_CurrentTransport];
					value.m_State &= ~PathFlags.Failed;
					value.m_State |= PathFlags.Obsolete;
					m_PathOwnerData[currentTransport.m_CurrentTransport] = value;
				}
				if (m_TargetData.HasComponent(currentTransport.m_CurrentTransport))
				{
					m_TargetData[currentTransport.m_CurrentTransport] = default(Target);
				}
			}
		}

		private void AddJournalData(HealthProblem problem)
		{
			if ((problem.m_Flags & (HealthProblemFlags.Sick | HealthProblemFlags.Dead | HealthProblemFlags.Injured)) != HealthProblemFlags.None)
			{
				Entity e = m_CommandBuffer.CreateEntity(m_JournalDataArchetype);
				m_CommandBuffer.SetComponent(e, new AddEventJournalData(problem.m_Event, EventDataTrackingType.Casualties));
			}
		}

		private void AddJournalData(HealthProblem oldProblem, HealthProblem newProblem)
		{
			if (oldProblem.m_Event != newProblem.m_Event)
			{
				AddJournalData(newProblem);
			}
			else if ((oldProblem.m_Flags & (HealthProblemFlags.Sick | HealthProblemFlags.Dead | HealthProblemFlags.Injured)) == 0)
			{
				AddJournalData(newProblem);
			}
		}

		private HealthProblem MergeProblems(HealthProblem problem1, HealthProblem problem2)
		{
			HealthProblemFlags healthProblemFlags = problem1.m_Flags ^ problem2.m_Flags;
			if ((healthProblemFlags & HealthProblemFlags.Dead) != HealthProblemFlags.None)
			{
				if ((problem1.m_Flags & HealthProblemFlags.Dead) == 0)
				{
					return problem2;
				}
				return problem1;
			}
			HealthProblem result;
			if ((healthProblemFlags & HealthProblemFlags.RequireTransport) != HealthProblemFlags.None)
			{
				result = (((problem1.m_Flags & HealthProblemFlags.RequireTransport) != HealthProblemFlags.None) ? problem1 : problem2);
				result.m_Flags |= (((problem1.m_Flags & HealthProblemFlags.RequireTransport) != HealthProblemFlags.None) ? problem2.m_Flags : problem1.m_Flags);
			}
			else if (problem1.m_Event != Entity.Null != (problem2.m_Event != Entity.Null))
			{
				result = ((problem1.m_Event != Entity.Null) ? problem1 : problem2);
				result.m_Flags |= ((problem1.m_Event != Entity.Null) ? problem2.m_Flags : problem1.m_Flags);
			}
			else
			{
				result = problem1;
				result.m_Flags |= problem2.m_Flags;
			}
			return result;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Ignite> __Game_Events_Ignite_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Destroy> __Game_Objects_Destroy_RO_ComponentTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<AddHealthProblem> __Game_Events_AddHealthProblem_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<CurrentTransport> __Game_Citizens_CurrentTransport_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RW_ComponentLookup;

		public ComponentLookup<PathOwner> __Game_Pathfind_PathOwner_RW_ComponentLookup;

		public ComponentLookup<Target> __Game_Common_Target_RW_ComponentLookup;

		public BufferLookup<TargetElement> __Game_Events_TargetElement_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Events_Ignite_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Ignite>(isReadOnly: true);
			__Game_Objects_Destroy_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Destroy>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentBuilding>(isReadOnly: true);
			__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HouseholdMember>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Events_AddHealthProblem_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AddHealthProblem>(isReadOnly: true);
			__Game_Citizens_CurrentTransport_RO_ComponentLookup = state.GetComponentLookup<CurrentTransport>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Citizens_HealthProblem_RW_ComponentLookup = state.GetComponentLookup<HealthProblem>();
			__Game_Pathfind_PathOwner_RW_ComponentLookup = state.GetComponentLookup<PathOwner>();
			__Game_Common_Target_RW_ComponentLookup = state.GetComponentLookup<Target>();
			__Game_Events_TargetElement_RW_BufferLookup = state.GetBufferLookup<TargetElement>();
		}
	}

	private IconCommandSystem m_IconCommandSystem;

	private ModificationBarrier4 m_ModificationBarrier;

	private EntityQuery m_AddHealthProblemQuery;

	private EntityQuery m_HealthcareSettingsQuery;

	private EntityQuery m_CitizenQuery;

	private EntityArchetype m_JournalDataArchetype;

	private TriggerSystem m_TriggerSystem;

	private CityStatisticsSystem m_CityStatisticsSystem;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier4>();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_AddHealthProblemQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Game.Common.Event>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<AddHealthProblem>(),
				ComponentType.ReadOnly<Ignite>(),
				ComponentType.ReadOnly<Destroy>()
			}
		});
		m_HealthcareSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<HealthcareParameterData>());
		m_CitizenQuery = GetEntityQuery(ComponentType.ReadOnly<Citizen>(), ComponentType.ReadOnly<CurrentBuilding>(), ComponentType.Exclude<Deleted>());
		m_JournalDataArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<AddEventJournalData>(), ComponentType.ReadWrite<Game.Common.Event>());
		RequireForUpdate(m_AddHealthProblemQuery);
		RequireForUpdate(m_HealthcareSettingsQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeArray<ArchetypeChunk> chunks = m_AddHealthProblemQuery.ToArchetypeChunkArray(Allocator.TempJob);
		NativeQueue<AddHealthProblem> addQueue = new NativeQueue<AddHealthProblem>(Allocator.TempJob);
		NativeQueue<AddHealthProblem>.ParallelWriter addQueue2 = addQueue.AsParallelWriter();
		ComponentTypeHandle<Ignite> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_Ignite_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<Destroy> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Destroy_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		for (int i = 0; i < chunks.Length; i++)
		{
			ArchetypeChunk archetypeChunk = chunks[i];
			NativeArray<Ignite> nativeArray = archetypeChunk.GetNativeArray(ref typeHandle);
			NativeArray<Destroy> nativeArray2 = archetypeChunk.GetNativeArray(ref typeHandle2);
			for (int j = 0; j < nativeArray.Length; j++)
			{
				Ignite ignite = nativeArray[j];
				if (base.EntityManager.HasComponent<Building>(ignite.m_Target))
				{
					JobHandle deps;
					FindCitizensInBuildingJob jobData = new FindCitizensInBuildingJob
					{
						m_Event = ignite.m_Event,
						m_Building = ignite.m_Target,
						m_Flags = HealthProblemFlags.InDanger,
						m_DeathProbability = 0f,
						m_RandomSeed = RandomSeed.Next(),
						m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
						m_CurrentBuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle, ref base.CheckedStateRef),
						m_HouseholdMemberType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle, ref base.CheckedStateRef),
						m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
						m_TriggerBuffer = m_TriggerSystem.CreateActionBuffer().AsParallelWriter(),
						m_StatisticsEventQueue = m_CityStatisticsSystem.GetStatisticsEventQueue(out deps).AsParallelWriter(),
						m_AddQueue = addQueue2
					};
					base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_CitizenQuery, JobHandle.CombineDependencies(base.Dependency, deps));
					m_CityStatisticsSystem.AddWriter(base.Dependency);
					m_TriggerSystem.AddActionBufferWriter(base.Dependency);
				}
			}
			for (int k = 0; k < nativeArray2.Length; k++)
			{
				Destroy destroy = nativeArray2[k];
				if (base.EntityManager.HasComponent<Building>(destroy.m_Object))
				{
					JobHandle deps2;
					FindCitizensInBuildingJob jobData2 = new FindCitizensInBuildingJob
					{
						m_Event = destroy.m_Event,
						m_Building = destroy.m_Object,
						m_Flags = HealthProblemFlags.Trapped,
						m_DeathProbability = m_HealthcareSettingsQuery.GetSingleton<HealthcareParameterData>().m_BuildingDestoryDeathRate,
						m_RandomSeed = RandomSeed.Next(),
						m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
						m_CurrentBuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle, ref base.CheckedStateRef),
						m_HouseholdMemberType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle, ref base.CheckedStateRef),
						m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
						m_TriggerBuffer = m_TriggerSystem.CreateActionBuffer().AsParallelWriter(),
						m_StatisticsEventQueue = m_CityStatisticsSystem.GetStatisticsEventQueue(out deps2).AsParallelWriter(),
						m_AddQueue = addQueue2
					};
					base.Dependency = JobChunkExtensions.ScheduleParallel(jobData2, m_CitizenQuery, JobHandle.CombineDependencies(base.Dependency, deps2));
					m_CityStatisticsSystem.AddWriter(base.Dependency);
					m_TriggerSystem.AddActionBufferWriter(base.Dependency);
				}
			}
		}
		AddHealthProblemJob jobData3 = new AddHealthProblemJob
		{
			m_Chunks = chunks,
			m_AddHealthProblemType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_AddHealthProblem_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CurrentTransport_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HealthcareParameterData = m_HealthcareSettingsQuery.GetSingleton<HealthcareParameterData>(),
			m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer(),
			m_HealthProblemData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HealthProblem_RW_ComponentLookup, ref base.CheckedStateRef),
			m_PathOwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathOwner_RW_ComponentLookup, ref base.CheckedStateRef),
			m_TargetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Target_RW_ComponentLookup, ref base.CheckedStateRef),
			m_TargetElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Events_TargetElement_RW_BufferLookup, ref base.CheckedStateRef),
			m_JournalDataArchetype = m_JournalDataArchetype,
			m_AddQueue = addQueue,
			m_TriggerBuffer = m_TriggerSystem.CreateActionBuffer(),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer()
		};
		base.Dependency = IJobExtensions.Schedule(jobData3, base.Dependency);
		addQueue.Dispose(base.Dependency);
		m_TriggerSystem.AddActionBufferWriter(base.Dependency);
		m_IconCommandSystem.AddCommandBufferWriter(base.Dependency);
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
	public AddHealthProblemSystem()
	{
	}
}

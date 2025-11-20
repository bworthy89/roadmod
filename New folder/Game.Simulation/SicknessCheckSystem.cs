using System.Runtime.CompilerServices;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Economy;
using Game.Events;
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
public class SicknessCheckSystem : GameSystemBase
{
	[BurstCompile]
	private struct SicknessCheckJob : IJobChunk
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_EventPrefabChunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Citizen> m_CitizenType;

		[ReadOnly]
		public ComponentTypeHandle<EventData> m_PrefabEventType;

		[ReadOnly]
		public ComponentTypeHandle<HealthEventData> m_HealthEventType;

		[ReadOnly]
		public ComponentTypeHandle<Locked> m_LockedType;

		[ReadOnly]
		public ComponentTypeHandle<HouseholdMember> m_HouseholdMemberType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public ComponentLookup<Citizen> m_CitizenDatas;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_CitizenBuffers;

		[ReadOnly]
		public ComponentLookup<Worker> m_Workers;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		[ReadOnly]
		public ComponentLookup<HealthProblem> m_HealthProblems;

		[ReadOnly]
		public BufferLookup<ServiceFee> m_Fees;

		[ReadOnly]
		public NativeArray<int> m_TaxRates;

		public EconomyParameterData m_EconomyParameters;

		public uint m_UpdateFrameIndex;

		public RandomSeed m_RandomSeed;

		public EntityArchetype m_AddProblemArchetype;

		public Entity m_City;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index == m_UpdateFrameIndex)
			{
				Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
				DynamicBuffer<CityModifier> cityModifiers = m_CityModifiers[m_City];
				NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
				NativeArray<Citizen> nativeArray2 = chunk.GetNativeArray(ref m_CitizenType);
				NativeArray<HouseholdMember> nativeArray3 = chunk.GetNativeArray(ref m_HouseholdMemberType);
				for (int i = 0; i < chunk.Count; i++)
				{
					Entity entity = nativeArray[i];
					Citizen citizen = nativeArray2[i];
					TryAddHealthProblem(unfilteredChunkIndex, ref random, entity, citizen, nativeArray3[i].m_Household, cityModifiers);
				}
			}
		}

		private void TryAddHealthProblem(int jobIndex, ref Random random, Entity entity, Citizen citizen, Entity household, DynamicBuffer<CityModifier> cityModifiers)
		{
			float t = math.saturate(math.pow(2f, 10f - (float)(int)citizen.m_Health * 0.1f) * 0.001f);
			for (int i = 0; i < m_EventPrefabChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_EventPrefabChunks[i];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<EventData> nativeArray2 = archetypeChunk.GetNativeArray(ref m_PrefabEventType);
				NativeArray<HealthEventData> nativeArray3 = archetypeChunk.GetNativeArray(ref m_HealthEventType);
				EnabledMask enabledMask = archetypeChunk.GetEnabledMask(ref m_LockedType);
				for (int j = 0; j < nativeArray3.Length; j++)
				{
					HealthEventData healthData = nativeArray3[j];
					if (healthData.m_RandomTargetType == EventTargetType.Citizen && (!enabledMask.EnableBit.IsValid || !enabledMask[j]))
					{
						float value = math.lerp(healthData.m_OccurenceProbability.min, healthData.m_OccurenceProbability.max, t);
						if (healthData.m_HealthEventType == HealthEventType.Disease)
						{
							CityUtils.ApplyModifier(ref value, cityModifiers, CityModifierType.DiseaseProbability);
						}
						if (random.NextFloat(100f) < value)
						{
							CreateHealthEvent(jobIndex, ref random, entity, nativeArray[j], household, citizen, nativeArray2[j], healthData);
							return;
						}
					}
				}
			}
		}

		private void CreateHealthEvent(int jobIndex, ref Random random, Entity targetEntity, Entity eventPrefab, Entity household, Citizen citizen, EventData eventData, HealthEventData healthData)
		{
			if (healthData.m_RequireTracking)
			{
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, eventData.m_Archetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new PrefabRef(eventPrefab));
				m_CommandBuffer.SetBuffer<TargetElement>(jobIndex, e).Add(new TargetElement(targetEntity));
				return;
			}
			HealthProblemFlags healthProblemFlags = HealthProblemFlags.None;
			switch (healthData.m_HealthEventType)
			{
			case HealthEventType.Disease:
				healthProblemFlags |= HealthProblemFlags.Sick;
				break;
			case HealthEventType.Injury:
				healthProblemFlags |= HealthProblemFlags.Injured;
				break;
			case HealthEventType.Death:
				healthProblemFlags |= HealthProblemFlags.Dead;
				break;
			}
			float num = math.lerp(healthData.m_TransportProbability.max, healthData.m_TransportProbability.min, (float)(int)citizen.m_Health * 0.01f);
			if (random.NextFloat(100f) < num)
			{
				healthProblemFlags |= HealthProblemFlags.RequireTransport;
			}
			float fee = ServiceFeeSystem.GetFee(PlayerResource.Healthcare, m_Fees[m_City]);
			int num2 = 0;
			if (m_CitizenBuffers.HasBuffer(household))
			{
				num2 = EconomyUtils.GetHouseholdIncome(m_CitizenBuffers[household], ref m_Workers, ref m_CitizenDatas, ref m_HealthProblems, ref m_EconomyParameters, m_TaxRates);
			}
			float num3 = 10f / (float)(int)citizen.m_Health - fee / 2f * (float)num2;
			if (random.NextFloat() < num3)
			{
				healthProblemFlags |= HealthProblemFlags.NoHealthcare;
			}
			Entity e2 = m_CommandBuffer.CreateEntity(jobIndex, m_AddProblemArchetype);
			m_CommandBuffer.SetComponent(jobIndex, e2, new AddHealthProblem
			{
				m_Event = Entity.Null,
				m_Target = targetEntity,
				m_Flags = healthProblemFlags
			});
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
		public ComponentTypeHandle<EventData> __Game_Prefabs_EventData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<HealthEventData> __Game_Prefabs_HealthEventData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Locked> __Game_Prefabs_Locked_RO_ComponentTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentTypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Worker> __Game_Citizens_Worker_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ServiceFee> __Game_City_ServiceFee_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Citizens_Citizen_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Citizen>(isReadOnly: true);
			__Game_Prefabs_EventData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EventData>(isReadOnly: true);
			__Game_Prefabs_HealthEventData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HealthEventData>(isReadOnly: true);
			__Game_Prefabs_Locked_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Locked>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HouseholdMember>(isReadOnly: true);
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Citizens_Worker_RO_ComponentLookup = state.GetComponentLookup<Worker>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
			__Game_City_ServiceFee_RO_BufferLookup = state.GetBufferLookup<ServiceFee>(isReadOnly: true);
			__Game_Citizens_HealthProblem_RO_ComponentLookup = state.GetComponentLookup<HealthProblem>(isReadOnly: true);
		}
	}

	public readonly int kUpdatesPerDay = 1;

	private EndFrameBarrier m_EndFrameBarrier;

	private SimulationSystem m_SimulationSystem;

	private CitySystem m_CitySystem;

	private TaxSystem m_TaxSystem;

	private EntityArchetype m_AddProblemArchetype;

	private EntityQuery m_CitizenQuery;

	private EntityQuery m_EventQuery;

	private EntityQuery m_EconomyParameterQuery;

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
		m_TaxSystem = base.World.GetOrCreateSystemManaged<TaxSystem>();
		m_CitizenQuery = GetEntityQuery(ComponentType.ReadOnly<Citizen>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<HealthProblem>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_EventQuery = GetEntityQuery(ComponentType.ReadWrite<HealthEventData>(), ComponentType.Exclude<Locked>());
		m_AddProblemArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<AddHealthProblem>());
		m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		RequireForUpdate(m_CitizenQuery);
		RequireForUpdate(m_EconomyParameterQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16);
		JobHandle outJobHandle;
		SicknessCheckJob jobData = new SicknessCheckJob
		{
			m_EventPrefabChunks = m_EventQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle),
			m_CitizenType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabEventType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_EventData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HealthEventType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_HealthEventData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LockedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_Locked_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_HouseholdMemberType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_CitizenDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CitizenBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
			m_Workers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
			m_Fees = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_ServiceFee_RO_BufferLookup, ref base.CheckedStateRef),
			m_HealthProblems = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EconomyParameters = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
			m_TaxRates = m_TaxSystem.GetTaxRates(),
			m_UpdateFrameIndex = updateFrame,
			m_RandomSeed = RandomSeed.Next(),
			m_AddProblemArchetype = m_AddProblemArchetype,
			m_City = m_CitySystem.City,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_CitizenQuery, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
		jobData.m_EventPrefabChunks.Dispose(jobHandle);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
		m_TaxSystem.AddReader(jobHandle);
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
	public SicknessCheckSystem()
	{
	}
}

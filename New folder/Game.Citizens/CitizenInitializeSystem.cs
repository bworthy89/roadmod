using System.Runtime.CompilerServices;
using Game.Agents;
using Game.Common;
using Game.Prefabs;
using Game.Simulation;
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

namespace Game.Citizens;

[CompilerGenerated]
public class CitizenInitializeSystem : GameSystemBase
{
	[BurstCompile]
	private struct InitializeCitizenJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public NativeList<Entity> m_CitizenPrefabs;

		[ReadOnly]
		public ComponentTypeHandle<HouseholdMember> m_HouseholdMemberType;

		public ComponentLookup<Arrived> m_Arriveds;

		public ComponentLookup<CarKeeper> m_CarKeepers;

		public ComponentLookup<BicycleOwner> m_BicycleOwners;

		public ComponentLookup<HasJobSeeker> m_HasJobSeekers;

		public ComponentLookup<PropertySeeker> m_PropertySeekers;

		public ComponentLookup<MailSender> m_MailSenders;

		[ReadOnly]
		public ComponentLookup<CitizenData> m_CitizenDatas;

		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		public ComponentLookup<CrimeVictim> m_CrimeVictims;

		public ComponentLookup<Citizen> m_Citizens;

		public NativeQueue<TriggerAction>.ParallelWriter m_TriggerBuffer;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public uint m_SimulationFrame;

		[ReadOnly]
		public TimeData m_TimeData;

		[ReadOnly]
		public DemandParameterData m_DemandParameters;

		[ReadOnly]
		public TimeSettingsData m_TimeSettings;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<HouseholdMember> nativeArray2 = chunk.GetNativeArray(ref m_HouseholdMemberType);
			int daysPerYear = m_TimeSettings.m_DaysPerYear;
			Random random = m_RandomSeed.GetRandom(0);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				m_Arriveds.SetComponentEnabled(entity, value: false);
				m_MailSenders.SetComponentEnabled(entity, value: false);
				m_CrimeVictims.SetComponentEnabled(entity, value: false);
				m_CarKeepers.SetComponentEnabled(entity, value: false);
				m_HasJobSeekers.SetComponentEnabled(entity, value: false);
				Citizen citizen = m_Citizens[entity];
				Entity household = nativeArray2[i].m_Household;
				bool flag = (citizen.m_State & CitizenFlags.Commuter) != 0;
				bool flag2 = (citizen.m_State & CitizenFlags.Tourist) != 0;
				citizen.m_PseudoRandom = (ushort)(random.NextUInt() % 65536);
				citizen.m_Health = (byte)(40 + random.NextInt(20));
				citizen.m_WellBeing = (byte)(40 + random.NextInt(20));
				if (flag2)
				{
					citizen.m_LeisureCounter = (byte)random.NextInt(128);
				}
				else
				{
					citizen.m_LeisureCounter = (byte)(random.NextInt(92) + 128);
				}
				if (random.NextBool())
				{
					citizen.m_State |= CitizenFlags.Male;
				}
				Entity citizenPrefabFromCitizen = CitizenUtils.GetCitizenPrefabFromCitizen(m_CitizenPrefabs, citizen, m_CitizenDatas, random);
				m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, new PrefabRef
				{
					m_Prefab = citizenPrefabFromCitizen
				});
				DynamicBuffer<HouseholdCitizen> dynamicBuffer = m_HouseholdCitizens[household];
				dynamicBuffer.Add(new HouseholdCitizen
				{
					m_Citizen = entity
				});
				int num = 0;
				int2 @int = int2.zero;
				if (citizen.m_BirthDay == 0)
				{
					citizen.SetAge(CitizenAge.Child);
					Entity entity2 = Entity.Null;
					Entity entity3 = Entity.Null;
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						Entity citizen2 = dynamicBuffer[j].m_Citizen;
						if (m_Citizens.HasComponent(citizen2) && m_Citizens[citizen2].GetAge() == CitizenAge.Adult)
						{
							if (entity2 == Entity.Null)
							{
								entity2 = citizen2;
							}
							else
							{
								entity3 = citizen2;
							}
						}
					}
					if (entity2 != Entity.Null)
					{
						if (entity3 != Entity.Null)
						{
							m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizenCoupleMadeBaby, Entity.Null, entity2, entity));
						}
						else
						{
							m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizenSingleMadeBaby, Entity.Null, entity2, entity));
						}
					}
				}
				else if (citizen.m_BirthDay == 1)
				{
					num = random.NextInt(AgingSystem.GetAdultAgeLimitInDays(), AgingSystem.GetElderAgeLimitInDays());
					citizen.SetAge(CitizenAge.Adult);
					@int.x = 0;
					@int.y = (flag ? 4 : 3);
				}
				else if (citizen.m_BirthDay == 2)
				{
					if (random.NextFloat(1f) > m_DemandParameters.m_TeenSpawnPercentage)
					{
						num = random.NextInt(AgingSystem.GetTeenAgeLimitInDays());
						citizen.SetAge(CitizenAge.Child);
					}
					else
					{
						num = random.NextInt(AgingSystem.GetTeenAgeLimitInDays(), AgingSystem.GetAdultAgeLimitInDays());
						citizen.SetAge(CitizenAge.Teen);
						@int = new int2(0, 1);
					}
				}
				else if (citizen.m_BirthDay == 3)
				{
					num = AgingSystem.GetElderAgeLimitInDays() + random.NextInt(5);
					citizen.SetAge(CitizenAge.Elderly);
					@int = new int2(0, 4);
				}
				else
				{
					num = AgingSystem.GetAdultAgeLimitInDays() + random.NextInt(daysPerYear);
					citizen.SetAge(CitizenAge.Adult);
					@int = new int2(2, 3);
				}
				float num2 = 0f;
				float num3 = 1f;
				for (int k = 0; k <= 3; k++)
				{
					if (k >= @int.x && k <= @int.y)
					{
						num2 += m_DemandParameters.m_NewCitizenEducationParameters[k];
					}
					num3 -= m_DemandParameters.m_NewCitizenEducationParameters[k];
				}
				if (@int.y == 4)
				{
					num2 += num3;
				}
				float num4 = random.NextFloat(num2);
				for (int l = @int.x; l <= @int.y; l++)
				{
					if (l == 4 || num4 < m_DemandParameters.m_NewCitizenEducationParameters[l])
					{
						citizen.SetEducationLevel(l);
						break;
					}
					num4 -= m_DemandParameters.m_NewCitizenEducationParameters[l];
				}
				bool flag3 = (citizen.m_State & (CitizenFlags.AgeBit1 | CitizenFlags.AgeBit2)) != 0;
				m_BicycleOwners.SetComponentEnabled(entity, !flag && !flag2 && flag3);
				citizen.m_BirthDay = (short)(TimeSystem.GetDay(m_SimulationFrame, m_TimeData) - num);
				m_Citizens[entity] = citizen;
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

		public ComponentTypeHandle<HouseholdMember> __Game_Citizens_HouseholdMember_RW_ComponentTypeHandle;

		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RW_ComponentLookup;

		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RW_BufferLookup;

		[ReadOnly]
		public ComponentLookup<CitizenData> __Game_Prefabs_CitizenData_RO_ComponentLookup;

		public ComponentLookup<Arrived> __Game_Citizens_Arrived_RW_ComponentLookup;

		public ComponentLookup<CarKeeper> __Game_Citizens_CarKeeper_RW_ComponentLookup;

		public ComponentLookup<BicycleOwner> __Game_Citizens_BicycleOwner_RW_ComponentLookup;

		public ComponentLookup<HasJobSeeker> __Game_Agents_HasJobSeeker_RW_ComponentLookup;

		public ComponentLookup<PropertySeeker> __Game_Agents_PropertySeeker_RW_ComponentLookup;

		public ComponentLookup<MailSender> __Game_Citizens_MailSender_RW_ComponentLookup;

		public ComponentLookup<CrimeVictim> __Game_Citizens_CrimeVictim_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Citizens_HouseholdMember_RW_ComponentTypeHandle = state.GetComponentTypeHandle<HouseholdMember>();
			__Game_Citizens_Citizen_RW_ComponentLookup = state.GetComponentLookup<Citizen>();
			__Game_Citizens_HouseholdCitizen_RW_BufferLookup = state.GetBufferLookup<HouseholdCitizen>();
			__Game_Prefabs_CitizenData_RO_ComponentLookup = state.GetComponentLookup<CitizenData>(isReadOnly: true);
			__Game_Citizens_Arrived_RW_ComponentLookup = state.GetComponentLookup<Arrived>();
			__Game_Citizens_CarKeeper_RW_ComponentLookup = state.GetComponentLookup<CarKeeper>();
			__Game_Citizens_BicycleOwner_RW_ComponentLookup = state.GetComponentLookup<BicycleOwner>();
			__Game_Agents_HasJobSeeker_RW_ComponentLookup = state.GetComponentLookup<HasJobSeeker>();
			__Game_Agents_PropertySeeker_RW_ComponentLookup = state.GetComponentLookup<PropertySeeker>();
			__Game_Citizens_MailSender_RW_ComponentLookup = state.GetComponentLookup<MailSender>();
			__Game_Citizens_CrimeVictim_RW_ComponentLookup = state.GetComponentLookup<CrimeVictim>();
		}
	}

	private EntityQuery m_NewCitizenQuery;

	private EntityQuery m_TimeSettingQuery;

	private EntityQuery m_CitizenPrefabQuery;

	private EntityQuery m_TimeDataQuery;

	private EntityQuery m_DemandParameterQuery;

	private SimulationSystem m_SimulationSystem;

	private TriggerSystem m_TriggerSystem;

	private ModificationBarrier5 m_EndFrameBarrier;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier5>();
		m_NewCitizenQuery = GetEntityQuery(ComponentType.ReadWrite<Citizen>(), ComponentType.ReadWrite<HouseholdMember>(), ComponentType.ReadOnly<Created>(), ComponentType.Exclude<Temp>());
		m_CitizenPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<CitizenData>());
		m_TimeSettingQuery = GetEntityQuery(ComponentType.ReadOnly<TimeSettingsData>());
		m_TimeDataQuery = GetEntityQuery(ComponentType.ReadOnly<TimeData>());
		m_DemandParameterQuery = GetEntityQuery(ComponentType.ReadOnly<DemandParameterData>());
		RequireForUpdate(m_NewCitizenQuery);
		RequireForUpdate(m_TimeDataQuery);
		RequireForUpdate(m_TimeSettingQuery);
		RequireForUpdate(m_DemandParameterQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		InitializeCitizenJob jobData = new InitializeCitizenJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_HouseholdMemberType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_HouseholdMember_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CitizenPrefabs = m_CitizenPrefabQuery.ToEntityListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
			m_Citizens = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RW_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RW_BufferLookup, ref base.CheckedStateRef),
			m_CitizenDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CitizenData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Arriveds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Arrived_RW_ComponentLookup, ref base.CheckedStateRef),
			m_CarKeepers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CarKeeper_RW_ComponentLookup, ref base.CheckedStateRef),
			m_BicycleOwners = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_BicycleOwner_RW_ComponentLookup, ref base.CheckedStateRef),
			m_HasJobSeekers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Agents_HasJobSeeker_RW_ComponentLookup, ref base.CheckedStateRef),
			m_PropertySeekers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Agents_PropertySeeker_RW_ComponentLookup, ref base.CheckedStateRef),
			m_MailSenders = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_MailSender_RW_ComponentLookup, ref base.CheckedStateRef),
			m_CrimeVictims = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CrimeVictim_RW_ComponentLookup, ref base.CheckedStateRef),
			m_DemandParameters = m_DemandParameterQuery.GetSingleton<DemandParameterData>(),
			m_TimeSettings = m_TimeSettingQuery.GetSingleton<TimeSettingsData>(),
			m_TimeData = m_TimeDataQuery.GetSingleton<TimeData>(),
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_RandomSeed = RandomSeed.Next(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_TriggerBuffer = m_TriggerSystem.CreateActionBuffer().AsParallelWriter()
		};
		base.Dependency = JobChunkExtensions.Schedule(jobData, m_NewCitizenQuery, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
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
	public CitizenInitializeSystem()
	{
	}
}

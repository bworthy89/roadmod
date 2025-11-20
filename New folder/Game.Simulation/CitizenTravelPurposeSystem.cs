using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
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
public class CitizenTravelPurposeSystem : GameSystemBase
{
	[BurstCompile]
	private struct CitizenArriveJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentBuilding> m_CurrentBuildingType;

		public ComponentTypeHandle<TravelPurpose> m_TravelPurposeType;

		[ReadOnly]
		public ComponentTypeHandle<HealthProblem> m_HealthProblemType;

		[ReadOnly]
		public ComponentTypeHandle<Arrived> m_ArrivedType;

		[ReadOnly]
		public ComponentLookup<Worker> m_Workers;

		[ReadOnly]
		public ComponentLookup<Game.Citizens.Student> m_Students;

		[ReadOnly]
		public ComponentLookup<WorkProvider> m_WorkProviders;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.School> m_Schools;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.PoliceStation> m_PoliceStationData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Prison> m_PrisonData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Hospital> m_HospitalData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.DeathcareFacility> m_DeathcareFacilityData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.EmergencyShelter> m_EmergencyShelterData;

		[ReadOnly]
		public ComponentLookup<Citizen> m_Citizens;

		[ReadOnly]
		public ComponentLookup<HealthProblem> m_HealthProblems;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> m_HouseholdMembers;

		[ReadOnly]
		public ComponentLookup<MovingAway> m_MovingAways;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<Arrive>.ParallelWriter m_ArriveQueue;

		public EconomyParameterData m_EconomyParameters;

		public float m_NormalizedTime;

		public RandomSeed m_RandomSeed;

		private bool IsSleepAllowed(Entity citizenEntity)
		{
			if (m_HouseholdMembers.TryGetComponent(citizenEntity, out var componentData))
			{
				return !m_MovingAways.HasComponent(componentData.m_Household);
			}
			return false;
		}

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<TravelPurpose> nativeArray2 = chunk.GetNativeArray(ref m_TravelPurposeType);
			NativeArray<CurrentBuilding> nativeArray3 = chunk.GetNativeArray(ref m_CurrentBuildingType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			bool flag = chunk.Has(ref m_HealthProblemType);
			for (int i = 0; i < chunk.Count; i++)
			{
				bool flag2 = chunk.IsComponentEnabled(ref m_ArrivedType, i);
				Entity entity = nativeArray[i];
				TravelPurpose value = nativeArray2[i];
				if (flag && CitizenUtils.IsDead(entity, ref m_HealthProblems) && value.m_Purpose != Purpose.Deathcare && value.m_Purpose != Purpose.InDeathcare && value.m_Purpose != Purpose.Hospital && value.m_Purpose != Purpose.InHospital)
				{
					m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
				}
				else if (value.m_Purpose == Purpose.Sleeping)
				{
					Citizen citizen = m_Citizens[entity];
					if (!IsSleepAllowed(entity) || !CitizenBehaviorSystem.IsSleepTime(entity, citizen, ref m_EconomyParameters, m_NormalizedTime, ref m_Workers, ref m_Students))
					{
						m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
						if (nativeArray3.Length != 0 && m_BuildingData.HasComponent(nativeArray3[i].m_CurrentBuilding))
						{
							m_ArriveQueue.Enqueue(new Arrive(entity, nativeArray3[i].m_CurrentBuilding, ArriveType.WakeUp));
						}
					}
				}
				else if (value.m_Purpose == Purpose.VisitAttractions)
				{
					if (flag2)
					{
						m_CommandBuffer.SetComponentEnabled<Arrived>(unfilteredChunkIndex, entity, value: false);
					}
					if (random.NextInt(100) == 0)
					{
						m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
					}
				}
				else
				{
					if (!flag2)
					{
						continue;
					}
					m_CommandBuffer.SetComponentEnabled<Arrived>(unfilteredChunkIndex, entity, value: false);
					switch (value.m_Purpose)
					{
					case Purpose.GoingHome:
						if (nativeArray3.Length != 0 && m_BuildingData.HasComponent(nativeArray3[i].m_CurrentBuilding))
						{
							m_ArriveQueue.Enqueue(new Arrive(entity, nativeArray3[i].m_CurrentBuilding, ArriveType.Resident));
						}
						m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
						break;
					case Purpose.None:
					case Purpose.Shopping:
					case Purpose.Leisure:
					case Purpose.Exporting:
					case Purpose.MovingAway:
					case Purpose.Safety:
					case Purpose.Escape:
					case Purpose.Traveling:
					case Purpose.SendMail:
					case Purpose.Disappear:
					case Purpose.WaitingHome:
					case Purpose.PathFailed:
						m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
						break;
					case Purpose.EmergencyShelter:
						if (nativeArray3.Length != 0 && m_EmergencyShelterData.HasComponent(nativeArray3[i].m_CurrentBuilding))
						{
							value.m_Purpose = Purpose.InEmergencyShelter;
							nativeArray2[i] = value;
							m_ArriveQueue.Enqueue(new Arrive(entity, nativeArray3[i].m_CurrentBuilding, ArriveType.Occupant));
						}
						else
						{
							m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
						}
						break;
					case Purpose.GoingToWork:
						if (nativeArray3.Length != 0 && m_BuildingData.HasComponent(nativeArray3[i].m_CurrentBuilding))
						{
							m_ArriveQueue.Enqueue(new Arrive(entity, nativeArray3[i].m_CurrentBuilding, ArriveType.Worker));
						}
						if (m_Workers.HasComponent(entity))
						{
							Entity workplace = m_Workers[entity].m_Workplace;
							if (m_WorkProviders.HasComponent(workplace))
							{
								value.m_Purpose = Purpose.Working;
								nativeArray2[i] = value;
							}
							else
							{
								m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
							}
						}
						break;
					case Purpose.GoingToSchool:
						if (m_Students.HasComponent(entity))
						{
							Entity school = m_Students[entity].m_School;
							if (m_Schools.HasComponent(school))
							{
								value.m_Purpose = Purpose.Studying;
								nativeArray2[i] = value;
							}
							else
							{
								m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
							}
						}
						else
						{
							m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
						}
						break;
					case Purpose.GoingToJail:
						if (nativeArray3.Length != 0 && m_PoliceStationData.HasComponent(nativeArray3[i].m_CurrentBuilding))
						{
							value.m_Purpose = Purpose.InJail;
							nativeArray2[i] = value;
							m_ArriveQueue.Enqueue(new Arrive(entity, nativeArray3[i].m_CurrentBuilding, ArriveType.Occupant));
						}
						else
						{
							m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
						}
						break;
					case Purpose.GoingToPrison:
						if (nativeArray3.Length != 0 && m_PrisonData.HasComponent(nativeArray3[i].m_CurrentBuilding))
						{
							value.m_Purpose = Purpose.InPrison;
							nativeArray2[i] = value;
							m_ArriveQueue.Enqueue(new Arrive(entity, nativeArray3[i].m_CurrentBuilding, ArriveType.Occupant));
						}
						else
						{
							m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
						}
						break;
					case Purpose.Hospital:
						if (nativeArray3.Length != 0 && m_HospitalData.HasComponent(nativeArray3[i].m_CurrentBuilding))
						{
							value.m_Purpose = Purpose.InHospital;
							nativeArray2[i] = value;
							m_ArriveQueue.Enqueue(new Arrive(entity, nativeArray3[i].m_CurrentBuilding, ArriveType.Patient));
						}
						else
						{
							m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
						}
						break;
					case Purpose.Deathcare:
						if (nativeArray3.Length != 0 && m_DeathcareFacilityData.HasComponent(nativeArray3[i].m_CurrentBuilding))
						{
							value.m_Purpose = Purpose.InDeathcare;
							nativeArray2[i] = value;
							m_ArriveQueue.Enqueue(new Arrive(entity, nativeArray3[i].m_CurrentBuilding, ArriveType.Patient));
						}
						else
						{
							m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
						}
						break;
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct Arrive
	{
		public Entity m_Citizen;

		public Entity m_Target;

		public ArriveType m_Type;

		public Arrive(Entity citizen, Entity target, ArriveType type)
		{
			m_Citizen = citizen;
			m_Target = target;
			m_Type = type;
		}
	}

	private enum ArriveType
	{
		Patient,
		Occupant,
		Resident,
		Worker,
		WakeUp
	}

	[BurstCompile]
	private struct ArriveJob : IJob
	{
		public ComponentLookup<CitizenPresence> m_CitizenPresenceData;

		public BufferLookup<Patient> m_Patients;

		public BufferLookup<Occupant> m_Occupants;

		public ComponentLookup<Household> m_Households;

		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> m_HouseholdMembers;

		public NativeQueue<StatisticsEvent> m_StatisticsQueue;

		public NativeQueue<Arrive> m_ArriveQueue;

		private void SetPresent(Arrive arrive)
		{
			if (m_CitizenPresenceData.HasComponent(arrive.m_Target))
			{
				CitizenPresence value = m_CitizenPresenceData[arrive.m_Target];
				value.m_Delta = (sbyte)math.min(127, value.m_Delta + 1);
				m_CitizenPresenceData[arrive.m_Target] = value;
			}
		}

		public void Execute()
		{
			int count = m_ArriveQueue.Count;
			for (int i = 0; i < count; i++)
			{
				Arrive present = m_ArriveQueue.Dequeue();
				switch (present.m_Type)
				{
				case ArriveType.Patient:
					if (m_Patients.HasBuffer(present.m_Target))
					{
						CollectionUtils.TryAddUniqueValue(m_Patients[present.m_Target], new Patient(present.m_Citizen));
					}
					break;
				case ArriveType.Occupant:
					if (m_Occupants.HasBuffer(present.m_Target))
					{
						CollectionUtils.TryAddUniqueValue(m_Occupants[present.m_Target], new Occupant(present.m_Citizen));
					}
					break;
				case ArriveType.Resident:
				{
					Entity household = m_HouseholdMembers[present.m_Citizen].m_Household;
					if (m_PropertyRenters.HasComponent(household) && m_PropertyRenters[household].m_Property == present.m_Target)
					{
						Household value = m_Households[household];
						if (m_HouseholdCitizens.HasBuffer(household) && (value.m_Flags & HouseholdFlags.MovedIn) == 0)
						{
							m_StatisticsQueue.Enqueue(new StatisticsEvent
							{
								m_Statistic = StatisticType.CitizensMovedIn,
								m_Change = m_HouseholdCitizens[household].Length
							});
						}
						value.m_Flags |= HouseholdFlags.MovedIn;
						m_Households[household] = value;
					}
					SetPresent(present);
					break;
				}
				case ArriveType.Worker:
				case ArriveType.WakeUp:
					SetPresent(present);
					break;
				}
			}
		}
	}

	[BurstCompile]
	private struct CitizenStuckJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<HouseholdMember> m_HouseholdMemberType;

		[ReadOnly]
		public ComponentTypeHandle<HealthProblem> m_HealthProblemType;

		public ComponentTypeHandle<Citizen> m_CitizenType;

		[ReadOnly]
		public ComponentLookup<Household> m_Households;

		[ReadOnly]
		public ComponentLookup<MovingAway> m_MovingAways;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		[ReadOnly]
		public ComponentLookup<Building> m_Buildings;

		[ReadOnly]
		public NativeList<Entity> m_OutsideConnections;

		[ReadOnly]
		public NativeList<Entity> m_ServiceBuildings;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public RandomSeed m_RandomSeed;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<HouseholdMember> nativeArray2 = chunk.GetNativeArray(ref m_HouseholdMemberType);
			NativeArray<HealthProblem> nativeArray3 = chunk.GetNativeArray(ref m_HealthProblemType);
			NativeArray<Citizen> nativeArray4 = chunk.GetNativeArray(ref m_CitizenType);
			if (nativeArray2.Length < chunk.Count || m_OutsideConnections.Length == 0)
			{
				return;
			}
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray[i];
				Entity household = nativeArray2[i].m_Household;
				bool flag = (m_Households[household].m_Flags & HouseholdFlags.MovedIn) != HouseholdFlags.None && !m_MovingAways.HasComponent(household);
				if (CollectionUtils.TryGet(nativeArray3, i, out var value) && (value.m_Flags & HealthProblemFlags.Dead) != HealthProblemFlags.None)
				{
					m_CommandBuffer.AddComponent<Deleted>(unfilteredChunkIndex, nativeArray[i]);
					continue;
				}
				Entity entity2 = Entity.Null;
				Random random = m_RandomSeed.GetRandom((1 + i) * (entity.Index + 1));
				if (flag)
				{
					if (m_PropertyRenters.HasComponent(household))
					{
						entity2 = m_PropertyRenters[household].m_Property;
					}
					if (entity2 == Entity.Null && m_ServiceBuildings.Length > 0)
					{
						int num = 0;
						do
						{
							num++;
							entity2 = m_ServiceBuildings[random.NextInt(m_ServiceBuildings.Length)];
						}
						while ((!m_Buildings.HasComponent(entity2) || m_Buildings[entity2].m_RoadEdge == Entity.Null) && num < 10);
					}
					if (!m_Buildings.HasComponent(entity2) || m_Buildings[entity2].m_RoadEdge == Entity.Null)
					{
						m_CommandBuffer.AddComponent<Deleted>(unfilteredChunkIndex, nativeArray[i]);
					}
				}
				else
				{
					entity2 = m_OutsideConnections[random.NextInt(m_OutsideConnections.Length)];
				}
				if (entity2 != Entity.Null)
				{
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, nativeArray[i], new CurrentBuilding
					{
						m_CurrentBuilding = entity2
					});
					m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, nativeArray[i]);
					Citizen value2 = nativeArray4[i];
					value2.m_PenaltyCounter = byte.MaxValue;
					nativeArray4[i] = value2;
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

		[ReadOnly]
		public ComponentTypeHandle<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle;

		public ComponentTypeHandle<TravelPurpose> __Game_Citizens_TravelPurpose_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Arrived> __Game_Citizens_Arrived_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.School> __Game_Buildings_School_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WorkProvider> __Game_Companies_WorkProvider_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Citizens.Student> __Game_Citizens_Student_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Worker> __Game_Citizens_Worker_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.PoliceStation> __Game_Buildings_PoliceStation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Prison> __Game_Buildings_Prison_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Hospital> __Game_Buildings_Hospital_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.DeathcareFacility> __Game_Buildings_DeathcareFacility_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.EmergencyShelter> __Game_Buildings_EmergencyShelter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MovingAway> __Game_Agents_MovingAway_RO_ComponentLookup;

		public ComponentLookup<CitizenPresence> __Game_Buildings_CitizenPresence_RW_ComponentLookup;

		public BufferLookup<Patient> __Game_Buildings_Patient_RW_BufferLookup;

		public BufferLookup<Occupant> __Game_Buildings_Occupant_RW_BufferLookup;

		public ComponentLookup<Household> __Game_Citizens_Household_RW_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Citizen> __Game_Citizens_Citizen_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentBuilding>(isReadOnly: true);
			__Game_Citizens_TravelPurpose_RW_ComponentTypeHandle = state.GetComponentTypeHandle<TravelPurpose>();
			__Game_Citizens_HealthProblem_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HealthProblem>(isReadOnly: true);
			__Game_Citizens_Arrived_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Arrived>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Buildings_School_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.School>(isReadOnly: true);
			__Game_Companies_WorkProvider_RO_ComponentLookup = state.GetComponentLookup<WorkProvider>(isReadOnly: true);
			__Game_Citizens_Student_RO_ComponentLookup = state.GetComponentLookup<Game.Citizens.Student>(isReadOnly: true);
			__Game_Citizens_Worker_RO_ComponentLookup = state.GetComponentLookup<Worker>(isReadOnly: true);
			__Game_Buildings_PoliceStation_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.PoliceStation>(isReadOnly: true);
			__Game_Buildings_Prison_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.Prison>(isReadOnly: true);
			__Game_Buildings_Hospital_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.Hospital>(isReadOnly: true);
			__Game_Buildings_DeathcareFacility_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.DeathcareFacility>(isReadOnly: true);
			__Game_Buildings_EmergencyShelter_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.EmergencyShelter>(isReadOnly: true);
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Citizens_HealthProblem_RO_ComponentLookup = state.GetComponentLookup<HealthProblem>(isReadOnly: true);
			__Game_Citizens_HouseholdMember_RO_ComponentLookup = state.GetComponentLookup<HouseholdMember>(isReadOnly: true);
			__Game_Agents_MovingAway_RO_ComponentLookup = state.GetComponentLookup<MovingAway>(isReadOnly: true);
			__Game_Buildings_CitizenPresence_RW_ComponentLookup = state.GetComponentLookup<CitizenPresence>();
			__Game_Buildings_Patient_RW_BufferLookup = state.GetBufferLookup<Patient>();
			__Game_Buildings_Occupant_RW_BufferLookup = state.GetBufferLookup<Occupant>();
			__Game_Citizens_Household_RW_ComponentLookup = state.GetComponentLookup<Household>();
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HouseholdMember>(isReadOnly: true);
			__Game_Citizens_Citizen_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Citizen>();
			__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(isReadOnly: true);
		}
	}

	private TimeSystem m_TimeSystem;

	private CityStatisticsSystem m_CityStatisticsSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_ArrivedGroup;

	private EntityQuery m_StuckGroup;

	private EntityQuery m_EconomyParameterGroup;

	private EntityQuery m_OutsideConnectionQuery;

	private EntityQuery m_ServiceBuildingQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_TimeSystem = base.World.GetOrCreateSystemManaged<TimeSystem>();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_ArrivedGroup = GetEntityQuery(ComponentType.ReadOnly<Citizen>(), ComponentType.ReadWrite<TravelPurpose>(), ComponentType.ReadWrite<TripNeeded>(), ComponentType.ReadOnly<CurrentBuilding>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_StuckGroup = GetEntityQuery(ComponentType.ReadOnly<Citizen>(), ComponentType.ReadWrite<TravelPurpose>(), ComponentType.ReadWrite<TripNeeded>(), ComponentType.Exclude<CurrentTransport>(), ComponentType.Exclude<CurrentBuilding>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_OutsideConnectionQuery = GetEntityQuery(ComponentType.ReadWrite<Game.Objects.OutsideConnection>(), ComponentType.Exclude<Game.Objects.ElectricityOutsideConnection>(), ComponentType.Exclude<Game.Objects.WaterPipeOutsideConnection>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_ServiceBuildingQuery = GetEntityQuery(ComponentType.ReadWrite<CityServiceUpkeep>(), ComponentType.ReadWrite<Building>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Temp>());
		m_EconomyParameterGroup = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		m_TimeSystem = base.World.GetOrCreateSystemManaged<TimeSystem>();
		RequireAnyForUpdate(m_ArrivedGroup, m_StuckGroup);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeQueue<Arrive> arriveQueue = new NativeQueue<Arrive>(Allocator.TempJob);
		CitizenArriveJob jobData = new CitizenArriveJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CurrentBuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TravelPurposeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_TravelPurpose_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HealthProblemType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ArrivedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Arrived_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Schools = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_School_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WorkProviders = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_WorkProvider_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Students = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Student_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Workers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PoliceStationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PoliceStation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrisonData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Prison_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HospitalData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Hospital_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DeathcareFacilityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_DeathcareFacility_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EmergencyShelterData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_EmergencyShelter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Citizens = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HealthProblems = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdMembers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MovingAways = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Agents_MovingAway_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EconomyParameters = m_EconomyParameterGroup.GetSingleton<EconomyParameterData>(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_ArriveQueue = arriveQueue.AsParallelWriter(),
			m_NormalizedTime = m_TimeSystem.normalizedTime,
			m_RandomSeed = RandomSeed.Next()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_ArrivedGroup, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
		JobHandle deps;
		JobHandle jobHandle = IJobExtensions.Schedule(new ArriveJob
		{
			m_CitizenPresenceData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_CitizenPresence_RW_ComponentLookup, ref base.CheckedStateRef),
			m_Patients = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Patient_RW_BufferLookup, ref base.CheckedStateRef),
			m_Occupants = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Occupant_RW_BufferLookup, ref base.CheckedStateRef),
			m_Households = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Household_RW_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdMembers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertyRenters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
			m_StatisticsQueue = m_CityStatisticsSystem.GetStatisticsEventQueue(out deps),
			m_ArriveQueue = arriveQueue
		}, JobHandle.CombineDependencies(base.Dependency, deps));
		arriveQueue.Dispose(jobHandle);
		m_CityStatisticsSystem.AddWriter(jobHandle);
		JobHandle outJobHandle;
		JobHandle outJobHandle2;
		JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(new CitizenStuckJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_HouseholdMemberType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HealthProblemType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CitizenType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_Households = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MovingAways = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Agents_MovingAway_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Buildings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertyRenters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_ServiceBuildings = m_ServiceBuildingQuery.ToEntityListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
			m_OutsideConnections = m_OutsideConnectionQuery.ToEntityListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle2),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_StuckGroup, JobUtils.CombineDependencies(outJobHandle2, outJobHandle, jobHandle, base.Dependency));
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);
		base.Dependency = JobHandle.CombineDependencies(jobHandle, jobHandle2);
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
	public CitizenTravelPurposeSystem()
	{
	}
}

#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Creatures;
using Game.Events;
using Game.Notifications;
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
using UnityEngine.Assertions;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class HealthProblemSystem : GameSystemBase
{
	[BurstCompile]
	private struct HealthProblemJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentBuilding> m_CurrentBuildingType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentTransport> m_CurrentTransportType;

		[ReadOnly]
		public ComponentTypeHandle<Citizen> m_CitizenType;

		[ReadOnly]
		public ComponentTypeHandle<TravelPurpose> m_TravelPurposeType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		public ComponentTypeHandle<HealthProblem> m_HealthProblemType;

		public BufferTypeHandle<TripNeeded> m_TripNeededType;

		[ReadOnly]
		public ComponentTypeHandle<HouseholdMember> m_HouseholdMemberType;

		[ReadOnly]
		public ComponentLookup<HealthcareRequest> m_HealthcareRequestData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Hospital> m_HospitalData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.DeathcareFacility> m_DeathcareFacilityData;

		[ReadOnly]
		public ComponentLookup<OnFire> m_OnFireData;

		[ReadOnly]
		public ComponentLookup<Destroyed> m_DestroyedData;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public ComponentLookup<Dispatched> m_DispatchedData;

		[ReadOnly]
		public ComponentLookup<Target> m_TargetData;

		[ReadOnly]
		public ComponentLookup<Static> m_StaticData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnectionData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.Ambulance> m_AmbulanceData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.Hearse> m_HearseData;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> m_CurrentVehicleData;

		[ReadOnly]
		public ComponentLookup<Divert> m_DivertData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_PrefabBuildingData;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		[ReadOnly]
		public uint m_UpdateFrameIndex;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public EntityArchetype m_HealthcareRequestArchetype;

		[ReadOnly]
		public EntityArchetype m_JournalDataArchetype;

		[ReadOnly]
		public EntityArchetype m_ResetTripArchetype;

		[ReadOnly]
		public EntityArchetype m_HandleRequestArchetype;

		[ReadOnly]
		public HealthcareParameterData m_HealthcareParameterData;

		[ReadOnly]
		public FireConfigurationData m_FireConfigurationData;

		public IconCommandBuffer m_IconCommandBuffer;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<Game.City.StatisticsEvent>.ParallelWriter m_StatisticsEventQueue;

		public NativeQueue<TriggerAction>.ParallelWriter m_TriggerBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			int num = (int)(m_HealthcareParameterData.m_TransportWarningTime * (15f / 64f));
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Citizen> nativeArray2 = chunk.GetNativeArray(ref m_CitizenType);
			NativeArray<HealthProblem> nativeArray3 = chunk.GetNativeArray(ref m_HealthProblemType);
			NativeArray<CurrentBuilding> nativeArray4 = chunk.GetNativeArray(ref m_CurrentBuildingType);
			NativeArray<TravelPurpose> nativeArray5 = chunk.GetNativeArray(ref m_TravelPurposeType);
			NativeArray<CurrentTransport> nativeArray6 = chunk.GetNativeArray(ref m_CurrentTransportType);
			BufferAccessor<TripNeeded> bufferAccessor = chunk.GetBufferAccessor(ref m_TripNeededType);
			NativeArray<HouseholdMember> nativeArray7 = chunk.GetNativeArray(ref m_HouseholdMemberType);
			for (int i = 0; i < nativeArray3.Length; i++)
			{
				HealthProblem healthProblem = nativeArray3[i];
				CurrentBuilding currentBuilding = default(CurrentBuilding);
				TravelPurpose travelPurpose = default(TravelPurpose);
				CurrentTransport currentTransport = default(CurrentTransport);
				if (nativeArray4.Length != 0)
				{
					currentBuilding = nativeArray4[i];
				}
				if (nativeArray5.Length != 0)
				{
					travelPurpose = nativeArray5[i];
				}
				if (nativeArray6.Length != 0)
				{
					currentTransport = nativeArray6[i];
				}
				if ((healthProblem.m_Flags & ~HealthProblemFlags.NoHealthcare) == 0)
				{
					Entity e = nativeArray[i];
					m_CommandBuffer.RemoveComponent<HealthProblem>(unfilteredChunkIndex, e);
					m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, e);
					continue;
				}
				if ((healthProblem.m_Flags & (HealthProblemFlags.InDanger | HealthProblemFlags.Trapped)) != HealthProblemFlags.None)
				{
					if ((healthProblem.m_Flags & HealthProblemFlags.Dead) != HealthProblemFlags.None)
					{
						healthProblem.m_Flags &= ~(HealthProblemFlags.InDanger | HealthProblemFlags.Trapped);
						nativeArray3[i] = healthProblem;
					}
					else
					{
						Entity entity = nativeArray[i];
						Citizen citizen = nativeArray2[i];
						DynamicBuffer<TripNeeded> tripNeededs = bufferAccessor[i];
						if (m_OnFireData.HasComponent(currentBuilding.m_CurrentBuilding))
						{
							if ((healthProblem.m_Flags & HealthProblemFlags.InDanger) != HealthProblemFlags.None)
							{
								OnFire onFire = m_OnFireData[currentBuilding.m_CurrentBuilding];
								float num2 = (float)(int)citizen.m_Health - onFire.m_Intensity * 0.5f;
								if (random.NextFloat(100f) < num2)
								{
									if ((healthProblem.m_Flags & HealthProblemFlags.Trapped) == 0)
									{
										healthProblem.m_Flags &= ~HealthProblemFlags.InDanger;
										nativeArray3[i] = healthProblem;
										GoToSafety(unfilteredChunkIndex, entity, currentBuilding, travelPurpose, currentTransport, tripNeededs);
									}
								}
								else if ((healthProblem.m_Flags & HealthProblemFlags.Trapped) == 0 && random.NextFloat() < m_FireConfigurationData.m_DeathRateOfFireAccident)
								{
									if ((healthProblem.m_Flags & HealthProblemFlags.RequireTransport) != HealthProblemFlags.None)
									{
										m_IconCommandBuffer.Remove(entity, m_HealthcareParameterData.m_AmbulanceNotificationPrefab);
										healthProblem.m_Timer = 0;
									}
									healthProblem.m_Flags &= ~(HealthProblemFlags.InDanger | HealthProblemFlags.Trapped);
									healthProblem.m_Flags |= HealthProblemFlags.Dead | HealthProblemFlags.RequireTransport;
									nativeArray3[i] = healthProblem;
									AddJournalData(unfilteredChunkIndex, healthProblem);
									Entity household = ((nativeArray7.Length != 0) ? nativeArray7[i].m_Household : Entity.Null);
									DeathCheckSystem.PerformAfterDeathActions(nativeArray[i], household, m_TriggerBuffer, m_StatisticsEventQueue, ref m_HouseholdCitizens);
								}
								else
								{
									healthProblem.m_Flags |= HealthProblemFlags.Trapped;
									nativeArray3[i] = healthProblem;
								}
							}
						}
						else if (m_DestroyedData.HasComponent(currentBuilding.m_CurrentBuilding))
						{
							if ((healthProblem.m_Flags & HealthProblemFlags.InDanger) != HealthProblemFlags.None)
							{
								healthProblem.m_Flags &= ~HealthProblemFlags.InDanger;
								nativeArray3[i] = healthProblem;
							}
							if ((healthProblem.m_Flags & HealthProblemFlags.Trapped) != HealthProblemFlags.None)
							{
								Destroyed destroyed = m_DestroyedData[currentBuilding.m_CurrentBuilding];
								if (random.NextFloat(1f) < destroyed.m_Cleared)
								{
									healthProblem.m_Flags &= ~HealthProblemFlags.Trapped;
									nativeArray3[i] = healthProblem;
									GoToSafety(unfilteredChunkIndex, entity, currentBuilding, travelPurpose, currentTransport, tripNeededs);
								}
							}
							else
							{
								GoToSafety(unfilteredChunkIndex, entity, currentBuilding, travelPurpose, currentTransport, tripNeededs);
							}
						}
						else
						{
							healthProblem.m_Flags &= ~(HealthProblemFlags.InDanger | HealthProblemFlags.Trapped);
							nativeArray3[i] = healthProblem;
						}
					}
				}
				if ((healthProblem.m_Flags & HealthProblemFlags.RequireTransport) != HealthProblemFlags.None)
				{
					if ((healthProblem.m_Flags & HealthProblemFlags.Dead) != HealthProblemFlags.None)
					{
						Entity entity2 = nativeArray[i];
						DynamicBuffer<TripNeeded> tripNeededs2 = bufferAccessor[i];
						Entity entity3 = currentBuilding.m_CurrentBuilding;
						if (entity3 == Entity.Null && (travelPurpose.m_Purpose == Purpose.Deathcare || travelPurpose.m_Purpose == Purpose.Hospital) && m_TargetData.HasComponent(currentTransport.m_CurrentTransport))
						{
							entity3 = m_TargetData[currentTransport.m_CurrentTransport].m_Target;
						}
						if (m_DeathcareFacilityData.HasComponent(entity3))
						{
							if ((m_DeathcareFacilityData[entity3].m_Flags & (DeathcareFacilityFlags.CanProcessCorpses | DeathcareFacilityFlags.CanStoreCorpses)) != 0)
							{
								if (healthProblem.m_Timer > 0)
								{
									m_IconCommandBuffer.Remove(entity2, m_HealthcareParameterData.m_HearseNotificationPrefab);
									healthProblem.m_Timer = 0;
									nativeArray3[i] = healthProblem;
								}
								HandleRequest(unfilteredChunkIndex, healthProblem);
								if (entity3 == currentBuilding.m_CurrentBuilding && travelPurpose.m_Purpose != Purpose.Deathcare && travelPurpose.m_Purpose != Purpose.InDeathcare)
								{
									GoToDeathcare(unfilteredChunkIndex, entity2, currentBuilding, travelPurpose, currentTransport, tripNeededs2, entity3);
								}
								continue;
							}
						}
						else if (m_HospitalData.HasComponent(entity3) && (m_HospitalData[entity3].m_Flags & HospitalFlags.CanProcessCorpses) != 0)
						{
							if (healthProblem.m_Timer > 0)
							{
								m_IconCommandBuffer.Remove(entity2, m_HealthcareParameterData.m_HearseNotificationPrefab);
								healthProblem.m_Timer = 0;
								nativeArray3[i] = healthProblem;
							}
							HandleRequest(unfilteredChunkIndex, healthProblem);
							if (entity3 == currentBuilding.m_CurrentBuilding && travelPurpose.m_Purpose != Purpose.Hospital && travelPurpose.m_Purpose != Purpose.InHospital)
							{
								GoToHospital(unfilteredChunkIndex, entity2, currentBuilding, travelPurpose, currentTransport, tripNeededs2, entity3, immediate: true);
							}
							continue;
						}
						if (m_OutsideConnectionData.HasComponent(entity3))
						{
							continue;
						}
						if (RequestVehicleIfNeeded(unfilteredChunkIndex, entity2, currentBuilding, travelPurpose, currentTransport, tripNeededs2, healthProblem))
						{
							if (currentTransport.m_CurrentTransport != Entity.Null || m_StaticData.HasComponent(currentBuilding.m_CurrentBuilding))
							{
								if (healthProblem.m_Timer < num)
								{
									if (++healthProblem.m_Timer == num)
									{
										m_IconCommandBuffer.Add(entity2, m_HealthcareParameterData.m_HearseNotificationPrefab, IconPriority.MajorProblem);
									}
									nativeArray3[i] = healthProblem;
								}
							}
							else if (healthProblem.m_Timer > 0)
							{
								m_IconCommandBuffer.Remove(entity2, m_HealthcareParameterData.m_HearseNotificationPrefab);
								healthProblem.m_Timer = 0;
								nativeArray3[i] = healthProblem;
							}
						}
						else
						{
							m_IconCommandBuffer.Remove(entity2, m_HealthcareParameterData.m_HearseNotificationPrefab);
						}
					}
					else if ((healthProblem.m_Flags & HealthProblemFlags.Injured) != HealthProblemFlags.None)
					{
						Entity entity4 = nativeArray[i];
						DynamicBuffer<TripNeeded> tripNeededs3 = bufferAccessor[i];
						Entity entity5 = currentBuilding.m_CurrentBuilding;
						if (entity5 == Entity.Null && travelPurpose.m_Purpose == Purpose.Hospital && m_TargetData.HasComponent(currentTransport.m_CurrentTransport))
						{
							entity5 = m_TargetData[currentTransport.m_CurrentTransport].m_Target;
						}
						if (m_HospitalData.HasComponent(entity5) && (m_HospitalData[entity5].m_Flags & HospitalFlags.CanCureInjury) != 0)
						{
							if (healthProblem.m_Timer > 0)
							{
								m_IconCommandBuffer.Remove(entity4, m_HealthcareParameterData.m_AmbulanceNotificationPrefab);
								healthProblem.m_Timer = 0;
								nativeArray3[i] = healthProblem;
							}
							HandleRequest(unfilteredChunkIndex, healthProblem);
							if (entity5 == currentBuilding.m_CurrentBuilding && travelPurpose.m_Purpose != Purpose.Hospital && travelPurpose.m_Purpose != Purpose.InHospital)
							{
								GoToHospital(unfilteredChunkIndex, entity4, currentBuilding, travelPurpose, currentTransport, tripNeededs3, entity5, immediate: true);
							}
						}
						else
						{
							if (m_OutsideConnectionData.HasComponent(entity5))
							{
								continue;
							}
							if (RequestVehicleIfNeeded(unfilteredChunkIndex, entity4, currentBuilding, travelPurpose, currentTransport, tripNeededs3, healthProblem))
							{
								if (currentTransport.m_CurrentTransport != Entity.Null || m_StaticData.HasComponent(currentBuilding.m_CurrentBuilding))
								{
									if (healthProblem.m_Timer < num)
									{
										if (++healthProblem.m_Timer == num)
										{
											m_IconCommandBuffer.Add(entity4, m_HealthcareParameterData.m_AmbulanceNotificationPrefab, IconPriority.MajorProblem);
										}
										nativeArray3[i] = healthProblem;
									}
								}
								else if (healthProblem.m_Timer > 0)
								{
									m_IconCommandBuffer.Remove(entity4, m_HealthcareParameterData.m_AmbulanceNotificationPrefab);
									healthProblem.m_Timer = 0;
									nativeArray3[i] = healthProblem;
								}
							}
							else
							{
								m_IconCommandBuffer.Remove(entity4, m_HealthcareParameterData.m_AmbulanceNotificationPrefab);
							}
						}
					}
					else
					{
						if ((healthProblem.m_Flags & (HealthProblemFlags.Sick | HealthProblemFlags.NoHealthcare)) != HealthProblemFlags.Sick)
						{
							continue;
						}
						Entity entity6 = nativeArray[i];
						DynamicBuffer<TripNeeded> tripNeededs4 = bufferAccessor[i];
						Entity entity7 = currentBuilding.m_CurrentBuilding;
						if (entity7 == Entity.Null && travelPurpose.m_Purpose == Purpose.Hospital && m_TargetData.HasComponent(currentTransport.m_CurrentTransport))
						{
							entity7 = m_TargetData[currentTransport.m_CurrentTransport].m_Target;
						}
						if (entity7 == Entity.Null)
						{
							HandleRequest(unfilteredChunkIndex, healthProblem);
						}
						else if (m_HospitalData.HasComponent(entity7) && (m_HospitalData[entity7].m_Flags & HospitalFlags.CanCureDisease) != 0)
						{
							if (healthProblem.m_Timer > 0)
							{
								m_IconCommandBuffer.Remove(entity6, m_HealthcareParameterData.m_AmbulanceNotificationPrefab);
								healthProblem.m_Timer = 0;
								nativeArray3[i] = healthProblem;
							}
							HandleRequest(unfilteredChunkIndex, healthProblem);
							if (entity7 == currentBuilding.m_CurrentBuilding && travelPurpose.m_Purpose != Purpose.Hospital && travelPurpose.m_Purpose != Purpose.InHospital)
							{
								GoToHospital(unfilteredChunkIndex, entity6, currentBuilding, travelPurpose, currentTransport, tripNeededs4, entity7, immediate: true);
							}
						}
						else
						{
							if (m_OutsideConnectionData.HasComponent(entity7))
							{
								continue;
							}
							if (RequestVehicleIfNeeded(unfilteredChunkIndex, entity6, currentBuilding, travelPurpose, currentTransport, tripNeededs4, healthProblem))
							{
								if (currentTransport.m_CurrentTransport != Entity.Null || m_StaticData.HasComponent(currentBuilding.m_CurrentBuilding))
								{
									if (healthProblem.m_Timer < num)
									{
										if (++healthProblem.m_Timer == num)
										{
											m_IconCommandBuffer.Add(entity6, m_HealthcareParameterData.m_AmbulanceNotificationPrefab, IconPriority.MajorProblem);
										}
										nativeArray3[i] = healthProblem;
									}
								}
								else if (healthProblem.m_Timer > 0)
								{
									m_IconCommandBuffer.Remove(entity6, m_HealthcareParameterData.m_AmbulanceNotificationPrefab);
									healthProblem.m_Timer = 0;
									nativeArray3[i] = healthProblem;
								}
							}
							else
							{
								m_IconCommandBuffer.Remove(entity6, m_HealthcareParameterData.m_AmbulanceNotificationPrefab);
							}
						}
					}
				}
				else
				{
					if ((healthProblem.m_Flags & (HealthProblemFlags.Dead | HealthProblemFlags.NoHealthcare)) != HealthProblemFlags.None)
					{
						continue;
					}
					if ((healthProblem.m_Flags & HealthProblemFlags.Sick) != HealthProblemFlags.None)
					{
						Entity entity8 = nativeArray[i];
						DynamicBuffer<TripNeeded> tripNeededs5 = bufferAccessor[i];
						Entity entity9 = currentBuilding.m_CurrentBuilding;
						if (entity9 == Entity.Null && travelPurpose.m_Purpose == Purpose.Hospital && m_TargetData.HasComponent(currentTransport.m_CurrentTransport))
						{
							entity9 = m_TargetData[currentTransport.m_CurrentTransport].m_Target;
						}
						if (entity9 == Entity.Null)
						{
							continue;
						}
						if (m_HospitalData.HasComponent(entity9) && (m_HospitalData[entity9].m_Flags & HospitalFlags.CanCureDisease) != 0)
						{
							if (entity9 == currentBuilding.m_CurrentBuilding && travelPurpose.m_Purpose != Purpose.Hospital && travelPurpose.m_Purpose != Purpose.InHospital)
							{
								GoToHospital(unfilteredChunkIndex, entity8, currentBuilding, travelPurpose, currentTransport, tripNeededs5, entity9, immediate: true);
							}
						}
						else if (!m_OutsideConnectionData.HasComponent(entity9))
						{
							GoToHospital(unfilteredChunkIndex, entity8, currentBuilding, travelPurpose, currentTransport, tripNeededs5, Entity.Null, immediate: false);
						}
					}
					else
					{
						if ((healthProblem.m_Flags & HealthProblemFlags.Injured) == 0)
						{
							continue;
						}
						Entity entity10 = nativeArray[i];
						DynamicBuffer<TripNeeded> tripNeededs6 = bufferAccessor[i];
						Entity entity11 = currentBuilding.m_CurrentBuilding;
						if (entity11 == Entity.Null && travelPurpose.m_Purpose == Purpose.Hospital && m_TargetData.HasComponent(currentTransport.m_CurrentTransport))
						{
							entity11 = m_TargetData[currentTransport.m_CurrentTransport].m_Target;
						}
						if (m_HospitalData.HasComponent(entity11) && (m_HospitalData[entity11].m_Flags & HospitalFlags.CanCureInjury) != 0)
						{
							if (entity11 == currentBuilding.m_CurrentBuilding && travelPurpose.m_Purpose != Purpose.Hospital && travelPurpose.m_Purpose != Purpose.InHospital)
							{
								GoToHospital(unfilteredChunkIndex, entity10, currentBuilding, travelPurpose, currentTransport, tripNeededs6, entity11, immediate: true);
							}
						}
						else if (!m_OutsideConnectionData.HasComponent(entity11))
						{
							GoToHospital(unfilteredChunkIndex, entity10, currentBuilding, travelPurpose, currentTransport, tripNeededs6, Entity.Null, immediate: true);
						}
					}
				}
			}
		}

		private void HandleRequest(int jobIndex, HealthProblem healthProblem)
		{
			if (m_HealthcareRequestData.HasComponent(healthProblem.m_HealthcareRequest))
			{
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(healthProblem.m_HealthcareRequest, Entity.Null, completed: true));
			}
		}

		private bool RequestVehicleIfNeeded(int jobIndex, Entity entity, CurrentBuilding currentBuilding, TravelPurpose travelPurpose, CurrentTransport currentTransport, DynamicBuffer<TripNeeded> tripNeededs, HealthProblem healthProblem)
		{
			if (m_HealthcareRequestData.HasComponent(healthProblem.m_HealthcareRequest))
			{
				if (m_DispatchedData.HasComponent(healthProblem.m_HealthcareRequest))
				{
					Dispatched dispatched = m_DispatchedData[healthProblem.m_HealthcareRequest];
					if ((healthProblem.m_Flags & HealthProblemFlags.Dead) != HealthProblemFlags.None)
					{
						if (m_HearseData.HasComponent(dispatched.m_Handler))
						{
							Game.Vehicles.Hearse hearse = m_HearseData[dispatched.m_Handler];
							if (hearse.m_TargetCorpse == entity && (hearse.m_State & HearseFlags.AtTarget) != 0)
							{
								GoToDeathcare(jobIndex, entity, currentBuilding, travelPurpose, currentTransport, tripNeededs, dispatched.m_Handler);
								return false;
							}
						}
						else if (m_AmbulanceData.HasComponent(dispatched.m_Handler))
						{
							Game.Vehicles.Ambulance ambulance = m_AmbulanceData[dispatched.m_Handler];
							if (ambulance.m_TargetPatient == entity && (ambulance.m_State & AmbulanceFlags.AtTarget) != 0)
							{
								GoToHospital(jobIndex, entity, currentBuilding, travelPurpose, currentTransport, tripNeededs, dispatched.m_Handler, immediate: true);
								return false;
							}
						}
					}
					else if (m_AmbulanceData.HasComponent(dispatched.m_Handler))
					{
						Game.Vehicles.Ambulance ambulance2 = m_AmbulanceData[dispatched.m_Handler];
						if (ambulance2.m_TargetPatient == entity && (ambulance2.m_State & AmbulanceFlags.AtTarget) != 0)
						{
							GoToHospital(jobIndex, entity, currentBuilding, travelPurpose, currentTransport, tripNeededs, dispatched.m_Handler, immediate: true);
							return false;
						}
					}
				}
				if (m_CurrentVehicleData.HasComponent(currentTransport.m_CurrentTransport))
				{
					return false;
				}
				if (m_TargetData.HasComponent(currentTransport.m_CurrentTransport) && !m_DeletedData.HasComponent(currentTransport.m_CurrentTransport) && (m_TargetData[currentTransport.m_CurrentTransport].m_Target != Entity.Null || m_DivertData.HasComponent(currentTransport.m_CurrentTransport)))
				{
					Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_ResetTripArchetype);
					m_CommandBuffer.SetComponent(jobIndex, e, new ResetTrip
					{
						m_Creature = currentTransport.m_CurrentTransport,
						m_Target = Entity.Null
					});
				}
				return true;
			}
			if (m_CurrentVehicleData.HasComponent(currentTransport.m_CurrentTransport))
			{
				return false;
			}
			HealthcareRequestType healthcareRequestType = (((healthProblem.m_Flags & HealthProblemFlags.Dead) != HealthProblemFlags.None) ? HealthcareRequestType.Hearse : HealthcareRequestType.Ambulance);
			bool flag = true;
			if (healthcareRequestType == HealthcareRequestType.Hearse)
			{
				flag = m_PrefabRefData.TryGetComponent(currentBuilding.m_CurrentBuilding, out var componentData) && m_PrefabBuildingData.TryGetComponent(componentData.m_Prefab, out var componentData2) && (componentData2.m_Flags & Game.Prefabs.BuildingFlags.HasInsideRoom) != 0;
			}
			if (flag)
			{
				Entity e2 = m_CommandBuffer.CreateEntity(jobIndex, m_HealthcareRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e2, new HealthcareRequest(entity, healthcareRequestType));
				m_CommandBuffer.SetComponent(jobIndex, e2, new RequestGroup(16u));
			}
			if (m_TargetData.HasComponent(currentTransport.m_CurrentTransport) && !m_DeletedData.HasComponent(currentTransport.m_CurrentTransport))
			{
				if (m_TargetData[currentTransport.m_CurrentTransport].m_Target != Entity.Null || m_DivertData.HasComponent(currentTransport.m_CurrentTransport))
				{
					Entity e3 = m_CommandBuffer.CreateEntity(jobIndex, m_ResetTripArchetype);
					m_CommandBuffer.SetComponent(jobIndex, e3, new ResetTrip
					{
						m_Creature = currentTransport.m_CurrentTransport,
						m_Target = Entity.Null
					});
				}
			}
			else if (!flag)
			{
				m_CommandBuffer.RemoveComponent<CurrentBuilding>(jobIndex, entity);
				m_CommandBuffer.AddComponent(jobIndex, entity, new TravelPurpose
				{
					m_Purpose = Purpose.GoingHome
				});
			}
			return true;
		}

		private void GoToHospital(int jobIndex, Entity entity, CurrentBuilding currentBuilding, TravelPurpose travelPurpose, CurrentTransport currentTransport, DynamicBuffer<TripNeeded> tripNeededs, Entity ambulance, bool immediate)
		{
			if (currentBuilding.m_CurrentBuilding != Entity.Null)
			{
				if (immediate)
				{
					tripNeededs.Clear();
				}
				else
				{
					for (int i = 0; i < tripNeededs.Length; i++)
					{
						if (tripNeededs[i].m_Purpose == Purpose.Hospital)
						{
							return;
						}
					}
				}
				tripNeededs.Add(new TripNeeded
				{
					m_Purpose = Purpose.Hospital,
					m_TargetAgent = ambulance
				});
				if (immediate)
				{
					m_CommandBuffer.RemoveComponent<ResourceBuyer>(jobIndex, entity);
					m_CommandBuffer.RemoveComponent<TravelPurpose>(jobIndex, entity);
					m_CommandBuffer.RemoveComponent<AttendingMeeting>(jobIndex, entity);
				}
			}
			else if (immediate && ambulance != Entity.Null && m_TargetData.HasComponent(currentTransport.m_CurrentTransport) && !m_DeletedData.HasComponent(currentTransport.m_CurrentTransport) && (!m_CurrentVehicleData.HasComponent(currentTransport.m_CurrentTransport) || !(m_CurrentVehicleData[currentTransport.m_CurrentTransport].m_Vehicle == ambulance)) && (travelPurpose.m_Purpose != Purpose.Hospital || m_TargetData[currentTransport.m_CurrentTransport].m_Target != ambulance || m_DivertData.HasComponent(currentTransport.m_CurrentTransport)))
			{
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_ResetTripArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new ResetTrip
				{
					m_Creature = currentTransport.m_CurrentTransport,
					m_Target = ambulance,
					m_TravelPurpose = Purpose.Hospital
				});
			}
		}

		private void GoToDeathcare(int jobIndex, Entity entity, CurrentBuilding currentBuilding, TravelPurpose travelPurpose, CurrentTransport currentTransport, DynamicBuffer<TripNeeded> tripNeededs, Entity hearse)
		{
			if (currentBuilding.m_CurrentBuilding != Entity.Null)
			{
				tripNeededs.Clear();
				tripNeededs.Add(new TripNeeded
				{
					m_Purpose = Purpose.Deathcare,
					m_TargetAgent = hearse
				});
				m_CommandBuffer.RemoveComponent<ResourceBuyer>(jobIndex, entity);
				m_CommandBuffer.RemoveComponent<TravelPurpose>(jobIndex, entity);
				m_CommandBuffer.RemoveComponent<AttendingMeeting>(jobIndex, entity);
			}
			else if (hearse != Entity.Null && m_TargetData.HasComponent(currentTransport.m_CurrentTransport) && !m_DeletedData.HasComponent(currentTransport.m_CurrentTransport) && (!m_CurrentVehicleData.HasComponent(currentTransport.m_CurrentTransport) || !(m_CurrentVehicleData[currentTransport.m_CurrentTransport].m_Vehicle == hearse)) && (travelPurpose.m_Purpose != Purpose.Deathcare || m_TargetData[currentTransport.m_CurrentTransport].m_Target != hearse || m_DivertData.HasComponent(currentTransport.m_CurrentTransport)))
			{
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_ResetTripArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new ResetTrip
				{
					m_Creature = currentTransport.m_CurrentTransport,
					m_Target = hearse,
					m_TravelPurpose = Purpose.Deathcare
				});
			}
		}

		private void GoToSafety(int jobIndex, Entity entity, CurrentBuilding currentBuilding, TravelPurpose travelPurpose, CurrentTransport currentTransport, DynamicBuffer<TripNeeded> tripNeededs)
		{
			if (currentBuilding.m_CurrentBuilding != Entity.Null)
			{
				tripNeededs.Clear();
				tripNeededs.Add(new TripNeeded
				{
					m_Purpose = Purpose.Safety
				});
				m_CommandBuffer.RemoveComponent<ResourceBuyer>(jobIndex, entity);
				m_CommandBuffer.RemoveComponent<TravelPurpose>(jobIndex, entity);
				m_CommandBuffer.RemoveComponent<AttendingMeeting>(jobIndex, entity);
			}
		}

		private void AddJournalData(int chunkIndex, HealthProblem problem)
		{
			Entity e = m_CommandBuffer.CreateEntity(chunkIndex, m_JournalDataArchetype);
			m_CommandBuffer.SetComponent(chunkIndex, e, new AddEventJournalData(problem.m_Event, EventDataTrackingType.Casualties));
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
		public ComponentTypeHandle<CurrentTransport> __Game_Citizens_CurrentTransport_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Citizen> __Game_Citizens_Citizen_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TravelPurpose> __Game_Citizens_TravelPurpose_RO_ComponentTypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		public ComponentTypeHandle<HealthProblem> __Game_Citizens_HealthProblem_RW_ComponentTypeHandle;

		public BufferTypeHandle<TripNeeded> __Game_Citizens_TripNeeded_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<HealthcareRequest> __Game_Simulation_HealthcareRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Hospital> __Game_Buildings_Hospital_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.DeathcareFacility> __Game_Buildings_DeathcareFacility_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<OnFire> __Game_Events_OnFire_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Dispatched> __Game_Simulation_Dispatched_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Target> __Game_Common_Target_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Static> __Game_Objects_Static_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.Ambulance> __Game_Vehicles_Ambulance_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.Hearse> __Game_Vehicles_Hearse_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> __Game_Creatures_CurrentVehicle_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Divert> __Game_Creatures_Divert_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentBuilding>(isReadOnly: true);
			__Game_Citizens_CurrentTransport_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentTransport>(isReadOnly: true);
			__Game_Citizens_Citizen_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Citizen>(isReadOnly: true);
			__Game_Citizens_TravelPurpose_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TravelPurpose>(isReadOnly: true);
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Citizens_HealthProblem_RW_ComponentTypeHandle = state.GetComponentTypeHandle<HealthProblem>();
			__Game_Citizens_TripNeeded_RW_BufferTypeHandle = state.GetBufferTypeHandle<TripNeeded>();
			__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HouseholdMember>(isReadOnly: true);
			__Game_Simulation_HealthcareRequest_RO_ComponentLookup = state.GetComponentLookup<HealthcareRequest>(isReadOnly: true);
			__Game_Buildings_Hospital_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.Hospital>(isReadOnly: true);
			__Game_Buildings_DeathcareFacility_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.DeathcareFacility>(isReadOnly: true);
			__Game_Events_OnFire_RO_ComponentLookup = state.GetComponentLookup<OnFire>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Simulation_Dispatched_RO_ComponentLookup = state.GetComponentLookup<Dispatched>(isReadOnly: true);
			__Game_Common_Target_RO_ComponentLookup = state.GetComponentLookup<Target>(isReadOnly: true);
			__Game_Objects_Static_RO_ComponentLookup = state.GetComponentLookup<Static>(isReadOnly: true);
			__Game_Objects_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.OutsideConnection>(isReadOnly: true);
			__Game_Vehicles_Ambulance_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.Ambulance>(isReadOnly: true);
			__Game_Vehicles_Hearse_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.Hearse>(isReadOnly: true);
			__Game_Creatures_CurrentVehicle_RO_ComponentLookup = state.GetComponentLookup<CurrentVehicle>(isReadOnly: true);
			__Game_Creatures_Divert_RO_ComponentLookup = state.GetComponentLookup<Divert>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
		}
	}

	private const uint SYSTEM_UPDATE_INTERVAL = 16u;

	private EndFrameBarrier m_EndFrameBarrier;

	private SimulationSystem m_SimulationSystem;

	private IconCommandSystem m_IconCommandSystem;

	private EntityArchetype m_HealthcareRequestArchetype;

	private EntityArchetype m_JournalDataArchetype;

	private EntityArchetype m_ResetTripArchetype;

	private EntityArchetype m_HandleRequestArchetype;

	private EntityQuery m_HealthProblemQuery;

	private EntityQuery m_HealthcareSettingsQuery;

	private EntityQuery m_FireSettingsQuery;

	private TriggerSystem m_TriggerSystem;

	private CityStatisticsSystem m_CityStatisticsSystem;

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
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_HealthProblemQuery = GetEntityQuery(ComponentType.ReadOnly<Citizen>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadWrite<HealthProblem>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_HealthcareSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<HealthcareParameterData>());
		m_FireSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<FireConfigurationData>());
		m_HealthcareRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<HealthcareRequest>(), ComponentType.ReadWrite<RequestGroup>());
		m_JournalDataArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<AddEventJournalData>(), ComponentType.ReadWrite<Game.Common.Event>());
		m_ResetTripArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<ResetTrip>());
		m_HandleRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<HandleRequest>(), ComponentType.ReadWrite<Game.Common.Event>());
		RequireForUpdate(m_HealthProblemQuery);
		RequireForUpdate(m_HealthcareSettingsQuery);
		Assert.IsTrue(condition: true);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrameIndex = (m_SimulationSystem.frameIndex / 16) & 0xF;
		JobHandle deps;
		HealthProblemJob jobData = new HealthProblemJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CurrentBuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentTransportType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_CurrentTransport_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CitizenType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TravelPurposeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_HealthProblemType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_HealthProblem_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TripNeededType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Citizens_TripNeeded_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_HouseholdMemberType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HealthcareRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_HealthcareRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HospitalData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Hospital_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DeathcareFacilityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_DeathcareFacility_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OnFireData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_OnFire_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DestroyedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DispatchedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_Dispatched_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TargetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Target_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StaticData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Static_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OutsideConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AmbulanceData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Ambulance_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HearseData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Hearse_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DivertData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Divert_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
			m_TriggerBuffer = m_TriggerSystem.CreateActionBuffer().AsParallelWriter(),
			m_StatisticsEventQueue = m_CityStatisticsSystem.GetStatisticsEventQueue(out deps).AsParallelWriter(),
			m_UpdateFrameIndex = updateFrameIndex,
			m_RandomSeed = RandomSeed.Next(),
			m_HealthcareRequestArchetype = m_HealthcareRequestArchetype,
			m_JournalDataArchetype = m_JournalDataArchetype,
			m_ResetTripArchetype = m_ResetTripArchetype,
			m_HandleRequestArchetype = m_HandleRequestArchetype,
			m_HealthcareParameterData = m_HealthcareSettingsQuery.GetSingleton<HealthcareParameterData>(),
			m_FireConfigurationData = m_FireSettingsQuery.GetSingleton<FireConfigurationData>(),
			m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_HealthProblemQuery, JobHandle.CombineDependencies(base.Dependency, deps));
		m_CityStatisticsSystem.AddWriter(base.Dependency);
		m_TriggerSystem.AddActionBufferWriter(base.Dependency);
		m_IconCommandSystem.AddCommandBufferWriter(base.Dependency);
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
	public HealthProblemSystem()
	{
	}
}

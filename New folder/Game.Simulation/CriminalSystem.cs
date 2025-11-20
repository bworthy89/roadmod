using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Creatures;
using Game.Economy;
using Game.Events;
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
public class CriminalSystem : GameSystemBase
{
	private struct CrimeData
	{
		public Entity m_Source;

		public Entity m_Target;

		public int m_StealAmount;

		public int m_EffectAmount;
	}

	[BurstCompile]
	private struct CriminalJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<TravelPurpose> m_TravelPurposeType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentBuilding> m_CurrentBuildingType;

		[ReadOnly]
		public ComponentTypeHandle<HealthProblem> m_HealthProblemType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		public ComponentTypeHandle<Criminal> m_CriminalType;

		public BufferTypeHandle<TripNeeded> m_TripNeededType;

		[ReadOnly]
		public ComponentLookup<AccidentSite> m_AccidentSiteData;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> m_HouseholdMemberData;

		[ReadOnly]
		public ComponentLookup<CurrentTransport> m_CurrentTransports;

		[ReadOnly]
		public ComponentLookup<Game.Creatures.Resident> m_Residents;

		[ReadOnly]
		public ComponentLookup<Dispatched> m_DispatchedData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.PoliceStation> m_PoliceStationData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Prison> m_PrisonData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PublicTransport> m_PublicTransportData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PoliceCar> m_PoliceCarData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<Game.Prefabs.CrimeData> m_PrefabCrimeData;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		[ReadOnly]
		public BufferLookup<ServiceDispatch> m_ServiceDispatches;

		[ReadOnly]
		public BufferLookup<Employee> m_Employees;

		[ReadOnly]
		public BufferLookup<Renter> m_Renters;

		[ReadOnly]
		public BufferLookup<Resources> m_Resources;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		public BufferLookup<Occupant> m_Occupants;

		[ReadOnly]
		public uint m_UpdateFrameIndex;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public PoliceConfigurationData m_PoliceConfigurationData;

		[ReadOnly]
		public EntityArchetype m_AddAccidentSiteArchetype;

		[ReadOnly]
		public Entity m_City;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<CrimeData>.ParallelWriter m_CrimeQueue;

		public NativeQueue<TriggerAction>.ParallelWriter m_TriggerBuffer;

		public NativeQueue<StatisticsEvent>.ParallelWriter m_StatisticsEventQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<TravelPurpose> nativeArray2 = chunk.GetNativeArray(ref m_TravelPurposeType);
			NativeArray<CurrentBuilding> nativeArray3 = chunk.GetNativeArray(ref m_CurrentBuildingType);
			NativeArray<HealthProblem> nativeArray4 = chunk.GetNativeArray(ref m_HealthProblemType);
			NativeArray<Criminal> nativeArray5 = chunk.GetNativeArray(ref m_CriminalType);
			BufferAccessor<TripNeeded> bufferAccessor = chunk.GetBufferAccessor(ref m_TripNeededType);
			DynamicBuffer<CityModifier> modifiers = m_CityModifiers[m_City];
			for (int i = 0; i < nativeArray5.Length; i++)
			{
				Criminal value = nativeArray5[i];
				if (value.m_Flags == (CriminalFlags)0)
				{
					Entity e = nativeArray[i];
					m_CommandBuffer.RemoveComponent<Criminal>(unfilteredChunkIndex, e);
				}
				else if ((value.m_Flags & CriminalFlags.Prisoner) != 0)
				{
					if (nativeArray3.Length == 0)
					{
						continue;
					}
					CurrentBuilding currentBuilding = nativeArray3[i];
					if (m_PrisonData.HasComponent(currentBuilding.m_CurrentBuilding))
					{
						if (m_BuildingData.HasComponent(currentBuilding.m_CurrentBuilding) && BuildingUtils.CheckOption(m_BuildingData[currentBuilding.m_CurrentBuilding], BuildingOption.Inactive))
						{
							Entity entity = nativeArray[i];
							RemoveTravelPurpose(unfilteredChunkIndex, entity, nativeArray2, i);
							if (m_Occupants.HasBuffer(currentBuilding.m_CurrentBuilding))
							{
								CollectionUtils.RemoveValue(m_Occupants[currentBuilding.m_CurrentBuilding], new Occupant(entity));
								continue;
							}
						}
						value.m_JailTime = (ushort)math.max(0, value.m_JailTime - 1);
						if (value.m_JailTime == 0)
						{
							Entity entity2 = nativeArray[i];
							value.m_Flags = (CriminalFlags)0;
							value.m_Event = Entity.Null;
							RemoveTravelPurpose(unfilteredChunkIndex, entity2, nativeArray2, i);
							m_CommandBuffer.RemoveComponent<Criminal>(unfilteredChunkIndex, entity2);
						}
						nativeArray5[i] = value;
					}
					else
					{
						Entity entity3 = nativeArray[i];
						value.m_Flags &= ~(CriminalFlags.Prisoner | CriminalFlags.Arrested | CriminalFlags.Sentenced);
						value.m_Event = Entity.Null;
						nativeArray5[i] = value;
						RemoveTravelPurpose(unfilteredChunkIndex, entity3, nativeArray2, i);
					}
				}
				else if ((value.m_Flags & CriminalFlags.Arrested) != 0)
				{
					if (nativeArray3.Length == 0)
					{
						continue;
					}
					CurrentBuilding currentBuilding2 = nativeArray3[i];
					if (m_PoliceStationData.HasComponent(currentBuilding2.m_CurrentBuilding))
					{
						if (m_BuildingData.HasComponent(currentBuilding2.m_CurrentBuilding) && BuildingUtils.CheckOption(m_BuildingData[currentBuilding2.m_CurrentBuilding], BuildingOption.Inactive))
						{
							Entity entity4 = nativeArray[i];
							RemoveTravelPurpose(unfilteredChunkIndex, entity4, nativeArray2, i);
							if (m_Occupants.HasBuffer(currentBuilding2.m_CurrentBuilding))
							{
								CollectionUtils.RemoveValue(m_Occupants[currentBuilding2.m_CurrentBuilding], new Occupant(entity4));
								continue;
							}
						}
						if ((value.m_Flags & CriminalFlags.Sentenced) != 0)
						{
							Game.Buildings.PoliceStation policeStation = m_PoliceStationData[currentBuilding2.m_CurrentBuilding];
							if (GetTransportVehicle(policeStation, out var vehicle) && CheckHealth(nativeArray4, i))
							{
								Entity entity5 = nativeArray[i];
								DynamicBuffer<TripNeeded> tripNeededs = bufferAccessor[i];
								value.m_Flags |= CriminalFlags.Prisoner;
								value.m_Event = Entity.Null;
								nativeArray5[i] = value;
								GoToPrison(unfilteredChunkIndex, entity5, tripNeededs, vehicle);
								continue;
							}
							value.m_JailTime = (ushort)math.max(0, value.m_JailTime - 1);
							if (value.m_JailTime == 0)
							{
								Entity entity6 = nativeArray[i];
								value.m_Flags = (CriminalFlags)0;
								value.m_Event = Entity.Null;
								RemoveTravelPurpose(unfilteredChunkIndex, entity6, nativeArray2, i);
								m_CommandBuffer.RemoveComponent<Criminal>(unfilteredChunkIndex, entity6);
							}
							nativeArray5[i] = value;
							continue;
						}
						value.m_JailTime = (ushort)math.max(0, value.m_JailTime - 1);
						if (value.m_JailTime == 0)
						{
							Entity entity7 = nativeArray[i];
							Random random = m_RandomSeed.GetRandom(entity7.Index);
							if (m_PrefabRefData.HasComponent(value.m_Event))
							{
								PrefabRef prefabRef = m_PrefabRefData[value.m_Event];
								if (m_PrefabCrimeData.HasComponent(prefabRef.m_Prefab))
								{
									Game.Prefabs.CrimeData crimeData = m_PrefabCrimeData[prefabRef.m_Prefab];
									if (random.NextFloat(100f) < crimeData.m_PrisonProbability)
									{
										float value2 = math.lerp(crimeData.m_PrisonTimeRange.min, crimeData.m_PrisonTimeRange.max, random.NextFloat(1f));
										CityUtils.ApplyModifier(ref value2, modifiers, CityModifierType.PrisonTime);
										value.m_Flags |= CriminalFlags.Sentenced;
										value.m_JailTime = (ushort)math.min(65535f, value2 * 262144f / 256f);
										value.m_Event = Entity.Null;
										m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizenGotSentencedToPrison, Entity.Null, entity7, Entity.Null));
									}
								}
							}
							if ((value.m_Flags & CriminalFlags.Sentenced) == 0)
							{
								value.m_Flags &= ~CriminalFlags.Arrested;
								value.m_Event = Entity.Null;
								RemoveTravelPurpose(unfilteredChunkIndex, entity7, nativeArray2, i);
							}
						}
						nativeArray5[i] = value;
					}
					else
					{
						Entity entity8 = nativeArray[i];
						value.m_Flags &= ~(CriminalFlags.Arrested | CriminalFlags.Sentenced);
						value.m_Event = Entity.Null;
						nativeArray5[i] = value;
						RemoveTravelPurpose(unfilteredChunkIndex, entity8, nativeArray2, i);
					}
				}
				else if ((value.m_Flags & CriminalFlags.Planning) != 0)
				{
					Entity entity9 = nativeArray[i];
					DynamicBuffer<TripNeeded> tripNeededs2 = bufferAccessor[i];
					Random random2 = m_RandomSeed.GetRandom(entity9.Index);
					if (!IsPreparingCrime(tripNeededs2))
					{
						tripNeededs2.Add(new TripNeeded
						{
							m_Purpose = Purpose.Crime
						});
					}
					float value3 = 0f;
					CityUtils.ApplyModifier(ref value3, modifiers, CityModifierType.CriminalMonitorProbability);
					if (random2.NextFloat(100f) < value3)
					{
						value.m_Flags |= CriminalFlags.Monitored;
					}
					value.m_Flags &= ~CriminalFlags.Planning;
					value.m_Flags |= CriminalFlags.Preparing;
					nativeArray5[i] = value;
				}
				else if ((value.m_Flags & CriminalFlags.Preparing) != 0)
				{
					DynamicBuffer<TripNeeded> tripNeededs3 = bufferAccessor[i];
					if (!IsPreparingCrime(tripNeededs3))
					{
						value.m_Flags &= ~CriminalFlags.Preparing;
						nativeArray5[i] = value;
					}
				}
				else
				{
					if (!(value.m_Event != Entity.Null))
					{
						continue;
					}
					TravelPurpose travelPurpose = default(TravelPurpose);
					CurrentBuilding currentBuilding3 = default(CurrentBuilding);
					if (nativeArray2.Length != 0)
					{
						travelPurpose = nativeArray2[i];
					}
					if (nativeArray3.Length != 0)
					{
						currentBuilding3 = nativeArray3[i];
					}
					if (travelPurpose.m_Purpose == Purpose.GoingToJail && m_CurrentTransports.HasComponent(nativeArray[i]) && m_Residents.HasComponent(m_CurrentTransports[nativeArray[i]].m_CurrentTransport) && (m_Residents[m_CurrentTransports[nativeArray[i]].m_CurrentTransport].m_Flags & ResidentFlags.InVehicle) != ResidentFlags.None)
					{
						value.m_Flags |= CriminalFlags.Arrested;
						nativeArray5[i] = value;
					}
					else if (travelPurpose.m_Purpose != Purpose.Crime && travelPurpose.m_Purpose != Purpose.GoingToJail)
					{
						value.m_Event = Entity.Null;
						value.m_Flags &= ~CriminalFlags.Monitored;
						nativeArray5[i] = value;
					}
					else if (m_AccidentSiteData.HasComponent(currentBuilding3.m_CurrentBuilding))
					{
						AccidentSite accidentSite = m_AccidentSiteData[currentBuilding3.m_CurrentBuilding];
						if ((accidentSite.m_Flags & (AccidentSiteFlags.Secured | AccidentSiteFlags.CrimeScene | AccidentSiteFlags.CrimeFinished)) == AccidentSiteFlags.CrimeScene && !(accidentSite.m_Event != value.m_Event))
						{
							continue;
						}
						Entity entity10 = nativeArray[i];
						DynamicBuffer<TripNeeded> tripNeededs4 = bufferAccessor[i];
						Random random3 = m_RandomSeed.GetRandom(entity10.Index);
						if (!CheckHealth(nativeArray4, i))
						{
							continue;
						}
						if ((accidentSite.m_Flags & AccidentSiteFlags.Secured) != 0 && GetPoliceCar(accidentSite, out var vehicle2))
						{
							float num = 0f;
							if (m_PrefabRefData.HasComponent(value.m_Event))
							{
								PrefabRef prefabRef2 = m_PrefabRefData[value.m_Event];
								if (m_PrefabCrimeData.HasComponent(prefabRef2.m_Prefab))
								{
									Game.Prefabs.CrimeData crimeData2 = m_PrefabCrimeData[prefabRef2.m_Prefab];
									num = math.lerp(crimeData2.m_JailTimeRange.min, crimeData2.m_JailTimeRange.max, random3.NextFloat(1f));
								}
							}
							value.m_Flags &= ~CriminalFlags.Monitored;
							value.m_JailTime = (ushort)math.min(65535f, num * 262144f / 256f);
							nativeArray5[i] = value;
							GoToJail(unfilteredChunkIndex, entity10, tripNeededs4, vehicle2);
							m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizenGotArrested, Entity.Null, entity10, value.m_Event));
							continue;
						}
						Entity crimeSource = GetCrimeSource(ref random3, currentBuilding3.m_CurrentBuilding);
						Entity crimeTarget = GetCrimeTarget(entity10);
						int num2 = 0;
						if (m_PrefabRefData.HasComponent(value.m_Event))
						{
							PrefabRef prefabRef3 = m_PrefabRefData[value.m_Event];
							if (m_PrefabCrimeData.HasComponent(prefabRef3.m_Prefab))
							{
								Game.Prefabs.CrimeData crimeData3 = m_PrefabCrimeData[prefabRef3.m_Prefab];
								if (crimeData3.m_CrimeType == CrimeType.Robbery)
								{
									num2 = GetStealAmount(ref random3, crimeSource, crimeData3);
									if (num2 > 0)
									{
										m_CrimeQueue.Enqueue(new CrimeData
										{
											m_Source = crimeSource,
											m_Target = crimeTarget,
											m_StealAmount = num2
										});
									}
								}
							}
						}
						AddCrimeEffects(crimeSource);
						value.m_Event = Entity.Null;
						value.m_Flags &= ~CriminalFlags.Monitored;
						nativeArray5[i] = value;
						TryEscape(unfilteredChunkIndex, entity10, tripNeededs4);
						m_StatisticsEventQueue.Enqueue(new StatisticsEvent
						{
							m_Statistic = StatisticType.EscapedArrestCount,
							m_Change = 1f
						});
					}
					else if (currentBuilding3.m_CurrentBuilding != Entity.Null)
					{
						AddCrimeScene(unfilteredChunkIndex, value.m_Event, currentBuilding3.m_CurrentBuilding);
					}
				}
			}
		}

		private void RemoveTravelPurpose(int jobIndex, Entity entity, NativeArray<TravelPurpose> travelPurposes, int index)
		{
			if (CollectionUtils.TryGet(travelPurposes, index, out var value) && (value.m_Purpose == Purpose.GoingToPrison || value.m_Purpose == Purpose.InPrison || value.m_Purpose == Purpose.GoingToJail || value.m_Purpose == Purpose.InJail))
			{
				m_CommandBuffer.RemoveComponent<TravelPurpose>(jobIndex, entity);
			}
		}

		private bool CheckHealth(NativeArray<HealthProblem> healthProblems, int index)
		{
			if (CollectionUtils.TryGet(healthProblems, index, out var value) && (value.m_Flags & HealthProblemFlags.RequireTransport) != HealthProblemFlags.None)
			{
				return false;
			}
			return true;
		}

		private void GoToPrison(int jobIndex, Entity entity, DynamicBuffer<TripNeeded> tripNeededs, Entity vehicle)
		{
			for (int i = 0; i < tripNeededs.Length; i++)
			{
				if (tripNeededs[i].m_Purpose == Purpose.GoingToPrison)
				{
					return;
				}
			}
			tripNeededs.Add(new TripNeeded
			{
				m_Purpose = Purpose.GoingToPrison,
				m_TargetAgent = vehicle
			});
			m_CommandBuffer.RemoveComponent<TravelPurpose>(jobIndex, entity);
		}

		private void GoToJail(int jobIndex, Entity entity, DynamicBuffer<TripNeeded> tripNeededs, Entity vehicle)
		{
			for (int i = 0; i < tripNeededs.Length; i++)
			{
				if (tripNeededs[i].m_Purpose == Purpose.GoingToJail)
				{
					return;
				}
			}
			tripNeededs.Add(new TripNeeded
			{
				m_Purpose = Purpose.GoingToJail,
				m_TargetAgent = vehicle
			});
			m_CommandBuffer.RemoveComponent<TravelPurpose>(jobIndex, entity);
		}

		private bool GetTransportVehicle(Game.Buildings.PoliceStation policeStation, out Entity vehicle)
		{
			vehicle = Entity.Null;
			if (!m_DispatchedData.HasComponent(policeStation.m_PrisonerTransportRequest))
			{
				return false;
			}
			Dispatched dispatched = m_DispatchedData[policeStation.m_PrisonerTransportRequest];
			if (!m_PublicTransportData.HasComponent(dispatched.m_Handler))
			{
				return false;
			}
			if ((m_PublicTransportData[dispatched.m_Handler].m_State & PublicTransportFlags.Boarding) == 0)
			{
				return false;
			}
			if (!m_ServiceDispatches.HasBuffer(dispatched.m_Handler))
			{
				return false;
			}
			DynamicBuffer<ServiceDispatch> dynamicBuffer = m_ServiceDispatches[dispatched.m_Handler];
			if (dynamicBuffer.Length == 0 || dynamicBuffer[0].m_Request != policeStation.m_PrisonerTransportRequest)
			{
				return false;
			}
			vehicle = dispatched.m_Handler;
			return true;
		}

		private bool GetPoliceCar(AccidentSite accidentSite, out Entity vehicle)
		{
			vehicle = Entity.Null;
			if (!m_DispatchedData.HasComponent(accidentSite.m_PoliceRequest))
			{
				return false;
			}
			Dispatched dispatched = m_DispatchedData[accidentSite.m_PoliceRequest];
			if (!m_PoliceCarData.HasComponent(dispatched.m_Handler))
			{
				return false;
			}
			if ((m_PoliceCarData[dispatched.m_Handler].m_State & PoliceCarFlags.AtTarget) == 0)
			{
				return false;
			}
			if (!m_ServiceDispatches.HasBuffer(dispatched.m_Handler))
			{
				return false;
			}
			DynamicBuffer<ServiceDispatch> dynamicBuffer = m_ServiceDispatches[dispatched.m_Handler];
			if (dynamicBuffer.Length == 0 || dynamicBuffer[0].m_Request != accidentSite.m_PoliceRequest)
			{
				return false;
			}
			vehicle = dispatched.m_Handler;
			return true;
		}

		private void AddCrimeEffects(Entity source)
		{
			if (m_HouseholdCitizens.HasBuffer(source))
			{
				DynamicBuffer<HouseholdCitizen> dynamicBuffer = m_HouseholdCitizens[source];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity citizen = dynamicBuffer[i].m_Citizen;
					if (m_PrefabRefData.HasComponent(citizen))
					{
						m_CrimeQueue.Enqueue(new CrimeData
						{
							m_Source = citizen,
							m_Target = Entity.Null,
							m_EffectAmount = m_PoliceConfigurationData.m_HomeCrimeEffect
						});
					}
				}
			}
			if (!m_Employees.HasBuffer(source))
			{
				return;
			}
			DynamicBuffer<Employee> dynamicBuffer2 = m_Employees[source];
			for (int j = 0; j < dynamicBuffer2.Length; j++)
			{
				Entity worker = dynamicBuffer2[j].m_Worker;
				if (m_PrefabRefData.HasComponent(worker))
				{
					m_CrimeQueue.Enqueue(new CrimeData
					{
						m_Source = worker,
						m_Target = Entity.Null,
						m_EffectAmount = m_PoliceConfigurationData.m_WorkplaceCrimeEffect
					});
				}
			}
		}

		private Entity GetCrimeSource(ref Random random, Entity building)
		{
			if (m_Renters.HasBuffer(building))
			{
				DynamicBuffer<Renter> dynamicBuffer = m_Renters[building];
				if (dynamicBuffer.Length > 0)
				{
					return dynamicBuffer[random.NextInt(dynamicBuffer.Length)].m_Renter;
				}
			}
			return building;
		}

		private Entity GetCrimeTarget(Entity criminal)
		{
			if (m_HouseholdMemberData.HasComponent(criminal))
			{
				return m_HouseholdMemberData[criminal].m_Household;
			}
			return criminal;
		}

		private int GetStealAmount(ref Random random, Entity source, Game.Prefabs.CrimeData crimeData)
		{
			float num = 0f;
			if (m_Resources.HasBuffer(source))
			{
				DynamicBuffer<Resources> resources = m_Resources[source];
				int resources2 = EconomyUtils.GetResources(Resource.Money, resources);
				if (resources2 > 0)
				{
					num += math.lerp(crimeData.m_CrimeIncomeRelative.min, crimeData.m_CrimeIncomeRelative.max, random.NextFloat(1f)) * (float)resources2;
				}
				num += math.lerp(crimeData.m_CrimeIncomeAbsolute.min, crimeData.m_CrimeIncomeAbsolute.max, random.NextFloat(1f));
			}
			return (int)num;
		}

		private void TryEscape(int jobIndex, Entity entity, DynamicBuffer<TripNeeded> tripNeededs)
		{
			tripNeededs.Clear();
			tripNeededs.Add(new TripNeeded
			{
				m_Purpose = Purpose.Escape
			});
			m_CommandBuffer.RemoveComponent<ResourceBuyer>(jobIndex, entity);
			m_CommandBuffer.RemoveComponent<TravelPurpose>(jobIndex, entity);
			m_CommandBuffer.RemoveComponent<AttendingMeeting>(jobIndex, entity);
		}

		private bool IsPreparingCrime(DynamicBuffer<TripNeeded> tripNeededs)
		{
			for (int i = 0; i < tripNeededs.Length; i++)
			{
				if (tripNeededs[i].m_Purpose == Purpose.Crime)
				{
					return true;
				}
			}
			return false;
		}

		private void AddCrimeScene(int jobIndex, Entity _event, Entity building)
		{
			AddAccidentSite component = new AddAccidentSite
			{
				m_Event = _event,
				m_Target = building,
				m_Flags = AccidentSiteFlags.CrimeScene
			};
			Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_AddAccidentSiteArchetype);
			m_CommandBuffer.SetComponent(jobIndex, e, component);
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct CrimeJob : IJob
	{
		public ComponentLookup<CrimeVictim> m_CrimeVictimData;

		public BufferLookup<Resources> m_Resources;

		public NativeQueue<CrimeData> m_CrimeQueue;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			int count = m_CrimeQueue.Count;
			if (count == 0)
			{
				return;
			}
			NativeParallelHashMap<Entity, CrimeVictim> nativeParallelHashMap = new NativeParallelHashMap<Entity, CrimeVictim>(count, Allocator.Temp);
			for (int i = 0; i < count; i++)
			{
				CrimeData crimeData = m_CrimeQueue.Dequeue();
				if (crimeData.m_StealAmount > 0)
				{
					if (!m_Resources.HasBuffer(crimeData.m_Source) || !m_Resources.HasBuffer(crimeData.m_Target))
					{
						continue;
					}
					DynamicBuffer<Resources> resources = m_Resources[crimeData.m_Source];
					DynamicBuffer<Resources> resources2 = m_Resources[crimeData.m_Target];
					EconomyUtils.AddResources(Resource.Money, -crimeData.m_StealAmount, resources);
					EconomyUtils.AddResources(Resource.Money, crimeData.m_StealAmount, resources2);
				}
				if (crimeData.m_EffectAmount > 0)
				{
					if (nativeParallelHashMap.TryGetValue(crimeData.m_Source, out var item))
					{
						item.m_Effect = (byte)math.min(item.m_Effect + crimeData.m_EffectAmount, 255);
						nativeParallelHashMap[crimeData.m_Source] = item;
					}
					else if (m_CrimeVictimData.HasComponent(crimeData.m_Source) && m_CrimeVictimData.IsComponentEnabled(crimeData.m_Source))
					{
						item = m_CrimeVictimData[crimeData.m_Source];
						item.m_Effect = (byte)math.min(item.m_Effect + crimeData.m_EffectAmount, 255);
						nativeParallelHashMap.Add(crimeData.m_Source, item);
					}
					else
					{
						item.m_Effect = (byte)math.min(crimeData.m_EffectAmount, 255);
						nativeParallelHashMap.Add(crimeData.m_Source, item);
					}
				}
			}
			if (nativeParallelHashMap.Count() <= 0)
			{
				return;
			}
			NativeArray<Entity> keyArray = nativeParallelHashMap.GetKeyArray(Allocator.Temp);
			for (int j = 0; j < keyArray.Length; j++)
			{
				Entity entity = keyArray[j];
				CrimeVictim value = nativeParallelHashMap[entity];
				if (m_CrimeVictimData.HasComponent(entity) && !m_CrimeVictimData.IsComponentEnabled(entity))
				{
					m_CrimeVictimData.SetComponentEnabled(entity, value: true);
				}
				m_CrimeVictimData[entity] = value;
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TravelPurpose> __Game_Citizens_TravelPurpose_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentTypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		public ComponentTypeHandle<Criminal> __Game_Citizens_Criminal_RW_ComponentTypeHandle;

		public BufferTypeHandle<TripNeeded> __Game_Citizens_TripNeeded_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<AccidentSite> __Game_Events_AccidentSite_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentTransport> __Game_Citizens_CurrentTransport_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Creatures.Resident> __Game_Creatures_Resident_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Dispatched> __Game_Simulation_Dispatched_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.PoliceStation> __Game_Buildings_PoliceStation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Prison> __Game_Buildings_Prison_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PublicTransport> __Game_Vehicles_PublicTransport_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PoliceCar> __Game_Vehicles_PoliceCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Prefabs.CrimeData> __Game_Prefabs_CrimeData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ServiceDispatch> __Game_Simulation_ServiceDispatch_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Employee> __Game_Companies_Employee_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Resources> __Game_Economy_Resources_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		public BufferLookup<Occupant> __Game_Buildings_Occupant_RW_BufferLookup;

		public ComponentLookup<CrimeVictim> __Game_Citizens_CrimeVictim_RW_ComponentLookup;

		public BufferLookup<Resources> __Game_Economy_Resources_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Citizens_TravelPurpose_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TravelPurpose>(isReadOnly: true);
			__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentBuilding>(isReadOnly: true);
			__Game_Citizens_HealthProblem_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HealthProblem>(isReadOnly: true);
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Citizens_Criminal_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Criminal>();
			__Game_Citizens_TripNeeded_RW_BufferTypeHandle = state.GetBufferTypeHandle<TripNeeded>();
			__Game_Events_AccidentSite_RO_ComponentLookup = state.GetComponentLookup<AccidentSite>(isReadOnly: true);
			__Game_Citizens_HouseholdMember_RO_ComponentLookup = state.GetComponentLookup<HouseholdMember>(isReadOnly: true);
			__Game_Citizens_CurrentTransport_RO_ComponentLookup = state.GetComponentLookup<CurrentTransport>(isReadOnly: true);
			__Game_Creatures_Resident_RO_ComponentLookup = state.GetComponentLookup<Game.Creatures.Resident>(isReadOnly: true);
			__Game_Simulation_Dispatched_RO_ComponentLookup = state.GetComponentLookup<Dispatched>(isReadOnly: true);
			__Game_Buildings_PoliceStation_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.PoliceStation>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Buildings_Prison_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.Prison>(isReadOnly: true);
			__Game_Vehicles_PublicTransport_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PublicTransport>(isReadOnly: true);
			__Game_Vehicles_PoliceCar_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PoliceCar>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_CrimeData_RO_ComponentLookup = state.GetComponentLookup<Game.Prefabs.CrimeData>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Simulation_ServiceDispatch_RO_BufferLookup = state.GetBufferLookup<ServiceDispatch>(isReadOnly: true);
			__Game_Companies_Employee_RO_BufferLookup = state.GetBufferLookup<Employee>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(isReadOnly: true);
			__Game_Economy_Resources_RO_BufferLookup = state.GetBufferLookup<Resources>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
			__Game_Buildings_Occupant_RW_BufferLookup = state.GetBufferLookup<Occupant>();
			__Game_Citizens_CrimeVictim_RW_ComponentLookup = state.GetComponentLookup<CrimeVictim>();
			__Game_Economy_Resources_RW_BufferLookup = state.GetBufferLookup<Resources>();
		}
	}

	public const uint SYSTEM_UPDATE_INTERVAL = 16u;

	private EndFrameBarrier m_EndFrameBarrier;

	private SimulationSystem m_SimulationSystem;

	private CityStatisticsSystem m_CityStatisticsSystem;

	private CitySystem m_CitySystem;

	private EntityQuery m_CriminalQuery;

	private EntityQuery m_PoliceConfigQuery;

	private EntityArchetype m_AddAccidentSiteArchetype;

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
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_CriminalQuery = GetEntityQuery(ComponentType.ReadOnly<Citizen>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadWrite<Criminal>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_PoliceConfigQuery = GetEntityQuery(ComponentType.ReadOnly<PoliceConfigurationData>());
		m_AddAccidentSiteArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<AddAccidentSite>());
		RequireForUpdate(m_CriminalQuery);
		RequireForUpdate(m_PoliceConfigQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrameIndex = (m_SimulationSystem.frameIndex / 16) & 0xF;
		NativeQueue<CrimeData> crimeQueue = new NativeQueue<CrimeData>(Allocator.TempJob);
		JobHandle deps;
		CriminalJob jobData = new CriminalJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TravelPurposeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentBuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HealthProblemType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_CriminalType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Criminal_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TripNeededType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Citizens_TripNeeded_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_AccidentSiteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_AccidentSite_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdMemberData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentTransports = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CurrentTransport_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Residents = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Resident_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DispatchedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_Dispatched_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PoliceStationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PoliceStation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrisonData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Prison_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PublicTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PublicTransport_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PoliceCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PoliceCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCrimeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CrimeData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
			m_ServiceDispatches = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RO_BufferLookup, ref base.CheckedStateRef),
			m_Employees = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_Employee_RO_BufferLookup, ref base.CheckedStateRef),
			m_Renters = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
			m_Resources = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RO_BufferLookup, ref base.CheckedStateRef),
			m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
			m_Occupants = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Occupant_RW_BufferLookup, ref base.CheckedStateRef),
			m_TriggerBuffer = m_TriggerSystem.CreateActionBuffer().AsParallelWriter(),
			m_StatisticsEventQueue = m_CityStatisticsSystem.GetStatisticsEventQueue(out deps).AsParallelWriter(),
			m_UpdateFrameIndex = updateFrameIndex,
			m_RandomSeed = RandomSeed.Next(),
			m_PoliceConfigurationData = m_PoliceConfigQuery.GetSingleton<PoliceConfigurationData>(),
			m_AddAccidentSiteArchetype = m_AddAccidentSiteArchetype,
			m_City = m_CitySystem.City,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_CrimeQueue = crimeQueue.AsParallelWriter()
		};
		CrimeJob jobData2 = new CrimeJob
		{
			m_CrimeVictimData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CrimeVictim_RW_ComponentLookup, ref base.CheckedStateRef),
			m_Resources = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RW_BufferLookup, ref base.CheckedStateRef),
			m_CrimeQueue = crimeQueue,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer()
		};
		JobHandle jobHandle = JobChunkExtensions.Schedule(jobData, m_CriminalQuery, JobHandle.CombineDependencies(base.Dependency, deps));
		JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, jobHandle);
		crimeQueue.Dispose(jobHandle2);
		m_TriggerSystem.AddActionBufferWriter(jobHandle2);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);
		m_CityStatisticsSystem.AddWriter(jobHandle);
		base.Dependency = jobHandle2;
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
	public CriminalSystem()
	{
	}
}

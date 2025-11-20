using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Events;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class LeisureSystem : GameSystemBase
{
	[BurstCompile]
	private struct SpendLeisureJob : IJob
	{
		public NativeQueue<LeisureEvent> m_LeisureQueue;

		public ComponentLookup<ServiceAvailable> m_ServiceAvailables;

		public ComponentLookup<CompanyStatisticData> m_CompanyStatisticDatas;

		public BufferLookup<Game.Economy.Resources> m_Resources;

		public ComponentLookup<Citizen> m_CitizenDatas;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_IndustrialProcesses;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> m_HouseholdMembers;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public ComponentLookup<ServiceCompanyData> m_ServiceCompanyDatas;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		public NativeArray<int> m_CitizensConsumptionAccumulator;

		public void Execute()
		{
			LeisureEvent item;
			while (m_LeisureQueue.TryDequeue(out item))
			{
				if (m_CitizenDatas.HasComponent(item.m_Citizen))
				{
					Citizen value = m_CitizenDatas[item.m_Citizen];
					int num = (int)math.ceil((float)item.m_Efficiency / kUpdateInterval);
					value.m_LeisureCounter = (byte)math.min(255, value.m_LeisureCounter + num);
					m_CitizenDatas[item.m_Citizen] = value;
				}
				if (!m_HouseholdMembers.HasComponent(item.m_Citizen) || !m_Prefabs.HasComponent(item.m_Provider))
				{
					continue;
				}
				Entity household = m_HouseholdMembers[item.m_Citizen].m_Household;
				Entity prefab = m_Prefabs[item.m_Provider].m_Prefab;
				if (!m_IndustrialProcesses.HasComponent(prefab))
				{
					continue;
				}
				Resource resource = m_IndustrialProcesses[prefab].m_Output.m_Resource;
				if (resource == Resource.NoResource || !m_Resources.HasBuffer(item.m_Provider) || !m_Resources.HasBuffer(household))
				{
					continue;
				}
				bool flag = false;
				float marketPrice = EconomyUtils.GetMarketPrice(resource, m_ResourcePrefabs, ref m_ResourceDatas);
				int num2 = 0;
				float num3 = 1f;
				if (m_ServiceAvailables.HasComponent(item.m_Provider) && m_ServiceCompanyDatas.HasComponent(prefab))
				{
					ServiceAvailable value2 = m_ServiceAvailables[item.m_Provider];
					ServiceCompanyData serviceCompanyData = m_ServiceCompanyDatas[prefab];
					num2 = math.max((int)((float)serviceCompanyData.m_ServiceConsuming / kUpdateInterval), 1);
					if (value2.m_ServiceAvailable > 0)
					{
						value2.m_ServiceAvailable -= num2;
						value2.m_MeanPriority = math.lerp(value2.m_MeanPriority, (float)value2.m_ServiceAvailable / (float)serviceCompanyData.m_MaxService, 0.1f);
						m_ServiceAvailables[item.m_Provider] = value2;
						num3 = EconomyUtils.GetServicePriceMultiplier(value2.m_ServiceAvailable, serviceCompanyData.m_MaxService);
						if (m_CompanyStatisticDatas.HasComponent(item.m_Provider))
						{
							CompanyStatisticData value3 = m_CompanyStatisticDatas[item.m_Provider];
							value3.m_CurrentNumberOfCustomers++;
							m_CompanyStatisticDatas[item.m_Provider] = value3;
						}
					}
					else
					{
						flag = true;
					}
				}
				if (!flag)
				{
					DynamicBuffer<Game.Economy.Resources> resources = m_Resources[item.m_Provider];
					num2 = math.min(EconomyUtils.GetResources(resource, resources), num2);
					int num4 = (int)((float)num2 * marketPrice * num3);
					DynamicBuffer<Game.Economy.Resources> resources2 = m_Resources[household];
					EconomyUtils.AddResources(resource, -num2, resources);
					EconomyUtils.AddResources(Resource.Money, Mathf.RoundToInt(num4), resources);
					EconomyUtils.AddResources(Resource.Money, -Mathf.RoundToInt(num4), resources2);
					m_CitizensConsumptionAccumulator[EconomyUtils.GetResourceIndex(resource)] += num2;
				}
			}
		}
	}

	[BurstCompile]
	private struct LeisureJob : IJobChunk
	{
		public ComponentTypeHandle<Leisure> m_LeisureType;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<HouseholdMember> m_HouseholdMemberType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		public BufferTypeHandle<TripNeeded> m_TripType;

		[ReadOnly]
		public ComponentTypeHandle<CreatureData> m_CreatureDataType;

		[ReadOnly]
		public ComponentTypeHandle<ResidentData> m_ResidentDataType;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInfos;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		[ReadOnly]
		public ComponentLookup<Target> m_Targets;

		[ReadOnly]
		public ComponentLookup<CarKeeper> m_CarKeepers;

		[ReadOnly]
		public ComponentLookup<BicycleOwner> m_BicycleOwners;

		[ReadOnly]
		public ComponentLookup<ParkedCar> m_ParkedCarData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PersonalCar> m_PersonalCarData;

		[ReadOnly]
		public ComponentLookup<CurrentBuilding> m_CurrentBuildings;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public ComponentLookup<LeisureProviderData> m_LeisureProviderDatas;

		[ReadOnly]
		public ComponentLookup<Worker> m_Workers;

		[ReadOnly]
		public ComponentLookup<Game.Citizens.Student> m_Students;

		[ReadOnly]
		public BufferLookup<Game.Economy.Resources> m_Resources;

		[ReadOnly]
		public ComponentLookup<Household> m_Households;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_Renters;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Citizen> m_CitizenDatas;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		[ReadOnly]
		public ComponentLookup<CarData> m_PrefabCarData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<HumanData> m_PrefabHumanData;

		[ReadOnly]
		public ComponentLookup<TravelPurpose> m_Purposes;

		[ReadOnly]
		public ComponentLookup<OutsideConnectionData> m_OutsideConnectionDatas;

		[ReadOnly]
		public ComponentLookup<TouristHousehold> m_TouristHouseholds;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_IndustrialProcesses;

		[ReadOnly]
		public ComponentLookup<ServiceAvailable> m_ServiceAvailables;

		[ReadOnly]
		public ComponentLookup<Population> m_PopulationData;

		[ReadOnly]
		public BufferLookup<Renter> m_RenterBufs;

		[ReadOnly]
		public ComponentLookup<ConsumptionData> m_ConsumptionDatas;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public ComponentTypeSet m_PathfindTypes;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_HumanChunks;

		[ReadOnly]
		public PersonalCarSelectData m_PersonalCarSelectData;

		public EconomyParameterData m_EconomyParameters;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;

		public NativeQueue<LeisureEvent>.ParallelWriter m_LeisureQueue;

		public NativeQueue<AddMeetingSystem.AddMeeting>.ParallelWriter m_MeetingQueue;

		public uint m_SimulationFrame;

		public uint m_UpdateFrameIndex;

		public float m_TimeOfDay;

		public float m_Weather;

		public float m_Temperature;

		public Entity m_PopulationEntity;

		public TimeData m_TimeData;

		private void SpendLeisure(int index, Entity entity, ref Citizen citizen, ref Leisure leisure, Entity providerEntity, LeisureProviderData provider)
		{
			bool flag = m_BuildingData.HasComponent(providerEntity) && BuildingUtils.CheckOption(m_BuildingData[providerEntity], BuildingOption.Inactive);
			if (m_ServiceAvailables.HasComponent(providerEntity) && m_ServiceAvailables[providerEntity].m_ServiceAvailable <= 0)
			{
				flag = true;
			}
			Entity prefab = m_PrefabRefs[providerEntity].m_Prefab;
			if (!flag && m_IndustrialProcesses.HasComponent(prefab))
			{
				Resource resource = m_IndustrialProcesses[prefab].m_Output.m_Resource;
				if (resource != Resource.NoResource && m_Resources.HasBuffer(providerEntity) && EconomyUtils.GetResources(resource, m_Resources[providerEntity]) <= 0)
				{
					flag = true;
				}
			}
			if (!flag)
			{
				m_LeisureQueue.Enqueue(new LeisureEvent
				{
					m_Citizen = entity,
					m_Provider = providerEntity,
					m_Efficiency = provider.m_Efficiency
				});
			}
			if ((float)(int)citizen.m_LeisureCounter > 255f - (float)provider.m_Efficiency / kUpdateInterval || m_SimulationFrame >= leisure.m_LastPossibleFrame || flag)
			{
				m_CommandBuffer.RemoveComponent<Leisure>(index, entity);
			}
		}

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Leisure> nativeArray2 = chunk.GetNativeArray(ref m_LeisureType);
			NativeArray<HouseholdMember> nativeArray3 = chunk.GetNativeArray(ref m_HouseholdMemberType);
			BufferAccessor<TripNeeded> bufferAccessor = chunk.GetBufferAccessor(ref m_TripType);
			int population = m_PopulationData[m_PopulationEntity].m_Population;
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Leisure leisure = nativeArray2[i];
				DynamicBuffer<TripNeeded> dynamicBuffer = bufferAccessor[i];
				Citizen citizen = m_CitizenDatas[entity];
				bool flag = m_Purposes.HasComponent(entity) && m_Purposes[entity].m_Purpose == Purpose.Traveling;
				Entity providerEntity = leisure.m_TargetAgent;
				Entity entity2 = Entity.Null;
				LeisureProviderData provider = default(LeisureProviderData);
				if (leisure.m_TargetAgent != Entity.Null && m_CurrentBuildings.HasComponent(entity))
				{
					Entity currentBuilding = m_CurrentBuildings[entity].m_CurrentBuilding;
					if (m_PropertyRenters.HasComponent(leisure.m_TargetAgent) && m_PropertyRenters[leisure.m_TargetAgent].m_Property == currentBuilding && m_PrefabRefs.HasComponent(leisure.m_TargetAgent))
					{
						Entity prefab = m_PrefabRefs[leisure.m_TargetAgent].m_Prefab;
						if (m_LeisureProviderDatas.HasComponent(prefab))
						{
							entity2 = prefab;
							provider = m_LeisureProviderDatas[entity2];
						}
					}
					else if (m_PrefabRefs.HasComponent(currentBuilding))
					{
						Entity prefab2 = m_PrefabRefs[currentBuilding].m_Prefab;
						providerEntity = currentBuilding;
						if (m_LeisureProviderDatas.HasComponent(prefab2))
						{
							entity2 = prefab2;
							provider = m_LeisureProviderDatas[entity2];
						}
						else if (flag && m_OutsideConnectionDatas.HasComponent(prefab2))
						{
							entity2 = prefab2;
							provider = new LeisureProviderData
							{
								m_Efficiency = 20,
								m_LeisureType = LeisureType.Travel,
								m_Resources = Resource.NoResource
							};
						}
					}
				}
				if (entity2 != Entity.Null)
				{
					SpendLeisure(unfilteredChunkIndex, entity, ref citizen, ref leisure, providerEntity, provider);
					nativeArray2[i] = leisure;
					m_CitizenDatas[entity] = citizen;
				}
				else if (!flag && m_PathInfos.HasComponent(entity))
				{
					PathInformation pathInformation = m_PathInfos[entity];
					if ((pathInformation.m_State & PathFlags.Pending) != 0)
					{
						continue;
					}
					Entity destination = pathInformation.m_Destination;
					if ((m_PropertyRenters.HasComponent(destination) || m_PrefabRefs.HasComponent(destination)) && !m_Targets.HasComponent(entity))
					{
						if ((!m_Workers.HasComponent(entity) || WorkerSystem.IsTodayOffDay(citizen, ref m_EconomyParameters, m_SimulationFrame, m_TimeData, population) || !WorkerSystem.IsTimeToWork(citizen, m_Workers[entity], ref m_EconomyParameters, m_TimeOfDay)) && (!m_Students.HasComponent(entity) || StudentSystem.IsTimeToStudy(citizen, m_Students[entity], ref m_EconomyParameters, m_TimeOfDay, m_SimulationFrame, m_TimeData, population)))
						{
							Entity prefab3 = m_PrefabRefs[destination].m_Prefab;
							if (m_LeisureProviderDatas[prefab3].m_Efficiency == 0)
							{
								UnityEngine.Debug.LogWarning($"Warning: Leisure provider {destination.Index} has zero efficiency");
							}
							leisure.m_TargetAgent = destination;
							nativeArray2[i] = leisure;
							dynamicBuffer.Add(new TripNeeded
							{
								m_TargetAgent = destination,
								m_Purpose = Purpose.Leisure
							});
							m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, new Target
							{
								m_Target = destination
							});
						}
						else
						{
							if (m_Purposes.HasComponent(entity) && (m_Purposes[entity].m_Purpose == Purpose.Leisure || m_Purposes[entity].m_Purpose == Purpose.Traveling))
							{
								m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
							}
							m_CommandBuffer.RemoveComponent<Leisure>(unfilteredChunkIndex, entity);
							m_CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity, in m_PathfindTypes);
						}
					}
					else if (!m_Targets.HasComponent(entity))
					{
						if (m_Purposes.HasComponent(entity) && (m_Purposes[entity].m_Purpose == Purpose.Leisure || m_Purposes[entity].m_Purpose == Purpose.Traveling))
						{
							m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
						}
						m_CommandBuffer.RemoveComponent<Leisure>(unfilteredChunkIndex, entity);
						m_CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity, in m_PathfindTypes);
					}
				}
				else if (!m_Purposes.HasComponent(entity))
				{
					Entity household = nativeArray3[i].m_Household;
					FindLeisure(unfilteredChunkIndex, entity, household, citizen, ref random, m_TouristHouseholds.HasComponent(household));
					nativeArray2[i] = leisure;
				}
			}
		}

		private float GetWeight(LeisureType type, int wealth, CitizenAge age)
		{
			float num = 1f;
			float num2;
			float xMin;
			float num3;
			switch (type)
			{
			case LeisureType.Meals:
				num2 = 10f;
				xMin = 0.2f;
				num3 = age switch
				{
					CitizenAge.Child => 10f, 
					CitizenAge.Teen => 25f, 
					CitizenAge.Elderly => 35f, 
					_ => 35f, 
				};
				break;
			case LeisureType.Entertainment:
				num2 = 10f;
				xMin = 0.3f;
				num3 = age switch
				{
					CitizenAge.Child => 0f, 
					CitizenAge.Teen => 45f, 
					CitizenAge.Elderly => 10f, 
					_ => 45f, 
				};
				break;
			case LeisureType.Commercial:
				num2 = 10f;
				xMin = 0.4f;
				num3 = age switch
				{
					CitizenAge.Child => 20f, 
					CitizenAge.Teen => 25f, 
					CitizenAge.Elderly => 25f, 
					_ => 30f, 
				};
				break;
			case LeisureType.CityIndoors:
			case LeisureType.CityPark:
			case LeisureType.CityBeach:
				num2 = 10f;
				xMin = 0f;
				num3 = age switch
				{
					CitizenAge.Child => 30f, 
					CitizenAge.Teen => 25f, 
					CitizenAge.Elderly => 15f, 
					_ => 30f, 
				};
				num = type switch
				{
					LeisureType.CityIndoors => 1f, 
					LeisureType.CityPark => 2f * (1f - 0.95f * m_Weather), 
					_ => 0.05f + 4f * math.saturate(0.35f - m_Weather) * math.saturate((m_Temperature - 20f) / 30f), 
				};
				break;
			case LeisureType.Travel:
				num2 = 1f;
				xMin = 0.5f;
				num = 0.5f + math.saturate((30f - m_Temperature) / 50f);
				num3 = age switch
				{
					CitizenAge.Child => 15f, 
					CitizenAge.Teen => 15f, 
					CitizenAge.Elderly => 30f, 
					_ => 40f, 
				};
				break;
			default:
				num2 = 0f;
				xMin = 0f;
				num3 = 0f;
				num = 0f;
				break;
			}
			return num3 * num * num2 * math.smoothstep(xMin, 1f, ((float)wealth + 5000f) / 10000f);
		}

		private LeisureType SelectLeisureType(Entity household, bool tourist, Citizen citizenData, ref Unity.Mathematics.Random random)
		{
			PropertyRenter propertyRenter = (m_Renters.HasComponent(household) ? m_Renters[household] : default(PropertyRenter));
			if (tourist && random.NextFloat() < 0.3f)
			{
				return LeisureType.Attractions;
			}
			if (m_Households.HasComponent(household) && m_Resources.HasBuffer(household) && m_HouseholdCitizens.HasBuffer(household))
			{
				int wealth = ((!tourist) ? EconomyUtils.GetHouseholdSpendableMoney(m_Households[household], m_Resources[household], ref m_RenterBufs, ref m_ConsumptionDatas, ref m_PrefabRefs, propertyRenter) : EconomyUtils.GetResources(Resource.Money, m_Resources[household]));
				float num = 0f;
				CitizenAge age = citizenData.GetAge();
				for (int i = 0; i < 10; i++)
				{
					num += GetWeight((LeisureType)i, wealth, age);
				}
				float num2 = num * random.NextFloat();
				for (int j = 0; j < 10; j++)
				{
					num2 -= GetWeight((LeisureType)j, wealth, age);
					if (num2 <= 0.001f)
					{
						return (LeisureType)j;
					}
				}
			}
			UnityEngine.Debug.LogWarning("Leisure type randomization failed");
			return LeisureType.Count;
		}

		private void FindLeisure(int chunkIndex, Entity citizen, Entity household, Citizen citizenData, ref Unity.Mathematics.Random random, bool tourist)
		{
			LeisureType leisureType = SelectLeisureType(household, tourist, citizenData, ref random);
			float value = 255f - (float)(int)citizenData.m_LeisureCounter;
			if (leisureType == LeisureType.Travel || leisureType == LeisureType.Sightseeing || leisureType == LeisureType.Attractions)
			{
				if (m_Purposes.HasComponent(citizen))
				{
					m_CommandBuffer.RemoveComponent<TravelPurpose>(chunkIndex, citizen);
				}
				m_MeetingQueue.Enqueue(new AddMeetingSystem.AddMeeting
				{
					m_Household = household,
					m_Type = leisureType
				});
				return;
			}
			m_CommandBuffer.AddComponent(chunkIndex, citizen, in m_PathfindTypes);
			m_CommandBuffer.SetComponent(chunkIndex, citizen, new PathInformation
			{
				m_State = PathFlags.Pending
			});
			CreatureData creatureData;
			PseudoRandomSeed randomSeed;
			Entity entity = ObjectEmergeSystem.SelectResidentPrefab(citizenData, m_HumanChunks, m_EntityType, ref m_CreatureDataType, ref m_ResidentDataType, out creatureData, out randomSeed);
			HumanData humanData = default(HumanData);
			if (entity != Entity.Null)
			{
				humanData = m_PrefabHumanData[entity];
			}
			Household household2 = m_Households[household];
			DynamicBuffer<HouseholdCitizen> dynamicBuffer = m_HouseholdCitizens[household];
			PathfindParameters parameters = new PathfindParameters
			{
				m_MaxSpeed = 277.77777f,
				m_WalkSpeed = humanData.m_WalkSpeed,
				m_Weights = CitizenUtils.GetPathfindWeights(citizenData, household2, dynamicBuffer.Length),
				m_Methods = (PathMethod.Pedestrian | PathMethod.Taxi | RouteUtils.GetPublicTransportMethods(m_TimeOfDay)),
				m_TaxiIgnoredRules = VehicleUtils.GetIgnoredPathfindRulesTaxiDefaults(),
				m_MaxCost = CitizenBehaviorSystem.kMaxPathfindCost
			};
			SetupQueueTarget origin = new SetupQueueTarget
			{
				m_Type = SetupTargetType.CurrentLocation,
				m_Methods = PathMethod.Pedestrian,
				m_RandomCost = 30f
			};
			SetupQueueTarget destination = new SetupQueueTarget
			{
				m_Type = SetupTargetType.Leisure,
				m_Methods = PathMethod.Pedestrian,
				m_Value = (int)leisureType,
				m_Value2 = value,
				m_RandomCost = 30f,
				m_ActivityMask = creatureData.m_SupportedActivities
			};
			if (m_PropertyRenters.TryGetComponent(household, out var componentData))
			{
				parameters.m_Authorization1 = componentData.m_Property;
			}
			if (m_Workers.HasComponent(citizen))
			{
				Worker worker = m_Workers[citizen];
				if (m_PropertyRenters.HasComponent(worker.m_Workplace))
				{
					parameters.m_Authorization2 = m_PropertyRenters[worker.m_Workplace].m_Property;
				}
				else
				{
					parameters.m_Authorization2 = worker.m_Workplace;
				}
			}
			if (m_CarKeepers.IsComponentEnabled(citizen))
			{
				Entity car = m_CarKeepers[citizen].m_Car;
				if (m_ParkedCarData.HasComponent(car))
				{
					PrefabRef prefabRef = m_PrefabRefs[car];
					ParkedCar parkedCar = m_ParkedCarData[car];
					CarData carData = m_PrefabCarData[prefabRef.m_Prefab];
					parameters.m_MaxSpeed.x = carData.m_MaxSpeed;
					parameters.m_ParkingTarget = parkedCar.m_Lane;
					parameters.m_ParkingDelta = parkedCar.m_CurvePosition;
					parameters.m_ParkingSize = VehicleUtils.GetParkingSize(car, ref m_PrefabRefs, ref m_ObjectGeometryData);
					parameters.m_Methods |= VehicleUtils.GetPathMethods(carData) | PathMethod.Parking;
					parameters.m_IgnoredRules = VehicleUtils.GetIgnoredPathfindRules(carData);
					if (m_PersonalCarData.TryGetComponent(car, out var componentData2) && (componentData2.m_State & PersonalCarFlags.HomeTarget) == 0)
					{
						parameters.m_PathfindFlags |= PathfindFlags.ParkingReset;
					}
				}
			}
			else if (m_BicycleOwners.IsComponentEnabled(citizen))
			{
				Entity bicycle = m_BicycleOwners[citizen].m_Bicycle;
				if (!m_PrefabRefs.TryGetComponent(bicycle, out var componentData3) && m_CurrentBuildings.TryGetComponent(citizen, out var componentData4) && componentData4.m_CurrentBuilding == componentData.m_Property)
				{
					Unity.Mathematics.Random random2 = citizenData.GetPseudoRandom(CitizenPseudoRandom.BicycleModel);
					componentData3.m_Prefab = m_PersonalCarSelectData.SelectVehiclePrefab(ref random2, 1, 0, avoidTrailers: true, noSlowVehicles: false, bicycle: true, out var _);
				}
				if (m_PrefabCarData.TryGetComponent(componentData3.m_Prefab, out var componentData5) && m_ObjectGeometryData.TryGetComponent(componentData3.m_Prefab, out var componentData6))
				{
					parameters.m_MaxSpeed.x = componentData5.m_MaxSpeed;
					parameters.m_ParkingSize = VehicleUtils.GetParkingSize(componentData6, out var _);
					parameters.m_Methods |= PathMethod.Bicycle | PathMethod.BicycleParking;
					parameters.m_IgnoredRules = VehicleUtils.GetIgnoredPathfindRulesBicycleDefaults();
					if (m_ParkedCarData.TryGetComponent(bicycle, out var componentData7))
					{
						parameters.m_ParkingTarget = componentData7.m_Lane;
						parameters.m_ParkingDelta = componentData7.m_CurvePosition;
						if (m_PersonalCarData.TryGetComponent(bicycle, out var componentData8) && (componentData8.m_State & PersonalCarFlags.HomeTarget) == 0)
						{
							parameters.m_PathfindFlags |= PathfindFlags.ParkingReset;
						}
					}
					else
					{
						origin.m_Methods |= PathMethod.Bicycle;
						origin.m_RoadTypes |= RoadTypes.Bicycle;
					}
				}
			}
			SetupQueueItem value2 = new SetupQueueItem(citizen, parameters, origin, destination);
			m_PathfindQueue.Enqueue(value2);
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

		public ComponentTypeHandle<Leisure> __Game_Citizens_Leisure_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentTypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		public BufferTypeHandle<TripNeeded> __Game_Citizens_TripNeeded_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CreatureData> __Game_Prefabs_CreatureData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ResidentData> __Game_Prefabs_ResidentData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarKeeper> __Game_Citizens_CarKeeper_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BicycleOwner> __Game_Citizens_BicycleOwner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkedCar> __Game_Vehicles_ParkedCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PersonalCar> __Game_Vehicles_PersonalCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Target> __Game_Common_Target_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LeisureProviderData> __Game_Prefabs_LeisureProviderData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Citizens.Student> __Game_Citizens_Student_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Worker> __Game_Citizens_Worker_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RO_BufferLookup;

		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RW_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarData> __Game_Prefabs_CarData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HumanData> __Game_Prefabs_HumanData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TravelPurpose> __Game_Citizens_TravelPurpose_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<OutsideConnectionData> __Game_Prefabs_OutsideConnectionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TouristHousehold> __Game_Citizens_TouristHousehold_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceAvailable> __Game_Companies_ServiceAvailable_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Population> __Game_City_Population_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<ConsumptionData> __Game_Prefabs_ConsumptionData_RO_ComponentLookup;

		public ComponentLookup<ServiceAvailable> __Game_Companies_ServiceAvailable_RW_ComponentLookup;

		public ComponentLookup<CompanyStatisticData> __Game_Companies_CompanyStatisticData_RW_ComponentLookup;

		public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RW_BufferLookup;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceCompanyData> __Game_Companies_ServiceCompanyData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Citizens_Leisure_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Leisure>();
			__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HouseholdMember>(isReadOnly: true);
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Citizens_TripNeeded_RW_BufferTypeHandle = state.GetBufferTypeHandle<TripNeeded>();
			__Game_Prefabs_CreatureData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CreatureData>(isReadOnly: true);
			__Game_Prefabs_ResidentData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ResidentData>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(isReadOnly: true);
			__Game_Citizens_CurrentBuilding_RO_ComponentLookup = state.GetComponentLookup<CurrentBuilding>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Citizens_CarKeeper_RO_ComponentLookup = state.GetComponentLookup<CarKeeper>(isReadOnly: true);
			__Game_Citizens_BicycleOwner_RO_ComponentLookup = state.GetComponentLookup<BicycleOwner>(isReadOnly: true);
			__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(isReadOnly: true);
			__Game_Vehicles_PersonalCar_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PersonalCar>(isReadOnly: true);
			__Game_Common_Target_RO_ComponentLookup = state.GetComponentLookup<Target>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_LeisureProviderData_RO_ComponentLookup = state.GetComponentLookup<LeisureProviderData>(isReadOnly: true);
			__Game_Citizens_Student_RO_ComponentLookup = state.GetComponentLookup<Game.Citizens.Student>(isReadOnly: true);
			__Game_Citizens_Worker_RO_ComponentLookup = state.GetComponentLookup<Worker>(isReadOnly: true);
			__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(isReadOnly: true);
			__Game_Economy_Resources_RO_BufferLookup = state.GetBufferLookup<Game.Economy.Resources>(isReadOnly: true);
			__Game_Citizens_Citizen_RW_ComponentLookup = state.GetComponentLookup<Citizen>();
			__Game_Prefabs_CarData_RO_ComponentLookup = state.GetComponentLookup<CarData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_HumanData_RO_ComponentLookup = state.GetComponentLookup<HumanData>(isReadOnly: true);
			__Game_Citizens_TravelPurpose_RO_ComponentLookup = state.GetComponentLookup<TravelPurpose>(isReadOnly: true);
			__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup = state.GetComponentLookup<OutsideConnectionData>(isReadOnly: true);
			__Game_Citizens_TouristHousehold_RO_ComponentLookup = state.GetComponentLookup<TouristHousehold>(isReadOnly: true);
			__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
			__Game_Companies_ServiceAvailable_RO_ComponentLookup = state.GetComponentLookup<ServiceAvailable>(isReadOnly: true);
			__Game_City_Population_RO_ComponentLookup = state.GetComponentLookup<Population>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(isReadOnly: true);
			__Game_Prefabs_ConsumptionData_RO_ComponentLookup = state.GetComponentLookup<ConsumptionData>(isReadOnly: true);
			__Game_Companies_ServiceAvailable_RW_ComponentLookup = state.GetComponentLookup<ServiceAvailable>();
			__Game_Companies_CompanyStatisticData_RW_ComponentLookup = state.GetComponentLookup<CompanyStatisticData>();
			__Game_Economy_Resources_RW_BufferLookup = state.GetBufferLookup<Game.Economy.Resources>();
			__Game_Citizens_HouseholdMember_RO_ComponentLookup = state.GetComponentLookup<HouseholdMember>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
			__Game_Companies_ServiceCompanyData_RO_ComponentLookup = state.GetComponentLookup<ServiceCompanyData>(isReadOnly: true);
		}
	}

	public static readonly int kUpdatePerDay = 4096;

	public static readonly float kUpdateInterval = 5f;

	private SimulationSystem m_SimulationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private PathfindSetupSystem m_PathFindSetupSystem;

	private TimeSystem m_TimeSystem;

	private ResourceSystem m_ResourceSystem;

	private ClimateSystem m_ClimateSystem;

	private AddMeetingSystem m_AddMeetingSystem;

	private CityProductionStatisticSystem m_CityProductionStatisticSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private EntityQuery m_LeisureQuery;

	private EntityQuery m_EconomyParameterQuery;

	private EntityQuery m_LeisureParameterQuery;

	private EntityQuery m_ResidentPrefabQuery;

	private EntityQuery m_TimeDataQuery;

	private EntityQuery m_PopulationQuery;

	private EntityQuery m_CarPrefabQuery;

	private ComponentTypeSet m_PathfindTypes;

	private NativeQueue<LeisureEvent> m_LeisureQueue;

	private PersonalCarSelectData m_PersonalCarSelectData;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / kUpdatePerDay;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_PathFindSetupSystem = base.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
		m_TimeSystem = base.World.GetOrCreateSystemManaged<TimeSystem>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
		m_AddMeetingSystem = base.World.GetOrCreateSystemManaged<AddMeetingSystem>();
		m_CityProductionStatisticSystem = base.World.GetOrCreateSystemManaged<CityProductionStatisticSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_PersonalCarSelectData = new PersonalCarSelectData(this);
		m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		m_LeisureParameterQuery = GetEntityQuery(ComponentType.ReadOnly<LeisureParametersData>());
		m_LeisureQuery = GetEntityQuery(ComponentType.ReadWrite<Citizen>(), ComponentType.ReadWrite<Leisure>(), ComponentType.ReadWrite<TripNeeded>(), ComponentType.ReadWrite<CurrentBuilding>(), ComponentType.Exclude<HealthProblem>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_ResidentPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<ObjectData>(), ComponentType.ReadOnly<HumanData>(), ComponentType.ReadOnly<ResidentData>(), ComponentType.ReadOnly<PrefabData>());
		m_TimeDataQuery = GetEntityQuery(ComponentType.ReadOnly<TimeData>());
		m_PopulationQuery = GetEntityQuery(ComponentType.ReadOnly<Population>());
		m_CarPrefabQuery = GetEntityQuery(PersonalCarSelectData.GetEntityQueryDesc());
		m_PathfindTypes = new ComponentTypeSet(ComponentType.ReadWrite<PathInformation>(), ComponentType.ReadWrite<PathElement>());
		m_LeisureQueue = new NativeQueue<LeisureEvent>(Allocator.Persistent);
		RequireForUpdate(m_LeisureQuery);
		RequireForUpdate(m_EconomyParameterQuery);
		RequireForUpdate(m_LeisureParameterQuery);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_LeisureQueue.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrameWithInterval = SimulationUtils.GetUpdateFrameWithInterval(m_SimulationSystem.frameIndex, (uint)GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16);
		float value = m_ClimateSystem.precipitation.value;
		m_PersonalCarSelectData.PreUpdate(this, m_CityConfigurationSystem, m_CarPrefabQuery, Allocator.TempJob, out var jobHandle);
		JobHandle outJobHandle;
		JobHandle deps;
		JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(new LeisureJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_LeisureType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Leisure_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HouseholdMemberType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_TripType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Citizens_TripNeeded_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_CreatureDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_CreatureData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ResidentDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ResidentData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathInfos = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentBuildings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertyRenters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarKeepers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CarKeeper_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BicycleOwners = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_BicycleOwner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PersonalCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PersonalCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Targets = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Target_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LeisureProviderDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_LeisureProviderData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Students = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Student_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Workers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Households = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Resources = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RO_BufferLookup, ref base.CheckedStateRef),
			m_CitizenDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RW_ComponentLookup, ref base.CheckedStateRef),
			m_Renters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabHumanData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_HumanData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Purposes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OutsideConnectionDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TouristHouseholds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_TouristHousehold_RO_ComponentLookup, ref base.CheckedStateRef),
			m_IndustrialProcesses = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceAvailables = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_ServiceAvailable_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PopulationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_Population_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
			m_RenterBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
			m_ConsumptionDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ConsumptionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EconomyParameters = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_TimeOfDay = m_TimeSystem.normalizedTime,
			m_UpdateFrameIndex = updateFrameWithInterval,
			m_Weather = value,
			m_Temperature = m_ClimateSystem.temperature,
			m_RandomSeed = RandomSeed.Next(),
			m_PathfindTypes = m_PathfindTypes,
			m_HumanChunks = m_ResidentPrefabQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
			m_PersonalCarSelectData = m_PersonalCarSelectData,
			m_PathfindQueue = m_PathFindSetupSystem.GetQueue(this, 64).AsParallelWriter(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_MeetingQueue = m_AddMeetingSystem.GetMeetingQueue(out deps).AsParallelWriter(),
			m_LeisureQueue = m_LeisureQueue.AsParallelWriter(),
			m_TimeData = m_TimeDataQuery.GetSingleton<TimeData>(),
			m_PopulationEntity = m_PopulationQuery.GetSingletonEntity()
		}, m_LeisureQuery, JobUtils.CombineDependencies(base.Dependency, outJobHandle, deps, jobHandle));
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);
		m_PathFindSetupSystem.AddQueueWriter(jobHandle2);
		m_PersonalCarSelectData.PostUpdate(jobHandle2);
		JobHandle deps2;
		JobHandle jobHandle3 = IJobExtensions.Schedule(new SpendLeisureJob
		{
			m_ServiceAvailables = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_ServiceAvailable_RW_ComponentLookup, ref base.CheckedStateRef),
			m_CompanyStatisticDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_CompanyStatisticData_RW_ComponentLookup, ref base.CheckedStateRef),
			m_Resources = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RW_BufferLookup, ref base.CheckedStateRef),
			m_CitizenDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RW_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdMembers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup, ref base.CheckedStateRef),
			m_IndustrialProcesses = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceCompanyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_ServiceCompanyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
			m_CitizensConsumptionAccumulator = m_CityProductionStatisticSystem.GetCityResourceUsageAccumulator(CityProductionStatisticSystem.CityResourceUsage.Consumer.Citizens, out deps2),
			m_LeisureQueue = m_LeisureQueue
		}, JobHandle.CombineDependencies(jobHandle2, deps2));
		m_ResourceSystem.AddPrefabsReader(jobHandle3);
		m_CityProductionStatisticSystem.AddCityUsageAccumulatorWriter(CityProductionStatisticSystem.CityResourceUsage.Consumer.Citizens, jobHandle3);
		base.Dependency = jobHandle3;
	}

	public static void AddToTempList(NativeList<LeisureProviderData> tempProviderList, LeisureProviderData providerToAdd)
	{
		for (int i = 0; i < tempProviderList.Length; i++)
		{
			LeisureProviderData value = tempProviderList[i];
			if (value.m_LeisureType == providerToAdd.m_LeisureType && value.m_Resources == providerToAdd.m_Resources)
			{
				value.m_Efficiency += providerToAdd.m_Efficiency;
				tempProviderList[i] = value;
				return;
			}
		}
		tempProviderList.Add(in providerToAdd);
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
	public LeisureSystem()
	{
	}
}

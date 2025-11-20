using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Debug;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Tools;
using Game.Triggers;
using Game.Vehicles;
using Game.Zones;
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
public class HouseholdFindPropertySystem : GameSystemBase
{
	public struct CachedPropertyInformation
	{
		public GenericApartmentQuality quality;

		public int free;
	}

	public struct GenericApartmentQuality
	{
		public float apartmentSize;

		public float2 educationBonus;

		public float welfareBonus;

		public float score;

		public int level;
	}

	[BurstCompile]
	private struct PreparePropertyJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> m_BuildingProperties;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public BufferLookup<Renter> m_Renters;

		[ReadOnly]
		public ComponentLookup<Abandoned> m_Abandoneds;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Park> m_Parks;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_BuildingDatas;

		[ReadOnly]
		public ComponentLookup<ParkData> m_ParkDatas;

		[ReadOnly]
		public ComponentLookup<Household> m_Households;

		[ReadOnly]
		public ComponentLookup<Building> m_Buildings;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableDatas;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> m_BuildingPropertyData;

		[ReadOnly]
		public BufferLookup<Game.Net.ServiceCoverage> m_ServiceCoverages;

		[ReadOnly]
		public ComponentLookup<CrimeProducer> m_Crimes;

		[ReadOnly]
		public ComponentLookup<Transform> m_Transforms;

		[ReadOnly]
		public ComponentLookup<Locked> m_Locked;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> m_ElectricityConsumers;

		[ReadOnly]
		public ComponentLookup<WaterConsumer> m_WaterConsumers;

		[ReadOnly]
		public ComponentLookup<GarbageProducer> m_GarbageProducers;

		[ReadOnly]
		public ComponentLookup<MailProducer> m_MailProducers;

		[ReadOnly]
		public NativeArray<AirPollution> m_AirPollutionMap;

		[ReadOnly]
		public NativeArray<GroundPollution> m_PollutionMap;

		[ReadOnly]
		public NativeArray<NoisePollution> m_NoiseMap;

		[ReadOnly]
		public CellMapData<TelecomCoverage> m_TelecomCoverages;

		public HealthcareParameterData m_HealthcareParameters;

		public ParkParameterData m_ParkParameters;

		public EducationParameterData m_EducationParameters;

		public TelecomParameterData m_TelecomParameters;

		public GarbageParameterData m_GarbageParameters;

		public PoliceConfigurationData m_PoliceParameters;

		public CitizenHappinessParameterData m_CitizenHappinessParameterData;

		public Entity m_City;

		public NativeParallelHashMap<Entity, CachedPropertyInformation>.ParallelWriter m_PropertyData;

		private int CalculateFree(Entity property)
		{
			Entity prefab = m_Prefabs[property].m_Prefab;
			int num = 0;
			if (m_BuildingDatas.HasComponent(prefab) && (m_Abandoneds.HasComponent(property) || (m_Parks.HasComponent(property) && m_ParkDatas[prefab].m_AllowHomeless)))
			{
				num = BuildingUtils.GetShelterHomelessCapacity(prefab, ref m_BuildingDatas, ref m_BuildingPropertyData) - m_Renters[property].Length;
			}
			else if (m_BuildingProperties.HasComponent(prefab))
			{
				BuildingPropertyData buildingPropertyData = m_BuildingProperties[prefab];
				DynamicBuffer<Renter> dynamicBuffer = m_Renters[property];
				num = buildingPropertyData.CountProperties(AreaType.Residential);
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity renter = dynamicBuffer[i].m_Renter;
					if (m_Households.HasComponent(renter))
					{
						num--;
					}
				}
			}
			return num;
		}

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				int num = CalculateFree(entity);
				if (num > 0)
				{
					Entity prefab = m_Prefabs[entity].m_Prefab;
					Building buildingData = m_Buildings[entity];
					Entity healthcareServicePrefab = m_HealthcareParameters.m_HealthcareServicePrefab;
					Entity parkServicePrefab = m_ParkParameters.m_ParkServicePrefab;
					Entity educationServicePrefab = m_EducationParameters.m_EducationServicePrefab;
					Entity telecomServicePrefab = m_TelecomParameters.m_TelecomServicePrefab;
					Entity garbageServicePrefab = m_GarbageParameters.m_GarbageServicePrefab;
					Entity policeServicePrefab = m_PoliceParameters.m_PoliceServicePrefab;
					DynamicBuffer<CityModifier> cityModifiers = m_CityModifiers[m_City];
					GenericApartmentQuality genericApartmentQuality = PropertyUtils.GetGenericApartmentQuality(entity, prefab, ref buildingData, ref m_BuildingProperties, ref m_BuildingDatas, ref m_SpawnableDatas, ref m_Crimes, ref m_ServiceCoverages, ref m_Locked, ref m_ElectricityConsumers, ref m_WaterConsumers, ref m_GarbageProducers, ref m_MailProducers, ref m_Transforms, ref m_Abandoneds, m_PollutionMap, m_AirPollutionMap, m_NoiseMap, m_TelecomCoverages, cityModifiers, healthcareServicePrefab, parkServicePrefab, educationServicePrefab, telecomServicePrefab, garbageServicePrefab, policeServicePrefab, m_CitizenHappinessParameterData, m_GarbageParameters);
					m_PropertyData.TryAdd(entity, new CachedPropertyInformation
					{
						free = num,
						quality = genericApartmentQuality
					});
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FindPropertyJob : IJob
	{
		public NativeList<Entity> m_HomelessHouseholdEntities;

		public NativeList<Entity> m_MovedInHouseholdEntities;

		public NativeParallelHashMap<Entity, CachedPropertyInformation> m_CachedPropertyInfo;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_BuildingDatas;

		[ReadOnly]
		public ComponentLookup<PropertyOnMarket> m_PropertiesOnMarket;

		[ReadOnly]
		public BufferLookup<PathInformations> m_PathInformationBuffers;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public ComponentLookup<Building> m_Buildings;

		[ReadOnly]
		public ComponentLookup<Worker> m_Workers;

		[ReadOnly]
		public ComponentLookup<Game.Citizens.Student> m_Students;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> m_BuildingProperties;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableDatas;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		[ReadOnly]
		public BufferLookup<ResourceAvailability> m_Availabilities;

		[ReadOnly]
		public BufferLookup<Game.Net.ServiceCoverage> m_ServiceCoverages;

		[ReadOnly]
		public ComponentLookup<HomelessHousehold> m_HomelessHouseholds;

		[ReadOnly]
		public ComponentLookup<Citizen> m_Citizens;

		[ReadOnly]
		public ComponentLookup<CrimeProducer> m_Crimes;

		[ReadOnly]
		public ComponentLookup<Transform> m_Transforms;

		[ReadOnly]
		public ComponentLookup<Locked> m_Lockeds;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		[ReadOnly]
		public ComponentLookup<HealthProblem> m_HealthProblems;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Park> m_Parks;

		[ReadOnly]
		public ComponentLookup<Abandoned> m_Abandoneds;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> m_OwnedVehicles;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> m_ElectricityConsumers;

		[ReadOnly]
		public ComponentLookup<WaterConsumer> m_WaterConsumers;

		[ReadOnly]
		public ComponentLookup<GarbageProducer> m_GarbageProducers;

		[ReadOnly]
		public ComponentLookup<MailProducer> m_MailProducers;

		[ReadOnly]
		public ComponentLookup<Household> m_Households;

		[ReadOnly]
		public ComponentLookup<CurrentBuilding> m_CurrentBuildings;

		[ReadOnly]
		public ComponentLookup<CurrentTransport> m_CurrentTransports;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformations;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_CitizenBuffers;

		public ComponentLookup<PropertySeeker> m_PropertySeekers;

		[ReadOnly]
		public NativeArray<AirPollution> m_AirPollutionMap;

		[ReadOnly]
		public NativeArray<GroundPollution> m_PollutionMap;

		[ReadOnly]
		public NativeArray<NoisePollution> m_NoiseMap;

		[ReadOnly]
		public CellMapData<TelecomCoverage> m_TelecomCoverages;

		[ReadOnly]
		public NativeArray<int> m_TaxRates;

		[ReadOnly]
		public CountResidentialPropertySystem.ResidentialPropertyData m_ResidentialPropertyData;

		[ReadOnly]
		public HealthcareParameterData m_HealthcareParameters;

		[ReadOnly]
		public ParkParameterData m_ParkParameters;

		[ReadOnly]
		public EducationParameterData m_EducationParameters;

		[ReadOnly]
		public TelecomParameterData m_TelecomParameters;

		[ReadOnly]
		public GarbageParameterData m_GarbageParameters;

		[ReadOnly]
		public PoliceConfigurationData m_PoliceParameters;

		[ReadOnly]
		public CitizenHappinessParameterData m_CitizenHappinessParameterData;

		[ReadOnly]
		public EconomyParameterData m_EconomyParameters;

		[ReadOnly]
		public uint m_SimulationFrame;

		public EntityCommandBuffer m_CommandBuffer;

		[ReadOnly]
		public Entity m_City;

		public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;

		public NativeQueue<RentAction>.ParallelWriter m_RentActionQueue;

		private void StartHomeFinding(Entity household, Entity commuteCitizen, Entity targetLocation, Entity oldHome, float minimumScore, bool targetIsOrigin, DynamicBuffer<HouseholdCitizen> citizens)
		{
			m_CommandBuffer.AddComponent(household, new PathInformation
			{
				m_State = PathFlags.Pending
			});
			Household household2 = m_Households[household];
			PathfindWeights weights = default(PathfindWeights);
			if (m_Citizens.TryGetComponent(commuteCitizen, out var componentData))
			{
				weights = CitizenUtils.GetPathfindWeights(componentData, household2, citizens.Length);
			}
			else
			{
				for (int i = 0; i < citizens.Length; i++)
				{
					weights.m_Value += CitizenUtils.GetPathfindWeights(componentData, household2, citizens.Length).m_Value;
				}
				weights.m_Value *= 1f / (float)citizens.Length;
			}
			PathfindParameters parameters = new PathfindParameters
			{
				m_MaxSpeed = 111.111115f,
				m_WalkSpeed = 1.6666667f,
				m_Weights = weights,
				m_Methods = (PathMethod.Pedestrian | PathMethod.PublicTransportDay),
				m_MaxCost = CitizenBehaviorSystem.kMaxPathfindCost,
				m_PathfindFlags = (PathfindFlags.Simplified | PathfindFlags.IgnorePath)
			};
			SetupQueueTarget a = new SetupQueueTarget
			{
				m_Type = SetupTargetType.CurrentLocation,
				m_Methods = PathMethod.Pedestrian,
				m_Entity = targetLocation
			};
			SetupQueueTarget b = new SetupQueueTarget
			{
				m_Type = SetupTargetType.FindHome,
				m_Methods = PathMethod.Pedestrian,
				m_Entity = household,
				m_Entity2 = oldHome,
				m_Value2 = minimumScore
			};
			if (m_OwnedVehicles.TryGetBuffer(household, out var bufferData) && bufferData.Length != 0)
			{
				parameters.m_Methods |= (PathMethod)(targetIsOrigin ? 8194 : 8198);
				parameters.m_ParkingSize = float.MinValue;
				parameters.m_IgnoredRules |= RuleFlags.ForbidCombustionEngines | RuleFlags.ForbidHeavyTraffic | RuleFlags.ForbidSlowTraffic | RuleFlags.AvoidBicycles;
				a.m_Methods |= PathMethod.Road | PathMethod.MediumRoad;
				a.m_RoadTypes |= RoadTypes.Car;
				b.m_Methods |= PathMethod.Road | PathMethod.MediumRoad;
				b.m_RoadTypes |= RoadTypes.Car;
			}
			if (targetIsOrigin)
			{
				parameters.m_MaxSpeed.y = 277.77777f;
				parameters.m_Methods |= PathMethod.Taxi | PathMethod.PublicTransportNight;
				parameters.m_TaxiIgnoredRules = VehicleUtils.GetIgnoredPathfindRulesTaxiDefaults();
			}
			else
			{
				CommonUtils.Swap(ref a, ref b);
			}
			parameters.m_MaxResultCount = 10;
			parameters.m_PathfindFlags |= (PathfindFlags)(targetIsOrigin ? 256 : 128);
			m_CommandBuffer.AddBuffer<PathInformations>(household).Add(new PathInformations
			{
				m_State = PathFlags.Pending
			});
			SetupQueueItem value = new SetupQueueItem(household, parameters, a, b);
			m_PathfindQueue.Enqueue(value);
		}

		private Entity GetFirstWorkplaceOrSchool(DynamicBuffer<HouseholdCitizen> citizens, ref Entity citizen)
		{
			for (int i = 0; i < citizens.Length; i++)
			{
				citizen = citizens[i].m_Citizen;
				if (m_Workers.HasComponent(citizen))
				{
					return m_Workers[citizen].m_Workplace;
				}
				if (m_Students.HasComponent(citizen))
				{
					return m_Students[citizen].m_School;
				}
			}
			return Entity.Null;
		}

		private Entity GetCurrentLocation(DynamicBuffer<HouseholdCitizen> citizens)
		{
			for (int i = 0; i < citizens.Length; i++)
			{
				if (m_CurrentBuildings.TryGetComponent(citizens[i].m_Citizen, out var componentData))
				{
					return componentData.m_CurrentBuilding;
				}
				if (m_CurrentTransports.TryGetComponent(citizens[i].m_Citizen, out var componentData2))
				{
					return componentData2.m_CurrentTransport;
				}
			}
			return Entity.Null;
		}

		public void Execute()
		{
			int num = 0;
			for (int i = 0; i < m_HomelessHouseholdEntities.Length; i++)
			{
				Entity householdEntity = m_HomelessHouseholdEntities[i];
				if (ProcessFindHome(householdEntity))
				{
					num++;
				}
				if (num >= kMaxProcessHomelessHouseholdPerUpdate)
				{
					break;
				}
			}
			num = 0;
			for (int j = 0; j < m_MovedInHouseholdEntities.Length; j++)
			{
				Entity householdEntity2 = m_MovedInHouseholdEntities[j];
				if (ProcessFindHome(householdEntity2))
				{
					num++;
				}
				if (num >= kMaxProcessNormalHouseholdPerUpdate)
				{
					break;
				}
			}
		}

		private bool ProcessFindHome(Entity householdEntity)
		{
			DynamicBuffer<HouseholdCitizen> dynamicBuffer = m_CitizenBuffers[householdEntity];
			if (dynamicBuffer.Length == 0)
			{
				return false;
			}
			PropertySeeker propertySeeker = m_PropertySeekers[householdEntity];
			int householdIncome = EconomyUtils.GetHouseholdIncome(dynamicBuffer, ref m_Workers, ref m_Citizens, ref m_HealthProblems, ref m_EconomyParameters, m_TaxRates);
			if (m_PathInformationBuffers.TryGetBuffer(householdEntity, out var bufferData))
			{
				ProcessPathInformations(householdEntity, bufferData, propertySeeker, dynamicBuffer, householdIncome);
				return false;
			}
			Entity householdHomeBuilding = BuildingUtils.GetHouseholdHomeBuilding(householdEntity, ref m_PropertyRenters, ref m_HomelessHouseholds);
			float bestPropertyScore = ((householdHomeBuilding != Entity.Null) ? PropertyUtils.GetPropertyScore(householdHomeBuilding, householdEntity, dynamicBuffer, ref m_PrefabRefs, ref m_BuildingProperties, ref m_Buildings, ref m_BuildingDatas, ref m_Households, ref m_Citizens, ref m_Students, ref m_Workers, ref m_SpawnableDatas, ref m_Crimes, ref m_ServiceCoverages, ref m_Lockeds, ref m_ElectricityConsumers, ref m_WaterConsumers, ref m_GarbageProducers, ref m_MailProducers, ref m_Transforms, ref m_Abandoneds, ref m_Parks, ref m_Availabilities, m_TaxRates, m_PollutionMap, m_AirPollutionMap, m_NoiseMap, m_TelecomCoverages, m_CityModifiers[m_City], m_HealthcareParameters.m_HealthcareServicePrefab, m_ParkParameters.m_ParkServicePrefab, m_EducationParameters.m_EducationServicePrefab, m_TelecomParameters.m_TelecomServicePrefab, m_GarbageParameters.m_GarbageServicePrefab, m_PoliceParameters.m_PoliceServicePrefab, m_CitizenHappinessParameterData, m_GarbageParameters) : float.NegativeInfinity);
			if (householdHomeBuilding == Entity.Null && propertySeeker.m_LastPropertySeekFrame + kFindPropertyCoolDown > m_SimulationFrame)
			{
				if (m_PathInformations[householdEntity].m_State != PathFlags.Pending && math.csum(m_ResidentialPropertyData.m_FreeProperties) < 10)
				{
					CitizenUtils.HouseholdMoveAway(m_CommandBuffer, householdEntity);
				}
				return false;
			}
			Entity citizen = Entity.Null;
			Entity firstWorkplaceOrSchool = GetFirstWorkplaceOrSchool(dynamicBuffer, ref citizen);
			bool flag = firstWorkplaceOrSchool == Entity.Null;
			Entity entity = (flag ? GetCurrentLocation(dynamicBuffer) : firstWorkplaceOrSchool);
			if (householdHomeBuilding == Entity.Null && entity == Entity.Null)
			{
				CitizenUtils.HouseholdMoveAway(m_CommandBuffer, householdEntity);
				return false;
			}
			propertySeeker.m_TargetProperty = firstWorkplaceOrSchool;
			propertySeeker.m_BestProperty = householdHomeBuilding;
			propertySeeker.m_BestPropertyScore = bestPropertyScore;
			propertySeeker.m_LastPropertySeekFrame = m_SimulationFrame;
			m_PropertySeekers[householdEntity] = propertySeeker;
			StartHomeFinding(householdEntity, citizen, entity, householdHomeBuilding, propertySeeker.m_BestPropertyScore, flag, dynamicBuffer);
			return true;
		}

		private void ProcessPathInformations(Entity householdEntity, DynamicBuffer<PathInformations> pathInformations, PropertySeeker propertySeeker, DynamicBuffer<HouseholdCitizen> citizens, int income)
		{
			int num = 0;
			PathInformations pathInformations2 = pathInformations[num];
			if ((pathInformations2.m_State & PathFlags.Pending) != 0)
			{
				return;
			}
			m_CommandBuffer.RemoveComponent<PathInformations>(householdEntity);
			bool flag = propertySeeker.m_TargetProperty != Entity.Null;
			Entity entity = (flag ? pathInformations2.m_Origin : pathInformations2.m_Destination);
			bool flag2 = false;
			while (!m_CachedPropertyInfo.ContainsKey(entity) || m_CachedPropertyInfo[entity].free <= 0)
			{
				num++;
				if (pathInformations.Length > num)
				{
					pathInformations2 = pathInformations[num];
					entity = (flag ? pathInformations2.m_Origin : pathInformations2.m_Destination);
					continue;
				}
				entity = Entity.Null;
				flag2 = true;
				break;
			}
			if (flag2 && pathInformations.Length != 0 && pathInformations[0].m_Destination != Entity.Null)
			{
				return;
			}
			float num2 = float.NegativeInfinity;
			if (entity != Entity.Null && m_CachedPropertyInfo.ContainsKey(entity) && m_CachedPropertyInfo[entity].free > 0)
			{
				num2 = PropertyUtils.GetPropertyScore(entity, householdEntity, citizens, ref m_PrefabRefs, ref m_BuildingProperties, ref m_Buildings, ref m_BuildingDatas, ref m_Households, ref m_Citizens, ref m_Students, ref m_Workers, ref m_SpawnableDatas, ref m_Crimes, ref m_ServiceCoverages, ref m_Lockeds, ref m_ElectricityConsumers, ref m_WaterConsumers, ref m_GarbageProducers, ref m_MailProducers, ref m_Transforms, ref m_Abandoneds, ref m_Parks, ref m_Availabilities, m_TaxRates, m_PollutionMap, m_AirPollutionMap, m_NoiseMap, m_TelecomCoverages, m_CityModifiers[m_City], m_HealthcareParameters.m_HealthcareServicePrefab, m_ParkParameters.m_ParkServicePrefab, m_EducationParameters.m_EducationServicePrefab, m_TelecomParameters.m_TelecomServicePrefab, m_GarbageParameters.m_GarbageServicePrefab, m_PoliceParameters.m_PoliceServicePrefab, m_CitizenHappinessParameterData, m_GarbageParameters);
			}
			if (num2 < propertySeeker.m_BestPropertyScore)
			{
				entity = propertySeeker.m_BestProperty;
			}
			bool flag3 = (m_Households[householdEntity].m_Flags & HouseholdFlags.MovedIn) != 0;
			bool flag4 = entity != Entity.Null && BuildingUtils.IsHomelessShelterBuilding(entity, ref m_Parks, ref m_Abandoneds);
			bool flag5 = CitizenUtils.IsHouseholdNeedSupport(citizens, ref m_Citizens, ref m_Students);
			bool flag6 = m_PropertiesOnMarket.HasComponent(entity) && (flag5 || m_PropertiesOnMarket[entity].m_AskingRent < income);
			bool flag7 = !m_PropertyRenters.HasComponent(householdEntity) || !m_PropertyRenters[householdEntity].m_Property.Equals(entity);
			Entity householdHomeBuilding = BuildingUtils.GetHouseholdHomeBuilding(householdEntity, ref m_PropertyRenters, ref m_HomelessHouseholds);
			if (householdHomeBuilding != Entity.Null && householdHomeBuilding == entity)
			{
				if (!m_HomelessHouseholds.HasComponent(householdEntity) && !flag5 && income < m_PropertyRenters[householdEntity].m_Rent)
				{
					CitizenUtils.HouseholdMoveAway(m_CommandBuffer, householdEntity, MoveAwayReason.NoMoney);
				}
				else
				{
					m_CommandBuffer.SetComponentEnabled<PropertySeeker>(householdEntity, value: false);
				}
			}
			else if ((flag6 && flag7) || (flag3 && flag4))
			{
				m_RentActionQueue.Enqueue(new RentAction
				{
					m_Property = entity,
					m_Renter = householdEntity
				});
				if (m_CachedPropertyInfo.ContainsKey(entity))
				{
					CachedPropertyInformation value = m_CachedPropertyInfo[entity];
					value.free--;
					m_CachedPropertyInfo[entity] = value;
				}
				m_CommandBuffer.SetComponentEnabled<PropertySeeker>(householdEntity, value: false);
			}
			else if (entity == Entity.Null && (!m_HomelessHouseholds.HasComponent(householdEntity) || m_HomelessHouseholds[householdEntity].m_TempHome == Entity.Null))
			{
				CitizenUtils.HouseholdMoveAway(m_CommandBuffer, householdEntity);
			}
			else
			{
				propertySeeker.m_BestProperty = default(Entity);
				propertySeeker.m_BestPropertyScore = float.NegativeInfinity;
				m_PropertySeekers[householdEntity] = propertySeeker;
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RW_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkData> __Game_Prefabs_ParkData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Abandoned> __Game_Buildings_Abandoned_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Park> __Game_Buildings_Park_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.ServiceCoverage> __Game_Net_ServiceCoverage_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<CrimeProducer> __Game_Buildings_CrimeProducer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Locked> __Game_Prefabs_Locked_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> __Game_Buildings_ElectricityConsumer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaterConsumer> __Game_Buildings_WaterConsumer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GarbageProducer> __Game_Buildings_GarbageProducer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MailProducer> __Game_Buildings_MailProducer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyOnMarket> __Game_Buildings_PropertyOnMarket_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ResourceAvailability> __Game_Net_ResourceAvailability_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<PathInformations> __Game_Pathfind_PathInformations_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Worker> __Game_Citizens_Worker_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Citizens.Student> __Game_Citizens_Student_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HomelessHousehold> __Game_Citizens_HomelessHousehold_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentTransport> __Game_Citizens_CurrentTransport_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		public ComponentLookup<PropertySeeker> __Game_Agents_PropertySeeker_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_BuildingPropertyData_RW_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>();
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_ParkData_RO_ComponentLookup = state.GetComponentLookup<ParkData>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(isReadOnly: true);
			__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(isReadOnly: true);
			__Game_Buildings_Abandoned_RO_ComponentLookup = state.GetComponentLookup<Abandoned>(isReadOnly: true);
			__Game_Buildings_Park_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.Park>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Net_ServiceCoverage_RO_BufferLookup = state.GetBufferLookup<Game.Net.ServiceCoverage>(isReadOnly: true);
			__Game_Buildings_CrimeProducer_RO_ComponentLookup = state.GetComponentLookup<CrimeProducer>(isReadOnly: true);
			__Game_Prefabs_Locked_RO_ComponentLookup = state.GetComponentLookup<Locked>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
			__Game_Buildings_ElectricityConsumer_RO_ComponentLookup = state.GetComponentLookup<ElectricityConsumer>(isReadOnly: true);
			__Game_Buildings_WaterConsumer_RO_ComponentLookup = state.GetComponentLookup<WaterConsumer>(isReadOnly: true);
			__Game_Buildings_GarbageProducer_RO_ComponentLookup = state.GetComponentLookup<GarbageProducer>(isReadOnly: true);
			__Game_Buildings_MailProducer_RO_ComponentLookup = state.GetComponentLookup<MailProducer>(isReadOnly: true);
			__Game_Buildings_PropertyOnMarket_RO_ComponentLookup = state.GetComponentLookup<PropertyOnMarket>(isReadOnly: true);
			__Game_Net_ResourceAvailability_RO_BufferLookup = state.GetBufferLookup<ResourceAvailability>(isReadOnly: true);
			__Game_Pathfind_PathInformations_RO_BufferLookup = state.GetBufferLookup<PathInformations>(isReadOnly: true);
			__Game_Citizens_Worker_RO_ComponentLookup = state.GetComponentLookup<Worker>(isReadOnly: true);
			__Game_Citizens_Student_RO_ComponentLookup = state.GetComponentLookup<Game.Citizens.Student>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Citizens_HomelessHousehold_RO_ComponentLookup = state.GetComponentLookup<HomelessHousehold>(isReadOnly: true);
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Citizens_HealthProblem_RO_ComponentLookup = state.GetComponentLookup<HealthProblem>(isReadOnly: true);
			__Game_Vehicles_OwnedVehicle_RO_BufferLookup = state.GetBufferLookup<OwnedVehicle>(isReadOnly: true);
			__Game_Citizens_CurrentBuilding_RO_ComponentLookup = state.GetComponentLookup<CurrentBuilding>(isReadOnly: true);
			__Game_Citizens_CurrentTransport_RO_ComponentLookup = state.GetComponentLookup<CurrentTransport>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Agents_PropertySeeker_RW_ComponentLookup = state.GetComponentLookup<PropertySeeker>();
		}
	}

	public bool debugDisableHomeless;

	private const int UPDATE_INTERVAL = 16;

	public static readonly int kMaxProcessNormalHouseholdPerUpdate = 128;

	public static readonly int kMaxProcessHomelessHouseholdPerUpdate = 1280;

	public static readonly int kFindPropertyCoolDown = 5000;

	[DebugWatchValue]
	private DebugWatchDistribution m_DefaultDistribution;

	[DebugWatchValue]
	private DebugWatchDistribution m_EvaluateDistributionLow;

	[DebugWatchValue]
	private DebugWatchDistribution m_EvaluateDistributionMedium;

	[DebugWatchValue]
	private DebugWatchDistribution m_EvaluateDistributionHigh;

	[DebugWatchValue]
	private DebugWatchDistribution m_EvaluateDistributionLowrent;

	private EntityQuery m_HouseholdQuery;

	private EntityQuery m_HomelessHouseholdQuery;

	private EntityQuery m_FreePropertyQuery;

	private EntityQuery m_EconomyParameterQuery;

	private EntityQuery m_DemandParameterQuery;

	private EndFrameBarrier m_EndFrameBarrier;

	private PathfindSetupSystem m_PathfindSetupSystem;

	private TaxSystem m_TaxSystem;

	private TriggerSystem m_TriggerSystem;

	private GroundPollutionSystem m_GroundPollutionSystem;

	private AirPollutionSystem m_AirPollutionSystem;

	private NoisePollutionSystem m_NoisePollutionSystem;

	private TelecomCoverageSystem m_TelecomCoverageSystem;

	private CitySystem m_CitySystem;

	private CityStatisticsSystem m_CityStatisticsSystem;

	private SimulationSystem m_SimulationSystem;

	private PropertyProcessingSystem m_PropertyProcessingSystem;

	private CountResidentialPropertySystem m_CountResidentialPropertySystem;

	private EntityQuery m_HealthcareParameterQuery;

	private EntityQuery m_ParkParameterQuery;

	private EntityQuery m_EducationParameterQuery;

	private EntityQuery m_TelecomParameterQuery;

	private EntityQuery m_GarbageParameterQuery;

	private EntityQuery m_PoliceParameterQuery;

	private EntityQuery m_CitizenHappinessParameterQuery;

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
		m_PathfindSetupSystem = base.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
		m_GroundPollutionSystem = base.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
		m_AirPollutionSystem = base.World.GetOrCreateSystemManaged<AirPollutionSystem>();
		m_NoisePollutionSystem = base.World.GetOrCreateSystemManaged<NoisePollutionSystem>();
		m_TelecomCoverageSystem = base.World.GetOrCreateSystemManaged<TelecomCoverageSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_TaxSystem = base.World.GetOrCreateSystemManaged<TaxSystem>();
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_PropertyProcessingSystem = base.World.GetOrCreateSystemManaged<PropertyProcessingSystem>();
		m_CountResidentialPropertySystem = base.World.GetOrCreateSystemManaged<CountResidentialPropertySystem>();
		m_HomelessHouseholdQuery = GetEntityQuery(ComponentType.ReadWrite<HomelessHousehold>(), ComponentType.ReadWrite<PropertySeeker>(), ComponentType.ReadOnly<HouseholdCitizen>(), ComponentType.Exclude<MovingAway>(), ComponentType.Exclude<TouristHousehold>(), ComponentType.Exclude<CommuterHousehold>(), ComponentType.Exclude<CurrentBuilding>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_HouseholdQuery = GetEntityQuery(ComponentType.ReadWrite<Household>(), ComponentType.ReadWrite<PropertySeeker>(), ComponentType.ReadOnly<HouseholdCitizen>(), ComponentType.Exclude<HomelessHousehold>(), ComponentType.Exclude<MovingAway>(), ComponentType.Exclude<TouristHousehold>(), ComponentType.Exclude<CommuterHousehold>(), ComponentType.Exclude<CurrentBuilding>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		m_DemandParameterQuery = GetEntityQuery(ComponentType.ReadOnly<DemandParameterData>());
		m_HealthcareParameterQuery = GetEntityQuery(ComponentType.ReadOnly<HealthcareParameterData>());
		m_ParkParameterQuery = GetEntityQuery(ComponentType.ReadOnly<ParkParameterData>());
		m_EducationParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EducationParameterData>());
		m_TelecomParameterQuery = GetEntityQuery(ComponentType.ReadOnly<TelecomParameterData>());
		m_GarbageParameterQuery = GetEntityQuery(ComponentType.ReadOnly<GarbageParameterData>());
		m_PoliceParameterQuery = GetEntityQuery(ComponentType.ReadOnly<PoliceConfigurationData>());
		m_CitizenHappinessParameterQuery = GetEntityQuery(ComponentType.ReadOnly<CitizenHappinessParameterData>());
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Building>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Abandoned>(),
				ComponentType.ReadOnly<Game.Buildings.Park>()
			},
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Temp>()
			}
		};
		EntityQueryDesc entityQueryDesc2 = new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<PropertyOnMarket>(),
				ComponentType.ReadOnly<ResidentialProperty>(),
				ComponentType.ReadOnly<Building>()
			},
			None = new ComponentType[5]
			{
				ComponentType.ReadOnly<Abandoned>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Condemned>()
			}
		};
		m_FreePropertyQuery = GetEntityQuery(entityQueryDesc, entityQueryDesc2);
		RequireForUpdate(m_EconomyParameterQuery);
		RequireForUpdate(m_HealthcareParameterQuery);
		RequireForUpdate(m_ParkParameterQuery);
		RequireForUpdate(m_EducationParameterQuery);
		RequireForUpdate(m_TelecomParameterQuery);
		RequireForUpdate(m_HouseholdQuery);
		RequireForUpdate(m_DemandParameterQuery);
		m_DefaultDistribution = new DebugWatchDistribution(persistent: true, relative: true);
		m_EvaluateDistributionLow = new DebugWatchDistribution(persistent: true, relative: true);
		m_EvaluateDistributionMedium = new DebugWatchDistribution(persistent: true, relative: true);
		m_EvaluateDistributionHigh = new DebugWatchDistribution(persistent: true, relative: true);
		m_EvaluateDistributionLowrent = new DebugWatchDistribution(persistent: true, relative: true);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_DefaultDistribution.Dispose();
		m_EvaluateDistributionLow.Dispose();
		m_EvaluateDistributionMedium.Dispose();
		m_EvaluateDistributionHigh.Dispose();
		m_EvaluateDistributionLowrent.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeParallelHashMap<Entity, CachedPropertyInformation> cachedPropertyInfo = new NativeParallelHashMap<Entity, CachedPropertyInformation>(m_FreePropertyQuery.CalculateEntityCount(), Allocator.TempJob);
		JobHandle dependencies;
		NativeArray<GroundPollution> map = m_GroundPollutionSystem.GetMap(readOnly: true, out dependencies);
		JobHandle dependencies2;
		NativeArray<AirPollution> map2 = m_AirPollutionSystem.GetMap(readOnly: true, out dependencies2);
		JobHandle dependencies3;
		NativeArray<NoisePollution> map3 = m_NoisePollutionSystem.GetMap(readOnly: true, out dependencies3);
		JobHandle dependencies4;
		CellMapData<TelecomCoverage> data = m_TelecomCoverageSystem.GetData(readOnly: true, out dependencies4);
		PreparePropertyJob jobData = new PreparePropertyJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_BuildingProperties = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RW_ComponentLookup, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ParkData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Renters = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
			m_Households = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Abandoneds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Parks = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Park_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnableDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingPropertyData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Buildings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceCoverages = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ServiceCoverage_RO_BufferLookup, ref base.CheckedStateRef),
			m_Crimes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_CrimeProducer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Locked = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_Locked_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Transforms = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
			m_ElectricityConsumers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WaterConsumers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GarbageProducers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_GarbageProducer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MailProducers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_MailProducer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PollutionMap = map,
			m_AirPollutionMap = map2,
			m_NoiseMap = map3,
			m_TelecomCoverages = data,
			m_HealthcareParameters = m_HealthcareParameterQuery.GetSingleton<HealthcareParameterData>(),
			m_ParkParameters = m_ParkParameterQuery.GetSingleton<ParkParameterData>(),
			m_EducationParameters = m_EducationParameterQuery.GetSingleton<EducationParameterData>(),
			m_TelecomParameters = m_TelecomParameterQuery.GetSingleton<TelecomParameterData>(),
			m_GarbageParameters = m_GarbageParameterQuery.GetSingleton<GarbageParameterData>(),
			m_PoliceParameters = m_PoliceParameterQuery.GetSingleton<PoliceConfigurationData>(),
			m_CitizenHappinessParameterData = m_CitizenHappinessParameterQuery.GetSingleton<CitizenHappinessParameterData>(),
			m_City = m_CitySystem.City,
			m_PropertyData = cachedPropertyInfo.AsParallelWriter()
		};
		JobHandle outJobHandle;
		JobHandle outJobHandle2;
		JobHandle deps;
		FindPropertyJob jobData2 = new FindPropertyJob
		{
			m_HomelessHouseholdEntities = m_HomelessHouseholdQuery.ToEntityListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
			m_MovedInHouseholdEntities = m_HouseholdQuery.ToEntityListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle2),
			m_CachedPropertyInfo = cachedPropertyInfo,
			m_BuildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertiesOnMarket = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyOnMarket_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Availabilities = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ResourceAvailability_RO_BufferLookup, ref base.CheckedStateRef),
			m_SpawnableDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingProperties = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Buildings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathInformationBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathInformations_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceCoverages = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ServiceCoverage_RO_BufferLookup, ref base.CheckedStateRef),
			m_Workers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Students = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Student_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertyRenters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HomelessHouseholds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HomelessHousehold_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Citizens = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Crimes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_CrimeProducer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Lockeds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_Locked_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Transforms = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
			m_HealthProblems = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Abandoneds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Parks = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Park_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnedVehicles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup, ref base.CheckedStateRef),
			m_ElectricityConsumers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WaterConsumers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GarbageProducers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_GarbageProducer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MailProducers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_MailProducer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Households = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentBuildings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentTransports = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CurrentTransport_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathInformations = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CitizenBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
			m_PropertySeekers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Agents_PropertySeeker_RW_ComponentLookup, ref base.CheckedStateRef),
			m_PollutionMap = map,
			m_AirPollutionMap = map2,
			m_NoiseMap = map3,
			m_TelecomCoverages = data,
			m_ResidentialPropertyData = m_CountResidentialPropertySystem.GetResidentialPropertyData(),
			m_HealthcareParameters = m_HealthcareParameterQuery.GetSingleton<HealthcareParameterData>(),
			m_ParkParameters = m_ParkParameterQuery.GetSingleton<ParkParameterData>(),
			m_EducationParameters = m_EducationParameterQuery.GetSingleton<EducationParameterData>(),
			m_TelecomParameters = m_TelecomParameterQuery.GetSingleton<TelecomParameterData>(),
			m_GarbageParameters = m_GarbageParameterQuery.GetSingleton<GarbageParameterData>(),
			m_PoliceParameters = m_PoliceParameterQuery.GetSingleton<PoliceConfigurationData>(),
			m_CitizenHappinessParameterData = m_CitizenHappinessParameterQuery.GetSingleton<CitizenHappinessParameterData>(),
			m_TaxRates = m_TaxSystem.GetTaxRates(),
			m_EconomyParameters = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_RentActionQueue = m_PropertyProcessingSystem.GetRentActionQueue(out deps).AsParallelWriter(),
			m_City = m_CitySystem.City,
			m_PathfindQueue = m_PathfindSetupSystem.GetQueue(this, 80, 16).AsParallelWriter(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer()
		};
		JobHandle job = JobChunkExtensions.ScheduleParallel(jobData, m_FreePropertyQuery, JobUtils.CombineDependencies(base.Dependency, dependencies, dependencies3, dependencies2, dependencies4, deps));
		base.Dependency = IJobExtensions.Schedule(jobData2, JobHandle.CombineDependencies(job, outJobHandle2, outJobHandle));
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
		m_PathfindSetupSystem.AddQueueWriter(base.Dependency);
		m_AirPollutionSystem.AddReader(base.Dependency);
		m_NoisePollutionSystem.AddReader(base.Dependency);
		m_GroundPollutionSystem.AddReader(base.Dependency);
		m_TelecomCoverageSystem.AddReader(base.Dependency);
		m_TriggerSystem.AddActionBufferWriter(base.Dependency);
		m_CityStatisticsSystem.AddWriter(base.Dependency);
		m_TaxSystem.AddReader(base.Dependency);
		cachedPropertyInfo.Dispose(base.Dependency);
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
	public HouseholdFindPropertySystem()
	{
	}
}

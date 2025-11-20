using Colossal.Entities;
using Game.Agents;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Simulation;

public struct CitizenPathfindSetup
{
	[BurstCompile]
	private struct SetupTouristTargetJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentLookup<LodgingProvider> m_LodgingProviders;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingDatas;

		[ReadOnly]
		public ComponentLookup<TouristHousehold> m_TouristHouseholds;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		[ReadOnly]
		public BufferLookup<ResourceAvailability> m_ResourceAvailabilityBufs;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			for (int i = 0; i < m_SetupData.Length; i++)
			{
				m_SetupData.GetItem(i, out var entity, out var targetSeeker);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Entity entity2 = nativeArray[j];
					Entity entity3 = entity2;
					float num = 0f;
					bool flag = m_TouristHouseholds.HasComponent(entity) && m_TouristHouseholds[entity].m_Hotel == Entity.Null;
					if (m_LodgingProviders.HasComponent(entity2) && flag)
					{
						if (m_LodgingProviders[entity2].m_FreeRooms == 0 || !m_PropertyRenters.HasComponent(entity2) || m_PropertyRenters[entity2].m_Property == Entity.Null)
						{
							continue;
						}
						entity3 = m_PropertyRenters[entity2].m_Property;
						num -= 5000f;
						num += -10f * (float)m_LodgingProviders[entity2].m_FreeRooms;
						float x = m_LodgingProviders[entity2].m_Price;
						num += math.min(x, 500f);
					}
					else
					{
						num += 5000f;
					}
					if (!m_BuildingDatas.HasComponent(entity3))
					{
						continue;
					}
					Building building = m_BuildingDatas[entity3];
					if (!BuildingUtils.CheckOption(building, BuildingOption.Inactive))
					{
						if (m_ResourceAvailabilityBufs.HasBuffer(building.m_RoadEdge))
						{
							float availability = NetUtils.GetAvailability(m_ResourceAvailabilityBufs[building.m_RoadEdge], AvailableResource.Attractiveness, building.m_CurvePosition);
							num -= availability * 0.01f;
						}
						targetSeeker.FindTargets(entity3, num);
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
	private struct SetupLeisureTargetJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<ServiceAvailable> m_ServiceAvailableType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_IndustrialProcessDatas;

		[ReadOnly]
		public ComponentLookup<LeisureProviderData> m_LeisureProviderDatas;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public ComponentLookup<ServiceCompanyData> m_ServiceDatas;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingDatas;

		[ReadOnly]
		public BufferLookup<Game.Economy.Resources> m_Resources;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		public PathfindSetupSystem.SetupData m_SetupData;

		public int m_LeisureSystemUpdateInterval;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<ServiceAvailable> nativeArray2 = chunk.GetNativeArray(ref m_ServiceAvailableType);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabType);
			for (int i = 0; i < m_SetupData.Length; i++)
			{
				m_SetupData.GetItem(i, out var _, out var targetSeeker);
				LeisureType value = (LeisureType)targetSeeker.m_SetupQueueTarget.m_Value;
				float value2 = targetSeeker.m_SetupQueueTarget.m_Value2;
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Entity entity2 = nativeArray[j];
					if (m_BuildingDatas.HasComponent(entity2) && BuildingUtils.CheckOption(m_BuildingDatas[entity2], BuildingOption.Inactive))
					{
						continue;
					}
					Entity prefab = nativeArray3[j].m_Prefab;
					if (!m_LeisureProviderDatas.HasComponent(prefab))
					{
						continue;
					}
					LeisureProviderData leisureProviderData = m_LeisureProviderDatas[prefab];
					float cost = 0f;
					if (value != leisureProviderData.m_LeisureType)
					{
						continue;
					}
					if ((value == LeisureType.Commercial || value == LeisureType.Meals) && nativeArray2.Length > 0 && m_ServiceDatas.HasComponent(prefab))
					{
						int serviceAvailable = nativeArray2[j].m_ServiceAvailable;
						if ((float)serviceAvailable < value2)
						{
							continue;
						}
						if (m_IndustrialProcessDatas.HasComponent(prefab))
						{
							IndustrialProcessData industrialProcessData = m_IndustrialProcessDatas[prefab];
							if (industrialProcessData.m_Output.m_Resource != Resource.NoResource)
							{
								serviceAvailable = math.min(serviceAvailable, EconomyUtils.GetResources(industrialProcessData.m_Output.m_Resource, m_Resources[entity2]));
								cost = 1000f * (1f - math.saturate(1f * (float)serviceAvailable / (float)m_ServiceDatas[prefab].m_MaxService) * 2f);
							}
						}
					}
					targetSeeker.FindTargets(entity2, cost);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct SetupSchoolSeekerToJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<Game.Buildings.Student> m_StudentType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_UpgradeType;

		[ReadOnly]
		public ComponentLookup<SchoolData> m_SchoolDatas;

		[ReadOnly]
		public BufferLookup<Efficiency> m_Efficiencies;

		[ReadOnly]
		public BufferLookup<ServiceDistrict> m_ServiceDistricts;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<Game.Buildings.Student> bufferAccessor = chunk.GetBufferAccessor(ref m_StudentType);
			BufferAccessor<InstalledUpgrade> bufferAccessor2 = chunk.GetBufferAccessor(ref m_UpgradeType);
			bool flag = bufferAccessor2.Length != 0;
			for (int i = 0; i < m_SetupData.Length; i++)
			{
				m_SetupData.GetItem(i, out var entity, out var targetSeeker);
				int value = targetSeeker.m_SetupQueueTarget.m_Value;
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Entity entity2 = nativeArray[j];
					if (!AreaUtils.CheckServiceDistrict(entity, entity2, m_ServiceDistricts))
					{
						continue;
					}
					Entity prefab = nativeArray2[j].m_Prefab;
					if (m_SchoolDatas.HasComponent(prefab))
					{
						SchoolData data = m_SchoolDatas[prefab];
						if (flag)
						{
							UpgradeUtils.CombineStats(ref data, bufferAccessor2[j], ref targetSeeker.m_PrefabRef, ref m_SchoolDatas);
						}
						int num = data.m_StudentCapacity;
						if (m_Efficiencies.TryGetBuffer(entity2, out var bufferData))
						{
							num = Mathf.RoundToInt((float)num * math.min(1f, BuildingUtils.GetEfficiency(bufferData)));
						}
						bool flag2 = data.m_EducationLevel == 5;
						int num2 = num - bufferAccessor[j].Length;
						if (((flag2 && value > 1) || data.m_EducationLevel == value) && num2 > 0)
						{
							targetSeeker.FindTargets(entity2, math.max(0f, (float)(-num2) + (flag2 ? 5000f : 0f)));
						}
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
	private struct SetupJobSeekerToJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<FreeWorkplaces> m_FreeWorkplaceType;

		[ReadOnly]
		public ComponentTypeHandle<WorkProvider> m_WorkProviderType;

		[ReadOnly]
		public ComponentTypeHandle<CityServiceUpkeep> m_CityServiceType;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<FreeWorkplaces> nativeArray2 = chunk.GetNativeArray(ref m_FreeWorkplaceType);
			NativeArray<WorkProvider> nativeArray3 = chunk.GetNativeArray(ref m_WorkProviderType);
			float num = (chunk.Has(ref m_CityServiceType) ? (-4000f) : 0f);
			for (int i = 0; i < m_SetupData.Length; i++)
			{
				m_SetupData.GetItem(i, out var _, out var targetSeeker);
				Unity.Mathematics.Random random = targetSeeker.m_RandomSeed.GetRandom(unfilteredChunkIndex);
				int num2 = targetSeeker.m_SetupQueueTarget.m_Value % 5;
				int num3 = targetSeeker.m_SetupQueueTarget.m_Value / 5 - 1;
				float value = targetSeeker.m_SetupQueueTarget.m_Value2;
				SetupTargetFlags flags = targetSeeker.m_SetupQueueTarget.m_Flags;
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Entity entity2 = nativeArray[j];
					FreeWorkplaces freeWorkplaces = nativeArray2[j];
					if ((flags & SetupTargetFlags.Export) != SetupTargetFlags.None)
					{
						if (freeWorkplaces.GetFree(num2) > 0 && !m_OutsideConnections.HasComponent(entity2))
						{
							targetSeeker.FindTargets(entity2, 2000f);
						}
					}
					else
					{
						if ((flags & SetupTargetFlags.Import) != SetupTargetFlags.None && m_OutsideConnections.HasComponent(entity2))
						{
							continue;
						}
						int lowestFree = freeWorkplaces.GetLowestFree();
						if (num2 >= lowestFree && num2 >= num3)
						{
							int bestFor = freeWorkplaces.GetBestFor(num2);
							int num4 = ((nativeArray3.Length > 0) ? nativeArray3[j].m_MaxWorkers : 0);
							if (freeWorkplaces.Count > 0 && num4 > 0)
							{
								float num5 = (float)freeWorkplaces.Count / (float)num4;
								int num6 = random.NextInt(4000);
								int num7 = (m_OutsideConnections.HasComponent(entity2) ? 8000 : (-4000));
								targetSeeker.FindTargets(entity2, 6000f * (1f - num5) + math.max(0f, 2f - value) * 4000f * (float)(num2 - bestFor) + num + (float)num6 + (float)num7);
							}
						}
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
	private struct SetupAttractionJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentLookup<AttractivenessProvider> m_AttractivenessProviders;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			for (int i = 0; i < m_SetupData.Length; i++)
			{
				m_SetupData.GetItem(i, out var _, out var targetSeeker);
				Unity.Mathematics.Random random = targetSeeker.m_RandomSeed.GetRandom(unfilteredChunkIndex);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Entity entity2 = nativeArray[j];
					if (m_AttractivenessProviders.HasComponent(entity2))
					{
						targetSeeker.FindTargets(entity2, -100f * (float)m_AttractivenessProviders[entity2].m_Attractiveness * random.NextFloat());
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
	private struct SetupHomelessJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<Renter> m_RenterType;

		[ReadOnly]
		public ComponentTypeHandle<Building> m_BuildingType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_BuildingDatas;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> m_BuildingProperties;

		[ReadOnly]
		public BufferLookup<Game.Net.ServiceCoverage> m_Coverages;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabType);
			NativeArray<Building> nativeArray3 = chunk.GetNativeArray(ref m_BuildingType);
			BufferAccessor<Renter> bufferAccessor = chunk.GetBufferAccessor(ref m_RenterType);
			for (int i = 0; i < m_SetupData.Length; i++)
			{
				m_SetupData.GetItem(i, out var _, out var targetSeeker);
				Unity.Mathematics.Random random = targetSeeker.m_RandomSeed.GetRandom(unfilteredChunkIndex);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Entity entity2 = nativeArray[j];
					Entity prefab = nativeArray2[j].m_Prefab;
					Building building = nativeArray3[j];
					if (building.m_RoadEdge != Entity.Null && m_Coverages.HasBuffer(building.m_RoadEdge) && m_BuildingDatas.HasComponent(prefab))
					{
						float serviceCoverage = NetUtils.GetServiceCoverage(m_Coverages[building.m_RoadEdge], CoverageService.Police, building.m_CurvePosition);
						float num = BuildingUtils.GetShelterHomelessCapacity(prefab, ref m_BuildingDatas, ref m_BuildingProperties);
						targetSeeker.FindTargets(entity2, 100f * serviceCoverage + 1000f * ((float)bufferAccessor[j].Length / num) + random.NextFloat(1000f));
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
	private struct SetupFindHomeJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<Renter> m_RenterType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public ComponentLookup<Building> m_Buildings;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_BuildingDatas;

		[ReadOnly]
		public ComponentLookup<ZoneData> m_ZoneDatas;

		[ReadOnly]
		public ComponentLookup<ZonePropertiesData> m_ZonePropertiesDatas;

		[ReadOnly]
		public BufferLookup<Game.Net.ServiceCoverage> m_Coverages;

		public PathfindSetupSystem.SetupData m_SetupData;

		[ReadOnly]
		public ComponentLookup<PropertyOnMarket> m_PropertiesOnMarket;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> m_BuildingProperties;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableDatas;

		[ReadOnly]
		public BufferLookup<ResourceAvailability> m_Availabilities;

		[ReadOnly]
		public BufferLookup<Game.Net.ServiceCoverage> m_ServiceCoverages;

		[ReadOnly]
		public ComponentLookup<Household> m_Households;

		[ReadOnly]
		public ComponentLookup<HomelessHousehold> m_HomelessHouseholds;

		[ReadOnly]
		public ComponentLookup<Citizen> m_Citizens;

		[ReadOnly]
		public ComponentLookup<HealthProblem> m_HealthProblems;

		[ReadOnly]
		public ComponentLookup<Worker> m_Workers;

		[ReadOnly]
		public ComponentLookup<Game.Citizens.Student> m_Students;

		[ReadOnly]
		public ComponentLookup<CrimeProducer> m_Crimes;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_Transforms;

		[ReadOnly]
		public ComponentLookup<Locked> m_Lockeds;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Park> m_Parks;

		[ReadOnly]
		public ComponentLookup<Abandoned> m_Abandoneds;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		[ReadOnly]
		public BufferLookup<Game.Economy.Resources> m_ResourcesBufs;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> m_ElectricityConsumers;

		[ReadOnly]
		public ComponentLookup<WaterConsumer> m_WaterConsumers;

		[ReadOnly]
		public ComponentLookup<GarbageProducer> m_GarbageProducers;

		[ReadOnly]
		public ComponentLookup<MailProducer> m_MailProducers;

		[ReadOnly]
		public NativeArray<int> m_TaxRates;

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

		public EconomyParameterData m_EconomyParameters;

		public TelecomParameterData m_TelecomParameters;

		public GarbageParameterData m_GarbageParameters;

		public PoliceConfigurationData m_PoliceParameters;

		public ServiceFeeParameterData m_ServiceFeeParameterData;

		public CitizenHappinessParameterData m_CitizenHappinessParameterData;

		[ReadOnly]
		public Entity m_City;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabType);
			BufferAccessor<Renter> bufferAccessor = chunk.GetBufferAccessor(ref m_RenterType);
			for (int i = 0; i < m_SetupData.Length; i++)
			{
				m_SetupData.GetItem(i, out var _, out var targetSeeker);
				Unity.Mathematics.Random random = targetSeeker.m_RandomSeed.GetRandom(unfilteredChunkIndex);
				Entity entity2 = targetSeeker.m_SetupQueueTarget.m_Entity;
				if (!m_HouseholdCitizens.TryGetBuffer(entity2, out var bufferData))
				{
					continue;
				}
				bool flag = m_HomelessHouseholds.HasComponent(entity2) && m_HomelessHouseholds[entity2].m_TempHome != Entity.Null;
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Entity entity3 = nativeArray[j];
					Entity prefab = nativeArray2[j].m_Prefab;
					Building building = m_Buildings[entity3];
					if (!(building.m_RoadEdge != Entity.Null) || !m_Coverages.HasBuffer(building.m_RoadEdge) || !m_BuildingDatas.HasComponent(prefab))
					{
						continue;
					}
					if (BuildingUtils.IsHomelessShelterBuilding(entity3, ref m_Parks, ref m_Abandoneds))
					{
						if (!flag)
						{
							float serviceCoverage = NetUtils.GetServiceCoverage(m_Coverages[building.m_RoadEdge], CoverageService.Police, building.m_CurvePosition);
							int shelterHomelessCapacity = BuildingUtils.GetShelterHomelessCapacity(prefab, ref m_BuildingDatas, ref m_BuildingProperties);
							if (bufferAccessor[j].Length < shelterHomelessCapacity)
							{
								targetSeeker.FindTargets(entity3, 100f * serviceCoverage + 1000f * (float)bufferAccessor[j].Length / (float)shelterHomelessCapacity + 10000f);
							}
						}
						continue;
					}
					int askingRent = m_PropertiesOnMarket[entity3].m_AskingRent;
					int num = 1;
					if (m_BuildingProperties.HasComponent(prefab))
					{
						num = m_BuildingProperties[prefab].CountProperties();
					}
					if (bufferAccessor[j].Length >= num)
					{
						continue;
					}
					int num2 = m_ServiceFeeParameterData.m_GarbageFeeRCIO.x / num;
					int householdIncome = EconomyUtils.GetHouseholdIncome(bufferData, ref m_Workers, ref m_Citizens, ref m_HealthProblems, ref m_EconomyParameters, m_TaxRates);
					bool flag2 = CitizenUtils.IsHouseholdNeedSupport(bufferData, ref m_Citizens, ref m_Students);
					Entity zonePrefab = m_SpawnableDatas[prefab].m_ZonePrefab;
					if (m_ZonePropertiesDatas.TryGetComponent(zonePrefab, out var componentData))
					{
						float num3 = PropertyUtils.GetZoneDensity(m_ZoneDatas[zonePrefab], componentData) switch
						{
							ZoneDensity.Medium => 0.7f, 
							ZoneDensity.Low => 0.5f, 
							_ => 1f, 
						};
						if (flag2 || !((float)(askingRent + num2) > (float)householdIncome * num3))
						{
							float propertyScore = PropertyUtils.GetPropertyScore(entity3, entity2, bufferData, ref m_PrefabRefs, ref m_BuildingProperties, ref m_Buildings, ref m_BuildingDatas, ref m_Households, ref m_Citizens, ref m_Students, ref m_Workers, ref m_SpawnableDatas, ref m_Crimes, ref m_ServiceCoverages, ref m_Lockeds, ref m_ElectricityConsumers, ref m_WaterConsumers, ref m_GarbageProducers, ref m_MailProducers, ref m_Transforms, ref m_Abandoneds, ref m_Parks, ref m_Availabilities, m_TaxRates, m_PollutionMap, m_AirPollutionMap, m_NoiseMap, m_TelecomCoverages, m_CityModifiers[m_City], m_HealthcareParameters.m_HealthcareServicePrefab, m_ParkParameters.m_ParkServicePrefab, m_EducationParameters.m_EducationServicePrefab, m_TelecomParameters.m_TelecomServicePrefab, m_GarbageParameters.m_GarbageServicePrefab, m_PoliceParameters.m_PoliceServicePrefab, m_CitizenHappinessParameterData, m_GarbageParameters);
							targetSeeker.FindTargets(entity3, 0f - propertyScore + 1000f * (float)bufferAccessor[j].Length / (float)num + (float)random.NextInt(500));
						}
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private EntityQuery m_LeisureProviderQuery;

	private EntityQuery m_TouristTargetQuery;

	private EntityQuery m_SchoolQuery;

	private EntityQuery m_FreeWorkplaceQuery;

	private EntityQuery m_AttractionQuery;

	private EntityQuery m_HomelessShelterQuery;

	private EntityQuery m_FindHomeQuery;

	private GroundPollutionSystem m_GroundPollutionSystem;

	private AirPollutionSystem m_AirPollutionSystem;

	private NoisePollutionSystem m_NoisePollutionSystem;

	private TelecomCoverageSystem m_TelecomCoverageSystem;

	private CitySystem m_CitySystem;

	private ResourceSystem m_ResourceSystem;

	private LeisureSystem m_LeisureSystem;

	private TaxSystem m_TaxSystem;

	private EntityQuery m_EconomyParameterQuery;

	private EntityQuery m_HealthcareParameterQuery;

	private EntityQuery m_ParkParameterQuery;

	private EntityQuery m_EducationParameterQuery;

	private EntityQuery m_TelecomParameterQuery;

	private EntityQuery m_GarbageParameterQuery;

	private EntityQuery m_PoliceParameterQuery;

	private EntityQuery m_CitizenHappinessParameterQuery;

	private EntityQuery m_ServiceFeeParameterQuery;

	private EntityTypeHandle m_EntityType;

	private ComponentTypeHandle<ServiceAvailable> m_ServiceAvailableType;

	private ComponentTypeHandle<FreeWorkplaces> m_FreeWorkplaceType;

	private ComponentTypeHandle<WorkProvider> m_WorkProviderType;

	private ComponentTypeHandle<CityServiceUpkeep> m_CityServiceType;

	private ComponentTypeHandle<Building> m_BuildingType;

	private ComponentTypeHandle<PrefabRef> m_PrefabRefType;

	private BufferTypeHandle<Renter> m_RenterType;

	private BufferTypeHandle<Game.Buildings.Student> m_StudentType;

	private BufferTypeHandle<InstalledUpgrade> m_UpgradeType;

	private ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;

	private ComponentLookup<ServiceCompanyData> m_ServiceDatas;

	private ComponentLookup<Building> m_Buildings;

	private ComponentLookup<Household> m_Households;

	private ComponentLookup<HomelessHousehold> m_HomelessHouseholds;

	private ComponentLookup<Worker> m_Workers;

	private ComponentLookup<Game.Citizens.Student> m_Students;

	private ComponentLookup<Citizen> m_Citizens;

	private ComponentLookup<HealthProblem> m_HealthProblems;

	private ComponentLookup<TouristHousehold> m_TouristHouseholds;

	private BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

	private ComponentLookup<Game.Objects.Transform> m_Transforms;

	private ComponentLookup<Building> m_BuildingDatas;

	private BufferLookup<Efficiency> m_Efficiencies;

	private ComponentLookup<LodgingProvider> m_LodgingProviders;

	private ComponentLookup<AttractivenessProvider> m_AttractivenessProviders;

	private ComponentLookup<PropertyOnMarket> m_PropertiesOnMarket;

	private ComponentLookup<PropertyRenter> m_PropertyRenters;

	private ComponentLookup<CrimeProducer> m_Crimes;

	private ComponentLookup<Game.Buildings.Park> m_Parks;

	private ComponentLookup<Abandoned> m_Abandoneds;

	private ComponentLookup<ElectricityConsumer> m_ElectricityConsumers;

	private ComponentLookup<WaterConsumer> m_WaterConsumers;

	private ComponentLookup<GarbageProducer> m_GarbageProducers;

	private ComponentLookup<MailProducer> m_MailProducers;

	private ComponentLookup<PathInformation> m_PathInfos;

	private ComponentLookup<PrefabRef> m_Prefabs;

	private ComponentLookup<IndustrialProcessData> m_IndustrialProcessDatas;

	private ComponentLookup<LeisureProviderData> m_LeisureProviderDatas;

	private ComponentLookup<ResourceData> m_ResourceDatas;

	private ComponentLookup<SchoolData> m_SchoolDatas;

	private ComponentLookup<BuildingData> m_PrefabBuildingDatas;

	private ComponentLookup<BuildingPropertyData> m_BuildingProperties;

	private ComponentLookup<SpawnableBuildingData> m_SpawnableDatas;

	private ComponentLookup<ZoneData> m_ZoneDatas;

	private ComponentLookup<ZonePropertiesData> m_ZonePropertiesDatas;

	private ComponentLookup<Locked> m_Lockeds;

	private BufferLookup<Game.Economy.Resources> m_Resources;

	private BufferLookup<ServiceDistrict> m_ServiceDistricts;

	private BufferLookup<ResourceAvailability> m_Availabilities;

	private BufferLookup<Game.Net.ServiceCoverage> m_ServiceCoverages;

	private BufferLookup<Renter> m_Renters;

	private BufferLookup<CityModifier> m_CityModifiers;

	private BufferLookup<OwnedVehicle> m_OwnedVehicles;

	public CitizenPathfindSetup(PathfindSetupSystem system)
	{
		m_LeisureProviderQuery = system.GetSetupQuery(ComponentType.ReadOnly<Game.Buildings.LeisureProvider>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Destroyed>());
		m_TouristTargetQuery = system.GetSetupQuery(new EntityQueryDesc
		{
			All = new ComponentType[0],
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<LodgingProvider>(),
				ComponentType.ReadOnly<AttractivenessProvider>()
			},
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_SchoolQuery = system.GetSetupQuery(ComponentType.ReadOnly<Game.Buildings.School>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Game.Buildings.ServiceUpgrade>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Temp>());
		m_FreeWorkplaceQuery = system.GetSetupQuery(ComponentType.ReadOnly<FreeWorkplaces>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Temp>());
		m_AttractionQuery = system.GetSetupQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<AttractivenessProvider>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Temp>());
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
		m_HomelessShelterQuery = system.GetSetupQuery(entityQueryDesc);
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
		m_FindHomeQuery = system.GetSetupQuery(entityQueryDesc, entityQueryDesc2);
		m_GroundPollutionSystem = system.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
		m_AirPollutionSystem = system.World.GetOrCreateSystemManaged<AirPollutionSystem>();
		m_NoisePollutionSystem = system.World.GetOrCreateSystemManaged<NoisePollutionSystem>();
		m_TelecomCoverageSystem = system.World.GetOrCreateSystemManaged<TelecomCoverageSystem>();
		m_CitySystem = system.World.GetOrCreateSystemManaged<CitySystem>();
		m_ResourceSystem = system.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_LeisureSystem = system.World.GetOrCreateSystemManaged<LeisureSystem>();
		m_TaxSystem = system.World.GetOrCreateSystemManaged<TaxSystem>();
		m_EconomyParameterQuery = system.GetSetupQuery(ComponentType.ReadOnly<EconomyParameterData>());
		m_HealthcareParameterQuery = system.GetSetupQuery(ComponentType.ReadOnly<HealthcareParameterData>());
		m_ParkParameterQuery = system.GetSetupQuery(ComponentType.ReadOnly<ParkParameterData>());
		m_EducationParameterQuery = system.GetSetupQuery(ComponentType.ReadOnly<EducationParameterData>());
		m_TelecomParameterQuery = system.GetSetupQuery(ComponentType.ReadOnly<TelecomParameterData>());
		m_GarbageParameterQuery = system.GetSetupQuery(ComponentType.ReadOnly<GarbageParameterData>());
		m_PoliceParameterQuery = system.GetSetupQuery(ComponentType.ReadOnly<PoliceConfigurationData>());
		m_CitizenHappinessParameterQuery = system.GetSetupQuery(ComponentType.ReadOnly<CitizenHappinessParameterData>());
		m_ServiceFeeParameterQuery = system.GetSetupQuery(ComponentType.ReadOnly<ServiceFeeParameterData>());
		m_EntityType = system.GetEntityTypeHandle();
		m_ServiceAvailableType = system.GetComponentTypeHandle<ServiceAvailable>(isReadOnly: true);
		m_FreeWorkplaceType = system.GetComponentTypeHandle<FreeWorkplaces>(isReadOnly: true);
		m_WorkProviderType = system.GetComponentTypeHandle<WorkProvider>(isReadOnly: true);
		m_CityServiceType = system.GetComponentTypeHandle<CityServiceUpkeep>(isReadOnly: true);
		m_BuildingType = system.GetComponentTypeHandle<Building>(isReadOnly: true);
		m_PrefabRefType = system.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
		m_RenterType = system.GetBufferTypeHandle<Renter>(isReadOnly: true);
		m_StudentType = system.GetBufferTypeHandle<Game.Buildings.Student>(isReadOnly: true);
		m_UpgradeType = system.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
		m_Buildings = system.GetComponentLookup<Building>(isReadOnly: true);
		m_Households = system.GetComponentLookup<Household>(isReadOnly: true);
		m_HomelessHouseholds = system.GetComponentLookup<HomelessHousehold>(isReadOnly: true);
		m_OutsideConnections = system.GetComponentLookup<Game.Objects.OutsideConnection>(isReadOnly: true);
		m_ServiceDatas = system.GetComponentLookup<ServiceCompanyData>(isReadOnly: true);
		m_Workers = system.GetComponentLookup<Worker>(isReadOnly: true);
		m_Students = system.GetComponentLookup<Game.Citizens.Student>(isReadOnly: true);
		m_Citizens = system.GetComponentLookup<Citizen>(isReadOnly: true);
		m_TouristHouseholds = system.GetComponentLookup<TouristHousehold>(isReadOnly: true);
		m_HealthProblems = system.GetComponentLookup<HealthProblem>(isReadOnly: true);
		m_Transforms = system.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
		m_BuildingDatas = system.GetComponentLookup<Building>(isReadOnly: true);
		m_Efficiencies = system.GetBufferLookup<Efficiency>(isReadOnly: true);
		m_AttractivenessProviders = system.GetComponentLookup<AttractivenessProvider>(isReadOnly: true);
		m_LodgingProviders = system.GetComponentLookup<LodgingProvider>(isReadOnly: true);
		m_PropertiesOnMarket = system.GetComponentLookup<PropertyOnMarket>(isReadOnly: true);
		m_PropertyRenters = system.GetComponentLookup<PropertyRenter>(isReadOnly: true);
		m_Crimes = system.GetComponentLookup<CrimeProducer>(isReadOnly: true);
		m_Parks = system.GetComponentLookup<Game.Buildings.Park>(isReadOnly: true);
		m_Abandoneds = system.GetComponentLookup<Abandoned>(isReadOnly: true);
		m_ElectricityConsumers = system.GetComponentLookup<ElectricityConsumer>(isReadOnly: true);
		m_WaterConsumers = system.GetComponentLookup<WaterConsumer>(isReadOnly: true);
		m_GarbageProducers = system.GetComponentLookup<GarbageProducer>(isReadOnly: true);
		m_MailProducers = system.GetComponentLookup<MailProducer>(isReadOnly: true);
		m_PathInfos = system.GetComponentLookup<PathInformation>(isReadOnly: true);
		m_Prefabs = system.GetComponentLookup<PrefabRef>(isReadOnly: true);
		m_IndustrialProcessDatas = system.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
		m_LeisureProviderDatas = system.GetComponentLookup<LeisureProviderData>(isReadOnly: true);
		m_ResourceDatas = system.GetComponentLookup<ResourceData>(isReadOnly: true);
		m_SchoolDatas = system.GetComponentLookup<SchoolData>(isReadOnly: true);
		m_PrefabBuildingDatas = system.GetComponentLookup<BuildingData>(isReadOnly: true);
		m_BuildingProperties = system.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
		m_SpawnableDatas = system.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
		m_ZoneDatas = system.GetComponentLookup<ZoneData>(isReadOnly: true);
		m_ZonePropertiesDatas = system.GetComponentLookup<ZonePropertiesData>(isReadOnly: true);
		m_Lockeds = system.GetComponentLookup<Locked>(isReadOnly: true);
		m_Resources = system.GetBufferLookup<Game.Economy.Resources>(isReadOnly: true);
		m_ServiceDistricts = system.GetBufferLookup<ServiceDistrict>(isReadOnly: true);
		m_Availabilities = system.GetBufferLookup<ResourceAvailability>(isReadOnly: true);
		m_ServiceCoverages = system.GetBufferLookup<Game.Net.ServiceCoverage>(isReadOnly: true);
		m_Renters = system.GetBufferLookup<Renter>(isReadOnly: true);
		m_CityModifiers = system.GetBufferLookup<CityModifier>(isReadOnly: true);
		m_OwnedVehicles = system.GetBufferLookup<OwnedVehicle>(isReadOnly: true);
		m_HouseholdCitizens = system.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
	}

	public JobHandle SetupLeisureTarget(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_ServiceAvailableType.Update(system);
		m_PrefabRefType.Update(system);
		m_LeisureProviderDatas.Update(system);
		m_Resources.Update(system);
		m_IndustrialProcessDatas.Update(system);
		m_ResourceDatas.Update(system);
		m_ServiceDatas.Update(system);
		m_BuildingDatas.Update(system);
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new SetupLeisureTargetJob
		{
			m_EntityType = m_EntityType,
			m_ServiceAvailableType = m_ServiceAvailableType,
			m_PrefabType = m_PrefabRefType,
			m_LeisureProviderDatas = m_LeisureProviderDatas,
			m_Resources = m_Resources,
			m_IndustrialProcessDatas = m_IndustrialProcessDatas,
			m_ResourceDatas = m_ResourceDatas,
			m_ServiceDatas = m_ServiceDatas,
			m_BuildingDatas = m_BuildingDatas,
			m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
			m_SetupData = setupData,
			m_LeisureSystemUpdateInterval = m_LeisureSystem.GetUpdateInterval(SystemUpdatePhase.GameSimulation)
		}, m_LeisureProviderQuery, inputDeps);
		m_ResourceSystem.AddPrefabsReader(jobHandle);
		return jobHandle;
	}

	public JobHandle SetupTouristTarget(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_TouristHouseholds.Update(system);
		m_LodgingProviders.Update(system);
		m_PropertyRenters.Update(system);
		m_BuildingDatas.Update(system);
		m_Availabilities.Update(system);
		return JobChunkExtensions.ScheduleParallel(new SetupTouristTargetJob
		{
			m_EntityType = m_EntityType,
			m_LodgingProviders = m_LodgingProviders,
			m_TouristHouseholds = m_TouristHouseholds,
			m_PropertyRenters = m_PropertyRenters,
			m_BuildingDatas = m_BuildingDatas,
			m_ResourceAvailabilityBufs = m_Availabilities,
			m_SetupData = setupData
		}, m_TouristTargetQuery, inputDeps);
	}

	public JobHandle SetupSchoolSeekerTo(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_PrefabRefType.Update(system);
		m_StudentType.Update(system);
		m_UpgradeType.Update(system);
		m_SchoolDatas.Update(system);
		m_Efficiencies.Update(system);
		m_ServiceDistricts.Update(system);
		return JobChunkExtensions.ScheduleParallel(new SetupSchoolSeekerToJob
		{
			m_EntityType = m_EntityType,
			m_PrefabRefType = m_PrefabRefType,
			m_StudentType = m_StudentType,
			m_UpgradeType = m_UpgradeType,
			m_SchoolDatas = m_SchoolDatas,
			m_Efficiencies = m_Efficiencies,
			m_ServiceDistricts = m_ServiceDistricts,
			m_SetupData = setupData
		}, m_SchoolQuery, inputDeps);
	}

	public JobHandle SetupJobSeekerTo(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_FreeWorkplaceType.Update(system);
		m_WorkProviderType.Update(system);
		m_CityServiceType.Update(system);
		m_OutsideConnections.Update(system);
		return JobChunkExtensions.ScheduleParallel(new SetupJobSeekerToJob
		{
			m_EntityType = m_EntityType,
			m_FreeWorkplaceType = m_FreeWorkplaceType,
			m_WorkProviderType = m_WorkProviderType,
			m_CityServiceType = m_CityServiceType,
			m_OutsideConnections = m_OutsideConnections,
			m_SetupData = setupData
		}, m_FreeWorkplaceQuery, inputDeps);
	}

	public JobHandle SetupHomeless(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_RenterType.Update(system);
		m_PrefabRefType.Update(system);
		m_BuildingType.Update(system);
		m_PrefabBuildingDatas.Update(system);
		m_ServiceCoverages.Update(system);
		return JobChunkExtensions.ScheduleParallel(new SetupHomelessJob
		{
			m_EntityType = m_EntityType,
			m_RenterType = m_RenterType,
			m_PrefabType = m_PrefabRefType,
			m_BuildingType = m_BuildingType,
			m_BuildingProperties = m_BuildingProperties,
			m_BuildingDatas = m_PrefabBuildingDatas,
			m_Coverages = m_ServiceCoverages,
			m_SetupData = setupData
		}, m_HomelessShelterQuery, inputDeps);
	}

	public JobHandle SetupFindHome(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_RenterType.Update(system);
		m_PrefabRefType.Update(system);
		m_BuildingType.Update(system);
		m_Buildings.Update(system);
		m_Households.Update(system);
		m_HomelessHouseholds.Update(system);
		m_PrefabBuildingDatas.Update(system);
		m_ServiceCoverages.Update(system);
		m_PropertiesOnMarket.Update(system);
		m_Availabilities.Update(system);
		m_SpawnableDatas.Update(system);
		m_ZoneDatas.Update(system);
		m_ZonePropertiesDatas.Update(system);
		m_BuildingProperties.Update(system);
		m_BuildingDatas.Update(system);
		m_PathInfos.Update(system);
		m_Prefabs.Update(system);
		m_Renters.Update(system);
		m_ServiceCoverages.Update(system);
		m_Workers.Update(system);
		m_Students.Update(system);
		m_PropertyRenters.Update(system);
		m_ResourceDatas.Update(system);
		m_Citizens.Update(system);
		m_Crimes.Update(system);
		m_Lockeds.Update(system);
		m_Transforms.Update(system);
		m_CityModifiers.Update(system);
		m_HealthProblems.Update(system);
		m_HouseholdCitizens.Update(system);
		m_OwnedVehicles.Update(system);
		m_Abandoneds.Update(system);
		m_Parks.Update(system);
		m_ElectricityConsumers.Update(system);
		m_WaterConsumers.Update(system);
		m_GarbageProducers.Update(system);
		m_MailProducers.Update(system);
		m_Resources.Update(system);
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle dependencies3;
		JobHandle dependencies4;
		return JobChunkExtensions.ScheduleParallel(new SetupFindHomeJob
		{
			m_EntityType = m_EntityType,
			m_RenterType = m_RenterType,
			m_PrefabType = m_PrefabRefType,
			m_Buildings = m_Buildings,
			m_Households = m_Households,
			m_HomelessHouseholds = m_HomelessHouseholds,
			m_BuildingDatas = m_PrefabBuildingDatas,
			m_Coverages = m_ServiceCoverages,
			m_PropertiesOnMarket = m_PropertiesOnMarket,
			m_Availabilities = m_Availabilities,
			m_SpawnableDatas = m_SpawnableDatas,
			m_BuildingProperties = m_BuildingProperties,
			m_PrefabRefs = m_Prefabs,
			m_ServiceCoverages = m_ServiceCoverages,
			m_Citizens = m_Citizens,
			m_Crimes = m_Crimes,
			m_Lockeds = m_Lockeds,
			m_Transforms = m_Transforms,
			m_CityModifiers = m_CityModifiers,
			m_HouseholdCitizens = m_HouseholdCitizens,
			m_Abandoneds = m_Abandoneds,
			m_Parks = m_Parks,
			m_ElectricityConsumers = m_ElectricityConsumers,
			m_WaterConsumers = m_WaterConsumers,
			m_GarbageProducers = m_GarbageProducers,
			m_MailProducers = m_MailProducers,
			m_HealthProblems = m_HealthProblems,
			m_Workers = m_Workers,
			m_Students = m_Students,
			m_ResourcesBufs = m_Resources,
			m_ZoneDatas = m_ZoneDatas,
			m_ZonePropertiesDatas = m_ZonePropertiesDatas,
			m_TaxRates = m_TaxSystem.GetTaxRates(),
			m_PollutionMap = m_GroundPollutionSystem.GetMap(readOnly: true, out dependencies),
			m_AirPollutionMap = m_AirPollutionSystem.GetMap(readOnly: true, out dependencies2),
			m_NoiseMap = m_NoisePollutionSystem.GetMap(readOnly: true, out dependencies3),
			m_TelecomCoverages = m_TelecomCoverageSystem.GetData(readOnly: true, out dependencies4),
			m_HealthcareParameters = m_HealthcareParameterQuery.GetSingleton<HealthcareParameterData>(),
			m_ParkParameters = m_ParkParameterQuery.GetSingleton<ParkParameterData>(),
			m_EducationParameters = m_EducationParameterQuery.GetSingleton<EducationParameterData>(),
			m_EconomyParameters = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
			m_TelecomParameters = m_TelecomParameterQuery.GetSingleton<TelecomParameterData>(),
			m_GarbageParameters = m_GarbageParameterQuery.GetSingleton<GarbageParameterData>(),
			m_PoliceParameters = m_PoliceParameterQuery.GetSingleton<PoliceConfigurationData>(),
			m_ServiceFeeParameterData = m_ServiceFeeParameterQuery.GetSingleton<ServiceFeeParameterData>(),
			m_CitizenHappinessParameterData = m_CitizenHappinessParameterQuery.GetSingleton<CitizenHappinessParameterData>(),
			m_City = m_CitySystem.City,
			m_SetupData = setupData
		}, m_FindHomeQuery, JobUtils.CombineDependencies(inputDeps, dependencies, dependencies2, dependencies3, dependencies4));
	}

	public JobHandle SetupAttraction(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_AttractivenessProviders.Update(system);
		return JobChunkExtensions.ScheduleParallel(new SetupAttractionJob
		{
			m_EntityType = m_EntityType,
			m_AttractivenessProviders = m_AttractivenessProviders,
			m_SetupData = setupData
		}, m_AttractionQuery, inputDeps);
	}
}

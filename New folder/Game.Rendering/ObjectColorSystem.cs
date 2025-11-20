using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Creatures;
using Game.Economy;
using Game.Events;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Routes;
using Game.Simulation;
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

namespace Game.Rendering;

[CompilerGenerated]
public class ObjectColorSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateObjectColorsJob : IJobChunk
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_InfomodeChunks;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<InfomodeActive> m_InfomodeActiveType;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewBuildingData> m_InfoviewBuildingType;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewBuildingStatusData> m_InfoviewBuildingStatusType;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewTransportStopData> m_InfoviewTransportStopType;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewVehicleData> m_InfoviewVehicleType;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewObjectStatusData> m_InfoviewObjectStatusType;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewNetStatusData> m_InfoviewNetStatusType;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Building> m_BuildingType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.Hospital> m_HospitalType;

		[ReadOnly]
		public ComponentTypeHandle<ElectricityProducer> m_ElectricityProducerType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.Transformer> m_TransformerType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.WaterPumpingStation> m_WaterPumpingStationType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.WaterTower> m_WaterTowerType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.SewageOutlet> m_SewageOutletType;

		[ReadOnly]
		public ComponentTypeHandle<WastewaterTreatmentPlant> m_WastewaterTreatmentPlantType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.TransportDepot> m_TransportDepotType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.TransportStation> m_TransportStationType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.GarbageFacility> m_GarbageFacilityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.FireStation> m_FireStationType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.PoliceStation> m_PoliceStationType;

		[ReadOnly]
		public ComponentTypeHandle<CrimeProducer> m_CrimeProducerType;

		[ReadOnly]
		public ComponentTypeHandle<RoadMaintenance> m_RoadMaintenanceType;

		[ReadOnly]
		public ComponentTypeHandle<ParkMaintenance> m_ParkMaintenanceType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.PostFacility> m_PostFacilityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.TelecomFacility> m_TelecomFacilityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.School> m_SchoolType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.Park> m_ParkType;

		[ReadOnly]
		public ComponentTypeHandle<AttractivenessProvider> m_AttractivenessProviderType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.EmergencyShelter> m_EmergencyShelterType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.DisasterFacility> m_DisasterFacilityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.FirewatchTower> m_FirewatchTowerType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.DeathcareFacility> m_DeathcareFacilityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.Prison> m_PrisonType;

		[ReadOnly]
		public ComponentTypeHandle<AdminBuilding> m_AdminBuildingType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.WelfareOffice> m_WelfareOfficeType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.ResearchFacility> m_ResearchFacilityType;

		[ReadOnly]
		public ComponentTypeHandle<CarParkingFacility> m_CarParkingFacilityType;

		[ReadOnly]
		public ComponentTypeHandle<BicycleParkingFacility> m_BicycleParkingFacilityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.Battery> m_BatteryType;

		[ReadOnly]
		public ComponentTypeHandle<MailProducer> m_MailProducerType;

		[ReadOnly]
		public ComponentTypeHandle<BuildingCondition> m_BuildingConditionType;

		[ReadOnly]
		public ComponentTypeHandle<ResidentialProperty> m_ResidentialPropertyType;

		[ReadOnly]
		public ComponentTypeHandle<CommercialProperty> m_CommercialPropertyType;

		[ReadOnly]
		public ComponentTypeHandle<IndustrialProperty> m_IndustrialPropertyType;

		[ReadOnly]
		public ComponentTypeHandle<OfficeProperty> m_OfficePropertyType;

		[ReadOnly]
		public ComponentTypeHandle<StorageProperty> m_StoragePropertyType;

		[ReadOnly]
		public ComponentTypeHandle<ExtractorProperty> m_ExtractorPropertyType;

		[ReadOnly]
		public ComponentTypeHandle<GarbageProducer> m_GarbageProducerType;

		[ReadOnly]
		public ComponentTypeHandle<Abandoned> m_AbandonedType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.LeisureProvider> m_LeisureProviderType;

		[ReadOnly]
		public ComponentTypeHandle<ElectricityConsumer> m_ElectricityConsumerType;

		[ReadOnly]
		public ComponentTypeHandle<WaterConsumer> m_WaterConsumerType;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> m_BuildingEfficiencyType;

		[ReadOnly]
		public BufferTypeHandle<Renter> m_RenterType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		[ReadOnly]
		public SharedComponentTypeHandle<CoverageServiceType> m_CoverageServiceType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Routes.TransportStop> m_TransportStopType;

		[ReadOnly]
		public ComponentTypeHandle<BusStop> m_BusStopType;

		[ReadOnly]
		public ComponentTypeHandle<TrainStop> m_TrainStopType;

		[ReadOnly]
		public ComponentTypeHandle<TaxiStand> m_TaxiStandType;

		[ReadOnly]
		public ComponentTypeHandle<TramStop> m_TramStopType;

		[ReadOnly]
		public ComponentTypeHandle<ShipStop> m_ShipStopType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Routes.MailBox> m_MailBoxType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Routes.WorkStop> m_WorkStopType;

		[ReadOnly]
		public ComponentTypeHandle<AirplaneStop> m_AirplaneStopType;

		[ReadOnly]
		public ComponentTypeHandle<SubwayStop> m_SubwayStopType;

		[ReadOnly]
		public ComponentTypeHandle<FerryStop> m_FerryStopType;

		[ReadOnly]
		public ComponentTypeHandle<BicycleParking> m_BicycleParkingType;

		[ReadOnly]
		public ComponentTypeHandle<Vehicle> m_VehicleType;

		[ReadOnly]
		public ComponentTypeHandle<PassengerTransport> m_PassengerTransportType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.CargoTransport> m_CargoTransportType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.Taxi> m_TaxiType;

		[ReadOnly]
		public ComponentTypeHandle<ParkMaintenanceVehicle> m_ParkMaintenanceVehicleType;

		[ReadOnly]
		public ComponentTypeHandle<RoadMaintenanceVehicle> m_RoadMaintenanceVehicleType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.Ambulance> m_AmbulanceType;

		[ReadOnly]
		public ComponentTypeHandle<EvacuatingTransport> m_EvacuatingTransportType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.FireEngine> m_FireEngineType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.GarbageTruck> m_GarbageTruckType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.Hearse> m_HearseType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.PoliceCar> m_PoliceCarType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.PostVan> m_PostVanType;

		[ReadOnly]
		public ComponentTypeHandle<PrisonerTransport> m_PrisonerTransportType;

		[ReadOnly]
		public ComponentTypeHandle<GoodsDeliveryVehicle> m_GoodsDeliveryVehicleType;

		[ReadOnly]
		public BufferTypeHandle<Passenger> m_PassengerType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Creatures.Resident> m_ResidentType;

		[ReadOnly]
		public ComponentTypeHandle<Tree> m_TreeType;

		[ReadOnly]
		public ComponentTypeHandle<Plant> m_PlantType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.UtilityObject> m_UtilityObjectType;

		[ReadOnly]
		public ComponentTypeHandle<Destroyed> m_DestroyedType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Damaged> m_DamagedType;

		[ReadOnly]
		public ComponentTypeHandle<UnderConstruction> m_UnderConstructionType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.UniqueObject> m_UniqueObjectType;

		[ReadOnly]
		public ComponentTypeHandle<Placeholder> m_PlaceholderType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentDistrict> m_CurrentDistrictType;

		[ReadOnly]
		public ComponentTypeHandle<Attached> m_AttachedType;

		[ReadOnly]
		public ComponentTypeHandle<AccidentSite> m_AccidentSiteType;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Park> m_ParkData;

		[ReadOnly]
		public ComponentLookup<Abandoned> m_AbandonedData;

		[ReadOnly]
		public ComponentLookup<TreeData> m_TreeData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_PrefabBuildingData;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> m_BuildingPropertyData;

		[ReadOnly]
		public ComponentLookup<PollutionData> m_PollutionData;

		[ReadOnly]
		public ComponentLookup<PollutionModifierData> m_PollutionModifierData;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingDatas;

		[ReadOnly]
		public ComponentLookup<PlaceholderBuildingData> m_PlaceholderBuildingData;

		[ReadOnly]
		public ComponentLookup<SewageOutletData> m_SewageOutletData;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> m_PlaceableObjectData;

		[ReadOnly]
		public ComponentLookup<Game.Creatures.Resident> m_ResidentData;

		[ReadOnly]
		public ComponentLookup<Household> m_Households;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		[ReadOnly]
		public ComponentLookup<Citizen> m_Citizens;

		[ReadOnly]
		public ComponentLookup<CommercialCompany> m_CommercialCompanies;

		[ReadOnly]
		public ComponentLookup<Profitability> m_Profitabilities;

		[ReadOnly]
		public ComponentLookup<CompanyStatisticData> m_CompanyStatisticData;

		[ReadOnly]
		public BufferLookup<CurrentTrading> m_CurrentTradings;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.LeisureProvider> m_LeisureProviders;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_IndustrialProcessData;

		[ReadOnly]
		public ComponentLookup<ZoneData> m_ZoneDatas;

		[ReadOnly]
		public BufferLookup<Employee> m_Employees;

		[ReadOnly]
		public BufferLookup<Game.Economy.Resources> m_ResourceBuffs;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<UtilityObjectData> m_PrefabUtilityObjectData;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> m_ElectricityConsumerData;

		[ReadOnly]
		public ComponentLookup<WaterConsumer> m_WaterConsumerData;

		[ReadOnly]
		public ComponentLookup<ElectricityNodeConnection> m_ElectricityNodeConnectionData;

		[ReadOnly]
		public ComponentLookup<WaterPipeNodeConnection> m_WaterPipeNodeConnectionData;

		[ReadOnly]
		public ComponentLookup<ElectricityFlowEdge> m_ElectricityFlowEdgeData;

		[ReadOnly]
		public ComponentLookup<WaterPipeEdge> m_WaterPipeEdgeData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ResourceConnection> m_ResourceConnectionData;

		[ReadOnly]
		public BufferLookup<ConnectedFlowEdge> m_ConnectedFlowEdges;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public ComponentLookup<PollutionEmitModifier> m_PollutionEmitModifiers;

		[ReadOnly]
		public EventHelpers.FireHazardData m_FireHazardData;

		[ReadOnly]
		public CellMapData<TelecomCoverage> m_TelecomCoverageData;

		[ReadOnly]
		public EconomyParameterData m_EconomyParameterData;

		public PollutionParameterData m_PollutionParameters;

		public Entity m_City;

		public ComponentTypeHandle<Game.Objects.Color> m_ColorType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Game.Objects.Color> nativeArray = chunk.GetNativeArray(ref m_ColorType);
			bool flag = chunk.Has(ref m_BuildingType);
			if (flag)
			{
				bool flag2 = chunk.Has(ref m_OwnerType);
				if ((!flag2 || chunk.Has(m_CoverageServiceType)) && GetBuildingColor(chunk, out var index))
				{
					for (int i = 0; i < nativeArray.Length; i++)
					{
						nativeArray[i] = new Game.Objects.Color((byte)index, 0);
					}
					CheckColors(nativeArray, chunk, flag);
					return;
				}
				if (!flag2 && GetBuildingStatusType(chunk, out var statusData, out var activeData))
				{
					GetBuildingStatusColors(nativeArray, chunk, statusData, activeData);
					CheckColors(nativeArray, chunk, flag);
					return;
				}
			}
			int index3;
			if (chunk.Has(ref m_TransportStopType) && GetTransportStopColor(chunk, out var index2))
			{
				for (int j = 0; j < nativeArray.Length; j++)
				{
					nativeArray[j] = new Game.Objects.Color((byte)index2, 0);
				}
			}
			else if (chunk.Has(ref m_VehicleType) && GetVehicleColor(chunk, out index3))
			{
				for (int k = 0; k < nativeArray.Length; k++)
				{
					nativeArray[k] = new Game.Objects.Color((byte)index3, 0);
				}
			}
			else
			{
				if (chunk.Has(ref m_UtilityObjectType) && GetNetStatusColors(nativeArray, chunk))
				{
					return;
				}
				if (GetObjectStatusType(chunk, out var statusData2, out var activeData2))
				{
					GetObjectStatusColors(nativeArray, chunk, statusData2, activeData2);
					CheckColors(nativeArray, chunk, flag);
					return;
				}
				for (int l = 0; l < nativeArray.Length; l++)
				{
					nativeArray[l] = default(Game.Objects.Color);
				}
				CheckColors(nativeArray, chunk, flag);
			}
		}

		private void CheckColors(NativeArray<Game.Objects.Color> colors, ArchetypeChunk chunk, bool isBuilding)
		{
			if (!isBuilding)
			{
				return;
			}
			NativeArray<Destroyed> nativeArray = chunk.GetNativeArray(ref m_DestroyedType);
			NativeArray<UnderConstruction> nativeArray2 = chunk.GetNativeArray(ref m_UnderConstructionType);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
			for (int i = 0; i < nativeArray3.Length; i++)
			{
				PrefabRef prefabRef = nativeArray3[i];
				if ((m_PrefabBuildingData[prefabRef.m_Prefab].m_Flags & Game.Prefabs.BuildingFlags.ColorizeLot) != 0 || (CollectionUtils.TryGet(nativeArray, i, out var value) && value.m_Cleared >= 0f) || (CollectionUtils.TryGet(nativeArray2, i, out var value2) && value2.m_NewPrefab == Entity.Null))
				{
					Game.Objects.Color value3 = colors[i];
					value3.m_SubColor = true;
					colors[i] = value3;
				}
			}
		}

		private bool GetBuildingColor(ArchetypeChunk chunk, out int index)
		{
			index = 0;
			int num = int.MaxValue;
			for (int i = 0; i < m_InfomodeChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_InfomodeChunks[i];
				NativeArray<InfoviewBuildingData> nativeArray = archetypeChunk.GetNativeArray(ref m_InfoviewBuildingType);
				if (nativeArray.Length == 0)
				{
					continue;
				}
				NativeArray<InfomodeActive> nativeArray2 = archetypeChunk.GetNativeArray(ref m_InfomodeActiveType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					InfomodeActive infomodeActive = nativeArray2[j];
					int priority = infomodeActive.m_Priority;
					if (priority < num && HasBuildingColor(nativeArray[j], chunk))
					{
						index = infomodeActive.m_Index;
						num = priority;
					}
				}
			}
			return num != int.MaxValue;
		}

		private bool HasBuildingColor(InfoviewBuildingData infoviewBuildingData, ArchetypeChunk chunk)
		{
			switch (infoviewBuildingData.m_Type)
			{
			case BuildingType.Hospital:
				return chunk.Has(ref m_HospitalType);
			case BuildingType.PowerPlant:
				return chunk.Has(ref m_ElectricityProducerType);
			case BuildingType.Transformer:
				return chunk.Has(ref m_TransformerType);
			case BuildingType.FreshWaterBuilding:
				if (!chunk.Has(ref m_WaterPumpingStationType))
				{
					return chunk.Has(ref m_WaterTowerType);
				}
				return true;
			case BuildingType.SewageBuilding:
				if (!chunk.Has(ref m_SewageOutletType))
				{
					return chunk.Has(ref m_WastewaterTreatmentPlantType);
				}
				return true;
			case BuildingType.TransportDepot:
				return chunk.Has(ref m_TransportDepotType);
			case BuildingType.TransportStation:
				return chunk.Has(ref m_TransportStationType);
			case BuildingType.GarbageFacility:
				return chunk.Has(ref m_GarbageFacilityType);
			case BuildingType.FireStation:
				return chunk.Has(ref m_FireStationType);
			case BuildingType.PoliceStation:
				return chunk.Has(ref m_PoliceStationType);
			case BuildingType.RoadMaintenanceDepot:
				return chunk.Has(ref m_RoadMaintenanceType);
			case BuildingType.PostFacility:
				return chunk.Has(ref m_PostFacilityType);
			case BuildingType.TelecomFacility:
				return chunk.Has(ref m_TelecomFacilityType);
			case BuildingType.School:
				return chunk.Has(ref m_SchoolType);
			case BuildingType.Park:
				if (!chunk.Has(ref m_ParkType))
				{
					return chunk.Has(ref m_AttractivenessProviderType);
				}
				return true;
			case BuildingType.EmergencyShelter:
				return chunk.Has(ref m_EmergencyShelterType);
			case BuildingType.DisasterFacility:
				return chunk.Has(ref m_DisasterFacilityType);
			case BuildingType.FirewatchTower:
				return chunk.Has(ref m_FirewatchTowerType);
			case BuildingType.DeathcareFacility:
				return chunk.Has(ref m_DeathcareFacilityType);
			case BuildingType.Prison:
				return chunk.Has(ref m_PrisonType);
			case BuildingType.AdminBuilding:
				return chunk.Has(ref m_AdminBuildingType);
			case BuildingType.WelfareOffice:
				return chunk.Has(ref m_WelfareOfficeType);
			case BuildingType.ResearchFacility:
				return chunk.Has(ref m_ResearchFacilityType);
			case BuildingType.ParkMaintenanceDepot:
				return chunk.Has(ref m_ParkMaintenanceType);
			case BuildingType.CarParkingFacility:
				return chunk.Has(ref m_CarParkingFacilityType);
			case BuildingType.BicycleParkingFacility:
				return chunk.Has(ref m_BicycleParkingFacilityType);
			case BuildingType.Battery:
				return chunk.Has(ref m_BatteryType);
			case BuildingType.ResidentialBuilding:
				return chunk.Has(ref m_ResidentialPropertyType);
			case BuildingType.CommercialBuilding:
				return chunk.Has(ref m_CommercialPropertyType);
			case BuildingType.IndustrialBuilding:
				if (chunk.Has(ref m_IndustrialPropertyType))
				{
					return !chunk.Has(ref m_OfficePropertyType);
				}
				return false;
			case BuildingType.OfficeBuilding:
				return chunk.Has(ref m_OfficePropertyType);
			case BuildingType.SignatureResidential:
				if (chunk.Has(ref m_ResidentialPropertyType))
				{
					return chunk.Has(ref m_UniqueObjectType);
				}
				return false;
			case BuildingType.ExtractorBuilding:
				return chunk.Has(ref m_ExtractorPropertyType);
			case BuildingType.SignatureCommercial:
				if (chunk.Has(ref m_CommercialPropertyType))
				{
					return chunk.Has(ref m_UniqueObjectType);
				}
				return false;
			case BuildingType.SignatureIndustrial:
				if (chunk.Has(ref m_IndustrialPropertyType) && chunk.Has(ref m_UniqueObjectType))
				{
					return !chunk.Has(ref m_OfficePropertyType);
				}
				return false;
			case BuildingType.SignatureOffice:
				if (chunk.Has(ref m_OfficePropertyType))
				{
					return chunk.Has(ref m_UniqueObjectType);
				}
				return false;
			case BuildingType.LandValueSources:
				if (!chunk.Has(ref m_CommercialPropertyType) && !chunk.Has(ref m_SchoolType) && !chunk.Has(ref m_HospitalType))
				{
					return chunk.Has(ref m_AttractivenessProviderType);
				}
				return true;
			default:
				return false;
			}
		}

		private bool GetBuildingStatusType(ArchetypeChunk chunk, out InfoviewBuildingStatusData statusData, out InfomodeActive activeData)
		{
			statusData = default(InfoviewBuildingStatusData);
			activeData = default(InfomodeActive);
			int num = int.MaxValue;
			for (int i = 0; i < m_InfomodeChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_InfomodeChunks[i];
				NativeArray<InfoviewBuildingStatusData> nativeArray = archetypeChunk.GetNativeArray(ref m_InfoviewBuildingStatusType);
				if (nativeArray.Length == 0)
				{
					continue;
				}
				NativeArray<InfomodeActive> nativeArray2 = archetypeChunk.GetNativeArray(ref m_InfomodeActiveType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					InfomodeActive infomodeActive = nativeArray2[j];
					int priority = infomodeActive.m_Priority;
					if (priority < num)
					{
						InfoviewBuildingStatusData infoviewBuildingStatusData = nativeArray[j];
						if (HasBuildingStatus(nativeArray[j], chunk))
						{
							statusData = infoviewBuildingStatusData;
							activeData = infomodeActive;
							num = priority;
						}
					}
				}
			}
			return num != int.MaxValue;
		}

		private bool HasBuildingStatus(InfoviewBuildingStatusData infoviewBuildingStatusData, ArchetypeChunk chunk)
		{
			switch (infoviewBuildingStatusData.m_Type)
			{
			case BuildingStatusType.CrimeProbability:
				return chunk.Has(ref m_CrimeProducerType);
			case BuildingStatusType.MailAccumulation:
				return chunk.Has(ref m_MailProducerType);
			case BuildingStatusType.Wealth:
			case BuildingStatusType.Education:
			case BuildingStatusType.Health:
			case BuildingStatusType.Age:
			case BuildingStatusType.Happiness:
			case BuildingStatusType.Wellbeing:
				if (chunk.Has(ref m_ResidentialPropertyType) && !chunk.Has(ref m_AbandonedType))
				{
					return !chunk.Has(ref m_ParkType);
				}
				return false;
			case BuildingStatusType.Level:
				if (chunk.Has(ref m_BuildingConditionType) && !chunk.Has(ref m_AbandonedType) && !chunk.Has(ref m_DestroyedType))
				{
					return !chunk.Has(ref m_UniqueObjectType);
				}
				return false;
			case BuildingStatusType.GarbageAccumulation:
				return chunk.Has(ref m_GarbageProducerType);
			case BuildingStatusType.Profitability:
				if (chunk.Has(ref m_CommercialPropertyType) || chunk.Has(ref m_IndustrialPropertyType))
				{
					return !chunk.Has(ref m_StoragePropertyType);
				}
				return false;
			case BuildingStatusType.LeisureProvider:
				if (!chunk.Has(ref m_LeisureProviderType))
				{
					if (chunk.Has(ref m_RenterType))
					{
						return chunk.Has(ref m_CommercialPropertyType);
					}
					return false;
				}
				return true;
			case BuildingStatusType.ElectricityConsumption:
				return chunk.Has(ref m_ElectricityConsumerType);
			case BuildingStatusType.NetworkQuality:
				return true;
			case BuildingStatusType.AirPollutionSource:
			case BuildingStatusType.GroundPollutionSource:
			case BuildingStatusType.NoisePollutionSource:
				return true;
			case BuildingStatusType.LodgingProvider:
				return chunk.Has(ref m_CommercialPropertyType);
			case BuildingStatusType.WaterPollutionSource:
				return chunk.Has(ref m_SewageOutletType);
			case BuildingStatusType.LandValue:
				return chunk.Has(ref m_RenterType);
			case BuildingStatusType.WaterConsumption:
				return chunk.Has(ref m_WaterConsumerType);
			case BuildingStatusType.ResidentialBuilding:
				return chunk.Has(ref m_ResidentialPropertyType);
			case BuildingStatusType.CommercialBuilding:
				return chunk.Has(ref m_CommercialPropertyType);
			case BuildingStatusType.IndustrialBuilding:
				if (chunk.Has(ref m_IndustrialPropertyType))
				{
					return !chunk.Has(ref m_OfficePropertyType);
				}
				return false;
			case BuildingStatusType.OfficeBuilding:
				return chunk.Has(ref m_OfficePropertyType);
			case BuildingStatusType.SignatureResidential:
				if (chunk.Has(ref m_ResidentialPropertyType))
				{
					return chunk.Has(ref m_UniqueObjectType);
				}
				return false;
			case BuildingStatusType.SignatureCommercial:
				if (chunk.Has(ref m_CommercialPropertyType))
				{
					return chunk.Has(ref m_UniqueObjectType);
				}
				return false;
			case BuildingStatusType.SignatureIndustrial:
				if (chunk.Has(ref m_IndustrialPropertyType) && chunk.Has(ref m_UniqueObjectType))
				{
					return !chunk.Has(ref m_OfficePropertyType);
				}
				return false;
			case BuildingStatusType.SignatureOffice:
				if (chunk.Has(ref m_OfficePropertyType))
				{
					return chunk.Has(ref m_UniqueObjectType);
				}
				return false;
			case BuildingStatusType.HomelessCount:
				if (!chunk.Has(ref m_AbandonedType))
				{
					return chunk.Has(ref m_ParkType);
				}
				return true;
			case BuildingStatusType.OutsideTrading:
				if (!chunk.Has(ref m_CommercialPropertyType) && !chunk.Has(ref m_IndustrialPropertyType))
				{
					return chunk.Has(ref m_OfficePropertyType);
				}
				return true;
			default:
				return false;
			}
		}

		private void GetBuildingStatusColors(NativeArray<Game.Objects.Color> results, ArchetypeChunk chunk, InfoviewBuildingStatusData statusData, InfomodeActive activeData)
		{
			switch (statusData.m_Type)
			{
			case BuildingStatusType.CrimeProbability:
			{
				NativeArray<CrimeProducer> nativeArray9 = chunk.GetNativeArray(ref m_CrimeProducerType);
				NativeArray<AccidentSite> nativeArray10 = chunk.GetNativeArray(ref m_AccidentSiteType);
				for (int num16 = 0; num16 < nativeArray9.Length; num16++)
				{
					float num17 = nativeArray9[num16].m_Crime;
					if (CollectionUtils.TryGet(nativeArray10, num16, out var value) && (value.m_Flags & AccidentSiteFlags.CrimeScene) != 0)
					{
						num17 *= 3.3333333f;
					}
					results[num16] = new Game.Objects.Color((byte)activeData.m_Index, (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, num17) * 255f), 0, 255));
				}
				break;
			}
			case BuildingStatusType.MailAccumulation:
			{
				NativeArray<MailProducer> nativeArray2 = chunk.GetNativeArray(ref m_MailProducerType);
				for (int n = 0; n < nativeArray2.Length; n++)
				{
					MailProducer mailProducer = nativeArray2[n];
					float status = math.max(mailProducer.m_SendingMail, mailProducer.receivingMail);
					results[n] = new Game.Objects.Color((byte)activeData.m_Index, (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, status) * 255f), 0, 255));
				}
				break;
			}
			case BuildingStatusType.Wealth:
			{
				BufferAccessor<Renter> bufferAccessor8 = chunk.GetBufferAccessor(ref m_RenterType);
				for (int num13 = 0; num13 < bufferAccessor8.Length; num13++)
				{
					float2 @float = default(float2);
					DynamicBuffer<Renter> dynamicBuffer7 = bufferAccessor8[num13];
					for (int num14 = 0; num14 < dynamicBuffer7.Length; num14++)
					{
						Entity renter2 = dynamicBuffer7[num14].m_Renter;
						if (m_Households.HasComponent(renter2) && m_HouseholdCitizens.HasBuffer(renter2) && m_ResourceBuffs.HasBuffer(renter2))
						{
							int householdTotalWealth = EconomyUtils.GetHouseholdTotalWealth(m_Households[renter2], m_ResourceBuffs[renter2]);
							@float.x += householdTotalWealth;
							@float.y += 1f;
						}
					}
					if (@float.y > 0f)
					{
						results[num13] = new Game.Objects.Color((byte)activeData.m_Index, (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, @float.x / @float.y) * 255f), 0, 255));
					}
					else
					{
						results[num13] = default(Game.Objects.Color);
					}
				}
				break;
			}
			case BuildingStatusType.Education:
			case BuildingStatusType.Health:
			case BuildingStatusType.Age:
			case BuildingStatusType.Happiness:
			case BuildingStatusType.Wellbeing:
			{
				BufferAccessor<Renter> bufferAccessor10 = chunk.GetBufferAccessor(ref m_RenterType);
				for (int num21 = 0; num21 < bufferAccessor10.Length; num21++)
				{
					DynamicBuffer<Renter> dynamicBuffer9 = bufferAccessor10[num21];
					float2 float2 = default(float2);
					for (int num22 = 0; num22 < dynamicBuffer9.Length; num22++)
					{
						Entity renter3 = dynamicBuffer9[num22].m_Renter;
						if (!m_HouseholdCitizens.HasBuffer(renter3))
						{
							continue;
						}
						DynamicBuffer<HouseholdCitizen> dynamicBuffer10 = m_HouseholdCitizens[renter3];
						for (int num23 = 0; num23 < dynamicBuffer10.Length; num23++)
						{
							Entity citizen = dynamicBuffer10[num23].m_Citizen;
							if (!m_Citizens.HasComponent(citizen))
							{
								continue;
							}
							Citizen citizen2 = m_Citizens[citizen];
							CitizenAge age = citizen2.GetAge();
							switch (statusData.m_Type)
							{
							case BuildingStatusType.Education:
								if (age == CitizenAge.Adult)
								{
									float2 += new float2(citizen2.GetEducationLevel(), 1f);
								}
								break;
							case BuildingStatusType.Health:
								float2 += new float2((int)citizen2.m_Health, 1f);
								break;
							case BuildingStatusType.Wellbeing:
								float2 += new float2((int)citizen2.m_WellBeing, 1f);
								break;
							case BuildingStatusType.Happiness:
								float2 += new float2(citizen2.Happiness, 1f);
								break;
							case BuildingStatusType.Age:
								float2 += new float2((float)age, 1f);
								break;
							}
						}
					}
					if (float2.y > 0f)
					{
						results[num21] = new Game.Objects.Color((byte)activeData.m_Index, (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, float2.x / float2.y) * 255f), 0, 255));
					}
					else
					{
						results[num21] = default(Game.Objects.Color);
					}
				}
				break;
			}
			case BuildingStatusType.Level:
			{
				NativeArray<PrefabRef> nativeArray12 = chunk.GetNativeArray(ref m_PrefabRefType);
				for (int num19 = 0; num19 < nativeArray12.Length; num19++)
				{
					Entity prefab2 = nativeArray12[num19].m_Prefab;
					if (m_SpawnableBuildingDatas.HasComponent(prefab2))
					{
						float status6 = (int)m_SpawnableBuildingDatas[prefab2].m_Level;
						results[num19] = new Game.Objects.Color((byte)activeData.m_Index, (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, status6) * 255f), 0, 255));
					}
					else
					{
						results[num19] = default(Game.Objects.Color);
					}
				}
				break;
			}
			case BuildingStatusType.GarbageAccumulation:
			{
				NativeArray<GarbageProducer> nativeArray6 = chunk.GetNativeArray(ref m_GarbageProducerType);
				for (int num6 = 0; num6 < nativeArray6.Length; num6++)
				{
					results[num6] = new Game.Objects.Color((byte)activeData.m_Index, (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, nativeArray6[num6].m_Garbage) * 255f), 0, 255));
				}
				break;
			}
			case BuildingStatusType.Profitability:
			{
				BufferAccessor<Renter> bufferAccessor2 = chunk.GetBufferAccessor(ref m_RenterType);
				for (int k = 0; k < bufferAccessor2.Length; k++)
				{
					DynamicBuffer<Renter> dynamicBuffer2 = bufferAccessor2[k];
					bool flag2 = false;
					for (int l = 0; l < dynamicBuffer2.Length; l++)
					{
						Entity renter = dynamicBuffer2[l].m_Renter;
						if (m_CompanyStatisticData.HasComponent(renter))
						{
							int companyProfitability = CompanyUtils.GetCompanyProfitability(m_CompanyStatisticData[renter].m_Profit, m_EconomyParameterData);
							results[k] = new Game.Objects.Color((byte)activeData.m_Index, (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, companyProfitability) * 255f), 0, 255));
							flag2 = true;
							break;
						}
					}
					if (!flag2)
					{
						results[k] = default(Game.Objects.Color);
					}
				}
				break;
			}
			case BuildingStatusType.LeisureProvider:
				if (chunk.Has(ref m_RenterType))
				{
					BufferAccessor<Renter> bufferAccessor6 = chunk.GetBufferAccessor(ref m_RenterType);
					for (int num10 = 0; num10 < bufferAccessor6.Length; num10++)
					{
						DynamicBuffer<Renter> dynamicBuffer5 = bufferAccessor6[num10];
						bool flag3 = false;
						foreach (Renter item in dynamicBuffer5)
						{
							if (m_LeisureProviders.HasComponent(item.m_Renter))
							{
								results[num10] = new Game.Objects.Color((byte)activeData.m_Index, byte.MaxValue);
								flag3 = true;
							}
						}
						if (!flag3)
						{
							results[num10] = default(Game.Objects.Color);
						}
					}
				}
				else
				{
					for (int num11 = 0; num11 < chunk.Count; num11++)
					{
						results[num11] = new Game.Objects.Color((byte)activeData.m_Index, byte.MaxValue);
					}
				}
				break;
			case BuildingStatusType.ElectricityConsumption:
			{
				NativeArray<ElectricityConsumer> nativeArray5 = chunk.GetNativeArray(ref m_ElectricityConsumerType);
				for (int num2 = 0; num2 < nativeArray5.Length; num2++)
				{
					ElectricityConsumer electricityConsumer = nativeArray5[num2];
					if (electricityConsumer.m_WantedConsumption > 0)
					{
						if (electricityConsumer.m_CooldownCounter >= DispatchElectricitySystem.kAlertCooldown)
						{
							results[num2] = new Game.Objects.Color((byte)activeData.m_Index, 0);
							continue;
						}
						float status3 = math.log10(math.max(electricityConsumer.m_WantedConsumption, 1)) / math.log10(20000f);
						results[num2] = new Game.Objects.Color((byte)activeData.m_Index, (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, status3) * 255f), 0, 255));
					}
					else
					{
						results[num2] = default(Game.Objects.Color);
					}
				}
				break;
			}
			case BuildingStatusType.NetworkQuality:
			{
				NativeArray<Game.Objects.Transform> nativeArray3 = chunk.GetNativeArray(ref m_TransformType);
				NativeArray<PrefabRef> nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);
				for (int num = 0; num < nativeArray3.Length; num++)
				{
					Game.Objects.Transform transform = nativeArray3[num];
					PrefabRef prefabRef = nativeArray4[num];
					if ((m_PrefabBuildingData[prefabRef.m_Prefab].m_Flags & Game.Prefabs.BuildingFlags.RequireRoad) != 0)
					{
						float status2 = TelecomCoverage.SampleNetworkQuality(m_TelecomCoverageData, transform.m_Position);
						results[num] = new Game.Objects.Color((byte)activeData.m_Index, (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, status2) * 255f), 0, 255));
					}
					else
					{
						results[num] = default(Game.Objects.Color);
					}
				}
				break;
			}
			case BuildingStatusType.AirPollutionSource:
			case BuildingStatusType.GroundPollutionSource:
			case BuildingStatusType.NoisePollutionSource:
			{
				NativeArray<PrefabRef> nativeArray13 = chunk.GetNativeArray(ref m_PrefabRefType);
				bool destroyed = chunk.Has(ref m_DestroyedType);
				bool abandoned = chunk.Has(ref m_AbandonedType);
				bool isPark = chunk.Has(ref m_ParkType);
				BufferAccessor<Efficiency> bufferAccessor11 = chunk.GetBufferAccessor(ref m_BuildingEfficiencyType);
				BufferAccessor<Renter> bufferAccessor12 = chunk.GetBufferAccessor(ref m_RenterType);
				BufferAccessor<InstalledUpgrade> bufferAccessor13 = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
				m_CityModifiers.TryGetBuffer(m_City, out var bufferData2);
				for (int num24 = 0; num24 < nativeArray13.Length; num24++)
				{
					Entity prefab3 = nativeArray13[num24].m_Prefab;
					float efficiency = BuildingUtils.GetEfficiency(bufferAccessor11, num24);
					DynamicBuffer<Renter> renters = ((bufferAccessor12.Length != 0) ? bufferAccessor12[num24] : default(DynamicBuffer<Renter>));
					DynamicBuffer<InstalledUpgrade> installedUpgrades = ((bufferAccessor13.Length != 0) ? bufferAccessor13[num24] : default(DynamicBuffer<InstalledUpgrade>));
					float value2 = BuildingPollutionAddSystem.GetBuildingPollution(prefab3, destroyed, abandoned, isPark, efficiency, renters, installedUpgrades, m_PollutionParameters, bufferData2, ref m_Prefabs, ref m_PrefabBuildingData, ref m_SpawnableBuildingDatas, ref m_PollutionData, ref m_PollutionModifierData, ref m_ZoneDatas, ref m_Employees, ref m_HouseholdCitizens, ref m_Citizens, ref m_PollutionEmitModifiers).GetValue(statusData.m_Type);
					if (value2 > 0f)
					{
						results[num24] = new Game.Objects.Color((byte)activeData.m_Index, (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, value2) * 255f), 0, 255));
					}
					else
					{
						results[num24] = default(Game.Objects.Color);
					}
				}
				break;
			}
			case BuildingStatusType.LodgingProvider:
			{
				BufferAccessor<Renter> bufferAccessor9 = chunk.GetBufferAccessor(ref m_RenterType);
				for (int num20 = 0; num20 < bufferAccessor9.Length; num20++)
				{
					DynamicBuffer<Renter> dynamicBuffer8 = bufferAccessor9[num20];
					bool flag4 = false;
					foreach (Renter item2 in dynamicBuffer8)
					{
						if (m_Prefabs.TryGetComponent(item2.m_Renter, out var componentData3) && m_IndustrialProcessData.TryGetComponent(componentData3.m_Prefab, out var componentData4) && (componentData4.m_Output.m_Resource & Resource.Lodging) != Resource.NoResource)
						{
							results[num20] = new Game.Objects.Color((byte)activeData.m_Index, byte.MaxValue);
							flag4 = true;
						}
					}
					if (!flag4)
					{
						results[num20] = default(Game.Objects.Color);
					}
				}
				break;
			}
			case BuildingStatusType.WaterPollutionSource:
			{
				NativeArray<PrefabRef> nativeArray11 = chunk.GetNativeArray(ref m_PrefabRefType);
				for (int num18 = 0; num18 < nativeArray11.Length; num18++)
				{
					Entity prefab = nativeArray11[num18].m_Prefab;
					if (m_SewageOutletData.TryGetComponent(prefab, out var componentData2))
					{
						float status5 = 1f - componentData2.m_Purification;
						results[num18] = new Game.Objects.Color((byte)activeData.m_Index, (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, status5) * 255f), 0, 255));
					}
					else
					{
						results[num18] = default(Game.Objects.Color);
					}
				}
				break;
			}
			case BuildingStatusType.WaterConsumption:
			{
				NativeArray<WaterConsumer> nativeArray8 = chunk.GetNativeArray(ref m_WaterConsumerType);
				for (int num15 = 0; num15 < nativeArray8.Length; num15++)
				{
					WaterConsumer waterConsumer = nativeArray8[num15];
					if (waterConsumer.m_WantedConsumption > 0)
					{
						if (waterConsumer.m_FreshCooldownCounter >= DispatchWaterSystem.kAlertCooldown || waterConsumer.m_SewageCooldownCounter >= DispatchWaterSystem.kAlertCooldown)
						{
							results[num15] = new Game.Objects.Color((byte)activeData.m_Index, 0);
							continue;
						}
						float status4 = math.log10(math.max(waterConsumer.m_WantedConsumption, 1)) / math.log10(20000f);
						results[num15] = new Game.Objects.Color((byte)activeData.m_Index, (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, status4) * 255f), 0, 255));
					}
					else
					{
						results[num15] = default(Game.Objects.Color);
					}
				}
				break;
			}
			case BuildingStatusType.IndustrialBuilding:
			case BuildingStatusType.OfficeBuilding:
			case BuildingStatusType.SignatureIndustrial:
			case BuildingStatusType.SignatureOffice:
			{
				BufferAccessor<Renter> bufferAccessor7 = chunk.GetBufferAccessor(ref m_RenterType);
				chunk.GetNativeArray(ref m_PrefabRefType);
				for (int num12 = 0; num12 < bufferAccessor7.Length; num12++)
				{
					DynamicBuffer<Renter> dynamicBuffer6 = bufferAccessor7[num12];
					results[num12] = new Game.Objects.Color((byte)activeData.m_Index, (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, dynamicBuffer6.Length) * 255f), 0, 255));
				}
				break;
			}
			case BuildingStatusType.ResidentialBuilding:
			case BuildingStatusType.SignatureResidential:
			{
				BufferAccessor<Renter> bufferAccessor5 = chunk.GetBufferAccessor(ref m_RenterType);
				NativeArray<PrefabRef> nativeArray7 = chunk.GetNativeArray(ref m_PrefabRefType);
				Bounds1 range = default(Bounds1);
				for (int num7 = 0; num7 < bufferAccessor5.Length; num7++)
				{
					DynamicBuffer<Renter> dynamicBuffer4 = bufferAccessor5[num7];
					PrefabRef prefabRef2 = nativeArray7[num7];
					float max = statusData.m_Range.max;
					if (chunk.Has(ref m_ResidentialPropertyType) && m_BuildingPropertyData.TryGetComponent(prefabRef2.m_Prefab, out var componentData))
					{
						max = componentData.m_ResidentialProperties;
					}
					int num8 = 0;
					for (int num9 = 0; num9 < dynamicBuffer4.Length; num9++)
					{
						if (m_Households.HasComponent(dynamicBuffer4[num9].m_Renter))
						{
							num8++;
						}
					}
					range.min = statusData.m_Range.min;
					range.max = max;
					statusData.m_Range = range;
					results[num7] = new Game.Objects.Color((byte)activeData.m_Index, (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, num8) * 255f), 0, 255));
				}
				break;
			}
			case BuildingStatusType.CommercialBuilding:
			case BuildingStatusType.SignatureCommercial:
			{
				BufferAccessor<Renter> bufferAccessor4 = chunk.GetBufferAccessor(ref m_RenterType);
				for (int num3 = 0; num3 < bufferAccessor4.Length; num3++)
				{
					DynamicBuffer<Renter> dynamicBuffer3 = bufferAccessor4[num3];
					int num4 = 0;
					for (int num5 = 0; num5 < dynamicBuffer3.Length; num5++)
					{
						if (m_CommercialCompanies.HasComponent(dynamicBuffer3[num5].m_Renter))
						{
							num4++;
						}
					}
					results[num3] = new Game.Objects.Color((byte)activeData.m_Index, (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, num4) * 255f), 0, 255));
				}
				break;
			}
			case BuildingStatusType.HomelessCount:
			{
				if (!chunk.Has(ref m_ParkType) && !chunk.Has(ref m_AbandonedType))
				{
					break;
				}
				NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
				BufferAccessor<Renter> bufferAccessor3 = chunk.GetBufferAccessor(ref m_RenterType);
				for (int m = 0; m < nativeArray.Length; m++)
				{
					if (BuildingUtils.IsHomelessShelterBuilding(nativeArray[m], ref m_ParkData, ref m_AbandonedData) && bufferAccessor3[m].Length > 0)
					{
						results[m] = new Game.Objects.Color((byte)activeData.m_Index, (byte)Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, 1f) * 255f));
					}
					else
					{
						results[m] = default(Game.Objects.Color);
					}
				}
				break;
			}
			case BuildingStatusType.OutsideTrading:
			{
				BufferAccessor<Renter> bufferAccessor = chunk.GetBufferAccessor(ref m_RenterType);
				for (int i = 0; i < bufferAccessor.Length; i++)
				{
					DynamicBuffer<Renter> dynamicBuffer = bufferAccessor[i];
					bool flag = false;
					foreach (Renter item3 in dynamicBuffer)
					{
						if (!m_CurrentTradings.TryGetBuffer(item3.m_Renter, out var bufferData))
						{
							continue;
						}
						for (int j = 0; j < bufferData.Length; j++)
						{
							if (bufferData[j].m_OutsideConnectionType != OutsideConnectionTransferType.None)
							{
								results[i] = new Game.Objects.Color((byte)activeData.m_Index, byte.MaxValue);
								flag = true;
							}
						}
					}
					if (!flag)
					{
						results[i] = default(Game.Objects.Color);
					}
				}
				break;
			}
			case BuildingStatusType.LandValue:
				break;
			}
		}

		private bool GetTransportStopColor(ArchetypeChunk chunk, out int index)
		{
			index = 0;
			int num = int.MaxValue;
			for (int i = 0; i < m_InfomodeChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_InfomodeChunks[i];
				NativeArray<InfoviewTransportStopData> nativeArray = archetypeChunk.GetNativeArray(ref m_InfoviewTransportStopType);
				if (nativeArray.Length == 0)
				{
					continue;
				}
				NativeArray<InfomodeActive> nativeArray2 = archetypeChunk.GetNativeArray(ref m_InfomodeActiveType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					InfomodeActive infomodeActive = nativeArray2[j];
					int priority = infomodeActive.m_Priority;
					if (priority < num && HasTransportStopColor(nativeArray[j], chunk))
					{
						index = infomodeActive.m_Index;
						num = priority;
					}
				}
			}
			return num != int.MaxValue;
		}

		private bool HasTransportStopColor(InfoviewTransportStopData infoviewTransportStopData, ArchetypeChunk chunk)
		{
			return infoviewTransportStopData.m_Type switch
			{
				TransportType.Bus => chunk.Has(ref m_BusStopType), 
				TransportType.Train => chunk.Has(ref m_TrainStopType), 
				TransportType.Taxi => chunk.Has(ref m_TaxiStandType), 
				TransportType.Tram => chunk.Has(ref m_TramStopType), 
				TransportType.Ship => chunk.Has(ref m_ShipStopType), 
				TransportType.Post => chunk.Has(ref m_MailBoxType), 
				TransportType.Airplane => chunk.Has(ref m_AirplaneStopType), 
				TransportType.Subway => chunk.Has(ref m_SubwayStopType), 
				TransportType.Ferry => chunk.Has(ref m_FerryStopType), 
				TransportType.Bicycle => chunk.Has(ref m_BicycleParkingType), 
				_ => false, 
			};
		}

		private bool GetVehicleColor(ArchetypeChunk chunk, out int index)
		{
			index = 0;
			int num = int.MaxValue;
			for (int i = 0; i < m_InfomodeChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_InfomodeChunks[i];
				NativeArray<InfoviewVehicleData> nativeArray = archetypeChunk.GetNativeArray(ref m_InfoviewVehicleType);
				if (nativeArray.Length == 0)
				{
					continue;
				}
				NativeArray<InfomodeActive> nativeArray2 = archetypeChunk.GetNativeArray(ref m_InfomodeActiveType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					InfomodeActive infomodeActive = nativeArray2[j];
					int priority = infomodeActive.m_Priority;
					if (priority < num && HasVehicleColor(nativeArray[j], chunk))
					{
						index = infomodeActive.m_Index;
						num = priority;
					}
				}
			}
			return num != int.MaxValue;
		}

		private bool HasVehicleColor(InfoviewVehicleData infoviewVehicleData, ArchetypeChunk chunk)
		{
			return infoviewVehicleData.m_Type switch
			{
				VehicleType.PassengerTransport => chunk.Has(ref m_PassengerTransportType), 
				VehicleType.CargoTransport => chunk.Has(ref m_CargoTransportType), 
				VehicleType.Taxi => chunk.Has(ref m_TaxiType), 
				VehicleType.ParkMaintenance => chunk.Has(ref m_ParkMaintenanceVehicleType), 
				VehicleType.RoadMaintenance => chunk.Has(ref m_RoadMaintenanceVehicleType), 
				VehicleType.Ambulance => chunk.Has(ref m_AmbulanceType), 
				VehicleType.EvacuatingTransport => chunk.Has(ref m_EvacuatingTransportType), 
				VehicleType.FireEngine => chunk.Has(ref m_FireEngineType), 
				VehicleType.GarbageTruck => chunk.Has(ref m_GarbageTruckType), 
				VehicleType.Hearse => chunk.Has(ref m_HearseType), 
				VehicleType.PoliceCar => chunk.Has(ref m_PoliceCarType), 
				VehicleType.PostVan => chunk.Has(ref m_PostVanType), 
				VehicleType.PrisonerTransport => chunk.Has(ref m_PrisonerTransportType), 
				VehicleType.GoodsDelivery => chunk.Has(ref m_GoodsDeliveryVehicleType), 
				_ => false, 
			};
		}

		private bool GetObjectStatusType(ArchetypeChunk chunk, out InfoviewObjectStatusData statusData, out InfomodeActive activeData)
		{
			statusData = default(InfoviewObjectStatusData);
			activeData = default(InfomodeActive);
			int num = int.MaxValue;
			for (int i = 0; i < m_InfomodeChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_InfomodeChunks[i];
				NativeArray<InfoviewObjectStatusData> nativeArray = archetypeChunk.GetNativeArray(ref m_InfoviewObjectStatusType);
				if (nativeArray.Length == 0)
				{
					continue;
				}
				NativeArray<InfomodeActive> nativeArray2 = archetypeChunk.GetNativeArray(ref m_InfomodeActiveType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					InfomodeActive infomodeActive = nativeArray2[j];
					int priority = infomodeActive.m_Priority;
					if (priority < num)
					{
						InfoviewObjectStatusData infoviewObjectStatusData = nativeArray[j];
						if (HasObjectStatus(nativeArray[j], chunk))
						{
							statusData = infoviewObjectStatusData;
							activeData = infomodeActive;
							num = priority;
						}
					}
				}
			}
			return num != int.MaxValue;
		}

		private bool HasObjectStatus(InfoviewObjectStatusData infoviewObjectStatusData, ArchetypeChunk chunk)
		{
			switch (infoviewObjectStatusData.m_Type)
			{
			case ObjectStatusType.WoodResource:
				return chunk.Has(ref m_TreeType);
			case ObjectStatusType.FireHazard:
				if (!chunk.Has(ref m_BuildingType))
				{
					return chunk.Has(ref m_TreeType);
				}
				return true;
			case ObjectStatusType.Damage:
				return chunk.Has(ref m_DamagedType);
			case ObjectStatusType.Destroyed:
				return chunk.Has(ref m_DestroyedType);
			case ObjectStatusType.ExtractorPlaceholder:
				return chunk.Has(ref m_PlaceholderType);
			case ObjectStatusType.Tourist:
				if (!chunk.Has(ref m_ResidentType))
				{
					if (chunk.Has(ref m_VehicleType))
					{
						return chunk.Has(ref m_PassengerType);
					}
					return false;
				}
				return true;
			default:
				return false;
			}
		}

		private void GetObjectStatusColors(NativeArray<Game.Objects.Color> results, ArchetypeChunk chunk, InfoviewObjectStatusData statusData, InfomodeActive activeData)
		{
			switch (statusData.m_Type)
			{
			case ObjectStatusType.WoodResource:
			{
				NativeArray<Tree> nativeArray2 = chunk.GetNativeArray(ref m_TreeType);
				NativeArray<Plant> nativeArray3 = chunk.GetNativeArray(ref m_PlantType);
				NativeArray<Damaged> nativeArray4 = chunk.GetNativeArray(ref m_DamagedType);
				NativeArray<PrefabRef> nativeArray5 = chunk.GetNativeArray(ref m_PrefabRefType);
				for (int k = 0; k < nativeArray5.Length; k++)
				{
					Tree tree = nativeArray2[k];
					Plant plant = nativeArray3[k];
					PrefabRef prefabRef = nativeArray5[k];
					CollectionUtils.TryGet(nativeArray4, k, out var value);
					if (m_TreeData.HasComponent(prefabRef.m_Prefab))
					{
						TreeData treeData = m_TreeData[prefabRef.m_Prefab];
						float status = ObjectUtils.CalculateWoodAmount(tree, plant, value, treeData);
						results[k] = new Game.Objects.Color((byte)activeData.m_Index, (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, status) * 255f), 0, 255));
					}
					else
					{
						results[k] = default(Game.Objects.Color);
					}
				}
				break;
			}
			case ObjectStatusType.FireHazard:
			{
				NativeArray<Building> nativeArray6 = chunk.GetNativeArray(ref m_BuildingType);
				NativeArray<PrefabRef> nativeArray7 = chunk.GetNativeArray(ref m_PrefabRefType);
				float fireHazard;
				if (nativeArray6.Length != 0)
				{
					NativeArray<Owner> nativeArray8 = chunk.GetNativeArray(ref m_OwnerType);
					NativeArray<CurrentDistrict> nativeArray9 = chunk.GetNativeArray(ref m_CurrentDistrictType);
					NativeArray<Damaged> nativeArray10 = chunk.GetNativeArray(ref m_DamagedType);
					NativeArray<UnderConstruction> nativeArray11 = chunk.GetNativeArray(ref m_UnderConstructionType);
					for (int l = 0; l < nativeArray6.Length; l++)
					{
						PrefabRef prefabRef2 = nativeArray7[l];
						Building building = nativeArray6[l];
						CurrentDistrict currentDistrict = nativeArray9[l];
						if (CollectionUtils.TryGet(nativeArray8, l, out var value2) && (!m_BuildingData.HasComponent(value2.m_Owner) || !m_PlaceableObjectData.TryGetComponent(prefabRef2.m_Prefab, out var componentData4) || (componentData4.m_Flags & Game.Objects.PlacementFlags.Floating) == 0))
						{
							results[l] = default(Game.Objects.Color);
							continue;
						}
						CollectionUtils.TryGet(nativeArray10, l, out var value3);
						if (!CollectionUtils.TryGet(nativeArray11, l, out var value4))
						{
							value4 = new UnderConstruction
							{
								m_Progress = byte.MaxValue
							};
						}
						if (m_FireHazardData.GetFireHazard(prefabRef2, building, currentDistrict, value3, value4, out fireHazard, out var riskFactor))
						{
							results[l] = new Game.Objects.Color((byte)activeData.m_Index, (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, riskFactor) * 255f), 0, 255));
						}
						else
						{
							results[l] = default(Game.Objects.Color);
						}
					}
					break;
				}
				NativeArray<Damaged> nativeArray12 = chunk.GetNativeArray(ref m_DamagedType);
				NativeArray<Game.Objects.Transform> nativeArray13 = chunk.GetNativeArray(ref m_TransformType);
				for (int m = 0; m < nativeArray13.Length; m++)
				{
					PrefabRef prefabRef3 = nativeArray7[m];
					Game.Objects.Transform transform = nativeArray13[m];
					CollectionUtils.TryGet(nativeArray12, m, out var value5);
					if (m_FireHazardData.GetFireHazard(prefabRef3, default(Tree), transform, value5, out fireHazard, out var riskFactor2))
					{
						results[m] = new Game.Objects.Color((byte)activeData.m_Index, (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, riskFactor2) * 255f), 0, 255));
					}
					else
					{
						results[m] = default(Game.Objects.Color);
					}
				}
				break;
			}
			case ObjectStatusType.Damage:
			{
				NativeArray<Damaged> nativeArray14 = chunk.GetNativeArray(ref m_DamagedType);
				for (int num = 0; num < nativeArray14.Length; num++)
				{
					float totalDamage = ObjectUtils.GetTotalDamage(nativeArray14[num]);
					results[num] = new Game.Objects.Color((byte)activeData.m_Index, (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, totalDamage) * 255f), 0, 255));
				}
				break;
			}
			case ObjectStatusType.Destroyed:
			{
				for (int n = 0; n < results.Length; n++)
				{
					results[n] = new Game.Objects.Color((byte)activeData.m_Index, 0);
				}
				break;
			}
			case ObjectStatusType.ExtractorPlaceholder:
			{
				NativeArray<PrefabRef> nativeArray15 = chunk.GetNativeArray(ref m_PrefabRefType);
				for (int num2 = 0; num2 < nativeArray15.Length; num2++)
				{
					PrefabRef prefabRef4 = nativeArray15[num2];
					if (m_PlaceholderBuildingData.TryGetComponent(prefabRef4.m_Prefab, out var componentData5) && componentData5.m_Type == BuildingType.ExtractorBuilding)
					{
						results[num2] = new Game.Objects.Color((byte)activeData.m_Index, byte.MaxValue);
					}
					else
					{
						results[num2] = default(Game.Objects.Color);
					}
				}
				break;
			}
			case ObjectStatusType.Tourist:
			{
				NativeArray<Game.Creatures.Resident> nativeArray = chunk.GetNativeArray(ref m_ResidentType);
				if (nativeArray.Length != 0)
				{
					for (int i = 0; i < nativeArray.Length; i++)
					{
						if (m_Citizens.TryGetComponent(nativeArray[i].m_Citizen, out var componentData) && (componentData.m_State & CitizenFlags.Tourist) != CitizenFlags.None)
						{
							results[i] = new Game.Objects.Color((byte)activeData.m_Index, byte.MaxValue);
						}
						else
						{
							results[i] = default(Game.Objects.Color);
						}
					}
					break;
				}
				BufferAccessor<Passenger> bufferAccessor = chunk.GetBufferAccessor(ref m_PassengerType);
				for (int j = 0; j < bufferAccessor.Length; j++)
				{
					DynamicBuffer<Passenger> dynamicBuffer = bufferAccessor[j];
					bool flag = false;
					foreach (Passenger item in dynamicBuffer)
					{
						if (m_ResidentData.TryGetComponent(item.m_Passenger, out var componentData2) && m_Citizens.TryGetComponent(componentData2.m_Citizen, out var componentData3) && (componentData3.m_State & CitizenFlags.Tourist) != CitizenFlags.None)
						{
							results[j] = new Game.Objects.Color((byte)activeData.m_Index, byte.MaxValue);
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						results[j] = default(Game.Objects.Color);
					}
				}
				break;
			}
			}
		}

		private bool GetNetStatusColors(NativeArray<Game.Objects.Color> results, ArchetypeChunk chunk)
		{
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			int num6 = int.MaxValue;
			int num7 = int.MaxValue;
			int num8 = int.MaxValue;
			int num9 = int.MaxValue;
			int num10 = int.MaxValue;
			for (int i = 0; i < m_InfomodeChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_InfomodeChunks[i];
				NativeArray<InfoviewNetStatusData> nativeArray = archetypeChunk.GetNativeArray(ref m_InfoviewNetStatusType);
				if (nativeArray.Length == 0)
				{
					continue;
				}
				NativeArray<InfomodeActive> nativeArray2 = archetypeChunk.GetNativeArray(ref m_InfomodeActiveType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					InfoviewNetStatusData infoviewNetStatusData = nativeArray[j];
					InfomodeActive infomodeActive = nativeArray2[j];
					int priority = infomodeActive.m_Priority;
					switch (infoviewNetStatusData.m_Type)
					{
					case NetStatusType.LowVoltageFlow:
						if (priority < num6)
						{
							num = infomodeActive.m_Index;
							num6 = priority;
						}
						break;
					case NetStatusType.HighVoltageFlow:
						if (priority < num7)
						{
							num2 = infomodeActive.m_Index;
							num7 = priority;
						}
						break;
					case NetStatusType.PipeWaterFlow:
						if (priority < num8)
						{
							num3 = infomodeActive.m_Index;
							num8 = priority;
						}
						break;
					case NetStatusType.PipeSewageFlow:
						if (priority < num9)
						{
							num4 = infomodeActive.m_Index;
							num9 = priority;
						}
						break;
					case NetStatusType.OilFlow:
						if (priority < num10)
						{
							num5 = infomodeActive.m_Index;
							num10 = priority;
						}
						break;
					}
				}
			}
			if (num == 0 && num2 == 0 && num3 == 0 && num4 == 0 && num5 == 0)
			{
				return false;
			}
			NativeArray<Owner> nativeArray3 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<PrefabRef> nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);
			for (int k = 0; k < results.Length; k++)
			{
				PrefabRef prefabRef = nativeArray4[k];
				if (m_PrefabUtilityObjectData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
				{
					int num11 = 0;
					if ((componentData.m_UtilityTypes & UtilityTypes.LowVoltageLine) != UtilityTypes.None && num != 0)
					{
						num11 = num;
					}
					else if ((componentData.m_UtilityTypes & UtilityTypes.HighVoltageLine) != UtilityTypes.None && num2 != 0)
					{
						num11 = num2;
					}
					else if ((componentData.m_UtilityTypes & UtilityTypes.WaterPipe) != UtilityTypes.None && num3 != 0)
					{
						num11 = num3;
					}
					else if ((componentData.m_UtilityTypes & UtilityTypes.SewagePipe) != UtilityTypes.None && num4 != 0)
					{
						num11 = num4;
					}
					else if ((componentData.m_UtilityTypes & UtilityTypes.Resource) != UtilityTypes.None && num5 != 0)
					{
						num11 = num5;
					}
					if (num11 != 0)
					{
						int num12 = 0;
						if (nativeArray3.Length != 0)
						{
							Owner owner = nativeArray3[k];
							if (num11 == num || num11 == num2)
							{
								ElectricityConsumer componentData4;
								if (m_ElectricityNodeConnectionData.TryGetComponent(owner.m_Owner, out var componentData2) && m_ConnectedFlowEdges.TryGetBuffer(componentData2.m_ElectricityNode, out var bufferData))
								{
									ElectricityFlowEdgeFlags electricityFlowEdgeFlags = ElectricityFlowEdgeFlags.None;
									for (int l = 0; l < bufferData.Length; l++)
									{
										if (m_ElectricityFlowEdgeData.TryGetComponent(bufferData[l].m_Edge, out var componentData3))
										{
											num12 = math.max(num12, math.select(0, 128, componentData3.m_Flow != 0));
											electricityFlowEdgeFlags |= componentData3.m_Flags;
										}
									}
									num12 = math.select(num12, 224, num12 != 0 && (electricityFlowEdgeFlags & ElectricityFlowEdgeFlags.BeyondBottleneck) != 0);
									num12 = math.select(num12, 255, num12 != 0 && (electricityFlowEdgeFlags & ElectricityFlowEdgeFlags.Bottleneck) != 0);
								}
								else if (m_ElectricityConsumerData.TryGetComponent(owner.m_Owner, out componentData4))
								{
									num12 = math.max(num12, math.select(0, 128, componentData4.m_FulfilledConsumption != 0));
									num12 = math.select(num12, 224, num12 != 0 && (componentData4.m_Flags & ElectricityConsumerFlags.BottleneckWarning) != 0);
								}
							}
							else if (num11 == num3 || num11 == num4)
							{
								WaterConsumer componentData7;
								if (m_WaterPipeNodeConnectionData.TryGetComponent(owner.m_Owner, out var componentData5) && m_ConnectedFlowEdges.TryGetBuffer(componentData5.m_WaterPipeNode, out var bufferData2))
								{
									float num13 = 0f;
									for (int m = 0; m < bufferData2.Length; m++)
									{
										if (m_WaterPipeEdgeData.TryGetComponent(bufferData2[m].m_Edge, out var componentData6))
										{
											if (num11 == num3)
											{
												num12 = math.max(num12, math.select(0, 128, componentData6.m_FreshFlow != 0));
												num13 = math.max(num13, componentData6.m_FreshPollution);
											}
											else
											{
												num12 = math.max(num12, math.select(0, 128, componentData6.m_SewageFlow != 0));
											}
										}
									}
									num12 = math.select(num12, 255, num12 != 0 && num13 > 0f);
								}
								else if (m_WaterConsumerData.TryGetComponent(owner.m_Owner, out componentData7))
								{
									if (num11 == num3)
									{
										num12 = math.max(num12, math.select(0, 128, componentData7.m_FulfilledFresh != 0));
										num12 = math.select(num12, 255, num12 != 0 && componentData7.m_Pollution > 0f);
									}
									else
									{
										num12 = math.max(num12, math.select(0, 128, componentData7.m_FulfilledSewage != 0));
									}
								}
							}
							else if (num11 == num5)
							{
								Game.Net.ResourceConnection componentData10;
								if (m_ConnectedEdges.TryGetBuffer(owner.m_Owner, out var bufferData3))
								{
									bool flag = false;
									for (int n = 0; n < bufferData3.Length; n++)
									{
										ConnectedEdge connectedEdge = bufferData3[n];
										Edge edge = m_EdgeData[connectedEdge.m_Edge];
										bool2 x = new bool2(edge.m_Start == owner.m_Owner, edge.m_End == owner.m_Owner);
										if (math.any(x))
										{
											flag = true;
											if (m_ResourceConnectionData.TryGetComponent(connectedEdge.m_Edge, out var componentData8))
											{
												int num14 = math.select(componentData8.m_Flow.x, componentData8.m_Flow.y, x.y);
												num12 = math.max(num12, math.select(0, 128, num14 != 0));
											}
										}
									}
									if (!flag && m_ResourceConnectionData.TryGetComponent(owner.m_Owner, out var componentData9))
									{
										num12 = math.max(num12, math.select(0, 128, componentData9.m_Flow.x != 0));
									}
								}
								else if (m_ResourceConnectionData.TryGetComponent(owner.m_Owner, out componentData10))
								{
									num12 = math.max(num12, math.select(0, 128, componentData10.m_Flow.x != 0 || componentData10.m_Flow.y != 0));
								}
							}
						}
						results[k] = new Game.Objects.Color((byte)num11, (byte)num12);
						continue;
					}
				}
				results[k] = default(Game.Objects.Color);
			}
			return true;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct UpdateMiddleObjectColorsJob : IJobChunk
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_InfomodeChunks;

		[ReadOnly]
		public ComponentTypeHandle<InfomodeActive> m_InfomodeActiveType;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewObjectStatusData> m_InfoviewObjectStatusType;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Controller> m_ControllerType;

		[ReadOnly]
		public ComponentTypeHandle<Building> m_BuildingType;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Game.Objects.Color> m_ColorData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			if (chunk.Has(ref m_BuildingType))
			{
				NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
				int2 excludedSubBuildingActiveIndices = GetExcludedSubBuildingActiveIndices();
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Entity entity = nativeArray[i];
					Owner owner = nativeArray2[i];
					Game.Objects.Color value = m_ColorData[entity];
					if (value.m_Index != 0)
					{
						continue;
					}
					while (true)
					{
						if (m_ColorData.TryGetComponent(owner.m_Owner, out var componentData) && math.all(componentData.m_Index != excludedSubBuildingActiveIndices))
						{
							value.m_Index = componentData.m_Index;
							value.m_Value = componentData.m_Value;
							m_ColorData[entity] = value;
						}
						if (!m_OwnerData.TryGetComponent(owner.m_Owner, out var componentData2))
						{
							break;
						}
						owner = componentData2;
					}
				}
				return;
			}
			NativeArray<Controller> nativeArray3 = chunk.GetNativeArray(ref m_ControllerType);
			if (nativeArray3.Length != 0)
			{
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Entity entity2 = nativeArray[j];
					Controller controller = nativeArray3[j];
					if (controller.m_Controller != entity2)
					{
						Game.Objects.Color componentData3 = m_ColorData[entity2];
						if (componentData3.m_Index == 0 && m_ColorData.TryGetComponent(controller.m_Controller, out componentData3))
						{
							m_ColorData[entity2] = componentData3;
						}
					}
				}
			}
			else
			{
				for (int k = 0; k < nativeArray.Length; k++)
				{
					Entity entity3 = nativeArray[k];
					m_ColorData[entity3] = default(Game.Objects.Color);
				}
			}
		}

		private int2 GetExcludedSubBuildingActiveIndices()
		{
			int2 result = -1;
			for (int i = 0; i < m_InfomodeChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_InfomodeChunks[i];
				NativeArray<InfoviewObjectStatusData> nativeArray = archetypeChunk.GetNativeArray(ref m_InfoviewObjectStatusType);
				if (nativeArray.Length == 0)
				{
					continue;
				}
				NativeArray<InfomodeActive> nativeArray2 = archetypeChunk.GetNativeArray(ref m_InfomodeActiveType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					InfomodeActive infomodeActive = nativeArray2[j];
					switch (nativeArray[j].m_Type)
					{
					case ObjectStatusType.Damage:
						result.x = infomodeActive.m_Index;
						break;
					case ObjectStatusType.Destroyed:
						result.y = infomodeActive.m_Index;
						break;
					}
				}
			}
			return result;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct UpdateTempObjectColorsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Game.Objects.Color> m_ColorData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Temp> nativeArray2 = chunk.GetNativeArray(ref m_TempType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Temp temp = nativeArray2[i];
				if (m_ColorData.TryGetComponent(temp.m_Original, out var componentData))
				{
					m_ColorData[nativeArray[i]] = componentData;
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct UpdateSubObjectColorsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Elevation> m_ElevationType;

		[ReadOnly]
		public ComponentTypeHandle<Tree> m_TreeType;

		[ReadOnly]
		public ComponentTypeHandle<Plant> m_PlantType;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Elevation> m_ElevationData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<Vehicle> m_VehicleData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Game.Objects.Color> m_ColorData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			if (chunk.Has(ref m_TreeType))
			{
				NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
				NativeArray<Game.Objects.Elevation> nativeArray3 = chunk.GetNativeArray(ref m_ElevationType);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Entity entity = nativeArray[i];
					Owner owner = nativeArray2[i];
					Game.Objects.Elevation value;
					bool flag = CollectionUtils.TryGet(nativeArray3, i, out value) && (value.m_Flags & ElevationFlags.OnGround) == 0;
					bool flag2 = flag && !m_ColorData.HasComponent(owner.m_Owner);
					Owner componentData;
					while (m_OwnerData.TryGetComponent(owner.m_Owner, out componentData) && !m_BuildingData.HasComponent(owner.m_Owner) && !m_VehicleData.HasComponent(owner.m_Owner))
					{
						if (flag2)
						{
							if (m_ColorData.HasComponent(owner.m_Owner))
							{
								flag2 = false;
							}
							else
							{
								flag &= m_ElevationData.TryGetComponent(owner.m_Owner, out value) && (value.m_Flags & ElevationFlags.OnGround) == 0;
							}
						}
						owner = componentData;
					}
					Game.Objects.Color value2 = default(Game.Objects.Color);
					if (m_ColorData.TryGetComponent(owner.m_Owner, out var componentData2) && (flag || componentData2.m_SubColor))
					{
						value2 = componentData2;
					}
					m_ColorData[entity] = value2;
				}
				return;
			}
			NativeArray<Owner> nativeArray4 = chunk.GetNativeArray(ref m_OwnerType);
			for (int j = 0; j < nativeArray.Length; j++)
			{
				Entity entity2 = nativeArray[j];
				Owner owner2 = nativeArray4[j];
				Owner componentData3;
				while (m_OwnerData.TryGetComponent(owner2.m_Owner, out componentData3) && !m_BuildingData.HasComponent(owner2.m_Owner) && !m_VehicleData.HasComponent(owner2.m_Owner))
				{
					owner2 = componentData3;
				}
				if (m_ColorData.TryGetComponent(owner2.m_Owner, out var componentData4))
				{
					m_ColorData[entity2] = componentData4;
				}
				else
				{
					m_ColorData[entity2] = default(Game.Objects.Color);
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
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InfomodeActive> __Game_Prefabs_InfomodeActive_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewBuildingData> __Game_Prefabs_InfoviewBuildingData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewBuildingStatusData> __Game_Prefabs_InfoviewBuildingStatusData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewTransportStopData> __Game_Prefabs_InfoviewTransportStopData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewVehicleData> __Game_Prefabs_InfoviewVehicleData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewObjectStatusData> __Game_Prefabs_InfoviewObjectStatusData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewNetStatusData> __Game_Prefabs_InfoviewNetStatusData_RO_ComponentTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Building> __Game_Buildings_Building_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.Hospital> __Game_Buildings_Hospital_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ElectricityProducer> __Game_Buildings_ElectricityProducer_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.Transformer> __Game_Buildings_Transformer_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.WaterPumpingStation> __Game_Buildings_WaterPumpingStation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.WaterTower> __Game_Buildings_WaterTower_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.SewageOutlet> __Game_Buildings_SewageOutlet_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WastewaterTreatmentPlant> __Game_Buildings_WastewaterTreatmentPlant_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.TransportDepot> __Game_Buildings_TransportDepot_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.TransportStation> __Game_Buildings_TransportStation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.GarbageFacility> __Game_Buildings_GarbageFacility_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.FireStation> __Game_Buildings_FireStation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.PoliceStation> __Game_Buildings_PoliceStation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CrimeProducer> __Game_Buildings_CrimeProducer_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<RoadMaintenance> __Game_Buildings_RoadMaintenance_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ParkMaintenance> __Game_Buildings_ParkMaintenance_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.PostFacility> __Game_Buildings_PostFacility_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.TelecomFacility> __Game_Buildings_TelecomFacility_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.School> __Game_Buildings_School_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.Park> __Game_Buildings_Park_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<AttractivenessProvider> __Game_Buildings_AttractivenessProvider_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.EmergencyShelter> __Game_Buildings_EmergencyShelter_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.DisasterFacility> __Game_Buildings_DisasterFacility_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.FirewatchTower> __Game_Buildings_FirewatchTower_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.DeathcareFacility> __Game_Buildings_DeathcareFacility_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.Prison> __Game_Buildings_Prison_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<AdminBuilding> __Game_Buildings_AdminBuilding_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.WelfareOffice> __Game_Buildings_WelfareOffice_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.ResearchFacility> __Game_Buildings_ResearchFacility_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CarParkingFacility> __Game_Buildings_CarParkingFacility_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<BicycleParkingFacility> __Game_Buildings_BicycleParkingFacility_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.Battery> __Game_Buildings_Battery_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<MailProducer> __Game_Buildings_MailProducer_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<BuildingCondition> __Game_Buildings_BuildingCondition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ResidentialProperty> __Game_Buildings_ResidentialProperty_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CommercialProperty> __Game_Buildings_CommercialProperty_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<IndustrialProperty> __Game_Buildings_IndustrialProperty_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<OfficeProperty> __Game_Buildings_OfficeProperty_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<StorageProperty> __Game_Buildings_StorageProperty_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ExtractorProperty> __Game_Buildings_ExtractorProperty_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<GarbageProducer> __Game_Buildings_GarbageProducer_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Abandoned> __Game_Buildings_Abandoned_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.LeisureProvider> __Game_Buildings_LeisureProvider_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ElectricityConsumer> __Game_Buildings_ElectricityConsumer_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WaterConsumer> __Game_Buildings_WaterConsumer_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Renter> __Game_Buildings_Renter_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		public SharedComponentTypeHandle<CoverageServiceType> __Game_Net_CoverageServiceType_SharedComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Routes.TransportStop> __Game_Routes_TransportStop_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<BusStop> __Game_Routes_BusStop_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TrainStop> __Game_Routes_TrainStop_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TaxiStand> __Game_Routes_TaxiStand_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TramStop> __Game_Routes_TramStop_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ShipStop> __Game_Routes_ShipStop_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Routes.MailBox> __Game_Routes_MailBox_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Routes.WorkStop> __Game_Routes_WorkStop_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<AirplaneStop> __Game_Routes_AirplaneStop_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<SubwayStop> __Game_Routes_SubwayStop_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<FerryStop> __Game_Routes_FerryStop_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<BicycleParking> __Game_Routes_BicycleParking_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Vehicle> __Game_Vehicles_Vehicle_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PassengerTransport> __Game_Vehicles_PassengerTransport_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.CargoTransport> __Game_Vehicles_CargoTransport_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.Taxi> __Game_Vehicles_Taxi_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ParkMaintenanceVehicle> __Game_Vehicles_ParkMaintenanceVehicle_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<RoadMaintenanceVehicle> __Game_Vehicles_RoadMaintenanceVehicle_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.Ambulance> __Game_Vehicles_Ambulance_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<EvacuatingTransport> __Game_Vehicles_EvacuatingTransport_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.FireEngine> __Game_Vehicles_FireEngine_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.GarbageTruck> __Game_Vehicles_GarbageTruck_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.Hearse> __Game_Vehicles_Hearse_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.PoliceCar> __Game_Vehicles_PoliceCar_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.PostVan> __Game_Vehicles_PostVan_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrisonerTransport> __Game_Vehicles_PrisonerTransport_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<GoodsDeliveryVehicle> __Game_Vehicles_GoodsDeliveryVehicle_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Passenger> __Game_Vehicles_Passenger_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Creatures.Resident> __Game_Creatures_Resident_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Tree> __Game_Objects_Tree_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Plant> __Game_Objects_Plant_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.UtilityObject> __Game_Objects_UtilityObject_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Destroyed> __Game_Common_Destroyed_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Damaged> __Game_Objects_Damaged_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<UnderConstruction> __Game_Objects_UnderConstruction_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.UniqueObject> __Game_Objects_UniqueObject_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Placeholder> __Game_Objects_Placeholder_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentDistrict> __Game_Areas_CurrentDistrict_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Attached> __Game_Objects_Attached_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<AccidentSite> __Game_Events_AccidentSite_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<TreeData> __Game_Prefabs_TreeData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Park> __Game_Buildings_Park_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Abandoned> __Game_Buildings_Abandoned_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PollutionData> __Game_Prefabs_PollutionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PollutionModifierData> __Game_Prefabs_PollutionModifierData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CommercialCompany> __Game_Companies_CommercialCompany_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceholderBuildingData> __Game_Prefabs_PlaceholderBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SewageOutletData> __Game_Prefabs_SewageOutletData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> __Game_Prefabs_PlaceableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Creatures.Resident> __Game_Creatures_Resident_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Profitability> __Game_Companies_Profitability_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CompanyStatisticData> __Game_Companies_CompanyStatisticData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CurrentTrading> __Game_Companies_CurrentTrading_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.LeisureProvider> __Game_Buildings_LeisureProvider_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Employee> __Game_Companies_Employee_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<UtilityObjectData> __Game_Prefabs_UtilityObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> __Game_Buildings_ElectricityConsumer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaterConsumer> __Game_Buildings_WaterConsumer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityNodeConnection> __Game_Simulation_ElectricityNodeConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaterPipeNodeConnection> __Game_Simulation_WaterPipeNodeConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityFlowEdge> __Game_Simulation_ElectricityFlowEdge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaterPipeEdge> __Game_Simulation_WaterPipeEdge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ResourceConnection> __Game_Net_ResourceConnection_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedFlowEdge> __Game_Simulation_ConnectedFlowEdge_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<PollutionEmitModifier> __Game_Buildings_PollutionEmitModifier_RO_ComponentLookup;

		public ComponentTypeHandle<Game.Objects.Color> __Game_Objects_Color_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Controller> __Game_Vehicles_Controller_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		public ComponentLookup<Game.Objects.Color> __Game_Objects_Color_RW_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Elevation> __Game_Objects_Elevation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Elevation> __Game_Objects_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Vehicle> __Game_Vehicles_Vehicle_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_InfomodeActive_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfomodeActive>(isReadOnly: true);
			__Game_Prefabs_InfoviewBuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewBuildingData>(isReadOnly: true);
			__Game_Prefabs_InfoviewBuildingStatusData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewBuildingStatusData>(isReadOnly: true);
			__Game_Prefabs_InfoviewTransportStopData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewTransportStopData>(isReadOnly: true);
			__Game_Prefabs_InfoviewVehicleData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewVehicleData>(isReadOnly: true);
			__Game_Prefabs_InfoviewObjectStatusData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewObjectStatusData>(isReadOnly: true);
			__Game_Prefabs_InfoviewNetStatusData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewNetStatusData>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Buildings_Building_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Building>(isReadOnly: true);
			__Game_Buildings_Hospital_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.Hospital>(isReadOnly: true);
			__Game_Buildings_ElectricityProducer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ElectricityProducer>(isReadOnly: true);
			__Game_Buildings_Transformer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.Transformer>(isReadOnly: true);
			__Game_Buildings_WaterPumpingStation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.WaterPumpingStation>(isReadOnly: true);
			__Game_Buildings_WaterTower_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.WaterTower>(isReadOnly: true);
			__Game_Buildings_SewageOutlet_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.SewageOutlet>(isReadOnly: true);
			__Game_Buildings_WastewaterTreatmentPlant_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WastewaterTreatmentPlant>(isReadOnly: true);
			__Game_Buildings_TransportDepot_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.TransportDepot>(isReadOnly: true);
			__Game_Buildings_TransportStation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.TransportStation>(isReadOnly: true);
			__Game_Buildings_GarbageFacility_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.GarbageFacility>(isReadOnly: true);
			__Game_Buildings_FireStation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.FireStation>(isReadOnly: true);
			__Game_Buildings_PoliceStation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.PoliceStation>(isReadOnly: true);
			__Game_Buildings_CrimeProducer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CrimeProducer>(isReadOnly: true);
			__Game_Buildings_RoadMaintenance_RO_ComponentTypeHandle = state.GetComponentTypeHandle<RoadMaintenance>(isReadOnly: true);
			__Game_Buildings_ParkMaintenance_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ParkMaintenance>(isReadOnly: true);
			__Game_Buildings_PostFacility_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.PostFacility>(isReadOnly: true);
			__Game_Buildings_TelecomFacility_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.TelecomFacility>(isReadOnly: true);
			__Game_Buildings_School_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.School>(isReadOnly: true);
			__Game_Buildings_Park_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.Park>(isReadOnly: true);
			__Game_Buildings_AttractivenessProvider_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AttractivenessProvider>(isReadOnly: true);
			__Game_Buildings_EmergencyShelter_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.EmergencyShelter>(isReadOnly: true);
			__Game_Buildings_DisasterFacility_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.DisasterFacility>(isReadOnly: true);
			__Game_Buildings_FirewatchTower_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.FirewatchTower>(isReadOnly: true);
			__Game_Buildings_DeathcareFacility_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.DeathcareFacility>(isReadOnly: true);
			__Game_Buildings_Prison_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.Prison>(isReadOnly: true);
			__Game_Buildings_AdminBuilding_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AdminBuilding>(isReadOnly: true);
			__Game_Buildings_WelfareOffice_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.WelfareOffice>(isReadOnly: true);
			__Game_Buildings_ResearchFacility_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.ResearchFacility>(isReadOnly: true);
			__Game_Buildings_CarParkingFacility_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CarParkingFacility>(isReadOnly: true);
			__Game_Buildings_BicycleParkingFacility_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BicycleParkingFacility>(isReadOnly: true);
			__Game_Buildings_Battery_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.Battery>(isReadOnly: true);
			__Game_Buildings_MailProducer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<MailProducer>(isReadOnly: true);
			__Game_Buildings_BuildingCondition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingCondition>(isReadOnly: true);
			__Game_Buildings_ResidentialProperty_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ResidentialProperty>(isReadOnly: true);
			__Game_Buildings_CommercialProperty_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CommercialProperty>(isReadOnly: true);
			__Game_Buildings_IndustrialProperty_RO_ComponentTypeHandle = state.GetComponentTypeHandle<IndustrialProperty>(isReadOnly: true);
			__Game_Buildings_OfficeProperty_RO_ComponentTypeHandle = state.GetComponentTypeHandle<OfficeProperty>(isReadOnly: true);
			__Game_Buildings_StorageProperty_RO_ComponentTypeHandle = state.GetComponentTypeHandle<StorageProperty>(isReadOnly: true);
			__Game_Buildings_ExtractorProperty_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ExtractorProperty>(isReadOnly: true);
			__Game_Buildings_GarbageProducer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<GarbageProducer>(isReadOnly: true);
			__Game_Buildings_Abandoned_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Abandoned>(isReadOnly: true);
			__Game_Buildings_LeisureProvider_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.LeisureProvider>(isReadOnly: true);
			__Game_Buildings_ElectricityConsumer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ElectricityConsumer>(isReadOnly: true);
			__Game_Buildings_WaterConsumer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WaterConsumer>(isReadOnly: true);
			__Game_Buildings_Efficiency_RO_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferTypeHandle = state.GetBufferTypeHandle<Renter>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Net_CoverageServiceType_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<CoverageServiceType>();
			__Game_Routes_TransportStop_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Routes.TransportStop>(isReadOnly: true);
			__Game_Routes_BusStop_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BusStop>(isReadOnly: true);
			__Game_Routes_TrainStop_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TrainStop>(isReadOnly: true);
			__Game_Routes_TaxiStand_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TaxiStand>(isReadOnly: true);
			__Game_Routes_TramStop_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TramStop>(isReadOnly: true);
			__Game_Routes_ShipStop_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ShipStop>(isReadOnly: true);
			__Game_Routes_MailBox_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Routes.MailBox>(isReadOnly: true);
			__Game_Routes_WorkStop_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Routes.WorkStop>(isReadOnly: true);
			__Game_Routes_AirplaneStop_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AirplaneStop>(isReadOnly: true);
			__Game_Routes_SubwayStop_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SubwayStop>(isReadOnly: true);
			__Game_Routes_FerryStop_RO_ComponentTypeHandle = state.GetComponentTypeHandle<FerryStop>(isReadOnly: true);
			__Game_Routes_BicycleParking_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BicycleParking>(isReadOnly: true);
			__Game_Vehicles_Vehicle_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Vehicle>(isReadOnly: true);
			__Game_Vehicles_PassengerTransport_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PassengerTransport>(isReadOnly: true);
			__Game_Vehicles_CargoTransport_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Vehicles.CargoTransport>(isReadOnly: true);
			__Game_Vehicles_Taxi_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Vehicles.Taxi>(isReadOnly: true);
			__Game_Vehicles_ParkMaintenanceVehicle_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ParkMaintenanceVehicle>(isReadOnly: true);
			__Game_Vehicles_RoadMaintenanceVehicle_RO_ComponentTypeHandle = state.GetComponentTypeHandle<RoadMaintenanceVehicle>(isReadOnly: true);
			__Game_Vehicles_Ambulance_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Vehicles.Ambulance>(isReadOnly: true);
			__Game_Vehicles_EvacuatingTransport_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EvacuatingTransport>(isReadOnly: true);
			__Game_Vehicles_FireEngine_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Vehicles.FireEngine>(isReadOnly: true);
			__Game_Vehicles_GarbageTruck_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Vehicles.GarbageTruck>(isReadOnly: true);
			__Game_Vehicles_Hearse_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Vehicles.Hearse>(isReadOnly: true);
			__Game_Vehicles_PoliceCar_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Vehicles.PoliceCar>(isReadOnly: true);
			__Game_Vehicles_PostVan_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Vehicles.PostVan>(isReadOnly: true);
			__Game_Vehicles_PrisonerTransport_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrisonerTransport>(isReadOnly: true);
			__Game_Vehicles_GoodsDeliveryVehicle_RO_ComponentTypeHandle = state.GetComponentTypeHandle<GoodsDeliveryVehicle>(isReadOnly: true);
			__Game_Vehicles_Passenger_RO_BufferTypeHandle = state.GetBufferTypeHandle<Passenger>(isReadOnly: true);
			__Game_Creatures_Resident_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Creatures.Resident>(isReadOnly: true);
			__Game_Objects_Tree_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Tree>(isReadOnly: true);
			__Game_Objects_Plant_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Plant>(isReadOnly: true);
			__Game_Objects_UtilityObject_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.UtilityObject>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Destroyed>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Objects_Damaged_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Damaged>(isReadOnly: true);
			__Game_Objects_UnderConstruction_RO_ComponentTypeHandle = state.GetComponentTypeHandle<UnderConstruction>(isReadOnly: true);
			__Game_Objects_UniqueObject_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.UniqueObject>(isReadOnly: true);
			__Game_Objects_Placeholder_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Placeholder>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
			__Game_Areas_CurrentDistrict_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentDistrict>(isReadOnly: true);
			__Game_Objects_Attached_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Attached>(isReadOnly: true);
			__Game_Events_AccidentSite_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AccidentSite>(isReadOnly: true);
			__Game_Prefabs_TreeData_RO_ComponentLookup = state.GetComponentLookup<TreeData>(isReadOnly: true);
			__Game_Buildings_Park_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.Park>(isReadOnly: true);
			__Game_Buildings_Abandoned_RO_ComponentLookup = state.GetComponentLookup<Abandoned>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
			__Game_Prefabs_PollutionData_RO_ComponentLookup = state.GetComponentLookup<PollutionData>(isReadOnly: true);
			__Game_Prefabs_PollutionModifierData_RO_ComponentLookup = state.GetComponentLookup<PollutionModifierData>(isReadOnly: true);
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Companies_CommercialCompany_RO_ComponentLookup = state.GetComponentLookup<CommercialCompany>(isReadOnly: true);
			__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Prefabs_PlaceholderBuildingData_RO_ComponentLookup = state.GetComponentLookup<PlaceholderBuildingData>(isReadOnly: true);
			__Game_Prefabs_SewageOutletData_RO_ComponentLookup = state.GetComponentLookup<SewageOutletData>(isReadOnly: true);
			__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup = state.GetComponentLookup<PlaceableObjectData>(isReadOnly: true);
			__Game_Creatures_Resident_RO_ComponentLookup = state.GetComponentLookup<Game.Creatures.Resident>(isReadOnly: true);
			__Game_Companies_Profitability_RO_ComponentLookup = state.GetComponentLookup<Profitability>(isReadOnly: true);
			__Game_Companies_CompanyStatisticData_RO_ComponentLookup = state.GetComponentLookup<CompanyStatisticData>(isReadOnly: true);
			__Game_Companies_CurrentTrading_RO_BufferLookup = state.GetBufferLookup<CurrentTrading>(isReadOnly: true);
			__Game_Buildings_LeisureProvider_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.LeisureProvider>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
			__Game_Companies_Employee_RO_BufferLookup = state.GetBufferLookup<Employee>(isReadOnly: true);
			__Game_Economy_Resources_RO_BufferLookup = state.GetBufferLookup<Game.Economy.Resources>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ZoneData_RO_ComponentLookup = state.GetComponentLookup<ZoneData>(isReadOnly: true);
			__Game_Prefabs_UtilityObjectData_RO_ComponentLookup = state.GetComponentLookup<UtilityObjectData>(isReadOnly: true);
			__Game_Buildings_ElectricityConsumer_RO_ComponentLookup = state.GetComponentLookup<ElectricityConsumer>(isReadOnly: true);
			__Game_Buildings_WaterConsumer_RO_ComponentLookup = state.GetComponentLookup<WaterConsumer>(isReadOnly: true);
			__Game_Simulation_ElectricityNodeConnection_RO_ComponentLookup = state.GetComponentLookup<ElectricityNodeConnection>(isReadOnly: true);
			__Game_Simulation_WaterPipeNodeConnection_RO_ComponentLookup = state.GetComponentLookup<WaterPipeNodeConnection>(isReadOnly: true);
			__Game_Simulation_ElectricityFlowEdge_RO_ComponentLookup = state.GetComponentLookup<ElectricityFlowEdge>(isReadOnly: true);
			__Game_Simulation_WaterPipeEdge_RO_ComponentLookup = state.GetComponentLookup<WaterPipeEdge>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_ResourceConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ResourceConnection>(isReadOnly: true);
			__Game_Simulation_ConnectedFlowEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedFlowEdge>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Buildings_PollutionEmitModifier_RO_ComponentLookup = state.GetComponentLookup<PollutionEmitModifier>(isReadOnly: true);
			__Game_Objects_Color_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Color>();
			__Game_Vehicles_Controller_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Controller>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Objects_Color_RW_ComponentLookup = state.GetComponentLookup<Game.Objects.Color>();
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Objects_Elevation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Elevation>(isReadOnly: true);
			__Game_Objects_Elevation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Elevation>(isReadOnly: true);
			__Game_Vehicles_Vehicle_RO_ComponentLookup = state.GetComponentLookup<Vehicle>(isReadOnly: true);
		}
	}

	private EntityQuery m_ObjectQuery;

	private EntityQuery m_MiddleObjectQuery;

	private EntityQuery m_TempObjectQuery;

	private EntityQuery m_SubObjectQuery;

	private EntityQuery m_InfomodeQuery;

	private EntityQuery m_HappinessParameterQuery;

	private EntityQuery m_EconomyParameterQuery;

	private EntityQuery m_PollutionParameterQuery;

	private EntityQuery m_FireConfigQuery;

	private EventHelpers.FireHazardData m_FireHazardData;

	private ToolSystem m_ToolSystem;

	private LocalEffectSystem m_LocalEffectSystem;

	private ClimateSystem m_ClimateSystem;

	private FireHazardSystem m_FireHazardSystem;

	private TelecomCoverageSystem m_TelecomCoverageSystem;

	private CitySystem m_CitySystem;

	private PrefabSystem m_PrefabSystem;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_LocalEffectSystem = base.World.GetOrCreateSystemManaged<LocalEffectSystem>();
		m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
		m_FireHazardSystem = base.World.GetOrCreateSystemManaged<FireHazardSystem>();
		m_TelecomCoverageSystem = base.World.GetOrCreateSystemManaged<TelecomCoverageSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_ObjectQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Game.Objects.Object>(),
				ComponentType.ReadWrite<Game.Objects.Color>()
			},
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Hidden>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Owner>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<Game.Objects.Object>(),
				ComponentType.ReadOnly<Owner>(),
				ComponentType.ReadWrite<Game.Objects.Color>()
			},
			Any = new ComponentType[4]
			{
				ComponentType.ReadOnly<Vehicle>(),
				ComponentType.ReadOnly<Creature>(),
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<Game.Objects.UtilityObject>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Hidden>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_MiddleObjectQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<Owner>(),
				ComponentType.ReadWrite<Game.Objects.Color>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Hidden>(),
				ComponentType.ReadOnly<Deleted>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<Vehicle>(),
				ComponentType.ReadOnly<Controller>(),
				ComponentType.ReadWrite<Game.Objects.Color>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Hidden>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_TempObjectQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<Game.Objects.Object>(),
				ComponentType.ReadWrite<Game.Objects.Color>(),
				ComponentType.ReadOnly<Temp>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Hidden>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_SubObjectQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<Game.Objects.Object>(),
				ComponentType.ReadOnly<Owner>(),
				ComponentType.ReadWrite<Game.Objects.Color>()
			},
			None = new ComponentType[6]
			{
				ComponentType.ReadOnly<Hidden>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Vehicle>(),
				ComponentType.ReadOnly<Creature>(),
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<Game.Objects.UtilityObject>()
			}
		});
		m_InfomodeQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<InfomodeActive>() },
			Any = new ComponentType[6]
			{
				ComponentType.ReadOnly<InfoviewBuildingData>(),
				ComponentType.ReadOnly<InfoviewBuildingStatusData>(),
				ComponentType.ReadOnly<InfoviewTransportStopData>(),
				ComponentType.ReadOnly<InfoviewVehicleData>(),
				ComponentType.ReadOnly<InfoviewObjectStatusData>(),
				ComponentType.ReadOnly<InfoviewNetStatusData>()
			},
			None = new ComponentType[0]
		});
		m_HappinessParameterQuery = GetEntityQuery(ComponentType.ReadOnly<CitizenHappinessParameterData>());
		m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		m_PollutionParameterQuery = GetEntityQuery(ComponentType.ReadOnly<PollutionParameterData>());
		m_FireConfigQuery = GetEntityQuery(ComponentType.ReadOnly<FireConfigurationData>());
		m_FireHazardData = new EventHelpers.FireHazardData(this);
		RequireForUpdate(m_HappinessParameterQuery);
		RequireForUpdate(m_EconomyParameterQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!(m_ToolSystem.activeInfoview == null) && !m_ObjectQuery.IsEmptyIgnoreFilter)
		{
			JobHandle dependencies;
			LocalEffectSystem.ReadData readData = m_LocalEffectSystem.GetReadData(out dependencies);
			JobHandle outJobHandle;
			NativeList<ArchetypeChunk> infomodeChunks = m_InfomodeQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
			FireConfigurationPrefab prefab = m_PrefabSystem.GetPrefab<FireConfigurationPrefab>(m_FireConfigQuery.GetSingletonEntity());
			m_FireHazardData.Update(this, readData, prefab, m_ClimateSystem.temperature, m_FireHazardSystem.noRainDays);
			JobHandle dependencies2;
			UpdateObjectColorsJob jobData = new UpdateObjectColorsJob
			{
				m_InfomodeChunks = infomodeChunks,
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_InfomodeActiveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfomodeActive_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_InfoviewBuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewBuildingData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_InfoviewBuildingStatusType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewBuildingStatusData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_InfoviewTransportStopType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewTransportStopData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_InfoviewVehicleType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewVehicleData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_InfoviewObjectStatusType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewObjectStatusData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_InfoviewNetStatusType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewNetStatusData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_BuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_HospitalType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Hospital_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ElectricityProducerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ElectricityProducer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TransformerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Transformer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_WaterPumpingStationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_WaterPumpingStation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_WaterTowerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_WaterTower_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_SewageOutletType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_SewageOutlet_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_WastewaterTreatmentPlantType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_WastewaterTreatmentPlant_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TransportDepotType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_TransportDepot_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TransportStationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_TransportStation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_GarbageFacilityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_GarbageFacility_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_FireStationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_FireStation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PoliceStationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_PoliceStation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CrimeProducerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_CrimeProducer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_RoadMaintenanceType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_RoadMaintenance_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ParkMaintenanceType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ParkMaintenance_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PostFacilityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_PostFacility_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TelecomFacilityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_TelecomFacility_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_SchoolType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_School_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ParkType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Park_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_AttractivenessProviderType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_AttractivenessProvider_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_EmergencyShelterType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_EmergencyShelter_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_DisasterFacilityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_DisasterFacility_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_FirewatchTowerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_FirewatchTower_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_DeathcareFacilityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_DeathcareFacility_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrisonType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Prison_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_AdminBuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_AdminBuilding_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_WelfareOfficeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_WelfareOffice_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ResearchFacilityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ResearchFacility_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CarParkingFacilityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_CarParkingFacility_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_BicycleParkingFacilityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_BicycleParkingFacility_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_BatteryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Battery_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_MailProducerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_MailProducer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_BuildingConditionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_BuildingCondition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ResidentialPropertyType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ResidentialProperty_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CommercialPropertyType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_CommercialProperty_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_IndustrialPropertyType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_IndustrialProperty_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OfficePropertyType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_OfficeProperty_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_StoragePropertyType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_StorageProperty_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ExtractorPropertyType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ExtractorProperty_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_GarbageProducerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_GarbageProducer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_AbandonedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Abandoned_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_LeisureProviderType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_LeisureProvider_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ElectricityConsumerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_WaterConsumerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_BuildingEfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_RenterType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_CoverageServiceType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Net_CoverageServiceType_SharedComponentTypeHandle, ref base.CheckedStateRef),
				m_TransportStopType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_TransportStop_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_BusStopType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_BusStop_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TrainStopType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_TrainStop_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TaxiStandType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_TaxiStand_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TramStopType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_TramStop_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ShipStopType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_ShipStop_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_MailBoxType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_MailBox_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_WorkStopType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_WorkStop_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_AirplaneStopType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_AirplaneStop_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_SubwayStopType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_SubwayStop_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_FerryStopType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_FerryStop_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_BicycleParkingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_BicycleParking_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_VehicleType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Vehicle_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PassengerTransportType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_PassengerTransport_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CargoTransportType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_CargoTransport_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TaxiType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Taxi_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ParkMaintenanceVehicleType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_ParkMaintenanceVehicle_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_RoadMaintenanceVehicleType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_RoadMaintenanceVehicle_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_AmbulanceType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Ambulance_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_EvacuatingTransportType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_EvacuatingTransport_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_FireEngineType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_FireEngine_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_GarbageTruckType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_GarbageTruck_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_HearseType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Hearse_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PoliceCarType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_PoliceCar_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PostVanType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_PostVan_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrisonerTransportType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_PrisonerTransport_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_GoodsDeliveryVehicleType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_GoodsDeliveryVehicle_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PassengerType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_Passenger_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_ResidentType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Resident_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TreeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Tree_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PlantType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Plant_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_UtilityObjectType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_UtilityObject_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_DestroyedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_DamagedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Damaged_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_UnderConstructionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_UnderConstruction_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_UniqueObjectType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_UniqueObject_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PlaceholderType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Placeholder_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CurrentDistrictType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_AttachedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_AccidentSiteType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_AccidentSite_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TreeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TreeData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ParkData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Park_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AbandonedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingPropertyData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PollutionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PollutionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PollutionModifierData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PollutionModifierData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Citizens = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CommercialCompanies = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_CommercialCompany_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Households = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
				m_SpawnableBuildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PlaceholderBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceholderBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SewageOutletData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SewageOutletData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PlaceableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ResidentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Resident_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Profitabilities = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_Profitability_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CompanyStatisticData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_CompanyStatisticData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurrentTradings = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_CurrentTrading_RO_BufferLookup, ref base.CheckedStateRef),
				m_LeisureProviders = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_LeisureProvider_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
				m_IndustrialProcessData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Employees = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_Employee_RO_BufferLookup, ref base.CheckedStateRef),
				m_ResourceBuffs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RO_BufferLookup, ref base.CheckedStateRef),
				m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ZoneDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabUtilityObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_UtilityObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ElectricityConsumerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
				m_WaterConsumerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ElectricityNodeConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ElectricityNodeConnection_RO_ComponentLookup, ref base.CheckedStateRef),
				m_WaterPipeNodeConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_WaterPipeNodeConnection_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ElectricityFlowEdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ElectricityFlowEdge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_WaterPipeEdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_WaterPipeEdge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ResourceConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ResourceConnection_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ConnectedFlowEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ConnectedFlowEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
				m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_PollutionEmitModifiers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PollutionEmitModifier_RO_ComponentLookup, ref base.CheckedStateRef),
				m_FireHazardData = m_FireHazardData,
				m_TelecomCoverageData = m_TelecomCoverageSystem.GetData(readOnly: true, out dependencies2),
				m_PollutionParameters = m_PollutionParameterQuery.GetSingleton<PollutionParameterData>(),
				m_EconomyParameterData = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
				m_City = m_CitySystem.City,
				m_ColorType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Color_RW_ComponentTypeHandle, ref base.CheckedStateRef)
			};
			UpdateMiddleObjectColorsJob jobData2 = new UpdateMiddleObjectColorsJob
			{
				m_InfomodeChunks = infomodeChunks,
				m_InfomodeActiveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfomodeActive_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_InfoviewObjectStatusType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewObjectStatusData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ControllerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_BuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ColorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Color_RW_ComponentLookup, ref base.CheckedStateRef)
			};
			UpdateTempObjectColorsJob jobData3 = new UpdateTempObjectColorsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ColorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Color_RW_ComponentLookup, ref base.CheckedStateRef)
			};
			UpdateSubObjectColorsJob jobData4 = new UpdateSubObjectColorsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ElevationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Elevation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TreeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Tree_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PlantType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Plant_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
				m_VehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Vehicle_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ColorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Color_RW_ComponentLookup, ref base.CheckedStateRef)
			};
			JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_ObjectQuery, JobHandle.CombineDependencies(base.Dependency, JobHandle.CombineDependencies(dependencies2, dependencies, outJobHandle)));
			JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(jobData2, m_MiddleObjectQuery, jobHandle);
			JobHandle dependency = JobChunkExtensions.ScheduleParallel(dependsOn: JobChunkExtensions.ScheduleParallel(jobData3, m_TempObjectQuery, jobHandle2), jobData: jobData4, query: m_SubObjectQuery);
			infomodeChunks.Dispose(jobHandle2);
			m_LocalEffectSystem.AddLocalEffectReader(jobHandle);
			m_TelecomCoverageSystem.AddReader(jobHandle);
			base.Dependency = dependency;
		}
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
	public ObjectColorSystem()
	{
	}
}

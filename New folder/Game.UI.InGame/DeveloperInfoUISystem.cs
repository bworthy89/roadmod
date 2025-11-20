using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Colossal;
using Colossal.Entities;
using Colossal.Rendering;
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
using Game.Notifications;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Rendering;
using Game.Routes;
using Game.Simulation;
using Game.Vehicles;
using Game.Zones;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class DeveloperInfoUISystem : UISystemBase
{
	private struct BuildingHappinessFactorValue : IComparable<BuildingHappinessFactorValue>
	{
		public BuildingHappinessFactor m_Factor;

		public int m_Value;

		public int CompareTo(BuildingHappinessFactorValue other)
		{
			return -math.abs(m_Value).CompareTo(math.abs(other.m_Value));
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ConsumptionData> __Game_Prefabs_ConsumptionData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> __Game_Buildings_ElectricityConsumer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaterConsumer> __Game_Buildings_WaterConsumer_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.ServiceCoverage> __Game_Net_ServiceCoverage_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Locked> __Game_Prefabs_Locked_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GarbageProducer> __Game_Buildings_GarbageProducer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CrimeProducer> __Game_Buildings_CrimeProducer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MailProducer> __Game_Buildings_MailProducer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<OfficeBuilding> __Game_Prefabs_OfficeBuilding_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<CompanyData> __Game_Companies_CompanyData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WorkProvider> __Game_Companies_WorkProvider_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Employee> __Game_Companies_Employee_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<WorkplaceData> __Game_Prefabs_WorkplaceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceAvailable> __Game_Companies_ServiceAvailable_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ZonePropertiesData> __Game_Prefabs_ZonePropertiesData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Efficiency> __Game_Buildings_Efficiency_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<ServiceCompanyData> __Game_Companies_ServiceCompanyData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ResourceAvailability> __Game_Net_ResourceAvailability_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<TradeCost> __Game_Companies_TradeCost_RO_BufferLookup;

		public BufferLookup<HappinessFactorParameterData> __Game_Prefabs_HappinessFactorParameterData_RW_BufferLookup;

		[ReadOnly]
		public ComponentLookup<PollutionData> __Game_Prefabs_PollutionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PollutionModifierData> __Game_Prefabs_PollutionModifierData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PollutionEmitModifier> __Game_Buildings_PollutionEmitModifier_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Worker> __Game_Citizens_Worker_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> __Game_Vehicles_DeliveryTruck_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Extractor> __Game_Areas_Extractor_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Geometry> __Game_Areas_Geometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Areas.Lot> __Game_Areas_Lot_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ExtractorAreaData> __Game_Prefabs_ExtractorAreaData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ExtractorCompanyData> __Game_Prefabs_ExtractorCompanyData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TouristHousehold> __Game_Citizens_TouristHousehold_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Citizens.Student> __Game_Citizens_Student_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Prefabs_ConsumptionData_RO_ComponentLookup = state.GetComponentLookup<ConsumptionData>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Buildings_ElectricityConsumer_RO_ComponentLookup = state.GetComponentLookup<ElectricityConsumer>(isReadOnly: true);
			__Game_Buildings_WaterConsumer_RO_ComponentLookup = state.GetComponentLookup<WaterConsumer>(isReadOnly: true);
			__Game_Net_ServiceCoverage_RO_BufferLookup = state.GetBufferLookup<Game.Net.ServiceCoverage>(isReadOnly: true);
			__Game_Prefabs_Locked_RO_ComponentLookup = state.GetComponentLookup<Locked>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Buildings_GarbageProducer_RO_ComponentLookup = state.GetComponentLookup<GarbageProducer>(isReadOnly: true);
			__Game_Buildings_CrimeProducer_RO_ComponentLookup = state.GetComponentLookup<CrimeProducer>(isReadOnly: true);
			__Game_Buildings_MailProducer_RO_ComponentLookup = state.GetComponentLookup<MailProducer>(isReadOnly: true);
			__Game_Prefabs_OfficeBuilding_RO_ComponentLookup = state.GetComponentLookup<OfficeBuilding>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(isReadOnly: true);
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Companies_CompanyData_RO_ComponentLookup = state.GetComponentLookup<CompanyData>(isReadOnly: true);
			__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
			__Game_Companies_WorkProvider_RO_ComponentLookup = state.GetComponentLookup<WorkProvider>(isReadOnly: true);
			__Game_Companies_Employee_RO_BufferLookup = state.GetBufferLookup<Employee>(isReadOnly: true);
			__Game_Prefabs_WorkplaceData_RO_ComponentLookup = state.GetComponentLookup<WorkplaceData>(isReadOnly: true);
			__Game_Citizens_HealthProblem_RO_ComponentLookup = state.GetComponentLookup<HealthProblem>(isReadOnly: true);
			__Game_Companies_ServiceAvailable_RO_ComponentLookup = state.GetComponentLookup<ServiceAvailable>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
			__Game_Prefabs_ZonePropertiesData_RO_ComponentLookup = state.GetComponentLookup<ZonePropertiesData>(isReadOnly: true);
			__Game_Buildings_Efficiency_RO_BufferLookup = state.GetBufferLookup<Efficiency>(isReadOnly: true);
			__Game_Companies_ServiceCompanyData_RO_ComponentLookup = state.GetComponentLookup<ServiceCompanyData>(isReadOnly: true);
			__Game_Net_ResourceAvailability_RO_BufferLookup = state.GetBufferLookup<ResourceAvailability>(isReadOnly: true);
			__Game_Companies_TradeCost_RO_BufferLookup = state.GetBufferLookup<TradeCost>(isReadOnly: true);
			__Game_Prefabs_HappinessFactorParameterData_RW_BufferLookup = state.GetBufferLookup<HappinessFactorParameterData>();
			__Game_Prefabs_PollutionData_RO_ComponentLookup = state.GetComponentLookup<PollutionData>(isReadOnly: true);
			__Game_Prefabs_PollutionModifierData_RO_ComponentLookup = state.GetComponentLookup<PollutionModifierData>(isReadOnly: true);
			__Game_Prefabs_ZoneData_RO_ComponentLookup = state.GetComponentLookup<ZoneData>(isReadOnly: true);
			__Game_Buildings_PollutionEmitModifier_RO_ComponentLookup = state.GetComponentLookup<PollutionEmitModifier>(isReadOnly: true);
			__Game_Citizens_Worker_RO_ComponentLookup = state.GetComponentLookup<Worker>(isReadOnly: true);
			__Game_Vehicles_DeliveryTruck_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.DeliveryTruck>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Areas_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Areas.SubArea>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
			__Game_Areas_Extractor_RO_ComponentLookup = state.GetComponentLookup<Extractor>(isReadOnly: true);
			__Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(isReadOnly: true);
			__Game_Areas_Geometry_RO_ComponentLookup = state.GetComponentLookup<Geometry>(isReadOnly: true);
			__Game_Areas_Lot_RO_ComponentLookup = state.GetComponentLookup<Game.Areas.Lot>(isReadOnly: true);
			__Game_Prefabs_ExtractorAreaData_RO_ComponentLookup = state.GetComponentLookup<ExtractorAreaData>(isReadOnly: true);
			__Game_Prefabs_ExtractorCompanyData_RO_ComponentLookup = state.GetComponentLookup<ExtractorCompanyData>(isReadOnly: true);
			__Game_Citizens_TouristHousehold_RO_ComponentLookup = state.GetComponentLookup<TouristHousehold>(isReadOnly: true);
			__Game_Citizens_Student_RO_ComponentLookup = state.GetComponentLookup<Game.Citizens.Student>(isReadOnly: true);
		}
	}

	protected CitySystem m_CitySystem;

	protected NameSystem m_NameSystem;

	protected PrefabSystem m_PrefabSystem;

	protected ResourceSystem m_ResourceSystem;

	protected SimulationSystem m_SimulationSystem;

	protected SelectedInfoUISystem m_InfoUISystem;

	protected GroundPollutionSystem m_GroundPollutionSystem;

	protected AirPollutionSystem m_AirPollutionSystem;

	protected NoisePollutionSystem m_NoisePollutionSystem;

	protected TelecomCoverageSystem m_TelecomCoverageSystem;

	protected TaxSystem m_TaxSystem;

	protected BatchManagerSystem m_BatchManagerSystem;

	protected CitizenHappinessSystem m_CitizenHappinessSystem;

	protected EntityQuery m_CitizenHappinessParameterQuery;

	protected EntityQuery m_HealthcareParameterQuery;

	protected EntityQuery m_ParkParameterQuery;

	protected EntityQuery m_EducationParameterQuery;

	protected EntityQuery m_TelecomParameterQuery;

	protected EntityQuery m_HappinessFactorParameterQuery;

	protected EntityQuery m_EconomyParameterQuery;

	protected EntityQuery m_ProcessQuery;

	protected EntityQuery m_TimeDataQuery;

	protected EntityQuery m_GarbageParameterQuery;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_746694607_0;

	private EntityQuery __query_746694607_1;

	private EntityQuery __query_746694607_2;

	private EntityQuery __query_746694607_3;

	private EntityQuery __query_746694607_4;

	private EntityQuery __query_746694607_5;

	private EntityQuery __query_746694607_6;

	private EntityQuery __query_746694607_7;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CitizenHappinessParameterQuery = GetEntityQuery(ComponentType.ReadOnly<CitizenHappinessParameterData>());
		m_HealthcareParameterQuery = GetEntityQuery(ComponentType.ReadOnly<HealthcareParameterData>());
		m_ParkParameterQuery = GetEntityQuery(ComponentType.ReadOnly<ParkParameterData>());
		m_EducationParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EducationParameterData>());
		m_TelecomParameterQuery = GetEntityQuery(ComponentType.ReadOnly<TelecomParameterData>());
		m_HappinessFactorParameterQuery = GetEntityQuery(ComponentType.ReadOnly<HappinessFactorParameterData>());
		m_GarbageParameterQuery = GetEntityQuery(ComponentType.ReadOnly<GarbageParameterData>());
		m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		m_ProcessQuery = GetEntityQuery(ComponentType.ReadOnly<IndustrialProcessData>());
		m_TimeDataQuery = GetEntityQuery(ComponentType.ReadOnly<TimeData>());
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_NameSystem = base.World.GetOrCreateSystemManaged<NameSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_InfoUISystem = base.World.GetOrCreateSystemManaged<SelectedInfoUISystem>();
		m_GroundPollutionSystem = base.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
		m_AirPollutionSystem = base.World.GetOrCreateSystemManaged<AirPollutionSystem>();
		m_NoisePollutionSystem = base.World.GetOrCreateSystemManaged<NoisePollutionSystem>();
		m_TelecomCoverageSystem = base.World.GetOrCreateSystemManaged<TelecomCoverageSystem>();
		m_TaxSystem = base.World.GetOrCreateSystemManaged<TaxSystem>();
		m_BatchManagerSystem = base.World.GetOrCreateSystemManaged<BatchManagerSystem>();
		m_CitizenHappinessSystem = base.World.GetOrCreateSystemManaged<CitizenHappinessSystem>();
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasEntityInfo, UpdateEntityInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasContentPrerequisite, UpdateContentPrerequisite));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasMeshGroupInfo, UpdateMeshGroupInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasBatchInfo, UpdateBatchInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasAddressInfo, UpdateAddressInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasCrimeInfo, UpdateCrimeInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasZoneInfo, UpdateZoneInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasZoneInfo, UpdateZoneHappinessInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasTelecomRangeInfo, UpdateTelecomRangeInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasPollutionInfo, UpdatePollutionInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasGarbageInfo, UpdateGarbageInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasWaterConsumeInfo, UpdateWaterConsumeInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasSpectatorSiteInfo, UpdateSpectatorSiteInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasInDangerInfo, UpdateInDangerInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasParkInfo, UpdateParkInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasCompanyInfo, UpdateCompanyInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasEfficiencyInfo, UpdateEfficiencyInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasElectricityProductionInfo, UpdateElectricityProductionInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasBatteriesInfo, UpdateBatteriesInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasGarbageProcessingInfo, UpdateGarbageProcessingInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasMailProcessingSpeedInfo, UpdateMailProcessingSpeedInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasMailInfo, UpdateMailInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasRentInfo, UpdateRentInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasTradeCostInfo, UpdateTradeCostInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasTradePartnerInfo, UpdateTradePartnerInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasWarehouseInfo, UpdateWarehouseInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasCompanyEconomyInfo, UpdateCompanyEconomyInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasCompanyProfitInfo, UpdateCompanyProfitInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasBuildingLevelInfo, UpdateBuildingLevelInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasElectricityConsumeInfo, UpdateElectricityConsumeInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasSendReceiveMailInfo, UpdateSendReceiveMailInfo));
		m_InfoUISystem.AddDeveloperInfo(new CapacityInfo(HasNetworkCapacityInfo, UpdateNetworkCapacityInfo));
		m_InfoUISystem.AddDeveloperInfo(new CapacityInfo(HasShelterInfo, UpdateShelterInfo));
		m_InfoUISystem.AddDeveloperInfo(new CapacityInfo(HasPoliceInfo, UpdatePoliceInfo));
		m_InfoUISystem.AddDeveloperInfo(new CapacityInfo(HasDeathcareInfo, UpdateDeathcareInfo));
		m_InfoUISystem.AddDeveloperInfo(new CapacityInfo(HasStoredGarbageInfo, UpdateStoredGarbageInfo));
		m_InfoUISystem.AddDeveloperInfo(new CapacityInfo(HasStoredMailInfo, UpdateStoredMailInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasPrisonInfo, UpdatePrisonInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasParkingInfo, UpdateParkingInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasVehicleInfo, UpdateVehicleInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasResourceProductionInfo, UpdateResourceProductionInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasHouseholdsInfo, UpdateHouseholdsInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasHouseholdInfo, UpdateHouseholdInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasHomelessInfo, UpdateHomelessInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasResidentsInfo, UpdateResidentInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasEmployeesInfo, UpdateEmployeesInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasPatientsInfo, UpdatePatientsInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasStudentsInfo, UpdateStudentsInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasStoredResourcesInfo, UpdateStoredResourcesInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasHouseholdPetsInfo, UpdateHouseholdPetsInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasServiceCompanyInfo, UpdateServiceCompanyInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasExtractorCompanyInfo, UpdateExtractorCompanyInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasProcessingCompanyInfo, UpdateProcessingCompanyInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasStorageInfo, UpdateStorageInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasTransferRequestInfo, UpdateTransferRequestInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasOwnerInfo, UpdateOwnerInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasKeeperInfo, UpdateKeeperInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasControllerInfo, UpdateControllerInfo));
		m_InfoUISystem.AddDeveloperInfo(new CapacityInfo(HasPassengerInfo, UpdatePassengerInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasPersonalCarInfo, UpdatePersonalCarInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasDeliveryTruckInfo, UpdateDeliveryTruckInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasAmbulanceInfo, UpdateAmbulanceInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasHearseInfo, UpdateHearseInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasGarbageTruckInfo, UpdateGarbageTruckInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasPublicTransportInfo, UpdatePublicTransportInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasCargoTransportInfo, UpdateCargoTransportInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasMaintenanceVehicleInfo, UpdateMaintenanceVehicleInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasPostVanInfo, UpdatePostVanInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasFireEngineInfo, UpdateFireEngineInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasPoliceCarInfo, UpdatePoliceCarInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasCitizenInfo, UpdateCitizenInfo));
		m_InfoUISystem.AddDeveloperInfo(new CapacityInfo(HasMailSenderInfo, UpdateMailSenderInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasAnimalInfo, UpdateAnimalInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasCreatureInfo, UpdateCreatureInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasGroupLeaderInfo, UpdateGroupLeaderInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasGroupMemberInfo, UpdateGroupMemberInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasAreaInfo, UpdateAreaInfo));
		m_InfoUISystem.AddDeveloperInfo(new CapacityInfo(HasTreeInfo, UpdateTreeInfo));
		m_InfoUISystem.AddDeveloperInfo(new CapacityInfo(HasMailBoxInfo, UpdateMailBoxInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasBoardingVehicleInfo, UpdateBoardingVehicleInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasWaitingPassengerInfo, UpdateWaitingPassengerInfo));
		m_InfoUISystem.AddDeveloperInfo(new CapacityInfo(HasMovingInfo, UpdateMovingInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasDamagedInfo, UpdateDamagedInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasDestroyedInfo, UpdateDestroyedInfo));
		m_InfoUISystem.AddDeveloperInfo(new CapacityInfo(HasDestroyedBuildingInfo, UpdateDestroyedBuildingInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasOnFireInfo, UpdateOnFireInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasFacingWeatherInfo, UpdateFacingWeatherInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasAccidentSiteInfo, UpdateAccidentSiteInfo));
		m_InfoUISystem.AddDeveloperInfo(new GenericInfo(HasInvolvedInAccidentInfo, UpdateInvolvedInAccidentInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasFloodedInfo, UpdateFloodedInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasEventInfo, UpdateEventInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasNotificationInfo, UpdateNotificationInfo));
		m_InfoUISystem.AddDeveloperInfo(new InfoList(HasVehicleModelInfo, UpdateVehicleModelInfo));
	}

	[Preserve]
	protected override void OnUpdate()
	{
	}

	protected void AddUpgradeData<T>(Entity entity, ref T data) where T : unmanaged, IComponentData, ICombineData<T>
	{
		if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<InstalledUpgrade> buffer))
		{
			UpgradeUtils.CombineStats(base.EntityManager, ref data, buffer);
		}
	}

	private bool HasContentPrerequisite(Entity entity, Entity prefab)
	{
		if (prefab != InfoList.Item.kNullEntity)
		{
			return base.EntityManager.HasComponent<ContentPrerequisiteData>(prefab);
		}
		return false;
	}

	private void UpdateContentPrerequisite(Entity entity, Entity prefab, InfoList info)
	{
		info.label = "ContentPrerequisite".Nicify();
		if (base.EntityManager.TryGetComponent<ContentPrerequisiteData>(prefab, out var component))
		{
			if (m_PrefabSystem.TryGetPrefab<ContentPrefab>(component.m_ContentPrerequisite, out var prefab2))
			{
				foreach (ComponentBase component2 in prefab2.components)
				{
					info.Add(new InfoList.Item(component2.GetDebugString(), InfoList.Item.kNullEntity));
				}
				return;
			}
			info.Add(new InfoList.Item(m_PrefabSystem.GetPrefabName(component.m_ContentPrerequisite), InfoList.Item.kNullEntity));
		}
		else
		{
			info.label = "Missing or invalid ContentPrefab";
		}
	}

	private bool HasEntityInfo(Entity entity, Entity prefab)
	{
		return entity != InfoList.Item.kNullEntity;
	}

	private void UpdateEntityInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		info.label = "Entity";
		info.value = m_NameSystem.GetDebugName(entity);
	}

	private bool HasMeshPrefabInfo(Entity entity, Entity prefab)
	{
		if (prefab != InfoList.Item.kNullEntity)
		{
			ObjectGeometryPrefab prefab2 = m_PrefabSystem.GetPrefab<ObjectGeometryPrefab>(prefab);
			if (prefab2 != null)
			{
				return prefab2.m_Meshes.Length != 0;
			}
		}
		return false;
	}

	private void UpdateMeshPrefabInfo(Entity entity, Entity prefab, InfoList info)
	{
		info.label = "MeshPrefabs";
		SubMeshFlags subMeshFlags = (SubMeshFlags)0u;
		if (base.EntityManager.TryGetComponent<Tree>(entity, out var component))
		{
			subMeshFlags = ((!base.EntityManager.TryGetComponent<GrowthScaleData>(prefab, out var component2)) ? (subMeshFlags | SubMeshFlags.RequireAdult) : (subMeshFlags | BatchDataHelpers.CalculateTreeSubMeshData(component, component2, out var _)));
		}
		ObjectGeometryPrefab prefab2 = m_PrefabSystem.GetPrefab<ObjectGeometryPrefab>(prefab);
		for (int i = 0; i < prefab2.m_Meshes.Length; i++)
		{
			ObjectState requireState = prefab2.m_Meshes[i].m_RequireState;
			string text = prefab2.m_Meshes[i].m_Mesh.name;
			if ((requireState == ObjectState.Child && (subMeshFlags & SubMeshFlags.RequireChild) == SubMeshFlags.RequireChild) || (requireState == ObjectState.Teen && (subMeshFlags & SubMeshFlags.RequireTeen) == SubMeshFlags.RequireTeen) || (requireState == ObjectState.Adult && (subMeshFlags & SubMeshFlags.RequireAdult) == SubMeshFlags.RequireAdult) || (requireState == ObjectState.Elderly && (subMeshFlags & SubMeshFlags.RequireElderly) == SubMeshFlags.RequireElderly) || (requireState == ObjectState.Dead && (subMeshFlags & SubMeshFlags.RequireDead) == SubMeshFlags.RequireDead) || (requireState == ObjectState.Stump && (subMeshFlags & SubMeshFlags.RequireStump) == SubMeshFlags.RequireStump))
			{
				text += " [X]";
			}
			info.Add(new InfoList.Item(text, InfoList.Item.kNullEntity));
		}
	}

	private bool HasMeshGroupInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<MeshGroup>(entity))
		{
			if (base.EntityManager.TryGetComponent<CurrentTransport>(entity, out var component))
			{
				return base.EntityManager.HasComponent<MeshGroup>(component.m_CurrentTransport);
			}
			return false;
		}
		return true;
	}

	private void UpdateMeshGroupInfo(Entity entity, Entity prefab, InfoList info)
	{
		if (base.EntityManager.TryGetComponent<CurrentTransport>(entity, out var component))
		{
			entity = component.m_CurrentTransport;
			if (base.EntityManager.TryGetComponent<PrefabRef>(entity, out var component2))
			{
				prefab = component2.m_Prefab;
			}
		}
		if (!base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<MeshGroup> buffer) || !m_PrefabSystem.TryGetPrefab<ObjectGeometryPrefab>(prefab, out var prefab2) || prefab2.m_Meshes == null)
		{
			return;
		}
		base.EntityManager.TryGetComponent<CreatureData>(prefab, out var component3);
		info.label = $"Mesh groups ({buffer.Length})";
		for (int i = 0; i < buffer.Length; i++)
		{
			int subMeshGroup = buffer[i].m_SubMeshGroup;
			int num = 0;
			while (true)
			{
				int num2;
				CharacterGroup.OverrideInfo overrideInfo;
				int num3;
				if (num < prefab2.m_Meshes.Length)
				{
					if (prefab2.m_Meshes[num].m_Mesh is CharacterGroup { m_Characters: not null } characterGroup)
					{
						num2 = 0;
						while (num2 < characterGroup.m_Characters.Length)
						{
							if ((characterGroup.m_Characters[num2].m_Style.m_Gender & component3.m_Gender) != component3.m_Gender || subMeshGroup-- != 0)
							{
								num2++;
								continue;
							}
							goto IL_0108;
						}
						if (characterGroup.m_Overrides != null)
						{
							for (int j = 0; j < characterGroup.m_Overrides.Length; j++)
							{
								overrideInfo = characterGroup.m_Overrides[j];
								num3 = 0;
								while (num3 < characterGroup.m_Characters.Length)
								{
									if ((characterGroup.m_Characters[num3].m_Style.m_Gender & component3.m_Gender) != component3.m_Gender || subMeshGroup-- != 0)
									{
										num3++;
										continue;
									}
									goto IL_0196;
								}
							}
						}
					}
					num++;
					continue;
				}
				info.Add(new InfoList.Item($"Unknown group #{buffer[i].m_SubMeshGroup}"));
				break;
				IL_0196:
				info.Add(new InfoList.Item($"{characterGroup.name} #{num3} ({overrideInfo.m_Group.name})"));
				break;
				IL_0108:
				info.Add(new InfoList.Item($"{characterGroup.name} #{num2}"));
				break;
			}
		}
	}

	private bool HasBatchInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<MeshBatch>(entity))
		{
			return base.EntityManager.HasComponent<CurrentTransport>(entity);
		}
		return true;
	}

	private void UpdateBatchInfo(Entity entity, Entity prefab, InfoList info)
	{
		if (base.EntityManager.TryGetComponent<CurrentTransport>(entity, out var component))
		{
			entity = component.m_CurrentTransport;
		}
		if (!base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<MeshBatch> buffer))
		{
			return;
		}
		JobHandle dependencies;
		NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData> nativeBatchGroups = m_BatchManagerSystem.GetNativeBatchGroups(readOnly: true, out dependencies);
		JobHandle dependencies2;
		NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData> nativeBatchInstances = m_BatchManagerSystem.GetNativeBatchInstances(readOnly: true, out dependencies2);
		ManagedBatches<OptionalProperties> managedBatches = m_BatchManagerSystem.GetManagedBatches();
		dependencies.Complete();
		dependencies2.Complete();
		int num = 0;
		for (int i = 0; i < buffer.Length; i++)
		{
			num += nativeBatchGroups.GetBatchCount(buffer[i].m_GroupIndex);
		}
		info.label = $"Batches ({num})";
		for (int j = 0; j < buffer.Length; j++)
		{
			MeshBatch meshBatch = buffer[j];
			GroupData groupData = nativeBatchGroups.GetGroupData(meshBatch.m_GroupIndex);
			num = nativeBatchGroups.GetBatchCount(meshBatch.m_GroupIndex);
			int mergedInstanceGroupIndex = nativeBatchInstances.GetMergedInstanceGroupIndex(meshBatch.m_GroupIndex, meshBatch.m_InstanceIndex);
			int num2 = -1;
			for (int k = 0; k < num; k++)
			{
				BatchData batchData = nativeBatchGroups.GetBatchData(meshBatch.m_GroupIndex, k);
				int managedBatchIndex = nativeBatchGroups.GetManagedBatchIndex(meshBatch.m_GroupIndex, k);
				int num3 = -1;
				if (mergedInstanceGroupIndex >= 0)
				{
					num3 = nativeBatchGroups.GetManagedBatchIndex(mergedInstanceGroupIndex, k);
				}
				if (batchData.m_LodIndex != num2)
				{
					num2 = batchData.m_LodIndex;
					info.Add(new InfoList.Item($"--- Mesh {meshBatch.m_MeshIndex}, Tile {meshBatch.m_TileIndex}, Layer {groupData.m_Layer}, Lod {batchData.m_LodIndex} ---"));
				}
				if (managedBatchIndex < 0)
				{
					continue;
				}
				CustomBatch customBatch = (CustomBatch)managedBatches.GetBatch(managedBatchIndex);
				RenderPrefab prefab4;
				if (num3 >= 0)
				{
					CustomBatch customBatch2 = (CustomBatch)managedBatches.GetBatch(num3);
					if (m_PrefabSystem.TryGetPrefab<RenderPrefab>(customBatch.sourceMeshEntity, out var prefab2) && m_PrefabSystem.TryGetPrefab<RenderPrefab>(customBatch2.sourceMeshEntity, out var prefab3))
					{
						if (customBatch.generatedType != GeneratedType.None)
						{
							info.Add(new InfoList.Item($"{prefab3.name} {customBatch2.generatedType} -> {prefab2.name} {customBatch.generatedType}"));
						}
						else
						{
							info.Add(new InfoList.Item($"{prefab3.name}[{customBatch2.sourceSubMeshIndex}] -> {prefab2.name}[{customBatch.sourceSubMeshIndex}]"));
						}
					}
					else
					{
						info.Add(new InfoList.Item(customBatch2.mesh.name + " -> " + customBatch.mesh.name));
					}
				}
				else if (m_PrefabSystem.TryGetPrefab<RenderPrefab>(customBatch.sourceMeshEntity, out prefab4))
				{
					if (customBatch.generatedType != GeneratedType.None)
					{
						info.Add(new InfoList.Item($"{prefab4.name} {customBatch.generatedType}"));
					}
					else
					{
						info.Add(new InfoList.Item($"{prefab4.name}[{customBatch.sourceSubMeshIndex}]"));
					}
				}
				else
				{
					info.Add(new InfoList.Item(customBatch.mesh.name));
				}
			}
		}
	}

	private bool HasAddressInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<Building>(entity))
		{
			return base.EntityManager.HasComponent<Attached>(entity);
		}
		return true;
	}

	private void UpdateAddressInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		info.label = "Address";
		if (BuildingUtils.GetAddress(base.EntityManager, entity, out var road, out var number))
		{
			info.value = m_NameSystem.GetDebugName(road) + " " + number;
			info.target = road;
		}
		else
		{
			info.value = "Unknown";
			info.target = InfoList.Item.kNullEntity;
		}
	}

	private bool HasCrimeInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.HasComponent<Building>(entity))
		{
			return base.EntityManager.HasComponent<CrimeProducer>(entity);
		}
		return false;
	}

	private void UpdateCrimeInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		info.label = "Crime";
		info.value = $"Accumulate: ({base.EntityManager.GetComponentData<CrimeProducer>(entity).m_Crime})";
	}

	private bool HasHomelessInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.HasComponent<Building>(entity) && base.EntityManager.HasComponent<Renter>(entity))
		{
			if (!base.EntityManager.HasComponent<Game.Buildings.Park>(entity))
			{
				return base.EntityManager.HasComponent<Abandoned>(entity);
			}
			return true;
		}
		return false;
	}

	private void UpdateHomelessInfo(Entity entity, Entity prefab, InfoList info)
	{
		info.label = "Homeless";
		DynamicBuffer<Renter> buffer = base.EntityManager.GetBuffer<Renter>(entity, isReadOnly: true);
		ComponentLookup<BuildingData> buildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<BuildingPropertyData> buildingPropertyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref base.CheckedStateRef);
		bool flag = false;
		if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Game.Objects.SubObject> buffer2))
		{
			for (int i = 0; i < buffer2.Length; i++)
			{
				if (!base.EntityManager.TryGetComponent<PrefabRef>(buffer2[i].m_SubObject, out var component) || !base.EntityManager.TryGetBuffer(component.m_Prefab, isReadOnly: true, out DynamicBuffer<ObjectRequirementElement> buffer3))
				{
					continue;
				}
				for (int j = 0; j < buffer3.Length; j++)
				{
					if ((buffer3[j].m_RequireFlags & ObjectRequirementFlags.Homeless) != 0)
					{
						flag = true;
						break;
					}
				}
			}
		}
		info.label = $"Homeless Count: ({buffer.Length}) MaxCapacity:{BuildingUtils.GetShelterHomelessCapacity(prefab, ref buildingDatas, ref buildingPropertyDatas)}";
		info.Add(new InfoList.Item($"HaveTent:{flag} "));
		for (int k = 0; k < buffer.Length; k++)
		{
			Entity renter = buffer[k].m_Renter;
			if (base.EntityManager.HasComponent<Household>(renter))
			{
				info.Add(new InfoList.Item(m_NameSystem.GetDebugName(renter), renter));
			}
		}
	}

	private bool HasTelecomRangeInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<Building>(entity) || !base.EntityManager.HasComponent<Game.Buildings.TelecomFacility>(entity) || !base.EntityManager.TryGetComponent<TelecomFacilityData>(prefab, out var data))
		{
			return false;
		}
		AddUpgradeData(entity, ref data);
		return data.m_Range >= 1f;
	}

	private void UpdateTelecomRangeInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		TelecomFacilityData data = base.EntityManager.GetComponentData<TelecomFacilityData>(prefab);
		AddUpgradeData(entity, ref data);
		float x = 1f;
		if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Efficiency> buffer))
		{
			x = BuildingUtils.GetEfficiency(buffer);
		}
		float f = data.m_Range * math.sqrt(x);
		info.label = "Range";
		info.value = Mathf.RoundToInt(f) + "/" + Mathf.RoundToInt(data.m_Range);
		info.target = InfoList.Item.kNullEntity;
	}

	private bool HasNetworkCapacityInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<Building>(entity) || !base.EntityManager.HasComponent<Game.Buildings.TelecomFacility>(entity) || !base.EntityManager.TryGetComponent<TelecomFacilityData>(prefab, out var data))
		{
			return false;
		}
		AddUpgradeData(entity, ref data);
		return data.m_NetworkCapacity >= 1f;
	}

	private void UpdateNetworkCapacityInfo(Entity entity, Entity prefab, CapacityInfo info)
	{
		TelecomFacilityData data = base.EntityManager.GetComponentData<TelecomFacilityData>(prefab);
		AddUpgradeData(entity, ref data);
		float num = 1f;
		if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Efficiency> buffer))
		{
			num = BuildingUtils.GetEfficiency(buffer);
		}
		DynamicBuffer<CityModifier> buffer2 = base.EntityManager.GetBuffer<CityModifier>(m_CitySystem.City, isReadOnly: true);
		CityUtils.ApplyModifier(ref data.m_NetworkCapacity, buffer2, CityModifierType.TelecomCapacity);
		float f = data.m_NetworkCapacity * num;
		info.label = "Network Capacity";
		info.value = Mathf.RoundToInt(f);
		info.max = info.value;
	}

	private bool HasZoneInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.HasComponent<Building>(entity) && base.EntityManager.HasComponent<Renter>(entity))
		{
			if (base.EntityManager.HasComponent<SpawnableBuildingData>(prefab))
			{
				return base.EntityManager.HasComponent<BuildingData>(prefab);
			}
			return false;
		}
		if (base.EntityManager.HasComponent<Household>(entity) || base.EntityManager.HasComponent<CompanyData>(entity))
		{
			if (base.EntityManager.HasComponent<PropertyRenter>(entity))
			{
				return base.EntityManager.HasComponent<SpawnableBuildingData>(base.EntityManager.GetComponentData<PrefabRef>(base.EntityManager.GetComponentData<PropertyRenter>(entity).m_Property).m_Prefab);
			}
			return false;
		}
		return false;
	}

	private void UpdateZoneInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		if (!base.EntityManager.HasComponent<Building>(entity))
		{
			entity = base.EntityManager.GetComponentData<PropertyRenter>(entity).m_Property;
			prefab = base.EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab;
		}
		SpawnableBuildingData componentData = base.EntityManager.GetComponentData<SpawnableBuildingData>(prefab);
		BuildingData componentData2 = base.EntityManager.GetComponentData<BuildingData>(prefab);
		ZoneData componentData3 = base.EntityManager.GetComponentData<ZoneData>(componentData.m_ZonePrefab);
		ZoneDensity zoneDensity = (ZoneDensity)255;
		if (base.EntityManager.TryGetComponent<ZonePropertiesData>(componentData.m_ZonePrefab, out var component))
		{
			zoneDensity = PropertyUtils.GetZoneDensity(componentData3, component);
		}
		string prefabName = m_PrefabSystem.GetPrefabName(componentData.m_ZonePrefab);
		info.label = "Zone Info";
		info.value = prefabName + " " + componentData2.m_LotSize.x + "x" + componentData2.m_LotSize.y + " Density:" + ((zoneDensity == (ZoneDensity)255) ? "N/A" : Enum.GetName(typeof(ZoneDensity), zoneDensity));
		info.target = InfoList.Item.kNullEntity;
	}

	private bool HasZoneHappinessInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.HasComponent<Building>(entity))
		{
			if (base.EntityManager.HasComponent<SpawnableBuildingData>(prefab))
			{
				return base.EntityManager.HasComponent<BuildingData>(prefab);
			}
			return false;
		}
		return false;
	}

	private void UpdateZoneHappinessInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		NativeArray<Entity> processes = m_ProcessQuery.ToEntityArray(Allocator.TempJob);
		ComponentLookup<PrefabRef> prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<SpawnableBuildingData> spawnableBuildings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<BuildingPropertyData> buildingPropertyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<ConsumptionData> consumptionDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ConsumptionData_RO_ComponentLookup, ref base.CheckedStateRef);
		BufferLookup<CityModifier> cityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef);
		ComponentLookup<Building> buildings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<ElectricityConsumer> electricityConsumers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<WaterConsumer> waterConsumers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentLookup, ref base.CheckedStateRef);
		BufferLookup<Game.Net.ServiceCoverage> serviceCoverages = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ServiceCoverage_RO_BufferLookup, ref base.CheckedStateRef);
		ComponentLookup<Locked> locked = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_Locked_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<Game.Objects.Transform> transforms = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<GarbageProducer> garbageProducers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_GarbageProducer_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<CrimeProducer> crimeProducers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_CrimeProducer_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<MailProducer> mailProducers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_MailProducer_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<OfficeBuilding> officeBuildings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_OfficeBuilding_RO_ComponentLookup, ref base.CheckedStateRef);
		BufferLookup<Renter> renters = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef);
		ComponentLookup<Citizen> citizenDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef);
		BufferLookup<HouseholdCitizen> householdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef);
		ComponentLookup<BuildingData> buildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<CompanyData> companies = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_CompanyData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<IndustrialProcessData> industrialProcessDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<WorkProvider> workProviders = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_WorkProvider_RO_ComponentLookup, ref base.CheckedStateRef);
		BufferLookup<Employee> employees = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_Employee_RO_BufferLookup, ref base.CheckedStateRef);
		ComponentLookup<WorkplaceData> workplaceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<Citizen> citizens = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<HealthProblem> healthProblems = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<ServiceAvailable> serviceAvailables = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_ServiceAvailable_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<ResourceData> resourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<ZonePropertiesData> zonePropertiesDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZonePropertiesData_RO_ComponentLookup, ref base.CheckedStateRef);
		BufferLookup<Efficiency> efficiencies = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferLookup, ref base.CheckedStateRef);
		ComponentLookup<ServiceCompanyData> serviceCompanyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_ServiceCompanyData_RO_ComponentLookup, ref base.CheckedStateRef);
		BufferLookup<ResourceAvailability> availabilities = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ResourceAvailability_RO_BufferLookup, ref base.CheckedStateRef);
		BufferLookup<TradeCost> tradeCosts = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_TradeCost_RO_BufferLookup, ref base.CheckedStateRef);
		CitizenHappinessParameterData singleton = m_CitizenHappinessParameterQuery.GetSingleton<CitizenHappinessParameterData>();
		HealthcareParameterData singleton2 = m_HealthcareParameterQuery.GetSingleton<HealthcareParameterData>();
		ParkParameterData singleton3 = m_ParkParameterQuery.GetSingleton<ParkParameterData>();
		EducationParameterData singleton4 = m_EducationParameterQuery.GetSingleton<EducationParameterData>();
		TelecomParameterData singleton5 = m_TelecomParameterQuery.GetSingleton<TelecomParameterData>();
		DynamicBuffer<HappinessFactorParameterData> bufferAfterCompletingDependency = InternalCompilerInterface.GetBufferAfterCompletingDependency(ref __TypeHandle.__Game_Prefabs_HappinessFactorParameterData_RW_BufferLookup, ref base.CheckedStateRef, m_HappinessFactorParameterQuery.GetSingletonEntity());
		GarbageParameterData singleton6 = m_GarbageParameterQuery.GetSingleton<GarbageParameterData>();
		EconomyParameterData economyParameters = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>();
		ServiceFeeParameterData singleton7 = __query_746694607_0.GetSingleton<ServiceFeeParameterData>();
		JobHandle dependencies;
		NativeArray<GroundPollution> buffer = m_GroundPollutionSystem.GetData(readOnly: true, out dependencies).m_Buffer;
		JobHandle dependencies2;
		NativeArray<NoisePollution> buffer2 = m_NoisePollutionSystem.GetData(readOnly: true, out dependencies2).m_Buffer;
		JobHandle dependencies3;
		NativeArray<AirPollution> buffer3 = m_AirPollutionSystem.GetData(readOnly: true, out dependencies3).m_Buffer;
		JobHandle dependencies4;
		CellMapData<TelecomCoverage> data = m_TelecomCoverageSystem.GetData(readOnly: true, out dependencies4);
		NativeArray<int> taxRates = m_TaxSystem.GetTaxRates();
		ResourcePrefabs prefabs2 = m_ResourceSystem.GetPrefabs();
		DynamicBuffer<ServiceFee> buffer4 = base.EntityManager.GetBuffer<ServiceFee>(m_CitySystem.City);
		float relativeElectricityFee = ServiceFeeSystem.GetFee(PlayerResource.Electricity, buffer4) / singleton7.m_ElectricityFee.m_Default;
		float relativeWaterFee = ServiceFeeSystem.GetFee(PlayerResource.Water, buffer4) / singleton7.m_WaterFee.m_Default;
		NativeArray<int> factors = new NativeArray<int>(29, Allocator.Temp);
		dependencies.Complete();
		dependencies2.Complete();
		dependencies3.Complete();
		dependencies4.Complete();
		CitizenHappinessSystem.GetBuildingHappinessFactors(entity, factors, ref prefabs, ref spawnableBuildings, ref buildingPropertyDatas, ref consumptionDatas, ref cityModifiers, ref buildings, ref electricityConsumers, ref waterConsumers, ref serviceCoverages, ref locked, ref transforms, ref garbageProducers, ref crimeProducers, ref mailProducers, ref officeBuildings, ref renters, ref citizenDatas, ref householdCitizens, ref buildingDatas, ref companies, ref industrialProcessDatas, ref workProviders, ref employees, ref workplaceDatas, ref citizens, ref healthProblems, ref serviceAvailables, ref resourceDatas, ref zonePropertiesDatas, ref efficiencies, ref serviceCompanyDatas, ref availabilities, ref tradeCosts, singleton, singleton6, singleton2, singleton3, singleton4, singleton5, ref economyParameters, bufferAfterCompletingDependency, buffer, buffer2, buffer3, data, m_CitySystem.City, taxRates, processes, prefabs2, relativeElectricityFee, relativeWaterFee);
		processes.Dispose();
		NativeList<BuildingHappinessFactorValue> list = new NativeList<BuildingHappinessFactorValue>(Allocator.Temp);
		for (int i = 0; i < factors.Length; i++)
		{
			if (factors[i] != 0)
			{
				BuildingHappinessFactorValue value = new BuildingHappinessFactorValue
				{
					m_Factor = (BuildingHappinessFactor)i,
					m_Value = factors[i]
				};
				list.Add(in value);
			}
		}
		list.Sort();
		string text = "";
		for (int j = 0; j < math.min(10, list.Length); j++)
		{
			text = text + list[j].m_Factor.ToString() + ": " + list[j].m_Value + "  ";
		}
		info.label = "Building happiness factors";
		info.value = text;
		info.target = InfoList.Item.kNullEntity;
		factors.Dispose();
		list.Dispose();
	}

	private bool HasPollutionInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.HasComponent<Building>(entity))
		{
			if (!base.EntityManager.HasComponent<PollutionData>(prefab) && !base.EntityManager.HasComponent<Abandoned>(entity) && !base.EntityManager.HasComponent<Destroyed>(entity))
			{
				return base.EntityManager.HasComponent<Game.Buildings.Park>(entity);
			}
			return true;
		}
		return false;
	}

	private void UpdatePollutionInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		CompleteDependency();
		bool destroyed = base.EntityManager.HasComponent<Destroyed>(entity);
		bool abandoned = base.EntityManager.HasComponent<Abandoned>(entity);
		bool isPark = base.EntityManager.HasComponent<Game.Buildings.Park>(entity);
		DynamicBuffer<Efficiency> buffer;
		float efficiency = (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out buffer) ? BuildingUtils.GetEfficiency(buffer) : 1f);
		base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Renter> buffer2);
		base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<InstalledUpgrade> buffer3);
		PollutionParameterData singleton = __query_746694607_1.GetSingleton<PollutionParameterData>();
		DynamicBuffer<CityModifier> singletonBuffer = __query_746694607_2.GetSingletonBuffer<CityModifier>(isReadOnly: true);
		ComponentLookup<PrefabRef> prefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<BuildingData> buildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<SpawnableBuildingData> spawnableDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<PollutionData> pollutionDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PollutionData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<PollutionModifierData> pollutionModifierDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PollutionModifierData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<ZoneData> zoneDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup, ref base.CheckedStateRef);
		BufferLookup<Employee> employees = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_Employee_RO_BufferLookup, ref base.CheckedStateRef);
		BufferLookup<HouseholdCitizen> householdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef);
		ComponentLookup<Citizen> citizens = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<PollutionEmitModifier> pollutionEmitModifiers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PollutionEmitModifier_RO_ComponentLookup, ref base.CheckedStateRef);
		PollutionData pollutionData = BuildingPollutionAddSystem.GetBuildingPollution(prefab, destroyed, abandoned, isPark, efficiency, buffer2, buffer3, singleton, singletonBuffer, ref prefabRefs, ref buildingDatas, ref spawnableDatas, ref pollutionDatas, ref pollutionModifierDatas, ref zoneDatas, ref employees, ref householdCitizens, ref citizens, ref pollutionEmitModifiers);
		if (base.EntityManager.TryGetComponent<PollutionEmitModifier>(entity, out var component))
		{
			component.UpdatePollutionData(ref pollutionData);
		}
		info.label = "Pollution";
		info.value = "Ground: " + pollutionData.m_GroundPollution + ". " + "Air: " + pollutionData.m_AirPollution + ". " + "Noise: " + pollutionData.m_NoisePollution + ".";
		info.target = InfoList.Item.kNullEntity;
	}

	private bool HasElectricityConsumeInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.HasComponent<ElectricityConsumer>(entity))
		{
			return base.EntityManager.HasComponent<ConsumptionData>(prefab);
		}
		return false;
	}

	private void UpdateElectricityConsumeInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		ConsumptionData data = base.EntityManager.GetComponentData<ConsumptionData>(prefab);
		AddUpgradeData(entity, ref data);
		ElectricityConsumer componentData = base.EntityManager.GetComponentData<ElectricityConsumer>(entity);
		info.label = "Electricity Consuming";
		info.value = $"consuming: {componentData.m_WantedConsumption}  fill: {componentData.m_FulfilledConsumption}";
	}

	private bool HasStorageInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.HasComponent<StorageLimitData>(prefab))
		{
			return base.EntityManager.HasComponent<Game.Economy.Resources>(entity);
		}
		return false;
	}

	private void UpdateStorageInfo(Entity entity, Entity prefab, InfoList info)
	{
		info.label = "Resource Storage";
		if (base.EntityManager.TryGetComponent<StorageLimitData>(prefab, out var component) && base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Game.Economy.Resources> buffer))
		{
			int num = 0;
			if (base.EntityManager.TryGetComponent<SpawnableBuildingData>(prefab, out var component2) && base.EntityManager.TryGetComponent<BuildingData>(prefab, out var component3))
			{
				num = component.GetAdjustedLimitForWarehouse(component2, component3);
			}
			else
			{
				AddUpgradeData(entity, ref component);
				num = component.m_Limit;
			}
			info.Add(new InfoList.Item($"Storage Limit: {num}"));
			int num2 = 0;
			if (base.EntityManager.TryGetComponent<StorageCompanyData>(prefab, out var component4))
			{
				AddUpgradeData(entity, ref component4);
				num2 = EconomyUtils.CountResources(component4.m_StoredResources);
			}
			int num3 = ((num2 == 0) ? component.m_Limit : (component.m_Limit / num2));
			info.Add(new InfoList.Item($"Per Resource Limit: {num3}"));
			for (int i = 0; i < buffer.Length; i++)
			{
				info.Add(new InfoList.Item($"{EconomyUtils.GetName(buffer[i].m_Resource)}({buffer[i].m_Amount})"));
			}
		}
	}

	private bool HasWaterConsumeInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.HasComponent<WaterConsumer>(entity))
		{
			return base.EntityManager.HasComponent<ConsumptionData>(prefab);
		}
		return false;
	}

	private void UpdateWaterConsumeInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		ConsumptionData data = base.EntityManager.GetComponentData<ConsumptionData>(prefab);
		AddUpgradeData(entity, ref data);
		WaterConsumer componentData = base.EntityManager.GetComponentData<WaterConsumer>(entity);
		info.label = "Water Consuming";
		info.value = $"consuming: {componentData.m_WantedConsumption}  fill: {componentData.m_FulfilledFresh}";
		info.target = InfoList.Item.kNullEntity;
	}

	private bool HasGarbageInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.HasComponent<GarbageProducer>(entity))
		{
			return base.EntityManager.HasComponent<ConsumptionData>(prefab);
		}
		return false;
	}

	private void UpdateGarbageInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		ConsumptionData data = base.EntityManager.GetComponentData<ConsumptionData>(prefab);
		AddUpgradeData(entity, ref data);
		GarbageProducer componentData = base.EntityManager.GetComponentData<GarbageProducer>(entity);
		GarbageParameterData garbageParameter = __query_746694607_3.GetSingleton<GarbageParameterData>();
		GarbageAccumulationSystem.GetGarbage(ref data, entity, prefab, GetBufferLookup<Renter>(isReadOnly: true), GetBufferLookup<Game.Buildings.Student>(isReadOnly: true), GetBufferLookup<Occupant>(isReadOnly: true), GetComponentLookup<HomelessHousehold>(isReadOnly: true), GetBufferLookup<HouseholdCitizen>(isReadOnly: true), GetComponentLookup<Citizen>(isReadOnly: true), GetBufferLookup<Employee>(isReadOnly: true), GetBufferLookup<Patient>(isReadOnly: true), GetComponentLookup<SpawnableBuildingData>(isReadOnly: true), GetComponentLookup<CurrentDistrict>(isReadOnly: true), GetBufferLookup<DistrictModifier>(isReadOnly: true), GetComponentLookup<ZoneData>(isReadOnly: true), GetBufferLookup<CityModifier>(isReadOnly: true)[m_CitySystem.City], ref garbageParameter);
		int num = 0;
		if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Renter> buffer))
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				if (base.EntityManager.HasComponent<HomelessHousehold>(buffer[i].m_Renter) && base.EntityManager.TryGetBuffer(buffer[i].m_Renter, isReadOnly: true, out DynamicBuffer<HouseholdCitizen> buffer2))
				{
					num += buffer2.Length;
				}
			}
		}
		string garbageStatus = GetGarbageStatus(Mathf.RoundToInt(data.m_GarbageAccumulation), componentData.m_Garbage, num, garbageParameter.m_HomelessGarbageProduce);
		info.label = "Garbage";
		info.value = garbageStatus;
		info.target = InfoList.Item.kNullEntity;
	}

	private string GetGarbageStatus(int accumulation, int garbage, int homeless, int homelessProduce)
	{
		return garbage + " (+" + accumulation + " / day) homeless(" + homeless + " * " + homelessProduce + "=" + homeless * homelessProduce + ")";
	}

	private bool HasSpectatorSiteInfo(Entity entity, Entity _)
	{
		if (base.EntityManager.TryGetComponent<SpectatorSite>(entity, out var component))
		{
			return base.EntityManager.HasComponent<Duration>(component.m_Event);
		}
		return false;
	}

	private void UpdateSpectatorSiteInfo(Entity entity, Entity _, GenericInfo info)
	{
		SpectatorSite componentData = base.EntityManager.GetComponentData<SpectatorSite>(entity);
		Duration componentData2 = base.EntityManager.GetComponentData<Duration>(componentData.m_Event);
		if (m_SimulationSystem.frameIndex < componentData2.m_StartFrame)
		{
			info.label = "Preparing";
		}
		else if (m_SimulationSystem.frameIndex < componentData2.m_EndFrame)
		{
			info.label = "Event";
		}
		info.value = m_NameSystem.GetDebugName(componentData.m_Event);
		info.target = componentData.m_Event;
	}

	private bool HasInDangerInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.TryGetComponent<InDanger>(entity, out var component))
		{
			return base.EntityManager.Exists(component.m_Event);
		}
		return false;
	}

	private void UpdateInDangerInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		InDanger componentData = base.EntityManager.GetComponentData<InDanger>(entity);
		if ((componentData.m_Flags & DangerFlags.Evacuate) != 0)
		{
			info.label = "Evacuating";
		}
		else if ((componentData.m_Flags & DangerFlags.StayIndoors) != 0)
		{
			info.label = "In danger";
		}
		info.value = m_NameSystem.GetDebugName(componentData.m_Event);
		info.target = componentData.m_Event;
	}

	private bool HasVehicleInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.HasComponent<Building>(entity) || base.EntityManager.HasComponent<CompanyData>(entity) || base.EntityManager.HasComponent<Household>(entity))
		{
			return base.EntityManager.HasComponent<OwnedVehicle>(entity);
		}
		return false;
	}

	private void UpdateVehicleInfo(Entity entity, Entity prefab, InfoList info)
	{
		DynamicBuffer<OwnedVehicle> buffer = base.EntityManager.GetBuffer<OwnedVehicle>(entity, isReadOnly: true);
		int availableVehicles = VehicleUIUtils.GetAvailableVehicles(entity, base.EntityManager);
		info.label = $"Vehicles availableVehicles:{availableVehicles}";
		for (int i = 0; i < buffer.Length; i++)
		{
			Entity vehicle = buffer[i].m_Vehicle;
			info.Add(new InfoList.Item(m_NameSystem.GetDebugName(vehicle), vehicle));
		}
	}

	private bool HasPoliceInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<Game.Buildings.PoliceStation>(entity) || !base.EntityManager.HasComponent<Occupant>(entity) || !base.EntityManager.TryGetComponent<PoliceStationData>(prefab, out var data))
		{
			return false;
		}
		AddUpgradeData(entity, ref data);
		return data.m_JailCapacity > 0;
	}

	private void UpdatePoliceInfo(Entity entity, Entity prefab, CapacityInfo info)
	{
		PoliceStationData data = base.EntityManager.GetComponentData<PoliceStationData>(prefab);
		DynamicBuffer<Occupant> buffer = base.EntityManager.GetBuffer<Occupant>(entity, isReadOnly: true);
		AddUpgradeData(entity, ref data);
		info.label = "Arrested criminals";
		info.value = buffer.Length;
		info.max = data.m_JailCapacity;
	}

	private bool HasPrisonInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<Game.Buildings.Prison>(entity) || !base.EntityManager.TryGetComponent<PrisonData>(prefab, out var data) || !base.EntityManager.HasComponent<Occupant>(entity))
		{
			return false;
		}
		AddUpgradeData(entity, ref data);
		return data.m_PrisonerCapacity > 0;
	}

	private void UpdatePrisonInfo(Entity entity, Entity prefab, InfoList info)
	{
		PrisonData data = base.EntityManager.GetComponentData<PrisonData>(prefab);
		DynamicBuffer<Occupant> buffer = base.EntityManager.GetBuffer<Occupant>(entity, isReadOnly: true);
		AddUpgradeData(entity, ref data);
		info.label = $"Prisoners ({buffer.Length})";
		info.Add(new InfoList.Item(buffer.Length + "/" + data.m_PrisonerCapacity));
		for (int i = 0; i < buffer.Length; i++)
		{
			info.Add(new InfoList.Item(m_NameSystem.GetDebugName(buffer[i].m_Occupant), buffer[i].m_Occupant));
		}
	}

	private bool HasResourceProductionInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.HasComponent<Game.Economy.Resources>(entity))
		{
			return base.EntityManager.HasComponent<Game.Buildings.ResourceProducer>(entity);
		}
		return false;
	}

	private void UpdateResourceProductionInfo(Entity entity, Entity prefab, InfoList info)
	{
		DynamicBuffer<Game.Economy.Resources> buffer = base.EntityManager.GetBuffer<Game.Economy.Resources>(entity, isReadOnly: true);
		NativeList<ResourceProductionData> resources = default(NativeList<ResourceProductionData>);
		if (base.EntityManager.TryGetBuffer(prefab, isReadOnly: true, out DynamicBuffer<ResourceProductionData> buffer2))
		{
			resources = new NativeList<ResourceProductionData>(Allocator.Temp);
			resources.AddRange(buffer2.AsNativeArray());
		}
		if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<InstalledUpgrade> buffer3))
		{
			for (int i = 0; i < buffer3.Length; i++)
			{
				Entity prefab2 = base.EntityManager.GetComponentData<PrefabRef>(buffer3[i].m_Upgrade).m_Prefab;
				if (base.EntityManager.TryGetBuffer(prefab2, isReadOnly: true, out DynamicBuffer<ResourceProductionData> buffer4))
				{
					if (!resources.IsCreated)
					{
						resources = new NativeList<ResourceProductionData>(Allocator.Temp);
					}
					ResourceProductionData.Combine(resources, buffer4);
				}
			}
		}
		info.label = "Resource Production";
		if (resources.IsCreated)
		{
			for (int j = 0; j < resources.Length; j++)
			{
				ResourceProductionData resourceProductionData = resources[j];
				int resources2 = EconomyUtils.GetResources(resourceProductionData.m_Type, buffer);
				info.Add(new InfoList.Item(string.Concat((EconomyUtils.GetName(resourceProductionData.m_Type), " ", resources2, "/", resourceProductionData.m_StorageCapacity))));
			}
			resources.Dispose();
		}
	}

	private bool HasShelterInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<Game.Buildings.EmergencyShelter>(entity) || !base.EntityManager.TryGetComponent<EmergencyShelterData>(prefab, out var data) || !base.EntityManager.HasComponent<Occupant>(entity))
		{
			return false;
		}
		AddUpgradeData(entity, ref data);
		return data.m_ShelterCapacity > 0;
	}

	private void UpdateShelterInfo(Entity entity, Entity prefab, CapacityInfo info)
	{
		EmergencyShelterData data = base.EntityManager.GetComponentData<EmergencyShelterData>(prefab);
		DynamicBuffer<Occupant> buffer = base.EntityManager.GetBuffer<Occupant>(entity, isReadOnly: true);
		AddUpgradeData(entity, ref data);
		info.label = "Occupants";
		info.value = buffer.Length;
		info.max = data.m_ShelterCapacity;
	}

	private bool HasParkInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.HasComponent<Game.Buildings.Park>(entity))
		{
			return base.EntityManager.HasComponent<ParkData>(prefab);
		}
		return false;
	}

	private void UpdateParkInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		Game.Buildings.Park componentData = base.EntityManager.GetComponentData<Game.Buildings.Park>(entity);
		ParkData componentData2 = base.EntityManager.GetComponentData<ParkData>(prefab);
		info.label = "Maintenance";
		info.value = componentData.m_Maintenance + "/" + componentData2.m_MaintenancePool;
		info.target = InfoList.Item.kNullEntity;
	}

	private bool HasHouseholdInfo(Entity entity, Entity prefab)
	{
		return base.EntityManager.HasComponent<Household>(entity);
	}

	private void UpdateHouseholdInfo(Entity entity, Entity prefab, InfoList info)
	{
		info.label = "Household info";
		DynamicBuffer<HouseholdCitizen> buffer = base.EntityManager.GetBuffer<HouseholdCitizen>(entity, isReadOnly: true);
		ComponentLookup<Worker> workers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<Citizen> citizenDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<HealthProblem> healthProblems = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup, ref base.CheckedStateRef);
		InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef);
		EconomyParameterData economyParameters = __query_746694607_4.GetSingleton<EconomyParameterData>();
		if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<OwnedVehicle> buffer2))
		{
			_ = buffer2.Length;
		}
		NativeArray<int> taxRates = m_TaxSystem.GetTaxRates();
		Household componentData = base.EntityManager.GetComponentData<Household>(entity);
		DynamicBuffer<Game.Economy.Resources> buffer3 = base.EntityManager.GetBuffer<Game.Economy.Resources>(entity, isReadOnly: true);
		EconomyUtils.GetResources(Resource.Money, buffer3);
		int householdTotalWealth = EconomyUtils.GetHouseholdTotalWealth(componentData, buffer3);
		info.Add(new InfoList.Item("Wealth: " + householdTotalWealth));
		info.Add(new InfoList.Item("Household Resource: " + componentData.m_Resources));
		info.Add(new InfoList.Item("Shopped This Month: " + componentData.m_ShoppedValuePerDay));
		info.Add(new InfoList.Item("Shopped Last Month: " + componentData.m_ShoppedValueLastDay));
		info.Add(new InfoList.Item("Income: " + EconomyUtils.GetHouseholdIncome(buffer, ref workers, ref citizenDatas, ref healthProblems, ref economyParameters, taxRates)));
	}

	private bool HasHouseholdsInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.HasComponent<Renter>(entity) && base.EntityManager.TryGetComponent<BuildingPropertyData>(prefab, out var component) && component.m_ResidentialProperties > 0 && base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Renter> buffer))
		{
			return buffer.Length > 0;
		}
		return false;
	}

	private void UpdateHouseholdsInfo(Entity entity, Entity prefab, InfoList info)
	{
		DynamicBuffer<Renter> buffer = base.EntityManager.GetBuffer<Renter>(entity, isReadOnly: true);
		info.label = $"Households ({buffer.Length})";
		for (int i = 0; i < buffer.Length; i++)
		{
			Entity renter = buffer[i].m_Renter;
			if (base.EntityManager.TryGetComponent<Household>(renter, out var component) && base.EntityManager.TryGetComponent<PropertyRenter>(renter, out var component2) && base.EntityManager.TryGetBuffer(renter, isReadOnly: true, out DynamicBuffer<Game.Economy.Resources> buffer2))
			{
				info.Add(new InfoList.Item($"Name:{m_NameSystem.GetDebugName(renter)} Rent:{component2.m_Rent} Wealth:{EconomyUtils.GetHouseholdTotalWealth(component, buffer2)}", renter));
			}
		}
	}

	private bool HasResidentsInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.HasComponent<Household>(entity) && base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<HouseholdCitizen> buffer))
		{
			return buffer.Length > 0;
		}
		return false;
	}

	private void UpdateResidentInfo(Entity entity, Entity prefab, InfoList info)
	{
		DynamicBuffer<HouseholdCitizen> buffer = base.EntityManager.GetBuffer<HouseholdCitizen>(entity, isReadOnly: true);
		info.label = $"Residents ({buffer.Length})";
		for (int i = 0; i < buffer.Length; i++)
		{
			Entity citizen = buffer[i].m_Citizen;
			if (base.EntityManager.HasComponent<Citizen>(citizen))
			{
				info.Add(new InfoList.Item(m_NameSystem.GetDebugName(citizen), citizen));
			}
		}
	}

	private bool HasCompanyInfo(Entity entity, Entity prefab)
	{
		Entity company;
		return HasCompany(entity, prefab, out company);
	}

	private bool HasCompany(Entity entity, Entity prefab, out Entity company)
	{
		if (base.EntityManager.HasComponent<Renter>(entity) && base.EntityManager.HasComponent<BuildingPropertyData>(prefab) && base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Renter> buffer) && buffer.Length > 0)
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				if (base.EntityManager.HasComponent<CompanyData>(buffer[i].m_Renter))
				{
					company = buffer[i].m_Renter;
					return true;
				}
			}
		}
		company = InfoList.Item.kNullEntity;
		return false;
	}

	private void UpdateCompanyInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		DynamicBuffer<Renter> buffer = base.EntityManager.GetBuffer<Renter>(entity, isReadOnly: true);
		info.label = "Company";
		for (int i = 0; i < buffer.Length; i++)
		{
			Entity renter = buffer[i].m_Renter;
			if (base.EntityManager.HasComponent<CompanyData>(renter) && base.EntityManager.TryGetComponent<PropertyRenter>(renter, out var component))
			{
				info.value = $"Name:{m_NameSystem.GetDebugName(renter)} Rent:{component.m_Rent}";
				info.target = renter;
			}
		}
	}

	private bool HasEmployeesInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<Employee>(entity))
		{
			if (HasCompany(entity, prefab, out var company))
			{
				return base.EntityManager.HasComponent<Employee>(company);
			}
			return false;
		}
		return true;
	}

	private void UpdateEmployeesInfo(Entity entity, Entity prefab, InfoList info)
	{
		if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Employee> buffer) || (HasCompany(entity, prefab, out var company) && base.EntityManager.TryGetBuffer(company, isReadOnly: true, out buffer)))
		{
			info.label = $"Employees ({buffer.Length})";
			for (int i = 0; i < buffer.Length; i++)
			{
				info.Add(new InfoList.Item(m_NameSystem.GetDebugName(buffer[i].m_Worker), buffer[i].m_Worker));
			}
		}
	}

	private bool HasPatientsInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<Game.Buildings.Hospital>(entity) || !base.EntityManager.TryGetComponent<HospitalData>(prefab, out var data))
		{
			return false;
		}
		AddUpgradeData(entity, ref data);
		return data.m_PatientCapacity > 0;
	}

	private void UpdatePatientsInfo(Entity entity, Entity prefab, InfoList info)
	{
		DynamicBuffer<Patient> buffer = base.EntityManager.GetBuffer<Patient>(entity, isReadOnly: true);
		HospitalData data = base.EntityManager.GetComponentData<HospitalData>(prefab);
		AddUpgradeData(entity, ref data);
		info.label = $"Patients ({buffer.Length})";
		for (int i = 0; i < buffer.Length; i++)
		{
			Entity patient = buffer[i].m_Patient;
			info.Add(new InfoList.Item(m_NameSystem.GetDebugName(patient), patient));
		}
	}

	private bool HasStudentsInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<Game.Buildings.School>(entity) || !base.EntityManager.HasBuffer<Game.Buildings.Student>(entity) || !base.EntityManager.TryGetComponent<SchoolData>(prefab, out var data))
		{
			return false;
		}
		AddUpgradeData(entity, ref data);
		return data.m_StudentCapacity > 0;
	}

	private void UpdateStudentsInfo(Entity entity, Entity prefab, InfoList info)
	{
		DynamicBuffer<Game.Buildings.Student> buffer = base.EntityManager.GetBuffer<Game.Buildings.Student>(entity, isReadOnly: true);
		SchoolData data = base.EntityManager.GetComponentData<SchoolData>(prefab);
		AddUpgradeData(entity, ref data);
		info.label = $"Students ({buffer.Length})";
		for (int i = 0; i < buffer.Length; i++)
		{
			Entity student = buffer[i].m_Student;
			Citizen componentData = base.EntityManager.GetComponentData<Citizen>(student);
			float studyWillingness = componentData.GetPseudoRandom(CitizenPseudoRandom.StudyWillingness).NextFloat();
			DynamicBuffer<Efficiency> buffer2;
			float efficiency = (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out buffer2) ? BuildingUtils.GetEfficiency(buffer2) : 1f);
			DynamicBuffer<CityModifier> buffer3 = base.EntityManager.GetBuffer<CityModifier>(m_CitySystem.City, isReadOnly: true);
			info.Add(new InfoList.Item(m_NameSystem.GetDebugName(student) + $"Graduation: {GraduationSystem.GetGraduationProbability(data.m_EducationLevel, componentData.m_WellBeing, data, buffer3, studyWillingness, efficiency)}", student));
		}
	}

	private bool HasDeathcareInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<Game.Buildings.DeathcareFacility>(entity) || !base.EntityManager.HasComponent<Patient>(entity) || !base.EntityManager.TryGetComponent<DeathcareFacilityData>(prefab, out var data))
		{
			return false;
		}
		AddUpgradeData(entity, ref data);
		return data.m_StorageCapacity > 0;
	}

	private void UpdateDeathcareInfo(Entity entity, Entity prefab, CapacityInfo info)
	{
		DynamicBuffer<Patient> buffer = base.EntityManager.GetBuffer<Patient>(entity, isReadOnly: true);
		Game.Buildings.DeathcareFacility componentData = base.EntityManager.GetComponentData<Game.Buildings.DeathcareFacility>(entity);
		DeathcareFacilityData data = base.EntityManager.GetComponentData<DeathcareFacilityData>(prefab);
		AddUpgradeData(entity, ref data);
		int value = componentData.m_LongTermStoredCount + buffer.Length;
		info.label = "Bodies";
		info.value = value;
		info.max = data.m_StorageCapacity;
	}

	private bool HasEfficiencyInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.HasComponent<Building>(entity))
		{
			return base.EntityManager.HasComponent<Efficiency>(entity);
		}
		if (base.EntityManager.TryGetComponent<PropertyRenter>(entity, out var component))
		{
			return base.EntityManager.HasComponent<Efficiency>(component.m_Property);
		}
		return false;
	}

	private void UpdateEfficiencyInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		PropertyRenter component;
		Entity entity2 = ((!base.EntityManager.TryGetComponent<PropertyRenter>(entity, out component)) ? entity : component.m_Property);
		float efficiency = BuildingUtils.GetEfficiency(base.EntityManager.GetBuffer<Efficiency>(entity2, isReadOnly: true));
		info.label = "Efficiency";
		info.value = Mathf.RoundToInt(100f * efficiency) + " %";
		info.target = InfoList.Item.kNullEntity;
	}

	private bool HasStoredResourcesInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.HasComponent<StorageCompanyData>(prefab) && base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Game.Economy.Resources> buffer))
		{
			return buffer.Length > 0;
		}
		return false;
	}

	private void UpdateStoredResourcesInfo(Entity entity, Entity prefab, InfoList info)
	{
		UpgradeUtils.TryGetCombinedComponent<StorageCompanyData>(base.EntityManager, entity, prefab, out var data);
		DynamicBuffer<Game.Economy.Resources> buffer = base.EntityManager.GetBuffer<Game.Economy.Resources>(entity, isReadOnly: true);
		ResourceIterator iterator = ResourceIterator.GetIterator();
		info.label = $"AllowedResources:{EconomyUtils.CountResources(data.m_StoredResources)}";
		while (iterator.Next())
		{
			if ((data.m_StoredResources & iterator.resource) != Resource.NoResource)
			{
				int resources = EconomyUtils.GetResources(iterator.resource, buffer);
				info.Add(new InfoList.Item(EconomyUtils.GetName(iterator.resource) + resources));
			}
		}
	}

	private bool HasElectricityProductionInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.HasComponent<ElectricityProducer>(entity))
		{
			return base.EntityManager.HasComponent<PowerPlantData>(prefab);
		}
		return false;
	}

	private void UpdateElectricityProductionInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		PowerPlantData data = base.EntityManager.GetComponentData<PowerPlantData>(prefab);
		AddUpgradeData(entity, ref data);
		int electricityProduction = data.m_ElectricityProduction;
		info.label = "Electricity Production";
		info.value = electricityProduction.ToString();
		info.target = InfoList.Item.kNullEntity;
	}

	private bool HasBatteriesInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<Game.Buildings.Battery>(entity) || !base.EntityManager.HasComponent<PowerPlantData>(prefab) || !base.EntityManager.TryGetComponent<BatteryData>(prefab, out var data))
		{
			return false;
		}
		AddUpgradeData(entity, ref data);
		return data.m_Capacity > 0;
	}

	private void UpdateBatteriesInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		Game.Buildings.Battery componentData = base.EntityManager.GetComponentData<Game.Buildings.Battery>(entity);
		BatteryData data = base.EntityManager.GetComponentData<BatteryData>(prefab);
		AddUpgradeData(entity, ref data);
		info.label = "Batteries";
		info.value = Mathf.RoundToInt(100f * (float)(componentData.m_StoredEnergy / data.capacityTicks)) + "%";
		info.target = InfoList.Item.kNullEntity;
	}

	private bool HasStoredGarbageInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<Game.Buildings.GarbageFacility>(entity) || !base.EntityManager.TryGetComponent<GarbageFacilityData>(prefab, out var data))
		{
			return false;
		}
		AddUpgradeData(entity, ref data);
		return data.m_GarbageCapacity > 0;
	}

	private void UpdateStoredGarbageInfo(Entity entity, Entity prefab, CapacityInfo info)
	{
		GarbageFacilityData data = base.EntityManager.GetComponentData<GarbageFacilityData>(prefab);
		AddUpgradeData(entity, ref data);
		if (base.EntityManager.TryGetComponent<PowerPlantData>(prefab, out var component))
		{
			AddUpgradeData(entity, ref component);
		}
		int num = 0;
		if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Game.Economy.Resources> buffer))
		{
			num = EconomyUtils.GetResources(Resource.Garbage, buffer);
		}
		if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Game.Areas.SubArea> buffer2))
		{
			for (int i = 0; i < buffer2.Length; i++)
			{
				Entity area = buffer2[i].m_Area;
				if (base.EntityManager.TryGetComponent<Storage>(area, out var component2))
				{
					PrefabRef componentData = base.EntityManager.GetComponentData<PrefabRef>(area);
					Geometry componentData2 = base.EntityManager.GetComponentData<Geometry>(area);
					if (base.EntityManager.TryGetComponent<StorageAreaData>(componentData.m_Prefab, out var component3))
					{
						data.m_GarbageCapacity += AreaUtils.CalculateStorageCapacity(componentData2, component3);
						num += component2.m_Amount;
					}
				}
			}
		}
		info.label = "Stored Garbage";
		info.value = num;
		info.max = data.m_GarbageCapacity;
	}

	private bool HasGarbageProcessingInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<Game.Buildings.GarbageFacility>(entity) || !base.EntityManager.TryGetComponent<GarbageFacilityData>(prefab, out var data))
		{
			return false;
		}
		AddUpgradeData(entity, ref data);
		return data.m_ProcessingSpeed > 0;
	}

	private void UpdateGarbageProcessingInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		Game.Buildings.GarbageFacility componentData = base.EntityManager.GetComponentData<Game.Buildings.GarbageFacility>(entity);
		GarbageFacilityData data = base.EntityManager.GetComponentData<GarbageFacilityData>(prefab);
		AddUpgradeData(entity, ref data);
		info.label = "Garbage Processing Speed";
		info.value = componentData.m_ProcessingRate + "/" + data.m_ProcessingSpeed;
		info.target = InfoList.Item.kNullEntity;
	}

	private bool HasMailProcessingSpeedInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<Game.Buildings.PostFacility>(entity) || !base.EntityManager.TryGetComponent<PostFacilityData>(prefab, out var data))
		{
			return false;
		}
		AddUpgradeData(entity, ref data);
		return data.m_SortingRate > 0;
	}

	private void UpdateMailProcessingSpeedInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		Game.Buildings.PostFacility componentData = base.EntityManager.GetComponentData<Game.Buildings.PostFacility>(entity);
		PostFacilityData data = base.EntityManager.GetComponentData<PostFacilityData>(prefab);
		AddUpgradeData(entity, ref data);
		int num = (data.m_SortingRate * componentData.m_ProcessingFactor + 50) / 100;
		info.label = "Mail Processing Speed";
		info.value = num + "/" + data.m_SortingRate;
		info.target = InfoList.Item.kNullEntity;
	}

	private bool HasMailInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<Game.Buildings.PostFacility>(entity) || !base.EntityManager.TryGetComponent<PostFacilityData>(prefab, out var data))
		{
			return false;
		}
		AddUpgradeData(entity, ref data);
		return data.m_MailCapacity > 0;
	}

	private void UpdateMailInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		PostFacilityData data = base.EntityManager.GetComponentData<PostFacilityData>(prefab);
		AddUpgradeData(entity, ref data);
		DynamicBuffer<Game.Economy.Resources> buffer = base.EntityManager.GetBuffer<Game.Economy.Resources>(entity, isReadOnly: true);
		int resources = EconomyUtils.GetResources(Resource.UnsortedMail, buffer);
		int resources2 = EconomyUtils.GetResources(Resource.LocalMail, buffer);
		int resources3 = EconomyUtils.GetResources(Resource.OutgoingMail, buffer);
		string text = ((data.m_PostVanCapacity > 0) ? ("Mail to deliver: " + resources2 + ". Collected mail: " + resources + ".") : ("Unsorted mail: " + resources + ". Local mail: " + resources2 + "."));
		if (data.m_SortingRate > 0 || resources3 > 0)
		{
			text = text + " Outgoing mail: " + resources3;
		}
		info.label = "Post Facility";
		info.value = text;
		info.target = InfoList.Item.kNullEntity;
	}

	private bool HasStoredMailInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<Game.Buildings.PostFacility>(entity) || !base.EntityManager.TryGetComponent<PostFacilityData>(prefab, out var data) || !base.EntityManager.HasComponent<Game.Economy.Resources>(entity))
		{
			return false;
		}
		AddUpgradeData(entity, ref data);
		return data.m_MailCapacity > 0;
	}

	private void UpdateStoredMailInfo(Entity entity, Entity prefab, CapacityInfo info)
	{
		PostFacilityData data = base.EntityManager.GetComponentData<PostFacilityData>(prefab);
		AddUpgradeData(entity, ref data);
		DynamicBuffer<Game.Economy.Resources> buffer = base.EntityManager.GetBuffer<Game.Economy.Resources>(entity, isReadOnly: true);
		int resources = EconomyUtils.GetResources(Resource.UnsortedMail, buffer);
		int resources2 = EconomyUtils.GetResources(Resource.LocalMail, buffer);
		int resources3 = EconomyUtils.GetResources(Resource.OutgoingMail, buffer);
		int value = resources + resources2 + resources3;
		info.label = "Stored Mail";
		info.value = value;
		info.max = data.m_MailCapacity;
	}

	private bool HasSendReceiveMailInfo(Entity entity, Entity prefab)
	{
		return base.EntityManager.HasComponent<MailProducer>(entity);
	}

	private void UpdateSendReceiveMailInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		MailProducer componentData = base.EntityManager.GetComponentData<MailProducer>(entity);
		info.label = "Send Receive Mail";
		info.value = $"Send: {componentData.m_SendingMail} Receive: {componentData.receivingMail}";
		info.target = InfoList.Item.kNullEntity;
	}

	private bool HasParkingInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<Building>(entity))
		{
			return base.EntityManager.HasComponent<BicycleParking>(entity);
		}
		return true;
	}

	private void UpdateParkingInfo(Entity entity, Entity prefab, InfoList info)
	{
		int parkedCars = 0;
		int slotCapacity = 0;
		int parkingFee = 0;
		int laneCount = 0;
		string empty = string.Empty;
		NativeList<Entity> parkedCarList = new NativeList<Entity>(Allocator.Temp);
		if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Game.Net.SubLane> buffer))
		{
			CheckParkingLanes(buffer, ref slotCapacity, ref parkedCars, ref parkingFee, ref laneCount, ref parkedCarList);
		}
		if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Game.Net.SubNet> buffer2))
		{
			CheckParkingLanes(buffer2, ref slotCapacity, ref parkedCars, ref parkingFee, ref laneCount, ref parkedCarList);
		}
		if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Game.Objects.SubObject> buffer3))
		{
			CheckParkingLanes(buffer3, ref slotCapacity, ref parkedCars, ref parkingFee, ref laneCount, ref parkedCarList);
		}
		info.label = $"Parking ({parkedCarList.Length})";
		if (laneCount != 0 && base.EntityManager.TryGetComponent<BuildingData>(prefab, out var component) && (component.m_Flags & (Game.Prefabs.BuildingFlags.RestrictedPedestrian | Game.Prefabs.BuildingFlags.RestrictedCar)) == 0)
		{
			parkingFee /= laneCount;
			info.Add(new InfoList.Item("Parking Fee: " + parkingFee));
		}
		info.Add(new InfoList.Item(empty + " Parked Cars: " + parkedCars + "/" + slotCapacity + "."));
		for (int i = 0; i < parkedCarList.Length; i++)
		{
			info.Add(new InfoList.Item(m_NameSystem.GetDebugName(parkedCarList[i]), parkedCarList[i]));
		}
		parkedCarList.Dispose();
	}

	private void CheckParkingLanes(DynamicBuffer<Game.Objects.SubObject> subObjects, ref int slotCapacity, ref int parkedCars, ref int parkingFee, ref int laneCount, ref NativeList<Entity> parkedCarList)
	{
		for (int i = 0; i < subObjects.Length; i++)
		{
			Entity subObject = subObjects[i].m_SubObject;
			if (base.EntityManager.TryGetBuffer(subObject, isReadOnly: true, out DynamicBuffer<Game.Net.SubLane> buffer))
			{
				CheckParkingLanes(buffer, ref slotCapacity, ref parkedCars, ref parkingFee, ref laneCount, ref parkedCarList);
			}
			if (base.EntityManager.TryGetBuffer(subObject, isReadOnly: true, out DynamicBuffer<Game.Objects.SubObject> buffer2))
			{
				CheckParkingLanes(buffer2, ref slotCapacity, ref parkedCars, ref parkingFee, ref laneCount, ref parkedCarList);
			}
		}
	}

	private void CheckParkingLanes(DynamicBuffer<Game.Net.SubNet> subNets, ref int slotCapacity, ref int parkedCars, ref int parkingFee, ref int laneCount, ref NativeList<Entity> parkedCarList)
	{
		for (int i = 0; i < subNets.Length; i++)
		{
			Entity subNet = subNets[i].m_SubNet;
			if (base.EntityManager.TryGetBuffer(subNet, isReadOnly: true, out DynamicBuffer<Game.Net.SubLane> buffer))
			{
				CheckParkingLanes(buffer, ref slotCapacity, ref parkedCars, ref parkingFee, ref laneCount, ref parkedCarList);
			}
		}
	}

	private void CheckParkingLanes(DynamicBuffer<Game.Net.SubLane> subLanes, ref int slotCapacity, ref int parkedCars, ref int parkingFee, ref int laneCount, ref NativeList<Entity> parkedCarList)
	{
		for (int i = 0; i < subLanes.Length; i++)
		{
			Entity subLane = subLanes[i].m_SubLane;
			GarageLane component2;
			if (base.EntityManager.TryGetComponent<Game.Net.ParkingLane>(subLane, out var component))
			{
				if ((component.m_Flags & ParkingLaneFlags.VirtualLane) != 0)
				{
					continue;
				}
				Entity prefab = base.EntityManager.GetComponentData<PrefabRef>(subLane).m_Prefab;
				Curve componentData = base.EntityManager.GetComponentData<Curve>(subLane);
				DynamicBuffer<LaneObject> buffer = base.EntityManager.GetBuffer<LaneObject>(subLane, isReadOnly: true);
				ParkingLaneData componentData2 = base.EntityManager.GetComponentData<ParkingLaneData>(prefab);
				if (componentData2.m_SlotInterval != 0f)
				{
					int parkingSlotCount = NetUtils.GetParkingSlotCount(componentData, component, componentData2);
					slotCapacity += parkingSlotCount;
				}
				else
				{
					slotCapacity = -1000000;
				}
				for (int j = 0; j < buffer.Length; j++)
				{
					if (base.EntityManager.HasComponent<ParkedCar>(buffer[j].m_LaneObject))
					{
						LaneObject laneObject = buffer[j];
						parkedCarList.Add(in laneObject.m_LaneObject);
						parkedCars++;
					}
				}
				parkingFee += component.m_ParkingFee;
				laneCount++;
			}
			else if (base.EntityManager.TryGetComponent<GarageLane>(subLane, out component2))
			{
				slotCapacity += component2.m_VehicleCapacity;
				parkedCars += component2.m_VehicleCount;
				parkingFee += component2.m_ParkingFee;
				laneCount++;
			}
		}
	}

	private bool HasHouseholdPetsInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.HasComponent<Household>(entity) && base.EntityManager.HasComponent<HouseholdData>(prefab) && base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<HouseholdAnimal> buffer))
		{
			return buffer.Length > 0;
		}
		return false;
	}

	private void UpdateHouseholdPetsInfo(Entity entity, Entity prefab, InfoList info)
	{
		DynamicBuffer<HouseholdAnimal> buffer = base.EntityManager.GetBuffer<HouseholdAnimal>(entity, isReadOnly: true);
		info.label = "Household Pets";
		for (int i = 0; i < buffer.Length; i++)
		{
			info.Add(new InfoList.Item(m_NameSystem.GetDebugName(buffer[i].m_HouseholdPet), buffer[i].m_HouseholdPet));
		}
	}

	private bool HasRentInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.TryGetComponent<PropertyRenter>(entity, out var component))
		{
			return component.m_Property != InfoList.Item.kNullEntity;
		}
		return false;
	}

	private void UpdateRentInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		PropertyRenter componentData = base.EntityManager.GetComponentData<PropertyRenter>(entity);
		DynamicBuffer<Game.Economy.Resources> buffer = base.EntityManager.GetBuffer<Game.Economy.Resources>(entity);
		info.label = "Rent";
		info.value = $"Rent: {componentData.m_Rent} Money:{EconomyUtils.GetResources(Resource.Money, buffer)}";
		info.target = InfoList.Item.kNullEntity;
	}

	private bool HasBuildingLevelInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.HasComponent<BuildingPropertyData>(prefab))
		{
			return base.EntityManager.HasComponent<Building>(entity);
		}
		return false;
	}

	private void UpdateBuildingLevelInfo(Entity entity, Entity prefab, InfoList info)
	{
		Building componentData = base.EntityManager.GetComponentData<Building>(entity);
		string[] array = new string[0];
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		float num5 = 0f;
		int num6 = 1;
		int lotSize = 0;
		bool flag = false;
		int num7 = 0;
		int num8 = 0;
		EconomyParameterData economyParameterData = __query_746694607_4.GetSingleton<EconomyParameterData>();
		BuildingConfigurationData singleton = __query_746694607_5.GetSingleton<BuildingConfigurationData>();
		if (base.EntityManager.TryGetComponent<LandValue>(componentData.m_RoadEdge, out var component))
		{
			num5 = component.m_LandValue;
		}
		if (base.EntityManager.TryGetComponent<ConsumptionData>(prefab, out var component2))
		{
			num = component2.m_Upkeep;
		}
		if (base.EntityManager.TryGetComponent<BuildingData>(prefab, out var component3))
		{
			lotSize = component3.m_LotSize.x * component3.m_LotSize.y;
		}
		if (base.EntityManager.TryGetComponent<Abandoned>(entity, out var component4))
		{
			flag = true;
			num7 = Mathf.FloorToInt((float)(m_SimulationSystem.frameIndex - component4.m_AbandonmentTime) / 60f);
			num8 = Mathf.FloorToInt((float)singleton.m_AbandonedDestroyDelay / 60f);
		}
		if (base.EntityManager.TryGetComponent<BuildingPropertyData>(prefab, out var component5))
		{
			Game.Zones.AreaType areaType = Game.Zones.AreaType.None;
			if (base.EntityManager.TryGetComponent<SpawnableBuildingData>(prefab, out var component6))
			{
				num6 = component6.m_Level;
				areaType = base.EntityManager.GetComponentData<ZoneData>(component6.m_ZonePrefab).m_AreaType;
				if (base.EntityManager.TryGetComponent<BuildingCondition>(entity, out var component7))
				{
					DynamicBuffer<CityModifier> buffer = base.EntityManager.GetBuffer<CityModifier>(m_CitySystem.City, isReadOnly: true);
					num3 = BuildingUtils.GetLevelingCost(areaType, component5, component6.m_Level, buffer);
					num4 = BuildingUtils.GetAbandonCost(areaType, component5, component6.m_Level, num3, buffer);
					num2 = component7.m_Condition;
				}
			}
			array = PropertyUtils.GetRentPriceDebugInfo(component5, num6, lotSize, num5, areaType, ref economyParameterData);
		}
		info.label = "Building Level";
		info.Add(new InfoList.Item("-- Building Economy --"));
		info.Add(new InfoList.Item($"Land value: {num5}"));
		info.Add(new InfoList.Item($"Upkeep: {num}"));
		info.Add(new InfoList.Item("-- Rent --"));
		string[] array2 = array;
		foreach (string text in array2)
		{
			info.Add(new InfoList.Item(text));
		}
		info.Add(new InfoList.Item("-- Leveling --"));
		info.Add(new InfoList.Item($"Level: {num6}"));
		if (flag)
		{
			info.Add(new InfoList.Item($"Abandoned time: {num7} s / {num8} s"));
			return;
		}
		info.Add(new InfoList.Item($"Abandon cost: {num4}"));
		info.Add(new InfoList.Item($"Progression: {num2} / {num3}"));
	}

	private bool HasTradeCostInfo(Entity entity, Entity prefab)
	{
		DynamicBuffer<TradeCost> buffer;
		return base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out buffer);
	}

	private void UpdateTradeCostInfo(Entity entity, Entity prefab, InfoList infos)
	{
		infos.label = "Trade Costs";
		if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<TradeCost> buffer))
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				TradeCost tradeCost = buffer[i];
				infos.Add(new InfoList.Item($"{EconomyUtils.GetName(tradeCost.m_Resource)} buy {tradeCost.m_BuyCost} sell {tradeCost.m_SellCost}"));
			}
		}
	}

	private bool HasTradePartnerInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.TryGetComponent<Game.Companies.StorageCompany>(entity, out var component) || !(component.m_LastTradePartner != InfoList.Item.kNullEntity))
		{
			if (base.EntityManager.TryGetComponent<BuyingCompany>(entity, out var component2))
			{
				return component2.m_LastTradePartner != InfoList.Item.kNullEntity;
			}
			return false;
		}
		return true;
	}

	private void UpdateTradePartnerInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		string debugName;
		Entity lastTradePartner;
		if (base.EntityManager.TryGetComponent<Game.Companies.StorageCompany>(entity, out var component) && component.m_LastTradePartner != InfoList.Item.kNullEntity)
		{
			debugName = m_NameSystem.GetDebugName(component.m_LastTradePartner);
			lastTradePartner = component.m_LastTradePartner;
		}
		else
		{
			BuyingCompany componentData = base.EntityManager.GetComponentData<BuyingCompany>(entity);
			debugName = m_NameSystem.GetDebugName(componentData.m_LastTradePartner);
			lastTradePartner = componentData.m_LastTradePartner;
		}
		info.label = "Trade Partner";
		info.value = debugName;
		info.target = lastTradePartner;
	}

	private bool HasWarehouseInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.HasComponent<Game.Companies.StorageCompany>(entity))
		{
			return base.EntityManager.HasComponent<StorageCompanyData>(prefab);
		}
		return false;
	}

	private void UpdateWarehouseInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		StorageCompanyData componentData = base.EntityManager.GetComponentData<StorageCompanyData>(prefab);
		DynamicBuffer<TradeCost> buffer = base.EntityManager.GetBuffer<TradeCost>(entity, isReadOnly: true);
		DynamicBuffer<Game.Economy.Resources> buffer2 = base.EntityManager.GetBuffer<Game.Economy.Resources>(entity, isReadOnly: true);
		TradeCost tradeCost = EconomyUtils.GetTradeCost(componentData.m_StoredResources, buffer);
		int resources = EconomyUtils.GetResources(componentData.m_StoredResources, buffer2);
		info.label = "Warehouse - ";
		info.value = "Stores: " + EconomyUtils.GetName(componentData.m_StoredResources) + " (" + resources + "). Buy Cost: " + tradeCost.m_BuyCost.ToString("F1") + ". Sell Cost: " + tradeCost.m_SellCost.ToString("F1");
		info.target = InfoList.Item.kNullEntity;
	}

	private bool HasCompanyEconomyInfo(Entity entity, Entity prefab)
	{
		return base.EntityManager.HasComponent<CompanyData>(entity);
	}

	private void UpdateCompanyEconomyInfo(Entity entity, Entity prefab, InfoList info)
	{
		ResourcePrefabs prefabs = m_ResourceSystem.GetPrefabs();
		EconomyParameterData econParams = __query_746694607_4.GetSingleton<EconomyParameterData>();
		DynamicBuffer<Game.Economy.Resources> buffer = base.EntityManager.GetBuffer<Game.Economy.Resources>(entity, isReadOnly: true);
		ComponentLookup<ResourceData> resourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<Game.Vehicles.DeliveryTruck> deliveryTrucks = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_DeliveryTruck_RO_ComponentLookup, ref base.CheckedStateRef);
		BufferLookup<LayoutElement> layouts = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef);
		ComponentLookup<Citizen> citizens = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef);
		InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_ServiceAvailable_RO_ComponentLookup, ref base.CheckedStateRef);
		InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_ServiceCompanyData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<IndustrialProcessData> componentLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef);
		int num = 0;
		IndustrialProcessData industrialProcessData = default(IndustrialProcessData);
		bool flag = !base.EntityManager.HasComponent<ServiceAvailable>(entity);
		if (componentLookup.TryGetComponent(prefab, out var componentData))
		{
			industrialProcessData = componentData;
		}
		DynamicBuffer<OwnedVehicle> buffer2;
		int worth = ((!base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out buffer2)) ? EconomyUtils.GetCompanyTotalWorth(flag, industrialProcessData, buffer, prefabs, ref resourceDatas) : EconomyUtils.GetCompanyTotalWorth(flag, industrialProcessData, buffer, buffer2, ref layouts, ref deliveryTrucks, prefabs, ref resourceDatas));
		bool flag2 = false;
		float buildingEfficiency = 0f;
		int num2 = 1;
		float num3 = 0f;
		float num4 = 0f;
		float num5 = 0f;
		if (base.EntityManager.TryGetComponent<PropertyRenter>(entity, out var component))
		{
			num = component.m_Rent;
			buildingEfficiency = (base.EntityManager.TryGetBuffer(component.m_Property, isReadOnly: true, out DynamicBuffer<Efficiency> buffer3) ? BuildingUtils.GetEfficiency(buffer3) : 1f);
			if (base.EntityManager.TryGetBuffer(component.m_Property, isReadOnly: true, out DynamicBuffer<Renter> buffer4))
			{
				num2 = buffer4.Length;
			}
			if (base.EntityManager.HasComponent<OfficeProperty>(component.m_Property))
			{
				flag2 = true;
			}
			DynamicBuffer<ServiceFee> buffer5 = base.EntityManager.GetBuffer<ServiceFee>(m_CitySystem.City, isReadOnly: true);
			if (base.EntityManager.TryGetComponent<ElectricityConsumer>(component.m_Property, out var component2))
			{
				num3 = (float)component2.m_FulfilledConsumption * ServiceFeeSystem.GetFee(PlayerResource.Electricity, buffer5);
			}
			if (base.EntityManager.TryGetComponent<WaterConsumer>(component.m_Property, out var component3))
			{
				num4 = (float)component3.m_FulfilledFresh * ServiceFeeSystem.GetFee(PlayerResource.Water, buffer5);
				num5 = (float)component3.m_FulfilledSewage * ServiceFeeSystem.GetFee(PlayerResource.Water, buffer5);
			}
		}
		int num6 = 0;
		if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Employee> buffer6))
		{
			num6 = EconomyUtils.CalculateTotalWage(buffer6, ref econParams);
		}
		float num7 = 0f;
		ServiceFeeParameterData singleton = __query_746694607_0.GetSingleton<ServiceFeeParameterData>();
		num7 = ((!flag) ? ((float)singleton.m_GarbageFeeRCIO.y) : ((!flag2) ? ((float)singleton.m_GarbageFeeRCIO.w) : ((float)singleton.m_GarbageFeeRCIO.z)));
		info.label = "Company Economy";
		info.Add(new InfoList.Item("Worth State: " + WorthToString(worth)));
		info.Add(new InfoList.Item("--Expenses--"));
		info.Add(new InfoList.Item("Wages Per Day: " + num6));
		info.Add(new InfoList.Item("Rent Per Day: " + num / num2));
		info.Add(new InfoList.Item("ElectricityFee Per Day: " + (int)(num3 / (float)num2)));
		info.Add(new InfoList.Item("WaterFee Per Day: " + (int)(num4 / (float)num2)));
		info.Add(new InfoList.Item("SewageFee Per Day: " + (int)(num5 / (float)num2)));
		info.Add(new InfoList.Item("GarbageFee Per Day: " + (int)(num7 / (float)num2)));
		if (base.EntityManager.TryGetComponent<IndustrialProcessData>(prefab, out var component4) && base.EntityManager.HasBuffer<Employee>(entity))
		{
			int companyProfitPerDay = EconomyUtils.GetCompanyProfitPerDay(buildingEfficiency, flag, buffer6, component4, prefabs, ref resourceDatas, ref citizens, ref econParams);
			info.Add(new InfoList.Item("--Income--"));
			info.Add(new InfoList.Item($"Profit Per Day:{companyProfitPerDay}"));
			info.Add(new InfoList.Item("--Info--"));
			ServiceAvailable serviceAvailable = (base.EntityManager.HasComponent<ServiceAvailable>(entity) ? base.EntityManager.GetComponentData<ServiceAvailable>(entity) : default(ServiceAvailable));
			ServiceCompanyData serviceCompanyData = (base.EntityManager.HasComponent<ServiceCompanyData>(prefab) ? base.EntityManager.GetComponentData<ServiceCompanyData>(prefab) : default(ServiceCompanyData));
			int companyProductionPerDay = EconomyUtils.GetCompanyProductionPerDay(buildingEfficiency, flag, buffer6, component4, prefabs, ref resourceDatas, ref citizens, ref econParams, serviceAvailable, serviceCompanyData);
			info.Add(new InfoList.Item($"Production Per Day: {companyProductionPerDay} * {EconomyUtils.GetNameFixed(component4.m_Output.m_Resource)}"));
			if (component4.m_Input1.m_Resource != Resource.NoResource)
			{
				info.Add(new InfoList.Item($"Input1: {component4.m_Input1.m_Amount}*{EconomyUtils.GetNameFixed(component4.m_Input1.m_Resource)}({EconomyUtils.GetIndustrialPrice(component4.m_Input1.m_Resource, prefabs, ref resourceDatas)})"));
			}
			if (component4.m_Input2.m_Resource != Resource.NoResource)
			{
				info.Add(new InfoList.Item($"Input2: {component4.m_Input2.m_Amount}*{EconomyUtils.GetNameFixed(component4.m_Input2.m_Resource)}({EconomyUtils.GetIndustrialPrice(component4.m_Input2.m_Resource, prefabs, ref resourceDatas)})"));
			}
			if (component4.m_Output.m_Resource != Resource.NoResource)
			{
				float num8 = (flag ? EconomyUtils.GetIndustrialPrice(component4.m_Output.m_Resource, prefabs, ref resourceDatas) : EconomyUtils.GetMarketPrice(component4.m_Output.m_Resource, prefabs, ref resourceDatas));
				info.Add(new InfoList.Item($"Output: {component4.m_Output.m_Amount}*{EconomyUtils.GetNameFixed(component4.m_Output.m_Resource)}({num8})"));
			}
		}
	}

	private string WorthToString(int worth)
	{
		if (worth < -7500)
		{
			return "Bankrupt ";
		}
		if (worth < -1000)
		{
			return "Poor ";
		}
		if (worth < 1000)
		{
			return "Stable ";
		}
		return "Wealthy ";
	}

	private bool HasCompanyProfitInfo(Entity entity, Entity prefab)
	{
		return base.EntityManager.HasComponent<Profitability>(entity);
	}

	private void UpdateCompanyProfitInfo(Entity entity, Entity prefab, InfoList info)
	{
		if (!base.EntityManager.TryGetComponent<Profitability>(entity, out var component))
		{
			return;
		}
		info.label = "Company Profit";
		info.Add(new InfoList.Item($"profitability: {component.m_Profitability}"));
		info.Add(new InfoList.Item($"Last Day Total Worth: {component.m_LastTotalWorth}"));
		ComponentLookup<ResourceData> resourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef);
		bool flag = base.EntityManager.HasComponent<Game.Companies.ExtractorCompany>(entity);
		if (!base.EntityManager.TryGetComponent<IndustrialProcessData>(prefab, out var component2))
		{
			return;
		}
		float num = 0f;
		float concentration = 0f;
		BufferLookup<Game.Areas.SubArea> subAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_SubArea_RO_BufferLookup, ref base.CheckedStateRef);
		BufferLookup<InstalledUpgrade> installedUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef);
		ComponentLookup<Extractor> extractors = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Extractor_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<Attached> attacheds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<Geometry> geometries = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Geometry_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<Game.Areas.Lot> lots = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Lot_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<ExtractorAreaData> extractorDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ExtractorAreaData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<PrefabRef> prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<ServiceCompanyData> serviceCompanyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_ServiceCompanyData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<BuildingData> buildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<BuildingPropertyData> buildingPropertyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<SpawnableBuildingData> spawnableBuildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<IndustrialProcessData> industrialProcessDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<ExtractorCompanyData> extractorCompanyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ExtractorCompanyData_RO_ComponentLookup, ref base.CheckedStateRef);
		if (base.EntityManager.TryGetComponent<PropertyRenter>(entity, out var component3))
		{
			num = (base.EntityManager.TryGetBuffer(component3.m_Property, isReadOnly: true, out DynamicBuffer<Efficiency> buffer) ? BuildingUtils.GetEfficiency(buffer) : 1f);
			if (base.EntityManager.TryGetComponent<Attached>(component3.m_Property, out var component4))
			{
				ExtractorCompanySystem.GetBestConcentration(component2.m_Output.m_Resource, component4.m_Parent, ref subAreas, ref installedUpgrades, ref extractors, ref geometries, ref prefabs, ref extractorDatas, __query_746694607_6.GetSingleton<ExtractorParameterData>(), m_ResourceSystem.GetPrefabs(), ref resourceDatas, out concentration, out var _);
			}
		}
		info.Add(flag ? new InfoList.Item($"efficiency:{num * 100f}% concentration:{concentration}") : new InfoList.Item($"efficiency:{num * 100f}%"));
		int companyMaxFittingWorkers = CompanyUtils.GetCompanyMaxFittingWorkers(entity, component3.m_Property, ref prefabs, ref serviceCompanyDatas, ref buildingDatas, ref buildingPropertyDatas, ref spawnableBuildingDatas, ref industrialProcessDatas, ref extractorCompanyDatas, ref attacheds, ref subAreas, ref installedUpgrades, ref lots, ref geometries);
		info.Add(new InfoList.Item($"maxFittingWorkers(not current max worker):{companyMaxFittingWorkers}"));
	}

	private bool HasServiceCompanyInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.HasComponent<CompanyData>(entity) && base.EntityManager.HasComponent<ServiceAvailable>(entity))
		{
			return base.EntityManager.HasComponent<Game.Economy.Resources>(entity);
		}
		return false;
	}

	private void UpdateServiceCompanyInfo(Entity entity, Entity prefab, InfoList info)
	{
		ServiceAvailable componentData = base.EntityManager.GetComponentData<ServiceAvailable>(entity);
		ServiceCompanyData componentData2 = base.EntityManager.GetComponentData<ServiceCompanyData>(prefab);
		DynamicBuffer<Game.Economy.Resources> buffer = base.EntityManager.GetBuffer<Game.Economy.Resources>(entity, isReadOnly: true);
		info.label = "Service Company";
		if (base.EntityManager.HasComponent<Game.Companies.ProcessingCompany>(entity) && base.EntityManager.TryGetComponent<IndustrialProcessData>(prefab, out var component))
		{
			Resource resource = component.m_Output.m_Resource;
			info.Add(new InfoList.Item(string.Concat("Service: ", componentData.m_ServiceAvailable + "/" + componentData2.m_MaxService + "(" + ServicesToString(componentData.m_ServiceAvailable, componentData2.m_MaxService) + ")")));
			info.Add(new InfoList.Item("Provide Resource Storage: " + EconomyUtils.GetName(resource) + " (" + EconomyUtils.GetResources(resource, buffer) + ")"));
			if (base.EntityManager.TryGetComponent<PropertyRenter>(entity, out var component2) && component2.m_Property != InfoList.Item.kNullEntity && base.EntityManager.TryGetComponent<LodgingProvider>(entity, out var component3) && resource == Resource.Lodging)
			{
				Entity property = component2.m_Property;
				Entity prefab2 = base.EntityManager.GetComponentData<PrefabRef>(property).m_Prefab;
				SpawnableBuildingData componentData3 = base.EntityManager.GetComponentData<SpawnableBuildingData>(prefab2);
				BuildingPropertyData componentData4 = base.EntityManager.GetComponentData<BuildingPropertyData>(prefab2);
				int roomCount = LodgingProviderSystem.GetRoomCount(base.EntityManager.GetComponentData<BuildingData>(prefab2).m_LotSize, componentData3.m_Level, componentData4);
				info.Add(new InfoList.Item("Lodging rooms free: " + component3.m_FreeRooms + "/" + roomCount));
			}
		}
		if (base.EntityManager.TryGetComponent<BuyingCompany>(entity, out var component4))
		{
			info.Add(new InfoList.Item("Trip Length: " + component4.m_MeanInputTripLength));
		}
	}

	private string ServicesToString(int services, int maxServices)
	{
		float num = (float)services / (float)maxServices;
		if (services <= 0)
		{
			return "Overworked";
		}
		if (num < 0.2f)
		{
			return "Busy";
		}
		if (num < 0.8f)
		{
			return "Operational";
		}
		return "Low on customers";
	}

	private bool HasExtractorCompanyInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<CompanyData>(entity) || !base.EntityManager.HasComponent<Game.Companies.ExtractorCompany>(entity) || !base.EntityManager.HasComponent<IndustrialProcessData>(prefab))
		{
			if (base.EntityManager.TryGetComponent<Owner>(entity, out var component) && base.EntityManager.TryGetComponent<Attachment>(component.m_Owner, out var component2) && base.EntityManager.TryGetBuffer(component2.m_Attached, isReadOnly: true, out DynamicBuffer<Renter> buffer))
			{
				return buffer.Length > 0;
			}
			return false;
		}
		return true;
	}

	private void UpdateExtractorCompanyInfo(Entity entity, Entity prefab, InfoList info)
	{
		if (base.EntityManager.TryGetComponent<Owner>(entity, out var component) && base.EntityManager.TryGetComponent<Attachment>(component.m_Owner, out var component2) && base.EntityManager.TryGetBuffer(component2.m_Attached, isReadOnly: true, out DynamicBuffer<Renter> buffer) && buffer.Length > 0)
		{
			entity = buffer[0].m_Renter;
			prefab = base.EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab;
		}
		DynamicBuffer<Game.Economy.Resources> buffer2 = base.EntityManager.GetBuffer<Game.Economy.Resources>(entity, isReadOnly: true);
		IndustrialProcessData componentData = base.EntityManager.GetComponentData<IndustrialProcessData>(prefab);
		info.label = "Extractor Company";
		Resource resource = componentData.m_Output.m_Resource;
		info.Add(new InfoList.Item("Produces: " + EconomyUtils.GetName(resource) + " (" + EconomyUtils.GetResources(resource, buffer2) + ")"));
		if (!base.EntityManager.TryGetComponent<PropertyRenter>(entity, out var component3) || !(component3.m_Property != InfoList.Item.kNullEntity) || !base.EntityManager.TryGetComponent<Attached>(component3.m_Property, out var component4) || !base.EntityManager.TryGetComponent<WorkplaceData>(prefab, out var _) || !base.EntityManager.TryGetComponent<PrefabRef>(component3.m_Property, out var _) || !base.EntityManager.TryGetComponent<IndustrialProcessData>(prefab, out var _))
		{
			return;
		}
		__query_746694607_6.GetSingleton<ExtractorParameterData>();
		__query_746694607_4.GetSingleton<EconomyParameterData>();
		InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Lot_RO_ComponentLookup, ref base.CheckedStateRef);
		InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Geometry_RO_ComponentLookup, ref base.CheckedStateRef);
		InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Extractor_RO_ComponentLookup, ref base.CheckedStateRef);
		base.World.GetOrCreateSystemManaged<ResourceSystem>().GetPrefabs();
		InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef);
		InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef);
		InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ExtractorAreaData_RO_ComponentLookup, ref base.CheckedStateRef);
		if (base.EntityManager.TryGetBuffer(component4.m_Parent, isReadOnly: true, out DynamicBuffer<Game.Areas.SubArea> buffer3))
		{
			foreach (Game.Areas.SubArea item in buffer3)
			{
				info.Add(new InfoList.Item("Area:" + m_NameSystem.GetDebugName(component4.m_Parent), item.m_Area));
				if (base.EntityManager.TryGetComponent<Extractor>(item.m_Area, out var component8))
				{
					info.Add(new InfoList.Item($"ResourceAmount: {component8.m_ResourceAmount}"));
					info.Add(new InfoList.Item($"WorkAmount(Harvest Work): {component8.m_WorkAmount}"));
					info.Add(new InfoList.Item($"HarvestedAmount(Collect Work): {component8.m_HarvestedAmount}"));
					info.Add(new InfoList.Item($"ExtractedAmount: {component8.m_ExtractedAmount}"));
					info.Add(new InfoList.Item($"TotalExtracted: {component8.m_TotalExtracted}"));
				}
			}
		}
		if (!base.EntityManager.TryGetBuffer(component4.m_Parent, isReadOnly: true, out DynamicBuffer<InstalledUpgrade> buffer4))
		{
			return;
		}
		foreach (InstalledUpgrade item2 in buffer4)
		{
			info.Add(new InfoList.Item("InstalledUpgrade:" + m_NameSystem.GetDebugName(item2), item2));
			if (!base.EntityManager.TryGetBuffer((Entity)item2, isReadOnly: true, out buffer3))
			{
				continue;
			}
			foreach (Game.Areas.SubArea item3 in buffer3)
			{
				info.Add(new InfoList.Item("Area:" + m_NameSystem.GetDebugName(component4.m_Parent), item3.m_Area));
				if (base.EntityManager.TryGetComponent<Extractor>(item3.m_Area, out var component9))
				{
					info.Add(new InfoList.Item($"ResourceAmount: {component9.m_ResourceAmount}"));
					info.Add(new InfoList.Item($"WorkAmount(Harvest Work): {component9.m_WorkAmount}"));
					info.Add(new InfoList.Item($"HarvestedAmount(Collect Work): {component9.m_HarvestedAmount}"));
					info.Add(new InfoList.Item($"ExtractedAmount: {component9.m_ExtractedAmount}"));
					info.Add(new InfoList.Item($"TotalExtracted: {component9.m_TotalExtracted}"));
				}
			}
		}
	}

	private bool HasProcessingCompanyInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.HasComponent<CompanyData>(entity) && base.EntityManager.HasComponent<Game.Companies.ProcessingCompany>(entity))
		{
			return base.EntityManager.HasComponent<IndustrialProcessData>(prefab);
		}
		return false;
	}

	private void UpdateProcessingCompanyInfo(Entity entity, Entity prefab, InfoList info)
	{
		DynamicBuffer<Game.Economy.Resources> buffer = base.EntityManager.GetBuffer<Game.Economy.Resources>(entity, isReadOnly: true);
		IndustrialProcessData componentData = base.EntityManager.GetComponentData<IndustrialProcessData>(prefab);
		ComponentLookup<Citizen> citizens = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef);
		info.label = "Processing Company";
		Resource resource = componentData.m_Input1.m_Resource;
		Resource resource2 = componentData.m_Input2.m_Resource;
		Resource resource3 = componentData.m_Output.m_Resource;
		info.Add(new InfoList.Item("In: " + EconomyUtils.GetName(resource) + " (" + EconomyUtils.GetResources(resource, buffer) + ")"));
		if (resource2 != Resource.NoResource)
		{
			info.Add(new InfoList.Item("In: " + EconomyUtils.GetName(resource2) + " (" + EconomyUtils.GetResources(resource2, buffer) + ")"));
		}
		info.Add(new InfoList.Item("Out: " + EconomyUtils.GetName(resource3) + " (" + EconomyUtils.GetResources(resource3, buffer) + ")"));
		EconomyParameterData singleton = __query_746694607_4.GetSingleton<EconomyParameterData>();
		if (!base.EntityManager.HasComponent<ServiceAvailable>(entity))
		{
			base.EntityManager.HasComponent<Game.Companies.ExtractorCompany>(entity);
		}
		if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Employee> buffer2) && base.EntityManager.TryGetComponent<PropertyRenter>(entity, out var component) && base.EntityManager.TryGetComponent<PrefabRef>(component.m_Property, out var component2) && base.EntityManager.TryGetComponent<WorkplaceData>(prefab, out var _) && base.EntityManager.TryGetComponent<SpawnableBuildingData>(component2, out var _))
		{
			float workforce = EconomyUtils.GetWorkforce(buffer2, ref citizens);
			info.Add(new InfoList.Item($"Workforce Per Tick:{workforce}"));
			DynamicBuffer<Efficiency> buffer3;
			float num = (base.EntityManager.TryGetBuffer(component.m_Property, isReadOnly: true, out buffer3) ? BuildingUtils.GetEfficiency(buffer3) : 1f);
			info.Add(new InfoList.Item($"Building Efficiency:{num}"));
		}
		if (base.EntityManager.TryGetComponent<BuyingCompany>(entity, out var component5))
		{
			info.Add(new InfoList.Item("Trip Length: " + component5.m_MeanInputTripLength));
		}
	}

	private bool HasOwnerInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.HasComponent<Vehicle>(entity))
		{
			return base.EntityManager.HasComponent<Owner>(entity);
		}
		return false;
	}

	private void UpdateOwnerInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		Owner componentData = base.EntityManager.GetComponentData<Owner>(entity);
		info.label = "Owner";
		info.value = m_NameSystem.GetDebugName(componentData.m_Owner);
		info.target = componentData.m_Owner;
	}

	private bool HasKeeperInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.HasComponent<Vehicle>(entity) && base.EntityManager.TryGetComponent<Game.Vehicles.PersonalCar>(entity, out var component))
		{
			return component.m_Keeper != InfoList.Item.kNullEntity;
		}
		return false;
	}

	private void UpdateKeeperInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		Game.Vehicles.PersonalCar componentData = base.EntityManager.GetComponentData<Game.Vehicles.PersonalCar>(entity);
		info.label = "Keeper";
		info.value = m_NameSystem.GetDebugName(componentData.m_Keeper);
		info.target = componentData.m_Keeper;
	}

	private bool HasControllerInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.HasComponent<Vehicle>(entity) && base.EntityManager.TryGetComponent<Controller>(entity, out var component))
		{
			return component.m_Controller != InfoList.Item.kNullEntity;
		}
		return false;
	}

	private void UpdateControllerInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		Controller componentData = base.EntityManager.GetComponentData<Controller>(entity);
		info.label = "Controller";
		info.value = m_NameSystem.GetDebugName(componentData.m_Controller);
		info.target = componentData.m_Controller;
	}

	private bool HasTransferRequestInfo(Entity entity, Entity prefab)
	{
		return base.EntityManager.HasComponent<StorageTransferRequest>(entity);
	}

	private void UpdateTransferRequestInfo(Entity entity, Entity prefab, InfoList info)
	{
		DynamicBuffer<StorageTransferRequest> buffer = base.EntityManager.GetBuffer<StorageTransferRequest>(entity, isReadOnly: true);
		info.label = "Transfer requests";
		for (int i = 0; i < buffer.Length; i++)
		{
			StorageTransferRequest storageTransferRequest = buffer[i];
			info.Add(new InfoList.Item(string.Format("{0} {1} {2} {3} {4}", storageTransferRequest.m_Amount, EconomyUtils.GetName(storageTransferRequest.m_Resource), ((storageTransferRequest.m_Flags & StorageTransferFlags.Incoming) != 0) ? " from " : " to ", storageTransferRequest.m_Target.Index, ((storageTransferRequest.m_Flags & StorageTransferFlags.Car) != 0) ? "(C)" : "")));
		}
	}

	private bool HasPassengerInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<Vehicle>(entity))
		{
			return false;
		}
		int num = 0;
		PublicTransportVehicleData component2;
		AmbulanceData component3;
		HearseData component4;
		PoliceCarData component5;
		TaxiData component6;
		if (base.EntityManager.TryGetComponent<PersonalCarData>(prefab, out var component))
		{
			num = component.m_PassengerCapacity;
		}
		else if (base.EntityManager.TryGetComponent<PublicTransportVehicleData>(prefab, out component2))
		{
			num = component2.m_PassengerCapacity;
		}
		else if (base.EntityManager.TryGetComponent<AmbulanceData>(prefab, out component3))
		{
			num = component3.m_PatientCapacity;
		}
		else if (base.EntityManager.TryGetComponent<HearseData>(prefab, out component4))
		{
			num = component4.m_CorpseCapacity;
		}
		else if (base.EntityManager.TryGetComponent<PoliceCarData>(prefab, out component5))
		{
			num = component5.m_CriminalCapacity;
		}
		else if (base.EntityManager.TryGetComponent<TaxiData>(prefab, out component6))
		{
			num = component6.m_PassengerCapacity;
		}
		return num > 0;
	}

	private void UpdatePassengerInfo(Entity entity, Entity prefab, CapacityInfo info)
	{
		int max = 0;
		int value = 0;
		PublicTransportVehicleData component2;
		AmbulanceData component3;
		HearseData component4;
		PoliceCarData component5;
		TaxiData component6;
		if (base.EntityManager.TryGetComponent<PersonalCarData>(prefab, out var component))
		{
			max = component.m_PassengerCapacity;
		}
		else if (base.EntityManager.TryGetComponent<PublicTransportVehicleData>(prefab, out component2))
		{
			max = component2.m_PassengerCapacity;
		}
		else if (base.EntityManager.TryGetComponent<AmbulanceData>(prefab, out component3))
		{
			max = component3.m_PatientCapacity;
		}
		else if (base.EntityManager.TryGetComponent<HearseData>(prefab, out component4))
		{
			max = component4.m_CorpseCapacity;
		}
		else if (base.EntityManager.TryGetComponent<PoliceCarData>(prefab, out component5))
		{
			max = component5.m_CriminalCapacity;
		}
		else if (base.EntityManager.TryGetComponent<TaxiData>(prefab, out component6))
		{
			max = component6.m_PassengerCapacity;
		}
		if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Passenger> buffer))
		{
			value = buffer.Length;
		}
		info.label = "Passengers";
		info.value = value;
		info.max = max;
	}

	private bool HasPersonalCarInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<Vehicle>(entity))
		{
			return false;
		}
		if (base.EntityManager.HasComponent<Game.Vehicles.PersonalCar>(entity))
		{
			return base.EntityManager.HasComponent<PersonalCarData>(prefab);
		}
		return false;
	}

	private void UpdatePersonalCarInfo(Entity entity, Entity prefab, InfoList info)
	{
		Game.Vehicles.PersonalCar componentData = base.EntityManager.GetComponentData<Game.Vehicles.PersonalCar>(entity);
		info.label = "Personal Car";
		if (base.EntityManager.HasComponent<ParkedCar>(entity))
		{
			info.Add(new InfoList.Item("Parked"));
		}
		if ((componentData.m_State & PersonalCarFlags.Boarding) != 0)
		{
			info.Add(new InfoList.Item("Boarding"));
		}
		else if ((componentData.m_State & PersonalCarFlags.Disembarking) != 0)
		{
			info.Add(new InfoList.Item("Disembarking"));
		}
		else if ((componentData.m_State & PersonalCarFlags.Transporting) != 0)
		{
			info.Add(new InfoList.Item("Transporting"));
		}
		if ((componentData.m_State & PersonalCarFlags.DummyTraffic) != 0)
		{
			info.Add(new InfoList.Item("Dummy Traffic"));
		}
		if ((componentData.m_State & PersonalCarFlags.HomeTarget) != 0)
		{
			info.Add(new InfoList.Item("Home Target"));
		}
	}

	private bool HasDeliveryTruckInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<Vehicle>(entity))
		{
			return false;
		}
		if (base.EntityManager.HasComponent<Game.Vehicles.DeliveryTruck>(entity))
		{
			return base.EntityManager.HasComponent<DeliveryTruckData>(prefab);
		}
		return false;
	}

	private void UpdateDeliveryTruckInfo(Entity entity, Entity prefab, InfoList info)
	{
		Game.Vehicles.DeliveryTruck componentData = base.EntityManager.GetComponentData<Game.Vehicles.DeliveryTruck>(entity);
		DeliveryTruckData componentData2 = base.EntityManager.GetComponentData<DeliveryTruckData>(prefab);
		Resource resource = Resource.NoResource;
		int num = 0;
		int num2 = 0;
		if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<LayoutElement> buffer) && buffer.Length != 0)
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				Entity vehicle = buffer[i].m_Vehicle;
				if (base.EntityManager.TryGetComponent<Game.Vehicles.DeliveryTruck>(vehicle, out var component))
				{
					resource |= component.m_Resource;
					if ((component.m_State & DeliveryTruckFlags.Loaded) != 0)
					{
						num += component.m_Amount;
					}
					Entity prefab2 = base.EntityManager.GetComponentData<PrefabRef>(vehicle).m_Prefab;
					if (base.EntityManager.TryGetComponent<DeliveryTruckData>(prefab2, out var component2))
					{
						num2 += component2.m_CargoCapacity;
					}
				}
			}
		}
		else
		{
			resource = componentData.m_Resource;
			if ((componentData.m_State & DeliveryTruckFlags.Loaded) != 0)
			{
				num = componentData.m_Amount;
			}
			num2 = componentData2.m_CargoCapacity;
		}
		bool flag = (componentData.m_State & DeliveryTruckFlags.StorageTransfer) != 0;
		bool flag2 = (componentData.m_State & DeliveryTruckFlags.Buying) != 0;
		bool flag3 = (componentData.m_State & DeliveryTruckFlags.Returning) != 0;
		bool flag4 = (componentData.m_State & DeliveryTruckFlags.Delivering) != 0;
		info.label = "Delivery Truck";
		if ((componentData.m_State & DeliveryTruckFlags.DummyTraffic) != 0)
		{
			info.Add(new InfoList.Item("Dummy Traffic"));
		}
		info.Add(new InfoList.Item("Cargo: " + num + "/" + num2));
		if (base.EntityManager.TryGetComponent<Target>(entity, out var component3))
		{
			if (flag2)
			{
				string text = (flag3 ? "Bought " : "Buying ");
				string text2 = (flag3 ? string.Empty : ("from " + m_NameSystem.GetDebugName(component3.m_Target)));
				Entity entity2 = (flag3 ? InfoList.Item.kNullEntity : component3.m_Target);
				info.Add(new InfoList.Item(string.Concat(text, resource, text2), entity2));
			}
			else if (flag)
			{
				string text3 = (flag3 ? "Exported " : "Exporting ");
				string text4 = (flag3 ? string.Empty : ("to " + m_NameSystem.GetDebugName(component3.m_Target)));
				Entity entity3 = (flag3 ? InfoList.Item.kNullEntity : component3.m_Target);
				info.Add(new InfoList.Item(string.Concat(text3, resource, text4), entity3));
			}
			else if (flag4)
			{
				string text5 = (flag3 ? "Delivered " : "Delivering ");
				string text6 = (flag3 ? string.Empty : ("to " + m_NameSystem.GetDebugName(component3.m_Target)));
				Entity entity4 = (flag3 ? InfoList.Item.kNullEntity : component3.m_Target);
				info.Add(new InfoList.Item(string.Concat(text5, resource, text6), entity4));
			}
			else
			{
				string text7 = (flag3 ? "Transported " : "Transporting ");
				string text8 = (flag3 ? string.Empty : ("to " + m_NameSystem.GetDebugName(component3.m_Target)));
				Entity entity5 = (flag3 ? InfoList.Item.kNullEntity : component3.m_Target);
				info.Add(new InfoList.Item(string.Concat(text7, resource, text8), entity5));
			}
		}
		if (flag3)
		{
			info.Add(new InfoList.Item("Returning"));
		}
	}

	private bool HasAmbulanceInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<Vehicle>(entity))
		{
			return false;
		}
		if (base.EntityManager.HasComponent<Game.Vehicles.Ambulance>(entity))
		{
			return base.EntityManager.HasComponent<AmbulanceData>(prefab);
		}
		return false;
	}

	private void UpdateAmbulanceInfo(Entity entity, Entity prefab, InfoList info)
	{
		Game.Vehicles.Ambulance componentData = base.EntityManager.GetComponentData<Game.Vehicles.Ambulance>(entity);
		info.label = "Ambulance";
		if (base.EntityManager.TryGetComponent<Target>(entity, out var component))
		{
			if (componentData.m_TargetPatient != InfoList.Item.kNullEntity)
			{
				info.Add(new InfoList.Item("Patient" + m_NameSystem.GetDebugName(componentData.m_TargetPatient), componentData.m_TargetPatient));
			}
			if ((componentData.m_State & AmbulanceFlags.Returning) != 0)
			{
				info.Add(new InfoList.Item("Returning"));
			}
			else if ((componentData.m_State & AmbulanceFlags.Transporting) != 0)
			{
				info.Add(new InfoList.Item("Transporting to: " + m_NameSystem.GetDebugName(component.m_Target), component.m_Target));
			}
			else
			{
				info.Add(new InfoList.Item("Picking up from: " + m_NameSystem.GetDebugName(component.m_Target), component.m_Target));
			}
		}
	}

	private bool HasHearseInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<Vehicle>(entity))
		{
			return false;
		}
		if (base.EntityManager.HasComponent<Game.Vehicles.Hearse>(entity))
		{
			return base.EntityManager.HasComponent<HearseData>(prefab);
		}
		return false;
	}

	private void UpdateHearseInfo(Entity entity, Entity prefab, InfoList info)
	{
		Game.Vehicles.Hearse componentData = base.EntityManager.GetComponentData<Game.Vehicles.Hearse>(entity);
		info.label = "Hearse";
		if (base.EntityManager.TryGetComponent<Target>(entity, out var component))
		{
			if (componentData.m_TargetCorpse != InfoList.Item.kNullEntity)
			{
				info.Add(new InfoList.Item("Body" + m_NameSystem.GetDebugName(componentData.m_TargetCorpse), componentData.m_TargetCorpse));
			}
			if ((componentData.m_State & HearseFlags.Returning) != 0)
			{
				info.Add(new InfoList.Item("Returning"));
			}
			else if ((componentData.m_State & HearseFlags.Transporting) != 0)
			{
				info.Add(new InfoList.Item("Transporting to" + m_NameSystem.GetDebugName(component.m_Target), component.m_Target));
			}
			else
			{
				info.Add(new InfoList.Item("Picking up from" + m_NameSystem.GetDebugName(component.m_Target), component.m_Target));
			}
		}
	}

	private bool HasGarbageTruckInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<Vehicle>(entity))
		{
			return false;
		}
		if (base.EntityManager.HasComponent<Game.Vehicles.GarbageTruck>(entity))
		{
			return base.EntityManager.HasComponent<GarbageTruckData>(prefab);
		}
		return false;
	}

	private void UpdateGarbageTruckInfo(Entity entity, Entity prefab, InfoList info)
	{
		Game.Vehicles.GarbageTruck componentData = base.EntityManager.GetComponentData<Game.Vehicles.GarbageTruck>(entity);
		GarbageTruckData componentData2 = base.EntityManager.GetComponentData<GarbageTruckData>(prefab);
		info.label = "Garbage Truck";
		info.Add(new InfoList.Item("Capacity: " + componentData.m_Garbage + "/" + componentData2.m_GarbageCapacity));
		if ((componentData.m_State & GarbageTruckFlags.Unloading) != 0)
		{
			info.Add(new InfoList.Item("Unloading"));
		}
		else if ((componentData.m_State & GarbageTruckFlags.Returning) != 0)
		{
			info.Add(new InfoList.Item("Returning"));
		}
	}

	private bool HasPublicTransportInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<Vehicle>(entity))
		{
			return false;
		}
		if (base.EntityManager.HasComponent<Game.Vehicles.PublicTransport>(entity))
		{
			return base.EntityManager.HasComponent<PublicTransportVehicleData>(prefab);
		}
		return false;
	}

	private void UpdatePublicTransportInfo(Entity entity, Entity prefab, InfoList info)
	{
		Game.Vehicles.PublicTransport componentData = base.EntityManager.GetComponentData<Game.Vehicles.PublicTransport>(entity);
		info.label = "Public Transport";
		if ((componentData.m_State & PublicTransportFlags.DummyTraffic) != 0)
		{
			info.Add(new InfoList.Item("Dummy Traffic"));
		}
		if (base.EntityManager.TryGetComponent<CurrentRoute>(entity, out var component))
		{
			info.Add(new InfoList.Item("Line: " + m_NameSystem.GetDebugName(component.m_Route), component.m_Route));
		}
		if ((componentData.m_State & PublicTransportFlags.Returning) != 0)
		{
			info.Add(new InfoList.Item("Returning"));
		}
		else if ((componentData.m_State & PublicTransportFlags.Boarding) != 0)
		{
			info.Add(new InfoList.Item("Boarding"));
			if (m_SimulationSystem.frameIndex < componentData.m_DepartureFrame)
			{
				int num = Mathf.CeilToInt((float)(componentData.m_DepartureFrame - m_SimulationSystem.frameIndex) / 60f);
				info.Add(new InfoList.Item("Departure: " + num + "s"));
			}
			else
			{
				Entity passengerWaiting = GetPassengerWaiting(entity);
				if (passengerWaiting != Entity.Null)
				{
					info.Add(new InfoList.Item("Waiting for: " + m_NameSystem.GetDebugName(passengerWaiting), passengerWaiting));
				}
			}
		}
		else if ((componentData.m_State & PublicTransportFlags.EnRoute) != 0)
		{
			info.Add(new InfoList.Item("En route"));
		}
		if (base.EntityManager.TryGetComponent<PublicTransportVehicleData>(prefab, out var component2))
		{
			Odometer component3;
			if ((componentData.m_State & PublicTransportFlags.RequiresMaintenance) != 0)
			{
				info.Add(new InfoList.Item("Maintenance scheduled"));
			}
			else if (component2.m_MaintenanceRange > 0.1f && base.EntityManager.TryGetComponent<Odometer>(entity, out component3))
			{
				int num2 = Mathf.RoundToInt(component2.m_MaintenanceRange * 0.001f);
				int num3 = math.max(0, Mathf.RoundToInt((component2.m_MaintenanceRange - component3.m_Distance) * 0.001f));
				info.Add(new InfoList.Item("Remaining range: " + num3 + "/" + num2));
			}
		}
		if (GetRoutePosition(entity, out var nextWaypointIndex, out var segmentPosition))
		{
			info.Add(new InfoList.Item("Route waypoint index: " + nextWaypointIndex));
			info.Add(new InfoList.Item("Route segment position: " + Mathf.RoundToInt(segmentPosition * 100f) + "%"));
		}
	}

	private Entity GetPassengerWaiting(Entity vehicleEntity)
	{
		if (base.EntityManager.TryGetBuffer(vehicleEntity, isReadOnly: true, out DynamicBuffer<LayoutElement> buffer))
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				Entity passengerWaiting = GetPassengerWaiting2(buffer[i].m_Vehicle);
				if (passengerWaiting != Entity.Null)
				{
					return passengerWaiting;
				}
			}
			return Entity.Null;
		}
		return GetPassengerWaiting2(vehicleEntity);
	}

	private Entity GetPassengerWaiting2(Entity vehicleEntity)
	{
		if (base.EntityManager.TryGetBuffer(vehicleEntity, isReadOnly: true, out DynamicBuffer<Passenger> buffer))
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				Entity passenger = buffer[i].m_Passenger;
				if (base.EntityManager.TryGetComponent<CurrentVehicle>(passenger, out var component) && (component.m_Flags & CreatureVehicleFlags.Ready) == 0)
				{
					return passenger;
				}
			}
		}
		return Entity.Null;
	}

	private bool GetRoutePosition(Entity transportVehicle, out int nextWaypointIndex, out float segmentPosition)
	{
		if (base.EntityManager.TryGetComponent<CurrentRoute>(transportVehicle, out var component))
		{
			if (base.EntityManager.TryGetComponent<PathInformation>(transportVehicle, out var component2) && base.EntityManager.TryGetComponent<Waypoint>(component2.m_Destination, out var component3) && base.EntityManager.TryGetBuffer(component.m_Route, isReadOnly: true, out DynamicBuffer<RouteSegment> buffer))
			{
				nextWaypointIndex = component3.m_Index;
				int index = math.select(nextWaypointIndex - 1, buffer.Length - 1, nextWaypointIndex == 0);
				RouteSegment routeSegment = buffer[index];
				if (base.EntityManager.TryGetBuffer(routeSegment.m_Segment, isReadOnly: true, out DynamicBuffer<PathElement> buffer2) && buffer2.Length != 0)
				{
					int num = 0;
					if (base.EntityManager.TryGetComponent<PathOwner>(transportVehicle, out var component4) && base.EntityManager.TryGetBuffer(transportVehicle, isReadOnly: true, out DynamicBuffer<PathElement> buffer3))
					{
						num += math.max(0, buffer3.Length - component4.m_ElementIndex);
					}
					DynamicBuffer<TrainNavigationLane> buffer5;
					DynamicBuffer<WatercraftNavigationLane> buffer6;
					DynamicBuffer<AircraftNavigationLane> buffer7;
					if (base.EntityManager.TryGetBuffer(transportVehicle, isReadOnly: true, out DynamicBuffer<CarNavigationLane> buffer4))
					{
						num += buffer4.Length;
					}
					else if (base.EntityManager.TryGetBuffer(transportVehicle, isReadOnly: true, out buffer5))
					{
						num += buffer5.Length;
					}
					else if (base.EntityManager.TryGetBuffer(transportVehicle, isReadOnly: true, out buffer6))
					{
						num += buffer6.Length;
					}
					else if (base.EntityManager.TryGetBuffer(transportVehicle, isReadOnly: true, out buffer7))
					{
						num += buffer7.Length;
					}
					segmentPosition = math.saturate((float)(buffer2.Length - num) / (float)buffer2.Length);
					return true;
				}
			}
			if (base.EntityManager.TryGetComponent<Target>(transportVehicle, out var component5) && base.EntityManager.TryGetComponent<Waypoint>(component5.m_Target, out component3) && base.EntityManager.TryGetBuffer(component.m_Route, isReadOnly: true, out DynamicBuffer<RouteWaypoint> buffer8))
			{
				nextWaypointIndex = component3.m_Index;
				int index2 = math.select(nextWaypointIndex - 1, buffer8.Length - 1, nextWaypointIndex == 0);
				RouteWaypoint routeWaypoint = buffer8[index2];
				if (base.EntityManager.TryGetComponent<Game.Objects.Transform>(transportVehicle, out var component6) && base.EntityManager.TryGetComponent<Position>(routeWaypoint.m_Waypoint, out var component7) && base.EntityManager.TryGetComponent<Position>(component5.m_Target, out var component8))
				{
					float num2 = math.distance(component6.m_Position, component8.m_Position);
					float num3 = math.max(1f, math.distance(component7.m_Position, component8.m_Position));
					segmentPosition = math.saturate((num3 - num2) / num3);
					return true;
				}
			}
		}
		nextWaypointIndex = 0;
		segmentPosition = 0f;
		return false;
	}

	private bool HasCargoTransportInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<Vehicle>(entity))
		{
			return false;
		}
		if (base.EntityManager.HasComponent<Game.Vehicles.CargoTransport>(entity))
		{
			return base.EntityManager.HasComponent<CargoTransportVehicleData>(prefab);
		}
		return false;
	}

	private void UpdateCargoTransportInfo(Entity entity, Entity prefab, InfoList info)
	{
		Game.Vehicles.CargoTransport componentData = base.EntityManager.GetComponentData<Game.Vehicles.CargoTransport>(entity);
		CargoTransportVehicleData componentData2 = base.EntityManager.GetComponentData<CargoTransportVehicleData>(prefab);
		info.label = "Cargo Transport";
		if ((componentData.m_State & CargoTransportFlags.DummyTraffic) != 0)
		{
			info.Add(new InfoList.Item("Dummy Traffic"));
		}
		if (base.EntityManager.TryGetComponent<CurrentRoute>(entity, out var component))
		{
			info.Add(new InfoList.Item("Route: " + m_NameSystem.GetDebugName(component.m_Route), component.m_Route));
		}
		if ((componentData.m_State & CargoTransportFlags.Returning) != 0)
		{
			info.Add(new InfoList.Item("Returning"));
		}
		else if ((componentData.m_State & CargoTransportFlags.Boarding) != 0)
		{
			info.Add(new InfoList.Item("Loading"));
			if (m_SimulationSystem.frameIndex < componentData.m_DepartureFrame)
			{
				int num = Mathf.CeilToInt((float)(componentData.m_DepartureFrame - m_SimulationSystem.frameIndex) / 60f);
				info.Add(new InfoList.Item("Departure: " + num + "s"));
			}
		}
		else if ((componentData.m_State & CargoTransportFlags.EnRoute) != 0)
		{
			info.Add(new InfoList.Item("En route"));
		}
		if (base.EntityManager.TryGetComponent<CargoTransportVehicleData>(prefab, out var component2))
		{
			Odometer component3;
			if ((componentData.m_State & CargoTransportFlags.RequiresMaintenance) != 0)
			{
				info.Add(new InfoList.Item("Maintenance scheduled"));
			}
			else if (component2.m_MaintenanceRange > 0.1f && base.EntityManager.TryGetComponent<Odometer>(entity, out component3))
			{
				int num2 = Mathf.RoundToInt(component2.m_MaintenanceRange * 0.001f);
				int num3 = math.max(0, Mathf.RoundToInt((component2.m_MaintenanceRange - component3.m_Distance) * 0.001f));
				info.Add(new InfoList.Item("Remaining range: " + num3 + "/" + num2));
			}
		}
		NativeList<Game.Economy.Resources> target = new NativeList<Game.Economy.Resources>(32, Allocator.Temp);
		int num4 = 0;
		if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<LayoutElement> buffer))
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				Entity vehicle = buffer[i].m_Vehicle;
				if (base.EntityManager.TryGetBuffer(vehicle, isReadOnly: true, out DynamicBuffer<Game.Economy.Resources> buffer2))
				{
					AddResources(buffer2, target);
				}
				if (base.EntityManager.TryGetComponent<PrefabRef>(vehicle, out var component4) && base.EntityManager.TryGetComponent<CargoTransportVehicleData>(component4.m_Prefab, out var component5))
				{
					num4 += component5.m_CargoCapacity;
				}
			}
		}
		else
		{
			if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Game.Economy.Resources> buffer3))
			{
				AddResources(buffer3, target);
			}
			num4 += componentData2.m_CargoCapacity;
		}
		info.Add(new InfoList.Item("Cargo: "));
		int num5 = 0;
		for (int j = 0; j < target.Length; j++)
		{
			Game.Economy.Resources resources = target[j];
			info.Add(new InfoList.Item(string.Concat(resources.m_Resource, " ", resources.m_Amount)));
			num5 += resources.m_Amount;
		}
		info.Add(new InfoList.Item("Capacity " + num5 + "/" + num4));
		target.Dispose();
	}

	private void AddResources(DynamicBuffer<Game.Economy.Resources> source, NativeList<Game.Economy.Resources> target)
	{
		for (int i = 0; i < source.Length; i++)
		{
			Game.Economy.Resources value = source[i];
			if (value.m_Amount == 0)
			{
				continue;
			}
			int num = 0;
			while (true)
			{
				if (num < target.Length)
				{
					Game.Economy.Resources value2 = target[num];
					if (value2.m_Resource == value.m_Resource)
					{
						value2.m_Amount += value.m_Amount;
						target[num] = value2;
						break;
					}
					num++;
					continue;
				}
				target.Add(in value);
				break;
			}
		}
	}

	private bool HasMaintenanceVehicleInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<Vehicle>(entity))
		{
			return false;
		}
		if (base.EntityManager.HasComponent<Game.Vehicles.MaintenanceVehicle>(entity))
		{
			return base.EntityManager.HasComponent<MaintenanceVehicleData>(prefab);
		}
		return false;
	}

	private void UpdateMaintenanceVehicleInfo(Entity entity, Entity prefab, InfoList info)
	{
		Game.Vehicles.MaintenanceVehicle componentData = base.EntityManager.GetComponentData<Game.Vehicles.MaintenanceVehicle>(entity);
		MaintenanceVehicleData componentData2 = base.EntityManager.GetComponentData<MaintenanceVehicleData>(prefab);
		componentData2.m_MaintenanceCapacity = Mathf.CeilToInt((float)componentData2.m_MaintenanceCapacity * componentData.m_Efficiency);
		info.label = "Maintenance Vehicle";
		info.Add(new InfoList.Item("Work shift: " + $"{Mathf.CeilToInt(math.select((float)componentData.m_Maintained / (float)componentData2.m_MaintenanceCapacity, 0f, componentData2.m_MaintenanceCapacity == 0) * 100f)}%"));
		if ((componentData.m_State & MaintenanceVehicleFlags.ClearingDebris) != 0)
		{
			info.Add(new InfoList.Item("Clearing debris"));
		}
		else if ((componentData.m_State & MaintenanceVehicleFlags.Returning) != 0)
		{
			info.Add(new InfoList.Item("Returning"));
		}
	}

	private bool HasPostVanInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<Vehicle>(entity))
		{
			return false;
		}
		if (base.EntityManager.HasComponent<Game.Vehicles.PostVan>(entity))
		{
			return base.EntityManager.HasComponent<PostVanData>(prefab);
		}
		return false;
	}

	private void UpdatePostVanInfo(Entity entity, Entity prefab, InfoList info)
	{
		Game.Vehicles.PostVan componentData = base.EntityManager.GetComponentData<Game.Vehicles.PostVan>(entity);
		PostVanData componentData2 = base.EntityManager.GetComponentData<PostVanData>(prefab);
		info.label = "Post Van";
		info.Add(new InfoList.Item("Mail to deliver: " + componentData.m_DeliveringMail + "/" + componentData2.m_MailCapacity));
		info.Add(new InfoList.Item("Collected mail: " + componentData.m_CollectedMail + "/" + componentData2.m_MailCapacity));
		if ((componentData.m_State & PostVanFlags.Returning) != 0)
		{
			info.Add(new InfoList.Item("Returning"));
		}
	}

	private bool HasFireEngineInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<Vehicle>(entity))
		{
			return false;
		}
		if (base.EntityManager.HasComponent<Game.Vehicles.FireEngine>(entity) && base.EntityManager.HasComponent<FireEngineData>(prefab))
		{
			return base.EntityManager.HasComponent<ServiceDispatch>(entity);
		}
		return false;
	}

	private void UpdateFireEngineInfo(Entity entity, Entity prefab, InfoList info)
	{
		Game.Vehicles.FireEngine componentData = base.EntityManager.GetComponentData<Game.Vehicles.FireEngine>(entity);
		FireEngineData componentData2 = base.EntityManager.GetComponentData<FireEngineData>(prefab);
		DynamicBuffer<ServiceDispatch> buffer = base.EntityManager.GetBuffer<ServiceDispatch>(entity, isReadOnly: true);
		info.label = "Fire Engine";
		int num = Mathf.CeilToInt(componentData.m_ExtinguishingAmount);
		int num2 = Mathf.CeilToInt(componentData2.m_ExtinguishingCapacity);
		if (num2 > 0)
		{
			info.Add(new InfoList.Item("Load: " + num + "/" + num2));
		}
		if ((componentData.m_State & FireEngineFlags.Extinguishing) != 0)
		{
			info.Add(new InfoList.Item("Extinguishing"));
		}
		else if ((componentData.m_State & FireEngineFlags.Rescueing) != 0)
		{
			info.Add(new InfoList.Item("Searching for survivors"));
		}
		else if ((componentData.m_State & FireEngineFlags.Returning) != 0)
		{
			info.Add(new InfoList.Item("Returning"));
		}
		else
		{
			if (componentData.m_RequestCount <= 0 || buffer.Length <= 0)
			{
				return;
			}
			ServiceDispatch serviceDispatch = buffer[0];
			if (base.EntityManager.TryGetComponent<FireRescueRequest>(serviceDispatch.m_Request, out var component))
			{
				Destroyed component3;
				if (base.EntityManager.TryGetComponent<OnFire>(component.m_Target, out var component2) && component2.m_Event != InfoList.Item.kNullEntity)
				{
					info.Add(new InfoList.Item("Dispatched" + m_NameSystem.GetDebugName(component2.m_Event), component2.m_Event));
				}
				else if (base.EntityManager.TryGetComponent<Destroyed>(component.m_Target, out component3) && component3.m_Event != InfoList.Item.kNullEntity)
				{
					info.Add(new InfoList.Item("Dispatched" + m_NameSystem.GetDebugName(component3.m_Event), component3.m_Event));
				}
				else if (component.m_Target != InfoList.Item.kNullEntity)
				{
					info.Add(new InfoList.Item("Dispatched" + m_NameSystem.GetDebugName(component.m_Target), component.m_Target));
				}
			}
		}
	}

	private bool HasPoliceCarInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<Vehicle>(entity))
		{
			return false;
		}
		if (base.EntityManager.HasComponent<Game.Vehicles.PoliceCar>(entity) && base.EntityManager.HasComponent<PoliceCarData>(prefab))
		{
			return base.EntityManager.HasComponent<ServiceDispatch>(entity);
		}
		return false;
	}

	private void UpdatePoliceCarInfo(Entity entity, Entity prefab, InfoList info)
	{
		Game.Vehicles.PoliceCar componentData = base.EntityManager.GetComponentData<Game.Vehicles.PoliceCar>(entity);
		PoliceCarData componentData2 = base.EntityManager.GetComponentData<PoliceCarData>(prefab);
		DynamicBuffer<ServiceDispatch> buffer = base.EntityManager.GetBuffer<ServiceDispatch>(entity, isReadOnly: true);
		info.label = "Police Car";
		if (componentData2.m_ShiftDuration != 0)
		{
			uint num = math.min(componentData.m_ShiftTime, componentData2.m_ShiftDuration);
			info.Add(new InfoList.Item("Work shift: " + num + "/" + componentData2.m_ShiftDuration));
		}
		if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Passenger> buffer2))
		{
			for (int i = 0; i < buffer2.Length; i++)
			{
				Entity entity2 = buffer2[i].m_Passenger;
				if (base.EntityManager.TryGetComponent<Game.Creatures.Resident>(entity2, out var component))
				{
					entity2 = component.m_Citizen;
				}
				if (base.EntityManager.HasComponent<Citizen>(entity2))
				{
					info.Add(new InfoList.Item("Arrested criminal" + m_NameSystem.GetDebugName(entity2), entity2));
				}
			}
		}
		if ((componentData.m_State & PoliceCarFlags.Returning) != 0)
		{
			info.Add(new InfoList.Item("Returning"));
		}
		else if ((componentData.m_State & PoliceCarFlags.AccidentTarget) != 0)
		{
			if ((componentData.m_State & PoliceCarFlags.AtTarget) != 0)
			{
				if (componentData.m_RequestCount <= 0 || buffer.Length <= 0)
				{
					return;
				}
				ServiceDispatch serviceDispatch = buffer[0];
				if (base.EntityManager.TryGetComponent<PoliceEmergencyRequest>(serviceDispatch.m_Request, out var component2) && base.EntityManager.TryGetComponent<AccidentSite>(component2.m_Site, out var component3))
				{
					if ((component3.m_Flags & AccidentSiteFlags.TrafficAccident) != 0)
					{
						info.Add(new InfoList.Item("Securing accident site"));
					}
					else if ((component3.m_Flags & AccidentSiteFlags.CrimeScene) != 0)
					{
						info.Add(new InfoList.Item("Securing crime scene"));
					}
				}
			}
			else
			{
				if (componentData.m_RequestCount <= 0 || buffer.Length <= 0)
				{
					return;
				}
				ServiceDispatch serviceDispatch2 = buffer[0];
				if (base.EntityManager.TryGetComponent<PoliceEmergencyRequest>(serviceDispatch2.m_Request, out var component4))
				{
					if (base.EntityManager.TryGetComponent<AccidentSite>(component4.m_Site, out var component5) && component5.m_Event != InfoList.Item.kNullEntity)
					{
						info.Add(new InfoList.Item("Dispatched" + m_NameSystem.GetDebugName(component5.m_Event), component5.m_Event));
					}
					else
					{
						info.Add(new InfoList.Item("Dispatched" + m_NameSystem.GetDebugName(component4.m_Site), component4.m_Site));
					}
				}
			}
		}
		else
		{
			info.Add(new InfoList.Item("Patrolling"));
		}
	}

	private bool HasCitizenInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.HasComponent<HouseholdMember>(entity))
		{
			return base.EntityManager.HasComponent<Citizen>(entity);
		}
		return false;
	}

	private void UpdateCitizenInfo(Entity entity, Entity prefab, InfoList info)
	{
		Citizen citizen = base.EntityManager.GetComponentData<Citizen>(entity);
		Entity household = base.EntityManager.GetComponentData<HouseholdMember>(entity).m_Household;
		Household householdData = default(Household);
		if (base.EntityManager.HasComponent<Household>(household))
		{
			householdData = base.EntityManager.GetComponentData<Household>(household);
			base.EntityManager.GetBuffer<HouseholdCitizen>(household, isReadOnly: true);
		}
		EconomyParameterData economyParameters = __query_746694607_4.GetSingleton<EconomyParameterData>();
		CitizenHappinessParameterData data = __query_746694607_7.GetSingleton<CitizenHappinessParameterData>();
		bool flag = (citizen.m_State & CitizenFlags.Tourist) != 0;
		bool flag2 = (citizen.m_State & CitizenFlags.Commuter) != 0;
		info.label = "Citizen";
		if (!flag2)
		{
			Entity entity2 = InfoList.Item.kNullEntity;
			if (base.EntityManager.TryGetComponent<CurrentBuilding>(entity, out var component))
			{
				entity2 = component.m_CurrentBuilding;
			}
			info.Add(new InfoList.Item("Current Building: " + entity2, entity2));
			info.Add(new InfoList.Item("Household: " + m_NameSystem.GetDebugName(household), household));
			info.Add(new InfoList.Item("Wellbeing: " + WellbeingToString(citizen.m_WellBeing) + "(" + citizen.m_WellBeing + ")"));
			info.Add(new InfoList.Item(" ---- Wellbeing (- from unemployed): " + CitizenHappinessSystem.GetUnemploymentBonuses(ref citizen, in data).y));
			info.Add(new InfoList.Item("Health: " + HealthToString(citizen.m_Health) + "(" + citizen.m_Health + ")"));
			if (base.EntityManager.HasComponent<Game.Economy.Resources>(household))
			{
				int householdTotalWealth = EconomyUtils.GetHouseholdTotalWealth(householdData, base.EntityManager.GetBuffer<Game.Economy.Resources>(household, isReadOnly: true));
				int resources = EconomyUtils.GetResources(Resource.Money, base.EntityManager.GetBuffer<Game.Economy.Resources>(household, isReadOnly: true));
				info.Add(new InfoList.Item("Household total Wealth Value: " + householdTotalWealth));
				info.Add(new InfoList.Item("Household Money: " + resources));
				if (base.EntityManager.TryGetComponent<PropertyRenter>(household, out var component2))
				{
					BufferLookup<Renter> m_RenterBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef);
					ComponentLookup<ConsumptionData> consumptionDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ConsumptionData_RO_ComponentLookup, ref base.CheckedStateRef);
					ComponentLookup<PrefabRef> prefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef);
					info.Add(new InfoList.Item("Household spendable Money: " + EconomyUtils.GetHouseholdSpendableMoney(householdData, base.EntityManager.GetBuffer<Game.Economy.Resources>(household, isReadOnly: true), ref m_RenterBufs, ref consumptionDatas, ref prefabRefs, component2)));
				}
			}
		}
		if (base.EntityManager.IsComponentEnabled<CarKeeper>(entity) && base.EntityManager.TryGetComponent<CarKeeper>(entity, out var component3))
		{
			info.Add(new InfoList.Item("Car: " + m_NameSystem.GetDebugName(component3.m_Car), component3.m_Car));
		}
		if (base.EntityManager.IsComponentEnabled<BicycleOwner>(entity) && base.EntityManager.TryGetComponent<BicycleOwner>(entity, out var component4))
		{
			info.Add(new InfoList.Item("Bicycle: " + m_NameSystem.GetDebugName(component4.m_Bicycle), component4.m_Bicycle));
		}
		if (!flag2)
		{
			info.Add(new InfoList.Item("Household total Resources: " + householdData.m_Resources));
			if (base.EntityManager.TryGetComponent<PropertyRenter>(household, out var component5))
			{
				info.Add(new InfoList.Item("Property: " + component5.m_Property.ToString(), component5.m_Property));
				info.Add(new InfoList.Item("Rent: " + component5.m_Rent));
			}
			else if (!flag)
			{
				info.Add(new InfoList.Item("Homeless"));
			}
		}
		else
		{
			info.Add(new InfoList.Item("From outside the city"));
		}
		Criminal component6;
		bool flag3 = base.EntityManager.TryGetComponent<Criminal>(entity, out component6);
		if (base.EntityManager.TryGetComponent<TravelPurpose>(entity, out var component7))
		{
			Entity entity3 = InfoList.Item.kNullEntity;
			string purposeText = GetPurposeText(component7, flag, component6, ref entity3);
			info.Add(new InfoList.Item(purposeText + " " + m_NameSystem.GetDebugName(entity3), entity3));
		}
		string text = " female";
		if ((citizen.m_State & CitizenFlags.Male) != CitizenFlags.None)
		{
			text = " male";
		}
		info.Add(new InfoList.Item(GetAgeString(entity) + "(" + citizen.GetAgeInDays(m_SimulationSystem.frameIndex, TimeData.GetSingleton(m_TimeDataQuery)).ToString(CultureInfo.InvariantCulture) + ")" + text));
		info.Add(new InfoList.Item("Leisure: " + citizen.m_LeisureCounter));
		if (base.EntityManager.TryGetComponent<HealthProblem>(entity, out var component8))
		{
			if ((component8.m_Flags & HealthProblemFlags.Sick) != HealthProblemFlags.None)
			{
				info.Add(new InfoList.Item("Sick " + m_NameSystem.GetDebugName(component8.m_Event), component8.m_Event));
			}
			else if ((component8.m_Flags & HealthProblemFlags.Injured) != HealthProblemFlags.None)
			{
				info.Add(new InfoList.Item("Injured " + m_NameSystem.GetDebugName(component8.m_Event), component8.m_Event));
			}
			else if ((component8.m_Flags & HealthProblemFlags.Dead) != HealthProblemFlags.None)
			{
				info.Add(new InfoList.Item("Dead " + m_NameSystem.GetDebugName(component8.m_Event), component8.m_Event));
			}
			else if ((component8.m_Flags & HealthProblemFlags.Trapped) != HealthProblemFlags.None)
			{
				info.Add(new InfoList.Item("Trapped " + m_NameSystem.GetDebugName(component8.m_Event), component8.m_Event));
			}
			else if ((component8.m_Flags & HealthProblemFlags.InDanger) != HealthProblemFlags.None)
			{
				info.Add(new InfoList.Item("In danger " + m_NameSystem.GetDebugName(component8.m_Event), component8.m_Event));
			}
		}
		if (flag3)
		{
			string text2 = "Criminal";
			if ((component6.m_Flags & CriminalFlags.Robber) != 0)
			{
				text2 += " Robber ";
			}
			if ((component6.m_Flags & CriminalFlags.Prisoner) != 0)
			{
				text2 = text2 + "Jail Time: " + (uint)(component6.m_JailTime * 16 * 16) / 262144u;
			}
			info.Add(new InfoList.Item(text2));
		}
		if (flag)
		{
			info.Add(new InfoList.Item("Tourist"));
			ComponentLookup<TouristHousehold> componentLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_TouristHousehold_RO_ComponentLookup, ref base.CheckedStateRef);
			if (componentLookup.HasComponent(household) && componentLookup.HasComponent(household))
			{
				TouristHousehold touristHousehold = componentLookup[household];
				if (base.EntityManager.Exists(touristHousehold.m_Hotel))
				{
					info.Add(new InfoList.Item("Staying at: " + m_NameSystem.GetDebugName(touristHousehold.m_Hotel), touristHousehold.m_Hotel));
				}
			}
		}
		if (!flag)
		{
			info.Add(new InfoList.Item(GetEducationString(citizen.GetEducationLevel())));
			if (base.EntityManager.TryGetComponent<Citizen>(entity, out var component9))
			{
				CitizenAge age = component9.GetAge();
				Game.Citizens.Student component11;
				if (base.EntityManager.TryGetComponent<Worker>(entity, out var component10))
				{
					Entity workplace = component10.m_Workplace;
					info.Add(new InfoList.Item("Works at: " + m_NameSystem.GetDebugName(workplace), workplace));
					float2 timeToWork = WorkerSystem.GetTimeToWork(citizen, component10, ref economyParameters, includeCommute: false);
					info.Add(new InfoList.Item(string.Concat("Work Shift: ", GetTimeString(timeToWork.x) + " to " + GetTimeString(timeToWork.y))));
					timeToWork = WorkerSystem.GetTimeToWork(citizen, component10, ref economyParameters, includeCommute: true);
					info.Add(new InfoList.Item(string.Concat("Work Shift(Commute): ", GetTimeString(timeToWork.x) + " to " + GetTimeString(timeToWork.y))));
				}
				else if (base.EntityManager.TryGetComponent<Game.Citizens.Student>(entity, out component11))
				{
					Entity school = component11.m_School;
					info.Add(new InfoList.Item("Studies at: " + m_NameSystem.GetDebugName(school), school));
					float2 timeToStudy = StudentSystem.GetTimeToStudy(citizen, component11, ref economyParameters);
					info.Add(new InfoList.Item(string.Concat("Study Time: ", GetTimeString(timeToStudy.x) + " to " + GetTimeString(timeToStudy.y))));
				}
				else
				{
					switch (age)
					{
					case CitizenAge.Adult:
						info.Add(new InfoList.Item("Unemployed"));
						break;
					case CitizenAge.Child:
					case CitizenAge.Teen:
						info.Add(new InfoList.Item("Not in school!"));
						break;
					}
				}
				ComponentLookup<Worker> workers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup, ref base.CheckedStateRef);
				ComponentLookup<Game.Citizens.Student> students = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Student_RO_ComponentLookup, ref base.CheckedStateRef);
				float2 sleepTime = CitizenBehaviorSystem.GetSleepTime(entity, citizen, ref economyParameters, ref workers, ref students);
				info.Add(new InfoList.Item(string.Concat("Sleep Time: ", GetTimeString(sleepTime.x) + " to " + GetTimeString(sleepTime.y))));
			}
		}
		if (base.EntityManager.TryGetComponent<AttendingMeeting>(entity, out var component12) && base.EntityManager.TryGetComponent<CoordinatedMeeting>(component12.m_Meeting, out var component13))
		{
			InfoList.Item item = ((component13.m_Target != InfoList.Item.kNullEntity) ? new InfoList.Item("Meeting at: " + m_NameSystem.GetDebugName(component13.m_Target), component13.m_Target) : new InfoList.Item("Planning a meeting"));
			info.Add(item);
		}
		if (base.EntityManager.TryGetComponent<AttendingEvent>(household, out var component14) && base.EntityManager.HasComponent<Game.Events.CalendarEvent>(component14.m_Event))
		{
			info.Add(new InfoList.Item("Participating in " + m_NameSystem.GetDebugName(component14.m_Event)));
		}
	}

	private string GetPurposeText(TravelPurpose purpose, bool tourist, Criminal criminal, ref Entity entity)
	{
		string result;
		switch (purpose.m_Purpose)
		{
		case Purpose.GoingHome:
			result = (tourist ? "Going to hotel" : "Going home");
			break;
		case Purpose.GoingToWork:
			result = "Going to work";
			break;
		case Purpose.GoingToSchool:
			result = "Going to school";
			break;
		case Purpose.Studying:
			result = "Studying";
			break;
		case Purpose.Shopping:
			result = "Buying " + EconomyUtils.GetName(purpose.m_Resource);
			break;
		case Purpose.Working:
			result = "Working";
			break;
		case Purpose.Sleeping:
			result = "Sleeping";
			break;
		case Purpose.MovingAway:
			result = "Moving away";
			break;
		case Purpose.Leisure:
			result = (tourist ? "Sightseeing" : "Spending free time");
			break;
		case Purpose.Hospital:
			result = "Seeking medical care";
			break;
		case Purpose.Safety:
			result = "Getting safe";
			break;
		case Purpose.Escape:
			result = "Escaping";
			break;
		case Purpose.EmergencyShelter:
			result = "Evacuating";
			break;
		case Purpose.Crime:
			result = "Committing crime";
			entity = criminal.m_Event;
			break;
		case Purpose.GoingToJail:
			result = "Going to jail";
			entity = criminal.m_Event;
			break;
		case Purpose.GoingToPrison:
			result = "Going to prison";
			break;
		case Purpose.InJail:
			if ((criminal.m_Flags & CriminalFlags.Sentenced) != 0)
			{
				result = "Sentenced to prison";
				break;
			}
			result = "In jail";
			entity = criminal.m_Event;
			break;
		case Purpose.InPrison:
			result = "In prison";
			break;
		case Purpose.InHospital:
			result = "Getting medical care";
			break;
		case Purpose.Deathcare:
			result = "Transferring to death care";
			break;
		case Purpose.InDeathcare:
			result = "Waiting for processing";
			break;
		case Purpose.SendMail:
			result = "Sending mail";
			break;
		case Purpose.Disappear:
			result = "Disappearing";
			break;
		case Purpose.WaitingHome:
			result = "Waiting for new home";
			break;
		case Purpose.PathFailed:
			result = "Can't reach destination";
			break;
		default:
			result = "Idling";
			break;
		}
		return result;
	}

	private static string WellbeingToString(int wellbeing)
	{
		if (wellbeing < 25)
		{
			return "Depressed";
		}
		if (wellbeing < 40)
		{
			return "Sad";
		}
		if (wellbeing < 60)
		{
			return "Neutral";
		}
		if (wellbeing < 80)
		{
			return "Content";
		}
		return "Happy";
	}

	private static string HealthToString(int wellbeing)
	{
		if (wellbeing < 25)
		{
			return "Weak";
		}
		if (wellbeing < 40)
		{
			return "Poor";
		}
		if (wellbeing < 60)
		{
			return "OK";
		}
		if (wellbeing < 80)
		{
			return "Healthy";
		}
		return "Vigorous";
	}

	private static string ConsumptionToString(int dailyConsumption, int citizens, CitizenHappinessParameterData happinessParameters)
	{
		int2 consumptionBonuses = CitizenHappinessSystem.GetConsumptionBonuses(dailyConsumption, citizens, in happinessParameters);
		int num = consumptionBonuses.x + consumptionBonuses.y;
		if (num < -15)
		{
			return "Wretched";
		}
		if (num < -5)
		{
			return "Poor";
		}
		if (num < 5)
		{
			return "Modest";
		}
		if (num < 15)
		{
			return "Comfortable";
		}
		return "Wealthy";
	}

	private static string GetLevelupTime(int condition, int levelup, int changePerDay)
	{
		if (changePerDay <= 0)
		{
			return "Decaying";
		}
		return "In " + Mathf.CeilToInt((float)(levelup - condition) / (float)changePerDay) + " days";
	}

	private string GetAgeString(Entity entity)
	{
		if (base.EntityManager.TryGetComponent<Citizen>(entity, out var component))
		{
			return component.GetAge() switch
			{
				CitizenAge.Child => "Child", 
				CitizenAge.Teen => "Teenager", 
				CitizenAge.Adult => "Adult", 
				_ => "Elderly", 
			};
		}
		return "Unknown";
	}

	private string GetEducationString(int education)
	{
		return education switch
		{
			0 => "Uneducated", 
			1 => "Poorly educated", 
			2 => "Educated", 
			3 => "Well educated", 
			4 => "Highly educated", 
			_ => "Unknown education", 
		};
	}

	private string GetTimeString(float time)
	{
		return Mathf.RoundToInt(time * 24f) + ":00";
	}

	private bool HasMailSenderInfo(Entity entity, Entity prefab)
	{
		return base.EntityManager.HasEnabledComponent<MailSender>(entity);
	}

	private void UpdateMailSenderInfo(Entity entity, Entity prefab, CapacityInfo info)
	{
		MailSender componentData = base.EntityManager.GetComponentData<MailSender>(entity);
		info.label = "Mail sender";
		info.value = componentData.m_Amount;
		info.max = 100;
	}

	private bool HasAnimalInfo(Entity entity, Entity prefab)
	{
		return base.EntityManager.HasComponent<HouseholdPet>(entity);
	}

	private void UpdateAnimalInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		HouseholdPet componentData = base.EntityManager.GetComponentData<HouseholdPet>(entity);
		info.label = "Household";
		info.value = m_NameSystem.GetDebugName(componentData.m_Household);
		info.target = componentData.m_Household;
	}

	private bool HasCreatureInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.TryGetComponent<CurrentTransport>(entity, out var component))
		{
			entity = component.m_CurrentTransport;
		}
		return base.EntityManager.HasComponent<Creature>(entity);
	}

	private void UpdateCreatureInfo(Entity entity, Entity prefab, InfoList info)
	{
		info.label = "Creature";
		if (base.EntityManager.TryGetComponent<Citizen>(entity, out var component) && base.EntityManager.TryGetComponent<CurrentTransport>(entity, out var component2))
		{
			info.Add(new InfoList.Item("Entity: " + m_NameSystem.GetDebugName(component2.m_CurrentTransport)));
			bool tourist = (component.m_State & CitizenFlags.Tourist) != 0;
			base.EntityManager.TryGetComponent<Criminal>(entity, out var component3);
			if (base.EntityManager.TryGetComponent<HumanNavigation>(component2.m_CurrentTransport, out var component4))
			{
				info.Add(new InfoList.Item("Current Activity : " + Enum.GetName(typeof(ActivityType), component4.m_TargetActivity)));
			}
			if (base.EntityManager.TryGetBuffer(component2.m_CurrentTransport, isReadOnly: true, out DynamicBuffer<Game.Objects.SubObject> buffer) && buffer.Length != 0)
			{
				info.Add(new InfoList.Item("Activity Prop: " + m_NameSystem.GetDebugName(buffer[0].m_SubObject), buffer[0].m_SubObject));
			}
			if (base.EntityManager.TryGetComponent<Divert>(component2.m_CurrentTransport, out var component5) && component5.m_Purpose != Purpose.None)
			{
				Entity entity2 = InfoList.Item.kNullEntity;
				string purposeText = GetPurposeText(new TravelPurpose
				{
					m_Purpose = component5.m_Purpose,
					m_Resource = component5.m_Resource
				}, tourist, component3, ref entity2);
				info.Add(new InfoList.Item(purposeText + " " + m_NameSystem.GetDebugName(entity2), entity2));
			}
			if (base.EntityManager.TryGetComponent<RideNeeder>(component2.m_CurrentTransport, out var component6))
			{
				if (base.EntityManager.TryGetComponent<Dispatched>(component6.m_RideRequest, out var component7) && base.EntityManager.TryGetBuffer(component7.m_Handler, isReadOnly: true, out DynamicBuffer<ServiceDispatch> buffer2) && buffer2.Length > 0 && buffer2[0].m_Request == component6.m_RideRequest)
				{
					info.Add(new InfoList.Item("Waiting for ride: " + m_NameSystem.GetDebugName(component7.m_Handler), component7.m_Handler));
				}
				else
				{
					info.Add(new InfoList.Item("Taking a taxi"));
				}
			}
			if (!base.EntityManager.TryGetComponent<HumanCurrentLane>(component2.m_CurrentTransport, out var component8))
			{
				return;
			}
			Creature componentData = base.EntityManager.GetComponentData<Creature>(component2.m_CurrentTransport);
			if ((component8.m_Flags & CreatureLaneFlags.EndReached) != 0)
			{
				if ((component8.m_Flags & CreatureLaneFlags.Transport) == 0)
				{
					return;
				}
				if (base.EntityManager.TryGetComponent<PathOwner>(component2.m_CurrentTransport, out var component9) && base.EntityManager.TryGetBuffer(component2.m_CurrentTransport, isReadOnly: true, out DynamicBuffer<PathElement> buffer3) && buffer3.Length > component9.m_ElementIndex)
				{
					Entity entity3 = buffer3[component9.m_ElementIndex].m_Target;
					if (base.EntityManager.TryGetComponent<Owner>(entity3, out var component10))
					{
						entity3 = component10.m_Owner;
					}
					info.Add(new InfoList.Item("Waiting for transport: " + m_NameSystem.GetDebugName(entity3), entity3));
				}
				else
				{
					info.Add(new InfoList.Item("Waiting for transport"));
				}
			}
			else
			{
				if (!(componentData.m_QueueArea.radius > 0f))
				{
					return;
				}
				if (componentData.m_QueueEntity != Entity.Null)
				{
					Entity entity4 = componentData.m_QueueEntity;
					if (base.EntityManager.HasComponent<Waypoint>(entity4) && base.EntityManager.TryGetComponent<Owner>(entity4, out var component11))
					{
						entity4 = component11.m_Owner;
					}
					info.Add(new InfoList.Item("Queueing for: " + m_NameSystem.GetDebugName(entity4), entity4));
				}
				else
				{
					info.Add(new InfoList.Item("Queueing"));
				}
			}
		}
		else if (base.EntityManager.HasComponent<HouseholdPet>(entity) && base.EntityManager.TryGetComponent<CurrentTransport>(entity, out component2))
		{
			info.Add(new InfoList.Item("Entity: " + m_NameSystem.GetDebugName(component2.m_CurrentTransport)));
		}
		else if (base.EntityManager.HasComponent<Game.Creatures.Wildlife>(entity))
		{
			info.Add(new InfoList.Item("Entity: " + m_NameSystem.GetDebugName(entity)));
		}
	}

	private bool HasGroupLeaderInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.TryGetComponent<CurrentTransport>(entity, out var component))
		{
			entity = component.m_CurrentTransport;
		}
		return base.EntityManager.HasComponent<GroupCreature>(entity);
	}

	private bool HasGroupMemberInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.TryGetComponent<CurrentTransport>(entity, out var component))
		{
			entity = component.m_CurrentTransport;
		}
		return base.EntityManager.HasComponent<GroupMember>(entity);
	}

	private void UpdateGroupLeaderInfo(Entity entity, Entity prefab, InfoList info)
	{
		if (base.EntityManager.TryGetComponent<CurrentTransport>(entity, out var component))
		{
			entity = component.m_CurrentTransport;
		}
		DynamicBuffer<GroupCreature> buffer = base.EntityManager.GetBuffer<GroupCreature>(entity, isReadOnly: true);
		info.label = $"Group members ({buffer.Length})";
		for (int i = 0; i < buffer.Length; i++)
		{
			info.Add(new InfoList.Item(m_NameSystem.GetDebugName(buffer[i].m_Creature), buffer[i].m_Creature));
		}
	}

	private void UpdateGroupMemberInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		if (base.EntityManager.TryGetComponent<CurrentTransport>(entity, out var component))
		{
			entity = component.m_CurrentTransport;
		}
		GroupMember componentData = base.EntityManager.GetComponentData<GroupMember>(entity);
		info.label = "Group leader";
		info.value = m_NameSystem.GetDebugName(componentData.m_Leader);
		info.target = componentData.m_Leader;
	}

	private bool HasVehicleModelInfo(Entity entity, Entity prefab)
	{
		return base.EntityManager.HasBuffer<VehicleModel>(entity);
	}

	private void UpdateVehicleModelInfo(Entity entity, Entity prefab, InfoList info)
	{
		DynamicBuffer<VehicleModel> buffer = base.EntityManager.GetBuffer<VehicleModel>(entity, isReadOnly: true);
		info.label = "Vehicle Model";
		for (int i = 0; i < buffer.Length; i++)
		{
			VehicleModel vehicleModel = buffer[i];
			if (base.EntityManager.HasComponent<PrefabData>(vehicleModel.m_PrimaryPrefab))
			{
				string text = m_PrefabSystem.GetPrefabName(vehicleModel.m_PrimaryPrefab);
				if (base.EntityManager.HasComponent<PrefabData>(vehicleModel.m_SecondaryPrefab))
				{
					text = text + " + " + m_PrefabSystem.GetPrefabName(vehicleModel.m_SecondaryPrefab);
				}
				info.Add(new InfoList.Item(text));
			}
		}
	}

	private bool HasAreaInfo(Entity entity, Entity prefab)
	{
		return base.EntityManager.HasComponent<Area>(entity);
	}

	private void UpdateAreaInfo(Entity entity, Entity prefab, InfoList info)
	{
		info.label = "Area Info";
		info.Add(new InfoList.Item("Nothing to see here, move along! (TBD)"));
	}

	private bool HasTreeInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.HasComponent<Game.Objects.Object>(entity))
		{
			return base.EntityManager.HasComponent<Tree>(entity);
		}
		return false;
	}

	private void UpdateTreeInfo(Entity entity, Entity prefab, CapacityInfo info)
	{
		Tree componentData = base.EntityManager.GetComponentData<Tree>(entity);
		Plant componentData2 = base.EntityManager.GetComponentData<Plant>(entity);
		base.EntityManager.TryGetComponent<Damaged>(entity, out var component);
		int num = 0;
		if (base.EntityManager.TryGetComponent<TreeData>(prefab, out var component2))
		{
			num = Mathf.RoundToInt(ObjectUtils.CalculateWoodAmount(componentData, componentData2, component, component2));
		}
		info.label = $"Wood: {num}";
		info.value = num;
		info.max = Mathf.RoundToInt(component2.m_WoodAmount);
	}

	private bool HasMailBoxInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.HasComponent<Game.Objects.Object>(entity) && base.EntityManager.HasComponent<Game.Routes.MailBox>(entity))
		{
			return base.EntityManager.HasComponent<MailBoxData>(prefab);
		}
		return false;
	}

	private void UpdateMailBoxInfo(Entity entity, Entity prefab, CapacityInfo info)
	{
		Game.Routes.MailBox componentData = base.EntityManager.GetComponentData<Game.Routes.MailBox>(entity);
		MailBoxData componentData2 = base.EntityManager.GetComponentData<MailBoxData>(prefab);
		info.label = "Stored Mail in Mailbox";
		info.value = componentData.m_MailAmount;
		info.max = componentData2.m_MailCapacity;
	}

	private bool HasBoardingVehicleInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.TryGetComponent<BoardingVehicle>(entity, out var component))
		{
			return component.m_Vehicle != InfoList.Item.kNullEntity;
		}
		return false;
	}

	private void UpdateBoardingVehicleInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		BoardingVehicle componentData = base.EntityManager.GetComponentData<BoardingVehicle>(entity);
		info.label = "Boarding";
		info.value = m_NameSystem.GetDebugName(componentData.m_Vehicle);
		info.target = componentData.m_Vehicle;
	}

	private bool HasWaitingPassengerInfo(Entity entity, Entity prefab)
	{
		if (!base.EntityManager.HasComponent<WaitingPassengers>(entity))
		{
			return base.EntityManager.HasBuffer<ConnectedRoute>(entity);
		}
		return true;
	}

	private void UpdateWaitingPassengerInfo(Entity entity, Entity prefab, InfoList info)
	{
		base.EntityManager.TryGetComponent<WaitingPassengers>(entity, out var component);
		if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<ConnectedRoute> buffer))
		{
			int num = 0;
			for (int i = 0; i < buffer.Length; i++)
			{
				if (base.EntityManager.TryGetComponent<WaitingPassengers>(buffer[i].m_Waypoint, out var component2))
				{
					component.m_Count += component2.m_Count;
					num += component2.m_AverageWaitingTime;
				}
			}
			num /= math.max(1, buffer.Length);
			num -= num % 5;
			component.m_AverageWaitingTime = (ushort)num;
		}
		info.label = "Waiting passengers";
		info.Add(new InfoList.Item("Passenger count: " + component.m_Count));
		info.Add(new InfoList.Item(string.Concat("Waiting time: ", component.m_AverageWaitingTime + "s")));
	}

	private bool HasMovingInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.TryGetComponent<CurrentTransport>(entity, out var component))
		{
			entity = component.m_CurrentTransport;
		}
		return base.EntityManager.HasComponent<Moving>(entity);
	}

	private void UpdateMovingInfo(Entity entity, Entity prefab, CapacityInfo info)
	{
		if (base.EntityManager.TryGetComponent<CurrentTransport>(entity, out var component))
		{
			entity = component.m_CurrentTransport;
			prefab = base.EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab;
		}
		int num = Mathf.RoundToInt(math.length(base.EntityManager.GetComponentData<Moving>(entity).m_Velocity) * 3.6f);
		int max = Mathf.RoundToInt(999.99994f);
		WatercraftData component3;
		AirplaneData component4;
		HelicopterData component5;
		TrainData component6;
		HumanData component7;
		AnimalData component8;
		if (base.EntityManager.TryGetComponent<CarData>(prefab, out var component2))
		{
			max = Mathf.RoundToInt(component2.m_MaxSpeed * 3.6f);
		}
		else if (base.EntityManager.TryGetComponent<WatercraftData>(prefab, out component3))
		{
			max = Mathf.RoundToInt(component3.m_MaxSpeed * 3.6f);
		}
		else if (base.EntityManager.TryGetComponent<AirplaneData>(prefab, out component4))
		{
			max = Mathf.RoundToInt(component4.m_FlyingSpeed.y * 3.6f);
		}
		else if (base.EntityManager.TryGetComponent<HelicopterData>(prefab, out component5))
		{
			max = Mathf.RoundToInt(component5.m_FlyingMaxSpeed * 3.6f);
		}
		else if (base.EntityManager.TryGetComponent<TrainData>(prefab, out component6))
		{
			max = Mathf.RoundToInt(component6.m_MaxSpeed * 3.6f);
		}
		else if (base.EntityManager.TryGetComponent<HumanData>(prefab, out component7))
		{
			max = Mathf.RoundToInt(component7.m_RunSpeed * 3.6f);
		}
		else if (base.EntityManager.TryGetComponent<AnimalData>(prefab, out component8))
		{
			AnimalCurrentLane componentData = base.EntityManager.GetComponentData<AnimalCurrentLane>(entity);
			max = (((componentData.m_Flags & CreatureLaneFlags.Flying) != 0) ? Mathf.RoundToInt(component8.m_FlySpeed * 3.6f) : (((componentData.m_Flags & CreatureLaneFlags.Swimming) == 0) ? Mathf.RoundToInt(component8.m_MoveSpeed * 3.6f) : Mathf.RoundToInt(component8.m_SwimSpeed * 3.6f)));
		}
		info.label = $"Moving: {num} km/h";
		info.value = num;
		info.max = max;
	}

	private bool HasDamagedInfo(Entity entity, Entity prefab)
	{
		return base.EntityManager.HasComponent<Damaged>(entity);
	}

	private void UpdateDamagedInfo(Entity entity, Entity prefab, InfoList info)
	{
		Damaged componentData = base.EntityManager.GetComponentData<Damaged>(entity);
		float4 @float = new float4(componentData.m_Damage, ObjectUtils.GetTotalDamage(componentData));
		@float = math.clamp(@float * 100f, 0f, 100f);
		@float = math.select(@float, 1f, (@float > 0f) & (@float < 1f));
		@float = math.select(@float, 99f, (@float > 99f) & (@float < 100f));
		info.label = "Damaged";
		info.Add(new InfoList.Item("Physical: " + Mathf.RoundToInt(@float.x) + "%"));
		info.Add(new InfoList.Item("Fire: " + Mathf.RoundToInt(@float.y) + "%"));
		info.Add(new InfoList.Item("Water: " + Mathf.RoundToInt(@float.z) + "%"));
		info.Add(new InfoList.Item("Total: " + Mathf.RoundToInt(@float.w) + "%"));
	}

	private bool HasDestroyedInfo(Entity entity, Entity prefab)
	{
		return base.EntityManager.HasComponent<Destroyed>(entity);
	}

	private void UpdateDestroyedInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		Destroyed componentData = base.EntityManager.GetComponentData<Destroyed>(entity);
		info.label = ((componentData.m_Event == InfoList.Item.kNullEntity) ? "Destroyed" : "Destroyed By");
		info.value = ((componentData.m_Event == InfoList.Item.kNullEntity) ? string.Empty : m_NameSystem.GetDebugName(componentData.m_Event));
		info.target = componentData.m_Event;
	}

	private bool HasDestroyedBuildingInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.HasComponent<Destroyed>(entity))
		{
			return base.EntityManager.HasComponent<Building>(entity);
		}
		return false;
	}

	private void UpdateDestroyedBuildingInfo(Entity entity, Entity prefab, CapacityInfo info)
	{
		Destroyed componentData = base.EntityManager.GetComponentData<Destroyed>(entity);
		info.label = $"Searching for survivors: {Mathf.RoundToInt(componentData.m_Cleared * 100f)}%)";
		info.value = Mathf.RoundToInt(componentData.m_Cleared * 100f);
		info.max = 100;
	}

	private bool HasOnFireInfo(Entity entity, Entity prefab)
	{
		return base.EntityManager.HasComponent<OnFire>(entity);
	}

	private void UpdateOnFireInfo(Entity entity, Entity prefab, InfoList info)
	{
		OnFire componentData = base.EntityManager.GetComponentData<OnFire>(entity);
		info.label = "On fire";
		if (componentData.m_Event != InfoList.Item.kNullEntity)
		{
			info.Add(new InfoList.Item("Ignited by: " + m_NameSystem.GetDebugName(componentData.m_Event), componentData.m_Event));
		}
		info.Add(new InfoList.Item("Intensity: " + Mathf.RoundToInt(componentData.m_Intensity) + "%"));
	}

	private bool HasFacingWeatherInfo(Entity entity, Entity prefab)
	{
		return base.EntityManager.HasComponent<FacingWeather>(entity);
	}

	private void UpdateFacingWeatherInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		FacingWeather componentData = base.EntityManager.GetComponentData<FacingWeather>(entity);
		info.label = ((componentData.m_Event == InfoList.Item.kNullEntity) ? "Suffering from weather" : "Weather phenomenon");
		info.value = ((componentData.m_Event == InfoList.Item.kNullEntity) ? string.Empty : m_NameSystem.GetDebugName(componentData.m_Event));
		info.target = componentData.m_Event;
	}

	private bool HasAccidentSiteInfo(Entity entity, Entity prefab)
	{
		return base.EntityManager.HasComponent<AccidentSite>(entity);
	}

	private void UpdateAccidentSiteInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		AccidentSite componentData = base.EntityManager.GetComponentData<AccidentSite>(entity);
		info.label = ((componentData.m_Event == InfoList.Item.kNullEntity) ? "Accident site" : "Incident");
		info.value = ((componentData.m_Event == InfoList.Item.kNullEntity) ? string.Empty : m_NameSystem.GetDebugName(componentData.m_Event));
		info.target = componentData.m_Event;
	}

	private bool HasInvolvedInAccidentInfo(Entity entity, Entity prefab)
	{
		return base.EntityManager.HasComponent<InvolvedInAccident>(entity);
	}

	private void UpdateInvolvedInAccidentInfo(Entity entity, Entity prefab, GenericInfo info)
	{
		InvolvedInAccident componentData = base.EntityManager.GetComponentData<InvolvedInAccident>(entity);
		info.label = ((componentData.m_Event == InfoList.Item.kNullEntity) ? "Involved in accident" : "Involved in");
		info.value = ((componentData.m_Event == InfoList.Item.kNullEntity) ? string.Empty : m_NameSystem.GetDebugName(componentData.m_Event));
		info.target = componentData.m_Event;
	}

	private bool HasFloodedInfo(Entity entity, Entity prefab)
	{
		return base.EntityManager.HasComponent<Flooded>(entity);
	}

	private void UpdateFloodedInfo(Entity entity, Entity prefab, InfoList info)
	{
		Flooded componentData = base.EntityManager.GetComponentData<Flooded>(entity);
		info.label = "Flooded";
		if (componentData.m_Event != InfoList.Item.kNullEntity)
		{
			info.Add(new InfoList.Item("Caused by: " + m_NameSystem.GetDebugName(componentData.m_Event), componentData.m_Event));
		}
		info.Add(new InfoList.Item("Depth: " + Mathf.RoundToInt(componentData.m_Depth) + "m"));
	}

	private bool HasEventInfo(Entity entity, Entity prefab)
	{
		if (base.EntityManager.HasComponent<Game.Events.Event>(entity))
		{
			return base.EntityManager.HasComponent<TargetElement>(entity);
		}
		return false;
	}

	private void UpdateEventInfo(Entity entity, Entity prefab, InfoList info)
	{
		DynamicBuffer<TargetElement> buffer = base.EntityManager.GetBuffer<TargetElement>(entity, isReadOnly: true);
		info.label = $"Affected Objects: {buffer.Length})";
		for (int i = 0; i < buffer.Length; i++)
		{
			info.Add(new InfoList.Item(m_NameSystem.GetDebugName(buffer[i].m_Entity), buffer[i].m_Entity));
		}
	}

	private bool HasNotificationInfo(Entity entity, Entity prefab)
	{
		return base.EntityManager.HasComponent<Icon>(entity);
	}

	private void UpdateNotificationInfo(Entity entity, Entity prefab, InfoList info)
	{
		NotificationIconPrefab prefab2 = m_PrefabSystem.GetPrefab<NotificationIconPrefab>(prefab);
		info.label = "Notification Info";
		info.Add(base.EntityManager.TryGetComponent<Owner>(entity, out var component) ? new InfoList.Item(prefab2.m_Description + m_NameSystem.GetDebugName(component.m_Owner), component.m_Owner) : new InfoList.Item(prefab2.m_Description));
		if (base.EntityManager.TryGetComponent<Target>(entity, out var component2))
		{
			info.Add(new InfoList.Item(prefab2.m_TargetDescription + m_NameSystem.GetDebugName(component2.m_Target), component2.m_Target));
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<ServiceFeeParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_746694607_0 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder2 = entityQueryBuilder.WithAll<PollutionParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_746694607_1 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder2 = entityQueryBuilder.WithAllRW<CityModifier>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_746694607_2 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder2 = entityQueryBuilder.WithAll<GarbageParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_746694607_3 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder2 = entityQueryBuilder.WithAll<EconomyParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_746694607_4 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder2 = entityQueryBuilder.WithAll<BuildingConfigurationData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_746694607_5 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder2 = entityQueryBuilder.WithAll<ExtractorParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_746694607_6 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder2 = entityQueryBuilder.WithAll<CitizenHappinessParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_746694607_7 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder.Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public DeveloperInfoUISystem()
	{
	}
}

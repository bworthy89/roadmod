using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Agents;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Creatures;
using Game.Economy;
using Game.Effects;
using Game.Events;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Policies;
using Game.Prefabs;
using Game.Rendering;
using Game.Routes;
using Game.Simulation;
using Game.Tools;
using Game.Triggers;
using Game.Vehicles;
using Game.Zones;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Serialization;

[CompilerGenerated]
public class RequiredComponentSystem : GameSystemBase
{
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private struct TypeHandle
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
		}
	}

	private LoadGameSystem m_LoadGameSystem;

	private EntityQuery m_BlockedLaneQuery;

	private EntityQuery m_CarLaneQuery;

	private EntityQuery m_BuildingEfficiencyQuery;

	private EntityQuery m_PolicyQuery;

	private EntityQuery m_CityModifierQuery;

	private EntityQuery m_ServiceDispatchQuery;

	private EntityQuery m_PathInformationQuery;

	private EntityQuery m_NodeGeometryQuery;

	private EntityQuery m_MeshColorQuery;

	private EntityQuery m_MeshBatchQuery;

	private EntityQuery m_RoutePolicyQuery;

	private EntityQuery m_RouteModifierQuery;

	private EntityQuery m_EdgeQuery;

	private EntityQuery m_StorageTaxQuery;

	private EntityQuery m_CityFeeQuery;

	private EntityQuery m_CityFeeQuery2;

	private EntityQuery m_ServiceFeeParameterQuery;

	private EntityQuery m_OutsideGarbageQuery;

	private EntityQuery m_OutsideFireStationQuery;

	private EntityQuery m_OutsidePoliceStationQuery;

	private EntityQuery m_OutsideEfficiencyQuery;

	private EntityQuery m_RouteInfoQuery;

	private EntityQuery m_CompanyProfitabilityQuery;

	private EntityQuery m_CompanyStatisticDataQuery;

	private EntityQuery m_StorageQuery;

	private EntityQuery m_GoodsDeliveryQuery;

	private EntityQuery m_RouteBufferIndexQuery;

	private EntityQuery m_CurveElementQuery;

	private EntityQuery m_CitizenPrefabQuery;

	private EntityQuery m_CitizenNameQuery;

	private EntityQuery m_HouseholdNameQuery;

	private EntityQuery m_LabelVertexQuery;

	private EntityQuery m_DistrictNameQuery;

	private EntityQuery m_AnimalNameQuery;

	private EntityQuery m_HouseholdPetQuery;

	private EntityQuery m_RoadNameQuery;

	private EntityQuery m_RouteNumberQuery;

	private EntityQuery m_ChirpRandomLocQuery;

	private EntityQuery m_BlockerQuery;

	private EntityQuery m_CitizenPresenceQuery;

	private EntityQuery m_SubLaneQuery;

	private EntityQuery m_SubObjectQuery;

	private EntityQuery m_NativeQuery;

	private EntityQuery m_GuestVehicleQuery;

	private EntityQuery m_TravelPurposeQuery;

	private EntityQuery m_TreeEffectQuery;

	private EntityQuery m_TakeoffLocationQuery;

	private EntityQuery m_LeisureQuery;

	private EntityQuery m_PlayerMoneyQuery;

	private EntityQuery m_PseudoRandomSeedQuery;

	private EntityQuery m_TransportDepotQuery;

	private EntityQuery m_ServiceUsageQuery;

	private EntityQuery m_OutsideSellerQuery;

	private EntityQuery m_LoadingResourcesQuery;

	private EntityQuery m_CompanyVehicleQuery;

	private EntityQuery m_LaneRestrictionQuery;

	private EntityQuery m_LaneOverlapQuery;

	private EntityQuery m_DispatchedRequestQuery;

	private EntityQuery m_HomelessShelterQuery;

	private EntityQuery m_QueueQuery;

	private EntityQuery m_BoneHistoryQuery;

	private EntityQuery m_UnspawnedQuery;

	private EntityQuery m_ConnectionLaneQuery;

	private EntityQuery m_AreaLaneQuery;

	private EntityQuery m_OfficeQuery;

	private EntityQuery m_PassengerTransportQuery;

	private EntityQuery m_ObjectColorQuery;

	private EntityQuery m_OutsideConnectionQuery;

	private EntityQuery m_NetConditionQuery;

	private EntityQuery m_NetPollutionQuery;

	private EntityQuery m_TrafficSpawnerQuery;

	private EntityQuery m_AreaExpandQuery;

	private EntityQuery m_EmissiveQuery;

	private EntityQuery m_TrainBogieFrameQuery;

	private EntityQuery m_EditorContainerQuery;

	private EntityQuery m_ProcessingTradeCostQuery;

	private EntityQuery m_StorageConditionQuery;

	private EntityQuery m_LaneColorQuery;

	private EntityQuery m_CompanyNotificationQuery;

	private EntityQuery m_PlantQuery;

	private EntityQuery m_CityPopulationQuery;

	private EntityQuery m_CityTourismQuery;

	private EntityQuery m_LaneElevationQuery;

	private EntityQuery m_BuildingNotificationQuery;

	private EntityQuery m_AreaElevationQuery;

	private EntityQuery m_BuildingLotQuery;

	private EntityQuery m_AreaTerrainQuery;

	private EntityQuery m_OwnedVehicleQuery;

	private EntityQuery m_EdgeMappingQuery;

	private EntityQuery m_SubFlowQuery;

	private EntityQuery m_PointOfInterestQuery;

	private EntityQuery m_BuildableAreaQuery;

	private EntityQuery m_SubAreaQuery;

	private EntityQuery m_CrimeVictimQuery;

	private EntityQuery m_ArrivedQuery;

	private EntityQuery m_MailSenderQuery;

	private EntityQuery m_CarKeeperQuery;

	private EntityQuery m_NeedAddHasJobSeekerQuery;

	private EntityQuery m_NeedAddPropertySeekerQuery;

	private EntityQuery m_AgeGroupQuery;

	private EntityQuery m_PrefabRefQuery;

	private EntityQuery m_LabelMaterialQuery;

	private EntityQuery m_ArrowMaterialQuery;

	private EntityQuery m_LockedQuery;

	private EntityQuery m_OutsideUpdateQuery;

	private EntityQuery m_WaitingPassengersQuery;

	private EntityQuery m_ObjectSurfaceQuery;

	private EntityQuery m_WaitingPassengersQuery2;

	private EntityQuery m_PillarQuery;

	private EntityQuery m_LegacyEfficiencyQuery;

	private EntityQuery m_SignatureQuery;

	private EntityQuery m_SubObjectOwnerQuery;

	private EntityQuery m_DangerLevelMissingQuery;

	private EntityQuery m_MeshGroupQuery;

	private EntityQuery m_RequiresAnimatedBufferQuery;

	private EntityQuery m_CreatureInvolvedInAccidentQuery;

	private EntityQuery m_UpdateFrameQuery;

	private EntityQuery m_FenceQuery;

	private EntityQuery m_NetGeometrySectionQuery;

	private EntityQuery m_NetLaneArchetypeDataQuery;

	private EntityQuery m_PathfindUpdatedQuery;

	private EntityQuery m_RouteColorQuery;

	private EntityQuery m_CitizenQuery;

	private EntityQuery m_ServiceUpkeepQuery;

	private EntityQuery m_MoveableBridgeQuery;

	private EntityQuery m_SwayingQuery;

	private EntityQuery m_OfficeCompaniesQuery;

	private EntityQuery m_PlaybackLayerQuery;

	private EntityQuery m_OldSignatureBuildingQuery;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_1938549536_0;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_LoadGameSystem = base.World.GetOrCreateSystemManaged<LoadGameSystem>();
		m_BlockedLaneQuery = GetEntityQuery(ComponentType.ReadOnly<Car>(), ComponentType.Exclude<BlockedLane>());
		m_CarLaneQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Net.CarLane>(), ComponentType.Exclude<MasterLane>(), ComponentType.Exclude<LaneFlow>());
		m_BuildingEfficiencyQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.TransportDepot>(), ComponentType.ReadOnly<Building>(), ComponentType.Exclude<Efficiency>());
		m_PolicyQuery = GetEntityQuery(ComponentType.ReadOnly<Game.City.City>(), ComponentType.Exclude<Policy>());
		m_CityModifierQuery = GetEntityQuery(ComponentType.ReadOnly<Game.City.City>(), ComponentType.Exclude<CityModifier>());
		m_ServiceDispatchQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[4]
			{
				ComponentType.ReadOnly<Game.Vehicles.PublicTransport>(),
				ComponentType.ReadOnly<Game.Vehicles.CargoTransport>(),
				ComponentType.ReadOnly<Game.Vehicles.DeliveryTruck>(),
				ComponentType.ReadOnly<Game.Companies.StorageCompany>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<ServiceDispatch>() }
		});
		m_PathInformationQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Game.Vehicles.PublicTransport>(),
				ComponentType.ReadOnly<Game.Vehicles.CargoTransport>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<PathInformation>() }
		});
		m_NodeGeometryQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Net.Node>(), ComponentType.ReadOnly<Game.Net.SubLane>(), ComponentType.Exclude<NodeGeometry>());
		m_MeshColorQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Tree>(),
				ComponentType.ReadOnly<Plant>(),
				ComponentType.ReadOnly<Human>()
			},
			None = new ComponentType[1] { ComponentType.Exclude<MeshColor>() }
		});
		m_MeshBatchQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[6]
			{
				ComponentType.ReadOnly<NodeGeometry>(),
				ComponentType.ReadOnly<EdgeGeometry>(),
				ComponentType.ReadOnly<LaneGeometry>(),
				ComponentType.ReadOnly<ObjectGeometry>(),
				ComponentType.ReadOnly<Game.Objects.Marker>(),
				ComponentType.ReadOnly<Block>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<MeshBatch>() }
		});
		m_RoutePolicyQuery = GetEntityQuery(ComponentType.ReadOnly<Route>(), ComponentType.Exclude<Policy>(), ComponentType.Exclude<RouteModifier>());
		m_RouteModifierQuery = GetEntityQuery(ComponentType.ReadOnly<Route>(), ComponentType.Exclude<RouteModifier>());
		m_EdgeQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Net.Edge>(), ComponentType.ReadOnly<ConnectedBuilding>(), ComponentType.Exclude<Density>());
		m_StorageTaxQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Companies.StorageCompany>(), ComponentType.ReadOnly<TaxPayer>());
		m_CityFeeQuery = GetEntityQuery(ComponentType.ReadOnly<Game.City.City>(), ComponentType.Exclude<ServiceFee>());
		m_CityFeeQuery2 = GetEntityQuery(ComponentType.ReadOnly<Game.City.City>(), ComponentType.ReadWrite<ServiceFee>());
		m_ServiceFeeParameterQuery = GetEntityQuery(ComponentType.ReadOnly<ServiceFeeParameterData>());
		m_OutsideGarbageQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Objects.OutsideConnection>(), ComponentType.Exclude<Game.Buildings.GarbageFacility>());
		m_OutsideFireStationQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Objects.OutsideConnection>(), ComponentType.Exclude<Game.Buildings.FireStation>());
		m_OutsidePoliceStationQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Objects.OutsideConnection>(), ComponentType.Exclude<Game.Buildings.PoliceStation>());
		m_GoodsDeliveryQuery = GetEntityQuery(ComponentType.ReadOnly<TransportCompany>(), ComponentType.Exclude<GoodsDeliveryFacility>());
		m_OutsideEfficiencyQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Objects.OutsideConnection>(), ComponentType.Exclude<Efficiency>());
		m_RouteInfoQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Routes.Segment>(), ComponentType.ReadOnly<PathTargets>(), ComponentType.Exclude<RouteInfo>());
		m_CompanyProfitabilityQuery = GetEntityQuery(ComponentType.ReadOnly<CompanyData>(), ComponentType.Exclude<Profitability>(), ComponentType.Exclude<Game.Companies.StorageCompany>());
		m_CompanyStatisticDataQuery = GetEntityQuery(ComponentType.ReadOnly<CompanyData>(), ComponentType.Exclude<CompanyStatisticData>());
		m_StorageQuery = GetEntityQuery(ComponentType.ReadOnly<IndustrialProperty>(), ComponentType.Exclude<StorageProperty>());
		m_RouteBufferIndexQuery = GetEntityQuery(ComponentType.ReadOnly<Route>(), ComponentType.Exclude<RouteBufferIndex>());
		m_CurveElementQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Routes.Segment>(), ComponentType.Exclude<CurveElement>());
		m_CitizenPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<Citizen>(), ComponentType.Exclude<PrefabRef>());
		m_CitizenNameQuery = GetEntityQuery(ComponentType.ReadOnly<Citizen>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<RandomLocalizationIndex>());
		m_HouseholdNameQuery = GetEntityQuery(ComponentType.ReadOnly<Household>(), ComponentType.Exclude<RandomLocalizationIndex>());
		m_DistrictNameQuery = GetEntityQuery(ComponentType.ReadOnly<District>(), ComponentType.ReadOnly<Area>(), ComponentType.Exclude<RandomLocalizationIndex>());
		m_AnimalNameQuery = GetEntityQuery(ComponentType.ReadOnly<Animal>(), ComponentType.Exclude<RandomLocalizationIndex>());
		m_HouseholdPetQuery = GetEntityQuery(ComponentType.ReadOnly<HouseholdPet>(), ComponentType.Exclude<RandomLocalizationIndex>());
		m_RoadNameQuery = GetEntityQuery(ComponentType.ReadOnly<Aggregate>(), ComponentType.ReadOnly<LabelMaterial>(), ComponentType.Exclude<RandomLocalizationIndex>());
		m_LabelVertexQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Areas.LabelExtents>(), ComponentType.Exclude<Game.Areas.LabelVertex>());
		m_RouteNumberQuery = GetEntityQuery(ComponentType.ReadOnly<TransportLine>(), ComponentType.Exclude<RouteNumber>());
		m_ChirpRandomLocQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<LifePathEntry>(),
				ComponentType.ReadOnly<ChirpEntity>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<RandomLocalizationIndex>() }
		});
		m_BlockerQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<HumanCurrentLane>(),
				ComponentType.ReadOnly<AnimalCurrentLane>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Blocker>() }
		});
		m_CitizenPresenceQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.Exclude<CitizenPresence>());
		m_SubLaneQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.Exclude<Game.Net.SubLane>());
		m_SubObjectQuery = GetEntityQuery(ComponentType.ReadOnly<Human>(), ComponentType.Exclude<Game.Objects.SubObject>());
		m_NativeQuery = GetEntityQuery(ComponentType.ReadOnly<MapTile>(), ComponentType.Exclude<Native>(), ComponentType.Exclude<Owner>());
		m_GuestVehicleQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Game.Buildings.PostFacility>(),
				ComponentType.ReadOnly<Game.Buildings.GarbageFacility>(),
				ComponentType.ReadOnly<ResourceNeeding>()
			},
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Game.Buildings.ServiceUpgrade>(),
				ComponentType.ReadOnly<Game.Objects.OutsideConnection>(),
				ComponentType.ReadOnly<GuestVehicle>()
			}
		});
		m_TravelPurposeQuery = GetEntityQuery(ComponentType.ReadOnly<TravelPurpose>());
		m_TreeEffectQuery = GetEntityQuery(ComponentType.ReadOnly<Tree>(), ComponentType.Exclude<EnabledEffect>());
		m_TakeoffLocationQuery = GetEntityQuery(ComponentType.ReadOnly<AirplaneStop>(), ComponentType.ReadOnly<Game.Net.SubLane>(), ComponentType.Exclude<Game.Routes.TakeoffLocation>());
		m_LeisureQuery = GetEntityQuery(ComponentType.ReadOnly<CompanyData>(), ComponentType.Exclude<Game.Buildings.LeisureProvider>(), ComponentType.ReadOnly<PrefabRef>());
		m_PlayerMoneyQuery = GetEntityQuery(ComponentType.ReadOnly<Game.City.City>(), ComponentType.ReadWrite<Game.Economy.Resources>(), ComponentType.Exclude<PlayerMoney>());
		m_PseudoRandomSeedQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[6]
			{
				ComponentType.ReadOnly<NodeGeometry>(),
				ComponentType.ReadOnly<EdgeGeometry>(),
				ComponentType.ReadOnly<ObjectGeometry>(),
				ComponentType.ReadOnly<AssetStamp>(),
				ComponentType.ReadOnly<Game.Objects.Marker>(),
				ComponentType.ReadOnly<Game.Areas.Lot>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<PseudoRandomSeed>() }
		});
		m_TransportDepotQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Objects.OutsideConnection>(), ComponentType.ReadOnly<Game.Buildings.GarbageFacility>(), ComponentType.Exclude<Game.Buildings.TransportDepot>());
		m_ServiceUsageQuery = GetEntityQuery(ComponentType.ReadOnly<CityServiceUpkeep>(), ComponentType.Exclude<ServiceUsage>());
		m_OutsideSellerQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Objects.OutsideConnection>(), ComponentType.Exclude<ResourceSeller>());
		m_LoadingResourcesQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Vehicles.CargoTransport>(), ComponentType.Exclude<LoadingResources>());
		m_CompanyVehicleQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Companies.ProcessingCompany>(), ComponentType.Exclude<OwnedVehicle>());
		m_LaneRestrictionQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Owner>() },
			Any = new ComponentType[7]
			{
				ComponentType.ReadOnly<Game.Net.CarLane>(),
				ComponentType.ReadOnly<Game.Net.ParkingLane>(),
				ComponentType.ReadOnly<Game.Net.PedestrianLane>(),
				ComponentType.ReadOnly<Game.Net.ConnectionLane>(),
				ComponentType.ReadOnly<Game.Routes.TransportStop>(),
				ComponentType.ReadOnly<Game.Routes.TakeoffLocation>(),
				ComponentType.ReadOnly<Game.Objects.SpawnLocation>()
			}
		});
		m_LaneOverlapQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Net.ParkingLane>(), ComponentType.Exclude<LaneOverlap>());
		m_DispatchedRequestQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<TransportLine>(),
				ComponentType.ReadOnly<TaxiStand>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<DispatchedRequest>() }
		});
		m_HomelessShelterQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Game.Buildings.Park>(),
				ComponentType.ReadOnly<Abandoned>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Renter>() }
		});
		m_QueueQuery = GetEntityQuery(ComponentType.ReadOnly<Human>(), ComponentType.Exclude<Queue>());
		m_BoneHistoryQuery = GetEntityQuery(ComponentType.ReadOnly<Bone>(), ComponentType.Exclude<BoneHistory>());
		m_UnspawnedQuery = GetEntityQuery(ComponentType.ReadOnly<CurrentVehicle>(), ComponentType.Exclude<Unspawned>());
		m_ConnectionLaneQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Net.ConnectionLane>(), ComponentType.ReadOnly<NodeLane>());
		m_AreaLaneQuery = GetEntityQuery(ComponentType.ReadOnly<Area>(), ComponentType.ReadOnly<Game.Net.SubLane>());
		m_OfficeQuery = GetEntityQuery(ComponentType.ReadOnly<IndustrialProperty>(), ComponentType.Exclude<OfficeProperty>());
		m_PassengerTransportQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Vehicles.PublicTransport>(), ComponentType.Exclude<PassengerTransport>(), ComponentType.Exclude<EvacuatingTransport>(), ComponentType.Exclude<PrisonerTransport>());
		m_ObjectColorQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[5]
			{
				ComponentType.ReadOnly<Tree>(),
				ComponentType.ReadOnly<Vehicle>(),
				ComponentType.ReadOnly<Creature>(),
				ComponentType.ReadOnly<Extension>(),
				ComponentType.ReadOnly<Game.Objects.UtilityObject>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Game.Objects.Color>() }
		});
		m_OutsideConnectionQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Game.Objects.ElectricityOutsideConnection>(),
				ComponentType.ReadOnly<Game.Objects.WaterPipeOutsideConnection>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Game.Objects.OutsideConnection>() }
		});
		m_NetConditionQuery = GetEntityQuery(ComponentType.ReadOnly<Road>(), ComponentType.Exclude<NetCondition>());
		m_NetPollutionQuery = GetEntityQuery(ComponentType.ReadOnly<Road>(), ComponentType.Exclude<Game.Net.Pollution>());
		m_TrafficSpawnerQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Objects.OutsideConnection>(), ComponentType.ReadOnly<OwnedVehicle>(), ComponentType.Exclude<Game.Buildings.TrafficSpawner>());
		m_AreaExpandQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Areas.Surface>(), ComponentType.Exclude<Expand>());
		m_EmissiveQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<TrafficLight>(),
				ComponentType.ReadOnly<Car>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Emissive>() }
		});
		m_TrainBogieFrameQuery = GetEntityQuery(ComponentType.ReadOnly<TrainCurrentLane>(), ComponentType.Exclude<TrainBogieFrame>());
		m_ProcessingTradeCostQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Companies.ProcessingCompany>(), ComponentType.Exclude<TradeCost>());
		m_EditorContainerQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Game.Net.Node>(),
				ComponentType.ReadOnly<Game.Net.Edge>(),
				ComponentType.ReadOnly<Game.Objects.Object>()
			},
			None = new ComponentType[6]
			{
				ComponentType.ReadOnly<NodeGeometry>(),
				ComponentType.ReadOnly<EdgeGeometry>(),
				ComponentType.ReadOnly<ObjectGeometry>(),
				ComponentType.ReadOnly<AssetStamp>(),
				ComponentType.ReadOnly<Game.Objects.Marker>(),
				ComponentType.ReadOnly<Game.Tools.EditorContainer>()
			}
		});
		m_StorageConditionQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Companies.StorageCompany>(), ComponentType.ReadOnly<PropertyRenter>());
		m_LaneColorQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Game.Net.TrackLane>(),
				ComponentType.ReadOnly<Game.Net.UtilityLane>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<LaneColor>() }
		});
		m_CompanyNotificationQuery = GetEntityQuery(ComponentType.ReadOnly<CompanyData>(), ComponentType.Exclude<CompanyNotifications>());
		m_PlantQuery = GetEntityQuery(ComponentType.ReadOnly<Tree>(), ComponentType.Exclude<Plant>());
		m_CityPopulationQuery = GetEntityQuery(ComponentType.ReadOnly<Game.City.City>(), ComponentType.Exclude<Population>());
		m_CityTourismQuery = GetEntityQuery(ComponentType.ReadOnly<Game.City.City>(), ComponentType.Exclude<Tourism>());
		m_BuildingNotificationQuery = GetEntityQuery(ComponentType.ReadOnly<ResidentialProperty>(), ComponentType.Exclude<BuildingNotifications>());
		m_LaneElevationQuery = GetEntityQuery(ComponentType.ReadOnly<Lane>(), ComponentType.ReadOnly<Owner>(), ComponentType.Exclude<EdgeLane>(), ComponentType.Exclude<AreaLane>(), ComponentType.Exclude<Game.Net.ConnectionLane>(), ComponentType.Exclude<Game.Net.Elevation>());
		m_AreaElevationQuery = GetEntityQuery(ComponentType.ReadOnly<Area>(), ComponentType.ReadOnly<Owner>());
		m_BuildingLotQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.Exclude<Game.Buildings.Lot>());
		m_AreaTerrainQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Areas.Lot>(), ComponentType.ReadOnly<Storage>(), ComponentType.Exclude<Game.Areas.Terrain>());
		m_OwnedVehicleQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Areas.Lot>(), ComponentType.ReadOnly<Storage>(), ComponentType.Exclude<OwnedVehicle>());
		m_EdgeMappingQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Net.UtilityLane>(), ComponentType.Exclude<EdgeMapping>());
		m_SubFlowQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Net.UtilityLane>(), ComponentType.Exclude<SubFlow>());
		m_PointOfInterestQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[5]
			{
				ComponentType.ReadOnly<Game.Vehicles.PoliceCar>(),
				ComponentType.ReadOnly<RenewableElectricityProduction>(),
				ComponentType.ReadOnly<Game.Buildings.ExtractorFacility>(),
				ComponentType.ReadOnly<Game.Buildings.TelecomFacility>(),
				ComponentType.ReadOnly<Game.Buildings.ResearchFacility>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<PointOfInterest>() }
		});
		m_BuildableAreaQuery = GetEntityQuery(ComponentType.ReadOnly<MapFeatureElement>(), ComponentType.Exclude<Updated>());
		m_SubAreaQuery = GetEntityQuery(ComponentType.ReadOnly<Extractor>(), ComponentType.Exclude<Game.Areas.SubArea>());
		m_CrimeVictimQuery = GetEntityQuery(ComponentType.ReadOnly<Citizen>(), ComponentType.Exclude<CrimeVictim>());
		m_ArrivedQuery = GetEntityQuery(ComponentType.ReadOnly<Citizen>(), ComponentType.Exclude<Arrived>());
		m_MailSenderQuery = GetEntityQuery(ComponentType.ReadOnly<Citizen>(), ComponentType.Exclude<MailSender>());
		m_CarKeeperQuery = GetEntityQuery(ComponentType.ReadOnly<Citizen>(), ComponentType.Exclude<CarKeeper>());
		m_NeedAddHasJobSeekerQuery = GetEntityQuery(ComponentType.ReadOnly<Citizen>(), ComponentType.Exclude<HasJobSeeker>());
		m_NeedAddPropertySeekerQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Household>(),
				ComponentType.ReadOnly<CompanyData>()
			},
			None = new ComponentType[1] { ComponentType.Exclude<PropertySeeker>() }
		});
		m_AgeGroupQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadWrite<Citizen>() },
			Any = new ComponentType[4]
			{
				ComponentType.ReadOnly<Child>(),
				ComponentType.ReadOnly<Teen>(),
				ComponentType.ReadOnly<Adult>(),
				ComponentType.ReadOnly<Elderly>()
			}
		});
		m_PrefabRefQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabRef>());
		m_LabelMaterialQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Net.LabelExtents>(), ComponentType.Exclude<LabelMaterial>());
		m_ArrowMaterialQuery = GetEntityQuery(ComponentType.ReadOnly<ArrowPosition>(), ComponentType.Exclude<ArrowMaterial>());
		m_LockedQuery = GetEntityQuery(ComponentType.ReadOnly<UnlockRequirement>(), ComponentType.Exclude<Locked>());
		m_OutsideUpdateQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Objects.OutsideConnection>());
		m_WaitingPassengersQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<AccessLane>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<TaxiStand>(),
				ComponentType.ReadOnly<Waypoint>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<WaitingPassengers>() }
		});
		m_WaitingPassengersQuery2 = GetEntityQuery(ComponentType.ReadOnly<WaitingPassengers>(), ComponentType.Exclude<TaxiStand>(), ComponentType.Exclude<Waypoint>());
		m_PillarQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<ObjectGeometry>(),
				ComponentType.ReadOnly<PrefabRef>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Game.Objects.UtilityObject>(),
				ComponentType.ReadOnly<Game.Objects.NetObject>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Pillar>() }
		});
		m_LegacyEfficiencyQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.BuildingEfficiency>());
		m_SignatureQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<Game.Objects.UniqueObject>(), ComponentType.ReadOnly<Renter>(), ComponentType.Exclude<Game.Buildings.Park>(), ComponentType.Exclude<Signature>());
		m_SubObjectOwnerQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Game.Objects.Object>(),
				ComponentType.ReadOnly<Owner>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Game.Creatures.CreatureSpawner>(),
				ComponentType.ReadOnly<Game.Tools.EditorContainer>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Vehicle>(),
				ComponentType.ReadOnly<Creature>()
			}
		});
		m_DangerLevelMissingQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Game.Events.Event>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Game.Events.WeatherPhenomenon>(),
				ComponentType.ReadOnly<WaterLevelChange>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Game.Events.DangerLevel>() }
		});
		m_MeshGroupQuery = GetEntityQuery(ComponentType.ReadOnly<Creature>(), ComponentType.ReadOnly<MeshBatch>(), ComponentType.Exclude<MeshGroup>());
		m_RequiresAnimatedBufferQuery = GetEntityQuery(ComponentType.ReadOnly<Creature>(), ComponentType.ReadOnly<MeshBatch>(), ComponentType.Exclude<Animated>());
		m_CreatureInvolvedInAccidentQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[4]
			{
				ComponentType.ReadOnly<Human>(),
				ComponentType.ReadOnly<Stumbling>(),
				ComponentType.ReadOnly<Stopped>(),
				ComponentType.ReadOnly<InvolvedInAccident>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<TransformFrame>(),
				ComponentType.ReadOnly<InterpolatedTransform>()
			}
		});
		m_ObjectSurfaceQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<ObjectGeometry>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<Creature>(),
				ComponentType.ReadOnly<Vehicle>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Game.Objects.Surface>() }
		}, new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<ObjectGeometry>() },
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Game.Objects.Surface>(),
				ComponentType.ReadOnly<Owner>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Road>(),
				ComponentType.ReadOnly<Game.Net.Node>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Game.Objects.Surface>() }
		});
		m_UpdateFrameQuery = GetEntityQuery(ComponentType.ReadOnly<Plant>(), ComponentType.Exclude<UpdateFrame>());
		m_FenceQuery = GetEntityQuery(ComponentType.ReadOnly<LaneGeometry>(), ComponentType.ReadOnly<Game.Net.UtilityLane>(), ComponentType.Exclude<PseudoRandomSeed>(), ComponentType.Exclude<MeshColor>(), ComponentType.Exclude<UpdateFrame>());
		m_NetGeometrySectionQuery = GetEntityQuery(ComponentType.ReadOnly<NetGeometryData>(), ComponentType.Exclude<NetGeometrySection>());
		m_NetLaneArchetypeDataQuery = GetEntityQuery(ComponentType.ReadOnly<NetLaneData>(), ComponentType.Exclude<NetLaneArchetypeData>());
		m_PathfindUpdatedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Owner>() },
			Any = new ComponentType[8]
			{
				ComponentType.ReadOnly<Game.Net.CarLane>(),
				ComponentType.ReadOnly<Game.Net.PedestrianLane>(),
				ComponentType.ReadOnly<Game.Net.TrackLane>(),
				ComponentType.ReadOnly<Game.Net.ParkingLane>(),
				ComponentType.ReadOnly<Game.Net.ConnectionLane>(),
				ComponentType.ReadOnly<Game.Routes.TransportStop>(),
				ComponentType.ReadOnly<Game.Routes.TakeoffLocation>(),
				ComponentType.ReadOnly<Game.Objects.SpawnLocation>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Game.Routes.MailBox>() }
		});
		m_RouteColorQuery = GetEntityQuery(ComponentType.ReadOnly<CurrentRoute>(), ComponentType.Exclude<Game.Routes.Color>());
		m_CitizenQuery = GetEntityQuery(ComponentType.ReadOnly<Citizen>());
		m_ServiceUpkeepQuery = GetEntityQuery(ComponentType.ReadOnly<CityServiceUpkeep>(), ComponentType.Exclude<OwnedVehicle>());
		m_MoveableBridgeQuery = GetEntityQuery(ComponentType.ReadOnly<Stack>(), ComponentType.ReadOnly<Pillar>(), ComponentType.ReadOnly<PointOfInterest>());
		m_SwayingQuery = GetEntityQuery(ComponentType.ReadOnly<Watercraft>(), ComponentType.ReadOnly<Moving>(), ComponentType.Exclude<Swaying>());
		m_OfficeCompaniesQuery = GetEntityQuery(ComponentType.ReadWrite<Game.Companies.ProcessingCompany>(), ComponentType.ReadOnly<PropertyRenter>(), ComponentType.ReadWrite<Game.Economy.Resources>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<OfficeCompany>(), ComponentType.Exclude<Game.Companies.ExtractorCompany>());
		m_PlaybackLayerQuery = GetEntityQuery(ComponentType.ReadOnly<Skeleton>(), ComponentType.Exclude<PlaybackLayer>());
		m_OldSignatureBuildingQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<Signature>(), ComponentType.Exclude<Abandoned>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<PropertyToBeOnMarket>(), ComponentType.Exclude<PropertyOnMarket>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_BlockedLaneQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<BlockedLane>(m_BlockedLaneQuery);
		}
		if (!m_CarLaneQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<LaneFlow>(m_CarLaneQuery);
		}
		if (!m_BuildingEfficiencyQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Efficiency>(m_BuildingEfficiencyQuery);
		}
		if (!m_PolicyQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Policy>(m_PolicyQuery);
		}
		if (!m_CityModifierQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<CityModifier>(m_CityModifierQuery);
		}
		if (!m_ServiceDispatchQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<ServiceDispatch>(m_ServiceDispatchQuery);
		}
		if (!m_PathInformationQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<PathInformation>(m_PathInformationQuery);
		}
		if (!m_NodeGeometryQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<NodeGeometry>(m_NodeGeometryQuery);
		}
		if (!m_MeshBatchQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<MeshBatch>(m_MeshBatchQuery);
		}
		if (!m_RoutePolicyQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Policy>(m_RoutePolicyQuery);
		}
		if (!m_RouteModifierQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<RouteModifier>(m_RouteModifierQuery);
		}
		if (!m_EdgeQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Density>(m_EdgeQuery);
		}
		if (!m_StorageTaxQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.RemoveComponent<TaxPayer>(m_StorageTaxQuery);
		}
		if (!m_CityFeeQuery.IsEmptyIgnoreFilter)
		{
			ServiceFeeParameterData singleton = m_ServiceFeeParameterQuery.GetSingleton<ServiceFeeParameterData>();
			NativeArray<Entity> nativeArray = m_CityFeeQuery.ToEntityArray(Allocator.TempJob);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				DynamicBuffer<ServiceFee> dynamicBuffer = base.EntityManager.AddBuffer<ServiceFee>(nativeArray[i]);
				foreach (ServiceFee defaultFee in singleton.GetDefaultFees())
				{
					dynamicBuffer.Add(defaultFee);
				}
			}
			nativeArray.Dispose();
		}
		if (!m_CityFeeQuery2.IsEmptyIgnoreFilter)
		{
			Entity singletonEntity = m_CityFeeQuery2.GetSingletonEntity();
			DynamicBuffer<ServiceFee> buffer = base.EntityManager.GetBuffer<ServiceFee>(singletonEntity);
			bool flag = false;
			for (int j = 0; j < buffer.Length; j++)
			{
				if (buffer[j].m_Resource == PlayerResource.Water)
				{
					flag = true;
				}
			}
			if (!flag)
			{
				ServiceFee elem = default(ServiceFee);
				elem.m_Resource = PlayerResource.Water;
				elem.m_Fee = elem.GetDefaultFee(elem.m_Resource);
				buffer.Add(elem);
			}
		}
		if (!m_OutsideGarbageQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray2 = m_OutsideGarbageQuery.ToEntityArray(Allocator.TempJob);
			for (int k = 0; k < nativeArray2.Length; k++)
			{
				Entity prefab = base.EntityManager.GetComponentData<PrefabRef>(nativeArray2[k]).m_Prefab;
				if (base.EntityManager.HasComponent<GarbageFacilityData>(prefab))
				{
					GarbageFacilityData componentData = base.EntityManager.GetComponentData<GarbageFacilityData>(prefab);
					if (base.EntityManager.TryGetBuffer(nativeArray2[k], isReadOnly: false, out DynamicBuffer<Game.Economy.Resources> buffer2))
					{
						EconomyUtils.SetResources(Resource.Garbage, buffer2, componentData.m_GarbageCapacity / 2);
					}
					if (!base.EntityManager.HasComponent<ServiceDispatch>(nativeArray2[k]))
					{
						base.EntityManager.AddBuffer<ServiceDispatch>(nativeArray2[k]);
					}
					if (!base.EntityManager.HasComponent<OwnedVehicle>(nativeArray2[k]))
					{
						base.EntityManager.AddBuffer<OwnedVehicle>(nativeArray2[k]);
					}
				}
			}
			nativeArray2.Dispose();
		}
		if (!m_OutsideFireStationQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray3 = m_OutsideFireStationQuery.ToEntityArray(Allocator.TempJob);
			for (int l = 0; l < nativeArray3.Length; l++)
			{
				Entity prefab2 = base.EntityManager.GetComponentData<PrefabRef>(nativeArray3[l]).m_Prefab;
				if (base.EntityManager.HasComponent<FireStationData>(prefab2))
				{
					base.EntityManager.GetComponentData<FireStationData>(prefab2);
					base.EntityManager.AddComponentData(nativeArray3[l], default(Game.Buildings.FireStation));
					if (!base.EntityManager.HasComponent<ServiceDispatch>(nativeArray3[l]))
					{
						base.EntityManager.AddBuffer<ServiceDispatch>(nativeArray3[l]);
					}
					if (!base.EntityManager.HasComponent<OwnedVehicle>(nativeArray3[l]))
					{
						base.EntityManager.AddBuffer<OwnedVehicle>(nativeArray3[l]);
					}
				}
			}
			nativeArray3.Dispose();
		}
		if (!m_OutsidePoliceStationQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray4 = m_OutsidePoliceStationQuery.ToEntityArray(Allocator.TempJob);
			for (int m = 0; m < nativeArray4.Length; m++)
			{
				Entity prefab3 = base.EntityManager.GetComponentData<PrefabRef>(nativeArray4[m]).m_Prefab;
				if (base.EntityManager.HasComponent<PoliceStationData>(prefab3))
				{
					PoliceStationData componentData2 = base.EntityManager.GetComponentData<PoliceStationData>(prefab3);
					base.EntityManager.AddComponentData(nativeArray4[m], new Game.Buildings.PoliceStation
					{
						m_PurposeMask = componentData2.m_PurposeMask
					});
					if (!base.EntityManager.HasComponent<ServiceDispatch>(nativeArray4[m]))
					{
						base.EntityManager.AddBuffer<ServiceDispatch>(nativeArray4[m]);
					}
					if (!base.EntityManager.HasComponent<OwnedVehicle>(nativeArray4[m]))
					{
						base.EntityManager.AddBuffer<OwnedVehicle>(nativeArray4[m]);
					}
				}
			}
			nativeArray4.Dispose();
		}
		if (!m_GoodsDeliveryQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray5 = m_GoodsDeliveryQuery.ToEntityArray(Allocator.TempJob);
			for (int n = 0; n < nativeArray5.Length; n++)
			{
				if (!base.EntityManager.HasComponent<CommercialCompany>(nativeArray5[n]))
				{
					if (!base.EntityManager.HasComponent<ServiceDispatch>(nativeArray5[n]))
					{
						base.EntityManager.AddBuffer<ServiceDispatch>(nativeArray5[n]);
					}
					if (!base.EntityManager.HasComponent<OwnedVehicle>(nativeArray5[n]))
					{
						base.EntityManager.AddBuffer<OwnedVehicle>(nativeArray5[n]);
					}
					if (!base.EntityManager.HasComponent<GoodsDeliveryFacility>(nativeArray5[n]))
					{
						base.EntityManager.AddComponent<GoodsDeliveryFacility>(nativeArray5[n]);
					}
				}
			}
			nativeArray5.Dispose();
		}
		if (!m_OutsideEfficiencyQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Efficiency>(m_OutsideEfficiencyQuery);
		}
		if (!m_RouteInfoQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<RouteInfo>(m_RouteInfoQuery);
		}
		if (!m_CompanyProfitabilityQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray6 = m_CompanyProfitabilityQuery.ToEntityArray(Allocator.TempJob);
			for (int num = 0; num < nativeArray6.Length; num++)
			{
				base.EntityManager.AddComponentData(nativeArray6[num], new Profitability
				{
					m_Profitability = 127
				});
			}
			nativeArray6.Dispose();
		}
		if (!m_CompanyStatisticDataQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray7 = m_CompanyStatisticDataQuery.ToEntityArray(Allocator.TempJob);
			for (int num2 = 0; num2 < nativeArray7.Length; num2++)
			{
				base.EntityManager.AddComponentData(nativeArray7[num2], default(CompanyStatisticData));
			}
			nativeArray7.Dispose();
		}
		if (!m_StorageQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray8 = m_StorageQuery.ToEntityArray(Allocator.TempJob);
			for (int num3 = 0; num3 < nativeArray8.Length; num3++)
			{
				if (base.EntityManager.TryGetComponent<PrefabRef>(nativeArray8[num3], out var component) && base.EntityManager.TryGetComponent<BuildingPropertyData>(component.m_Prefab, out var component2) && component2.m_AllowedStored != Resource.NoResource)
				{
					base.EntityManager.AddComponent<StorageProperty>(nativeArray8[num3]);
				}
			}
			nativeArray8.Dispose();
		}
		if (!m_RouteBufferIndexQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<RouteBufferIndex>(m_RouteBufferIndexQuery);
		}
		if (!m_CurveElementQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<CurveElement>(m_CurveElementQuery);
		}
		if (!m_CitizenPrefabQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<PrefabRef>(m_CitizenPrefabQuery);
			NativeArray<Entity> nativeArray9 = m_CitizenPrefabQuery.ToEntityArray(Allocator.TempJob);
			for (int num4 = 0; num4 < nativeArray9.Length; num4++)
			{
				base.EntityManager.AddBuffer<RandomLocalizationIndex>(nativeArray9[num4]).Add(RandomLocalizationIndex.kNone);
			}
			nativeArray9.Dispose();
		}
		if (!m_CitizenNameQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray10 = m_CitizenNameQuery.ToEntityArray(Allocator.TempJob);
			for (int num5 = 0; num5 < nativeArray10.Length; num5++)
			{
				base.EntityManager.AddBuffer<RandomLocalizationIndex>(nativeArray10[num5]).Add(RandomLocalizationIndex.kNone);
			}
			nativeArray10.Dispose();
		}
		if (!m_HouseholdNameQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray11 = m_HouseholdNameQuery.ToEntityArray(Allocator.TempJob);
			for (int num6 = 0; num6 < nativeArray11.Length; num6++)
			{
				base.EntityManager.AddBuffer<RandomLocalizationIndex>(nativeArray11[num6]).Add(RandomLocalizationIndex.kNone);
			}
			nativeArray11.Dispose();
		}
		if (!m_DistrictNameQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray12 = m_DistrictNameQuery.ToEntityArray(Allocator.TempJob);
			for (int num7 = 0; num7 < nativeArray12.Length; num7++)
			{
				base.EntityManager.AddBuffer<RandomLocalizationIndex>(nativeArray12[num7]).Add(RandomLocalizationIndex.kNone);
			}
			nativeArray12.Dispose();
		}
		if (!m_AnimalNameQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray13 = m_AnimalNameQuery.ToEntityArray(Allocator.TempJob);
			for (int num8 = 0; num8 < nativeArray13.Length; num8++)
			{
				base.EntityManager.AddBuffer<RandomLocalizationIndex>(nativeArray13[num8]).Add(RandomLocalizationIndex.kNone);
			}
			nativeArray13.Dispose();
		}
		if (!m_HouseholdPetQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray14 = m_HouseholdPetQuery.ToEntityArray(Allocator.TempJob);
			for (int num9 = 0; num9 < nativeArray14.Length; num9++)
			{
				base.EntityManager.AddBuffer<RandomLocalizationIndex>(nativeArray14[num9]).Add(RandomLocalizationIndex.kNone);
			}
			nativeArray14.Dispose();
		}
		if (!m_RoadNameQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray15 = m_RoadNameQuery.ToEntityArray(Allocator.TempJob);
			for (int num10 = 0; num10 < nativeArray15.Length; num10++)
			{
				base.EntityManager.AddBuffer<RandomLocalizationIndex>(nativeArray15[num10]).Add(RandomLocalizationIndex.kNone);
			}
			nativeArray15.Dispose();
		}
		if (!m_ChirpRandomLocQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray16 = m_ChirpRandomLocQuery.ToEntityArray(Allocator.TempJob);
			for (int num11 = 0; num11 < nativeArray16.Length; num11++)
			{
				base.EntityManager.AddBuffer<RandomLocalizationIndex>(nativeArray16[num11]).Add(RandomLocalizationIndex.kNone);
			}
			nativeArray16.Dispose();
		}
		if (!m_LabelVertexQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Game.Areas.LabelVertex>(m_LabelVertexQuery);
		}
		if (!m_RouteNumberQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<RouteNumber>(m_RouteNumberQuery);
		}
		if (!m_BlockerQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Blocker>(m_BlockerQuery);
		}
		if (!m_CitizenPresenceQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray17 = m_CitizenPresenceQuery.ToEntityArray(Allocator.TempJob);
			for (int num12 = 0; num12 < nativeArray17.Length; num12++)
			{
				base.EntityManager.AddComponentData(nativeArray17[num12], new CitizenPresence
				{
					m_Presence = 128
				});
			}
			nativeArray17.Dispose();
		}
		if (!m_SubLaneQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Game.Net.SubLane>(m_SubLaneQuery);
		}
		Context context = base.World.GetOrCreateSystemManaged<LoadGameSystem>().context;
		if (context.version < Version.netUpkeepCost && !m_NativeQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Native>(m_NativeQuery);
		}
		if (!m_GuestVehicleQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<GuestVehicle>(m_GuestVehicleQuery);
		}
		if (!m_TravelPurposeQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray18 = m_TravelPurposeQuery.ToEntityArray(Allocator.TempJob);
			for (int num13 = 0; num13 < nativeArray18.Length; num13++)
			{
				TravelPurpose componentData3 = base.EntityManager.GetComponentData<TravelPurpose>(nativeArray18[num13]);
				if ((componentData3.m_Purpose == Game.Citizens.Purpose.GoingToWork || componentData3.m_Purpose == Game.Citizens.Purpose.Working) && !base.EntityManager.HasComponent<Worker>(nativeArray18[num13]))
				{
					base.EntityManager.RemoveComponent<TravelPurpose>(nativeArray18[num13]);
				}
				else if ((componentData3.m_Purpose == Game.Citizens.Purpose.GoingToSchool || componentData3.m_Purpose == Game.Citizens.Purpose.Studying) && !base.EntityManager.HasComponent<Game.Citizens.Student>(nativeArray18[num13]))
				{
					base.EntityManager.RemoveComponent<TravelPurpose>(nativeArray18[num13]);
				}
			}
			nativeArray18.Dispose();
		}
		if (!m_TreeEffectQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<EnabledEffect>(m_TreeEffectQuery);
		}
		if (!m_TakeoffLocationQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Game.Routes.TakeoffLocation>(m_TakeoffLocationQuery);
		}
		if (!m_LeisureQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray19 = m_LeisureQuery.ToEntityArray(Allocator.TempJob);
			NativeArray<PrefabRef> nativeArray20 = m_LeisureQuery.ToComponentDataArray<PrefabRef>(Allocator.TempJob);
			for (int num14 = 0; num14 < nativeArray20.Length; num14++)
			{
				if (base.EntityManager.HasComponent<LeisureProviderData>(nativeArray20[num14].m_Prefab))
				{
					base.EntityManager.AddComponent<Game.Buildings.LeisureProvider>(nativeArray19[num14]);
				}
			}
			nativeArray20.Dispose();
			nativeArray19.Dispose();
		}
		if (!m_TransportDepotQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Game.Buildings.TransportDepot>(m_TransportDepotQuery);
		}
		if (!m_ServiceUsageQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray21 = m_ServiceUsageQuery.ToEntityArray(Allocator.TempJob);
			for (int num15 = 0; num15 < nativeArray21.Length; num15++)
			{
				base.EntityManager.AddComponentData(nativeArray21[num15], new ServiceUsage
				{
					m_Usage = 1f
				});
			}
			nativeArray21.Dispose();
		}
		if (!m_OutsideSellerQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<ResourceSeller>(m_OutsideSellerQuery);
		}
		if (!m_LoadingResourcesQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<LoadingResources>(m_LoadingResourcesQuery);
		}
		if (!m_CompanyVehicleQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<OwnedVehicle>(m_CompanyVehicleQuery);
		}
		if (m_LoadGameSystem.context.version < Version.pathfindAccessRestriction && !m_LaneRestrictionQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<PathfindUpdated>(m_LaneRestrictionQuery);
		}
		if (!m_LaneOverlapQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<LaneOverlap>(m_LaneOverlapQuery);
		}
		if (!m_DispatchedRequestQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<DispatchedRequest>(m_DispatchedRequestQuery);
		}
		if (!m_HomelessShelterQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray22 = m_HomelessShelterQuery.ToEntityArray(Allocator.TempJob);
			for (int num16 = 0; num16 < nativeArray22.Length; num16++)
			{
				base.EntityManager.AddBuffer<Renter>(nativeArray22[num16]);
			}
			nativeArray22.Dispose();
		}
		if (!m_QueueQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Queue>(m_QueueQuery);
		}
		if (!m_BoneHistoryQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<BoneHistory>(m_BoneHistoryQuery);
		}
		if (m_LoadGameSystem.context.version < Version.currentVehicleRefactoring && !m_UnspawnedQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Unspawned>(m_UnspawnedQuery);
		}
		if (m_LoadGameSystem.context.version < Version.areaLaneComponent)
		{
			if (!m_ConnectionLaneQuery.IsEmptyIgnoreFilter)
			{
				base.EntityManager.RemoveComponent<NodeLane>(m_ConnectionLaneQuery);
			}
			if (!m_AreaLaneQuery.IsEmptyIgnoreFilter)
			{
				base.EntityManager.AddComponent<Updated>(m_AreaLaneQuery);
			}
		}
		if (m_LoadGameSystem.context.version < Version.officePropertyComponent && !m_OfficeQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray23 = m_OfficeQuery.ToEntityArray(Allocator.TempJob);
			for (int num17 = 0; num17 < nativeArray23.Length; num17++)
			{
				if (base.EntityManager.TryGetComponent<PrefabRef>(nativeArray23[num17], out var component3) && base.EntityManager.TryGetComponent<BuildingPropertyData>(component3.m_Prefab, out var component4) && EconomyUtils.IsOfficeResource(component4.m_AllowedManufactured))
				{
					base.EntityManager.AddComponent<OfficeProperty>(nativeArray23[num17]);
				}
			}
			nativeArray23.Dispose();
		}
		if (!m_PassengerTransportQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<PassengerTransport>(m_PassengerTransportQuery);
		}
		if (!m_ObjectColorQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Game.Objects.Color>(m_ObjectColorQuery);
		}
		if (!m_OutsideConnectionQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Game.Objects.OutsideConnection>(m_OutsideConnectionQuery);
		}
		if (!m_NetConditionQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<NetCondition>(m_NetConditionQuery);
		}
		if (m_LoadGameSystem.context.version < Version.netPollutionAccumulation && !m_NetPollutionQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Game.Net.Pollution>(m_NetPollutionQuery);
		}
		if (!m_TrafficSpawnerQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Game.Buildings.TrafficSpawner>(m_TrafficSpawnerQuery);
		}
		if (!m_AreaExpandQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Expand>(m_AreaExpandQuery);
		}
		if (!m_EmissiveQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<LightState>(m_EmissiveQuery);
			base.EntityManager.AddComponent<Emissive>(m_EmissiveQuery);
		}
		if (!m_TrainBogieFrameQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<TrainBogieFrame>(m_TrainBogieFrameQuery);
		}
		if (!m_ProcessingTradeCostQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<TradeCost>(m_ProcessingTradeCostQuery);
		}
		if (context.version < Version.editorContainerFix && !m_EditorContainerQuery.IsEmptyIgnoreFilter)
		{
			if (context.purpose == Colossal.Serialization.Entities.Purpose.LoadMap)
			{
				base.EntityManager.AddComponent<CullingInfo>(m_EditorContainerQuery);
				base.EntityManager.AddComponent<Game.Tools.EditorContainer>(m_EditorContainerQuery);
			}
			else
			{
				base.EntityManager.DestroyEntity(m_EditorContainerQuery);
			}
		}
		if (!m_StorageConditionQuery.IsEmptyIgnoreFilter && context.version < Version.storageConditionReset)
		{
			NativeArray<Entity> nativeArray24 = m_StorageConditionQuery.ToEntityArray(Allocator.TempJob);
			for (int num18 = 0; num18 < nativeArray24.Length; num18++)
			{
				if (base.EntityManager.TryGetComponent<PropertyRenter>(nativeArray24[num18], out var component5) && base.EntityManager.TryGetComponent<BuildingCondition>(component5.m_Property, out var component6) && base.EntityManager.TryGetBuffer(nativeArray24[num18], isReadOnly: false, out DynamicBuffer<Game.Economy.Resources> buffer3))
				{
					component6.m_Condition = 0;
					base.EntityManager.SetComponentData(component5.m_Property, component6);
					EconomyUtils.SetResources(Resource.Money, buffer3, 0);
				}
			}
			nativeArray24.Dispose();
		}
		if (!m_LaneColorQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<LaneColor>(m_LaneColorQuery);
		}
		if (!m_CompanyNotificationQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<CompanyNotifications>(m_CompanyNotificationQuery);
		}
		if (!m_PlantQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Plant>(m_PlantQuery);
		}
		if (!m_CityPopulationQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Population>(m_CityPopulationQuery);
		}
		if (!m_CityTourismQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Tourism>(m_CityTourismQuery);
		}
		if (!m_BuildingNotificationQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<BuildingNotifications>(m_BuildingNotificationQuery);
		}
		if (context.version < Version.laneElevation)
		{
			if (!m_LaneElevationQuery.IsEmptyIgnoreFilter)
			{
				NativeArray<Entity> nativeArray25 = m_LaneElevationQuery.ToEntityArray(Allocator.TempJob);
				Game.Net.Elevation componentData5 = default(Game.Net.Elevation);
				for (int num19 = 0; num19 < nativeArray25.Length; num19++)
				{
					Entity entity = nativeArray25[num19];
					Entity owner = base.EntityManager.GetComponentData<Owner>(entity).m_Owner;
					if (base.EntityManager.TryGetComponent<Game.Objects.Transform>(owner, out var component7))
					{
						Curve componentData4 = base.EntityManager.GetComponentData<Curve>(entity);
						componentData5.m_Elevation.x = componentData4.m_Bezier.a.y - component7.m_Position.y;
						componentData5.m_Elevation.y = componentData4.m_Bezier.d.y - component7.m_Position.y;
						base.EntityManager.RemoveComponent<NodeLane>(entity);
						bool2 @bool = math.abs(componentData5.m_Elevation) >= 0.1f;
						if (math.any(@bool))
						{
							componentData5.m_Elevation = math.select(float.MinValue, componentData5.m_Elevation, @bool);
							base.EntityManager.AddComponentData(entity, componentData5);
						}
					}
				}
				nativeArray25.Dispose();
			}
			if (!m_AreaElevationQuery.IsEmptyIgnoreFilter)
			{
				NativeArray<Entity> nativeArray26 = m_AreaElevationQuery.ToEntityArray(Allocator.TempJob);
				for (int num20 = 0; num20 < nativeArray26.Length; num20++)
				{
					Entity entity2 = nativeArray26[num20];
					DynamicBuffer<Game.Areas.Node> buffer4 = base.EntityManager.GetBuffer<Game.Areas.Node>(entity2);
					if (base.EntityManager.HasComponent<Game.Areas.Space>(entity2) && base.EntityManager.TryGetComponent<Owner>(entity2, out var component8) && base.EntityManager.TryGetComponent<Game.Objects.Transform>(component8.m_Owner, out var component9))
					{
						for (int num21 = 0; num21 < buffer4.Length; num21++)
						{
							ref Game.Areas.Node reference = ref buffer4.ElementAt(num21);
							reference.m_Elevation = reference.m_Position.y - component9.m_Position.y;
							reference.m_Elevation = math.select(float.MinValue, reference.m_Elevation, math.abs(reference.m_Elevation) >= 0.1f);
						}
					}
					else
					{
						for (int num22 = 0; num22 < buffer4.Length; num22++)
						{
							buffer4.ElementAt(num22).m_Elevation = float.MinValue;
						}
					}
				}
				nativeArray26.Dispose();
			}
		}
		if (!m_BuildingLotQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Game.Buildings.Lot>(m_BuildingLotQuery);
		}
		if (!m_AreaTerrainQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Game.Areas.Terrain>(m_AreaTerrainQuery);
		}
		if (!m_OwnedVehicleQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<OwnedVehicle>(m_OwnedVehicleQuery);
		}
		if (context.version < Version.laneSubFlow)
		{
			if (!m_EdgeMappingQuery.IsEmptyIgnoreFilter)
			{
				base.EntityManager.AddComponent<EdgeMapping>(m_EdgeMappingQuery);
			}
			if (!m_SubFlowQuery.IsEmptyIgnoreFilter)
			{
				base.EntityManager.AddComponent<SubFlow>(m_SubFlowQuery);
			}
		}
		if (!m_PointOfInterestQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<PointOfInterest>(m_PointOfInterestQuery);
		}
		if (m_LoadGameSystem.context.version < Version.buildableArea && !m_BuildableAreaQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Updated>(m_BuildableAreaQuery);
		}
		if (context.version < Version.extractorSubAreas && !m_SubAreaQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Game.Areas.SubArea>(m_SubAreaQuery);
		}
		if (context.version < Version.enableableCrimeVictim && !m_CrimeVictimQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray27 = m_CrimeVictimQuery.ToEntityArray(Allocator.TempJob);
			base.EntityManager.AddComponent<CrimeVictim>(m_CrimeVictimQuery);
			for (int num23 = 0; num23 < nativeArray27.Length; num23++)
			{
				base.EntityManager.SetComponentEnabled<CrimeVictim>(nativeArray27[num23], value: false);
			}
			nativeArray27.Dispose();
		}
		if (context.version < Version.enableableCrimeVictim && !m_ArrivedQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray28 = m_ArrivedQuery.ToEntityArray(Allocator.TempJob);
			base.EntityManager.AddComponent<Arrived>(m_ArrivedQuery);
			for (int num24 = 0; num24 < nativeArray28.Length; num24++)
			{
				base.EntityManager.SetComponentEnabled<Arrived>(nativeArray28[num24], value: false);
			}
			nativeArray28.Dispose();
		}
		if (context.version < Version.enableableCrimeVictim && !m_MailSenderQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray29 = m_MailSenderQuery.ToEntityArray(Allocator.TempJob);
			base.EntityManager.AddComponent<MailSender>(m_MailSenderQuery);
			for (int num25 = 0; num25 < nativeArray29.Length; num25++)
			{
				base.EntityManager.SetComponentEnabled<MailSender>(nativeArray29[num25], value: false);
			}
			nativeArray29.Dispose();
		}
		if (context.version < Version.enableableCrimeVictim && !m_CarKeeperQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray30 = m_CarKeeperQuery.ToEntityArray(Allocator.TempJob);
			base.EntityManager.AddComponent<CarKeeper>(m_CarKeeperQuery);
			for (int num26 = 0; num26 < nativeArray30.Length; num26++)
			{
				base.EntityManager.SetComponentEnabled<CarKeeper>(nativeArray30[num26], value: false);
			}
			nativeArray30.Dispose();
		}
		if (context.version < Version.findJobOptimize && !m_NeedAddHasJobSeekerQuery.IsEmpty)
		{
			NativeArray<Entity> nativeArray31 = m_NeedAddHasJobSeekerQuery.ToEntityArray(Allocator.TempJob);
			base.EntityManager.AddComponent<HasJobSeeker>(m_NeedAddHasJobSeekerQuery);
			for (int num27 = 0; num27 < nativeArray31.Length; num27++)
			{
				base.EntityManager.SetComponentEnabled<HasJobSeeker>(nativeArray31[num27], value: false);
			}
			nativeArray31.Dispose();
		}
		if (!m_AgeGroupQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray32 = m_AgeGroupQuery.ToEntityArray(Allocator.TempJob);
			for (int num28 = 0; num28 < nativeArray32.Length; num28++)
			{
				Entity entity3 = nativeArray32[num28];
				Citizen componentData6 = base.EntityManager.GetComponentData<Citizen>(entity3);
				CitizenAge age;
				if (base.EntityManager.HasComponent<Child>(entity3))
				{
					age = CitizenAge.Child;
					base.EntityManager.RemoveComponent<Child>(entity3);
				}
				else if (base.EntityManager.HasComponent<Teen>(entity3))
				{
					age = CitizenAge.Teen;
					base.EntityManager.RemoveComponent<Teen>(entity3);
				}
				else if (base.EntityManager.HasComponent<Adult>(entity3))
				{
					age = CitizenAge.Adult;
					base.EntityManager.RemoveComponent<Adult>(entity3);
				}
				else
				{
					age = CitizenAge.Elderly;
					base.EntityManager.RemoveComponent<Elderly>(entity3);
				}
				componentData6.SetAge(age);
				base.EntityManager.SetComponentData(entity3, componentData6);
			}
			nativeArray32.Dispose();
		}
		if (context.version < Version.prefabRefAbuseFix)
		{
			NativeArray<Entity> nativeArray33 = m_PrefabRefQuery.ToEntityArray(Allocator.TempJob);
			NativeArray<PrefabRef> nativeArray34 = m_PrefabRefQuery.ToComponentDataArray<PrefabRef>(Allocator.TempJob);
			for (int num29 = 0; num29 < nativeArray34.Length; num29++)
			{
				if (!base.EntityManager.HasComponent<PrefabData>(nativeArray34[num29]))
				{
					base.EntityManager.DestroyEntity(nativeArray33[num29]);
				}
			}
			nativeArray33.Dispose();
			nativeArray34.Dispose();
		}
		if (!m_LabelMaterialQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<LabelMaterial>(m_LabelMaterialQuery);
		}
		if (!m_ArrowMaterialQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<ArrowMaterial>(m_ArrowMaterialQuery);
		}
		if (context.version < Version.enableableLocked && !m_LockedQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray35 = m_LockedQuery.ToEntityArray(Allocator.TempJob);
			for (int num30 = 0; num30 < nativeArray35.Length; num30++)
			{
				if (!base.EntityManager.HasComponent<Locked>(nativeArray35[num30]))
				{
					base.EntityManager.AddComponent<Locked>(nativeArray35[num30]);
					base.EntityManager.SetComponentEnabled<Locked>(nativeArray35[num30], value: false);
				}
			}
			nativeArray35.Dispose();
		}
		if (context.version < Version.pedestrianBorderCost && !m_OutsideUpdateQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Updated>(m_OutsideUpdateQuery);
		}
		if (context.version < Version.passengerWaitTimeCost)
		{
			if (!m_WaitingPassengersQuery.IsEmptyIgnoreFilter)
			{
				base.EntityManager.AddComponent<WaitingPassengers>(m_WaitingPassengersQuery);
			}
			if (!m_WaitingPassengersQuery2.IsEmptyIgnoreFilter)
			{
				base.EntityManager.RemoveComponent<WaitingPassengers>(m_WaitingPassengersQuery2);
			}
		}
		if (context.version < Version.pillarTerrainModification && !m_PillarQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray36 = m_PillarQuery.ToEntityArray(Allocator.TempJob);
			NativeArray<PrefabRef> nativeArray37 = m_PillarQuery.ToComponentDataArray<PrefabRef>(Allocator.TempJob);
			for (int num31 = 0; num31 < nativeArray36.Length; num31++)
			{
				if (base.EntityManager.HasComponent<PillarData>(nativeArray37[num31].m_Prefab))
				{
					base.EntityManager.AddComponent<Pillar>(nativeArray36[num31]);
				}
			}
			nativeArray36.Dispose();
			nativeArray37.Dispose();
		}
		if (context.version < Version.buildingEfficiencyRework && !m_LegacyEfficiencyQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Efficiency>(m_LegacyEfficiencyQuery);
			base.EntityManager.RemoveComponent<Game.Buildings.BuildingEfficiency>(m_LegacyEfficiencyQuery);
			NativeArray<Entity> nativeArray38 = m_LegacyEfficiencyQuery.ToEntityArray(Allocator.TempJob);
			for (int num32 = 0; num32 < nativeArray38.Length; num32++)
			{
				if (base.EntityManager.TryGetComponent<PrefabRef>(nativeArray38[num32], out var component10) && base.EntityManager.TryGetComponent<ConsumptionData>(component10, out var component11) && component11.m_TelecomNeed > 0f)
				{
					base.EntityManager.AddComponent<TelecomConsumer>(nativeArray38[num32]);
				}
			}
			nativeArray38.Dispose();
		}
		if (context.version < Version.signatureBuildingComponent && !m_SignatureQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Signature>(m_SignatureQuery);
		}
		if (context.version < Version.missingOwnerFix && !m_SubObjectOwnerQuery.IsEmptyIgnoreFilter)
		{
			int num33 = 0;
			NativeArray<Entity> nativeArray39 = m_SubObjectOwnerQuery.ToEntityArray(Allocator.TempJob);
			for (int num34 = 0; num34 < nativeArray39.Length; num34++)
			{
				if (base.EntityManager.TryGetComponent<Owner>(nativeArray39[num34], out var component12) && !base.EntityManager.Exists(component12.m_Owner))
				{
					base.EntityManager.DestroyEntity(nativeArray39[num34]);
					num33++;
				}
			}
			nativeArray39.Dispose();
			if (num33 != 0)
			{
				UnityEngine.Debug.LogWarning($"Destroyed {num33} entities with missing owners");
			}
		}
		if (context.version < Version.dangerLevel && !m_DangerLevelMissingQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Game.Events.DangerLevel>(m_DangerLevelMissingQuery);
		}
		if (!context.format.Has(FormatTags.ActivityProps))
		{
			if (!m_MeshGroupQuery.IsEmptyIgnoreFilter)
			{
				base.EntityManager.AddComponent<MeshGroup>(m_MeshGroupQuery);
			}
			if (!m_RequiresAnimatedBufferQuery.IsEmptyIgnoreFilter)
			{
				base.EntityManager.AddComponent<Animated>(m_RequiresAnimatedBufferQuery);
			}
			if (!m_CreatureInvolvedInAccidentQuery.IsEmptyIgnoreFilter)
			{
				NativeArray<Entity> nativeArray40 = m_CreatureInvolvedInAccidentQuery.ToEntityArray(Allocator.TempJob);
				for (int num35 = 0; num35 < nativeArray40.Length; num35++)
				{
					base.EntityManager.AddBuffer<TransformFrame>(nativeArray40[num35]);
					if (base.EntityManager.TryGetComponent<Human>(nativeArray40[num35], out var component13))
					{
						component13.m_Flags |= HumanFlags.Collapsed;
						base.EntityManager.SetComponentData(nativeArray40[num35], component13);
					}
					base.EntityManager.AddComponent<InterpolatedTransform>(nativeArray40[num35]);
				}
				nativeArray40.Dispose();
			}
			if (!m_SubObjectQuery.IsEmptyIgnoreFilter)
			{
				base.EntityManager.AddComponent<Game.Objects.SubObject>(m_SubObjectQuery);
			}
		}
		if (context.version < Version.surfaceStates && !m_ObjectSurfaceQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Game.Objects.Surface>(m_ObjectSurfaceQuery);
		}
		if (context.version < Version.meshColors && !m_MeshColorQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<MeshColor>(m_MeshColorQuery);
		}
		if (context.version < Version.plantUpdateFrame && !m_UpdateFrameQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray41 = m_UpdateFrameQuery.ToEntityArray(Allocator.TempJob);
			for (int num36 = 0; num36 < nativeArray41.Length; num36++)
			{
				base.EntityManager.AddSharedComponent(nativeArray41[num36], new UpdateFrame((uint)(num36 & 0xF)));
			}
			nativeArray41.Dispose();
		}
		if (context.version < Version.fenceColors && !m_PseudoRandomSeedQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray42 = m_PseudoRandomSeedQuery.ToEntityArray(Allocator.TempJob);
			Unity.Mathematics.Random random = new Unity.Mathematics.Random(math.max(1u, (uint)DateTime.Now.Ticks));
			for (int num37 = 0; num37 < nativeArray42.Length; num37++)
			{
				base.EntityManager.AddComponentData(nativeArray42[num37], new PseudoRandomSeed(ref random));
			}
			nativeArray42.Dispose();
		}
		if (context.version < Version.fenceColors && !m_FenceQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray43 = m_FenceQuery.ToEntityArray(Allocator.TempJob);
			for (int num38 = 0; num38 < nativeArray43.Length; num38++)
			{
				Entity entity4 = nativeArray43[num38];
				if (!base.EntityManager.TryGetComponent<PrefabRef>(entity4, out var component14))
				{
					continue;
				}
				if (base.EntityManager.TryGetComponent<NetLaneData>(component14.m_Prefab, out var component15) && (component15.m_Flags & LaneFlags.PseudoRandom) != 0)
				{
					PseudoRandomSeed component16 = default(PseudoRandomSeed);
					Entity entity5 = entity4;
					Owner component17;
					while (base.EntityManager.TryGetComponent<Owner>(entity5, out component17) && !base.EntityManager.TryGetComponent<PseudoRandomSeed>(component17.m_Owner, out component16))
					{
						entity5 = component17.m_Owner;
					}
					base.EntityManager.AddComponentData(entity4, component16);
					base.EntityManager.AddComponent<MeshColor>(entity4);
				}
				if (base.EntityManager.HasComponent<PlantData>(component14.m_Prefab))
				{
					base.EntityManager.AddComponent<Plant>(entity4);
					base.EntityManager.AddSharedComponent(entity4, new UpdateFrame((uint)(num38 & 0xF)));
				}
			}
			nativeArray43.Dispose();
		}
		if (context.version < Version.obsoleteNetPrefabs && !m_NetGeometrySectionQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<NetGeometryComposition>(m_NetGeometrySectionQuery);
			base.EntityManager.AddComponent<NetGeometrySection>(m_NetGeometrySectionQuery);
		}
		if (context.version < Version.obsoleteNetLanePrefabs && !m_NetLaneArchetypeDataQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<NetLaneArchetypeData>(m_NetLaneArchetypeDataQuery);
		}
		if (context.version < Version.pathfindRestrictions && !m_PathfindUpdatedQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<PathfindUpdated>(m_PathfindUpdatedQuery);
		}
		if (context.version < Version.cacheRouteColors && !m_RouteColorQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray44 = m_RouteColorQuery.ToEntityArray(Allocator.TempJob);
			for (int num39 = 0; num39 < nativeArray44.Length; num39++)
			{
				if (base.EntityManager.TryGetComponent<CurrentRoute>(nativeArray44[num39], out var component18) && base.EntityManager.TryGetComponent<Game.Routes.Color>(component18.m_Route, out var component19))
				{
					base.EntityManager.AddComponentData(nativeArray44[num39], component19);
				}
			}
			nativeArray44.Dispose();
		}
		if (context.version < Version.deathWaveMitigation && !m_CitizenQuery.IsEmptyIgnoreFilter)
		{
			Unity.Mathematics.Random random2 = RandomSeed.Next().GetRandom(0);
			NativeArray<Entity> nativeArray45 = m_CitizenQuery.ToEntityArray(Allocator.TempJob);
			TimeData singleton2 = GetEntityQuery(ComponentType.ReadOnly<TimeData>()).GetSingleton<TimeData>();
			uint frameIndex = base.World.GetOrCreateSystemManaged<SimulationSystem>().frameIndex;
			int day = TimeSystem.GetDay(frameIndex, __query_1938549536_0.GetSingleton<TimeData>());
			for (int num40 = 0; num40 < nativeArray45.Length; num40++)
			{
				Citizen component21;
				if (base.EntityManager.TryGetComponent<HealthProblem>(nativeArray45[num40], out var component20) && CitizenUtils.IsDead(component20) && (component20.m_HealthcareRequest == Entity.Null || !base.EntityManager.HasComponent<Dispatched>(component20.m_HealthcareRequest)))
				{
					base.EntityManager.AddComponent<Deleted>(nativeArray45[num40]);
				}
				else if (base.EntityManager.TryGetComponent<Citizen>(nativeArray45[num40], out component21) && component21.GetAgeInDays(frameIndex, singleton2) >= (float)AgingSystem.GetElderAgeLimitInDays() && random2.NextInt(100) > 1)
				{
					switch (random2.NextInt(3))
					{
					case 0:
						component21.m_BirthDay = (short)(day - 54 + random2.NextInt(18));
						break;
					case 1:
						component21.m_BirthDay = (short)(day - 69 + random2.NextInt(21));
						break;
					default:
						component21.m_BirthDay = (short)(day - 84 + random2.NextInt(21));
						break;
					}
					component21.SetAge(CitizenAge.Adult);
					base.EntityManager.SetComponentData(nativeArray45[num40], component21);
				}
			}
			nativeArray45.Dispose();
		}
		if (!m_ServiceUpkeepQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<OwnedVehicle>(m_ServiceUpkeepQuery);
		}
		if (!context.format.Has(FormatTags.StandingLegOffset) && !m_MoveableBridgeQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray46 = m_MoveableBridgeQuery.ToEntityArray(Allocator.TempJob);
			for (int num41 = 0; num41 < nativeArray46.Length; num41++)
			{
				if (base.EntityManager.TryGetComponent<Stack>(nativeArray46[num41], out var component22) && base.EntityManager.TryGetComponent<PrefabRef>(nativeArray46[num41], out var component23) && base.EntityManager.TryGetComponent<ObjectGeometryData>(component23.m_Prefab, out var component24))
				{
					component22.m_Range.max = math.max(component22.m_Range.max, component24.m_Bounds.max.y);
					base.EntityManager.SetComponentData(nativeArray46[num41], component22);
				}
			}
			nativeArray46.Dispose();
		}
		if (!m_SwayingQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Swaying>(m_SwayingQuery);
		}
		if (!m_OfficeCompaniesQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray47 = m_OfficeCompaniesQuery.ToEntityArray(Allocator.TempJob);
			for (int num42 = 0; num42 < nativeArray47.Length; num42++)
			{
				if (base.EntityManager.TryGetComponent<PropertyRenter>(nativeArray47[num42], out var component25) && base.EntityManager.HasComponent<OfficeProperty>(component25.m_Property))
				{
					base.EntityManager.AddComponent<OfficeCompany>(nativeArray47[num42]);
				}
			}
			nativeArray47.Dispose();
		}
		if (!m_PlaybackLayerQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray48 = m_PlaybackLayerQuery.ToEntityArray(Allocator.TempJob);
			for (int num43 = 0; num43 < nativeArray48.Length; num43++)
			{
				if (!base.EntityManager.TryGetComponent<PrefabRef>(nativeArray48[num43], out var component26) || !base.EntityManager.TryGetBuffer(component26.m_Prefab, isReadOnly: true, out DynamicBuffer<SubMesh> buffer5))
				{
					continue;
				}
				for (int num44 = 0; num44 < buffer5.Length; num44++)
				{
					if (base.EntityManager.HasComponent<AnimationClip>(buffer5[num44].m_SubMesh))
					{
						base.EntityManager.AddComponent<PlaybackLayer>(nativeArray48[num43]);
						break;
					}
				}
			}
			nativeArray48.Dispose();
		}
		if (!m_OldSignatureBuildingQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray49 = m_OldSignatureBuildingQuery.ToEntityArray(Allocator.TempJob);
			for (int num45 = 0; num45 < nativeArray49.Length; num45++)
			{
				base.EntityManager.AddComponent<PropertyToBeOnMarket>(nativeArray49[num45]);
			}
			nativeArray49.Dispose();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<TimeData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_1938549536_0 = entityQueryBuilder2.Build(ref state);
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
	public RequiredComponentSystem()
	{
	}
}

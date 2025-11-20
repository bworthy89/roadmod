using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.Common;
using Game.Creatures;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Routes;
using Game.Serialization;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Notifications;

[CompilerGenerated]
public class MarkerCreateSystem : GameSystemBase, IPostDeserialize
{
	[BurstCompile]
	private struct MarkerCreateJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Hidden> m_HiddenType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewTransportStopData> m_InfoviewTransportStopType;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewBuildingData> m_InfoviewBuildingType;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewBuildingStatusData> m_InfoviewBuildingStatusType;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewVehicleData> m_InfoviewVehicleType;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewMarkerData> m_InfoviewMarkerType;

		[ReadOnly]
		public ComponentTypeHandle<InfomodeActive> m_InfomodeActiveType;

		[ReadOnly]
		public ComponentTypeHandle<TransportStopMarkerData> m_TransportStopMarkerType;

		[ReadOnly]
		public ComponentTypeHandle<BuildingMarkerData> m_BuildingMarkerType;

		[ReadOnly]
		public ComponentTypeHandle<VehicleMarkerData> m_VehicleMarkerType;

		[ReadOnly]
		public ComponentTypeHandle<MarkerMarkerData> m_MarkerMarkerType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Routes.TransportStop> m_TransportStopType;

		[ReadOnly]
		public ComponentTypeHandle<Building> m_BuildingType;

		[ReadOnly]
		public ComponentTypeHandle<Vehicle> m_VehicleType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Marker> m_MarkerType;

		[ReadOnly]
		public ComponentTypeHandle<Controller> m_ControllerType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.UniqueObject> m_UniqueObjectType;

		[ReadOnly]
		public ComponentTypeHandle<ParkedCar> m_ParkedCarType;

		[ReadOnly]
		public ComponentTypeHandle<ParkedTrain> m_ParkedTrainType;

		[ReadOnly]
		public BufferTypeHandle<IconElement> m_IconElementType;

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
		public ComponentTypeHandle<Game.Buildings.Hospital> m_HospitalType;

		[ReadOnly]
		public ComponentTypeHandle<ElectricityProducer> m_ElectricityProducerType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.Transformer> m_TransformerType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.Battery> m_BatteryType;

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
		public ComponentTypeHandle<Game.Buildings.EmergencyShelter> m_EmergencyShelterType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.DisasterFacility> m_DisasterFacilityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.FirewatchTower> m_FirewatchTowerType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.Park> m_ParkType;

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
		public ComponentTypeHandle<ResidentialProperty> m_ResidentialPropertyType;

		[ReadOnly]
		public ComponentTypeHandle<CommercialProperty> m_CommercialPropertyType;

		[ReadOnly]
		public ComponentTypeHandle<IndustrialProperty> m_IndustrialPropertyType;

		[ReadOnly]
		public ComponentTypeHandle<OfficeProperty> m_OfficePropertyType;

		[ReadOnly]
		public ComponentTypeHandle<ExtractorProperty> m_ExtractorPropertyType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.ServiceUpgrade> m_ServiceUpgradeType;

		[ReadOnly]
		public SharedComponentTypeHandle<CoverageServiceType> m_CoverageServiceType;

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
		public ComponentTypeHandle<Game.Creatures.CreatureSpawner> m_CreatureSpawnerType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.OutsideConnection> m_OutsideConnectionType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.ElectricityOutsideConnection> m_ElectricityOutsideConnectionType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.WaterPipeOutsideConnection> m_WaterPipeOutsideConnectionType;

		[ReadOnly]
		public ComponentLookup<Icon> m_IconData;

		[ReadOnly]
		public ComponentLookup<Controller> m_ControllerData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<TransportStopData> m_TransportStopData;

		[ReadOnly]
		public ComponentLookup<WorkStopData> m_WorkStopData;

		[NativeDisableContainerSafetyRestriction]
		[ReadOnly]
		public NativeArray<ArchetypeChunk> m_InfomodeChunks;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_IconChunks;

		[ReadOnly]
		public TransportType m_RequiredTransportStopType;

		[ReadOnly]
		public MarkerType m_RequiredMarkerType;

		[ReadOnly]
		public bool m_RequireStandaloneStops;

		public IconCommandBuffer m_IconCommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.Has(ref m_ServiceUpgradeType) && !chunk.Has(m_CoverageServiceType))
			{
				return;
			}
			Entity entity = Entity.Null;
			Entity entity2 = Entity.Null;
			bool disallowCluster = false;
			bool flag = false;
			bool flag2 = false;
			VehicleType vehicleType;
			Entity markerPrefab3;
			if (chunk.Has(ref m_TransportStopType))
			{
				if (GetTransportStopType(chunk, out var transportType))
				{
					Entity markerPrefabA;
					Entity markerPrefabB;
					if (chunk.Has(ref m_OutsideConnectionType))
					{
						if (GetMarkerType(transportType, out var markerType) && FindMarkerPrefab(markerType, out var markerPrefab))
						{
							entity = markerPrefab;
						}
					}
					else if (FindMarkerPrefab(transportType, out markerPrefabA, out markerPrefabB))
					{
						entity = markerPrefabA;
						entity2 = markerPrefabB;
						flag2 = true;
					}
				}
			}
			else if (chunk.Has(ref m_BuildingType))
			{
				if (GetBuildingType(chunk, out var buildingType) && FindMarkerPrefab(buildingType, out var markerPrefab2))
				{
					entity = markerPrefab2;
				}
			}
			else if (chunk.Has(ref m_VehicleType) && GetVehicleType(chunk, out vehicleType) && FindMarkerPrefab(vehicleType, out markerPrefab3))
			{
				entity = markerPrefab3;
				disallowCluster = true;
				flag = true;
			}
			if (entity == Entity.Null && entity2 == Entity.Null && chunk.Has(ref m_MarkerType) && GetMarkerType(chunk, out var markerType2) && FindMarkerPrefab(markerType2, out var markerPrefab4))
			{
				entity = markerPrefab4;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<IconElement> bufferAccessor = chunk.GetBufferAccessor(ref m_IconElementType);
			if (entity != Entity.Null || (flag2 && entity2 != Entity.Null))
			{
				NativeArray<Temp> nativeArray2 = chunk.GetNativeArray(ref m_TempType);
				bool isHidden = chunk.Has(ref m_HiddenType);
				bool flag3 = nativeArray2.Length != 0;
				if (flag)
				{
					NativeArray<Controller> nativeArray3 = chunk.GetNativeArray(ref m_ControllerType);
					if (nativeArray3.Length != 0)
					{
						for (int i = 0; i < nativeArray.Length; i++)
						{
							Entity entity3 = nativeArray[i];
							if (flag3)
							{
								Entity original = nativeArray2[i].m_Original;
								if (m_ControllerData.TryGetComponent(original, out var componentData))
								{
									if (componentData.m_Controller != original)
									{
										continue;
									}
								}
								else if (nativeArray3[i].m_Controller != entity3)
								{
									continue;
								}
							}
							else if (nativeArray3[i].m_Controller != entity3)
							{
								continue;
							}
							m_IconCommandBuffer.Add(entity3, entity, IconPriority.Info, IconClusterLayer.Marker, IconFlags.Unique, Entity.Null, flag3, isHidden, disallowCluster);
						}
					}
					else
					{
						flag = false;
					}
				}
				if (flag2)
				{
					NativeArray<PrefabRef> nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);
					for (int j = 0; j < nativeArray.Length; j++)
					{
						Entity owner = nativeArray[j];
						PrefabRef prefabRef = nativeArray4[j];
						Entity prefab;
						if (m_WorkStopData.TryGetComponent(prefabRef.m_Prefab, out var componentData2))
						{
							if (!componentData2.m_WorkLocation && entity != Entity.Null)
							{
								prefab = entity;
							}
							else
							{
								if (!componentData2.m_WorkLocation || !(entity2 != Entity.Null))
								{
									continue;
								}
								prefab = entity2;
							}
						}
						else
						{
							if (!m_TransportStopData.TryGetComponent(prefabRef.m_Prefab, out var componentData3))
							{
								continue;
							}
							if (componentData3.m_PassengerTransport && entity != Entity.Null)
							{
								prefab = entity;
							}
							else
							{
								if (!componentData3.m_CargoTransport || !(entity2 != Entity.Null))
								{
									continue;
								}
								prefab = entity2;
							}
						}
						m_IconCommandBuffer.Add(owner, prefab, IconPriority.Info, IconClusterLayer.Marker, IconFlags.Unique, Entity.Null, flag3, isHidden, disallowCluster);
					}
				}
				if (!flag && !flag2)
				{
					for (int k = 0; k < nativeArray.Length; k++)
					{
						Entity owner2 = nativeArray[k];
						m_IconCommandBuffer.Add(owner2, entity, IconPriority.Info, IconClusterLayer.Marker, IconFlags.Unique, Entity.Null, flag3, isHidden, disallowCluster);
					}
				}
			}
			for (int l = 0; l < bufferAccessor.Length; l++)
			{
				Entity owner3 = nativeArray[l];
				DynamicBuffer<IconElement> dynamicBuffer = bufferAccessor[l];
				for (int m = 0; m < dynamicBuffer.Length; m++)
				{
					IconElement iconElement = dynamicBuffer[m];
					if (m_IconData[iconElement.m_Icon].m_ClusterLayer == IconClusterLayer.Marker)
					{
						PrefabRef prefabRef2 = m_PrefabRefData[iconElement.m_Icon];
						if (prefabRef2.m_Prefab != entity && prefabRef2.m_Prefab != entity2)
						{
							m_IconCommandBuffer.Remove(owner3, prefabRef2.m_Prefab);
						}
					}
				}
			}
		}

		private bool GetTransportStopType(ArchetypeChunk chunk, out TransportType transportType)
		{
			transportType = TransportType.None;
			int num = int.MaxValue;
			if (m_InfomodeChunks.IsCreated)
			{
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
						int priority = nativeArray2[j].m_Priority;
						if (priority < num)
						{
							TransportType type = nativeArray[j].m_Type;
							if (IsTransportStopType(chunk, type))
							{
								transportType = type;
								num = priority;
							}
						}
					}
				}
			}
			if (transportType == TransportType.None && IsTransportStopType(chunk, m_RequiredTransportStopType))
			{
				transportType = m_RequiredTransportStopType;
			}
			if (transportType == TransportType.None && m_RequireStandaloneStops && !chunk.Has(ref m_OutsideConnectionType) && !chunk.Has(ref m_OwnerType))
			{
				if (chunk.Has(ref m_BusStopType))
				{
					transportType = TransportType.Bus;
				}
				else if (chunk.Has(ref m_TaxiStandType))
				{
					transportType = TransportType.Taxi;
				}
				else if (chunk.Has(ref m_TramStopType))
				{
					transportType = TransportType.Tram;
				}
				else if (chunk.Has(ref m_MailBoxType))
				{
					transportType = TransportType.Post;
				}
				else if (chunk.Has(ref m_WorkStopType))
				{
					transportType = TransportType.Work;
				}
				else if (chunk.Has(ref m_BicycleParkingType))
				{
					transportType = TransportType.Bicycle;
				}
			}
			return transportType != TransportType.None;
		}

		private bool IsTransportStopType(ArchetypeChunk chunk, TransportType transportType)
		{
			return transportType switch
			{
				TransportType.Bus => chunk.Has(ref m_BusStopType), 
				TransportType.Train => chunk.Has(ref m_TrainStopType), 
				TransportType.Taxi => chunk.Has(ref m_TaxiStandType), 
				TransportType.Tram => chunk.Has(ref m_TramStopType), 
				TransportType.Ship => chunk.Has(ref m_ShipStopType), 
				TransportType.Post => chunk.Has(ref m_MailBoxType), 
				TransportType.Work => chunk.Has(ref m_WorkStopType), 
				TransportType.Airplane => chunk.Has(ref m_AirplaneStopType), 
				TransportType.Subway => chunk.Has(ref m_SubwayStopType), 
				TransportType.Ferry => chunk.Has(ref m_FerryStopType), 
				TransportType.Bicycle => chunk.Has(ref m_BicycleParkingType), 
				_ => false, 
			};
		}

		private bool FindMarkerPrefab(TransportType transportType, out Entity markerPrefabA, out Entity markerPrefabB)
		{
			bool result = false;
			markerPrefabA = Entity.Null;
			markerPrefabB = Entity.Null;
			for (int i = 0; i < m_IconChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_IconChunks[i];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<TransportStopMarkerData> nativeArray2 = archetypeChunk.GetNativeArray(ref m_TransportStopMarkerType);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					TransportStopMarkerData transportStopMarkerData = nativeArray2[j];
					if (transportStopMarkerData.m_TransportType == transportType)
					{
						if (transportStopMarkerData.m_StopTypeA)
						{
							markerPrefabA = nativeArray[j];
							result = true;
						}
						else if (transportStopMarkerData.m_StopTypeB)
						{
							markerPrefabB = nativeArray[j];
							result = true;
						}
					}
				}
			}
			return result;
		}

		private bool GetBuildingType(ArchetypeChunk chunk, out BuildingType buildingType)
		{
			buildingType = BuildingType.None;
			int num = int.MaxValue;
			if (m_InfomodeChunks.IsCreated)
			{
				for (int i = 0; i < m_InfomodeChunks.Length; i++)
				{
					ArchetypeChunk archetypeChunk = m_InfomodeChunks[i];
					NativeArray<InfoviewBuildingData> nativeArray = archetypeChunk.GetNativeArray(ref m_InfoviewBuildingType);
					NativeArray<InfoviewBuildingStatusData> nativeArray2 = archetypeChunk.GetNativeArray(ref m_InfoviewBuildingStatusType);
					if (nativeArray.Length == 0 && nativeArray2.Length == 0)
					{
						continue;
					}
					NativeArray<InfomodeActive> nativeArray3 = archetypeChunk.GetNativeArray(ref m_InfomodeActiveType);
					for (int j = 0; j < nativeArray.Length; j++)
					{
						int priority = nativeArray3[j].m_Priority;
						if (priority < num)
						{
							BuildingType type = nativeArray[j].m_Type;
							if (IsBuildingType(chunk, type))
							{
								buildingType = type;
								num = priority;
							}
						}
					}
					for (int k = 0; k < nativeArray2.Length; k++)
					{
						int priority2 = nativeArray3[k].m_Priority;
						if (priority2 < num)
						{
							BuildingType buildingType2 = GetBuildingType(nativeArray2[k].m_Type);
							if (IsBuildingType(chunk, buildingType2))
							{
								buildingType = buildingType2;
								num = priority2;
							}
						}
					}
				}
			}
			return buildingType != BuildingType.None;
		}

		private BuildingType GetBuildingType(BuildingStatusType buildingStatusType)
		{
			return buildingStatusType switch
			{
				BuildingStatusType.SignatureResidential => BuildingType.SignatureResidential, 
				BuildingStatusType.SignatureCommercial => BuildingType.SignatureCommercial, 
				BuildingStatusType.SignatureIndustrial => BuildingType.SignatureIndustrial, 
				BuildingStatusType.SignatureOffice => BuildingType.SignatureOffice, 
				_ => BuildingType.None, 
			};
		}

		private bool IsBuildingType(ArchetypeChunk chunk, BuildingType buildingType)
		{
			switch (buildingType)
			{
			case BuildingType.Hospital:
				return chunk.Has(ref m_HospitalType);
			case BuildingType.PowerPlant:
				return chunk.Has(ref m_ElectricityProducerType);
			case BuildingType.Transformer:
				return chunk.Has(ref m_TransformerType);
			case BuildingType.Battery:
				return chunk.Has(ref m_BatteryType);
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
			case BuildingType.ParkMaintenanceDepot:
				return chunk.Has(ref m_ParkMaintenanceType);
			case BuildingType.PostFacility:
				return chunk.Has(ref m_PostFacilityType);
			case BuildingType.TelecomFacility:
				return chunk.Has(ref m_TelecomFacilityType);
			case BuildingType.School:
				return chunk.Has(ref m_SchoolType);
			case BuildingType.EmergencyShelter:
				return chunk.Has(ref m_EmergencyShelterType);
			case BuildingType.DisasterFacility:
				return chunk.Has(ref m_DisasterFacilityType);
			case BuildingType.FirewatchTower:
				return chunk.Has(ref m_FirewatchTowerType);
			case BuildingType.Park:
				return chunk.Has(ref m_ParkType);
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
			case BuildingType.CarParkingFacility:
				return chunk.Has(ref m_CarParkingFacilityType);
			case BuildingType.BicycleParkingFacility:
				return chunk.Has(ref m_BicycleParkingFacilityType);
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
			default:
				return false;
			}
		}

		private bool FindMarkerPrefab(BuildingType buildingType, out Entity markerPrefab)
		{
			for (int i = 0; i < m_IconChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_IconChunks[i];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<BuildingMarkerData> nativeArray2 = archetypeChunk.GetNativeArray(ref m_BuildingMarkerType);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					if (nativeArray2[j].m_BuildingType == buildingType)
					{
						markerPrefab = nativeArray[j];
						return true;
					}
				}
			}
			markerPrefab = Entity.Null;
			return false;
		}

		private bool GetVehicleType(ArchetypeChunk chunk, out VehicleType vehicleType)
		{
			vehicleType = VehicleType.None;
			int num = int.MaxValue;
			if (m_InfomodeChunks.IsCreated && !chunk.Has(ref m_ParkedCarType) && !chunk.Has(ref m_ParkedTrainType))
			{
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
						int priority = nativeArray2[j].m_Priority;
						if (priority < num)
						{
							VehicleType type = nativeArray[j].m_Type;
							if (IsVehicleType(chunk, type))
							{
								vehicleType = type;
								num = priority;
							}
						}
					}
				}
			}
			return vehicleType != VehicleType.None;
		}

		private bool IsVehicleType(ArchetypeChunk chunk, VehicleType vehicleType)
		{
			return vehicleType switch
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

		private bool FindMarkerPrefab(VehicleType vehicleType, out Entity markerPrefab)
		{
			for (int i = 0; i < m_IconChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_IconChunks[i];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<VehicleMarkerData> nativeArray2 = archetypeChunk.GetNativeArray(ref m_VehicleMarkerType);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					if (nativeArray2[j].m_VehicleType == vehicleType)
					{
						markerPrefab = nativeArray[j];
						return true;
					}
				}
			}
			markerPrefab = Entity.Null;
			return false;
		}

		private bool GetMarkerType(ArchetypeChunk chunk, out MarkerType markerType)
		{
			markerType = MarkerType.None;
			int num = int.MaxValue;
			if (m_InfomodeChunks.IsCreated)
			{
				for (int i = 0; i < m_InfomodeChunks.Length; i++)
				{
					ArchetypeChunk archetypeChunk = m_InfomodeChunks[i];
					NativeArray<InfoviewMarkerData> nativeArray = archetypeChunk.GetNativeArray(ref m_InfoviewMarkerType);
					if (nativeArray.Length == 0)
					{
						continue;
					}
					NativeArray<InfomodeActive> nativeArray2 = archetypeChunk.GetNativeArray(ref m_InfomodeActiveType);
					for (int j = 0; j < nativeArray.Length; j++)
					{
						int priority = nativeArray2[j].m_Priority;
						if (priority < num)
						{
							MarkerType type = nativeArray[j].m_Type;
							if (IsMarkerType(chunk, type))
							{
								markerType = type;
								num = priority;
							}
						}
					}
				}
			}
			if (markerType == MarkerType.None && IsMarkerType(chunk, m_RequiredMarkerType))
			{
				markerType = m_RequiredMarkerType;
			}
			return markerType != MarkerType.None;
		}

		private bool GetMarkerType(TransportType transportType, out MarkerType markerType)
		{
			switch (transportType)
			{
			case TransportType.Bus:
				markerType = MarkerType.RoadOutsideConnection;
				return true;
			case TransportType.Train:
				markerType = MarkerType.TrainOutsideConnection;
				return true;
			case TransportType.Ship:
				markerType = MarkerType.ShipOutsideConnection;
				return true;
			case TransportType.Airplane:
				markerType = MarkerType.AirplaneOutsideConnection;
				return true;
			default:
				markerType = MarkerType.None;
				return false;
			}
		}

		private bool IsMarkerType(ArchetypeChunk chunk, MarkerType markerType)
		{
			switch (markerType)
			{
			case MarkerType.CreatureSpawner:
				return chunk.Has(ref m_CreatureSpawnerType);
			case MarkerType.RoadOutsideConnection:
				if (chunk.Has(ref m_BusStopType))
				{
					return chunk.Has(ref m_OutsideConnectionType);
				}
				return false;
			case MarkerType.TrainOutsideConnection:
				if (chunk.Has(ref m_TrainStopType))
				{
					return chunk.Has(ref m_OutsideConnectionType);
				}
				return false;
			case MarkerType.ShipOutsideConnection:
				if (chunk.Has(ref m_ShipStopType))
				{
					return chunk.Has(ref m_OutsideConnectionType);
				}
				return false;
			case MarkerType.AirplaneOutsideConnection:
				if (chunk.Has(ref m_AirplaneStopType))
				{
					return chunk.Has(ref m_OutsideConnectionType);
				}
				return false;
			case MarkerType.ElectricityOutsideConnection:
				return chunk.Has(ref m_ElectricityOutsideConnectionType);
			case MarkerType.WaterPipeOutsideConnection:
				return chunk.Has(ref m_WaterPipeOutsideConnectionType);
			default:
				return false;
			}
		}

		private bool FindMarkerPrefab(MarkerType markerType, out Entity markerPrefab)
		{
			for (int i = 0; i < m_IconChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_IconChunks[i];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<MarkerMarkerData> nativeArray2 = archetypeChunk.GetNativeArray(ref m_MarkerMarkerType);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					if (nativeArray2[j].m_MarkerType == markerType)
					{
						markerPrefab = nativeArray[j];
						return true;
					}
				}
			}
			markerPrefab = Entity.Null;
			return false;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<InfoviewTransportStopData> __Game_Prefabs_InfoviewTransportStopData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewBuildingData> __Game_Prefabs_InfoviewBuildingData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewBuildingStatusData> __Game_Prefabs_InfoviewBuildingStatusData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewVehicleData> __Game_Prefabs_InfoviewVehicleData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewMarkerData> __Game_Prefabs_InfoviewMarkerData_RO_ComponentTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Hidden> __Game_Tools_Hidden_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InfomodeActive> __Game_Prefabs_InfomodeActive_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TransportStopMarkerData> __Game_Prefabs_TransportStopMarkerData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<BuildingMarkerData> __Game_Prefabs_BuildingMarkerData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<VehicleMarkerData> __Game_Prefabs_VehicleMarkerData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<MarkerMarkerData> __Game_Prefabs_MarkerMarkerData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Routes.TransportStop> __Game_Routes_TransportStop_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Building> __Game_Buildings_Building_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Vehicle> __Game_Vehicles_Vehicle_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Marker> __Game_Objects_Marker_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Controller> __Game_Vehicles_Controller_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ParkedCar> __Game_Vehicles_ParkedCar_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ParkedTrain> __Game_Vehicles_ParkedTrain_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.UniqueObject> __Game_Objects_UniqueObject_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<IconElement> __Game_Notifications_IconElement_RO_BufferTypeHandle;

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
		public ComponentTypeHandle<Game.Buildings.Hospital> __Game_Buildings_Hospital_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ElectricityProducer> __Game_Buildings_ElectricityProducer_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.Transformer> __Game_Buildings_Transformer_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.Battery> __Game_Buildings_Battery_RO_ComponentTypeHandle;

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
		public ComponentTypeHandle<Game.Buildings.EmergencyShelter> __Game_Buildings_EmergencyShelter_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.DisasterFacility> __Game_Buildings_DisasterFacility_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.FirewatchTower> __Game_Buildings_FirewatchTower_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.Park> __Game_Buildings_Park_RO_ComponentTypeHandle;

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
		public ComponentTypeHandle<ResidentialProperty> __Game_Buildings_ResidentialProperty_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CommercialProperty> __Game_Buildings_CommercialProperty_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<IndustrialProperty> __Game_Buildings_IndustrialProperty_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<OfficeProperty> __Game_Buildings_OfficeProperty_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ExtractorProperty> __Game_Buildings_ExtractorProperty_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.ServiceUpgrade> __Game_Buildings_ServiceUpgrade_RO_ComponentTypeHandle;

		public SharedComponentTypeHandle<CoverageServiceType> __Game_Net_CoverageServiceType_SharedComponentTypeHandle;

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
		public ComponentTypeHandle<Game.Creatures.CreatureSpawner> __Game_Creatures_CreatureSpawner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.ElectricityOutsideConnection> __Game_Objects_ElectricityOutsideConnection_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.WaterPipeOutsideConnection> __Game_Objects_WaterPipeOutsideConnection_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Icon> __Game_Notifications_Icon_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Controller> __Game_Vehicles_Controller_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TransportStopData> __Game_Prefabs_TransportStopData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WorkStopData> __Game_Prefabs_WorkStopData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_InfoviewTransportStopData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewTransportStopData>(isReadOnly: true);
			__Game_Prefabs_InfoviewBuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewBuildingData>(isReadOnly: true);
			__Game_Prefabs_InfoviewBuildingStatusData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewBuildingStatusData>(isReadOnly: true);
			__Game_Prefabs_InfoviewVehicleData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewVehicleData>(isReadOnly: true);
			__Game_Prefabs_InfoviewMarkerData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewMarkerData>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Tools_Hidden_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Hidden>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_InfomodeActive_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfomodeActive>(isReadOnly: true);
			__Game_Prefabs_TransportStopMarkerData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TransportStopMarkerData>(isReadOnly: true);
			__Game_Prefabs_BuildingMarkerData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingMarkerData>(isReadOnly: true);
			__Game_Prefabs_VehicleMarkerData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<VehicleMarkerData>(isReadOnly: true);
			__Game_Prefabs_MarkerMarkerData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<MarkerMarkerData>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Routes_TransportStop_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Routes.TransportStop>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Building>(isReadOnly: true);
			__Game_Vehicles_Vehicle_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Vehicle>(isReadOnly: true);
			__Game_Objects_Marker_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Marker>(isReadOnly: true);
			__Game_Vehicles_Controller_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Controller>(isReadOnly: true);
			__Game_Vehicles_ParkedCar_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ParkedCar>(isReadOnly: true);
			__Game_Vehicles_ParkedTrain_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ParkedTrain>(isReadOnly: true);
			__Game_Objects_UniqueObject_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.UniqueObject>(isReadOnly: true);
			__Game_Notifications_IconElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<IconElement>(isReadOnly: true);
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
			__Game_Buildings_Hospital_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.Hospital>(isReadOnly: true);
			__Game_Buildings_ElectricityProducer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ElectricityProducer>(isReadOnly: true);
			__Game_Buildings_Transformer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.Transformer>(isReadOnly: true);
			__Game_Buildings_Battery_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.Battery>(isReadOnly: true);
			__Game_Buildings_WaterPumpingStation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.WaterPumpingStation>(isReadOnly: true);
			__Game_Buildings_WaterTower_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.WaterTower>(isReadOnly: true);
			__Game_Buildings_SewageOutlet_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.SewageOutlet>(isReadOnly: true);
			__Game_Buildings_WastewaterTreatmentPlant_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WastewaterTreatmentPlant>(isReadOnly: true);
			__Game_Buildings_TransportDepot_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.TransportDepot>(isReadOnly: true);
			__Game_Buildings_TransportStation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.TransportStation>(isReadOnly: true);
			__Game_Buildings_GarbageFacility_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.GarbageFacility>(isReadOnly: true);
			__Game_Buildings_FireStation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.FireStation>(isReadOnly: true);
			__Game_Buildings_PoliceStation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.PoliceStation>(isReadOnly: true);
			__Game_Buildings_RoadMaintenance_RO_ComponentTypeHandle = state.GetComponentTypeHandle<RoadMaintenance>(isReadOnly: true);
			__Game_Buildings_ParkMaintenance_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ParkMaintenance>(isReadOnly: true);
			__Game_Buildings_PostFacility_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.PostFacility>(isReadOnly: true);
			__Game_Buildings_TelecomFacility_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.TelecomFacility>(isReadOnly: true);
			__Game_Buildings_School_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.School>(isReadOnly: true);
			__Game_Buildings_EmergencyShelter_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.EmergencyShelter>(isReadOnly: true);
			__Game_Buildings_DisasterFacility_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.DisasterFacility>(isReadOnly: true);
			__Game_Buildings_FirewatchTower_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.FirewatchTower>(isReadOnly: true);
			__Game_Buildings_Park_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.Park>(isReadOnly: true);
			__Game_Buildings_DeathcareFacility_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.DeathcareFacility>(isReadOnly: true);
			__Game_Buildings_Prison_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.Prison>(isReadOnly: true);
			__Game_Buildings_AdminBuilding_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AdminBuilding>(isReadOnly: true);
			__Game_Buildings_WelfareOffice_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.WelfareOffice>(isReadOnly: true);
			__Game_Buildings_ResearchFacility_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.ResearchFacility>(isReadOnly: true);
			__Game_Buildings_CarParkingFacility_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CarParkingFacility>(isReadOnly: true);
			__Game_Buildings_BicycleParkingFacility_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BicycleParkingFacility>(isReadOnly: true);
			__Game_Buildings_ResidentialProperty_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ResidentialProperty>(isReadOnly: true);
			__Game_Buildings_CommercialProperty_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CommercialProperty>(isReadOnly: true);
			__Game_Buildings_IndustrialProperty_RO_ComponentTypeHandle = state.GetComponentTypeHandle<IndustrialProperty>(isReadOnly: true);
			__Game_Buildings_OfficeProperty_RO_ComponentTypeHandle = state.GetComponentTypeHandle<OfficeProperty>(isReadOnly: true);
			__Game_Buildings_ExtractorProperty_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ExtractorProperty>(isReadOnly: true);
			__Game_Buildings_ServiceUpgrade_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.ServiceUpgrade>(isReadOnly: true);
			__Game_Net_CoverageServiceType_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<CoverageServiceType>();
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
			__Game_Creatures_CreatureSpawner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Creatures.CreatureSpawner>(isReadOnly: true);
			__Game_Objects_OutsideConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.OutsideConnection>(isReadOnly: true);
			__Game_Objects_ElectricityOutsideConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.ElectricityOutsideConnection>(isReadOnly: true);
			__Game_Objects_WaterPipeOutsideConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.WaterPipeOutsideConnection>(isReadOnly: true);
			__Game_Notifications_Icon_RO_ComponentLookup = state.GetComponentLookup<Icon>(isReadOnly: true);
			__Game_Vehicles_Controller_RO_ComponentLookup = state.GetComponentLookup<Controller>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_TransportStopData_RO_ComponentLookup = state.GetComponentLookup<TransportStopData>(isReadOnly: true);
			__Game_Prefabs_WorkStopData_RO_ComponentLookup = state.GetComponentLookup<WorkStopData>(isReadOnly: true);
		}
	}

	private ToolSystem m_ToolSystem;

	private IconCommandSystem m_IconCommandSystem;

	private EntityQuery m_EntityQuery;

	private EntityQuery m_UpdatedQuery;

	private EntityQuery m_InfomodeQuery;

	private EntityQuery m_IconQuery;

	private uint m_TransportTypeMask;

	private uint m_BuildingTypeMask;

	private uint m_BuildingStatusTypeMask;

	private uint m_VehicleTypeMask;

	private uint m_MarkerTypeMask;

	private bool m_Loaded;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_EntityQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Game.Routes.TransportStop>(),
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<Vehicle>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<CarTrailer>()
			}
		}, new EntityQueryDesc
		{
			Any = new ComponentType[1] { ComponentType.ReadOnly<Game.Objects.Marker>() },
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Owner>()
			}
		}, new EntityQueryDesc
		{
			Any = new ComponentType[1] { ComponentType.ReadOnly<Game.Objects.OutsideConnection>() },
			None = new ComponentType[1] { ComponentType.ReadOnly<Deleted>() }
		});
		m_UpdatedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Game.Routes.TransportStop>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Building>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Vehicle>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<CarTrailer>() }
		}, new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Game.Objects.Marker>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Owner>() }
		}, new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Game.Objects.OutsideConnection>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_InfomodeQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<InfomodeActive>() },
			Any = new ComponentType[5]
			{
				ComponentType.ReadOnly<InfoviewTransportStopData>(),
				ComponentType.ReadOnly<InfoviewBuildingData>(),
				ComponentType.ReadOnly<InfoviewBuildingStatusData>(),
				ComponentType.ReadOnly<InfoviewVehicleData>(),
				ComponentType.ReadOnly<InfoviewMarkerData>()
			}
		});
		m_IconQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[5]
			{
				ComponentType.ReadOnly<TransportStopMarkerData>(),
				ComponentType.ReadOnly<BuildingMarkerData>(),
				ComponentType.ReadOnly<VehicleMarkerData>(),
				ComponentType.ReadOnly<MarkerMarkerData>(),
				ComponentType.ReadOnly<PrefabData>()
			}
		});
	}

	public void PostDeserialize(Context context)
	{
		m_TransportTypeMask = uint.MaxValue;
		m_BuildingTypeMask = uint.MaxValue;
		m_BuildingStatusTypeMask = uint.MaxValue;
		m_VehicleTypeMask = uint.MaxValue;
		m_MarkerTypeMask = uint.MaxValue;
		m_Loaded = true;
	}

	private bool GetLoaded()
	{
		if (m_Loaded)
		{
			m_Loaded = false;
			return true;
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		EntityQuery entityQuery = (GetLoaded() ? m_EntityQuery : m_UpdatedQuery);
		TransportType requiredTransportStopType = TransportType.None;
		MarkerType requiredMarkerType = MarkerType.None;
		uint num = 0u;
		uint num2 = 0u;
		uint num3 = 0u;
		uint num4 = 0u;
		uint num5 = 0u;
		if (m_ToolSystem.activeTool != null)
		{
			if (m_ToolSystem.activeTool.requireStops != TransportType.None)
			{
				num |= (uint)(1 << (int)m_ToolSystem.activeTool.requireStops);
				requiredTransportStopType = m_ToolSystem.activeTool.requireStops;
			}
			if (m_ToolSystem.activeTool.requireStopIcons)
			{
				num = uint.MaxValue;
			}
		}
		if (m_ToolSystem.actionMode.IsEditor())
		{
			num5 |= 1;
			requiredMarkerType = MarkerType.CreatureSpawner;
		}
		NativeArray<ArchetypeChunk> infomodeChunks = default(NativeArray<ArchetypeChunk>);
		if (!m_InfomodeQuery.IsEmptyIgnoreFilter)
		{
			ComponentTypeHandle<InfoviewTransportStopData> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewTransportStopData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<InfoviewBuildingData> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewBuildingData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<InfoviewBuildingStatusData> typeHandle3 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewBuildingStatusData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<InfoviewVehicleData> typeHandle4 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewVehicleData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<InfoviewMarkerData> typeHandle5 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewMarkerData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			infomodeChunks = m_InfomodeQuery.ToArchetypeChunkArray(Allocator.TempJob);
			for (int i = 0; i < infomodeChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = infomodeChunks[i];
				NativeArray<InfoviewTransportStopData> nativeArray = archetypeChunk.GetNativeArray(ref typeHandle);
				NativeArray<InfoviewBuildingData> nativeArray2 = archetypeChunk.GetNativeArray(ref typeHandle2);
				NativeArray<InfoviewBuildingStatusData> nativeArray3 = archetypeChunk.GetNativeArray(ref typeHandle3);
				NativeArray<InfoviewVehicleData> nativeArray4 = archetypeChunk.GetNativeArray(ref typeHandle4);
				NativeArray<InfoviewMarkerData> nativeArray5 = archetypeChunk.GetNativeArray(ref typeHandle5);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					InfoviewTransportStopData infoviewTransportStopData = nativeArray[j];
					if (infoviewTransportStopData.m_Type != TransportType.None)
					{
						num |= (uint)(1 << (int)infoviewTransportStopData.m_Type);
					}
				}
				for (int k = 0; k < nativeArray2.Length; k++)
				{
					num2 |= (uint)(1 << (int)nativeArray2[k].m_Type);
				}
				for (int l = 0; l < nativeArray3.Length; l++)
				{
					num3 |= (uint)(1 << (int)nativeArray3[l].m_Type);
				}
				for (int m = 0; m < nativeArray4.Length; m++)
				{
					num4 |= (uint)(1 << (int)nativeArray4[m].m_Type);
				}
				for (int n = 0; n < nativeArray5.Length; n++)
				{
					num5 |= (uint)(1 << (int)nativeArray5[n].m_Type);
				}
			}
		}
		if (num == m_TransportTypeMask && num2 == m_BuildingTypeMask && num3 == m_BuildingStatusTypeMask && num4 == m_VehicleTypeMask && num5 == m_MarkerTypeMask && ((num == 0 && num2 == 0 && num3 == 0 && num4 == 0 && num5 == 0) || entityQuery.IsEmptyIgnoreFilter))
		{
			if (infomodeChunks.IsCreated)
			{
				infomodeChunks.Dispose();
			}
			return;
		}
		m_TransportTypeMask = num;
		m_BuildingTypeMask = num2;
		m_BuildingStatusTypeMask = num3;
		m_VehicleTypeMask = num4;
		m_MarkerTypeMask = num5;
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> iconChunks = m_IconQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new MarkerCreateJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_HiddenType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InfoviewTransportStopType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewTransportStopData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InfoviewBuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewBuildingData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InfoviewBuildingStatusType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewBuildingStatusData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InfoviewVehicleType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewVehicleData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InfoviewMarkerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewMarkerData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InfomodeActiveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfomodeActive_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransportStopMarkerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_TransportStopMarkerData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingMarkerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_BuildingMarkerData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_VehicleMarkerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_VehicleMarkerData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MarkerMarkerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_MarkerMarkerData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransportStopType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_TransportStop_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_VehicleType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Vehicle_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MarkerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Marker_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ControllerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ParkedCarType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ParkedTrainType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_ParkedTrain_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UniqueObjectType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_UniqueObject_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_IconElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Notifications_IconElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
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
			m_HospitalType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Hospital_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ElectricityProducerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ElectricityProducer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Transformer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BatteryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Battery_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WaterPumpingStationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_WaterPumpingStation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WaterTowerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_WaterTower_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SewageOutletType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_SewageOutlet_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WastewaterTreatmentPlantType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_WastewaterTreatmentPlant_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransportDepotType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_TransportDepot_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransportStationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_TransportStation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_GarbageFacilityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_GarbageFacility_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_FireStationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_FireStation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PoliceStationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_PoliceStation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RoadMaintenanceType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_RoadMaintenance_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ParkMaintenanceType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ParkMaintenance_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PostFacilityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_PostFacility_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TelecomFacilityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_TelecomFacility_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SchoolType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_School_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EmergencyShelterType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_EmergencyShelter_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DisasterFacilityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_DisasterFacility_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_FirewatchTowerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_FirewatchTower_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ParkType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Park_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DeathcareFacilityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_DeathcareFacility_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrisonType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Prison_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AdminBuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_AdminBuilding_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WelfareOfficeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_WelfareOffice_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ResearchFacilityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ResearchFacility_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CarParkingFacilityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_CarParkingFacility_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BicycleParkingFacilityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_BicycleParkingFacility_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ResidentialPropertyType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ResidentialProperty_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CommercialPropertyType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_CommercialProperty_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_IndustrialPropertyType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_IndustrialProperty_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OfficePropertyType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_OfficeProperty_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ExtractorPropertyType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ExtractorProperty_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ServiceUpgradeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ServiceUpgrade_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CoverageServiceType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Net_CoverageServiceType_SharedComponentTypeHandle, ref base.CheckedStateRef),
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
			m_CreatureSpawnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_CreatureSpawner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OutsideConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ElectricityOutsideConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_ElectricityOutsideConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WaterPipeOutsideConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_WaterPipeOutsideConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_IconData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Notifications_Icon_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ControllerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransportStopData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TransportStopData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WorkStopData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WorkStopData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_InfomodeChunks = infomodeChunks,
			m_IconChunks = iconChunks,
			m_RequiredTransportStopType = requiredTransportStopType,
			m_RequiredMarkerType = requiredMarkerType,
			m_RequireStandaloneStops = (num == uint.MaxValue),
			m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer()
		}, m_EntityQuery, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
		if (infomodeChunks.IsCreated)
		{
			infomodeChunks.Dispose(jobHandle);
		}
		iconChunks.Dispose(jobHandle);
		m_IconCommandSystem.AddCommandBufferWriter(jobHandle);
		base.Dependency = jobHandle;
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
	public MarkerCreateSystem()
	{
	}
}

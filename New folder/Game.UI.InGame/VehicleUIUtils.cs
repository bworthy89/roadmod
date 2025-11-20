using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Creatures;
using Game.Events;
using Game.Objects;
using Game.Prefabs;
using Game.Routes;
using Game.Simulation;
using Game.Vehicles;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.UI.InGame;

public static class VehicleUIUtils
{
	public readonly struct EntityWrapper
	{
		public Entity entity { get; }

		public EntityWrapper(Entity entity)
		{
			this.entity = entity;
		}

		public void Write(IJsonWriter writer, NameSystem nameSystem)
		{
			writer.TypeBegin(GetType().FullName);
			writer.PropertyName("entity");
			if (entity == Entity.Null)
			{
				writer.WriteNull();
			}
			else
			{
				writer.Write(entity);
			}
			writer.PropertyName("name");
			if (entity == Entity.Null)
			{
				writer.WriteNull();
			}
			else
			{
				nameSystem.BindName(writer, entity);
			}
			writer.TypeEnd();
		}
	}

	public static int GetAvailableVehicles(Entity vehicleOwnerEntity, EntityManager entityManager)
	{
		float efficiency = 1f;
		if (entityManager.TryGetBuffer(vehicleOwnerEntity, isReadOnly: true, out DynamicBuffer<Efficiency> buffer))
		{
			efficiency = Mathf.Min(BuildingUtils.GetEfficiency(buffer), 1f);
			efficiency = Mathf.Min(BuildingUtils.GetImmediateEfficiency(buffer), efficiency);
		}
		int num = 0;
		if (entityManager.TryGetComponent<PrefabRef>(vehicleOwnerEntity, out var component))
		{
			DeathcareFacilityData data2;
			EmergencyShelterData data3;
			FireStationData data4;
			HospitalData data5;
			MaintenanceDepotData data6;
			PoliceStationData data7;
			PrisonData data8;
			TransportDepotData data9;
			PostFacilityData data10;
			TransportCompanyData component2;
			if (UpgradeUtils.TryGetCombinedComponent<GarbageFacilityData>(entityManager, vehicleOwnerEntity, component.m_Prefab, out var data))
			{
				num += BuildingUtils.GetVehicleCapacity(efficiency, data.m_VehicleCapacity);
			}
			else if (UpgradeUtils.TryGetCombinedComponent<DeathcareFacilityData>(entityManager, vehicleOwnerEntity, component.m_Prefab, out data2))
			{
				num += BuildingUtils.GetVehicleCapacity(efficiency, data2.m_HearseCapacity);
			}
			else if (UpgradeUtils.TryGetCombinedComponent<EmergencyShelterData>(entityManager, vehicleOwnerEntity, component.m_Prefab, out data3))
			{
				num += BuildingUtils.GetVehicleCapacity(efficiency, data3.m_VehicleCapacity);
			}
			else if (UpgradeUtils.TryGetCombinedComponent<FireStationData>(entityManager, vehicleOwnerEntity, component.m_Prefab, out data4))
			{
				num += BuildingUtils.GetVehicleCapacity(efficiency, data4.m_FireEngineCapacity);
				num += BuildingUtils.GetVehicleCapacity(efficiency, data4.m_FireHelicopterCapacity);
			}
			else if (UpgradeUtils.TryGetCombinedComponent<HospitalData>(entityManager, vehicleOwnerEntity, component.m_Prefab, out data5))
			{
				num += BuildingUtils.GetVehicleCapacity(efficiency, data5.m_AmbulanceCapacity);
				num += BuildingUtils.GetVehicleCapacity(efficiency, data5.m_MedicalHelicopterCapacity);
			}
			else if (UpgradeUtils.TryGetCombinedComponent<MaintenanceDepotData>(entityManager, vehicleOwnerEntity, component.m_Prefab, out data6))
			{
				num += BuildingUtils.GetVehicleCapacity(efficiency, data6.m_VehicleCapacity);
			}
			else if (UpgradeUtils.TryGetCombinedComponent<PoliceStationData>(entityManager, vehicleOwnerEntity, component.m_Prefab, out data7))
			{
				num += BuildingUtils.GetVehicleCapacity(efficiency, data7.m_PatrolCarCapacity);
				num += BuildingUtils.GetVehicleCapacity(efficiency, data7.m_PoliceHelicopterCapacity);
			}
			else if (UpgradeUtils.TryGetCombinedComponent<PrisonData>(entityManager, vehicleOwnerEntity, component.m_Prefab, out data8))
			{
				num += BuildingUtils.GetVehicleCapacity(efficiency, data8.m_PrisonVanCapacity);
			}
			else if (UpgradeUtils.TryGetCombinedComponent<TransportDepotData>(entityManager, vehicleOwnerEntity, component.m_Prefab, out data9))
			{
				num += BuildingUtils.GetVehicleCapacity(efficiency, data9.m_VehicleCapacity);
			}
			else if (UpgradeUtils.TryGetCombinedComponent<PostFacilityData>(entityManager, vehicleOwnerEntity, component.m_Prefab, out data10))
			{
				num += BuildingUtils.GetVehicleCapacity(efficiency, data10.m_PostVanCapacity);
				num += BuildingUtils.GetVehicleCapacity(efficiency, data10.m_PostTruckCapacity);
			}
			else if (entityManager.TryGetComponent<TransportCompanyData>(component.m_Prefab, out component2))
			{
				num += BuildingUtils.GetVehicleCapacity(efficiency, component2.m_MaxTransports);
			}
		}
		return num;
	}

	public static Entity GetDestination(EntityManager entityManager, Entity vehicleEntity)
	{
		Entity entity = Entity.Null;
		if (entityManager.TryGetComponent<Target>(vehicleEntity, out var component))
		{
			entity = component.m_Target;
			if (entityManager.TryGetComponent<Connected>(entity, out var component2))
			{
				entity = component2.m_Connected;
			}
			if (entityManager.HasComponent<Game.Objects.OutsideConnection>(entity))
			{
				return entity;
			}
			if (entityManager.HasComponent<Vehicle>(entity))
			{
				return entity;
			}
			if (entityManager.HasComponent<CompanyData>(entity) && entityManager.TryGetComponent<PropertyRenter>(entity, out var component3))
			{
				return component3.m_Property;
			}
			if (entityManager.TryGetComponent<Owner>(entity, out var component4))
			{
				entity = component4.m_Owner;
			}
			if (entityManager.TryGetComponent<Game.Creatures.Resident>(entity, out var component5))
			{
				entity = component5.m_Citizen;
			}
			if (!entityManager.HasComponent<Connected>(component.m_Target) && entityManager.TryGetComponent<Waypoint>(component.m_Target, out var component6) && entityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<RouteWaypoint> buffer))
			{
				int num = component6.m_Index + 1;
				for (int i = 0; i < buffer.Length; i++)
				{
					num += i;
					num = math.select(num, 0, num >= buffer.Length);
					if (entityManager.TryGetComponent<Connected>(buffer[num].m_Waypoint, out component2))
					{
						entity = component2.m_Connected;
						break;
					}
				}
			}
		}
		return entity;
	}

	public static VehicleStateLocaleKey GetStateKey(Entity entity, EntityManager entityManager)
	{
		if (entityManager.TryGetComponent<Game.Vehicles.PublicTransport>(entity, out var component))
		{
			return GetStateKey(entity, component, entityManager);
		}
		if (entityManager.TryGetComponent<Game.Vehicles.PersonalCar>(entity, out var component2))
		{
			return GetStateKey(entity, component2, entityManager);
		}
		if (entityManager.TryGetComponent<Game.Vehicles.PostVan>(entity, out var component3))
		{
			return GetStateKey(entity, component3, entityManager);
		}
		if (entityManager.TryGetComponent<Game.Vehicles.PoliceCar>(entity, out var component4))
		{
			entityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<ServiceDispatch> buffer);
			return GetStateKey(entity, component4, buffer, entityManager);
		}
		if (entityManager.TryGetComponent<Game.Vehicles.MaintenanceVehicle>(entity, out var component5))
		{
			return GetStateKey(entity, component5, entityManager);
		}
		if (entityManager.TryGetComponent<Game.Vehicles.Ambulance>(entity, out var component6))
		{
			return GetStateKey(entity, component6, entityManager);
		}
		if (entityManager.TryGetComponent<Game.Vehicles.GarbageTruck>(entity, out var component7))
		{
			return GetStateKey(entity, component7, entityManager);
		}
		if (entityManager.TryGetComponent<Game.Vehicles.FireEngine>(entity, out var component8))
		{
			entityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<ServiceDispatch> buffer2);
			return GetStateKey(entity, component8, buffer2, entityManager);
		}
		if (entityManager.TryGetComponent<Game.Vehicles.DeliveryTruck>(entity, out var component9))
		{
			return GetStateKey(entity, component9, entityManager);
		}
		if (entityManager.TryGetComponent<Game.Vehicles.Hearse>(entity, out var component10))
		{
			return GetStateKey(entity, component10, entityManager);
		}
		if (entityManager.TryGetComponent<Game.Vehicles.CargoTransport>(entity, out var component11))
		{
			return GetStateKey(entity, component11, entityManager);
		}
		if (entityManager.TryGetComponent<Game.Vehicles.Taxi>(entity, out var component12))
		{
			return GetStateKey(entity, component12, entityManager);
		}
		return VehicleStateLocaleKey.Unknown;
	}

	public static VehicleStateLocaleKey GetStateKey(Entity entity, Game.Vehicles.PublicTransport publicTransportVehicle, EntityManager entityManager)
	{
		if (entityManager.HasComponent<InvolvedInAccident>(entity))
		{
			return VehicleStateLocaleKey.InvolvedInAccident;
		}
		if (entityManager.HasComponent<ParkedCar>(entity) || entityManager.HasComponent<ParkedTrain>(entity))
		{
			return VehicleStateLocaleKey.Parked;
		}
		if ((publicTransportVehicle.m_State & PublicTransportFlags.Returning) != 0)
		{
			return VehicleStateLocaleKey.Returning;
		}
		if ((publicTransportVehicle.m_State & PublicTransportFlags.Boarding) != 0)
		{
			return VehicleStateLocaleKey.Boarding;
		}
		if ((publicTransportVehicle.m_State & PublicTransportFlags.Evacuating) == 0)
		{
			return VehicleStateLocaleKey.EnRoute;
		}
		return VehicleStateLocaleKey.Evacuating;
	}

	public static VehicleStateLocaleKey GetStateKey(Entity entity, Game.Vehicles.PersonalCar personalCar, EntityManager entityManager)
	{
		if (entityManager.HasComponent<InvolvedInAccident>(entity))
		{
			return VehicleStateLocaleKey.InvolvedInAccident;
		}
		if (entityManager.HasComponent<ParkedCar>(entity))
		{
			return VehicleStateLocaleKey.Parked;
		}
		if ((personalCar.m_State & PersonalCarFlags.Boarding) != 0)
		{
			return VehicleStateLocaleKey.Boarding;
		}
		if ((personalCar.m_State & PersonalCarFlags.Disembarking) != 0)
		{
			return VehicleStateLocaleKey.Disembarking;
		}
		if ((personalCar.m_State & PersonalCarFlags.Transporting) != 0)
		{
			return VehicleStateLocaleKey.Transporting;
		}
		return VehicleStateLocaleKey.EnRoute;
	}

	public static VehicleStateLocaleKey GetStateKey(Entity entity, Game.Vehicles.PostVan postVan, EntityManager entityManager)
	{
		if (entityManager.HasComponent<InvolvedInAccident>(entity))
		{
			return VehicleStateLocaleKey.InvolvedInAccident;
		}
		if (entityManager.HasComponent<ParkedCar>(entity))
		{
			return VehicleStateLocaleKey.Parked;
		}
		if ((postVan.m_State & PostVanFlags.Delivering) != 0)
		{
			return VehicleStateLocaleKey.Delivering;
		}
		if ((postVan.m_State & PostVanFlags.Collecting) != 0)
		{
			return VehicleStateLocaleKey.Collecting;
		}
		if ((postVan.m_State & PostVanFlags.Returning) == 0)
		{
			return VehicleStateLocaleKey.Unknown;
		}
		return VehicleStateLocaleKey.Returning;
	}

	public static VehicleStateLocaleKey GetStateKey(Entity entity, Game.Vehicles.PoliceCar policeCar, DynamicBuffer<ServiceDispatch> dispatches, EntityManager entityManager)
	{
		if (entityManager.HasComponent<InvolvedInAccident>(entity))
		{
			return VehicleStateLocaleKey.InvolvedInAccident;
		}
		if (entityManager.HasComponent<ParkedCar>(entity))
		{
			return VehicleStateLocaleKey.Parked;
		}
		if ((policeCar.m_State & PoliceCarFlags.Returning) != 0)
		{
			return VehicleStateLocaleKey.Returning;
		}
		if ((policeCar.m_State & PoliceCarFlags.AccidentTarget) != 0 && policeCar.m_RequestCount > 0 && dispatches.IsCreated && dispatches.Length > 0)
		{
			if ((policeCar.m_State & PoliceCarFlags.AtTarget) != 0)
			{
				if (entityManager.TryGetComponent<PoliceEmergencyRequest>(dispatches[0].m_Request, out var component) && entityManager.TryGetComponent<AccidentSite>(component.m_Site, out var component2))
				{
					if ((component2.m_Flags & AccidentSiteFlags.TrafficAccident) != 0)
					{
						return VehicleStateLocaleKey.AccidentSite;
					}
					if ((component2.m_Flags & AccidentSiteFlags.CrimeScene) != 0)
					{
						return VehicleStateLocaleKey.CrimeScene;
					}
				}
			}
			else if (entityManager.HasComponent<PoliceEmergencyRequest>(dispatches[0].m_Request))
			{
				return VehicleStateLocaleKey.Dispatched;
			}
			return VehicleStateLocaleKey.Unknown;
		}
		return VehicleStateLocaleKey.Patrolling;
	}

	public static VehicleStateLocaleKey GetStateKey(Entity entity, Game.Vehicles.MaintenanceVehicle maintenanceVehicle, EntityManager entityManager)
	{
		if (entityManager.HasComponent<InvolvedInAccident>(entity))
		{
			return VehicleStateLocaleKey.InvolvedInAccident;
		}
		if (entityManager.HasComponent<ParkedCar>(entity))
		{
			return VehicleStateLocaleKey.Parked;
		}
		if ((maintenanceVehicle.m_State & MaintenanceVehicleFlags.TransformTarget) != 0)
		{
			if (entityManager.TryGetComponent<Target>(entity, out var component) && entityManager.HasComponent<InvolvedInAccident>(component.m_Target))
			{
				return VehicleStateLocaleKey.AccidentSite;
			}
			return VehicleStateLocaleKey.Dispatched;
		}
		if ((maintenanceVehicle.m_State & MaintenanceVehicleFlags.Returning) == 0)
		{
			return VehicleStateLocaleKey.Working;
		}
		return VehicleStateLocaleKey.Returning;
	}

	public static VehicleStateLocaleKey GetStateKey(Entity entity, Game.Vehicles.Ambulance ambulance, EntityManager entityManager)
	{
		if (entityManager.HasComponent<InvolvedInAccident>(entity))
		{
			return VehicleStateLocaleKey.InvolvedInAccident;
		}
		if (entityManager.HasComponent<ParkedCar>(entity))
		{
			return VehicleStateLocaleKey.Parked;
		}
		if ((ambulance.m_State & AmbulanceFlags.Returning) != 0)
		{
			return VehicleStateLocaleKey.Returning;
		}
		if ((ambulance.m_State & AmbulanceFlags.Transporting) == 0)
		{
			return VehicleStateLocaleKey.Dispatched;
		}
		return VehicleStateLocaleKey.Transporting;
	}

	public static VehicleStateLocaleKey GetStateKey(Entity entity, Game.Vehicles.GarbageTruck garbageTruck, EntityManager entityManager)
	{
		if (entityManager.HasComponent<InvolvedInAccident>(entity))
		{
			return VehicleStateLocaleKey.InvolvedInAccident;
		}
		if (entityManager.HasComponent<ParkedCar>(entity))
		{
			return VehicleStateLocaleKey.Parked;
		}
		if ((garbageTruck.m_State & GarbageTruckFlags.Returning) == 0)
		{
			return VehicleStateLocaleKey.Collecting;
		}
		return VehicleStateLocaleKey.Returning;
	}

	public static VehicleStateLocaleKey GetStateKey(Entity entity, Game.Vehicles.FireEngine fireEngine, DynamicBuffer<ServiceDispatch> dispatches, EntityManager entityManager)
	{
		if (entityManager.HasComponent<InvolvedInAccident>(entity))
		{
			return VehicleStateLocaleKey.InvolvedInAccident;
		}
		if ((fireEngine.m_State & FireEngineFlags.Extinguishing) != 0)
		{
			return VehicleStateLocaleKey.Extinguishing;
		}
		if ((fireEngine.m_State & FireEngineFlags.Rescueing) != 0)
		{
			return VehicleStateLocaleKey.Rescuing;
		}
		if (fireEngine.m_RequestCount > 0 && dispatches.Length > 0)
		{
			return VehicleStateLocaleKey.Dispatched;
		}
		if (!entityManager.HasComponent<ParkedCar>(entity))
		{
			return VehicleStateLocaleKey.Returning;
		}
		return VehicleStateLocaleKey.Parked;
	}

	public static VehicleStateLocaleKey GetStateKey(Entity entity, Game.Vehicles.DeliveryTruck truck, EntityManager entityManager)
	{
		if (entityManager.HasComponent<InvolvedInAccident>(entity))
		{
			return VehicleStateLocaleKey.InvolvedInAccident;
		}
		if (entityManager.HasComponent<ParkedCar>(entity))
		{
			return VehicleStateLocaleKey.Parked;
		}
		if ((truck.m_State & DeliveryTruckFlags.Returning) != 0)
		{
			return VehicleStateLocaleKey.Returning;
		}
		if ((truck.m_State & DeliveryTruckFlags.Buying) != 0)
		{
			return VehicleStateLocaleKey.Buying;
		}
		if ((truck.m_State & DeliveryTruckFlags.StorageTransfer) != 0)
		{
			if (entityManager.TryGetComponent<Owner>(entity, out var component) && entityManager.HasComponent<Game.Objects.OutsideConnection>(component.m_Owner))
			{
				return VehicleStateLocaleKey.Importing;
			}
			if (entityManager.TryGetComponent<Target>(entity, out var component2) && entityManager.HasComponent<Game.Objects.OutsideConnection>(component2.m_Target))
			{
				return VehicleStateLocaleKey.Exporting;
			}
		}
		if ((truck.m_State & DeliveryTruckFlags.Delivering) == 0)
		{
			return VehicleStateLocaleKey.Transporting;
		}
		return VehicleStateLocaleKey.Delivering;
	}

	public static VehicleStateLocaleKey GetStateKey(Entity entity, Game.Vehicles.Hearse hearse, EntityManager entityManager)
	{
		if (entityManager.HasComponent<InvolvedInAccident>(entity))
		{
			return VehicleStateLocaleKey.InvolvedInAccident;
		}
		if (entityManager.HasComponent<ParkedCar>(entity))
		{
			return VehicleStateLocaleKey.Parked;
		}
		if ((hearse.m_State & HearseFlags.Returning) != 0)
		{
			return VehicleStateLocaleKey.Returning;
		}
		if ((hearse.m_State & HearseFlags.Transporting) == 0)
		{
			return VehicleStateLocaleKey.Gathering;
		}
		return VehicleStateLocaleKey.Conveying;
	}

	public static VehicleStateLocaleKey GetStateKey(Entity entity, Game.Vehicles.CargoTransport cargoTransport, EntityManager entityManager)
	{
		if (entityManager.HasComponent<InvolvedInAccident>(entity))
		{
			return VehicleStateLocaleKey.InvolvedInAccident;
		}
		if (entityManager.HasComponent<ParkedCar>(entity) || entityManager.HasComponent<ParkedTrain>(entity))
		{
			return VehicleStateLocaleKey.Parked;
		}
		if ((cargoTransport.m_State & CargoTransportFlags.Returning) != 0)
		{
			return VehicleStateLocaleKey.Returning;
		}
		if ((cargoTransport.m_State & CargoTransportFlags.Boarding) == 0)
		{
			return VehicleStateLocaleKey.EnRoute;
		}
		return VehicleStateLocaleKey.Loading;
	}

	public static VehicleStateLocaleKey GetStateKey(Entity entity, Game.Vehicles.Taxi taxi, EntityManager entityManager)
	{
		if (entityManager.HasComponent<InvolvedInAccident>(entity))
		{
			return VehicleStateLocaleKey.InvolvedInAccident;
		}
		if (entityManager.HasComponent<ParkedCar>(entity))
		{
			return VehicleStateLocaleKey.Parked;
		}
		if ((taxi.m_State & TaxiFlags.Returning) != 0)
		{
			return VehicleStateLocaleKey.Returning;
		}
		if ((taxi.m_State & TaxiFlags.Dispatched) != 0)
		{
			return VehicleStateLocaleKey.Dispatched;
		}
		if ((taxi.m_State & TaxiFlags.Boarding) != 0)
		{
			return VehicleStateLocaleKey.Boarding;
		}
		if ((taxi.m_State & TaxiFlags.Transporting) != 0)
		{
			return VehicleStateLocaleKey.Transporting;
		}
		return VehicleStateLocaleKey.EnRoute;
	}

	public static VehicleLocaleKey GetPoliceVehicleLocaleKey(PolicePurpose purposeMask)
	{
		if ((purposeMask & PolicePurpose.Intelligence) != 0)
		{
			return VehicleLocaleKey.PoliceIntelligenceCar;
		}
		if ((purposeMask & PolicePurpose.Patrol) != 0)
		{
			return VehicleLocaleKey.PolicePatrolCar;
		}
		return VehicleLocaleKey.Vehicle;
	}
}

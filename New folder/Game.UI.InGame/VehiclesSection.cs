using System;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Citizens;
using Game.Companies;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class VehiclesSection : InfoSectionBase
{
	public readonly struct UIVehicle : IComparable<UIVehicle>
	{
		public Entity entity { get; }

		public VehicleLocaleKey vehicleKey { get; }

		public VehicleStateLocaleKey stateKey { get; }

		public UIVehicle(Entity entity, VehicleLocaleKey vehicleKey, VehicleStateLocaleKey stateKey)
		{
			this.entity = entity;
			this.vehicleKey = vehicleKey;
			this.stateKey = stateKey;
		}

		public int CompareTo(UIVehicle other)
		{
			int num = vehicleKey - other.vehicleKey;
			if (num == 0)
			{
				return stateKey - other.stateKey;
			}
			return num;
		}
	}

	private DynamicBuffer<OwnedVehicle> m_Buffer;

	private Entity m_CompanyEntity;

	protected override string group => "VehiclesSection";

	private VehicleLocaleKey vehicleKey { get; set; }

	private int vehicleCount { get; set; }

	private int availableVehicleCount { get; set; }

	private int vehicleCapacity { get; set; }

	private NativeList<UIVehicle> vehicleList { get; set; }

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		vehicleList = new NativeList<UIVehicle>(50, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		vehicleList.Dispose();
		base.OnDestroy();
	}

	protected override void Reset()
	{
		vehicleCount = 0;
		vehicleCapacity = 0;
		vehicleList.Clear();
		m_Buffer = default(DynamicBuffer<OwnedVehicle>);
		m_CompanyEntity = Entity.Null;
	}

	private bool Visible()
	{
		if (!base.EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out m_Buffer))
		{
			if (CompanyUIUtils.HasCompany(base.EntityManager, selectedEntity, selectedPrefab, out m_CompanyEntity) && base.EntityManager.TryGetComponent<PrefabRef>(m_CompanyEntity, out var component) && base.EntityManager.HasComponent<TransportCompanyData>(component.m_Prefab))
			{
				return base.EntityManager.TryGetBuffer(m_CompanyEntity, isReadOnly: true, out m_Buffer);
			}
			return false;
		}
		return true;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	protected override void OnProcess()
	{
		vehicleKey = VehicleLocaleKey.Vehicle;
		string item = VehicleLocaleKey.Vehicle.ToString();
		Entity entity = selectedEntity;
		Entity prefab = selectedPrefab;
		if (m_CompanyEntity != Entity.Null)
		{
			entity = m_CompanyEntity;
			prefab = (base.EntityManager.TryGetComponent<PrefabRef>(m_CompanyEntity, out var component) ? component.m_Prefab : Entity.Null);
		}
		if (TryGetComponentWithUpgrades<HospitalData>(entity, prefab, out var data))
		{
			int ambulanceCapacity = data.m_AmbulanceCapacity;
			int medicalHelicopterCapacity = data.m_MedicalHelicopterCapacity;
			vehicleCapacity += ambulanceCapacity + medicalHelicopterCapacity;
		}
		if (TryGetComponentWithUpgrades<PoliceStationData>(entity, prefab, out var data2))
		{
			int patrolCarCapacity = data2.m_PatrolCarCapacity;
			int policeHelicopterCapacity = data2.m_PoliceHelicopterCapacity;
			vehicleCapacity += patrolCarCapacity + policeHelicopterCapacity;
		}
		if (TryGetComponentWithUpgrades<FireStationData>(entity, prefab, out var data3))
		{
			int fireEngineCapacity = data3.m_FireEngineCapacity;
			int fireHelicopterCapacity = data3.m_FireHelicopterCapacity;
			vehicleCapacity += fireEngineCapacity + fireHelicopterCapacity;
		}
		if (TryGetComponentWithUpgrades<PostFacilityData>(entity, prefab, out var data4))
		{
			int postVanCapacity = data4.m_PostVanCapacity;
			int postTruckCapacity = data4.m_PostTruckCapacity;
			vehicleCapacity += postVanCapacity + postTruckCapacity;
		}
		if (TryGetComponentWithUpgrades<MaintenanceDepotData>(entity, prefab, out var data5))
		{
			vehicleCapacity += data5.m_VehicleCapacity;
		}
		if (TryGetComponentWithUpgrades<TransportDepotData>(entity, prefab, out var data6))
		{
			vehicleCapacity += data6.m_VehicleCapacity;
			item = VehicleLocaleKey.PublicTransportVehicle.ToString();
		}
		if (TryGetComponentWithUpgrades<DeathcareFacilityData>(entity, prefab, out var data7))
		{
			vehicleCapacity += data7.m_HearseCapacity;
		}
		if (TryGetComponentWithUpgrades<GarbageFacilityData>(entity, prefab, out var data8))
		{
			vehicleCapacity += data8.m_VehicleCapacity;
		}
		if (TryGetComponentWithUpgrades<PrisonData>(entity, prefab, out var data9))
		{
			vehicleCapacity += data9.m_PrisonVanCapacity;
		}
		if (TryGetComponentWithUpgrades<EmergencyShelterData>(entity, prefab, out var data10))
		{
			vehicleCapacity += data10.m_VehicleCapacity;
		}
		if (TryGetComponentWithUpgrades<TransportCompanyData>(entity, prefab, out var data11))
		{
			vehicleCapacity += data11.m_MaxTransports;
		}
		bool flag = m_Buffer.Length > 0 && vehicleCapacity == 0;
		if (base.EntityManager.HasComponent<Household>(selectedEntity))
		{
			item = VehicleLocaleKey.HouseholdVehicle.ToString();
		}
		for (int i = 0; i < m_Buffer.Length; i++)
		{
			Entity vehicle = m_Buffer[i].m_Vehicle;
			if (!base.EntityManager.HasComponent<ParkedCar>(vehicle) && !base.EntityManager.HasComponent<ParkedTrain>(vehicle))
			{
				AddVehicle(base.EntityManager, vehicle, vehicleList);
			}
		}
		base.tooltipKeys.Add(item);
		vehicleCount = vehicleList.Length;
		if (flag)
		{
			vehicleCapacity = m_Buffer.Length;
			availableVehicleCount = vehicleCapacity;
		}
		else
		{
			availableVehicleCount = VehicleUIUtils.GetAvailableVehicles(entity, base.EntityManager);
		}
		vehicleList.Sort();
	}

	public static void AddVehicle(EntityManager entityManager, Entity vehicle, NativeList<UIVehicle> vehicleList)
	{
		VehicleStateLocaleKey stateKey = VehicleUIUtils.GetStateKey(vehicle, entityManager);
		PrefabRef componentData = entityManager.GetComponentData<PrefabRef>(vehicle);
		VehicleLocaleKey vehicleLocaleKey = VehicleLocaleKey.Vehicle;
		if (entityManager.HasComponent<Car>(vehicle))
		{
			if (entityManager.HasComponent<Game.Vehicles.Ambulance>(vehicle))
			{
				vehicleLocaleKey = VehicleLocaleKey.Ambulance;
			}
			if (entityManager.TryGetComponent<PoliceCarData>(componentData.m_Prefab, out var component))
			{
				vehicleLocaleKey = VehicleUIUtils.GetPoliceVehicleLocaleKey(component.m_PurposeMask);
			}
			if (entityManager.HasComponent<Game.Vehicles.FireEngine>(vehicle))
			{
				vehicleLocaleKey = VehicleLocaleKey.FireEngine;
			}
			if (entityManager.HasComponent<Game.Vehicles.PostVan>(vehicle))
			{
				vehicleLocaleKey = VehicleLocaleKey.PostVan;
			}
			if (entityManager.HasComponent<Game.Vehicles.DeliveryTruck>(vehicle))
			{
				vehicleLocaleKey = VehicleLocaleKey.DeliveryTruck;
			}
			if (entityManager.HasComponent<Game.Vehicles.Hearse>(vehicle))
			{
				vehicleLocaleKey = VehicleLocaleKey.Hearse;
			}
			if (entityManager.HasComponent<Game.Vehicles.GarbageTruck>(vehicle))
			{
				vehicleLocaleKey = VehicleLocaleKey.GarbageTruck;
			}
			if (entityManager.HasComponent<Game.Vehicles.Taxi>(vehicle))
			{
				vehicleLocaleKey = VehicleLocaleKey.Taxi;
			}
			if (entityManager.HasComponent<Game.Vehicles.MaintenanceVehicle>(vehicle))
			{
				vehicleLocaleKey = VehicleLocaleKey.MaintenanceVehicle;
			}
			if (entityManager.HasComponent<Game.Vehicles.PublicTransport>(vehicle) && entityManager.TryGetComponent<PublicTransportVehicleData>(componentData.m_Prefab, out var component2))
			{
				vehicleLocaleKey = (((component2.m_PurposeMask & PublicTransportPurpose.PrisonerTransport) != 0) ? VehicleLocaleKey.PrisonVan : (((component2.m_PurposeMask & PublicTransportPurpose.Evacuation) != 0) ? VehicleLocaleKey.EvacuationBus : VehicleLocaleKey.PublicTransportVehicle));
			}
		}
		if (entityManager.HasComponent<Helicopter>(vehicle))
		{
			if (entityManager.HasComponent<Game.Vehicles.Ambulance>(vehicle))
			{
				vehicleLocaleKey = VehicleLocaleKey.MedicalHelicopter;
			}
			if (entityManager.HasComponent<Game.Vehicles.PoliceCar>(vehicle))
			{
				vehicleLocaleKey = VehicleLocaleKey.PoliceHelicopter;
			}
			if (entityManager.HasComponent<Game.Vehicles.FireEngine>(vehicle))
			{
				vehicleLocaleKey = VehicleLocaleKey.FireHelicopter;
			}
		}
		vehicleList.Add(new UIVehicle(vehicle, vehicleLocaleKey, stateKey));
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("vehicleKey");
		writer.Write(Enum.GetName(typeof(VehicleLocaleKey), vehicleKey));
		writer.PropertyName("vehicleCount");
		writer.Write(vehicleCount);
		writer.PropertyName("availableVehicleCount");
		writer.Write(availableVehicleCount);
		writer.PropertyName("vehicleCapacity");
		writer.Write(vehicleCapacity);
		vehicleList.Sort();
		writer.PropertyName("vehicleList");
		writer.ArrayBegin(vehicleList.Length);
		for (int i = 0; i < vehicleList.Length; i++)
		{
			BindVehicle(m_NameSystem, writer, vehicleList[i]);
		}
		writer.ArrayEnd();
	}

	public static void BindVehicle(NameSystem nameSystem, IJsonWriter binder, UIVehicle vehicle)
	{
		binder.TypeBegin("Game.UI.InGame.VehiclesSection.Vehicle");
		binder.PropertyName("entity");
		binder.Write(vehicle.entity);
		binder.PropertyName("name");
		nameSystem.BindName(binder, vehicle.entity);
		binder.PropertyName("vehicleKey");
		binder.Write(Enum.GetName(typeof(VehicleLocaleKey), vehicle.vehicleKey));
		binder.PropertyName("stateKey");
		binder.Write(Enum.GetName(typeof(VehicleStateLocaleKey), vehicle.stateKey));
		binder.TypeEnd();
	}

	[Preserve]
	public VehiclesSection()
	{
	}
}

using System;
using System.Collections.Generic;
using Game.Areas;
using Game.Buildings;
using Game.Simulation;
using Game.Vehicles;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[ComponentMenu("Buildings/CityServices/", new Type[]
{
	typeof(BuildingPrefab),
	typeof(BuildingExtensionPrefab),
	typeof(MarkerObjectPrefab)
})]
public class Hospital : ComponentBase, IServiceUpgrade
{
	public int m_AmbulanceCapacity = 10;

	public int m_MedicalHelicopterCapacity;

	public int m_PatientCapacity = 10;

	public int m_TreatmentBonus = 3;

	public int2 m_HealthRange = new int2(0, 100);

	public bool m_TreatDiseases = true;

	public bool m_TreatInjuries = true;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<HospitalData>());
		components.Add(ComponentType.ReadWrite<UpdateFrameData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		if (GetComponent<ServiceUpgrade>() == null)
		{
			components.Add(ComponentType.ReadWrite<Game.Buildings.Hospital>());
			if (GetComponent<CityServiceBuilding>() != null)
			{
				components.Add(ComponentType.ReadWrite<Efficiency>());
				components.Add(ComponentType.ReadWrite<ServiceUsage>());
			}
			components.Add(ComponentType.ReadWrite<OwnedVehicle>());
			components.Add(ComponentType.ReadWrite<ServiceDispatch>());
			if (GetComponent<UniqueObject>() == null)
			{
				components.Add(ComponentType.ReadWrite<ServiceDistrict>());
			}
			if (m_PatientCapacity != 0)
			{
				components.Add(ComponentType.ReadWrite<Patient>());
			}
		}
	}

	public void GetUpgradeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Buildings.Hospital>());
		components.Add(ComponentType.ReadWrite<ServiceDispatch>());
		components.Add(ComponentType.ReadWrite<OwnedVehicle>());
		components.Add(ComponentType.ReadWrite<ServiceUsage>());
		if (m_PatientCapacity != 0)
		{
			components.Add(ComponentType.ReadWrite<Patient>());
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		entityManager.SetComponentData(entity, new HospitalData
		{
			m_AmbulanceCapacity = m_AmbulanceCapacity,
			m_MedicalHelicopterCapacity = m_MedicalHelicopterCapacity,
			m_PatientCapacity = m_PatientCapacity,
			m_TreatmentBonus = m_TreatmentBonus,
			m_HealthRange = m_HealthRange,
			m_TreatDiseases = m_TreatDiseases,
			m_TreatInjuries = m_TreatInjuries
		});
		entityManager.SetComponentData(entity, new UpdateFrameData(1));
	}
}

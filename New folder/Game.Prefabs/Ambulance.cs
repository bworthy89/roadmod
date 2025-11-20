using System;
using System.Collections.Generic;
using Game.Objects;
using Game.Pathfind;
using Game.PSI;
using Game.Simulation;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Prefabs;

[ExcludeGeneratedModTag]
[ComponentMenu("Vehicles/", new Type[]
{
	typeof(CarPrefab),
	typeof(AircraftPrefab)
})]
public class Ambulance : ComponentBase
{
	public int m_PatientCapacity = 1;

	public override IEnumerable<string> modTags
	{
		get
		{
			foreach (string modTag in base.modTags)
			{
				yield return modTag;
			}
			if (GetComponent<AircraftPrefab>() != null)
			{
				yield return "AmbulanceAircraft";
			}
			else
			{
				yield return "Ambulance";
			}
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<AmbulanceData>());
		components.Add(ComponentType.ReadWrite<UpdateFrameData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Vehicles.Ambulance>());
		components.Add(ComponentType.ReadWrite<Passenger>());
		if (components.Contains(ComponentType.ReadWrite<Moving>()))
		{
			components.Add(ComponentType.ReadWrite<PathInformation>());
			components.Add(ComponentType.ReadWrite<ServiceDispatch>());
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		entityManager.SetComponentData(entity, new AmbulanceData(m_PatientCapacity));
		if (entityManager.HasComponent<CarData>(entity))
		{
			entityManager.SetComponentData(entity, new UpdateFrameData(0));
		}
	}
}

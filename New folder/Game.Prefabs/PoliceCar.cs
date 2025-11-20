using System;
using System.Collections.Generic;
using Game.Common;
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
public class PoliceCar : ComponentBase
{
	public int m_CriminalCapacity = 2;

	public float m_CrimeReductionRate = 10000f;

	public float m_ShiftDuration = 1f;

	[EnumFlag]
	public PolicePurpose m_Purposes = PolicePurpose.Patrol | PolicePurpose.Emergency;

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
				yield return "PoliceCarAircraft";
			}
			else
			{
				yield return "PoliceCar";
			}
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<PoliceCarData>());
		components.Add(ComponentType.ReadWrite<UpdateFrameData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Vehicles.PoliceCar>());
		components.Add(ComponentType.ReadWrite<Passenger>());
		components.Add(ComponentType.ReadWrite<PointOfInterest>());
		if (components.Contains(ComponentType.ReadWrite<Moving>()))
		{
			components.Add(ComponentType.ReadWrite<PathInformation>());
			components.Add(ComponentType.ReadWrite<ServiceDispatch>());
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		uint shiftDuration = (uint)(m_ShiftDuration * 262144f);
		entityManager.SetComponentData(entity, new PoliceCarData(m_CriminalCapacity, m_CrimeReductionRate, shiftDuration, m_Purposes));
		if (entityManager.HasComponent<CarData>(entity))
		{
			entityManager.SetComponentData(entity, new UpdateFrameData(5));
		}
	}
}

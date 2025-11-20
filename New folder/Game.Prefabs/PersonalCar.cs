using System;
using System.Collections.Generic;
using Game.Vehicles;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Vehicles/", new Type[]
{
	typeof(CarPrefab),
	typeof(CarTrailerPrefab)
})]
public class PersonalCar : ComponentBase
{
	public int m_PassengerCapacity = 5;

	public int m_BaggageCapacity = 5;

	public int m_CostToDrive = 8;

	[Range(0f, 100f)]
	public int m_Probability = 100;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<PersonalCarData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Vehicles.PersonalCar>());
		components.Add(ComponentType.ReadWrite<Passenger>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		entityManager.SetComponentData(entity, new PersonalCarData
		{
			m_PassengerCapacity = m_PassengerCapacity,
			m_BaggageCapacity = m_BaggageCapacity,
			m_CostToDrive = m_CostToDrive,
			m_Probability = m_Probability
		});
	}
}

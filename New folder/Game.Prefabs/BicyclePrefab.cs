using System;
using System.Collections.Generic;
using Game.Common;
using Game.Objects;
using Game.Pathfind;
using Game.Rendering;
using Game.Vehicles;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[ComponentMenu("Vehicles/", new Type[] { })]
public class BicyclePrefab : VehiclePrefab
{
	public float m_MaxSpeed = 60f;

	public float m_Acceleration = 5f;

	public float m_Braking = 10f;

	public float2 m_Turning = new float2(90f, 15f);

	public float m_Stiffness = 100f;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<CarData>());
		components.Add(ComponentType.ReadWrite<BicycleData>());
		components.Add(ComponentType.ReadWrite<SwayingData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
		components.Add(ComponentType.ReadWrite<Car>());
		components.Add(ComponentType.ReadWrite<Bicycle>());
		components.Add(ComponentType.ReadWrite<BlockedLane>());
		if (components.Contains(ComponentType.ReadWrite<Stopped>()))
		{
			components.Add(ComponentType.ReadWrite<ParkedCar>());
		}
		if (components.Contains(ComponentType.ReadWrite<Moving>()))
		{
			components.Add(ComponentType.ReadWrite<CarNavigation>());
			components.Add(ComponentType.ReadWrite<CarNavigationLane>());
			components.Add(ComponentType.ReadWrite<CarCurrentLane>());
			components.Add(ComponentType.ReadWrite<PathOwner>());
			components.Add(ComponentType.ReadWrite<PathElement>());
			components.Add(ComponentType.ReadWrite<Target>());
			components.Add(ComponentType.ReadWrite<Blocker>());
			components.Add(ComponentType.ReadWrite<Swaying>());
		}
	}
}

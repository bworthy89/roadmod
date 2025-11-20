using System.Collections.Generic;
using Game.Objects;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Prefabs;

public abstract class VehiclePrefab : MovingObjectPrefab
{
	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<VehicleData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
		components.Add(ComponentType.ReadWrite<Vehicle>());
		components.Add(ComponentType.ReadWrite<Color>());
		components.Add(ComponentType.ReadWrite<Surface>());
	}
}

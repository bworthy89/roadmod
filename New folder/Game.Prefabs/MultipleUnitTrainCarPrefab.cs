using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Vehicles/", new Type[] { })]
public class MultipleUnitTrainCarPrefab : TrainPrefab
{
	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<TrainCarriageData>());
		components.Add(ComponentType.ReadWrite<MultipleUnitTrainData>());
	}
}

using System;
using System.Collections.Generic;
using Game.Net;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Net/", new Type[]
{
	typeof(PathwayPrefab),
	typeof(TaxiwayPrefab),
	typeof(PowerLinePrefab),
	typeof(PipelinePrefab)
})]
public class MarkerNet : ComponentBase
{
	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<MarkerNetData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		if (components.Contains(ComponentType.ReadWrite<Edge>()))
		{
			components.Add(ComponentType.ReadWrite<Marker>());
		}
		else if (components.Contains(ComponentType.ReadWrite<Node>()))
		{
			components.Add(ComponentType.ReadWrite<Marker>());
		}
	}
}

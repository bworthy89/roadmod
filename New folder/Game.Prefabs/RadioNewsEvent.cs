using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Triggers/", new Type[]
{
	typeof(TriggerPrefab),
	typeof(StatisticTriggerPrefab)
})]
public class RadioNewsEvent : ComponentBase
{
	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
	}
}

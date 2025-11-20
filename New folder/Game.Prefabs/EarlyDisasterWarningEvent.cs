using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Events/", new Type[] { typeof(EventPrefab) })]
public class EarlyDisasterWarningEvent : ComponentBase
{
	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<EarlyDisasterWarningEventData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}
}
